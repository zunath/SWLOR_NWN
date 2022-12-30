using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidHeadRecipes: IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            // Droid Head MPV I
            _builder.Create(RecipeType.DroidHeadMPV1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpv1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MPW I
            _builder.Create(RecipeType.DroidHeadMPW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpw1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MPA I
            _builder.Create(RecipeType.DroidHeadMPA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpa1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MPS I
            _builder.Create(RecipeType.DroidHeadMPS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mps1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MVW I
            _builder.Create(RecipeType.DroidHeadMVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvw1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MVA I
            _builder.Create(RecipeType.DroidHeadMVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mva1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MVS I
            _builder.Create(RecipeType.DroidHeadMVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvs1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MWA I
            _builder.Create(RecipeType.DroidHeadMWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mwa1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MWS I
            _builder.Create(RecipeType.DroidHeadMWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mws1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head MAS I
            _builder.Create(RecipeType.DroidHeadMAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mas1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head PVW I
            _builder.Create(RecipeType.DroidHeadPVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvw1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head PVA I
            _builder.Create(RecipeType.DroidHeadPVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pva1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head PVS I
            _builder.Create(RecipeType.DroidHeadPVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvs1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head VWA I
            _builder.Create(RecipeType.DroidHeadVWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vwa1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head VWS I
            _builder.Create(RecipeType.DroidHeadVWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vws1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head VAS I
            _builder.Create(RecipeType.DroidHeadVAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vas1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);

            // Droid Head WAS I
            _builder.Create(RecipeType.DroidHeadWAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_was1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("d_brain1", 1)
                .Component("d_sensor1", 2);


        }

        private void Tier2()
        {
            // Droid Head MPV II
            _builder.Create(RecipeType.DroidHeadMPV2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpv2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MPW II
            _builder.Create(RecipeType.DroidHeadMPW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpw2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MPA II
            _builder.Create(RecipeType.DroidHeadMPA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpa2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MPS II
            _builder.Create(RecipeType.DroidHeadMPS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mps2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MVW II
            _builder.Create(RecipeType.DroidHeadMVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvw2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MVA II
            _builder.Create(RecipeType.DroidHeadMVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mva2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MVS II
            _builder.Create(RecipeType.DroidHeadMVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvs2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MWA II
            _builder.Create(RecipeType.DroidHeadMWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mwa2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MWS II
            _builder.Create(RecipeType.DroidHeadMWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mws2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head MAS II
            _builder.Create(RecipeType.DroidHeadMAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mas2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head PVW II
            _builder.Create(RecipeType.DroidHeadPVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvw2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head PVA II
            _builder.Create(RecipeType.DroidHeadPVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pva2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head PVS II
            _builder.Create(RecipeType.DroidHeadPVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvs2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head VWA II
            _builder.Create(RecipeType.DroidHeadVWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vwa2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head VWS II
            _builder.Create(RecipeType.DroidHeadVWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vws2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head VAS II
            _builder.Create(RecipeType.DroidHeadVAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vas2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);

            // Droid Head WAS II
            _builder.Create(RecipeType.DroidHeadWAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_was2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("d_brain2", 1)
                .Component("d_sensor2", 2);


        }
        private void Tier3()
        {
            // Droid Head MPV III
            _builder.Create(RecipeType.DroidHeadMPV3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpv3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MPW III
            _builder.Create(RecipeType.DroidHeadMPW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpw3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MPA III
            _builder.Create(RecipeType.DroidHeadMPA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpa3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MPS III
            _builder.Create(RecipeType.DroidHeadMPS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mps3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MVW III
            _builder.Create(RecipeType.DroidHeadMVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvw3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MVA III
            _builder.Create(RecipeType.DroidHeadMVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mva3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MVS III
            _builder.Create(RecipeType.DroidHeadMVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvs3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MWA III
            _builder.Create(RecipeType.DroidHeadMWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mwa3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MWS III
            _builder.Create(RecipeType.DroidHeadMWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mws3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head MAS III
            _builder.Create(RecipeType.DroidHeadMAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mas3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head PVW III
            _builder.Create(RecipeType.DroidHeadPVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvw3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head PVA III
            _builder.Create(RecipeType.DroidHeadPVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pva3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head PVS III
            _builder.Create(RecipeType.DroidHeadPVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvs3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head VWA III
            _builder.Create(RecipeType.DroidHeadVWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vwa3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head VWS III
            _builder.Create(RecipeType.DroidHeadVWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vws3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head VAS III
            _builder.Create(RecipeType.DroidHeadVAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vas3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);

            // Droid Head WAS III
            _builder.Create(RecipeType.DroidHeadWAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_was3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain3", 1)
                .Component("d_sensor3", 2);
        }
        private void Tier4()
        {
            // Droid Head MPV IV
            _builder.Create(RecipeType.DroidHeadMPV4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpv4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MPW IV
            _builder.Create(RecipeType.DroidHeadMPW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpw4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MPA IV
            _builder.Create(RecipeType.DroidHeadMPA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpa4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MPS IV
            _builder.Create(RecipeType.DroidHeadMPS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mps4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MVW IV
            _builder.Create(RecipeType.DroidHeadMVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvw4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MVA IV
            _builder.Create(RecipeType.DroidHeadMVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mva4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MVS IV
            _builder.Create(RecipeType.DroidHeadMVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvs4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MWA IV
            _builder.Create(RecipeType.DroidHeadMWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mwa4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MWS IV
            _builder.Create(RecipeType.DroidHeadMWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mws4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head MAS IV
            _builder.Create(RecipeType.DroidHeadMAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mas4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head PVW IV
            _builder.Create(RecipeType.DroidHeadPVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvw4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head PVA IV
            _builder.Create(RecipeType.DroidHeadPVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pva4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head PVS IV
            _builder.Create(RecipeType.DroidHeadPVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvs4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head VWA IV
            _builder.Create(RecipeType.DroidHeadVWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vwa4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head VWS IV
            _builder.Create(RecipeType.DroidHeadVWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vws4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head VAS IV
            _builder.Create(RecipeType.DroidHeadVAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vas4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);

            // Droid Head WAS IV
            _builder.Create(RecipeType.DroidHeadWAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_was4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain4", 1)
                .Component("d_sensor4", 2);


        }
        private void Tier5()
        {
            // Droid Head MPV V
            _builder.Create(RecipeType.DroidHeadMPV5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpv5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MPW V
            _builder.Create(RecipeType.DroidHeadMPW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpw5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MPA V
            _builder.Create(RecipeType.DroidHeadMPA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mpa5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MPS V
            _builder.Create(RecipeType.DroidHeadMPS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mps5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MVW V
            _builder.Create(RecipeType.DroidHeadMVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvw5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MVA V
            _builder.Create(RecipeType.DroidHeadMVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mva5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MVS V
            _builder.Create(RecipeType.DroidHeadMVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mvs5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MWA V
            _builder.Create(RecipeType.DroidHeadMWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mwa5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MWS V
            _builder.Create(RecipeType.DroidHeadMWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mws5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head MAS V
            _builder.Create(RecipeType.DroidHeadMAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_mas5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head PVW V
            _builder.Create(RecipeType.DroidHeadPVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvw5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head PVA V
            _builder.Create(RecipeType.DroidHeadPVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pva5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head PVS V
            _builder.Create(RecipeType.DroidHeadPVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_pvs5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head VWA V
            _builder.Create(RecipeType.DroidHeadVWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vwa5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head VWS V
            _builder.Create(RecipeType.DroidHeadVWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vws5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head VAS V
            _builder.Create(RecipeType.DroidHeadVAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_vas5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);

            // Droid Head WAS V
            _builder.Create(RecipeType.DroidHeadWAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidHead)
                .Resref("d_hd_was5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("d_brain5", 1)
                .Component("d_sensor5", 2);


        }
    }
}
