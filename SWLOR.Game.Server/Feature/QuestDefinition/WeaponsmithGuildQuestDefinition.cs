using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class WeaponsmithGuildQuestDefinition : IQuestListDefinition
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
        private Dictionary<int, RewardDetails> _rewardDetails { get; set; }

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            _rewardDetails = new Dictionary<int, RewardDetails>
            {
                { 0, new RewardDetails(23, 7)},
                { 1, new RewardDetails(84, 27)},
                { 2, new RewardDetails(122, 39)},
                { 3, new RewardDetails(184, 52)},
                { 4, new RewardDetails(245, 65)},
                { 5, new RewardDetails(312, 82)},
            };
            var builder = new QuestBuilder();

			BasicBatonC(builder);
			BasicBatonM(builder);
			BasicBatonMS(builder);
			BasicFinesseVibrobladeD(builder);
			BasicFinesseVibrobladeK(builder);
			BasicFinesseVibrobladeR(builder);
			BasicFinesseVibrobladeSS(builder);
			BasicHeavyVibrobladeGA(builder);
			BasicHeavyVibrobladeGS(builder);
			BasicPolearmH(builder);
			BasicPolearmS(builder);
			BasicQuarterstaff(builder);
			BasicTwinVibrobladeDA(builder);
			BasicTwinVibrobladeTS(builder);
			BasicVibrobladeBA(builder);
			BasicVibrobladeBS(builder);
			BasicVibrobladeK(builder);
			BasicVibrobladeLS(builder);
			BatonC1(builder);
			BatonC2(builder);
			BatonC3(builder);
			BatonC4(builder);
			BatonM1(builder);
			BatonM2(builder);
			BatonM3(builder);
			BatonM4(builder);
			BatonMS1(builder);
			BatonMS2(builder);
			BatonMS3(builder);
			BatonMS4(builder);
			BatonRepairKitI(builder);
			BatonRepairKitII(builder);
			BatonRepairKitIII(builder);
			BatonRepairKitIV(builder);
			FinesseVibrobladeD1(builder);
			FinesseVibrobladeD2(builder);
			FinesseVibrobladeD3(builder);
			FinesseVibrobladeD4(builder);
			FinesseVibrobladeK1(builder);
			FinesseVibrobladeK2(builder);
			FinesseVibrobladeK3(builder);
			FinesseVibrobladeK4(builder);
			FinesseVibrobladeR1(builder);
			FinesseVibrobladeR2(builder);
			FinesseVibrobladeR3(builder);
			FinesseVibrobladeR4(builder);
			FinesseVibrobladeRepairKitI(builder);
			FinesseVibrobladeRepairKitII(builder);
			FinesseVibrobladeRepairKitIII(builder);
			FinesseVibrobladeRepairKitIV(builder);
			FinesseVibrobladeSS1(builder);
			FinesseVibrobladeSS2(builder);
			FinesseVibrobladeSS3(builder);
			FinesseVibrobladeSS4(builder);
			HeavyVibrobladeGA1(builder);
			HeavyVibrobladeGA2(builder);
			HeavyVibrobladeGA3(builder);
			HeavyVibrobladeGA4(builder);
			HeavyVibrobladeGS1(builder);
			HeavyVibrobladeGS2(builder);
			HeavyVibrobladeGS3(builder);
			HeavyVibrobladeGS4(builder);
			HeavyVibrobladeRepairKitI(builder);
			HeavyVibrobladeRepairKitII(builder);
			HeavyVibrobladeRepairKitIII(builder);
			HeavyVibrobladeRepairKitIV(builder);
			LargeBlade(builder);
			LargeHandle(builder);
			MartialArtsWeaponRepairKitI(builder);
			MartialArtsWeaponRepairKitII(builder);
			MartialArtsWeaponRepairKitIII(builder);
			MartialArtsWeaponRepairKitIV(builder);
			MediumBlade(builder);
			MediumHandle(builder);
			MetalBatonFrame(builder);
			PolearmH1(builder);
			PolearmH2(builder);
			PolearmH3(builder);
			PolearmH4(builder);
			PolearmRepairKitI(builder);
			PolearmRepairKitII(builder);
			PolearmRepairKitIII(builder);
			PolearmRepairKitIV(builder);
			PolearmS1(builder);
			PolearmS2(builder);
			PolearmS3(builder);
			PolearmS4(builder);
			QuarterstaffI(builder);
			QuarterstaffII(builder);
			QuarterstaffIII(builder);
			QuarterstaffIV(builder);
			Shaft(builder);
			SmallBlade(builder);
			SmallHandle(builder);
			TwinVibrobladeDA1(builder);
			TwinVibrobladeDA2(builder);
			TwinVibrobladeDA3(builder);
			TwinVibrobladeDA4(builder);
			TwinVibrobladeRepairKitI(builder);
			TwinVibrobladeRepairKitII(builder);
			TwinVibrobladeRepairKitIII(builder);
			TwinVibrobladeRepairKitIV(builder);
			TwinVibrobladeTS1(builder);
			TwinVibrobladeTS2(builder);
			TwinVibrobladeTS3(builder);
			TwinVibrobladeTS4(builder);
			VibrobladeBA1(builder);
			VibrobladeBA2(builder);
			VibrobladeBA3(builder);
			VibrobladeBA4(builder);
			VibrobladeBS1(builder);
			VibrobladeBS2(builder);
			VibrobladeBS3(builder);
			VibrobladeBS4(builder);
			VibrobladeK1(builder);
			VibrobladeK2(builder);
			VibrobladeK3(builder);
			VibrobladeK4(builder);
			VibrobladeLS1(builder);
			VibrobladeLS2(builder);
			VibrobladeLS3(builder);
			VibrobladeLS4(builder);
			VibrobladeRepairKitI(builder);
			VibrobladeRepairKitII(builder);
			VibrobladeRepairKitIII(builder);
			VibrobladeRepairKitIV(builder);
			WoodBatonFrame(builder);


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
                .IsGuildTask(GuildType.WeaponsmithGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Weaponsmith Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.WeaponsmithGuild, rewardDetails.GP);
        }

		private void BasicBatonC(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_222", "club_b", 1, 0);
		}

		private void BasicBatonM(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_223", "mace_b", 1, 0);
		}

		private void BasicBatonMS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_224", "morningstar_b", 1, 0);
		}

		private void BasicFinesseVibrobladeD(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_225", "dagger_b", 1, 0);
		}

		private void BasicFinesseVibrobladeK(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_226", "kukri_b", 1, 0);
		}

		private void BasicFinesseVibrobladeR(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_227", "rapier_b", 1, 0);
		}

		private void BasicFinesseVibrobladeSS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_228", "shortsword_b", 1, 0);
		}

		private void BasicHeavyVibrobladeGA(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_229", "greataxe_b", 1, 0);
		}

		private void BasicHeavyVibrobladeGS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_230", "greatsword_b", 1, 0);
		}

		private void BasicPolearmH(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_231", "halberd_b", 1, 0);
		}

		private void BasicPolearmS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_232", "spear_b", 1, 0);
		}

		private void BasicQuarterstaff(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_233", "quarterstaff_b", 1, 0);
		}

		private void BasicTwinVibrobladeDA(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_234", "doubleaxe_b", 1, 0);
		}

		private void BasicTwinVibrobladeTS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_235", "twinblade_b", 1, 0);
		}

		private void BasicVibrobladeBA(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_236", "battleaxe_b", 1, 0);
		}

		private void BasicVibrobladeBS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_237", "bst_sword_b", 1, 0);
		}

		private void BasicVibrobladeK(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_238", "katana_b", 1, 0);
		}

		private void BasicVibrobladeLS(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_239", "longsword_b", 1, 0);
		}

		private void BatonC1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_249", "club_1", 1, 1);
		}

		private void BatonC2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_274", "club_2", 1, 2);
		}

		private void BatonC3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_299", "club_3", 1, 3);
		}

		private void BatonC4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_324", "club_4", 1, 4);
		}

		private void BatonM1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_250", "mace_1", 1, 1);
		}

		private void BatonM2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_275", "mace_2", 1, 2);
		}

		private void BatonM3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_300", "mace_3", 1, 3);
		}

		private void BatonM4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_325", "mace_4", 1, 4);
		}

		private void BatonMS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_251", "morningstar_1", 1, 1);
		}

		private void BatonMS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_276", "morningstar_2", 1, 2);
		}

		private void BatonMS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_301", "morningstar_3", 1, 3);
		}

		private void BatonMS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_326", "morningstar_4", 1, 4);
		}

		private void BatonRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_252", "bt_rep_1", 1, 1);
		}

		private void BatonRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_277", "bt_rep_2", 1, 2);
		}

		private void BatonRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_302", "bt_rep_3", 1, 3);
		}

		private void BatonRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_327", "bt_rep_4", 1, 4);
		}

		private void FinesseVibrobladeD1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_253", "dagger_1", 1, 1);
		}

		private void FinesseVibrobladeD2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_278", "dagger_2", 1, 2);
		}

		private void FinesseVibrobladeD3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_303", "dagger_3", 1, 3);
		}

		private void FinesseVibrobladeD4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_328", "dagger_4", 1, 4);
		}

		private void FinesseVibrobladeK1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_254", "kukri_1", 1, 1);
		}

		private void FinesseVibrobladeK2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_279", "kukri_2", 1, 2);
		}

		private void FinesseVibrobladeK3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_304", "kukri_3", 1, 3);
		}

		private void FinesseVibrobladeK4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_329", "kukri_4", 1, 4);
		}

		private void FinesseVibrobladeR1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_255", "rapier_1", 1, 1);
		}

		private void FinesseVibrobladeR2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_280", "rapier_2", 1, 2);
		}

		private void FinesseVibrobladeR3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_305", "rapier_3", 1, 3);
		}

		private void FinesseVibrobladeR4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_330", "rapier_4", 1, 4);
		}

		private void FinesseVibrobladeRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_256", "fv_rep_1", 1, 1);
		}

		private void FinesseVibrobladeRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_281", "fv_rep_2", 1, 2);
		}

		private void FinesseVibrobladeRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_306", "fv_rep_3", 1, 3);
		}

		private void FinesseVibrobladeRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_331", "fv_rep_4", 1, 4);
		}

		private void FinesseVibrobladeSS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_257", "shortsword_1", 1, 1);
		}

		private void FinesseVibrobladeSS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_282", "shortsword_2", 1, 2);
		}

		private void FinesseVibrobladeSS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_307", "shortsword_3", 1, 3);
		}

		private void FinesseVibrobladeSS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_332", "shortsword_4", 1, 4);
		}

		private void HeavyVibrobladeGA1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_258", "greataxe_1", 1, 1);
		}

		private void HeavyVibrobladeGA2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_283", "greataxe_2", 1, 2);
		}

		private void HeavyVibrobladeGA3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_308", "greataxe_3", 1, 3);
		}

		private void HeavyVibrobladeGA4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_333", "greataxe_4", 1, 4);
		}

		private void HeavyVibrobladeGS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_259", "greatsword_1", 1, 1);
		}

		private void HeavyVibrobladeGS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_284", "greatsword_2", 1, 2);
		}

		private void HeavyVibrobladeGS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_309", "greatsword_3", 1, 3);
		}

		private void HeavyVibrobladeGS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_334", "greatsword_4", 1, 4);
		}

		private void HeavyVibrobladeRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_260", "hv_rep_1", 1, 1);
		}

		private void HeavyVibrobladeRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_285", "hv_rep_2", 1, 2);
		}

		private void HeavyVibrobladeRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_310", "hv_rep_3", 1, 3);
		}

		private void HeavyVibrobladeRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_335", "hv_rep_4", 1, 4);
		}

		private void LargeBlade(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_240", "large_blade", 1, 0);
		}

		private void LargeHandle(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_241", "large_handle", 1, 0);
		}

		private void MartialArtsWeaponRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_261", "ma_rep_1", 1, 1);
		}

		private void MartialArtsWeaponRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_286", "ma_rep_2", 1, 2);
		}

		private void MartialArtsWeaponRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_311", "ma_rep_3", 1, 3);
		}

		private void MartialArtsWeaponRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_336", "ma_rep_4", 1, 4);
		}

		private void MediumBlade(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_242", "medium_blade", 1, 0);
		}

		private void MediumHandle(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_243", "medium_handle", 1, 0);
		}

		private void MetalBatonFrame(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_244", "m_baton_frame", 1, 0);
		}

		private void PolearmH1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_262", "halberd_1", 1, 1);
		}

		private void PolearmH2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_287", "halberd_2", 1, 2);
		}

		private void PolearmH3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_312", "halberd_3", 1, 3);
		}

		private void PolearmH4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_337", "halberd_4", 1, 4);
		}

		private void PolearmRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_263", "po_rep_1", 1, 1);
		}

		private void PolearmRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_288", "po_rep_2", 1, 2);
		}

		private void PolearmRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_313", "po_rep_3", 1, 3);
		}

		private void PolearmRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_338", "po_rep_4", 1, 4);
		}

		private void PolearmS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_264", "spear_1", 1, 1);
		}

		private void PolearmS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_289", "spear_2", 1, 2);
		}

		private void PolearmS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_314", "spear_3", 1, 3);
		}

		private void PolearmS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_339", "spear_4", 1, 4);
		}

		private void QuarterstaffI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_265", "quarterstaff_1", 1, 1);
		}

		private void QuarterstaffII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_290", "quarterstaff_2", 1, 2);
		}

		private void QuarterstaffIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_315", "quarterstaff_3", 1, 3);
		}

		private void QuarterstaffIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_340", "quarterstaff_4", 1, 4);
		}

		private void Shaft(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_245", "shaft", 1, 0);
		}

		private void SmallBlade(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_246", "small_blade", 1, 0);
		}

		private void SmallHandle(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_247", "small_handle", 1, 0);
		}

		private void TwinVibrobladeDA1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_266", "doubleaxe_1", 1, 1);
		}

		private void TwinVibrobladeDA2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_291", "doubleaxe_2", 1, 2);
		}

		private void TwinVibrobladeDA3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_316", "doubleaxe_3", 1, 3);
		}

		private void TwinVibrobladeDA4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_341", "doubleaxe_4", 1, 4);
		}

		private void TwinVibrobladeRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_267", "tb_rep_1", 1, 1);
		}

		private void TwinVibrobladeRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_292", "tb_rep_2", 1, 2);
		}

		private void TwinVibrobladeRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_317", "tb_rep_3", 1, 3);
		}

		private void TwinVibrobladeRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_342", "tb_rep_4", 1, 4);
		}

		private void TwinVibrobladeTS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_268", "twinblade_1", 1, 1);
		}

		private void TwinVibrobladeTS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_293", "twinblade_2", 1, 2);
		}

		private void TwinVibrobladeTS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_318", "twinblade_3", 1, 3);
		}

		private void TwinVibrobladeTS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_343", "twinblade_4", 1, 4);
		}

		private void VibrobladeBA1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_269", "battleaxe_1", 1, 1);
		}

		private void VibrobladeBA2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_294", "battleaxe_2", 1, 2);
		}

		private void VibrobladeBA3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_319", "battleaxe_3", 1, 3);
		}

		private void VibrobladeBA4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_344", "battleaxe_4", 1, 4);
		}

		private void VibrobladeBS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_270", "bst_sword_1", 1, 1);
		}

		private void VibrobladeBS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_295", "bst_sword_2", 1, 2);
		}

		private void VibrobladeBS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_320", "bst_sword_3", 1, 3);
		}

		private void VibrobladeBS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_345", "bst_sword_4", 1, 4);
		}

		private void VibrobladeK1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_271", "katana_1", 1, 1);
		}

		private void VibrobladeK2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_296", "katana_2", 1, 2);
		}

		private void VibrobladeK3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_321", "katana_3", 1, 3);
		}

		private void VibrobladeK4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_346", "katana_4", 1, 4);
		}

		private void VibrobladeLS1(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_272", "longsword_1", 1, 1);
		}

		private void VibrobladeLS2(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_297", "longsword_2", 1, 2);
		}

		private void VibrobladeLS3(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_322", "longsword_3", 1, 3);
		}

		private void VibrobladeLS4(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_347", "longsword_4", 1, 4);
		}

		private void VibrobladeRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_273", "vb_rep_1", 1, 1);
		}

		private void VibrobladeRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_298", "vb_rep_2", 1, 2);
		}

		private void VibrobladeRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_323", "vb_rep_3", 1, 3);
		}

		private void VibrobladeRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_348", "vb_rep_4", 1, 4);
		}

		private void WoodBatonFrame(QuestBuilder builder)
		{
			BuildItemTask(builder, "wpn_tsk_248", "w_baton_frame", 1, 0);
		}


	}
}