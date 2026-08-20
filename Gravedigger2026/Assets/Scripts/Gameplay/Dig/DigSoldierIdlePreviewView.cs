using Gravedigger2026.Gameplay.Defend;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.Dig
{
    /// <summary>
    /// Idle-only soldier preview at a grave (D-078). Destroys the View after duration; does not own the pool instance.
    /// </summary>
    public sealed class DigSoldierIdlePreviewView : MonoBehaviour
    {
        public const float PreviewScale = 2f;

        private float _remaining;

        public void Play(GameObject appearancePrefab, Vector3 worldPosition, float durationSeconds)
        {
            transform.position = worldPosition;
            _remaining = Mathf.Max(0.01f, durationSeconds);
            if (appearancePrefab == null)
            {
                Destroy(gameObject);
                return;
            }

            var instance = Instantiate(appearancePrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * PreviewScale;

            var agent = instance.GetComponent<WarriorAgentView>();
            if (agent != null)
            {
                agent.enabled = false;
            }

            var nav = instance.GetComponent<NavMeshAgent>();
            if (nav != null)
            {
                nav.enabled = false;
            }

            var anim = instance.GetComponent<WarriorAnimView>()
                       ?? instance.GetComponentInChildren<WarriorAnimView>(true);
            if (anim != null)
            {
                anim.ResetToIdle();
                anim.SetMoving(false);
            }
        }

        private void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
