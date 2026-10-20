using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// Blueprint resrefs of the training sabers and saberstaffs. They are Plot items, and the
    /// appearance editor lets players restyle them. Other plot sabers (for example the
    /// _l1/_l2/_w1 plot blueprints) share the same default training look, so the look alone
    /// cannot identify a training weapon; the blueprint resref is checked instead.
    /// </summary>
    public static class TrainingSaberCatalog
    {
        private static readonly HashSet<string> Lightsabers = new(StringComparer.OrdinalIgnoreCase)
        {
            "saber_train_1",
            "saber_train_2",
            "saber_train_3",
            "saber_train_4",
            "saber_train_5",
            "fld_trnsaber",
            "asc_trnsaber",
            "prm_trnsaber",
            "vet_trnsaber"
        };

        private static readonly HashSet<string> Saberstaffs = new(StringComparer.OrdinalIgnoreCase)
        {
            "trn_saberstaff_1",
            "trn_saberstaff_2",
            "trn_saberstaff_3",
            "trn_saberstaff_4",
            "trn_saberstaff_5",
            "fld_trnsabstaff",
            "asc_trnsabstaff",
            "prm_trnsabstaff",
            "vet_trnsabstaff"
        };

        public static IReadOnlyCollection<string> LightsaberResrefs => Lightsabers;

        public static IReadOnlyCollection<string> SaberstaffResrefs => Saberstaffs;

        /// <summary>
        /// Returns true when the resref is a training blueprint of the matching saber base type.
        /// </summary>
        public static bool IsTrainingSaber(BaseItem itemType, string resref)
        {
            if (string.IsNullOrEmpty(resref))
                return false;

            return itemType switch
            {
                BaseItem.Lightsaber => Lightsabers.Contains(resref),
                BaseItem.Saberstaff => Saberstaffs.Contains(resref),
                _ => false
            };
        }
    }
}
