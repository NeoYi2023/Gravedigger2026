using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PushMap spawn-point marker; SpawnPointId matches PushMapSpawnConfig (SPEC_04 §9.22/§9.23).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnPointId = "SP_01";

        public string SpawnPointId => string.IsNullOrWhiteSpace(_spawnPointId)
            ? name
            : _spawnPointId.Trim();

        public void SetSpawnPointId(string spawnPointId)
        {
            _spawnPointId = spawnPointId ?? string.Empty;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.35f, 0.35f, 0.95f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.2f, 0.3f);
        }
    }
}
