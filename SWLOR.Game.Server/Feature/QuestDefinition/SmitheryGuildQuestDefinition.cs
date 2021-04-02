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

			// Weapons
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

			// Armor

			AdditionalFuelTankMedium(builder);
			AdditionalFuelTankSmall(builder);
			AdditionalStronidiumTankMedium(builder);
			AdditionalStronidiumTankSmall(builder);
			BasicBreastplate(builder);
			BasicForceBoots(builder);
			BasicForceHelmet(builder);
			BasicForceRobes(builder);
			BasicHeavyBoots(builder);
			BasicHeavyHelmet(builder);
			BasicLargeShield(builder);
			BasicLeatherTunic(builder);
			BasicLightBoots(builder);
			BasicLightHelmet(builder);
			BasicPowerGlove(builder);
			BasicSmallShield(builder);
			BasicTowerShield(builder);
			BreastplateI(builder);
			BreastplateII(builder);
			BreastplateIII(builder);
			BreastplateIV(builder);
			FiberplastPadding(builder);
			ForceArmorCore(builder);
			ForceArmorRepairKitI(builder);
			ForceArmorRepairKitII(builder);
			ForceArmorRepairKitIII(builder);
			ForceArmorRepairKitIV(builder);
			ForceArmorSegment(builder);
			ForceBeltI(builder);
			ForceBeltII(builder);
			ForceBeltIII(builder);
			ForceBootsI(builder);
			ForceBootsII(builder);
			ForceBootsIII(builder);
			ForceBootsIV(builder);
			ForceHelmetI(builder);
			ForceHelmetII(builder);
			ForceHelmetIII(builder);
			ForceHelmetIV(builder);
			ForceNecklaceI(builder);
			ForceNecklaceII(builder);
			ForceNecklaceIII(builder);
			ForceNecklaceIV(builder);
			ForceRobesI(builder);
			ForceRobesII(builder);
			ForceRobesIII(builder);
			ForceRobesIV(builder);
			HeavyArmorCore(builder);
			HeavyArmorRepairKitI(builder);
			HeavyArmorRepairKitII(builder);
			HeavyArmorRepairKitIII(builder);
			HeavyArmorRepairKitIV(builder);
			HeavyArmorSegment(builder);
			HeavyBeltI(builder);
			HeavyBeltII(builder);
			HeavyBeltIII(builder);
			HeavyBootsI(builder);
			HeavyBootsII(builder);
			HeavyBootsIII(builder);
			HeavyBootsIV(builder);
			HeavyCrestI(builder);
			HeavyCrestII(builder);
			HeavyCrestIII(builder);
			HeavyCrestIV(builder);
			HeavyHelmetI(builder);
			HeavyHelmetII(builder);
			HeavyHelmetIII(builder);
			HeavyHelmetIV(builder);
			HullPlating(builder);
			LargeShieldI(builder);
			LargeShieldII(builder);
			LargeShieldIII(builder);
			LargeShieldIV(builder);
			LeatherTunicI(builder);
			LeatherTunicII(builder);
			LeatherTunicIII(builder);
			LeatherTunicIV(builder);
			LightArmorCore(builder);
			LightArmorRepairKitI(builder);
			LightArmorRepairKitII(builder);
			LightArmorRepairKitIII(builder);
			LightArmorRepairKitIV(builder);
			LightArmorSegment(builder);
			LightBeltI(builder);
			LightBeltII(builder);
			LightBeltIII(builder);
			LightBootsI(builder);
			LightBootsII(builder);
			LightBootsIII(builder);
			LightBootsIV(builder);
			LightChokerI(builder);
			LightChokerII(builder);
			LightChokerIII(builder);
			LightChokerIV(builder);
			LightHelmetI(builder);
			LightHelmetII(builder);
			LightHelmetIII(builder);
			LightHelmetIV(builder);
			MetalReinforcement(builder);
			PowerGloveI(builder);
			PowerGloveII(builder);
			PowerGloveIII(builder);
			PowerGloveIV(builder);
			PrismaticForceBelt(builder);
			PrismaticHeavyBelt(builder);
			PrismaticLightBelt(builder);
			PrismForceNecklace(builder);
			PrismHeavyNecklace(builder);
			PrismLightNecklace(builder);
			ShieldRepairKitI(builder);
			ShieldRepairKitII(builder);
			ShieldRepairKitIII(builder);
			ShieldRepairKitIV(builder);
			SmallShieldI(builder);
			SmallShieldII(builder);
			SmallShieldIII(builder);
			SmallShieldIV(builder);
			TowerShieldI(builder);
			TowerShieldII(builder);
			TowerShieldIII(builder);
			TowerShieldIV(builder);


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


		private void AdditionalFuelTankMedium(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_193", "ssfuel2", 1, 4);
		}

		private void AdditionalFuelTankSmall(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_169", "ssfuel1", 1, 3);
		}

		private void AdditionalStronidiumTankMedium(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_194", "ssstron2", 1, 4);
		}

		private void AdditionalStronidiumTankSmall(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_170", "ssstron1", 1, 3);
		}

		private void BasicBreastplate(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_101", "breastplate_b", 1, 0);
		}

		private void BasicForceBoots(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_102", "force_boots_b", 1, 0);
		}

		private void BasicForceHelmet(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_103", "helmet_fb", 1, 0);
		}

		private void BasicForceRobes(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_104", "force_robe_b", 1, 0);
		}

		private void BasicHeavyBoots(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_105", "heavy_boots_b", 1, 0);
		}

		private void BasicHeavyHelmet(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_106", "helmet_hb", 1, 0);
		}

		private void BasicLargeShield(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_107", "large_shield_b", 1, 0);
		}

		private void BasicLeatherTunic(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_108", "leather_tunic_b", 1, 0);
		}

		private void BasicLightBoots(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_109", "light_boots_b", 1, 0);
		}

		private void BasicLightHelmet(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_110", "helmet_lb", 1, 0);
		}

		private void BasicPowerGlove(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_111", "powerglove_b", 1, 0);
		}

		private void BasicSmallShield(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_112", "small_shield_b", 1, 0);
		}

		private void BasicTowerShield(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_113", "tower_shield_b", 1, 0);
		}

		private void BreastplateI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_122", "breastplate_1", 1, 1);
		}

		private void BreastplateII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_145", "breastplate_2", 1, 2);
		}

		private void BreastplateIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_171", "breastplate_3", 1, 3);
		}

		private void BreastplateIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_195", "breastplate_4", 1, 4);
		}

		private void FiberplastPadding(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_114", "padding_fiber", 1, 0);
		}

		private void ForceArmorCore(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_115", "core_f_armor", 1, 0);
		}

		private void ForceArmorRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_123", "fa_rep_1", 1, 1);
		}

		private void ForceArmorRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_146", "fa_rep_2", 1, 2);
		}

		private void ForceArmorRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_172", "fa_rep_3", 1, 3);
		}

		private void ForceArmorRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_196", "fa_rep_4", 1, 4);
		}

		private void ForceArmorSegment(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_116", "f_armor_segment", 1, 0);
		}

		private void ForceBeltI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_124", "force_belt_1", 1, 1);
		}

		private void ForceBeltII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_147", "force_belt_2", 1, 2);
		}

		private void ForceBeltIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_173", "force_belt_3", 1, 3);
		}

		private void ForceBootsI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_125", "force_boots_1", 1, 1);
		}

		private void ForceBootsII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_148", "force_boots_2", 1, 2);
		}

		private void ForceBootsIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_174", "force_boots_3", 1, 3);
		}

		private void ForceBootsIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_197", "force_boots_4", 1, 4);
		}

		private void ForceHelmetI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_126", "helmet_f1", 1, 1);
		}

		private void ForceHelmetII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_149", "helmet_f2", 1, 2);
		}

		private void ForceHelmetIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_175", "helmet_f3", 1, 3);
		}

		private void ForceHelmetIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_198", "helmet_f4", 1, 4);
		}

		private void ForceNecklaceI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_127", "force_neck_1", 1, 1);
		}

		private void ForceNecklaceII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_150", "force_neck_2", 1, 2);
		}

		private void ForceNecklaceIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_176", "force_neck_3", 1, 3);
		}

		private void ForceNecklaceIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_199", "force_neck_4", 1, 4);
		}

		private void ForceRobesI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_128", "force_robe_1", 1, 1);
		}

		private void ForceRobesII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_151", "force_robe_2", 1, 2);
		}

		private void ForceRobesIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_177", "force_robe_3", 1, 3);
		}

		private void ForceRobesIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_200", "force_robe_4", 1, 4);
		}

		private void HeavyArmorCore(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_117", "core_h_armor", 1, 0);
		}

		private void HeavyArmorRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_129", "ha_rep_1", 1, 1);
		}

		private void HeavyArmorRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_152", "ha_rep_2", 1, 2);
		}

		private void HeavyArmorRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_178", "ha_rep_3", 1, 3);
		}

		private void HeavyArmorRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_201", "ha_rep_4", 1, 4);
		}

		private void HeavyArmorSegment(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_118", "h_armor_segment", 1, 0);
		}

		private void HeavyBeltI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_130", "heavy_belt_1", 1, 1);
		}

		private void HeavyBeltII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_153", "heavy_belt_2", 1, 2);
		}

		private void HeavyBeltIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_179", "heavy_belt_3", 1, 3);
		}

		private void HeavyBootsI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_131", "heavy_boots_1", 1, 1);
		}

		private void HeavyBootsII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_154", "heavy_boots_2", 1, 2);
		}

		private void HeavyBootsIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_180", "heavy_boots_3", 1, 3);
		}

		private void HeavyBootsIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_202", "heavy_boots_4", 1, 4);
		}

		private void HeavyCrestI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_132", "h_crest_1", 1, 1);
		}

		private void HeavyCrestII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_155", "h_crest_2", 1, 2);
		}

		private void HeavyCrestIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_181", "h_crest_3", 1, 3);
		}

		private void HeavyCrestIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_203", "h_crest_4", 1, 4);
		}

		private void HeavyHelmetI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_133", "helmet_h1", 1, 1);
		}

		private void HeavyHelmetII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_156", "helmet_h2", 1, 2);
		}

		private void HeavyHelmetIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_157", "helmet_h3", 1, 2);
		}

		private void HeavyHelmetIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_204", "helmet_h4", 1, 4);
		}

		private void HullPlating(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_205", "hull_plating", 1, 4);
		}

		private void LargeShieldI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_134", "large_shield_1", 1, 1);
		}

		private void LargeShieldII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_158", "large_shield_2", 1, 2);
		}

		private void LargeShieldIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_182", "large_shield_3", 1, 3);
		}

		private void LargeShieldIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_206", "large_shield_4", 1, 4);
		}

		private void LeatherTunicI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_135", "leather_tunic_1", 1, 1);
		}

		private void LeatherTunicII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_159", "leather_tunic_2", 1, 2);
		}

		private void LeatherTunicIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_183", "leather_tunic_3", 1, 3);
		}

		private void LeatherTunicIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_207", "leather_tunic_4", 1, 4);
		}

		private void LightArmorCore(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_119", "core_l_armor", 1, 0);
		}

		private void LightArmorRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_136", "la_rep_1", 1, 1);
		}

		private void LightArmorRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_160", "la_rep_2", 1, 2);
		}

		private void LightArmorRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_184", "la_rep_3", 1, 3);
		}

		private void LightArmorRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_208", "la_rep_4", 1, 4);
		}

		private void LightArmorSegment(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_120", "l_armor_segment", 1, 0);
		}

		private void LightBeltI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_137", "light_belt_1", 1, 1);
		}

		private void LightBeltII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_161", "light_belt_2", 1, 2);
		}

		private void LightBeltIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_185", "light_belt_3", 1, 3);
		}

		private void LightBootsI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_138", "light_boots_1", 1, 1);
		}

		private void LightBootsII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_162", "light_boots_2", 1, 2);
		}

		private void LightBootsIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_186", "light_boots_3", 1, 3);
		}

		private void LightBootsIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_209", "light_boots_4", 1, 4);
		}

		private void LightChokerI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_139", "lt_choker_1", 1, 1);
		}

		private void LightChokerII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_163", "lt_choker_2", 1, 2);
		}

		private void LightChokerIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_187", "lt_choker_3", 1, 3);
		}

		private void LightChokerIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_210", "lt_choker_4", 1, 4);
		}

		private void LightHelmetI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_140", "helmet_l1", 1, 1);
		}

		private void LightHelmetII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_164", "helmet_l2", 1, 2);
		}

		private void LightHelmetIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_188", "helmet_l3", 1, 3);
		}

		private void LightHelmetIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_211", "helmet_l4", 1, 4);
		}

		private void MetalReinforcement(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_121", "padding_metal", 1, 0);
		}

		private void PowerGloveI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_141", "powerglove_1", 1, 1);
		}

		private void PowerGloveII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_165", "powerglove_2", 1, 2);
		}

		private void PowerGloveIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_189", "powerglove_3", 1, 3);
		}

		private void PowerGloveIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_212", "powerglove_4", 1, 4);
		}

		private void PrismaticForceBelt(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_216", "prism_belt_f", 1, 4);
		}

		private void PrismaticHeavyBelt(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_217", "prism_belt_h", 1, 4);
		}

		private void PrismaticLightBelt(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_218", "prism_belt_l", 1, 4);
		}

		private void PrismForceNecklace(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_213", "prism_neck_f", 1, 4);
		}

		private void PrismHeavyNecklace(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_214", "prism_neck_h", 1, 4);
		}

		private void PrismLightNecklace(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_215", "prism_neck_l", 1, 4);
		}

		private void ShieldRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_142", "sh_rep_1", 1, 1);
		}

		private void ShieldRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_166", "sh_rep_2", 1, 2);
		}

		private void ShieldRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_190", "sh_rep_3", 1, 3);
		}

		private void ShieldRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_219", "sh_rep_4", 1, 4);
		}

		private void SmallShieldI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_143", "small_shield_1", 1, 1);
		}

		private void SmallShieldII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_167", "small_shield_2", 1, 2);
		}

		private void SmallShieldIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_191", "small_shield_3", 1, 3);
		}

		private void SmallShieldIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_220", "small_shield_4", 1, 4);
		}

		private void TowerShieldI(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_144", "tower_shield_1", 1, 1);
		}

		private void TowerShieldII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_168", "tower_shield_2", 1, 2);
		}

		private void TowerShieldIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_192", "tower_shield_3", 1, 3);
		}

		private void TowerShieldIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "arm_tsk_221", "tower_shield_4", 1, 4);
		}


	}
}