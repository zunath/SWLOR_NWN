using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What the status effects ask of the resistances: whether a resistance type is real, whether a creature is immune to it and how
    /// many ticks of an effect it leaves (part of the status effect split, Readmes/MaintainabilityRefactorStatusEffectSplit.md).
    /// Character owns the interface and <c>IResistanceService</c> in Combat extends it (supply rule 2 of
    /// MaintainabilityRefactorDomainOrder.md), so the status effects read it without reaching up to Combat. The provider needs only
    /// the database, Mimicry, the stat reader and the status effect trackers, so no path leads back to the parts that apply effects.
    /// </summary>
    public interface IStatusResistance
    {
        bool IsValidResistanceType(ResistanceType type);

        bool HasImmunity(uint creature, ResistanceType type);

        int CalculateResistedTicks(uint creature, ResistanceType type, int baseTicks);
    }
}
