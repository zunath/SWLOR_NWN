using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What perks need from the quick bar: whether a creature knows a feat, taking a feat away and applying the player's targeting
    /// preferences after the feats changed. Character owns the interface and <c>IQuickCastService</c> extends it (supply rule 2 of
    /// MaintainabilityRefactorDomainOrder.md), so the perk services below Abilities need nothing from the quick cast service.
    /// </summary>
    public interface IFeatHotBar
    {
        bool KnowsFeat(uint creature, FeatType feat);

        void RemoveFeat(uint creature, FeatType feat);

        void ApplyPreferences(uint player);
    }
}
