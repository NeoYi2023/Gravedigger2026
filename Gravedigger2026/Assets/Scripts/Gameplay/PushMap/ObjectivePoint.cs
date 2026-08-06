using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Ordered PushMap objective marker (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectivePoint : MonoBehaviour
    {
        [SerializeField] private int _objectiveOrder = 1;
        [SerializeField] private CaptureZone _captureZone;

        public int ObjectiveOrder => Mathf.Max(1, _objectiveOrder);

        public CaptureZone CaptureZone => _captureZone;

        public void SetObjectiveOrder(int order)
        {
            _objectiveOrder = Mathf.Max(1, order);
        }

        public void SetCaptureZone(CaptureZone captureZone)
        {
            _captureZone = captureZone;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_captureZone == null)
            {
                _captureZone = GetComponent<CaptureZone>() ?? GetComponentInChildren<CaptureZone>(true);
            }
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.75f, 0.15f, 0.95f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.25f, 0.35f);
        }
    }
}
