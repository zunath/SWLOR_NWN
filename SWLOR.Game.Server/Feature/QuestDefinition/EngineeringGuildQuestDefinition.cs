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
            { 0, new RewardDetails(23, 7)},
            { 1, new RewardDetails(84, 27)},
            { 2, new RewardDetails(122, 39)},
            { 3, new RewardDetails(184, 52)},
            { 4, new RewardDetails(245, 65)},
            { 5, new RewardDetails(312, 82)},
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
            BuildItemTask(builder, "eng_tsk_017", "cap_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_018", "com_laser_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_019", "em_amp_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_020", "therm_amp_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_021", "exp_amp_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_022", "eva_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_023", "hull_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_024", "hull_rep_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_025", "ion_cann_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_026", "min_laser_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_027", "msl_launch_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_028", "shld_boost_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_029", "shld_rep_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_030", "tgt_sys_1", 1, 1);
            BuildItemTask(builder, "eng_tsk_031", "sdeed_hound", 1, 1);
            BuildItemTask(builder, "eng_tsk_032", "sdeed_panther", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "eng_tsk_033", "cap_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_034", "com_laser_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_035", "em_amp_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_036", "therm_amp_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_037", "exp_amp_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_038", "eva_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_039", "hull_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_040", "hull_rep_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_041", "ion_cann_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_042", "min_laser_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_043", "msl_launch_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_044", "shld_boost_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_045", "shld_rep_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_046", "tgt_sys_2", 1, 2);
            BuildItemTask(builder, "eng_tsk_047", "sdeed_saber", 1, 2);
            BuildItemTask(builder, "eng_tsk_048", "sdeed_falchion", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "eng_tsk_049", "cap_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_050", "com_laser_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_051", "em_amp_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_052", "therm_amp_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_053", "exp_amp_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_054", "eva_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_055", "hull_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_056", "hull_rep_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_057", "ion_cann_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_058", "min_laser_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_059", "msl_launch_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_060", "shld_boost_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_061", "shld_rep_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_062", "tgt_sys_3", 1, 3);
            BuildItemTask(builder, "eng_tsk_063", "sdeed_mule", 1, 3);
            BuildItemTask(builder, "eng_tsk_064", "sdeed_merchant", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "eng_tsk_065", "cap_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_066", "com_laser_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_067", "em_amp_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_068", "therm_amp_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_069", "exp_amp_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_070", "eva_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_071", "hull_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_072", "hull_rep_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_073", "ion_cann_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_074", "min_laser_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_075", "msl_launch_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_076", "shld_boost_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_077", "shld_rep_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_078", "tgt_sys_4", 1, 4);
            BuildItemTask(builder, "eng_tsk_079", "sdeed_throne", 1, 4);
            BuildItemTask(builder, "eng_tsk_080", "sdeed_consular", 1, 4);

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