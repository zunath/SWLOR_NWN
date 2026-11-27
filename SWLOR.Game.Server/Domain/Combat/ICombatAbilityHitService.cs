using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability hit roll for combat: an ability's hit chance and roll, and the test overrides of the ability and
    /// auto-attack hit rolls.
    /// </summary>
    public interface ICombatAbilityHitService
    {
        /// <summary>
        /// Forces every TryResolveAbilityHit call to the given outcome instead of rolling.
        /// Intended solely for the in-engine test harness: ability behavior assertions cannot
        /// be made against a hit roll that legitimately misses up to 5% of the time even at
        /// capped hit rates. Pass null to restore normal resolution. Always restore in a
        /// finally block.
        /// </summary>
        void SetAbilityHitResolutionOverride(bool? forcedOutcome);

        /// <summary>
        /// Forces every native auto-attack roll (ResolveAttackRoll hook) to the given outcome
        /// instead of rolling. Intended solely for the in-engine test harness: ability damage
        /// assertions cannot distinguish ability damage from the activator's resumed
        /// auto-attacks, so behavior sweeps force auto-attack misses. Pass null to restore
        /// normal resolution. Always restore in a finally block.
        /// </summary>
        void SetAutoAttackHitResolutionOverride(bool? forcedOutcome);

        /// <summary>
        /// The current auto-attack resolution override, if any. Read by the native
        /// ResolveAttackRoll hook.
        /// </summary>
        bool? GetAutoAttackHitResolutionOverride();

        bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid,
            int accuracyFlatAdjustment = 0);
    }
}
