using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What the lower domains ask about a companion: whether a creature is a player's beast or droid, and what a droid's
    /// controller item gives it. Character owns the interface and Companions implements it (supply rule 2 of
    /// MaintainabilityRefactorDomainOrder.md): the implementation takes no service, so the services that ask it are built
    /// without a construction cycle. The reads run per attack and per stat rebuild and allocate nothing beyond what the
    /// droid controller read allocates.
    /// </summary>
    public interface ICompanionLookup
    {
        /// <summary>The resref of a droid controller item.</summary>
        const string DroidControlItemResref = "droid_control";

        /// <summary>Whether a creature is a player's droid.</summary>
        bool IsDroid(uint creature);

        /// <summary>Whether a creature is a beast that belongs to a player.</summary>
        bool IsPlayerBeast(uint creature);

        /// <summary>The type of a spawned beast, <see cref="BeastType.Invalid"/> when the creature is not a beast.</summary>
        BeastType GetBeastType(uint creature);

        /// <summary>The id of the beast entity a spawned beast was made from.</summary>
        string GetBeastId(uint beast);

        /// <summary>The level of a perk a droid's controller item gives it, 0 when it has none.</summary>
        int GetDroidPerkLevel(uint droid, PerkType perkType);

        /// <summary>The rank a droid's controller item gives in a skill, 0 when it has none.</summary>
        int GetDroidSkillRank(uint droid, SkillType skillType);
    }
}
