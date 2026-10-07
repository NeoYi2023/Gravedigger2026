using Gravedigger2026.Core.Coc;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Closed translucent red edge for each deploy-block polygon (SPEC_03 §3.21, D-103).
    /// No fill. Draws above the fog sheet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocDeployBlockOutlineView : MonoBehaviour
    {
        public const float LineWidth = 0.15f;
        public const float HeightAboveGround = 0.05f;
        public const int SortingOrder = 260;

        public static readonly Color LineColor = new Color(1f, 0.15f, 0.15f, 0.55f);

        private const string ShaderResourcePath = "Coc/CocDeployBlockLine";

        private Material _material;

        public static CocDeployBlockOutlineView Create(
            Transform parent,
            CocDeployBlockArea area,
            float groundY)
        {
            if (parent == null || area == null || !area.HasCoverage)
            {
                return null;
            }

            var shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null)
            {
                Debug.LogError("[CocDeployBlock] Shader missing at Resources/" + ShaderResourcePath + ".");
                return null;
            }

            var go = new GameObject("CocDeployBlockOutline");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CocDeployBlockOutlineView>();
            view._material = new Material(shader) { color = Color.white };
            var y = groundY + HeightAboveGround;
            var polygons = area.Polygons;
            for (var i = 0; i < polygons.Count; i++)
            {
                view.AddLoop(polygons[i], y);
            }

            return view;
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        private void AddLoop(Vector2[] poly, float y)
        {
            if (poly == null || poly.Length < 3 || _material == null)
            {
                return;
            }

            var lineGo = new GameObject("Edge");
            lineGo.transform.SetParent(transform, false);
            var line = lineGo.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = poly.Length;
            line.startWidth = LineWidth;
            line.endWidth = LineWidth;
            line.startColor = LineColor;
            line.endColor = LineColor;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = _material;
            line.sortingOrder = SortingOrder;
            for (var i = 0; i < poly.Length; i++)
            {
                line.SetPosition(i, new Vector3(poly[i].x, y, poly[i].y));
            }
        }
    }
}
