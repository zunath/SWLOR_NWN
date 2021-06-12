using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition
{
    public class ArmorRecipes1 : IRecipeListDefinition
    {
        private readonly RecipeBuilder _builder = new RecipeBuilder();

        public Dictionary<RecipeType, RecipeDetail> BuildRecipes()
        {
            BasicBreastplate();
            BasicTunic();
            BasicBattlemasterHelmet();
            BasicChannelerCap();
            BasicDuelistHelmet();
            BasicGunslingerCap();
            BasicShield();
            BasicMightBelt();
            BasicThiefBelt();
            BasicWarriorBelt();
            BasicBattlemasterLegging();
            BasicChannelerBoot();
            BasicDuelistLegging();
            BasicGunslingerBoot();
            BasicDuelistBracer();
            BasicBattlemasterBracer();
            BasicChannelerGlove();
            BasicGunslingerGlove();
            BasicNecklace();
            BasicQuicknessRing();
            BasicMightCloak();
            BasicThiefCloak();
            BasicWarriorCloak();

            return _builder.Build();
        }

        private void BasicBreastplate()
        {
            _builder.Create(RecipeType.BasicBreastplate, SkillType.Smithery)
                .Category(RecipeCategoryType.Breastplate)
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("breastplate_b")
                .Component("ref_veldite", 8)
                .Component("lth_destroyed", 2);
        }

        private void BasicTunic()
        {
            _builder.Create(RecipeType.BasicTunic, SkillType.Smithery)
                .Category(RecipeCategoryType.Tunic)
                .Level(1)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("tunic_b")
                .Component("ref_veldite", 2)
                .Component("lth_destroyed", 8);
        }

        private void BasicBattlemasterHelmet()
        {
            _builder.Create(RecipeType.BasicBattlemasterHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("bm_helm_b")
                .Component("ref_veldite", 4)
                .Component("lth_destroyed", 4);
        }

        private void BasicChannelerCap()
        {
            _builder.Create(RecipeType.BasicChannelerCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("cn_cap_b")
                .Component("ref_veldite", 3)
                .Component("lth_destroyed", 5);
        }

        private void BasicDuelistHelmet()
        {
            _builder.Create(RecipeType.BasicDuelistHelmet, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("du_helm_b")
                .Component("ref_veldite", 6)
                .Component("lth_destroyed", 2);
        }

        private void BasicGunslingerCap()
        {
            _builder.Create(RecipeType.BasicGunslingerCap, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("gn_cap_b")
                .Component("ref_veldite", 2)
                .Component("lth_destroyed", 6);
        }

        private void BasicShield()
        {
            _builder.Create(RecipeType.BasicShield, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("shield_b")
                .Component("ref_veldite", 8)
                .Component("lth_destroyed", 3);
        }

        private void BasicMightBelt()
        {
            _builder.Create(RecipeType.BasicMightBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("mght_belt_b")
                .Component("ref_veldite", 3)
                .Component("lth_destroyed", 5);
        }

        private void BasicThiefBelt()
        {
            _builder.Create(RecipeType.BasicThiefBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("thf_belt_b")
                .Component("ref_veldite", 4)
                .Component("lth_destroyed", 4);
        }

        private void BasicWarriorBelt()
        {
            _builder.Create(RecipeType.BasicWarriorBelt, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("war_belt_b")
                .Component("ref_veldite", 8)
                .Component("lth_destroyed", 2);
        }

        private void BasicBattlemasterLegging()
        {
            _builder.Create(RecipeType.BasicBattlemasterLegging, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(2)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("bm_legging_b")
                .Component("ref_veldite", 7)
                .Component("lth_destroyed", 5);
        }

        private void BasicChannelerBoot()
        {
            _builder.Create(RecipeType.BasicChannelerBoot, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("cn_boot_b")
                .Component("ref_veldite", 5)
                .Component("lth_destroyed", 7);
        }

        private void BasicDuelistLegging()
        {
            _builder.Create(RecipeType.BasicDuelistLegging, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("du_legging_b")
                .Component("ref_veldite", 6)
                .Component("lth_destroyed", 5);
        }

        private void BasicGunslingerBoot()
        {
            _builder.Create(RecipeType.BasicGunslingerBoot, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(5)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("gn_boot_b")
                .Component("ref_veldite", 5)
                .Component("lth_destroyed", 6);
        }

        private void BasicDuelistBracer()
        {
            _builder.Create(RecipeType.BasicDuelistBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(3)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("du_bracer_b")
                .Component("ref_veldite", 7)
                .Component("lth_destroyed", 1);
        }

        private void BasicBattlemasterBracer()
        {
            _builder.Create(RecipeType.BasicBattlemasterBracer, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(4)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("bm_bracer_b")
                .Component("ref_veldite", 7)
                .Component("lth_destroyed", 2);
        }

        private void BasicChannelerGlove()
        {
            _builder.Create(RecipeType.BasicChannelerGlove, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("cn_glove_b")
                .Component("ref_veldite", 2)
                .Component("lth_destroyed", 7);
        }

        private void BasicGunslingerGlove()
        {
            _builder.Create(RecipeType.BasicGunslingerGlove, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("gn_glove_b")
                .Component("ref_veldite", 3)
                .Component("lth_destroyed", 6);
        }

        private void BasicNecklace()
        {
            _builder.Create(RecipeType.BasicNecklace, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(9)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("necklace_b")
                .Component("ref_veldite", 4)
                .Component("lth_destroyed", 2);
        }

        private void BasicQuicknessRing()
        {
            _builder.Create(RecipeType.BasicQuicknessRing, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(10)
                .Quantity(1)
                .RequirementPerk(PerkType.AccessoryBlueprints, 1)
                .Resref("qck_ring_b")
                .Component("ref_veldite", 4)
                .Component("lth_destroyed", 2);
        }

        private void BasicMightCloak()
        {
            _builder.Create(RecipeType.BasicMightCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(8)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("mght_cloak_b")
                .Component("ref_veldite", 2)
                .Component("lth_destroyed", 9);
        }

        private void BasicThiefCloak()
        {
            _builder.Create(RecipeType.BasicThiefCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(7)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("thf_cloak_b")
                .Component("ref_veldite", 3)
                .Component("lth_destroyed", 7);
        }

        private void BasicWarriorCloak()
        {
            _builder.Create(RecipeType.BasicWarriorCloak, SkillType.Smithery)
                .Category(RecipeCategoryType.Helmet)
                .Level(6)
                .Quantity(1)
                .RequirementPerk(PerkType.ArmorBlueprints, 1)
                .Resref("war_cloak_b")
                .Component("ref_veldite", 4)
                .Component("lth_destroyed", 6);
        }
    }
}