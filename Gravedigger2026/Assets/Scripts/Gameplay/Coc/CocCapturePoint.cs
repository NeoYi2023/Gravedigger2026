using UnityEngine;

namespace Gravedigger2026.Gameplay.Coc
{
    /// <summary>
    /// COC capture marker (SPEC_04 §9.35 / §9.37). Radius lives on CocCapturePointConfig.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CocCapturePoint : MonoBehaviour
    {
        [SerializeField] private string _capturePointId = "CP_01";

        public string CapturePointId => string.IsNullOrWhiteSpace(_capturePointId)
            ? name
            : _capturePointId.Trim();

        public void SetCapturePointId(string capturePointId)
        {
            _capturePointId = capturePointId ?? string.Empty;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.2f, 0.95f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.35f, 0.45f);
        }
    }
}
