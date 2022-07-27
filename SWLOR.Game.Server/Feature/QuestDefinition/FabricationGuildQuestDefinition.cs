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
            BuildItemTask(builder, "fab_tsk_200", "structure_0007", 1, 1);
            BuildItemTask(builder, "fab_tsk_201", "structure_0026", 1, 1);
            BuildItemTask(builder, "fab_tsk_202", "structure_0113", 1, 1);
            BuildItemTask(builder, "fab_tsk_203", "structure_0008", 1, 1);
            BuildItemTask(builder, "fab_tsk_204", "structure_0082", 1, 1);
            BuildItemTask(builder, "fab_tsk_205", "structure_0127", 1, 1);
            BuildItemTask(builder, "fab_tsk_206", "structure_0060", 1, 1);
            BuildItemTask(builder, "fab_tsk_207", "structure_0025", 1, 1);
            BuildItemTask(builder, "fab_tsk_208", "structure_0139", 1, 1);
            BuildItemTask(builder, "fab_tsk_209", "structure_0190", 1, 1);
            BuildItemTask(builder, "fab_tsk_210", "structure_0021", 1, 1);
            BuildItemTask(builder, "fab_tsk_211", "structure_0084", 1, 1);
            BuildItemTask(builder, "fab_tsk_212", "structure_0135", 1, 1);
            BuildItemTask(builder, "fab_tsk_213", "structure_0017", 1, 1);
            BuildItemTask(builder, "fab_tsk_214", "structure_0086", 1, 1);
            BuildItemTask(builder, "fab_tsk_215", "structure_0140", 1, 1);
            BuildItemTask(builder, "fab_tsk_216", "structure_0018", 1, 1);
            BuildItemTask(builder, "fab_tsk_217", "structure_0038", 1, 1);
            BuildItemTask(builder, "fab_tsk_218", "structure_0141", 1, 1);
            BuildItemTask(builder, "fab_tsk_219", "structure_0020", 1, 1);
            BuildItemTask(builder, "fab_tsk_220", "structure_0088", 1, 1);
            BuildItemTask(builder, "fab_tsk_221", "structure_0136", 1, 1);
            BuildItemTask(builder, "fab_tsk_222", "structure_0174", 1, 1);
            BuildItemTask(builder, "fab_tsk_223", "structure_0209", 1, 1);
            BuildItemTask(builder, "fab_tsk_224", "structure_0005", 1, 1);
            BuildItemTask(builder, "fab_tsk_225", "structure_0092", 1, 1);
            BuildItemTask(builder, "fab_tsk_226", "structure_0142", 1, 1);
            BuildItemTask(builder, "fab_tsk_227", "structure_0177", 1, 1);
            BuildItemTask(builder, "fab_tsk_228", "structure_0030", 1, 1);
            BuildItemTask(builder, "fab_tsk_229", "structure_0040", 1, 1);
            BuildItemTask(builder, "fab_tsk_230", "structure_0137", 1, 1);
            BuildItemTask(builder, "fab_tsk_231", "structure_0189", 1, 1);
            BuildItemTask(builder, "fab_tsk_232", "structure_0014", 1, 1);
            BuildItemTask(builder, "fab_tsk_233", "structure_0090", 1, 1);
            BuildItemTask(builder, "fab_tsk_234", "structure_0143", 1, 1);
            BuildItemTask(builder, "fab_tsk_235", "structure_0175", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "fab_tsk_400", "structure_0004", 1, 2);
            BuildItemTask(builder, "fab_tsk_401", "structure_0032", 1, 2);
            BuildItemTask(builder, "fab_tsk_402", "structure_0146", 1, 2);
            BuildItemTask(builder, "fab_tsk_403", "structure_0180", 1, 2);
            BuildItemTask(builder, "fab_tsk_404", "pow_supp_unit", 1, 2);
            BuildItemTask(builder, "fab_tsk_405", "structure_0031", 1, 2);
            BuildItemTask(builder, "fab_tsk_406", "structure_0076", 1, 2);
            BuildItemTask(builder, "fab_tsk_407", "structure_0147", 1, 2);
            BuildItemTask(builder, "fab_tsk_408", "structure_0181", 1, 2);
            BuildItemTask(builder, "fab_tsk_409", "const_parts", 1, 2);
            BuildItemTask(builder, "fab_tsk_410", "structure_0029", 1, 2);
            BuildItemTask(builder, "fab_tsk_411", "structure_0078", 1, 2);
            BuildItemTask(builder, "fab_tsk_412", "structure_0148", 1, 2);
            BuildItemTask(builder, "fab_tsk_413", "structure_0182", 1, 2);
            BuildItemTask(builder, "fab_tsk_414", "structure_0067", 1, 2);
            BuildItemTask(builder, "fab_tsk_415", "structure_0033", 1, 2);
            BuildItemTask(builder, "fab_tsk_416", "structure_0149", 1, 2);
            BuildItemTask(builder, "fab_tsk_417", "structure_0178", 1, 2);
            BuildItemTask(builder, "fab_tsk_418", "structure_0037", 1, 2);
            BuildItemTask(builder, "fab_tsk_419", "structure_0079", 1, 2);
            BuildItemTask(builder, "fab_tsk_420", "structure_0100", 1, 2);
            BuildItemTask(builder, "fab_tsk_421", "structure_0183", 1, 2);
            BuildItemTask(builder, "fab_tsk_422", "structure_5000", 1, 2);
            BuildItemTask(builder, "fab_tsk_423", "structure_0024", 1, 2);
            BuildItemTask(builder, "fab_tsk_424", "structure_0083", 1, 2);
            BuildItemTask(builder, "fab_tsk_425", "structure_0128", 1, 2);
            BuildItemTask(builder, "fab_tsk_426", "structure_0184", 1, 2);
            BuildItemTask(builder, "fab_tsk_427", "structure_5005", 1, 2);
            BuildItemTask(builder, "fab_tsk_428", "structure_0043", 1, 2);
            BuildItemTask(builder, "fab_tsk_429", "structure_0101", 1, 2);
            BuildItemTask(builder, "fab_tsk_430", "structure_0151", 1, 2);
            BuildItemTask(builder, "fab_tsk_431", "structure_0179", 1, 2);
            BuildItemTask(builder, "fab_tsk_432", "structure_5006", 1, 2);
            BuildItemTask(builder, "fab_tsk_433", "structure_0068", 1, 2);
            BuildItemTask(builder, "fab_tsk_434", "structure_0035", 1, 2);
            BuildItemTask(builder, "fab_tsk_435", "structure_0185", 1, 2);
            BuildItemTask(builder, "fab_tsk_436", "structure_0064", 1, 2);
            BuildItemTask(builder, "fab_tsk_437", "structure_0093", 1, 2);
            BuildItemTask(builder, "fab_tsk_438", "structure_0153", 1, 2);
            BuildItemTask(builder, "fab_tsk_439", "structure_0186", 1, 2);
            BuildItemTask(builder, "fab_tsk_440", "structure_5007", 1, 2);
            BuildItemTask(builder, "fab_tsk_441", "structure_0055", 1, 2);
            BuildItemTask(builder, "fab_tsk_442", "structure_0095", 1, 2);
            BuildItemTask(builder, "fab_tsk_443", "structure_0154", 1, 2);
            BuildItemTask(builder, "fab_tsk_444", "structure_0187", 1, 2);
            BuildItemTask(builder, "fab_tsk_445", "structure_5008", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "fab_tsk_600", "r_const_parts", 1, 3);
            BuildItemTask(builder, "fab_tsk_601", "structure_0058", 1, 3);
            BuildItemTask(builder, "fab_tsk_602", "structure_0075", 1, 3);
            BuildItemTask(builder, "fab_tsk_603", "structure_0155", 1, 3);
            BuildItemTask(builder, "fab_tsk_604", "structure_0193", 1, 3);
            BuildItemTask(builder, "fab_tsk_605", "r_pow_supp_unit", 1, 3);
            BuildItemTask(builder, "fab_tsk_606", "structure_0012", 1, 3);
            BuildItemTask(builder, "fab_tsk_607", "structure_0115", 1, 3);
            BuildItemTask(builder, "fab_tsk_608", "structure_0130", 1, 3);
            BuildItemTask(builder, "fab_tsk_609", "structure_0194", 1, 3);
            BuildItemTask(builder, "fab_tsk_610", "structure_0073", 1, 3);
            BuildItemTask(builder, "fab_tsk_611", "structure_0054", 1, 3);
            BuildItemTask(builder, "fab_tsk_612", "structure_0156", 1, 3);
            BuildItemTask(builder, "fab_tsk_613", "structure_0195", 1, 3);
            BuildItemTask(builder, "fab_tsk_614", "structure_5009", 1, 3);
            BuildItemTask(builder, "fab_tsk_615", "structure_0065", 1, 3);
            BuildItemTask(builder, "fab_tsk_616", "structure_0114", 1, 3);
            BuildItemTask(builder, "fab_tsk_617", "structure_0131", 1, 3);
            BuildItemTask(builder, "fab_tsk_618", "structure_0202", 1, 3);
            BuildItemTask(builder, "fab_tsk_619", "structure_5010", 1, 3);
            BuildItemTask(builder, "fab_tsk_620", "structure_0009", 1, 3);
            BuildItemTask(builder, "fab_tsk_621", "structure_0111", 1, 3);
            BuildItemTask(builder, "fab_tsk_622", "structure_0157", 1, 3);
            BuildItemTask(builder, "fab_tsk_623", "structure_0203", 1, 3);
            BuildItemTask(builder, "fab_tsk_624", "structure_5011", 1, 3);
            BuildItemTask(builder, "fab_tsk_625", "structure_0036", 1, 3);
            BuildItemTask(builder, "fab_tsk_626", "structure_0108", 1, 3);
            BuildItemTask(builder, "fab_tsk_627", "structure_0158", 1, 3);
            BuildItemTask(builder, "fab_tsk_628", "structure_0204", 1, 3);
            BuildItemTask(builder, "fab_tsk_629", "structure_5012", 1, 3);
            BuildItemTask(builder, "fab_tsk_630", "structure_0028", 1, 3);
            BuildItemTask(builder, "fab_tsk_631", "structure_0126", 1, 3);
            BuildItemTask(builder, "fab_tsk_632", "structure_0159", 1, 3);
            BuildItemTask(builder, "fab_tsk_633", "structure_0205", 1, 3);
            BuildItemTask(builder, "fab_tsk_634", "structure_0211", 1, 3);
            BuildItemTask(builder, "fab_tsk_635", "structure_0010", 1, 3);
            BuildItemTask(builder, "fab_tsk_636", "structure_0074", 1, 3);
            BuildItemTask(builder, "fab_tsk_637", "structure_0160", 1, 3);
            BuildItemTask(builder, "fab_tsk_638", "structure_0206", 1, 3);
            BuildItemTask(builder, "fab_tsk_639", "structure_0212", 1, 3);
            BuildItemTask(builder, "fab_tsk_640", "structure_0106", 1, 3);
            BuildItemTask(builder, "fab_tsk_641", "structure_0052", 1, 3);
            BuildItemTask(builder, "fab_tsk_642", "structure_0161", 1, 3);
            BuildItemTask(builder, "fab_tsk_643", "structure_0207", 1, 3);
            BuildItemTask(builder, "fab_tsk_644", "structure_0213", 1, 3);
            BuildItemTask(builder, "fab_tsk_645", "structure_0104", 1, 3);
            BuildItemTask(builder, "fab_tsk_646", "structure_0041", 1, 3);
            BuildItemTask(builder, "fab_tsk_647", "structure_0162", 1, 3);
            BuildItemTask(builder, "fab_tsk_648", "structure_0208", 1, 3);
            BuildItemTask(builder, "fab_tsk_649", "structure_0214", 1, 3);
            BuildItemTask(builder, "fab_tsk_650", "structure_5004", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "fab_tsk_800", "structure_0027", 1, 4);
            BuildItemTask(builder, "fab_tsk_801", "structure_0098", 1, 4);
            BuildItemTask(builder, "fab_tsk_802", "structure_0163", 1, 4);
            BuildItemTask(builder, "fab_tsk_803", "structure_0196", 1, 4);
            BuildItemTask(builder, "fab_tsk_804", "structure_5001", 1, 4);
            BuildItemTask(builder, "fab_tsk_805", "structure_0048", 1, 4);
            BuildItemTask(builder, "fab_tsk_806", "structure_0099", 1, 4);
            BuildItemTask(builder, "fab_tsk_807", "structure_0164", 1, 4);
            BuildItemTask(builder, "fab_tsk_808", "structure_0197", 1, 4);
            BuildItemTask(builder, "fab_tsk_809", "structure_0042", 1, 4);
            BuildItemTask(builder, "fab_tsk_810", "structure_0089", 1, 4);
            BuildItemTask(builder, "fab_tsk_811", "structure_0165", 1, 4);
            BuildItemTask(builder, "fab_tsk_812", "structure_0198", 1, 4);
            BuildItemTask(builder, "fab_tsk_813", "structure_5002", 1, 4);
            BuildItemTask(builder, "fab_tsk_814", "structure_0122", 1, 4);
            BuildItemTask(builder, "fab_tsk_815", "structure_0097", 1, 4);
            BuildItemTask(builder, "fab_tsk_816", "structure_0166", 1, 4);
            BuildItemTask(builder, "fab_tsk_817", "structure_0199", 1, 4);
            BuildItemTask(builder, "fab_tsk_818", "structure_0121", 1, 4);
            BuildItemTask(builder, "fab_tsk_819", "structure_0112", 1, 4);
            BuildItemTask(builder, "fab_tsk_820", "structure_0167", 1, 4);
            BuildItemTask(builder, "fab_tsk_821", "structure_0200", 1, 4);
            BuildItemTask(builder, "fab_tsk_822", "structure_5013", 1, 4);
            BuildItemTask(builder, "fab_tsk_823", "structure_0044", 1, 4);
            BuildItemTask(builder, "fab_tsk_824", "structure_0103", 1, 4);
            BuildItemTask(builder, "fab_tsk_825", "structure_0168", 1, 4);
            BuildItemTask(builder, "fab_tsk_826", "structure_5014", 1, 4);
            BuildItemTask(builder, "fab_tsk_827", "structure_0096", 1, 4);
            BuildItemTask(builder, "fab_tsk_828", "structure_0116", 1, 4);
            BuildItemTask(builder, "fab_tsk_829", "structure_0169", 1, 4);
            BuildItemTask(builder, "fab_tsk_830", "structure_5015", 1, 4);
            BuildItemTask(builder, "fab_tsk_831", "structure_0123", 1, 4);
            BuildItemTask(builder, "fab_tsk_832", "structure_0102", 1, 4);
            BuildItemTask(builder, "fab_tsk_833", "structure_0170", 1, 4);
            BuildItemTask(builder, "fab_tsk_834", "structure_5016", 1, 4);
            BuildItemTask(builder, "fab_tsk_835", "structure_0124", 1, 4);
            BuildItemTask(builder, "fab_tsk_836", "structure_0117", 1, 4);
            BuildItemTask(builder, "fab_tsk_837", "structure_0171", 1, 4);
            BuildItemTask(builder, "fab_tsk_838", "structure_0125", 1, 4);
            BuildItemTask(builder, "fab_tsk_839", "structure_0172", 1, 4);
            BuildItemTask(builder, "fab_tsk_840", "structure_0173", 1, 4);
            BuildItemTask(builder, "fab_tsk_841", "structure_5003", 1, 4);

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