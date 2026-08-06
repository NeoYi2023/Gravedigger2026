using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// Boss spawn marker on PushMap map Prefab (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossPoint : MonoBehaviour
    {
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.2f, 0.95f, 0.95f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.4f, 0.5f);
        }
    }
}
