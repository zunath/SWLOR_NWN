namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// One recorded combat event. A flat value type so the main thread can enqueue it without
    /// allocating; strings are only references to names that already exist. The meaning of the
    /// generic Value slots depends on <see cref="Type"/> and is fixed by the factory methods and
    /// <see cref="CombatRecordSerializer"/>.
    /// </summary>
    public struct CombatRecordEvent
    {
        public CombatRecordEventType Type;
        public long TimestampMs;
        public ulong Frame;
        public int ActorId;
        public int TargetId;
        public CombatRecordSourceKind SourceKind;
        public string SourceName;
        public int SourceCode;
        public int Rank;
        public string Text;
        public string Text2;
        public bool HasLocation;
        public float X;
        public float Y;
        public float Z;
        public bool Flag;
        public int Value0;
        public int Value1;
        public int Value2;
        public int Value3;
        public int Value4;
        public int Value5;
        public int Value6;
        public int Value7;
        public CombatRecordDamageBreakdown Damage;

        public const int PermanentDuration = -1;

        public static CombatRecordEvent RecordingStarted(string label, string areaName, string areaResref)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.RecordingStarted,
                Text = label,
                Text2 = areaName,
                SourceName = areaResref
            };
        }

        public static CombatRecordEvent RecordingStopped(string reason, int eventCount)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.RecordingStopped,
                Text = reason,
                Value0 = eventCount
            };
        }

        public static CombatRecordEvent Participant(int participantId, string name, bool isPlayer, string resref)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Participant,
                ActorId = participantId,
                Text = name,
                Flag = isPlayer,
                Text2 = resref
            };
        }

        /// <param name="activationType">The AbilityActivationType value.</param>
        /// <param name="isToggleOff">True when the activation turned a toggled stance off.</param>
        public static CombatRecordEvent AbilityCast(
            int casterId, int targetId, string ability, int feat, int rank,
            int activationType, int activationDelayMs, bool isToggleOff = false)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.AbilityCast,
                Flag = isToggleOff,
                ActorId = casterId,
                TargetId = targetId,
                SourceKind = CombatRecordSourceKind.Ability,
                SourceName = ability,
                SourceCode = feat,
                Rank = rank,
                Value0 = activationDelayMs,
                Value1 = activationType
            };
        }

        public static CombatRecordEvent AbilityQueued(int casterId, int targetId, string ability, int feat, int rank)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.AbilityQueued,
                ActorId = casterId,
                TargetId = targetId,
                SourceKind = CombatRecordSourceKind.Ability,
                SourceName = ability,
                SourceCode = feat,
                Rank = rank
            };
        }

        /// <param name="isRecurring">True for a later pulse of a cast, such as a damage field's tick.</param>
        public static CombatRecordEvent AbilityImpact(
            int casterId, int targetId, string ability, int feat, int rank,
            int targetsHit, int criticalHits, int attributedDamage, bool isRecurring = false)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.AbilityImpact,
                Flag = isRecurring,
                ActorId = casterId,
                TargetId = targetId,
                SourceKind = CombatRecordSourceKind.Ability,
                SourceName = ability,
                SourceCode = feat,
                Rank = rank,
                Value0 = targetsHit,
                Value1 = criticalHits,
                Value2 = attributedDamage
            };
        }

        /// <param name="fpPaid">Net FP the requirements took; negative when a cost rider refunded more.</param>
        /// <param name="staminaPaid">Net STM the requirements took.</param>
        public static CombatRecordEvent AbilityCost(
            int casterId, string ability, int feat, int rank, int fpPaid, int staminaPaid)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.AbilityCost,
                ActorId = casterId,
                SourceKind = CombatRecordSourceKind.AbilityCost,
                SourceName = ability,
                SourceCode = feat,
                Rank = rank,
                Value0 = fpPaid,
                Value1 = staminaPaid
            };
        }

        public static CombatRecordEvent Attack(
            int attackerId, int defenderId, CombatRecordAttackResult result, int hitChance, string ability)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Attack,
                ActorId = attackerId,
                TargetId = defenderId,
                SourceKind = ability == null ? CombatRecordSourceKind.Autoattack : CombatRecordSourceKind.Ability,
                SourceName = ability,
                Value0 = (int)result,
                Value1 = hitChance
            };
        }

        public static CombatRecordEvent DamageApplied(
            int sourceId, int targetId, CombatRecordSourceKind kind, string sourceName, int sourceCode,
            CombatRecordDamageBreakdown damage,
            int hitPointsBefore, int hitPointsAfter,
            int temporaryHitPointsBefore, int temporaryHitPointsAfter, int temporaryHitPointsAbsorbed, int maxHitPoints,
            bool isEngineApplied = false)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Damage,
                Flag = isEngineApplied,
                ActorId = sourceId,
                TargetId = targetId,
                SourceKind = kind,
                SourceName = sourceName,
                SourceCode = sourceCode,
                Damage = damage,
                Value0 = damage.Total,
                Value1 = hitPointsBefore,
                Value2 = hitPointsAfter,
                Value3 = temporaryHitPointsBefore,
                Value4 = temporaryHitPointsAfter,
                Value5 = maxHitPoints,
                Value6 = temporaryHitPointsAbsorbed
            };
        }

        /// <param name="grantedCode">The granting source's code, such as the StatType of a stat-triggered shield.</param>
        public static CombatRecordEvent TemporaryHitPointsAbsorbed(
            int attackerId, int ownerId, string effectKey, int amount,
            int grantedById, CombatRecordSourceKind grantedKind, string grantedName, int grantedCode = 0)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.TemporaryHitPointsAbsorbed,
                ActorId = attackerId,
                TargetId = ownerId,
                Text = effectKey,
                Value0 = amount,
                Value1 = grantedById,
                SourceKind = grantedKind,
                SourceName = grantedName,
                SourceCode = grantedCode
            };
        }

        public static CombatRecordEvent Healing(
            int sourceId, int targetId, CombatRecordSourceKind kind, string sourceName, int sourceCode,
            int requested, int hitPointsBefore, int hitPointsAfter, int maxHitPoints)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Healing,
                ActorId = sourceId,
                TargetId = targetId,
                SourceKind = kind,
                SourceName = sourceName,
                SourceCode = sourceCode,
                Value0 = requested,
                Value1 = Math.Max(0, hitPointsAfter - hitPointsBefore),
                Value2 = hitPointsBefore,
                Value3 = hitPointsAfter,
                Value4 = maxHitPoints
            };
        }

        public static CombatRecordEvent TemporaryHitPointsGranted(
            int sourceId, int targetId, CombatRecordSourceKind kind, string sourceName, int sourceCode,
            string effectKey, int amount, int durationMs)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.TemporaryHitPointsGranted,
                ActorId = sourceId,
                TargetId = targetId,
                SourceKind = kind,
                SourceName = sourceName,
                SourceCode = sourceCode,
                Text = effectKey,
                Value0 = amount,
                Value1 = durationMs
            };
        }

        /// <param name="valueAfter">The pool after the change, or -1 when it is not tracked (items).</param>
        public static CombatRecordEvent ResourceChanged(
            int creatureId, CombatRecordResource resource, int delta, int valueAfter,
            CombatRecordSourceKind reason, string reasonName, int reasonCode, string itemResref = null)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Resource,
                ActorId = creatureId,
                Value0 = (int)resource,
                Value1 = delta,
                Value2 = valueAfter,
                SourceKind = reason,
                SourceName = reasonName,
                SourceCode = reasonCode,
                Text = itemResref
            };
        }

        /// <param name="durationMs">Duration in milliseconds, or <see cref="PermanentDuration"/>.</param>
        /// <param name="effectType">The status effect's class name.</param>
        public static CombatRecordEvent StatusApplied(
            int sourceId, int targetId, string effectName, string effectType, string originatingAbility, int durationMs, bool isRefresh)
        {
            return new CombatRecordEvent
            {
                Type = isRefresh ? CombatRecordEventType.StatusRefreshed : CombatRecordEventType.StatusApplied,
                ActorId = sourceId,
                TargetId = targetId,
                Text = effectName,
                SourceName = effectType,
                Text2 = originatingAbility,
                Value0 = durationMs
            };
        }

        public static CombatRecordEvent StatusRemoved(
            int sourceId, int targetId, string effectName, string effectType, CombatRecordStatusRemoval reason)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.StatusRemoved,
                ActorId = sourceId,
                TargetId = targetId,
                Text = effectName,
                SourceName = effectType,
                Value0 = (int)reason
            };
        }

        public static CombatRecordEvent Death(int deadId, int killerId, int lastDamageSourceId)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Death,
                TargetId = deadId,
                ActorId = killerId,
                Value0 = lastDamageSourceId
            };
        }

        public static CombatRecordEvent Resurrection(
            int targetId, int casterId, CombatRecordSourceKind kind, string sourceName, int sourceCode)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Resurrection,
                TargetId = targetId,
                ActorId = casterId,
                SourceKind = kind,
                SourceName = sourceName,
                SourceCode = sourceCode
            };
        }

        public static CombatRecordEvent Subdual(int subduedId, int subduedById)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Subdual,
                TargetId = subduedId,
                ActorId = subduedById
            };
        }

        public static CombatRecordEvent Snapshot(
            int creatureId, int hitPoints, int maxHitPoints, int temporaryHitPoints,
            int fp, int maxFP, int stamina, int maxStamina, bool inCombat, bool isDead)
        {
            return new CombatRecordEvent
            {
                Type = CombatRecordEventType.Snapshot,
                ActorId = creatureId,
                Value0 = hitPoints,
                Value1 = maxHitPoints,
                Value2 = temporaryHitPoints,
                Value3 = fp,
                Value4 = maxFP,
                Value5 = stamina,
                Value6 = maxStamina,
                Flag = inCombat,
                Value7 = isDead ? 1 : 0
            };
        }

        /// <summary>
        /// Damage events: HP the hit actually removed. HP below zero is overkill, not damage taken.
        /// </summary>
        public int GetHitPointsLost()
        {
            return Math.Max(0, Math.Max(0, Value1) - Math.Max(0, Value2));
        }

        /// <summary>
        /// Damage events: how far this hit drove HP below zero.
        /// </summary>
        public int GetOverkill()
        {
            return Math.Max(0, Math.Max(0, -Value2) - Math.Max(0, -Value1));
        }

        /// <summary>
        /// Damage events: temporary HP the hit took from the pools that existed before it.
        /// </summary>
        public int GetTemporaryHitPointsAbsorbed()
        {
            return Math.Max(0, Value6);
        }

        public CombatRecordEvent WithLocation(float x, float y, float z)
        {
            HasLocation = true;
            X = x;
            Y = y;
            Z = z;
            return this;
        }
    }
}
