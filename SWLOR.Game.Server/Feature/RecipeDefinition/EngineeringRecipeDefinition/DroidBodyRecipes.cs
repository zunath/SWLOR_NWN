using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.EngineeringRecipeDefinition
{
    public class DroidBodyRecipes: IRecipeListDefinition
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
            // Droid Body MP I
            _builder.Create(RecipeType.DroidBodyMP1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mp1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body MV I
            _builder.Create(RecipeType.DroidBodyMV1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mv1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body MW I
            _builder.Create(RecipeType.DroidBodyMW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mw1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body MA I
            _builder.Create(RecipeType.DroidBodyMA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ma1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body MS I
            _builder.Create(RecipeType.DroidBodyMS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ms1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body PV I
            _builder.Create(RecipeType.DroidBodyPV1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pv1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body PW I
            _builder.Create(RecipeType.DroidBodyPW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pw1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body PA I
            _builder.Create(RecipeType.DroidBodyPA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pa1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body PS I
            _builder.Create(RecipeType.DroidBodyPS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ps1")
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body VW I
            _builder.Create(RecipeType.DroidBodyVW1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vw1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body VA I
            _builder.Create(RecipeType.DroidBodyVA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_va1")
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body VS I
            _builder.Create(RecipeType.DroidBodyVS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vs1")
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body WA I
            _builder.Create(RecipeType.DroidBodyWA1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_wa1")
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body WS I
            _builder.Create(RecipeType.DroidBodyWS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ws1")
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);

            // Droid Body AS I
            _builder.Create(RecipeType.DroidBodyAS1, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_as1")
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 1)
                .Component("diag_circuit1", 1)
                .Component("dp_supply1", 1)
                .Component("d_chassis1", 1);


        }

        private void Tier2()
        {
            // Droid Body MP II
            _builder.Create(RecipeType.DroidBodyMP2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mp2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body MV II
            _builder.Create(RecipeType.DroidBodyMV2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mv2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body MW II
            _builder.Create(RecipeType.DroidBodyMW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mw2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body MA II
            _builder.Create(RecipeType.DroidBodyMA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ma2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body MS II
            _builder.Create(RecipeType.DroidBodyMS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ms2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body PV II
            _builder.Create(RecipeType.DroidBodyPV2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pv2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body PW II
            _builder.Create(RecipeType.DroidBodyPW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pw2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body PA II
            _builder.Create(RecipeType.DroidBodyPA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pa2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body PS II
            _builder.Create(RecipeType.DroidBodyPS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ps2")
                .Level(18)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body VW II
            _builder.Create(RecipeType.DroidBodyVW2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vw2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body VA II
            _builder.Create(RecipeType.DroidBodyVA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_va2")
                .Level(17)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body VS II
            _builder.Create(RecipeType.DroidBodyVS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vs2")
                .Level(16)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body WA II
            _builder.Create(RecipeType.DroidBodyWA2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_wa2")
                .Level(19)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body WS II
            _builder.Create(RecipeType.DroidBodyWS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ws2")
                .Level(20)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);

            // Droid Body AS II
            _builder.Create(RecipeType.DroidBodyAS2, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_as2")
                .Level(15)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 2)
                .Component("diag_circuit2", 1)
                .Component("dp_supply2", 1)
                .Component("d_chassis2", 1);


        }
        private void Tier3()
        {
            // Droid Body MP III
            _builder.Create(RecipeType.DroidBodyMP3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mp3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body MV III
            _builder.Create(RecipeType.DroidBodyMV3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mv3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body MW III
            _builder.Create(RecipeType.DroidBodyMW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mw3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body MA III
            _builder.Create(RecipeType.DroidBodyMA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ma3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body MS III
            _builder.Create(RecipeType.DroidBodyMS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ms3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body PV III
            _builder.Create(RecipeType.DroidBodyPV3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pv3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body PW III
            _builder.Create(RecipeType.DroidBodyPW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pw3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body PA III
            _builder.Create(RecipeType.DroidBodyPA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pa3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body PS III
            _builder.Create(RecipeType.DroidBodyPS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ps3")
                .Level(28)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body VW III
            _builder.Create(RecipeType.DroidBodyVW3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vw3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body VA III
            _builder.Create(RecipeType.DroidBodyVA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_va3")
                .Level(27)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body VS III
            _builder.Create(RecipeType.DroidBodyVS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vs3")
                .Level(26)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body WA III
            _builder.Create(RecipeType.DroidBodyWA3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_wa3")
                .Level(29)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body WS III
            _builder.Create(RecipeType.DroidBodyWS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ws3")
                .Level(30)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);

            // Droid Body AS III
            _builder.Create(RecipeType.DroidBodyAS3, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_as3")
                .Level(25)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 3)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit3", 1)
                .Component("dp_supply3", 1)
                .Component("d_chassis3", 1);


        }
        private void Tier4()
        {
            // Droid Body MP IV
            _builder.Create(RecipeType.DroidBodyMP4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mp4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body MV IV
            _builder.Create(RecipeType.DroidBodyMV4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mv4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body MW IV
            _builder.Create(RecipeType.DroidBodyMW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mw4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body MA IV
            _builder.Create(RecipeType.DroidBodyMA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ma4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body MS IV
            _builder.Create(RecipeType.DroidBodyMS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ms4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body PV IV
            _builder.Create(RecipeType.DroidBodyPV4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pv4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body PW IV
            _builder.Create(RecipeType.DroidBodyPW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pw4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body PA IV
            _builder.Create(RecipeType.DroidBodyPA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pa4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body PS IV
            _builder.Create(RecipeType.DroidBodyPS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ps4")
                .Level(38)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body VW IV
            _builder.Create(RecipeType.DroidBodyVW4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vw4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body VA IV
            _builder.Create(RecipeType.DroidBodyVA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_va4")
                .Level(37)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body VS IV
            _builder.Create(RecipeType.DroidBodyVS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vs4")
                .Level(36)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body WA IV
            _builder.Create(RecipeType.DroidBodyWA4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_wa4")
                .Level(39)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body WS IV
            _builder.Create(RecipeType.DroidBodyWS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ws4")
                .Level(40)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);

            // Droid Body AS IV
            _builder.Create(RecipeType.DroidBodyAS4, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_as4")
                .Level(35)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 4)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit4", 1)
                .Component("dp_supply4", 1)
                .Component("d_chassis4", 1);


        }
        private void Tier5()
        {
            // Droid Body MP V
            _builder.Create(RecipeType.DroidBodyMP5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mp5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body MV V
            _builder.Create(RecipeType.DroidBodyMV5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mv5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body MW V
            _builder.Create(RecipeType.DroidBodyMW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_mw5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body MA V
            _builder.Create(RecipeType.DroidBodyMA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ma5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body MS V
            _builder.Create(RecipeType.DroidBodyMS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ms5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body PV V
            _builder.Create(RecipeType.DroidBodyPV5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pv5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body PW V
            _builder.Create(RecipeType.DroidBodyPW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pw5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body PA V
            _builder.Create(RecipeType.DroidBodyPA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_pa5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body PS V
            _builder.Create(RecipeType.DroidBodyPS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ps5")
                .Level(48)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body VW V
            _builder.Create(RecipeType.DroidBodyVW5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vw5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body VA V
            _builder.Create(RecipeType.DroidBodyVA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_va5")
                .Level(47)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body VS V
            _builder.Create(RecipeType.DroidBodyVS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_vs5")
                .Level(46)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body WA V
            _builder.Create(RecipeType.DroidBodyWA5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_wa5")
                .Level(49)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body WS V
            _builder.Create(RecipeType.DroidBodyWS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_ws5")
                .Level(50)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);

            // Droid Body AS V
            _builder.Create(RecipeType.DroidBodyAS5, SkillType.Engineering)
                .Category(RecipeCategoryType.DroidBody)
                .Resref("d_bd_as5")
                .Level(45)
                .Quantity(1)
                .RequirementPerk(PerkType.DroidEquipmentBlueprints, 5)
                .EnhancementSlots(RecipeEnhancementType.Droid, 1)
                .Component("diag_circuit5", 1)
                .Component("dp_supply5", 1)
                .Component("d_chassis5", 1);


        }
    }
}
