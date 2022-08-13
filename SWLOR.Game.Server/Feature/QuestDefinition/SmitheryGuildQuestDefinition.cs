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
            BuildItemTask(builder, "smth_tsk_200", "tit_greatsword", 1, 1);
            BuildItemTask(builder, "smth_tsk_201", "tit_spear", 1, 1);
            BuildItemTask(builder, "smth_tsk_202", "tit_knife", 1, 1);
            BuildItemTask(builder, "smth_tsk_203", "tit_pistol", 1, 1);
            BuildItemTask(builder, "smth_tsk_204", "tit_staff", 1, 1);
            BuildItemTask(builder, "smth_tsk_205", "tit_longsword", 1, 1);
            BuildItemTask(builder, "smth_tsk_206", "tit_katar", 1, 1);
            BuildItemTask(builder, "smth_tsk_207", "tit_shuriken", 1, 1);
            BuildItemTask(builder, "smth_tsk_208", "tit_rifle", 1, 1);
            BuildItemTask(builder, "smth_tsk_209", "tit_twinblade", 1, 1);
            BuildItemTask(builder, "smth_tsk_210", "tit_shield", 1, 1);
            BuildItemTask(builder, "smth_tsk_211", "tit_cloak", 1, 1);
            BuildItemTask(builder, "smth_tsk_212", "tit_belt", 1, 1);
            BuildItemTask(builder, "smth_tsk_213", "tit_ring", 1, 1);
            BuildItemTask(builder, "smth_tsk_214", "tit_necklace", 1, 1);
            BuildItemTask(builder, "smth_tsk_215", "tit_armor", 1, 1);
            BuildItemTask(builder, "smth_tsk_216", "tit_helmet", 1, 1);
            BuildItemTask(builder, "smth_tsk_217", "tit_bracer", 1, 1);
            BuildItemTask(builder, "smth_tsk_218", "tit_leggings", 1, 1);
            BuildItemTask(builder, "smth_tsk_219", "viv_cloak", 1, 1);
            BuildItemTask(builder, "smth_tsk_220", "viv_belt", 1, 1);
            BuildItemTask(builder, "smth_tsk_221", "viv_ring", 1, 1);
            BuildItemTask(builder, "smth_tsk_222", "viv_necklace", 1, 1);
            BuildItemTask(builder, "smth_tsk_223", "viv_tunic", 1, 1);
            BuildItemTask(builder, "smth_tsk_224", "viv_cap", 1, 1);
            BuildItemTask(builder, "smth_tsk_225", "viv_gloves", 1, 1);
            BuildItemTask(builder, "smth_tsk_226", "viv_boots", 1, 1);
            BuildItemTask(builder, "smth_tsk_227", "val_cloak", 1, 1);
            BuildItemTask(builder, "smth_tsk_228", "val_belt", 1, 1);
            BuildItemTask(builder, "smth_tsk_229", "val_ring", 1, 1);
            BuildItemTask(builder, "smth_tsk_230", "val_necklace", 1, 1);
            BuildItemTask(builder, "smth_tsk_231", "val_tunic", 1, 1);
            BuildItemTask(builder, "smth_tsk_232", "val_cap", 1, 1);
            BuildItemTask(builder, "smth_tsk_233", "val_gloves", 1, 1);
            BuildItemTask(builder, "smth_tsk_234", "val_boots", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "smth_tsk_400", "del_greatsword", 1, 2);
            BuildItemTask(builder, "smth_tsk_401", "del_spear", 1, 2);
            BuildItemTask(builder, "smth_tsk_402", "del_knife", 1, 2);
            BuildItemTask(builder, "smth_tsk_403", "del_pistol", 1, 2);
            BuildItemTask(builder, "smth_tsk_404", "del_staff", 1, 2);
            BuildItemTask(builder, "smth_tsk_405", "del_longsword", 1, 2);
            BuildItemTask(builder, "smth_tsk_406", "del_katar", 1, 2);
            BuildItemTask(builder, "smth_tsk_407", "del_shuriken", 1, 2);
            BuildItemTask(builder, "smth_tsk_408", "del_rifle", 1, 2);
            BuildItemTask(builder, "smth_tsk_409", "del_twinblade", 1, 2);
            BuildItemTask(builder, "smth_tsk_410", "qk_shield", 1, 2);
            BuildItemTask(builder, "smth_tsk_411", "qk_cloak", 1, 2);
            BuildItemTask(builder, "smth_tsk_412", "qk_belt", 1, 2);
            BuildItemTask(builder, "smth_tsk_413", "qk_ring", 1, 2);
            BuildItemTask(builder, "smth_tsk_414", "qk_necklace", 1, 2);
            BuildItemTask(builder, "smth_tsk_415", "qk_armor", 1, 2);
            BuildItemTask(builder, "smth_tsk_416", "qk_helmet", 1, 2);
            BuildItemTask(builder, "smth_tsk_417", "qk_bracer", 1, 2);
            BuildItemTask(builder, "smth_tsk_418", "qk_leggings", 1, 2);
            BuildItemTask(builder, "smth_tsk_419", "reg_cloak", 1, 2);
            BuildItemTask(builder, "smth_tsk_420", "reg_belt", 1, 2);
            BuildItemTask(builder, "smth_tsk_421", "reg_ring", 1, 2);
            BuildItemTask(builder, "smth_tsk_422", "reg_necklace", 1, 2);
            BuildItemTask(builder, "smth_tsk_423", "reg_tunic", 1, 2);
            BuildItemTask(builder, "smth_tsk_424", "reg_cap", 1, 2);
            BuildItemTask(builder, "smth_tsk_425", "reg_gloves", 1, 2);
            BuildItemTask(builder, "smth_tsk_426", "reg_boots", 1, 2);
            BuildItemTask(builder, "smth_tsk_427", "for_cloak", 1, 2);
            BuildItemTask(builder, "smth_tsk_428", "for_belt", 1, 2);
            BuildItemTask(builder, "smth_tsk_429", "for_ring", 1, 2);
            BuildItemTask(builder, "smth_tsk_430", "for_necklace", 1, 2);
            BuildItemTask(builder, "smth_tsk_431", "for_tunic", 1, 2);
            BuildItemTask(builder, "smth_tsk_432", "for_cap", 1, 2);
            BuildItemTask(builder, "smth_tsk_433", "for_gloves", 1, 2);
            BuildItemTask(builder, "smth_tsk_434", "for_boots", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "smth_tsk_600", "proto_greatsword", 1, 3);
            BuildItemTask(builder, "smth_tsk_601", "proto_spear", 1, 3);
            BuildItemTask(builder, "smth_tsk_602", "proto_knife", 1, 3);
            BuildItemTask(builder, "smth_tsk_603", "proto_pistol", 1, 3);
            BuildItemTask(builder, "smth_tsk_604", "proto_staff", 1, 3);
            BuildItemTask(builder, "smth_tsk_605", "pro_longsword", 1, 3);
            BuildItemTask(builder, "smth_tsk_606", "proto_katar", 1, 3);
            BuildItemTask(builder, "smth_tsk_607", "proto_shuriken", 1, 3);
            BuildItemTask(builder, "smth_tsk_608", "proto_rifle", 1, 3);
            BuildItemTask(builder, "smth_tsk_609", "proto_twinblade", 1, 3);
            BuildItemTask(builder, "smth_tsk_610", "ar_shield", 1, 3);
            BuildItemTask(builder, "smth_tsk_611", "ar_cloak", 1, 3);
            BuildItemTask(builder, "smth_tsk_612", "ar_belt", 1, 3);
            BuildItemTask(builder, "smth_tsk_613", "ar_ring", 1, 3);
            BuildItemTask(builder, "smth_tsk_614", "ar_necklace", 1, 3);
            BuildItemTask(builder, "smth_tsk_615", "ar_armor", 1, 3);
            BuildItemTask(builder, "smth_tsk_616", "ar_helmet", 1, 3);
            BuildItemTask(builder, "smth_tsk_617", "ar_bracer", 1, 3);
            BuildItemTask(builder, "smth_tsk_618", "ar_leggings", 1, 3);
            BuildItemTask(builder, "smth_tsk_619", "gre_cloak", 1, 3);
            BuildItemTask(builder, "smth_tsk_620", "gre_belt", 1, 3);
            BuildItemTask(builder, "smth_tsk_621", "gr_ring", 1, 3);
            BuildItemTask(builder, "smth_tsk_622", "gr_necklace", 1, 3);
            BuildItemTask(builder, "smth_tsk_623", "gr_tunic", 1, 3);
            BuildItemTask(builder, "smth_tsk_624", "gr_cap", 1, 3);
            BuildItemTask(builder, "smth_tsk_625", "gr_gloves", 1, 3);
            BuildItemTask(builder, "smth_tsk_626", "gr_boots", 1, 3);
            BuildItemTask(builder, "smth_tsk_627", "sur_cloak", 1, 3);
            BuildItemTask(builder, "smth_tsk_628", "sur_belt", 1, 3);
            BuildItemTask(builder, "smth_tsk_629", "sur_ring", 1, 3);
            BuildItemTask(builder, "smth_tsk_630", "sur_necklace", 1, 3);
            BuildItemTask(builder, "smth_tsk_631", "sur_tunic", 1, 3);
            BuildItemTask(builder, "smth_tsk_632", "sur_cap", 1, 3);
            BuildItemTask(builder, "smth_tsk_633", "sur_gloves", 1, 3);
            BuildItemTask(builder, "smth_tsk_634", "sur_boots", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "smth_tsk_800", "oph_greatsword", 1, 4);
            BuildItemTask(builder, "smth_tsk_801", "oph_spear", 1, 4);
            BuildItemTask(builder, "smth_tsk_802", "oph_knife", 1, 4);
            BuildItemTask(builder, "smth_tsk_803", "oph_pistol", 1, 4);
            BuildItemTask(builder, "smth_tsk_804", "oph_staff", 1, 4);
            BuildItemTask(builder, "smth_tsk_805", "oph_longsword", 1, 4);
            BuildItemTask(builder, "smth_tsk_806", "oph_katar", 1, 4);
            BuildItemTask(builder, "smth_tsk_807", "oph_shuriken", 1, 4);
            BuildItemTask(builder, "smth_tsk_808", "oph_rifle", 1, 4);
            BuildItemTask(builder, "smth_tsk_809", "oph_twinblade", 1, 4);
            BuildItemTask(builder, "smth_tsk_810", "ec_shield", 1, 4);
            BuildItemTask(builder, "smth_tsk_811", "ec_cloak", 1, 4);
            BuildItemTask(builder, "smth_tsk_812", "ec_belt", 1, 4);
            BuildItemTask(builder, "smth_tsk_813", "ec_ring", 1, 4);
            BuildItemTask(builder, "smth_tsk_814", "ec_necklace", 1, 4);
            BuildItemTask(builder, "smth_tsk_815", "ec_armor", 1, 4);
            BuildItemTask(builder, "smth_tsk_816", "ec_helmet", 1, 4);
            BuildItemTask(builder, "smth_tsk_817", "ec_bracer", 1, 4);
            BuildItemTask(builder, "smth_tsk_818", "ec_leggings", 1, 4);
            BuildItemTask(builder, "smth_tsk_819", "tran_cloak", 1, 4);
            BuildItemTask(builder, "smth_tsk_820", "tran_belt", 1, 4);
            BuildItemTask(builder, "smth_tsk_821", "tran_ring", 1, 4);
            BuildItemTask(builder, "smth_tsk_822", "tran_necklace", 1, 4);
            BuildItemTask(builder, "smth_tsk_823", "tran_tunic", 1, 4);
            BuildItemTask(builder, "smth_tsk_824", "tran_cap", 1, 4);
            BuildItemTask(builder, "smth_tsk_825", "tran_gloves", 1, 4);
            BuildItemTask(builder, "smth_tsk_826", "tran_boots", 1, 4);
            BuildItemTask(builder, "smth_tsk_827", "sup_cloak", 1, 4);
            BuildItemTask(builder, "smth_tsk_828", "sup_belt", 1, 4);
            BuildItemTask(builder, "smth_tsk_829", "sup_ring", 1, 4);
            BuildItemTask(builder, "smth_tsk_830", "sup_necklace", 1, 4);
            BuildItemTask(builder, "smth_tsk_831", "sup_tunic", 1, 4);
            BuildItemTask(builder, "smth_tsk_832", "sup_cap", 1, 4);
            BuildItemTask(builder, "smth_tsk_833", "sup_gloves", 1, 4);
            BuildItemTask(builder, "smth_tsk_834", "sup_boots", 1, 4);

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