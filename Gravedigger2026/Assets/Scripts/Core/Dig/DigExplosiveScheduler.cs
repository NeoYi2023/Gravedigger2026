using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    /// <summary>
    /// Pending explosive barrels: flight then fuse then blast (D-077). Pure C#; Views subscribe.
    /// </summary>
    public sealed class DigExplosiveScheduler
    {
        private readonly List<PendingBarrel> _pending = new List<PendingBarrel>(8);
        private readonly List<PendingBarrel> _tickScratch = new List<PendingBarrel>(8);
        private int _nextBarrelId = 1;

        public event Action<int, Vector3, Vector3, float> BarrelQueued;
        public event Action<int, Vector3, float, float> BlastStarted;

        public int PendingCount => _pending.Count;

        public int Enqueue(
            Vector3 origin,
            Vector3 target,
            float flightSeconds,
            float fuseSeconds,
            float blastRadius,
            float blastDamage,
            float ringSeconds)
        {
            var id = _nextBarrelId++;
            _pending.Add(new PendingBarrel
            {
                BarrelId = id,
                Origin = origin,
                Target = target,
                FlightRemaining = Mathf.Max(0.01f, flightSeconds),
                FuseRemaining = Mathf.Max(0f, fuseSeconds),
                BlastRadius = blastRadius,
                BlastDamage = blastDamage,
                RingSeconds = ringSeconds,
                Flying = true
            });
            BarrelQueued?.Invoke(id, origin, target, Mathf.Max(0.01f, flightSeconds));
            return id;
        }

        public void Tick(float deltaTime, Action<Vector3, float, float> onBlast)
        {
            if (deltaTime <= 0f || _pending.Count == 0)
            {
                return;
            }

            _tickScratch.Clear();
            for (var i = 0; i < _pending.Count; i++)
            {
                _tickScratch.Add(_pending[i]);
            }

            for (var i = 0; i < _tickScratch.Count; i++)
            {
                var barrel = _tickScratch[i];
                if (barrel.Flying)
                {
                    barrel.FlightRemaining -= deltaTime;
                    if (barrel.FlightRemaining > 0f)
                    {
                        continue;
                    }

                    barrel.Flying = false;
                    continue;
                }

                barrel.FuseRemaining -= deltaTime;
                if (barrel.FuseRemaining > 0f)
                {
                    continue;
                }

                _pending.Remove(barrel);
                BlastStarted?.Invoke(barrel.BarrelId, barrel.Target, barrel.BlastRadius, barrel.RingSeconds);
                onBlast?.Invoke(barrel.Target, barrel.BlastRadius, barrel.BlastDamage);
            }
        }

        public void Clear()
        {
            _pending.Clear();
        }

        private sealed class PendingBarrel
        {
            public int BarrelId;
            public Vector3 Origin;
            public Vector3 Target;
            public float FlightRemaining;
            public float FuseRemaining;
            public float BlastRadius;
            public float BlastDamage;
            public float RingSeconds;
            public bool Flying;
        }
    }
}
