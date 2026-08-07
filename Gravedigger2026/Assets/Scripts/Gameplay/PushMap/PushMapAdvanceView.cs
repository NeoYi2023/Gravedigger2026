using System;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PM-04: loyal-soldier advance toward shared CurrentObjective (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// Path = current ObjectivePoint transform. Capture-zone monster presence does NOT pause
    /// advance (probe feeds TickCapture only). Engage interrupt awaits WarriorCombat. Rebels do not advance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapAdvanceView : MonoBehaviour
    {
        private const float StoppingDistance = 0.2f;
        private const float NavMeshSampleRadius = 12f;

        private Func<ObjectivePoint> _currentObjectiveProvider;
        private NavMeshAgent _agent;
        private float _moveSpeed = 3.5f;
        private bool _isRebel;

        public bool IsRebel => _isRebel;

        public void Bind(Func<ObjectivePoint> currentObjectiveProvider, float moveSpeed)
        {
            _currentObjectiveProvider = currentObjectiveProvider ?? throw new ArgumentNullException(nameof(currentObjectiveProvider));
            _moveSpeed = Mathf.Max(0.1f, moveSpeed);

            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null)
            {
                _agent = gameObject.AddComponent<NavMeshAgent>();
            }

            _agent.speed = _moveSpeed;
            _agent.stoppingDistance = StoppingDistance;
            _agent.angularSpeed = 720f;
            _agent.acceleration = 24f;
            _agent.radius = 0.03f;
            _agent.height = 1.8f;
            _agent.autoBraking = true;
            // Facing via Animator DirIndex in PushMap as in Defend (SPEC_04 §15.2).
            _agent.updateRotation = false;

            TryWarpOntoNavMesh();
        }

        public void SetRebel(bool isRebel)
        {
            _isRebel = isRebel;
            if (isRebel && _agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        private void Update()
        {
            if (_isRebel || _agent == null || _currentObjectiveProvider == null)
            {
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                TryWarpOntoNavMesh();
                if (!_agent.isOnNavMesh)
                {
                    return;
                }
            }

            var objective = _currentObjectiveProvider();
            if (objective == null)
            {
                if (!_agent.isStopped)
                {
                    _agent.isStopped = true;
                }
                return;
            }

            if (_agent.isStopped)
            {
                _agent.isStopped = false;
            }

            var dest = objective.transform.position;
            if (NavMesh.SamplePosition(dest, out var destHit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                dest = destHit.position;
            }

            if (!_agent.hasPath || (_agent.destination - dest).sqrMagnitude > 0.04f)
            {
                _agent.SetDestination(dest);
            }
        }

        private void TryWarpOntoNavMesh()
        {
            if (_agent == null || _agent.isOnNavMesh)
            {
                return;
            }

            if (NavMesh.SamplePosition(transform.position, out var hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
        }
    }
}
