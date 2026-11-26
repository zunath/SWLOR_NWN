using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The feats perks grant: the lookups built when the module loads and the sync that gives a creature the feats of a perk
    /// level. The feat reads run per ability use and per login and allocate nothing.
    /// </summary>
    public interface IPerkGrantedFeatService
    {
        /// <summary>
        /// When the module has cached its abilities, builds the feat lookups of every perk.
        /// </summary>
        void CachePerkFeatData();

        /// <summary>
        /// When the module loads, forgets the feat lookups of an earlier cache build.
        /// </summary>
        void ResetPerkFeatCache();

        IReadOnlyList<FeatType> GetGrantedFeatsForPerkLevel(PerkType perkType, int perkLevel);

        IReadOnlyList<FeatType> GetCurrentActiveAbilityFeats(PerkType perkType, int perkLevel);

        IReadOnlyList<FeatType> GetAllActiveAbilityFeats(PerkType perkType);

        RecastGroup GetActiveAbilityRecastGroup(PerkType perkType);

        /// <summary>
        /// Restores native feats from purchased perks after loading or migrating a character file.
        /// This does not repeat purchase triggers or change the player's quickbar layout.
        /// </summary>
        void RestorePlayerFeats(uint player);

        void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel);

        bool ShouldEnforceActiveAbilityFeatReplacement(uint creature, PerkType perkType);

        bool IsCurrentActiveAbilityFeat(FeatType feat, PerkType perkType, int perkLevel);
    }
}
