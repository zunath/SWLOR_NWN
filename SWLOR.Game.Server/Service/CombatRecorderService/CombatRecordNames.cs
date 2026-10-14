using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Stable JSON names for recorder enums. File consumers depend on these strings, so they are
    /// spelled out instead of derived from enum member names.
    /// </summary>
    public static class CombatRecordNames
    {
        public static string GetEventTypeName(CombatRecordEventType type)
        {
            return type switch
            {
                CombatRecordEventType.RecordingStarted => "recording_started",
                CombatRecordEventType.RecordingStopped => "recording_stopped",
                CombatRecordEventType.Participant => "participant",
                CombatRecordEventType.AbilityCast => "ability_cast",
                CombatRecordEventType.AbilityQueued => "ability_queued",
                CombatRecordEventType.AbilityImpact => "ability_impact",
                CombatRecordEventType.AbilityCost => "ability_cost",
                CombatRecordEventType.Attack => "attack",
                CombatRecordEventType.Damage => "damage",
                CombatRecordEventType.TemporaryHitPointsAbsorbed => "temp_hp_absorb",
                CombatRecordEventType.Healing => "heal",
                CombatRecordEventType.TemporaryHitPointsGranted => "temp_hp_grant",
                CombatRecordEventType.Resource => "resource",
                CombatRecordEventType.StatusApplied => "status_apply",
                CombatRecordEventType.StatusRefreshed => "status_refresh",
                CombatRecordEventType.StatusRemoved => "status_remove",
                CombatRecordEventType.Death => "death",
                CombatRecordEventType.Resurrection => "resurrection",
                CombatRecordEventType.Subdual => "subdual",
                CombatRecordEventType.Snapshot => "snapshot",
                CombatRecordEventType.AbilityDenied => "ability_denied",
                CombatRecordEventType.AbilityInterrupted => "ability_interrupted",
                _ => "unknown"
            };
        }

        public static string GetInterruptCauseName(CombatRecordInterruptCause cause)
        {
            return cause switch
            {
                CombatRecordInterruptCause.Moved => "moved",
                CombatRecordInterruptCause.CrowdControl => "crowd_control",
                CombatRecordInterruptCause.Died => "died",
                _ => "other"
            };
        }

        public static string GetSourceKindName(CombatRecordSourceKind kind)
        {
            return kind switch
            {
                CombatRecordSourceKind.Ability => "ability",
                CombatRecordSourceKind.AbilityCost => "ability_cost",
                CombatRecordSourceKind.Autoattack => "autoattack",
                CombatRecordSourceKind.StatusEffect => "status_effect",
                CombatRecordSourceKind.Lifesteal => "lifesteal",
                CombatRecordSourceKind.OnKill => "on_kill",
                CombatRecordSourceKind.Regeneration => "regeneration",
                CombatRecordSourceKind.Rest => "rest",
                CombatRecordSourceKind.Item => "item",
                CombatRecordSourceKind.StatTrigger => "stat_trigger",
                CombatRecordSourceKind.DamageShare => "damage_share",
                CombatRecordSourceKind.Respawn => "respawn",
                CombatRecordSourceKind.Subdual => "subdual",
                CombatRecordSourceKind.DungeonMaster => "dm",
                CombatRecordSourceKind.Trap => "trap",
                CombatRecordSourceKind.Setup => "setup",
                _ => "unattributed"
            };
        }

        /// <summary>
        /// The specific source name: an ability, status effect or item name, the StatType that
        /// drove a stat trigger, or the weapon skill of an autoattack.
        /// </summary>
        public static string GetSourceName(CombatRecordSourceKind kind, string name, int code)
        {
            if (!string.IsNullOrEmpty(name) || code <= 0)
                return name;

            return kind switch
            {
                CombatRecordSourceKind.StatTrigger => ((StatType)code).ToString(),
                CombatRecordSourceKind.Autoattack => ((SkillType)code).ToString(),
                _ => name
            };
        }

        public static string GetResourceName(CombatRecordResource resource)
        {
            return resource switch
            {
                CombatRecordResource.FP => "fp",
                CombatRecordResource.Stamina => "stm",
                CombatRecordResource.Item => "item",
                _ => "unknown"
            };
        }

        public static string GetAttackResultName(CombatRecordAttackResult result)
        {
            return result switch
            {
                CombatRecordAttackResult.Hit => "hit",
                CombatRecordAttackResult.Critical => "critical",
                CombatRecordAttackResult.Miss => "miss",
                CombatRecordAttackResult.Deflect => "deflect",
                _ => "unknown"
            };
        }

        public static string GetStatusRemovalName(CombatRecordStatusRemoval removal)
        {
            return removal switch
            {
                CombatRecordStatusRemoval.Expired => "expired",
                CombatRecordStatusRemoval.Replaced => "replaced",
                _ => "removed"
            };
        }

        public static string GetActivationTypeName(int activationType)
        {
            return (AbilityActivationType)activationType switch
            {
                AbilityActivationType.Casted => "casted",
                AbilityActivationType.Weapon => "weapon",
                AbilityActivationType.Stance => "stance",
                _ => "invalid"
            };
        }
    }
}
