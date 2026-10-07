using Gravedigger2026.Core.Coc;
using UnityEngine;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// Ground sprite of fog cells (SPEC_03 §3.21, Approach A).
    /// The alpha sheet is one texel per 0.25 cell. Group ids live on a linear R8 texture
    /// so the 0.5 GPU fade does not pick up sRGB noise. Refresh writes when the grid changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocFogOverlayView : MonoBehaviour
    {
        public const int SortingOrder = 250;
        public const float EdgeFade = 0.5f;
        public const int FadeCells = 2;

        private const float HeightAboveGround = 0.02f;
        private const string ShaderResourcePath = "Coc/CocFogSoft";

        private Texture2D _texture;
        private Texture2D _groupTexture;
        private Sprite _sprite;
        private Material _material;
        private Color[] _pixels;
        private Color[] _groupPixels;
        private float _unexploredAlpha = 0.9f;
        private float _exploredAlpha = 0.7f;
        private int _texWidth;
        private int _texHeight;

        public static CocFogOverlayView Create(
            Transform parent,
            CocFogGrid grid,
            float unexploredAlpha,
            float exploredAlpha,
            float groundY)
        {
            if (parent == null || grid == null || !grid.HasCoverage)
            {
                return null;
            }

            var go = new GameObject("CocFogOverlay");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CocFogOverlayView>();
            view.Build(grid, unexploredAlpha, exploredAlpha, groundY);
            return view;
        }

        public void Refresh(CocFogGrid grid)
        {
            if (_texture == null || grid == null || !grid.HasCoverage)
            {
                return;
            }

            Paint(grid);
            _texture.Apply(false, false);
            if (_groupTexture != null)
            {
                _groupTexture.Apply(false, false);
            }
        }

        private void Build(CocFogGrid grid, float unexploredAlpha, float exploredAlpha, float groundY)
        {
            _unexploredAlpha = Mathf.Clamp01(unexploredAlpha);
            _exploredAlpha = Mathf.Clamp01(exploredAlpha);
            _texWidth = grid.Width + FadeCells * 2;
            _texHeight = grid.Height + FadeCells * 2;

            _texture = new Texture2D(_texWidth, _texHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "CocFogGrid"
            };
            _groupTexture = new Texture2D(_texWidth, _texHeight, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "CocFogGroups"
            };
            _pixels = new Color[_texWidth * _texHeight];
            _groupPixels = new Color[_texWidth * _texHeight];

            Paint(grid);
            _texture.Apply(false, false);
            _groupTexture.Apply(false, false);

            var worldW = _texWidth * CocFogGrid.CellSize;
            var worldH = _texHeight * CocFogGrid.CellSize;
            _sprite = Sprite.Create(
                _texture,
                new Rect(0f, 0f, _texWidth, _texHeight),
                new Vector2(0.5f, 0.5f),
                1f / CocFogGrid.CellSize,
                0,
                SpriteMeshType.FullRect);
            _sprite.name = "CocFogGrid";

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sortingOrder = SortingOrder;
            renderer.color = Color.white;
            var shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader != null)
            {
                _material = new Material(shader);
                _material.SetTexture("_GroupTex", _groupTexture);
                renderer.material = _material;
            }
            else
            {
                Debug.LogWarning("[CocFog] CocFogSoft shader missing. Fog edges stay hard.");
            }

            var originX = grid.OriginX - FadeCells * CocFogGrid.CellSize;
            var originZ = grid.OriginZ - FadeCells * CocFogGrid.CellSize;
            transform.position = new Vector3(
                originX + worldW * 0.5f,
                groundY + HeightAboveGround,
                originZ + worldH * 0.5f);
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void Paint(CocFogGrid grid)
        {
            if (_texture == null || _pixels == null || _groupPixels == null)
            {
                return;
            }

            for (var tz = 0; tz < _texHeight; tz++)
            {
                var iz = tz - FadeCells;
                for (var tx = 0; tx < _texWidth; tx++)
                {
                    var ix = tx - FadeCells;
                    var index = tz * _texWidth + tx;
                    var group = grid.GetGroup(ix, iz);
                    var encodedGroup = group <= CocFogGrid.NoGroup
                        ? 0f
                        : (group + 1) / 255f;
                    _pixels[index] = new Color(0f, 0f, 0f, CellAlpha(grid.GetCell(ix, iz)));
                    _groupPixels[index] = new Color(encodedGroup, 0f, 0f, 1f);
                }
            }

            _texture.SetPixels(_pixels);
            _groupTexture.SetPixels(_groupPixels);
        }

        private float CellAlpha(byte cell)
        {
            if (cell == CocFogGrid.Unseen)
            {
                return _unexploredAlpha;
            }

            if (cell == CocFogGrid.Explored)
            {
                return _exploredAlpha;
            }

            return 0f;
        }

        private void OnDestroy()
        {
            if (_sprite != null)
            {
                Destroy(_sprite);
                _sprite = null;
            }

            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }

            if (_groupTexture != null)
            {
                Destroy(_groupTexture);
                _groupTexture = null;
            }

            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }

            _pixels = null;
            _groupPixels = null;
        }
    }
}
