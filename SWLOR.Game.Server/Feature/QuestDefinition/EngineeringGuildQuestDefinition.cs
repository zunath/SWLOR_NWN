using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class EngineeringGuildQuestDefinition : IQuestListDefinition
    {
        private class RewardDetails
        {
            public int Gold { get; }
            public int GP { get; }

            public RewardDetails(int gold, int gp)
            {
                Gold = gold;
                GP = gp;
            }
        }

        private readonly Dictionary<int, RewardDetails> _rewardDetails = new()
        {
            { 0, new RewardDetails(1000, 21)},
            { 1, new RewardDetails(2500, 81)},
            { 2, new RewardDetails(4000, 117)},
            { 3, new RewardDetails(5500, 156)},
            { 4, new RewardDetails(7200, 195)},
            { 5, new RewardDetails(8200, 246)},
        };

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();

            // Tier 1 (Rank 0)
            BuildItemTask(builder, "eng_tsk_001", "sm_capb_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_002", "sm_laser_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_003", "sm_ampem_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_004", "sm_ampth_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_005", "sm_ampex_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_006", "sm_thrust_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_007", "sm_plate_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_008", "sm_hrep_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_009", "sm_ion_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_010", "sm_mine_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_011", "sm_missl_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_012", "sm_shbst_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_013", "sm_shrep_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_014", "sm_tcomp_00", 1, 0);
            BuildItemTask(builder, "eng_tsk_015", "sd_striker", 1, 0);
            BuildItemTask(builder, "eng_tsk_016", "sd_condor", 1, 0);

            // Tier 2 (Rank 1)
            BuildItemTask(builder, "eng_tsk_200", "sm_capb_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_201", "sm_laser_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_202", "sm_ampem_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_203", "sm_ampth_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_204", "sm_ampex_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_205", "sm_thrust_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_206", "sm_plate_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_207", "sm_hrep_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_208", "sm_ion_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_209", "sm_mine_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_210", "sm_missl_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_211", "sm_shbst_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_212", "sm_shrep_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_213", "sm_tcomp_10", 1, 1);
            BuildItemTask(builder, "eng_tsk_214", "sd_republicstrik", 1, 1);
            BuildItemTask(builder, "eng_tsk_215", "sd_sithscout", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "eng_tsk_400", "sm_capb_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_401", "sm_laser_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_402", "sm_ampem_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_403", "sm_ampth_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_404", "sm_ampex_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_405", "sm_thrust_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_406", "sm_plate_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_407", "sm_hrep_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_408", "sm_ion_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_409", "sm_mine_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_410", "sm_missl_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_411", "sm_shbst_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_412", "sm_shrep_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_413", "sm_tcomp_20", 1, 2);
            BuildItemTask(builder, "eng_tsk_414", "sd_republicscout", 1, 2);
            BuildItemTask(builder, "eng_tsk_415", "sd_sithscout", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "eng_tsk_600", "sm_capb_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_601", "sm_laser_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_602", "sm_ampem_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_603", "sm_ampth_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_604", "sm_ampex_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_605", "sm_thrust_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_606", "sm_plate_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_607", "sm_hrep_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_608", "sm_ion_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_609", "sm_mine_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_610", "sm_missl_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_611", "sm_shbst_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_612", "sm_shrep_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_613", "sm_tcomp_30", 1, 3);
            BuildItemTask(builder, "eng_tsk_614", "sd_republicgunsh", 1, 3);
            BuildItemTask(builder, "eng_tsk_615", "sd_sithgunship", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "eng_tsk_800", "sm_capb_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_801", "sm_laser_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_802", "sm_ampem_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_803", "sm_ampth_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_804", "sm_ampex_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_805", "sm_thrust_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_806", "sm_plate_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_807", "sm_hrep_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_808", "sm_ion_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_809", "sm_mine_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_810", "sm_missl_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_811", "sm_shbst_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_812", "sm_shrep_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_813", "sm_tcomp_40", 1, 4);
            BuildItemTask(builder, "eng_tsk_814", "sd_sithinfiltrat", 1, 4);
            BuildItemTask(builder, "eng_tsk_815", "sd_republicgunsh", 1, 4);

            return builder.Build();
        }

        private void BuildItemTask(
            QuestBuilder builder,
            string questId,
            string resref,
            int amount,
            int guildRank)
        {
            var itemName = Cache.GetItemNameByResref(resref);
            var rewardDetails = _rewardDetails[guildRank];

            builder.Create(questId, $"Craft {amount}x {itemName}")
                .IsRepeatable()
                .IsGuildTask(GuildType.EngineeringGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Craft {amount}x {itemName} and return to the Engineering Guildmaster")
                .AddCollectItemObjective(resref, amount, CollectItemProducerRequirementType.PlayerProduced)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.EngineeringGuild, rewardDetails.GP)
                .AddGuildTaskXpReward(GuildType.EngineeringGuild, resref, amount, true);
        }
    }
}
