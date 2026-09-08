using Gravedigger2026.Core.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Pooled Overlay Image piece for UI-016 StepA rain (canvas reference pixels).
    /// Collision is a rotatable inset OBB resolved with SAT (SPEC_03 §3.15).
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))]
    public sealed class AmBodyPartPiece : MonoBehaviour
    {
        public const float DefaultMaxEdgePx = 49f;
        private const float SleepSpeedPx = 28f;
        private const float SleepAngularDeg = 22f;
        private const float MinAirTime = 0.18f;
        private const float MaxAngularSpeedDeg = 720f;
        private const float AngularGravityTorque = 0.01f;
        private const float CollisionSpinScale = 1.6f;
        private const float AxisEpsilon = 1e-5f;

        private RectTransform _rt;
        private Image _image;
        private AmBodyPartPool _owner;
        private Vector2 _velocity;
        private float _angleDeg;
        private float _angularVel;
        private float _airTime;
        private bool _sleeping;

        public bool IsSleeping => _sleeping;
        public Vector2 AnchoredPosition => _rt != null ? _rt.anchoredPosition : Vector2.zero;
        public Vector2 Size => _rt != null ? _rt.sizeDelta : Vector2.zero;

        public static AmBodyPartPiece EnsurePrefab()
        {
            var prefab = Resources.Load<GameObject>("Prefabs/AutoManufacture/AmBodyPartPiece");
            if (prefab != null)
            {
                var fromPrefab = prefab.GetComponent<AmBodyPartPiece>();
                if (fromPrefab != null && prefab.GetComponent<Image>() != null)
                {
                    return fromPrefab;
                }
            }

            var go = new GameObject(
                "AmBodyPartPiece",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AmBodyPartPiece));
            var piece = go.GetComponent<AmBodyPartPiece>();
            piece.ConfigureRuntime();
            return piece;
        }

        public void ConfigureRuntime()
        {
            CacheComponents();
            _image.raycastTarget = false;
            _image.preserveAspect = true;
            _image.color = Color.white;
        }

        public void BindPool(AmBodyPartPool owner)
        {
            _owner = owner;
        }

        public void Activate(
            Sprite sprite,
            Vector2 anchoredPosition,
            Color fallbackColor,
            float spawnAngleMaxDeg,
            float maxEdgePx)
        {
            CacheComponents();
            _sleeping = false;
            _airTime = 0f;
            _velocity = Vector2.zero;
            var yawCap = Mathf.Max(0f, spawnAngleMaxDeg);
            _angleDeg = yawCap > 0.01f ? Random.Range(-yawCap, yawCap) : 0f;
            _angularVel = yawCap > 0.01f ? Random.Range(-yawCap, yawCap) * 3f : 0f;
            gameObject.SetActive(true);
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.localScale = Vector3.one;
            _rt.anchoredPosition = anchoredPosition;
            _rt.sizeDelta = FitSize(sprite, maxEdgePx > 0.01f ? maxEdgePx : DefaultMaxEdgePx);
            ApplyAngle();
            if (sprite != null)
            {
                _image.sprite = sprite;
                _image.color = Color.white;
            }
            else
            {
                _image.sprite = null;
                _image.color = fallbackColor;
            }

            _image.enabled = true;
        }

        public void TickPhysics(
            float dt,
            float gravityPx,
            float bounciness,
            float friction,
            float floorY,
            float colliderInset,
            float bodyAngleMaxDeg,
            System.Collections.Generic.IReadOnlyList<AmBodyPartPiece> others)
        {
            if (_sleeping || _rt == null)
            {
                return;
            }

            _airTime += dt;
            _velocity.y -= gravityPx * dt;
            _velocity.x *= Mathf.Clamp01(1f - friction * dt * 2f);
            _angularVel *= Mathf.Clamp01(1f - friction * dt * 3f);
            _rt.anchoredPosition += _velocity * dt;
            _angleDeg += _angularVel * dt;
            ClampAngle(bodyAngleMaxDeg);
            ApplyAngle();

            ResolveFloor(dt, gravityPx, bounciness, friction, floorY, colliderInset);

            if (others != null)
            {
                for (var i = 0; i < others.Count; i++)
                {
                    var other = others[i];
                    if (other == null || other == this || !other.gameObject.activeSelf)
                    {
                        continue;
                    }

                    SeparateFrom(other, bounciness, colliderInset);
                }
            }

            LiftAboveFloor(floorY, colliderInset);

            _angularVel = Mathf.Clamp(_angularVel, -MaxAngularSpeedDeg, MaxAngularSpeedDeg);
            ClampAngle(bodyAngleMaxDeg);
            ApplyAngle();
            if (_airTime >= MinAirTime
                && _velocity.sqrMagnitude <= SleepSpeedPx * SleepSpeedPx
                && Mathf.Abs(_angularVel) <= SleepAngularDeg)
            {
                _velocity = Vector2.zero;
                _angularVel = 0f;
                _sleeping = true;
            }
        }

        public void Release()
        {
            if (_owner != null)
            {
                _owner.Release(this);
                return;
            }

            gameObject.SetActive(false);
        }

        public static Vector2 FitSize(Sprite sprite, float maxEdgePx)
        {
            if (sprite == null)
            {
                return new Vector2(72f, 72f);
            }

            var w = Mathf.Max(1f, sprite.rect.width);
            var h = Mathf.Max(1f, sprite.rect.height);
            var scale = maxEdgePx / Mathf.Max(w, h);
            return new Vector2(w * scale, h * scale);
        }

        public static Color FallbackColor(BodySlot slot)
        {
            switch (slot)
            {
                case BodySlot.Head:
                    return new Color(0.85f, 0.75f, 0.55f, 1f);
                case BodySlot.Torso:
                    return new Color(0.45f, 0.7f, 0.4f, 1f);
                case BodySlot.Arm:
                    return new Color(0.75f, 0.4f, 0.35f, 1f);
                case BodySlot.Leg:
                    return new Color(0.4f, 0.55f, 0.8f, 1f);
                default:
                    return new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }

        private void SeparateFrom(AmBodyPartPiece other, float bounciness, float colliderInset)
        {
            GetObb(colliderInset, out var center, out var right, out var up, out var half);
            other.GetObb(colliderInset, out var oCenter, out var oRight, out var oUp, out var oHalf);
            if (!TrySatMtv(center, right, up, half, oCenter, oRight, oUp, oHalf, out var mtv, out var overlap))
            {
                return;
            }

            var pos = _rt.anchoredPosition + mtv;
            _rt.anchoredPosition = pos;

            var n = mtv.sqrMagnitude > AxisEpsilon ? mtv.normalized : Vector2.up;
            var vn = Vector2.Dot(_velocity, n);
            if (vn < 0f)
            {
                _velocity -= n * vn * (1f + bounciness);
            }

            var rel = center - oCenter;
            _angularVel += (rel.x * n.y - rel.y * n.x) * CollisionSpinScale;
            _angularVel += overlap * Mathf.Sign(rel.x) * 0.35f;
        }

        private void ResolveFloor(
            float dt,
            float gravityPx,
            float bounciness,
            float friction,
            float floorY,
            float colliderInset)
        {
            GetObb(colliderInset, out var center, out var right, out var up, out var half);
            GetLowestCorner(center, right, up, half, out var lowest, out var minY);
            if (minY >= floorY)
            {
                return;
            }

            var pos = _rt.anchoredPosition;
            pos.y += floorY - minY;
            _rt.anchoredPosition = pos;
            if (_velocity.y < 0f)
            {
                _velocity.y *= -bounciness;
                if (Mathf.Abs(_velocity.y) < SleepSpeedPx)
                {
                    _velocity.y = 0f;
                }
            }

            _velocity.x *= Mathf.Clamp01(1f - friction * dt * 4f);
            var supportX = lowest.x;
            _angularVel -= (center.x - supportX) * gravityPx * AngularGravityTorque * dt;
            _angularVel *= Mathf.Clamp01(1f - friction * dt * 4f);
        }

        private void LiftAboveFloor(float floorY, float colliderInset)
        {
            GetObb(colliderInset, out var center, out var right, out var up, out var half);
            GetLowestCorner(center, right, up, half, out _, out var minY);
            if (minY >= floorY)
            {
                return;
            }

            var pos = _rt.anchoredPosition;
            pos.y += floorY - minY;
            _rt.anchoredPosition = pos;
        }

        private void GetObb(
            float colliderInset,
            out Vector2 center,
            out Vector2 right,
            out Vector2 up,
            out Vector2 half)
        {
            var inset = Mathf.Clamp(colliderInset, 0.35f, 1f);
            var rad = _angleDeg * Mathf.Deg2Rad;
            var c = Mathf.Cos(rad);
            var s = Mathf.Sin(rad);
            center = _rt.anchoredPosition;
            right = new Vector2(c, s);
            up = new Vector2(-s, c);
            half = _rt.sizeDelta * (0.5f * inset);
        }

        private static bool TrySatMtv(
            Vector2 center,
            Vector2 right,
            Vector2 up,
            Vector2 half,
            Vector2 oCenter,
            Vector2 oRight,
            Vector2 oUp,
            Vector2 oHalf,
            out Vector2 mtv,
            out float overlap)
        {
            mtv = Vector2.zero;
            overlap = float.MaxValue;
            Vector2 bestAxis = Vector2.up;

            if (!TestAxis(center, right, up, half, oCenter, oRight, oUp, oHalf, right, ref overlap, ref bestAxis)
                || !TestAxis(center, right, up, half, oCenter, oRight, oUp, oHalf, up, ref overlap, ref bestAxis)
                || !TestAxis(center, right, up, half, oCenter, oRight, oUp, oHalf, oRight, ref overlap, ref bestAxis)
                || !TestAxis(center, right, up, half, oCenter, oRight, oUp, oHalf, oUp, ref overlap, ref bestAxis))
            {
                return false;
            }

            if (overlap <= 0f || overlap >= float.MaxValue * 0.5f)
            {
                return false;
            }

            if (Vector2.Dot(center - oCenter, bestAxis) < 0f)
            {
                bestAxis = -bestAxis;
            }

            mtv = bestAxis * overlap;
            return true;
        }

        private static bool TestAxis(
            Vector2 center,
            Vector2 right,
            Vector2 up,
            Vector2 half,
            Vector2 oCenter,
            Vector2 oRight,
            Vector2 oUp,
            Vector2 oHalf,
            Vector2 axis,
            ref float minOverlap,
            ref Vector2 bestAxis)
        {
            if (axis.sqrMagnitude < AxisEpsilon)
            {
                return true;
            }

            axis.Normalize();
            ProjectObb(center, right, up, half, axis, out var minA, out var maxA);
            ProjectObb(oCenter, oRight, oUp, oHalf, axis, out var minB, out var maxB);
            var o = Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);
            if (o <= 0f)
            {
                return false;
            }

            if (o < minOverlap)
            {
                minOverlap = o;
                bestAxis = axis;
            }

            return true;
        }

        private static void ProjectObb(
            Vector2 center,
            Vector2 right,
            Vector2 up,
            Vector2 half,
            Vector2 axis,
            out float min,
            out float max)
        {
            var c = Vector2.Dot(center, axis);
            var r = Mathf.Abs(Vector2.Dot(right, axis)) * half.x
                + Mathf.Abs(Vector2.Dot(up, axis)) * half.y;
            min = c - r;
            max = c + r;
        }

        private static void GetLowestCorner(
            Vector2 center,
            Vector2 right,
            Vector2 up,
            Vector2 half,
            out Vector2 lowest,
            out float minY)
        {
            lowest = center;
            minY = float.MaxValue;
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sy = -1; sy <= 1; sy += 2)
                {
                    var p = center + right * (half.x * sx) + up * (half.y * sy);
                    if (p.y < minY)
                    {
                        minY = p.y;
                        lowest = p;
                    }
                }
            }
        }

        private void ClampAngle(float bodyAngleMaxDeg)
        {
            var max = Mathf.Max(0f, bodyAngleMaxDeg);
            if (max <= 0.01f)
            {
                _angleDeg = 0f;
                _angularVel = 0f;
                return;
            }

            if (_angleDeg > max)
            {
                _angleDeg = max;
                _angularVel = 0f;
            }
            else if (_angleDeg < -max)
            {
                _angleDeg = -max;
                _angularVel = 0f;
            }
        }

        private void ApplyAngle()
        {
            _rt.localRotation = Quaternion.Euler(0f, 0f, _angleDeg);
        }

        private void CacheComponents()
        {
            if (_rt == null)
            {
                _rt = GetComponent<RectTransform>();
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }
    }
}
