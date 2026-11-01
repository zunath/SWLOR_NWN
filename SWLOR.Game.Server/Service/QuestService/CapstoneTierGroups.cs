using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// The enemy groups that share a tier in each capstone dungeon. Every line in a dungeon hunts the same adepts,
    /// specialists and inner circle, whichever line the blueprint was first built for.
    /// </summary>
    public static class CapstoneTierGroups
    {
        private static readonly NPCGroupType[][] _tiers =
        {
            // Anchorhead Canyon Range
            new[] { NPCGroupType.Tatooine_LastWord_Adept, NPCGroupType.Tatooine_DeadMansHand_Adept, NPCGroupType.Tatooine_UnmovingCenter_Adept },
            new[] { NPCGroupType.Tatooine_LastWord_Specialist, NPCGroupType.Tatooine_DeadMansHand_Specialist, NPCGroupType.Tatooine_UnmovingCenter_Specialist },
            new[] { NPCGroupType.Tatooine_LastWord_InnerCircle, NPCGroupType.Tatooine_DeadMansHand_InnerCircle, NPCGroupType.Tatooine_UnmovingCenter_InnerCircle },
            // CZ-220 Breaker Yard
            new[] { NPCGroupType.CZ220_AdamantineGuard_Adept, NPCGroupType.CZ220_ScrapheapLockdown_Adept, NPCGroupType.CZ220_Worldbreaker_Adept },
            new[] { NPCGroupType.CZ220_AdamantineGuard_Specialist, NPCGroupType.CZ220_ScrapheapLockdown_Specialist, NPCGroupType.CZ220_Worldbreaker_Specialist },
            new[] { NPCGroupType.CZ220_AdamantineGuard_InnerCircle, NPCGroupType.CZ220_ScrapheapLockdown_InnerCircle, NPCGroupType.CZ220_Worldbreaker_InnerCircle },
            // Czerka Arms Test Range
            new[] { NPCGroupType.SmugglersMoon_KillBox_Adept, NPCGroupType.SmugglersMoon_OneShot_Adept, NPCGroupType.SmugglersMoon_RainOfSteel_Adept },
            new[] { NPCGroupType.SmugglersMoon_KillBox_Specialist, NPCGroupType.SmugglersMoon_OneShot_Specialist, NPCGroupType.SmugglersMoon_RainOfSteel_Specialist },
            new[] { NPCGroupType.SmugglersMoon_KillBox_InnerCircle, NPCGroupType.SmugglersMoon_OneShot_InnerCircle, NPCGroupType.SmugglersMoon_RainOfSteel_InnerCircle },
            // Dathomir Grotto Apex Den
            new[] { NPCGroupType.Dathomir_PrimalOverrun_Adept, NPCGroupType.Dathomir_UntouchableInstinct_Adept, NPCGroupType.Dathomir_ForceBondedBeast_Adept },
            new[] { NPCGroupType.Dathomir_PrimalOverrun_Specialist, NPCGroupType.Dathomir_UntouchableInstinct_Specialist, NPCGroupType.Dathomir_ForceBondedBeast_Specialist },
            new[] { NPCGroupType.Dathomir_PrimalOverrun_InnerCircle, NPCGroupType.Dathomir_UntouchableInstinct_InnerCircle, NPCGroupType.Dathomir_ForceBondedBeast_InnerCircle },
            // Dathomir Tarn Jungle Preserve
            new[] { NPCGroupType.Dathomir_ApexBite_Adept, NPCGroupType.Dathomir_UnbreakableBeast_Adept, NPCGroupType.Dathomir_AlphaRhythm_Adept },
            new[] { NPCGroupType.Dathomir_ApexBite_Specialist, NPCGroupType.Dathomir_UnbreakableBeast_Specialist, NPCGroupType.Dathomir_AlphaRhythm_Specialist },
            new[] { NPCGroupType.Dathomir_ApexBite_InnerCircle, NPCGroupType.Dathomir_UnbreakableBeast_InnerCircle, NPCGroupType.Dathomir_AlphaRhythm_InnerCircle },
            // Hutlar Jedi Trial Halls
            new[] { NPCGroupType.Hutlar_StormDrillDroid, NPCGroupType.Hutlar_GuardianPatrolDroid, NPCGroupType.Hutlar_CycloneDrillDroid },
            new[] { NPCGroupType.Hutlar_StormDuelistDroid, NPCGroupType.Hutlar_GuardianBulwarkDroid, NPCGroupType.Hutlar_CycloneDuelistDroid },
            new[] { NPCGroupType.Hutlar_StormExaminerDroid, NPCGroupType.Hutlar_GuardianShieldDroid, NPCGroupType.Hutlar_CycloneExaminerDroid },
            // Hutlar Qion Test Site
            new[] { NPCGroupType.Hutlar_ThermalDetonator_Adept, NPCGroupType.Hutlar_OverloadBarrage_Adept, NPCGroupType.Hutlar_PerfectFlurry_Adept },
            new[] { NPCGroupType.Hutlar_ThermalDetonator_Specialist, NPCGroupType.Hutlar_OverloadBarrage_Specialist, NPCGroupType.Hutlar_PerfectFlurry_Specialist },
            new[] { NPCGroupType.Hutlar_ThermalDetonator_InnerCircle, NPCGroupType.Hutlar_OverloadBarrage_InnerCircle, NPCGroupType.Hutlar_PerfectFlurry_InnerCircle },
            // Mon Cala Medical Sublevel
            new[] { NPCGroupType.MonCala_EmergencyCocktail_Adept, NPCGroupType.MonCala_HoldTheLine_Adept, NPCGroupType.MonCala_InfiniteConduit_Adept },
            new[] { NPCGroupType.MonCala_EmergencyCocktail_Specialist, NPCGroupType.MonCala_HoldTheLine_Specialist, NPCGroupType.MonCala_InfiniteConduit_Specialist },
            new[] { NPCGroupType.MonCala_EmergencyCocktail_InnerCircle, NPCGroupType.MonCala_HoldTheLine_InnerCircle, NPCGroupType.MonCala_InfiniteConduit_InnerCircle },
            // Mon Cala Sith Crypt Depths
            new[] { NPCGroupType.MonCala_CryptRaider, NPCGroupType.MonCala_VitaeReaverInitiate, NPCGroupType.MonCala_UmbralRaiderScout },
            new[] { NPCGroupType.MonCala_CryptMarauder, NPCGroupType.MonCala_VitaeReaverAdept, NPCGroupType.MonCala_UmbralRaiderAdept },
            new[] { NPCGroupType.MonCala_CryptReaver, NPCGroupType.MonCala_VitaeReaverBinder, NPCGroupType.MonCala_UmbralRaiderSaboteur },
            // Smuggler's Moon Fight Club Backrooms
            new[] { NPCGroupType.SmugglersMoon_CripplingDefense_Adept, NPCGroupType.SmugglersMoon_TempestBloom_Adept, NPCGroupType.SmugglersMoon_RedBloom_Adept },
            new[] { NPCGroupType.SmugglersMoon_CripplingDefense_Specialist, NPCGroupType.SmugglersMoon_TempestBloom_Specialist, NPCGroupType.SmugglersMoon_RedBloom_Specialist },
            new[] { NPCGroupType.SmugglersMoon_CripplingDefense_InnerCircle, NPCGroupType.SmugglersMoon_TempestBloom_InnerCircle, NPCGroupType.SmugglersMoon_RedBloom_InnerCircle },
            // Tatooine Forge Caverns
            new[] { NPCGroupType.Tatooine_AbsoluteDefense_Adept, NPCGroupType.Tatooine_SoulAscension_Adept, NPCGroupType.Tatooine_Forcebane_Adept },
            new[] { NPCGroupType.Tatooine_AbsoluteDefense_Specialist, NPCGroupType.Tatooine_SoulAscension_Specialist, NPCGroupType.Tatooine_Forcebane_Specialist },
            new[] { NPCGroupType.Tatooine_AbsoluteDefense_InnerCircle, NPCGroupType.Tatooine_SoulAscension_InnerCircle, NPCGroupType.Tatooine_Forcebane_InnerCircle },
            // Veles Militia Annex
            new[] { NPCGroupType.Viscara_Invincible_Adept, NPCGroupType.Viscara_VitalRupture_Adept, NPCGroupType.Viscara_SystemicShutdown_Adept },
            new[] { NPCGroupType.Viscara_Invincible_Specialist, NPCGroupType.Viscara_VitalRupture_Specialist, NPCGroupType.Viscara_SystemicShutdown_Specialist },
            new[] { NPCGroupType.Viscara_Invincible_InnerCircle, NPCGroupType.Viscara_VitalRupture_InnerCircle, NPCGroupType.Viscara_SystemicShutdown_InnerCircle },
            // Viscara Republic Engineering Bunker
            new[] { NPCGroupType.Viscara_KillzoneBeacon_Adept, NPCGroupType.Viscara_EmergencyBunker_Adept, NPCGroupType.Viscara_DecisiveCommand_Adept },
            new[] { NPCGroupType.Viscara_KillzoneBeacon_Specialist, NPCGroupType.Viscara_EmergencyBunker_Specialist, NPCGroupType.Viscara_DecisiveCommand_Specialist },
            new[] { NPCGroupType.Viscara_KillzoneBeacon_InnerCircle, NPCGroupType.Viscara_EmergencyBunker_InnerCircle, NPCGroupType.Viscara_DecisiveCommand_InnerCircle },
        };

        private static readonly Dictionary<NPCGroupType, NPCGroupType[]> _byGroup =
            _tiers.SelectMany(tier => tier.Select(group => (group, tier))).ToDictionary(entry => entry.group, entry => entry.tier);

        /// <summary>The groups that share this group's dungeon tier, including the group itself.</summary>
        public static IReadOnlyCollection<NPCGroupType> GetTier(NPCGroupType group)
        {
            if (!_byGroup.TryGetValue(group, out var tier))
                throw new KeyNotFoundException($"NPC group '{group}' is not a capstone dungeon tier.");

            return tier;
        }

        public static IReadOnlyCollection<NPCGroupType[]> GetTiers() => _tiers;
    }
}
