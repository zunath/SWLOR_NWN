using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class FabricationGuildQuestDefinition : IQuestListDefinition
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
            BuildItemTask(builder, "fab_tsk_001", "structure_0085", 1, 0);
            BuildItemTask(builder, "fab_tsk_002", "structure_0045", 1, 0);
            BuildItemTask(builder, "fab_tsk_003", "structure_0132", 1, 0);
            BuildItemTask(builder, "fab_tsk_004", "structure_0062", 1, 0);
            BuildItemTask(builder, "fab_tsk_005", "structure_0077", 1, 0);
            BuildItemTask(builder, "fab_tsk_006", "structure_0133", 1, 0);
            BuildItemTask(builder, "fab_tsk_007", "structure_0215", 1, 0);
            BuildItemTask(builder, "fab_tsk_008", "structure_0069", 1, 0);
            BuildItemTask(builder, "fab_tsk_009", "structure_0047", 1, 0);
            BuildItemTask(builder, "fab_tsk_010", "structure_0120", 1, 0);
            BuildItemTask(builder, "fab_tsk_011", "structure_0023", 1, 0);
            BuildItemTask(builder, "fab_tsk_012", "structure_0072", 1, 0);
            BuildItemTask(builder, "fab_tsk_013", "structure_0119", 1, 0);
            BuildItemTask(builder, "fab_tsk_014", "structure_0201", 1, 0);
            BuildItemTask(builder, "fab_tsk_015", "structure_0061", 1, 0);
            BuildItemTask(builder, "fab_tsk_016", "structure_0070", 1, 0);
            BuildItemTask(builder, "fab_tsk_017", "structure_0118", 1, 0);
            BuildItemTask(builder, "fab_tsk_018", "structure_0057", 1, 0);
            BuildItemTask(builder, "fab_tsk_019", "structure_0059", 1, 0);
            BuildItemTask(builder, "fab_tsk_020", "structure_0107", 1, 0);
            BuildItemTask(builder, "fab_tsk_021", "structure_0176", 1, 0);
            BuildItemTask(builder, "fab_tsk_022", "structure_0022", 1, 0);
            BuildItemTask(builder, "fab_tsk_023", "structure_0080", 1, 0);
            BuildItemTask(builder, "fab_tsk_024", "structure_0091", 1, 0);
            BuildItemTask(builder, "fab_tsk_025", "structure_0051", 1, 0);
            BuildItemTask(builder, "fab_tsk_026", "structure_0013", 1, 0);
            BuildItemTask(builder, "fab_tsk_027", "structure_0144", 1, 0);
            BuildItemTask(builder, "fab_tsk_028", "structure_0210", 1, 0);
            BuildItemTask(builder, "fab_tsk_029", "structure_0019", 1, 0);
            BuildItemTask(builder, "fab_tsk_030", "structure_0081", 1, 0);
            BuildItemTask(builder, "fab_tsk_031", "structure_0145", 1, 0);
            BuildItemTask(builder, "fab_tsk_032", "structure_0150", 1, 0);
            BuildItemTask(builder, "fab_tsk_033", "structure_0039", 1, 0);
            BuildItemTask(builder, "fab_tsk_034", "structure_0006", 1, 0);
            BuildItemTask(builder, "fab_tsk_035", "structure_0056", 1, 0);
            BuildItemTask(builder, "fab_tsk_036", "structure_0192", 1, 0);

            // Tier 2 (Rank 1)
            BuildItemTask(builder, "fab_tsk_037", "structure_0007", 1, 1);
            BuildItemTask(builder, "fab_tsk_038", "structure_0026", 1, 1);
            BuildItemTask(builder, "fab_tsk_039", "structure_0113", 1, 1);
            BuildItemTask(builder, "fab_tsk_040", "structure_0008", 1, 1);
            BuildItemTask(builder, "fab_tsk_041", "structure_0082", 1, 1);
            BuildItemTask(builder, "fab_tsk_042", "structure_0127", 1, 1);
            BuildItemTask(builder, "fab_tsk_043", "structure_0060", 1, 1);
            BuildItemTask(builder, "fab_tsk_044", "structure_0025", 1, 1);
            BuildItemTask(builder, "fab_tsk_045", "structure_0139", 1, 1);
            BuildItemTask(builder, "fab_tsk_046", "structure_0190", 1, 1);
            BuildItemTask(builder, "fab_tsk_047", "structure_0021", 1, 1);
            BuildItemTask(builder, "fab_tsk_048", "structure_0084", 1, 1);
            BuildItemTask(builder, "fab_tsk_049", "structure_0135", 1, 1);
            BuildItemTask(builder, "fab_tsk_050", "structure_0017", 1, 1);
            BuildItemTask(builder, "fab_tsk_051", "structure_0086", 1, 1);
            BuildItemTask(builder, "fab_tsk_052", "structure_0140", 1, 1);
            BuildItemTask(builder, "fab_tsk_053", "structure_0018", 1, 1);
            BuildItemTask(builder, "fab_tsk_054", "structure_0038", 1, 1);
            BuildItemTask(builder, "fab_tsk_055", "structure_0141", 1, 1);
            BuildItemTask(builder, "fab_tsk_056", "structure_0020", 1, 1);
            BuildItemTask(builder, "fab_tsk_057", "structure_0088", 1, 1);
            BuildItemTask(builder, "fab_tsk_058", "structure_0136", 1, 1);
            BuildItemTask(builder, "fab_tsk_059", "structure_0174", 1, 1);
            BuildItemTask(builder, "fab_tsk_060", "structure_0209", 1, 1);
            BuildItemTask(builder, "fab_tsk_061", "structure_0005", 1, 1);
            BuildItemTask(builder, "fab_tsk_062", "structure_0092", 1, 1);
            BuildItemTask(builder, "fab_tsk_063", "structure_0142", 1, 1);
            BuildItemTask(builder, "fab_tsk_064", "structure_0177", 1, 1);
            BuildItemTask(builder, "fab_tsk_065", "structure_0030", 1, 1);
            BuildItemTask(builder, "fab_tsk_066", "structure_0040", 1, 1);
            BuildItemTask(builder, "fab_tsk_067", "structure_0137", 1, 1);
            BuildItemTask(builder, "fab_tsk_068", "structure_0189", 1, 1);
            BuildItemTask(builder, "fab_tsk_069", "structure_0014", 1, 1);
            BuildItemTask(builder, "fab_tsk_070", "structure_0090", 1, 1);
            BuildItemTask(builder, "fab_tsk_071", "structure_0143", 1, 1);
            BuildItemTask(builder, "fab_tsk_072", "structure_0175", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "fab_tsk_073", "structure_0004", 1, 2);
            BuildItemTask(builder, "fab_tsk_074", "structure_0032", 1, 2);
            BuildItemTask(builder, "fab_tsk_075", "structure_0146", 1, 2);
            BuildItemTask(builder, "fab_tsk_076", "structure_0180", 1, 2);
            BuildItemTask(builder, "fab_tsk_077", "pow_supp_unit", 1, 2);
            BuildItemTask(builder, "fab_tsk_078", "structure_0031", 1, 2);
            BuildItemTask(builder, "fab_tsk_079", "structure_0076", 1, 2);
            BuildItemTask(builder, "fab_tsk_080", "structure_0147", 1, 2);
            BuildItemTask(builder, "fab_tsk_081", "structure_0181", 1, 2);
            BuildItemTask(builder, "fab_tsk_082", "const_parts", 1, 2);
            BuildItemTask(builder, "fab_tsk_083", "structure_0029", 1, 2);
            BuildItemTask(builder, "fab_tsk_084", "structure_0078", 1, 2);
            BuildItemTask(builder, "fab_tsk_085", "structure_0148", 1, 2);
            BuildItemTask(builder, "fab_tsk_086", "structure_0182", 1, 2);
            BuildItemTask(builder, "fab_tsk_087", "structure_0067", 1, 2);
            BuildItemTask(builder, "fab_tsk_088", "structure_0033", 1, 2);
            BuildItemTask(builder, "fab_tsk_089", "structure_0149", 1, 2);
            BuildItemTask(builder, "fab_tsk_090", "structure_0178", 1, 2);
            BuildItemTask(builder, "fab_tsk_091", "structure_0037", 1, 2);
            BuildItemTask(builder, "fab_tsk_092", "structure_0079", 1, 2);
            BuildItemTask(builder, "fab_tsk_093", "structure_0100", 1, 2);
            BuildItemTask(builder, "fab_tsk_094", "structure_0183", 1, 2);
            BuildItemTask(builder, "fab_tsk_095", "structure_5000", 1, 2);
            BuildItemTask(builder, "fab_tsk_096", "structure_0024", 1, 2);
            BuildItemTask(builder, "fab_tsk_097", "structure_0083", 1, 2);
            BuildItemTask(builder, "fab_tsk_098", "structure_0128", 1, 2);
            BuildItemTask(builder, "fab_tsk_099", "structure_0184", 1, 2);
            BuildItemTask(builder, "fab_tsk_100", "structure_5005", 1, 2);
            BuildItemTask(builder, "fab_tsk_101", "structure_0043", 1, 2);
            BuildItemTask(builder, "fab_tsk_102", "structure_0101", 1, 2);
            BuildItemTask(builder, "fab_tsk_103", "structure_0151", 1, 2);
            BuildItemTask(builder, "fab_tsk_104", "structure_0179", 1, 2);
            BuildItemTask(builder, "fab_tsk_105", "structure_5006", 1, 2);
            BuildItemTask(builder, "fab_tsk_106", "structure_0068", 1, 2);
            BuildItemTask(builder, "fab_tsk_107", "structure_0035", 1, 2);
            BuildItemTask(builder, "fab_tsk_108", "structure_0185", 1, 2);
            BuildItemTask(builder, "fab_tsk_109", "structure_0064", 1, 2);
            BuildItemTask(builder, "fab_tsk_110", "structure_0093", 1, 2);
            BuildItemTask(builder, "fab_tsk_111", "structure_0153", 1, 2);
            BuildItemTask(builder, "fab_tsk_112", "structure_0186", 1, 2);
            BuildItemTask(builder, "fab_tsk_113", "structure_5007", 1, 2);
            BuildItemTask(builder, "fab_tsk_114", "structure_0055", 1, 2);
            BuildItemTask(builder, "fab_tsk_115", "structure_0095", 1, 2);
            BuildItemTask(builder, "fab_tsk_116", "structure_0154", 1, 2);
            BuildItemTask(builder, "fab_tsk_117", "structure_0187", 1, 2);
            BuildItemTask(builder, "fab_tsk_118", "structure_5008", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "fab_tsk_119", "r_const_parts", 1, 3);
            BuildItemTask(builder, "fab_tsk_120", "structure_0058", 1, 3);
            BuildItemTask(builder, "fab_tsk_121", "structure_0075", 1, 3);
            BuildItemTask(builder, "fab_tsk_122", "structure_0155", 1, 3);
            BuildItemTask(builder, "fab_tsk_123", "structure_0193", 1, 3);
            BuildItemTask(builder, "fab_tsk_124", "r_pow_supp_unit", 1, 3);
            BuildItemTask(builder, "fab_tsk_125", "structure_0012", 1, 3);
            BuildItemTask(builder, "fab_tsk_126", "structure_0115", 1, 3);
            BuildItemTask(builder, "fab_tsk_127", "structure_0130", 1, 3);
            BuildItemTask(builder, "fab_tsk_128", "structure_0194", 1, 3);
            BuildItemTask(builder, "fab_tsk_129", "structure_0073", 1, 3);
            BuildItemTask(builder, "fab_tsk_130", "structure_0054", 1, 3);
            BuildItemTask(builder, "fab_tsk_131", "structure_0156", 1, 3);
            BuildItemTask(builder, "fab_tsk_132", "structure_0195", 1, 3);
            BuildItemTask(builder, "fab_tsk_133", "structure_5009", 1, 3);
            BuildItemTask(builder, "fab_tsk_134", "structure_0065", 1, 3);
            BuildItemTask(builder, "fab_tsk_135", "structure_0114", 1, 3);
            BuildItemTask(builder, "fab_tsk_136", "structure_0131", 1, 3);
            BuildItemTask(builder, "fab_tsk_137", "structure_0202", 1, 3);
            BuildItemTask(builder, "fab_tsk_138", "structure_5010", 1, 3);
            BuildItemTask(builder, "fab_tsk_139", "structure_0009", 1, 3);
            BuildItemTask(builder, "fab_tsk_140", "structure_0111", 1, 3);
            BuildItemTask(builder, "fab_tsk_141", "structure_0157", 1, 3);
            BuildItemTask(builder, "fab_tsk_142", "structure_0203", 1, 3);
            BuildItemTask(builder, "fab_tsk_143", "structure_5011", 1, 3);
            BuildItemTask(builder, "fab_tsk_144", "structure_0036", 1, 3);
            BuildItemTask(builder, "fab_tsk_145", "structure_0108", 1, 3);
            BuildItemTask(builder, "fab_tsk_146", "structure_0158", 1, 3);
            BuildItemTask(builder, "fab_tsk_147", "structure_0204", 1, 3);
            BuildItemTask(builder, "fab_tsk_148", "structure_5012", 1, 3);
            BuildItemTask(builder, "fab_tsk_149", "structure_0028", 1, 3);
            BuildItemTask(builder, "fab_tsk_150", "structure_0126", 1, 3);
            BuildItemTask(builder, "fab_tsk_151", "structure_0159", 1, 3);
            BuildItemTask(builder, "fab_tsk_152", "structure_0205", 1, 3);
            BuildItemTask(builder, "fab_tsk_153", "structure_0211", 1, 3);
            BuildItemTask(builder, "fab_tsk_154", "structure_0010", 1, 3);
            BuildItemTask(builder, "fab_tsk_155", "structure_0074", 1, 3);
            BuildItemTask(builder, "fab_tsk_156", "structure_0160", 1, 3);
            BuildItemTask(builder, "fab_tsk_157", "structure_0206", 1, 3);
            BuildItemTask(builder, "fab_tsk_158", "structure_0212", 1, 3);
            BuildItemTask(builder, "fab_tsk_159", "structure_0106", 1, 3);
            BuildItemTask(builder, "fab_tsk_160", "structure_0052", 1, 3);
            BuildItemTask(builder, "fab_tsk_161", "structure_0161", 1, 3);
            BuildItemTask(builder, "fab_tsk_162", "structure_0207", 1, 3);
            BuildItemTask(builder, "fab_tsk_163", "structure_0213", 1, 3);
            BuildItemTask(builder, "fab_tsk_164", "structure_0104", 1, 3);
            BuildItemTask(builder, "fab_tsk_165", "structure_0041", 1, 3);
            BuildItemTask(builder, "fab_tsk_166", "structure_0162", 1, 3);
            BuildItemTask(builder, "fab_tsk_167", "structure_0208", 1, 3);
            BuildItemTask(builder, "fab_tsk_168", "structure_0214", 1, 3);
            BuildItemTask(builder, "fab_tsk_169", "structure_5004", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "fab_tsk_170", "structure_0027", 1, 4);
            BuildItemTask(builder, "fab_tsk_171", "structure_0098", 1, 4);
            BuildItemTask(builder, "fab_tsk_172", "structure_0163", 1, 4);
            BuildItemTask(builder, "fab_tsk_173", "structure_0196", 1, 4);
            BuildItemTask(builder, "fab_tsk_174", "structure_5001", 1, 4);
            BuildItemTask(builder, "fab_tsk_175", "structure_0048", 1, 4);
            BuildItemTask(builder, "fab_tsk_176", "structure_0099", 1, 4);
            BuildItemTask(builder, "fab_tsk_177", "structure_0164", 1, 4);
            BuildItemTask(builder, "fab_tsk_178", "structure_0197", 1, 4);
            BuildItemTask(builder, "fab_tsk_179", "structure_0042", 1, 4);
            BuildItemTask(builder, "fab_tsk_180", "structure_0089", 1, 4);
            BuildItemTask(builder, "fab_tsk_181", "structure_0165", 1, 4);
            BuildItemTask(builder, "fab_tsk_182", "structure_0198", 1, 4);
            BuildItemTask(builder, "fab_tsk_183", "structure_5002", 1, 4);
            BuildItemTask(builder, "fab_tsk_184", "structure_0122", 1, 4);
            BuildItemTask(builder, "fab_tsk_185", "structure_0097", 1, 4);
            BuildItemTask(builder, "fab_tsk_186", "structure_0166", 1, 4);
            BuildItemTask(builder, "fab_tsk_187", "structure_0199", 1, 4);
            BuildItemTask(builder, "fab_tsk_188", "structure_0121", 1, 4);
            BuildItemTask(builder, "fab_tsk_189", "structure_0112", 1, 4);
            BuildItemTask(builder, "fab_tsk_190", "structure_0167", 1, 4);
            BuildItemTask(builder, "fab_tsk_191", "structure_0200", 1, 4);
            BuildItemTask(builder, "fab_tsk_192", "structure_5013", 1, 4);
            BuildItemTask(builder, "fab_tsk_193", "structure_0044", 1, 4);
            BuildItemTask(builder, "fab_tsk_194", "structure_0103", 1, 4);
            BuildItemTask(builder, "fab_tsk_195", "structure_0168", 1, 4);
            BuildItemTask(builder, "fab_tsk_196", "structure_5014", 1, 4);
            BuildItemTask(builder, "fab_tsk_197", "structure_0096", 1, 4);
            BuildItemTask(builder, "fab_tsk_198", "structure_0116", 1, 4);
            BuildItemTask(builder, "fab_tsk_199", "structure_0169", 1, 4);
            BuildItemTask(builder, "fab_tsk_200", "structure_5015", 1, 4);
            BuildItemTask(builder, "fab_tsk_201", "structure_0123", 1, 4);
            BuildItemTask(builder, "fab_tsk_202", "structure_0102", 1, 4);
            BuildItemTask(builder, "fab_tsk_203", "structure_0170", 1, 4);
            BuildItemTask(builder, "fab_tsk_204", "structure_5016", 1, 4);
            BuildItemTask(builder, "fab_tsk_205", "structure_0124", 1, 4);
            BuildItemTask(builder, "fab_tsk_206", "structure_0117", 1, 4);
            BuildItemTask(builder, "fab_tsk_207", "structure_0171", 1, 4);
            BuildItemTask(builder, "fab_tsk_208", "structure_0125", 1, 4);
            BuildItemTask(builder, "fab_tsk_209", "structure_0172", 1, 4);
            BuildItemTask(builder, "fab_tsk_210", "structure_0173", 1, 4);
            BuildItemTask(builder, "fab_tsk_211", "structure_5003", 1, 4);

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
                .IsGuildTask(GuildType.FabricationGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Fabrication Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.FabricationGuild, rewardDetails.GP);
        }
    }
}