using System;
using UnityEngine;
using UnityEngine.AI;

namespace Gravedigger2026.Gameplay.PushMap
{
    /// <summary>
    /// PM-04: loyal-soldier advance toward shared CurrentObjective (SPEC_03 §3.14 / SPEC_04 §9.22).
    /// Path = current ObjectivePoint transform; pauses while a living monster is detected
    /// (Engage-interrupt placeholder; full targeting arrives with PM-05/06). Rebels do not advance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PushMapAdvanceView : MonoBehaviour
    {
        private const float StoppingDistance = 0.2f;

        private Func<ObjectivePoint> _currentObjectiveProvider;
        private Func<bool> _monsterThreatProvider;
        private NavMeshAgent _agent;
        private float _moveSpeed = 3.5f;
        private bool _isRebel;

        public bool IsRebel => _isRebel;

        public void Bind(
            Func<ObjectivePoint> currentObjectiveProvider,
            Func<bool> monsterThreatProvider,
            float moveSpeed)
        {
            _currentObjectiveProvider = currentObjectiveProvider ?? throw new ArgumentNullException(nameof(currentObjectiveProvider));
            _monsterThreatProvider = monsterThreatProvider;
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
            _agent.radius = 0.35f;
            _agent.height = 1.8f;
            _agent.autoBraking = true;
            // Facing via Animator DirIndex in PushMap as in Defend (SPEC_04 §15.2).
            _agent.updateRotation = false;

            if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
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
            if (_isRebel || _agent == null || !_agent.isOnNavMesh || _currentObjectiveProvider == null)
            {
                return;
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

            var threatened = _monsterThreatProvider != null && _monsterThreatProvider();
            if (threatened)
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
            if (!_agent.hasPath || (_agent.destination - dest).sqrMagnitude > 0.04f)
            {
                _agent.SetDestination(dest);
            }
        }
    }
}
