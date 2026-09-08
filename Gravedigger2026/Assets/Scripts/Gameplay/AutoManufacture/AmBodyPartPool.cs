using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Gameplay.AutoManufacture
{
    /// <summary>
    /// Simple pool for <see cref="AmBodyPartPiece"/> Overlay Images.
    /// </summary>
    public sealed class AmBodyPartPool
    {
        private readonly Stack<AmBodyPartPiece> _inactive = new Stack<AmBodyPartPiece>();
        private readonly Transform _parent;
        private readonly AmBodyPartPiece _prototype;

        public AmBodyPartPool(Transform parent)
        {
            _parent = parent;
            _prototype = AmBodyPartPiece.EnsurePrefab();
            if (_prototype != null)
            {
                _prototype.gameObject.SetActive(false);
                if (_prototype.transform.parent == null && parent != null)
                {
                    _prototype.transform.SetParent(parent, false);
                }
            }
        }

        public AmBodyPartPiece Rent()
        {
            AmBodyPartPiece piece;
            if (_inactive.Count > 0)
            {
                piece = _inactive.Pop();
            }
            else
            {
                piece = Object.Instantiate(_prototype, _parent);
                piece.name = "AmBodyPart_" + _inactive.Count;
                piece.ConfigureRuntime();
            }

            piece.BindPool(this);
            return piece;
        }

        public void Release(AmBodyPartPiece piece)
        {
            if (piece == null)
            {
                return;
            }

            piece.gameObject.SetActive(false);
            piece.transform.SetParent(_parent, false);
            _inactive.Push(piece);
        }

        public void Clear()
        {
            while (_inactive.Count > 0)
            {
                var piece = _inactive.Pop();
                if (piece != null)
                {
                    Object.Destroy(piece.gameObject);
                }
            }
        }
    }
}
