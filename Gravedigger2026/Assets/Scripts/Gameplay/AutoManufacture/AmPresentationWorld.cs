using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Off-screen ortho Physics2D world (kept for editor/legacy). Runtime UI-016 rain
    /// composites as Overlay Images — ScreenSpaceOverlay covers a world camera / zero-size RT.
    /// </summary>
    public sealed class AmPresentationWorld : MonoBehaviour
    {
        public const int RtWidth = 1920;
        public const int RtHeight = 1080;
        private const float WorldOriginY = 800f;
        private const float DropSpawnYPx = 520f;

        private Camera _camera;
        private RenderTexture _target;
        private Transform _bodiesRoot;
        private Transform _soldiersRoot;
        private Transform _fxRoot;
        private BoxCollider2D _floorCollider;
        private BoxCollider2D _soldierFloorCollider;
        private AutoMfgPresentationConstants _constants;

        public Camera PresentationCamera => _camera;
        public RenderTexture TargetTexture => _target;
        public Transform BodiesRoot => _bodiesRoot;
        public Transform SoldiersRoot => _soldiersRoot;
        public Transform FxRoot => _fxRoot;
        public AutoMfgPresentationConstants Constants => _constants;

        public static AmPresentationWorld Ensure(AutoMfgPresentationConstants constants)
        {
            AmPresentationLayers.ConfigureCollisionMatrix();

            var go = new GameObject("AmPresentationWorld");
            // Absolute space so Overlay Canvas parents do not distort Physics2D / sprites.
            go.transform.SetParent(null, false);
            go.transform.position = new Vector3(0f, WorldOriginY, 0f);

            var world = go.AddComponent<AmPresentationWorld>();
            world.Build(constants);
            return world;
        }

        public void ApplyConstants(AutoMfgPresentationConstants constants)
        {
            _constants = constants;
            if (_camera != null)
            {
                _camera.orthographicSize = AutoMfgPresentationConstants.OrthoSize;
            }

            EnsureRenderTexture();
            RefreshFloor();
        }

        public Vector2 ResolveDropSpawn(float jitterXPx)
        {
            var x = Random.Range(-jitterXPx, jitterXPx);
            return LocalPixelsToWorld(x, DropSpawnYPx);
        }

        public Vector2 ResolvePileCenter()
        {
            return LocalPixelsToWorld(0f, _constants.PileFloorYPx);
        }

        public Vector2 ResolveReviveSpawn()
        {
            return LocalPixelsToWorld(0f, _constants.PileFloorYPx + _constants.ReviveSpawnOffsetYPx);
        }

        private Vector2 LocalPixelsToWorld(float xPx, float yPx)
        {
            var local = _constants.PixelsToWorld(xPx, yPx);
            var p = transform.TransformPoint(new Vector3(local.x, local.y, 0f));
            return new Vector2(p.x, p.y);
        }

        private void Build(AutoMfgPresentationConstants constants)
        {
            _constants = constants;

            var camGo = new GameObject("AmPresentationCamera", typeof(Camera));
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -10f);
            camGo.transform.localRotation = Quaternion.identity;
            _camera = camGo.GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = AutoMfgPresentationConstants.OrthoSize;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 50f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.depth = -100;
            _camera.cullingMask = BuildCullingMask();
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            EnsureRenderTexture();

            _bodiesRoot = CreateChild(transform, "Bodies");
            _soldiersRoot = CreateChild(transform, "Soldiers");
            _fxRoot = CreateChild(transform, "Fx");

            var floorGo = new GameObject("PileFloor", typeof(BoxCollider2D));
            floorGo.transform.SetParent(transform, false);
            floorGo.layer = Mathf.Max(0, AmPresentationLayers.FloorLayer);
            _floorCollider = floorGo.GetComponent<BoxCollider2D>();

            var soldierFloorGo = new GameObject("SoldierFloor", typeof(BoxCollider2D));
            soldierFloorGo.transform.SetParent(transform, false);
            soldierFloorGo.layer = Mathf.Max(0, AmPresentationLayers.SoldierLayer);
            _soldierFloorCollider = soldierFloorGo.GetComponent<BoxCollider2D>();

            RefreshFloor();
        }

        private void EnsureRenderTexture()
        {
            if (_camera == null)
            {
                return;
            }

            if (_target != null
                && _target.IsCreated()
                && _target.width == RtWidth
                && _target.height == RtHeight)
            {
                _camera.targetTexture = _target;
                return;
            }

            if (_target != null)
            {
                _camera.targetTexture = null;
                _target.Release();
                Destroy(_target);
            }

            _target = new RenderTexture(RtWidth, RtHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "AmPresentationRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _target.Create();
            _camera.targetTexture = _target;
        }

        private static int BuildCullingMask()
        {
            var mask = 0;
            if (AmPresentationLayers.BodyLayer >= 0)
            {
                mask |= 1 << AmPresentationLayers.BodyLayer;
            }

            if (AmPresentationLayers.SoldierLayer >= 0)
            {
                mask |= 1 << AmPresentationLayers.SoldierLayer;
            }

            if (AmPresentationLayers.FloorLayer >= 0)
            {
                mask |= 1 << AmPresentationLayers.FloorLayer;
            }

            // Default: mystery / magic-circle until layered.
            mask |= 1 << 0;
            return mask;
        }

        private void RefreshFloor()
        {
            if (_floorCollider == null)
            {
                return;
            }

            var width = 1920f / AutoMfgPresentationConstants.PixelsPerUnit;
            _floorCollider.size = new Vector2(width, 0.2f);
            _floorCollider.offset = Vector2.zero;
            _floorCollider.transform.localPosition = new Vector3(
                0f,
                _constants.PileFloorWorldY,
                0f);

            if (_soldierFloorCollider != null)
            {
                _soldierFloorCollider.size = new Vector2(width, 0.2f);
                _soldierFloorCollider.offset = Vector2.zero;
                _soldierFloorCollider.transform.localPosition = new Vector3(
                    0f,
                    _constants.SoldierLandWorldY,
                    0f);
            }
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private void OnDestroy()
        {
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
                _target = null;
            }
        }
    }
}
