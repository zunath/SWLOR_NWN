using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidLegsRecipes: IRecipeListDefinition
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
            // Droid Legs MPV I
            _builder.Create(RecipeType.DroidLegsMPV1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpv1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MPW I
            _builder.Create(RecipeType.DroidLegsMPW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpw1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MPA I
            _builder.Create(RecipeType.DroidLegsMPA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpa1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MPS I
            _builder.Create(RecipeType.DroidLegsMPS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mps1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MVW I
            _builder.Create(RecipeType.DroidLegsMVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvw1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MVA I
            _builder.Create(RecipeType.DroidLegsMVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mva1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MVS I
            _builder.Create(RecipeType.DroidLegsMVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvs1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MWA I
            _builder.Create(RecipeType.DroidLegsMWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mwa1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MWS I
            _builder.Create(RecipeType.DroidLegsMWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mws1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs MAS I
            _builder.Create(RecipeType.DroidLegsMAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mas1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs PVW I
            _builder.Create(RecipeType.DroidLegsPVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvw1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs PVA I
            _builder.Create(RecipeType.DroidLegsPVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pva1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs PVS I
            _builder.Create(RecipeType.DroidLegsPVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvs1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs VWA I
            _builder.Create(RecipeType.DroidLegsVWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vwa1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs VWS I
            _builder.Create(RecipeType.DroidLegsVWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vws1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs VAS I
            _builder.Create(RecipeType.DroidLegsVAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vas1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);

            // Droid Legs WAS I
            _builder.Create(RecipeType.DroidLegsWAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_was1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("dmotive_sys1", 2)
                .Component("discharge_unit1", 1);


        }

        private void Tier2()
        {
            // Droid Legs MPV II
            _builder.Create(RecipeType.DroidLegsMPV2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpv2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MPW II
            _builder.Create(RecipeType.DroidLegsMPW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpw2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MPA II
            _builder.Create(RecipeType.DroidLegsMPA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpa2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MPS II
            _builder.Create(RecipeType.DroidLegsMPS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mps2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MVW II
            _builder.Create(RecipeType.DroidLegsMVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvw2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MVA II
            _builder.Create(RecipeType.DroidLegsMVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mva2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MVS II
            _builder.Create(RecipeType.DroidLegsMVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvs2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MWA II
            _builder.Create(RecipeType.DroidLegsMWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mwa2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MWS II
            _builder.Create(RecipeType.DroidLegsMWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mws2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs MAS II
            _builder.Create(RecipeType.DroidLegsMAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mas2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs PVW II
            _builder.Create(RecipeType.DroidLegsPVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvw2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs PVA II
            _builder.Create(RecipeType.DroidLegsPVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pva2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs PVS II
            _builder.Create(RecipeType.DroidLegsPVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvs2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs VWA II
            _builder.Create(RecipeType.DroidLegsVWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vwa2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs VWS II
            _builder.Create(RecipeType.DroidLegsVWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vws2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs VAS II
            _builder.Create(RecipeType.DroidLegsVAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vas2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);

            // Droid Legs WAS II
            _builder.Create(RecipeType.DroidLegsWAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_was2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("dmotive_sys2", 2)
                .Component("discharge_unit2", 1);


        }
        private void Tier3()
        {
            // Droid Legs MPV III
            _builder.Create(RecipeType.DroidLegsMPV3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpv3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MPW III
            _builder.Create(RecipeType.DroidLegsMPW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpw3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MPA III
            _builder.Create(RecipeType.DroidLegsMPA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpa3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MPS III
            _builder.Create(RecipeType.DroidLegsMPS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mps3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MVW III
            _builder.Create(RecipeType.DroidLegsMVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvw3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MVA III
            _builder.Create(RecipeType.DroidLegsMVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mva3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MVS III
            _builder.Create(RecipeType.DroidLegsMVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvs3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MWA III
            _builder.Create(RecipeType.DroidLegsMWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mwa3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MWS III
            _builder.Create(RecipeType.DroidLegsMWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mws3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs MAS III
            _builder.Create(RecipeType.DroidLegsMAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mas3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs PVW III
            _builder.Create(RecipeType.DroidLegsPVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvw3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs PVA III
            _builder.Create(RecipeType.DroidLegsPVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pva3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs PVS III
            _builder.Create(RecipeType.DroidLegsPVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvs3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs VWA III
            _builder.Create(RecipeType.DroidLegsVWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vwa3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs VWS III
            _builder.Create(RecipeType.DroidLegsVWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vws3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs VAS III
            _builder.Create(RecipeType.DroidLegsVAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vas3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);

            // Droid Legs WAS III
            _builder.Create(RecipeType.DroidLegsWAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_was3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys3", 2)
                .Component("discharge_unit3", 1);


        }
        private void Tier4()
        {
            // Droid Legs MPV IV
            _builder.Create(RecipeType.DroidLegsMPV4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpv4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MPW IV
            _builder.Create(RecipeType.DroidLegsMPW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpw4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MPA IV
            _builder.Create(RecipeType.DroidLegsMPA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpa4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MPS IV
            _builder.Create(RecipeType.DroidLegsMPS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mps4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MVW IV
            _builder.Create(RecipeType.DroidLegsMVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvw4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MVA IV
            _builder.Create(RecipeType.DroidLegsMVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mva4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MVS IV
            _builder.Create(RecipeType.DroidLegsMVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvs4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MWA IV
            _builder.Create(RecipeType.DroidLegsMWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mwa4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MWS IV
            _builder.Create(RecipeType.DroidLegsMWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mws4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs MAS IV
            _builder.Create(RecipeType.DroidLegsMAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mas4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs PVW IV
            _builder.Create(RecipeType.DroidLegsPVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvw4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs PVA IV
            _builder.Create(RecipeType.DroidLegsPVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pva4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs PVS IV
            _builder.Create(RecipeType.DroidLegsPVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvs4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs VWA IV
            _builder.Create(RecipeType.DroidLegsVWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vwa4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs VWS IV
            _builder.Create(RecipeType.DroidLegsVWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vws4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs VAS IV
            _builder.Create(RecipeType.DroidLegsVAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vas4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);

            // Droid Legs WAS IV
            _builder.Create(RecipeType.DroidLegsWAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_was4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys4", 2)
                .Component("discharge_unit4", 1);


        }
        private void Tier5()
        {
            // Droid Legs MPV V
            _builder.Create(RecipeType.DroidLegsMPV5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpv5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MPW V
            _builder.Create(RecipeType.DroidLegsMPW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpw5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MPA V
            _builder.Create(RecipeType.DroidLegsMPA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mpa5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MPS V
            _builder.Create(RecipeType.DroidLegsMPS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mps5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MVW V
            _builder.Create(RecipeType.DroidLegsMVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvw5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MVA V
            _builder.Create(RecipeType.DroidLegsMVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mva5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MVS V
            _builder.Create(RecipeType.DroidLegsMVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mvs5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MWA V
            _builder.Create(RecipeType.DroidLegsMWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mwa5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MWS V
            _builder.Create(RecipeType.DroidLegsMWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mws5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs MAS V
            _builder.Create(RecipeType.DroidLegsMAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_mas5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs PVW V
            _builder.Create(RecipeType.DroidLegsPVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvw5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs PVA V
            _builder.Create(RecipeType.DroidLegsPVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pva5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs PVS V
            _builder.Create(RecipeType.DroidLegsPVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_pvs5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs VWA V
            _builder.Create(RecipeType.DroidLegsVWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vwa5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs VWS V
            _builder.Create(RecipeType.DroidLegsVWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vws5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs VAS V
            _builder.Create(RecipeType.DroidLegsVAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_vas5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);

            // Droid Legs WAS V
            _builder.Create(RecipeType.DroidLegsWAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidLegs)
                .Resref("d_lg_was5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("dmotive_sys5", 2)
                .Component("discharge_unit5", 1);


        }
    }
}
