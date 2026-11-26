using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What a perk refund takes away: the status effects a perk's abilities left on a creature, and the refund of perk levels when
    /// a skill decays below what the perk needs.
    /// </summary>
    public interface IPerkRefundService
    {
        /// <summary>
        /// Removes declared self effects and effects granted by the refunded perk's abilities.
        /// Source-owned effects retain their ability identity even when another perk uses the same status class.
        /// </summary>
        void RemoveStatusEffectsOnPerkRefund(uint creature, PerkType perkType);

        /// <summary>
        /// Selects distinct ability/status pairs for a perk refund. Deduplication retains separate
        /// abilities that share a status class so each cleanup uses its own originating ability.
        /// </summary>
        IReadOnlyList<(AbilityDetail Ability, Type StatusEffectType)> GetSourceOwnedStatusEffectRefunds(
            IEnumerable<AbilityDetail> abilities, PerkType perkType);

        /// <summary>
        /// When a skill receives decay, any perks tied to that skill should be checked.
        /// If the player no longer meets the requirements for those perks, they should be reduced in level.
        /// </summary>
        void RemovePerkLevelOnSkillDecay();
    }
}
