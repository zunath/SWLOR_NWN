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

			ActivationSpeedI(builder);
			ActivationSpeedII(builder);
			ActivationSpeedIII(builder);
			ArmorClassI(builder);
			ArmorClassII(builder);
			ArmorClassIII(builder);
			ArmorsmithI(builder);
			ArmorsmithII(builder);
			ArmorsmithIII(builder);
			AttackBonusI(builder);
			AttackBonusII(builder);
			AttackBonusIII(builder);
			AuxiliaryShieldGeneratorMedium(builder);
			AuxiliaryShieldGeneratorSmall(builder);
			AuxiliaryTargeterBasic(builder);
			AuxiliaryTargeterImproved(builder);
			AuxiliaryThrusterMedium(builder);
			AuxiliaryThrusterSmall(builder);
			BaseAttackBonusI(builder);
			BasicBlasterPistol(builder);
			BasicBlasterRifle(builder);
			BasicMineralScanner(builder);
			BasicResourceHarvester(builder);
			BasicResourceScanner(builder);
			BasicTrainingBlue(builder);
			BlasterPistolI(builder);
			BlasterPistolII(builder);
			BlasterPistolIII(builder);
			BlasterPistolIV(builder);
			BlasterPistolRepairKitI(builder);
			BlasterPistolRepairKitII(builder);
			BlasterPistolRepairKitIII(builder);
			BlasterPistolRepairKitIV(builder);
			BlasterRifleI(builder);
			BlasterRifleII(builder);
			BlasterRifleIII(builder);
			BlasterRifleIV(builder);
			BlasterRifleRepairKitI(builder);
			BlasterRifleRepairKitII(builder);
			BlasterRifleRepairKitIII(builder);
			BlasterRifleRepairKitIV(builder);
			BlueCrystalCluster(builder);
			CharismaI(builder);
			CharismaII(builder);
			CharismaIII(builder);
			CloakingGeneratorMedium(builder);
			CloakingGeneratorSmall(builder);
			ConstitutionI(builder);
			ConstitutionII(builder);
			ConstitutionIII(builder);
			CookingI(builder);
			CookingII(builder);
			CookingIII(builder);
			DamageI(builder);
			DamageII(builder);
			DamageIII(builder);
			DexterityI(builder);
			DexterityII(builder);
			DexterityIII(builder);
			DurabilityI(builder);
			DurabilityII(builder);
			DurabilityIII(builder);
			Emitter(builder);
			EngineeringI(builder);
			EngineeringII(builder);
			EngineeringIII(builder);
			EnhancementBonusI(builder);
			EnhancementBonusII(builder);
			EnhancementBonusIII(builder);
			FabricationI(builder);
			FabricationII(builder);
			FabricationIII(builder);
			FirstAidI(builder);
			FirstAidII(builder);
			FirstAidIII(builder);
			FPI(builder);
			FPII(builder);
			FPIII(builder);
			FPRegen(builder);
			FPRegenII(builder);
			FPRegenIII(builder);
			GreenCrystalCluster(builder);
			HarvestingI(builder);
			HarvestingII(builder);
			HarvestingIII(builder);
			HitPointsI(builder);
			HitPointsII(builder);
			HitPointsIII(builder);
			HPRegen(builder);
			HPRegenII(builder);
			HPRegenIII(builder);
			Hyperdrive(builder);
			ImprovedEnmity(builder);
			ImprovedEnmityII(builder);
			ImprovedEnmityIII(builder);
			IntelligenceI(builder);
			IntelligenceII(builder);
			IntelligenceIII(builder);
			LevelDecreaseI(builder);
			LevelIncreaseI(builder);
			LightsaberRepairKitI(builder);
			LightsaberRepairKitII(builder);
			LightsaberRepairKitIII(builder);
			LightsaberRepairKitIV(builder);
			LightStarshipBlaster(builder);
			LuckI(builder);
			LuckII(builder);
			MeditateI(builder);
			MeditateII(builder);
			PistolBarrel(builder);
			PowerCrystalCluster(builder);
			RangedWeaponCore(builder);
			RedCrystalCluster(builder);
			ReducedEnmityI(builder);
			ReducedEnmityII(builder);
			ReducedEnmityIII(builder);
			ResourceHarvesterI(builder);
			ResourceHarvesterII(builder);
			ResourceHarvesterIII(builder);
			ResourceHarvesterIV(builder);
			ResourceScannerI(builder);
			ResourceScannerII(builder);
			ResourceScannerIII(builder);
			ResourceScannerIV(builder);
			RifleBarrel(builder);
			SaberHilt(builder);
			SaberstaffRepairKitI(builder);
			SaberstaffRepairKitII(builder);
			SaberstaffRepairKitIII(builder);
			SaberstaffRepairKitIV(builder);
			ScanningArrayMedium(builder);
			ScanningArraySmall(builder);
			SneakAttackI(builder);
			SneakAttackII(builder);
			SneakAttackIII(builder);
			StarshipAuxiliaryBlaster(builder);
			StarshipAuxiliaryLightCannon(builder);
			StrengthI(builder);
			StrengthII(builder);
			StrengthIII(builder);
			WeaponsmithI(builder);
			WeaponsmithII(builder);
			WeaponsmithIII(builder);
			WisdomI(builder);
			WisdomII(builder);
			WisdomIII(builder);
			YellowCrystalCluster(builder);


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

		private void ActivationSpeedI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_363", "rune_cstspd1", 1, 1);
		}

		private void ActivationSpeedII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_411", "rune_cstspd2", 1, 2);
		}

		private void ActivationSpeedIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_498", "rune_cstspd3", 1, 4);
		}

		private void ArmorClassI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_364", "rune_ac1", 1, 1);
		}

		private void ArmorClassII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_448", "rune_ac2", 1, 3);
		}

		private void ArmorClassIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_499", "rune_ac3", 1, 4);
		}

		private void ArmorsmithI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_365", "rune_armsmth1", 1, 1);
		}

		private void ArmorsmithII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_449", "rune_armsmth2", 1, 3);
		}

		private void ArmorsmithIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_500", "rune_armsmth3", 1, 4);
		}

		private void AttackBonusI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_366", "rune_ab1", 1, 1);
		}

		private void AttackBonusII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_412", "rune_ab2", 1, 2);
		}

		private void AttackBonusIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_501", "rune_ab3", 1, 4);
		}

		private void AuxiliaryShieldGeneratorMedium(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_502", "ssshld2", 1, 4);
		}

		private void AuxiliaryShieldGeneratorSmall(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_450", "ssshld1", 1, 3);
		}

		private void AuxiliaryTargeterBasic(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_451", "ssrang1", 1, 3);
		}

		private void AuxiliaryTargeterImproved(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_503", "ssrang2", 1, 4);
		}

		private void AuxiliaryThrusterMedium(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_504", "ssspd2", 1, 4);
		}

		private void AuxiliaryThrusterSmall(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_452", "ssspd1", 1, 3);
		}

		private void BaseAttackBonusI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_413", "rune_bab1", 1, 2);
		}

		private void BasicBlasterPistol(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_349", "blaster_b", 1, 0);
		}

		private void BasicBlasterRifle(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_350", "rifle_b", 1, 0);
		}

		private void BasicMineralScanner(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_351", "scanner_m_b", 1, 0);
		}

		private void BasicResourceHarvester(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_352", "harvest_r_b", 1, 0);
		}

		private void BasicResourceScanner(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_353", "scanner_r_b", 1, 0);
		}

		private void BasicTrainingBlue(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_371", "saberstaff_b", 1, 1);
		}

		private void BlasterPistolI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_375", "blaster_1", 1, 1);
		}

		private void BlasterPistolII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_414", "blaster_2", 1, 2);
		}

		private void BlasterPistolIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_454", "blaster_3", 1, 3);
		}

		private void BlasterPistolIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_506", "blaster_4", 1, 4);
		}

		private void BlasterPistolRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_376", "bp_rep_1", 1, 1);
		}

		private void BlasterPistolRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_415", "bp_rep_2", 1, 2);
		}

		private void BlasterPistolRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_455", "bp_rep_3", 1, 3);
		}

		private void BlasterPistolRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_507", "bp_rep_4", 1, 4);
		}

		private void BlasterRifleI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_377", "rifle_1", 1, 1);
		}

		private void BlasterRifleII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_416", "rifle_2", 1, 2);
		}

		private void BlasterRifleIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_456", "rifle_3", 1, 3);
		}

		private void BlasterRifleIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_508", "rifle_4", 1, 4);
		}

		private void BlasterRifleRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_378", "br_rep_1", 1, 1);
		}

		private void BlasterRifleRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_417", "br_rep_2", 1, 2);
		}

		private void BlasterRifleRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_457", "br_rep_3", 1, 3);
		}

		private void BlasterRifleRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_509", "br_rep_4", 1, 4);
		}

		private void BlueCrystalCluster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_354", "c_cluster_blue", 1, 0);
		}

		private void CharismaI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_379", "rune_cha1", 1, 1);
		}

		private void CharismaII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_418", "rune_cha2", 1, 2);
		}

		private void CharismaIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_510", "rune_cha3", 1, 4);
		}

		private void CloakingGeneratorMedium(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_511", "ssstlth2", 1, 4);
		}

		private void CloakingGeneratorSmall(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_458", "ssstlth1", 1, 3);
		}

		private void ConstitutionI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_380", "rune_con1", 1, 1);
		}

		private void ConstitutionII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_419", "rune_con2", 1, 2);
		}

		private void ConstitutionIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_512", "rune_con3", 1, 4);
		}

		private void CookingI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_381", "rune_cooking1", 1, 1);
		}

		private void CookingII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_459", "rune_cooking2", 1, 3);
		}

		private void CookingIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_513", "rune_cooking3", 1, 4);
		}

		private void DamageI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_382", "rune_dmg1", 1, 1);
		}

		private void DamageII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_460", "rune_dmg2", 1, 3);
		}

		private void DamageIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_514", "rune_dmg3", 1, 4);
		}

		private void DexterityI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_385", "rune_dex1", 1, 1);
		}

		private void DexterityII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_420", "rune_dex2", 1, 2);
		}

		private void DexterityIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_517", "rune_dex3", 1, 4);
		}

		private void DurabilityI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_421", "rune_dur1", 1, 2);
		}

		private void DurabilityII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_463", "rune_dur2", 1, 3);
		}

		private void DurabilityIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_518", "rune_dur3", 1, 4);
		}

		private void Emitter(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_388", "emitter", 1, 1);
		}

		private void EngineeringI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_389", "rune_engin1", 1, 1);
		}

		private void EngineeringII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_466", "rune_engin2", 1, 3);
		}

		private void EngineeringIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_521", "rune_engin3", 1, 4);
		}

		private void EnhancementBonusI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_422", "rune_eb1", 1, 2);
		}

		private void EnhancementBonusII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_467", "rune_eb2", 1, 3);
		}

		private void EnhancementBonusIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_522", "rune_eb3", 1, 4);
		}

		private void FabricationI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_390", "rune_fab1", 1, 1);
		}

		private void FabricationII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_468", "rune_fab2", 1, 3);
		}

		private void FabricationIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_523", "rune_fab3", 1, 4);
		}

		private void FirstAidI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_391", "rune_faid1", 1, 1);
		}

		private void FirstAidII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_469", "rune_faid2", 1, 3);
		}

		private void FirstAidIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_524", "rune_faid3", 1, 4);
		}

		private void FPI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_392", "rune_mana1", 1, 1);
		}

		private void FPII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_470", "rune_mana2", 1, 3);
		}

		private void FPIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_525", "rune_mana3", 1, 4);
		}

		private void FPRegen(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_423", "rune_manareg1", 1, 2);
		}

		private void FPRegenII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_471", "rune_manareg2", 1, 3);
		}

		private void FPRegenIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_526", "rune_manareg3", 1, 4);
		}

		private void GreenCrystalCluster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_356", "c_cluster_green", 1, 0);
		}

		private void HarvestingI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_394", "rune_mining1", 1, 1);
		}

		private void HarvestingII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_473", "rune_mining2", 1, 3);
		}

		private void HarvestingIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_528", "rune_mining3", 1, 4);
		}

		private void HitPointsI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_395", "rune_hp1", 1, 1);
		}

		private void HitPointsII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_474", "rune_hp2", 1, 3);
		}

		private void HitPointsIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_529", "rune_hp3", 1, 4);
		}

		private void HPRegen(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_425", "rune_hpregen1", 1, 2);
		}

		private void HPRegenII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_475", "rune_hpregen2", 1, 3);
		}

		private void HPRegenIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_530", "rune_hpregen3", 1, 4);
		}

		private void Hyperdrive(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_531", "hyperdrive", 1, 4);
		}

		private void ImprovedEnmity(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_396", "rune_enmup1", 1, 1);
		}

		private void ImprovedEnmityII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_476", "rune_enmup2", 1, 3);
		}

		private void ImprovedEnmityIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_532", "rune_enmup3", 1, 4);
		}

		private void IntelligenceI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_397", "rune_int1", 1, 1);
		}

		private void IntelligenceII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_426", "rune_int2", 1, 2);
		}

		private void IntelligenceIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_533", "rune_int3", 1, 4);
		}

		private void LevelDecreaseI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_427", "rune_lvldown1", 1, 2);
		}

		private void LevelIncreaseI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_428", "rune_lvlup1", 1, 2);
		}

		private void LightsaberRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_400", "ls_rep_1", 1, 1);
		}

		private void LightsaberRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_433", "ls_rep_2", 1, 2);
		}

		private void LightsaberRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_483", "ls_rep_3", 1, 3);
		}

		private void LightsaberRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_541", "ls_rep_4", 1, 4);
		}

		private void LightStarshipBlaster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_536", "ship_blaster_1", 1, 4);
		}

		private void LuckI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_434", "rune_luck1", 1, 2);
		}

		private void LuckII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_435", "rune_luck2", 1, 2);
		}

		private void MeditateI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_436", "rune_med1", 1, 2);
		}

		private void MeditateII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_437", "rune_med2", 1, 2);
		}

		private void PistolBarrel(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_357", "pistol_barrel", 1, 0);
		}

		private void PowerCrystalCluster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_358", "c_cluster_power", 1, 0);
		}

		private void RangedWeaponCore(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_359", "r_weapon_core", 1, 0);
		}

		private void RedCrystalCluster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_360", "c_cluster_red", 1, 0);
		}

		private void ReducedEnmityI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_438", "rune_enmdown1", 1, 2);
		}

		private void ReducedEnmityII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_486", "rune_enmdown2", 1, 3);
		}

		private void ReducedEnmityIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_544", "rune_enmdown3", 1, 4);
		}

		private void ResourceHarvesterI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_403", "harvest_r_1", 1, 1);
		}

		private void ResourceHarvesterII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_439", "harvest_r_2", 1, 2);
		}

		private void ResourceHarvesterIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_487", "harvest_r_3", 1, 3);
		}

		private void ResourceHarvesterIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_545", "harvest_r_4", 1, 4);
		}

		private void ResourceScannerI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_404", "scanner_r_1", 1, 1);
		}

		private void ResourceScannerII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_440", "scanner_r_2", 1, 2);
		}

		private void ResourceScannerIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_488", "scanner_r_3", 1, 3);
		}

		private void ResourceScannerIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_546", "scanner_r_4", 1, 4);
		}

		private void RifleBarrel(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_361", "rifle_barrel", 1, 0);
		}

		private void SaberHilt(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_405", "ls_hilt", 1, 1);
		}

		private void SaberstaffRepairKitI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_406", "ss_rep_1", 1, 1);
		}

		private void SaberstaffRepairKitII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_445", "ss_rep_2", 1, 2);
		}

		private void SaberstaffRepairKitIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_493", "ss_rep_3", 1, 3);
		}

		private void SaberstaffRepairKitIV(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_551", "ss_rep_4", 1, 4);
		}

		private void ScanningArrayMedium(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_552", "ssscan2", 1, 4);
		}

		private void ScanningArraySmall(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_494", "ssscan1", 1, 3);
		}

		private void SneakAttackI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_407", "rune_snkatk1", 1, 1);
		}

		private void SneakAttackII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_495", "rune_snkatk2", 1, 3);
		}

		private void SneakAttackIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_553", "rune_snkatk3", 1, 4);
		}

		private void StarshipAuxiliaryBlaster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_496", "sswpn1", 1, 3);
		}

		private void StarshipAuxiliaryLightCannon(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_554", "sswpn2", 1, 4);
		}

		private void StrengthI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_408", "rune_str1", 1, 1);
		}

		private void StrengthII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_446", "rune_str2", 1, 2);
		}

		private void StrengthIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_555", "rune_str3", 1, 4);
		}

		private void WeaponsmithI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_409", "rune_wpnsmth1", 1, 1);
		}

		private void WeaponsmithII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_497", "rune_wpnsmth2", 1, 3);
		}

		private void WeaponsmithIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_556", "rune_wpnsmth3", 1, 4);
		}

		private void WisdomI(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_410", "rune_wis1", 1, 1);
		}

		private void WisdomII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_447", "rune_wis2", 1, 2);
		}

		private void WisdomIII(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_557", "rune_wis3", 1, 4);
		}

		private void YellowCrystalCluster(QuestBuilder builder)
		{
			BuildItemTask(builder, "eng_tsk_362", "c_cluster_yellow", 1, 0);
		}


	}
}