using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class ArmorsmithGuildQuestDefinition : IQuestListDefinition
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
                .IsGuildTask(GuildType.ArmorsmithGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Engineering Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.ArmorsmithGuild, rewardDetails.GP);
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