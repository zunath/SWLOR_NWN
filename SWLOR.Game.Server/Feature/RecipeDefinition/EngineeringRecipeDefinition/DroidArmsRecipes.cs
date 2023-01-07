using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidArmsRecipes: IRecipeListDefinition
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
            // Droid Arms MPV I
            _builder.Create(RecipeType.DroidArmsMPV1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpv1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MPW I
            _builder.Create(RecipeType.DroidArmsMPW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpw1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MPA I
            _builder.Create(RecipeType.DroidArmsMPA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpa1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MPS I
            _builder.Create(RecipeType.DroidArmsMPS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mps1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MVW I
            _builder.Create(RecipeType.DroidArmsMVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvw1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MVA I
            _builder.Create(RecipeType.DroidArmsMVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mva1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MVS I
            _builder.Create(RecipeType.DroidArmsMVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvs1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MWA I
            _builder.Create(RecipeType.DroidArmsMWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mwa1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MWS I
            _builder.Create(RecipeType.DroidArmsMWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mws1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms MAS I
            _builder.Create(RecipeType.DroidArmsMAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mas1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms PVW I
            _builder.Create(RecipeType.DroidArmsPVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvw1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms PVA I
            _builder.Create(RecipeType.DroidArmsPVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pva1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms PVS I
            _builder.Create(RecipeType.DroidArmsPVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvs1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms VWA I
            _builder.Create(RecipeType.DroidArmsVWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vwa1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms VWS I
            _builder.Create(RecipeType.DroidArmsVWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vws1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms VAS I
            _builder.Create(RecipeType.DroidArmsVAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vas1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);

            // Droid Arms WAS I
            _builder.Create(RecipeType.DroidArmsWAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_was1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("manip_arm1", 2)
                .Component("discharge_unit1", 1);


        }

        private void Tier2()
        {
            // Droid Arms MPV II
            _builder.Create(RecipeType.DroidArmsMPV2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpv2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MPW II
            _builder.Create(RecipeType.DroidArmsMPW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpw2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MPA II
            _builder.Create(RecipeType.DroidArmsMPA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpa2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MPS II
            _builder.Create(RecipeType.DroidArmsMPS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mps2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MVW II
            _builder.Create(RecipeType.DroidArmsMVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvw2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MVA II
            _builder.Create(RecipeType.DroidArmsMVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mva2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MVS II
            _builder.Create(RecipeType.DroidArmsMVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvs2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MWA II
            _builder.Create(RecipeType.DroidArmsMWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mwa2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MWS II
            _builder.Create(RecipeType.DroidArmsMWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mws2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms MAS II
            _builder.Create(RecipeType.DroidArmsMAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mas2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms PVW II
            _builder.Create(RecipeType.DroidArmsPVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvw2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms PVA II
            _builder.Create(RecipeType.DroidArmsPVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pva2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms PVS II
            _builder.Create(RecipeType.DroidArmsPVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvs2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms VWA II
            _builder.Create(RecipeType.DroidArmsVWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vwa2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms VWS II
            _builder.Create(RecipeType.DroidArmsVWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vws2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms VAS II
            _builder.Create(RecipeType.DroidArmsVAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vas2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);

            // Droid Arms WAS II
            _builder.Create(RecipeType.DroidArmsWAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_was2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("manip_arm2", 2)
                .Component("discharge_unit2", 1);


        }
        private void Tier3()
        {
            // Droid Arms MPV III
            _builder.Create(RecipeType.DroidArmsMPV3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpv3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MPW III
            _builder.Create(RecipeType.DroidArmsMPW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpw3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MPA III
            _builder.Create(RecipeType.DroidArmsMPA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpa3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MPS III
            _builder.Create(RecipeType.DroidArmsMPS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mps3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MVW III
            _builder.Create(RecipeType.DroidArmsMVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvw3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MVA III
            _builder.Create(RecipeType.DroidArmsMVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mva3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MVS III
            _builder.Create(RecipeType.DroidArmsMVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvs3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MWA III
            _builder.Create(RecipeType.DroidArmsMWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mwa3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MWS III
            _builder.Create(RecipeType.DroidArmsMWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mws3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms MAS III
            _builder.Create(RecipeType.DroidArmsMAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mas3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms PVW III
            _builder.Create(RecipeType.DroidArmsPVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvw3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms PVA III
            _builder.Create(RecipeType.DroidArmsPVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pva3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms PVS III
            _builder.Create(RecipeType.DroidArmsPVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvs3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms VWA III
            _builder.Create(RecipeType.DroidArmsVWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vwa3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms VWS III
            _builder.Create(RecipeType.DroidArmsVWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vws3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms VAS III
            _builder.Create(RecipeType.DroidArmsVAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vas3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);

            // Droid Arms WAS III
            _builder.Create(RecipeType.DroidArmsWAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_was3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm3", 2)
                .Component("discharge_unit3", 1);


        }
        private void Tier4()
        {
            // Droid Arms MPV IV
            _builder.Create(RecipeType.DroidArmsMPV4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpv4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MPW IV
            _builder.Create(RecipeType.DroidArmsMPW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpw4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MPA IV
            _builder.Create(RecipeType.DroidArmsMPA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpa4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MPS IV
            _builder.Create(RecipeType.DroidArmsMPS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mps4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MVW IV
            _builder.Create(RecipeType.DroidArmsMVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvw4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MVA IV
            _builder.Create(RecipeType.DroidArmsMVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mva4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MVS IV
            _builder.Create(RecipeType.DroidArmsMVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvs4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MWA IV
            _builder.Create(RecipeType.DroidArmsMWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mwa4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MWS IV
            _builder.Create(RecipeType.DroidArmsMWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mws4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms MAS IV
            _builder.Create(RecipeType.DroidArmsMAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mas4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms PVW IV
            _builder.Create(RecipeType.DroidArmsPVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvw4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms PVA IV
            _builder.Create(RecipeType.DroidArmsPVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pva4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms PVS IV
            _builder.Create(RecipeType.DroidArmsPVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvs4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms VWA IV
            _builder.Create(RecipeType.DroidArmsVWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vwa4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms VWS IV
            _builder.Create(RecipeType.DroidArmsVWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vws4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms VAS IV
            _builder.Create(RecipeType.DroidArmsVAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vas4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);

            // Droid Arms WAS IV
            _builder.Create(RecipeType.DroidArmsWAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_was4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm4", 2)
                .Component("discharge_unit4", 1);


        }
        private void Tier5()
        {
            // Droid Arms MPV V
            _builder.Create(RecipeType.DroidArmsMPV5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpv5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MPW V
            _builder.Create(RecipeType.DroidArmsMPW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpw5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MPA V
            _builder.Create(RecipeType.DroidArmsMPA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mpa5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MPS V
            _builder.Create(RecipeType.DroidArmsMPS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mps5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MVW V
            _builder.Create(RecipeType.DroidArmsMVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvw5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MVA V
            _builder.Create(RecipeType.DroidArmsMVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mva5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MVS V
            _builder.Create(RecipeType.DroidArmsMVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mvs5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MWA V
            _builder.Create(RecipeType.DroidArmsMWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mwa5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MWS V
            _builder.Create(RecipeType.DroidArmsMWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mws5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms MAS V
            _builder.Create(RecipeType.DroidArmsMAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_mas5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms PVW V
            _builder.Create(RecipeType.DroidArmsPVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvw5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms PVA V
            _builder.Create(RecipeType.DroidArmsPVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pva5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms PVS V
            _builder.Create(RecipeType.DroidArmsPVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_pvs5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms VWA V
            _builder.Create(RecipeType.DroidArmsVWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vwa5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms VWS V
            _builder.Create(RecipeType.DroidArmsVWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vws5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms VAS V
            _builder.Create(RecipeType.DroidArmsVAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_vas5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);

            // Droid Arms WAS V
            _builder.Create(RecipeType.DroidArmsWAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidArms)
                .Resref("d_ar_was5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("manip_arm5", 2)
                .Component("discharge_unit5", 1);


        }
    }
}
