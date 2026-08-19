using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Defend;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>Mutable per-dispatch context passed to SkillEffect handlers.</summary>
    public sealed class SkillEffectContext
    {
        public DefendCombatWarriorState Warrior;
        public DefendCombatMonsterState TargetMonster;
        public string TargetMonsterRuntimeId;
        public float OutgoingDamage;
        /// <summary>Incoming damage before HP subtraction (OnWarriorWouldDie / OnIncomingDamageSettle).</summary>
        public float IncomingDamage;
        /// <summary>True when this hit targets a different monster than the warrior's last AA target.</summary>
        public bool IsNewTargetFirstHit;
        /// <summary>Set by Pipeline before handler.Apply for the current SoldierSkills entry.</summary>
        public SkillConfigRow CurrentSkillRow;
        /// <summary>True after CheatDeathInvincible intercept applied HP=1 + invincible.</summary>
        public bool WouldDieIntercepted;
        /// <summary>SkillId that triggered on this dispatch (icon popup).</summary>
        public string TriggeredSkillId;
        /// <summary>True when a handler committed Mode2 internal CD this dispatch (LOC roll).</summary>
        public bool CommittedInternalCooldown;
        /// <summary>Trigger hook passed to Pipeline.Dispatch (handler branch selector).</summary>
        public string DispatchTriggerHook;
        /// <summary>When true, Session should invoke SkillPersistChanged for <see cref="SkillPersistSkillId"/>.</summary>
        public bool SkillPersistOn;
        public string SkillPersistSkillId;
        public CombatStatusService CombatStatus;

        /// <summary>AA hit center XZ (hit monster world position).</summary>
        public Vector2 HitCenterXZ;

        /// <summary>True when <see cref="HitCenterXZ"/> was resolved for this dispatch.</summary>
        public bool HasHitCenterXZ;

        /// <summary>Alive monsters with world XZ (Session fills via Stage position provider).</summary>
        public IReadOnlyList<MonsterWorldXZ> AliveMonstersXZ;

        /// <summary>
        /// RuntimeIds already hit by this projectile (includes the current target).
        /// Filled by View; Handler uses count to compute remaining extra hits.
        /// </summary>
        public IReadOnlyCollection<string> AlreadyHitRuntimeIds;

        /// <summary>
        /// Handler-written remaining extra hits after this projectile hit (0 = despawn).
        /// </summary>
        public int ExtraHitsRemaining;

        /// <summary>Warrior XZ when acquiring a new attack target (OnWarriorTargetAcquired).</summary>
        public Vector2 WarriorPositionXZ;

        public bool HasWarriorPositionXZ;

        /// <summary>Warrior BodyRadius used for behind-offset (Skill_12).</summary>
        public float WarriorBodyRadius;

        /// <summary>MassMove ArriveEpsilon for behind-offset (Skill_12).</summary>
        public float ArriveEpsilon;

        /// <summary>
        /// View-injected local NavMesh sample. Args: desired XZ, sample radius.
        /// Returns sampled XZ or null when unwalkable (no AirWall snap).
        /// </summary>
        public Func<Vector2, float, Vector2?> SampleWalkableXZ;

        /// <summary>Handler-written override target for this acquire (farthest enemy).</summary>
        public string OverrideTargetRuntimeId;

        /// <summary>Handler-written walkable landing after SamplePosition.</summary>
        public Vector2 TeleportLandingXZ;

        public bool HasTeleportOverride;
    }
}
