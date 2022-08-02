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
            { 0, new RewardDetails(138, 21)},
            { 1, new RewardDetails(343, 81)},
            { 2, new RewardDetails(532, 117)},
            { 3, new RewardDetails(733, 156)},
            { 4, new RewardDetails(874, 195)},
            { 5, new RewardDetails(960, 246)},
        };

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();

            // Tier 1 (Rank 0)
            BuildItemTask(builder, "eng_tsk_001", "cap_boost_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_002", "com_laser_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_003", "em_amp_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_004", "therm_amp_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_005", "exp_amp_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_006", "eva_boost_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_007", "hull_boost_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_008", "hull_rep_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_009", "ion_cann_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_010", "min_laser_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_011", "msl_launch_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_012", "shld_boost_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_013", "shld_rep_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_014", "tgt_sys_b", 1, 0);
            BuildItemTask(builder, "eng_tsk_015", "sdeed_striker", 1, 0);
            BuildItemTask(builder, "eng_tsk_016", "sdeed_condor", 1, 0);

            // Tier 2 (Rank 1)
            BuildItemTask(builder, "eng_tsk_200", "cap_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_201", "com_laser_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_202", "em_amp_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_203", "therm_amp_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_204", "exp_amp_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_205", "eva_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_206", "hull_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_207", "hull_rep_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_208", "ion_cann_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_209", "min_laser_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_210", "msl_launch_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_211", "shld_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_212", "shld_rep_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_213", "tgt_sys_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_214", "sdeed_hound", 1, 1);
            BuildItemTask(builder, "eng_tsk_215", "sdeed_panther", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "eng_tsk_400", "cap_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_401", "com_laser_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_402", "em_amp_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_403", "therm_amp_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_404", "exp_amp_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_405", "eva_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_406", "hull_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_407", "hull_rep_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_408", "ion_cann_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_409", "min_laser_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_410", "msl_launch_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_411", "shld_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_412", "shld_rep_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_413", "tgt_sys_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_414", "sdeed_saber", 1, 2);
            BuildItemTask(builder, "eng_tsk_415", "sdeed_falchion", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "eng_tsk_600", "cap_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_601", "com_laser_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_602", "em_amp_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_603", "therm_amp_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_604", "exp_amp_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_605", "eva_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_606", "hull_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_607", "hull_rep_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_608", "ion_cann_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_609", "min_laser_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_610", "msl_launch_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_611", "shld_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_612", "shld_rep_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_613", "tgt_sys_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_614", "sdeed_mule", 1, 3);
            BuildItemTask(builder, "eng_tsk_615", "sdeed_merchant", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "eng_tsk_800", "cap_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_801", "com_laser_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_802", "em_amp_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_803", "therm_amp_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_804", "exp_amp_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_805", "eva_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_806", "hull_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_807", "hull_rep_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_808", "ion_cann_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_809", "min_laser_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_810", "msl_launch_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_811", "shld_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_812", "shld_rep_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_813", "tgt_sys_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_814", "sdeed_throne", 1, 4);
            BuildItemTask(builder, "eng_tsk_815", "sdeed_consular", 1, 4);

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

            builder.Create(questId, $"{amount}x {itemName}")
                .IsRepeatable()
                .IsGuildTask(GuildType.EngineeringGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Engineering Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.EngineeringGuild, rewardDetails.GP);
        }
    }
}