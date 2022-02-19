using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class SmitheryGuildQuestDefinition : IQuestListDefinition
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
			BuildItemTask(builder, "smth_tsk_001", "b_greatsword", 1, 0);
			BuildItemTask(builder, "smth_tsk_002", "b_spear", 1, 0);
			BuildItemTask(builder, "smth_tsk_003", "b_knife", 1, 0);
			BuildItemTask(builder, "smth_tsk_004", "b_pistol", 1, 0);
			BuildItemTask(builder, "smth_tsk_005", "b_staff", 1, 0);
			BuildItemTask(builder, "smth_tsk_006", "b_longsword", 1, 0);
			BuildItemTask(builder, "smth_tsk_007", "b_katar", 1, 0);
			BuildItemTask(builder, "smth_tsk_008", "b_shuriken", 1, 0);
			BuildItemTask(builder, "smth_tsk_009", "b_rifle", 1, 0);
			BuildItemTask(builder, "smth_tsk_010", "b_twinblade", 1, 0);
			BuildItemTask(builder, "smth_tsk_011", "bm_shield", 1, 0);
			BuildItemTask(builder, "smth_tsk_012", "bm_cloak", 1, 0);
			BuildItemTask(builder, "smth_tsk_013", "bm_belt", 1, 0);
			BuildItemTask(builder, "smth_tsk_014", "bm_ring", 1, 0);
			BuildItemTask(builder, "smth_tsk_015", "bm_necklace", 1, 0);
			BuildItemTask(builder, "smth_tsk_016", "bm_armor", 1, 0);
			BuildItemTask(builder, "smth_tsk_017", "bm_helmet", 1, 0);
			BuildItemTask(builder, "smth_tsk_018", "bm_bracer", 1, 0);
			BuildItemTask(builder, "smth_tsk_019", "bm_leggings", 1, 0);
			BuildItemTask(builder, "smth_tsk_020", "sm_cloak", 1, 0);
			BuildItemTask(builder, "smth_tsk_021", "sm_belt", 1, 0);
			BuildItemTask(builder, "smth_tsk_022", "sm_ring", 1, 0);
			BuildItemTask(builder, "smth_tsk_023", "sm_necklace", 1, 0);
			BuildItemTask(builder, "smth_tsk_024", "sm_tunic", 1, 0);
			BuildItemTask(builder, "smth_tsk_025", "sm_cap", 1, 0);
			BuildItemTask(builder, "smth_tsk_026", "sm_gloves", 1, 0);
			BuildItemTask(builder, "smth_tsk_027", "sm_boots", 1, 0);
			BuildItemTask(builder, "smth_tsk_028", "com_cloak", 1, 0);
			BuildItemTask(builder, "smth_tsk_029", "com_belt", 1, 0);
			BuildItemTask(builder, "smth_tsk_030", "com_ring", 1, 0);
			BuildItemTask(builder, "smth_tsk_031", "com_necklace", 1, 0);
			BuildItemTask(builder, "smth_tsk_032", "com_tunic", 1, 0);
			BuildItemTask(builder, "smth_tsk_033", "com_cap", 1, 0);
			BuildItemTask(builder, "smth_tsk_034", "com_gloves", 1, 0);
			BuildItemTask(builder, "smth_tsk_035", "com_boots", 1, 0);

            // Tier 2 (Rank 1)
            BuildItemTask(builder, "smth_tsk_036", "tit_greatsword", 1, 1);
            BuildItemTask(builder, "smth_tsk_037", "tit_spear", 1, 1);
            BuildItemTask(builder, "smth_tsk_038", "tit_knife", 1, 1);
            BuildItemTask(builder, "smth_tsk_039", "tit_pistol", 1, 1);
            BuildItemTask(builder, "smth_tsk_040", "tit_staff", 1, 1);
            BuildItemTask(builder, "smth_tsk_041", "tit_longsword", 1, 1);
            BuildItemTask(builder, "smth_tsk_042", "tit_katar", 1, 1);
            BuildItemTask(builder, "smth_tsk_043", "tit_shuriken", 1, 1);
            BuildItemTask(builder, "smth_tsk_044", "tit_rifle", 1, 1);
            BuildItemTask(builder, "smth_tsk_045", "tit_twinblade", 1, 1);
            BuildItemTask(builder, "smth_tsk_046", "tit_shield", 1, 1);
            BuildItemTask(builder, "smth_tsk_047", "tit_cloak", 1, 1);
            BuildItemTask(builder, "smth_tsk_048", "tit_belt", 1, 1);
            BuildItemTask(builder, "smth_tsk_049", "tit_ring", 1, 1);
            BuildItemTask(builder, "smth_tsk_050", "tit_necklace", 1, 1);
            BuildItemTask(builder, "smth_tsk_051", "tit_armor", 1, 1);
            BuildItemTask(builder, "smth_tsk_052", "tit_helmet", 1, 1);
            BuildItemTask(builder, "smth_tsk_053", "tit_bracer", 1, 1);
            BuildItemTask(builder, "smth_tsk_054", "tit_leggings", 1, 1);
            BuildItemTask(builder, "smth_tsk_055", "viv_cloak", 1, 1);
            BuildItemTask(builder, "smth_tsk_056", "viv_belt", 1, 1);
            BuildItemTask(builder, "smth_tsk_057", "viv_ring", 1, 1);
            BuildItemTask(builder, "smth_tsk_058", "viv_necklace", 1, 1);
            BuildItemTask(builder, "smth_tsk_059", "viv_tunic", 1, 1);
            BuildItemTask(builder, "smth_tsk_060", "viv_cap", 1, 1);
            BuildItemTask(builder, "smth_tsk_061", "viv_gloves", 1, 1);
            BuildItemTask(builder, "smth_tsk_062", "viv_boots", 1, 1);
            BuildItemTask(builder, "smth_tsk_063", "val_cloak", 1, 1);
            BuildItemTask(builder, "smth_tsk_064", "val_belt", 1, 1);
            BuildItemTask(builder, "smth_tsk_065", "val_ring", 1, 1);
            BuildItemTask(builder, "smth_tsk_066", "val_necklace", 1, 1);
            BuildItemTask(builder, "smth_tsk_067", "val_tunic", 1, 1);
            BuildItemTask(builder, "smth_tsk_068", "val_cap", 1, 1);
            BuildItemTask(builder, "smth_tsk_069", "val_gloves", 1, 1);
            BuildItemTask(builder, "smth_tsk_070", "val_boots", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "smth_tsk_071", "del_greatsword", 1, 2);
            BuildItemTask(builder, "smth_tsk_072", "del_spear", 1, 2);
            BuildItemTask(builder, "smth_tsk_073", "del_knife", 1, 2);
            BuildItemTask(builder, "smth_tsk_074", "del_pistol", 1, 2);
            BuildItemTask(builder, "smth_tsk_075", "del_staff", 1, 2);
            BuildItemTask(builder, "smth_tsk_076", "del_longsword", 1, 2);
            BuildItemTask(builder, "smth_tsk_077", "del_katar", 1, 2);
            BuildItemTask(builder, "smth_tsk_078", "del_shuriken", 1, 2);
            BuildItemTask(builder, "smth_tsk_079", "del_rifle", 1, 2);
            BuildItemTask(builder, "smth_tsk_080", "del_twinblade", 1, 2);
            BuildItemTask(builder, "smth_tsk_081", "qk_shield", 1, 2);
            BuildItemTask(builder, "smth_tsk_082", "qk_cloak", 1, 2);
            BuildItemTask(builder, "smth_tsk_083", "qk_belt", 1, 2);
            BuildItemTask(builder, "smth_tsk_084", "qk_ring", 1, 2);
            BuildItemTask(builder, "smth_tsk_085", "qk_necklace", 1, 2);
            BuildItemTask(builder, "smth_tsk_086", "qk_armor", 1, 2);
            BuildItemTask(builder, "smth_tsk_087", "qk_helmet", 1, 2);
            BuildItemTask(builder, "smth_tsk_088", "qk_bracer", 1, 2);
            BuildItemTask(builder, "smth_tsk_089", "qk_leggings", 1, 2);
            BuildItemTask(builder, "smth_tsk_090", "reg_cloak", 1, 2);
            BuildItemTask(builder, "smth_tsk_091", "reg_belt", 1, 2);
            BuildItemTask(builder, "smth_tsk_092", "reg_ring", 1, 2);
            BuildItemTask(builder, "smth_tsk_093", "reg_necklace", 1, 2);
            BuildItemTask(builder, "smth_tsk_094", "reg_tunic", 1, 2);
            BuildItemTask(builder, "smth_tsk_095", "reg_cap", 1, 2);
            BuildItemTask(builder, "smth_tsk_096", "reg_gloves", 1, 2);
            BuildItemTask(builder, "smth_tsk_097", "reg_boots", 1, 2);
            BuildItemTask(builder, "smth_tsk_098", "for_cloak", 1, 2);
            BuildItemTask(builder, "smth_tsk_099", "for_belt", 1, 2);
            BuildItemTask(builder, "smth_tsk_100", "for_ring", 1, 2);
            BuildItemTask(builder, "smth_tsk_101", "for_necklace", 1, 2);
            BuildItemTask(builder, "smth_tsk_102", "for_tunic", 1, 2);
            BuildItemTask(builder, "smth_tsk_103", "for_cap", 1, 2);
            BuildItemTask(builder, "smth_tsk_104", "for_gloves", 1, 2);
            BuildItemTask(builder, "smth_tsk_105", "for_boots", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "smth_tsk_106", "proto_greatsword", 1, 3);
            BuildItemTask(builder, "smth_tsk_107", "proto_spear", 1, 3);
            BuildItemTask(builder, "smth_tsk_108", "proto_knife", 1, 3);
            BuildItemTask(builder, "smth_tsk_109", "proto_pistol", 1, 3);
            BuildItemTask(builder, "smth_tsk_110", "proto_staff", 1, 3);
            BuildItemTask(builder, "smth_tsk_111", "pro_longsword", 1, 3);
            BuildItemTask(builder, "smth_tsk_112", "proto_katar", 1, 3);
            BuildItemTask(builder, "smth_tsk_113", "proto_shuriken", 1, 3);
            BuildItemTask(builder, "smth_tsk_114", "proto_rifle", 1, 3);
            BuildItemTask(builder, "smth_tsk_115", "proto_twinblade", 1, 3);
            BuildItemTask(builder, "smth_tsk_116", "ar_shield", 1, 3);
            BuildItemTask(builder, "smth_tsk_117", "ar_cloak", 1, 3);
            BuildItemTask(builder, "smth_tsk_118", "ar_belt", 1, 3);
            BuildItemTask(builder, "smth_tsk_119", "ar_ring", 1, 3);
            BuildItemTask(builder, "smth_tsk_120", "ar_necklace", 1, 3);
            BuildItemTask(builder, "smth_tsk_121", "ar_armor", 1, 3);
            BuildItemTask(builder, "smth_tsk_122", "ar_helmet", 1, 3);
            BuildItemTask(builder, "smth_tsk_123", "ar_bracer", 1, 3);
            BuildItemTask(builder, "smth_tsk_124", "ar_leggings", 1, 3);
            BuildItemTask(builder, "smth_tsk_125", "gr_gloak", 1, 3);
            BuildItemTask(builder, "smth_tsk_126", "gr_belt", 1, 3);
            BuildItemTask(builder, "smth_tsk_127", "gr_ring", 1, 3);
            BuildItemTask(builder, "smth_tsk_128", "gr_necklace", 1, 3);
            BuildItemTask(builder, "smth_tsk_129", "gr_tunic", 1, 3);
            BuildItemTask(builder, "smth_tsk_130", "gr_cap", 1, 3);
            BuildItemTask(builder, "smth_tsk_131", "gr_gloves", 1, 3);
            BuildItemTask(builder, "smth_tsk_132", "gr_boots", 1, 3);
            BuildItemTask(builder, "smth_tsk_133", "sur_cloak", 1, 3);
            BuildItemTask(builder, "smth_tsk_134", "sur_belt", 1, 3);
            BuildItemTask(builder, "smth_tsk_135", "sur_ring", 1, 3);
            BuildItemTask(builder, "smth_tsk_136", "sur_necklace", 1, 3);
            BuildItemTask(builder, "smth_tsk_137", "sur_tunic", 1, 3);
            BuildItemTask(builder, "smth_tsk_138", "sur_cap", 1, 3);
            BuildItemTask(builder, "smth_tsk_139", "sur_gloves", 1, 3);
            BuildItemTask(builder, "smth_tsk_140", "sur_boots", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "smth_tsk_141", "oph_greatsword", 1, 4);
            BuildItemTask(builder, "smth_tsk_142", "oph_spear", 1, 4);
            BuildItemTask(builder, "smth_tsk_143", "oph_knife", 1, 4);
            BuildItemTask(builder, "smth_tsk_144", "oph_pistol", 1, 4);
            BuildItemTask(builder, "smth_tsk_145", "oph_staff", 1, 4);
            BuildItemTask(builder, "smth_tsk_146", "oph_longsword", 1, 4);
            BuildItemTask(builder, "smth_tsk_147", "oph_katar", 1, 4);
            BuildItemTask(builder, "smth_tsk_148", "oph_shuriken", 1, 4);
            BuildItemTask(builder, "smth_tsk_149", "oph_rifle", 1, 4);
            BuildItemTask(builder, "smth_tsk_150", "oph_twinblade", 1, 4);
            BuildItemTask(builder, "smth_tsk_151", "ec_shield", 1, 4);
            BuildItemTask(builder, "smth_tsk_152", "ec_cloak", 1, 4);
            BuildItemTask(builder, "smth_tsk_153", "ec_belt", 1, 4);
            BuildItemTask(builder, "smth_tsk_154", "ec_ring", 1, 4);
            BuildItemTask(builder, "smth_tsk_155", "ec_necklace", 1, 4);
            BuildItemTask(builder, "smth_tsk_156", "ec_armor", 1, 4);
            BuildItemTask(builder, "smth_tsk_157", "ec_helmet", 1, 4);
            BuildItemTask(builder, "smth_tsk_158", "ec_bracer", 1, 4);
            BuildItemTask(builder, "smth_tsk_159", "ec_leggings", 1, 4);
            BuildItemTask(builder, "smth_tsk_160", "tran_cloak", 1, 4);
            BuildItemTask(builder, "smth_tsk_161", "tran_belt", 1, 4);
            BuildItemTask(builder, "smth_tsk_162", "tran_ring", 1, 4);
            BuildItemTask(builder, "smth_tsk_163", "tran_necklace", 1, 4);
            BuildItemTask(builder, "smth_tsk_164", "tran_tunic", 1, 4);
            BuildItemTask(builder, "smth_tsk_165", "tran_cap", 1, 4);
            BuildItemTask(builder, "smth_tsk_166", "tran_gloves", 1, 4);
            BuildItemTask(builder, "smth_tsk_167", "tran_boots", 1, 4);
            BuildItemTask(builder, "smth_tsk_168", "sup_cloak", 1, 4);
            BuildItemTask(builder, "smth_tsk_169", "sup_belt", 1, 4);
            BuildItemTask(builder, "smth_tsk_170", "sup_ring", 1, 4);
            BuildItemTask(builder, "smth_tsk_171", "sup_necklace", 1, 4);
            BuildItemTask(builder, "smth_tsk_172", "sup_tunic", 1, 4);
            BuildItemTask(builder, "smth_tsk_173", "sup_cap", 1, 4);
            BuildItemTask(builder, "smth_tsk_174", "sup_gloves", 1, 4);
            BuildItemTask(builder, "smth_tsk_175", "sup_boots", 1, 4);

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
                .IsGuildTask(GuildType.SmitheryGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Smithery Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.SmitheryGuild, rewardDetails.GP);
        }
    }
}