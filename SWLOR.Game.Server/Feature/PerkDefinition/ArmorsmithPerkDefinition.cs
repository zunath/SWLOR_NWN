using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ArmorsmithPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            ArmorModInstallation(builder);
            ArmorBlueprints(builder);
            HelmetBlueprints(builder);
            BeltBlueprints(builder);
            BootBlueprints(builder);
            BracerBlueprints(builder);
            CloakBlueprints(builder);
            RingBlueprints(builder);
            NeckguardBlueprints(builder);

            return builder.Build();
        }

        private void ArmorModInstallation(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.ArmorModInstallation)
                .Name("Armor Mod Installation")

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 1 armors.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 5)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 2 armors.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 15)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 3 armors.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 25)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 4 armors.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 35)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 5 armors.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 45);
        }

        private void ArmorBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.ArmorBlueprints)
                .Name("Armor Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Armor blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Armor blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Armor blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void HelmetBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.HelmetBlueprints)
                .Name("Helmet Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Helmet blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Helmet blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Helmet blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Helmet blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Helmet blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void BeltBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.BeltBlueprints)
                .Name("Belt Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Belt blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Belt blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Belt blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Belt blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Belt blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void BootBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.BootBlueprints)
                .Name("Boot Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Boot blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Boot blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Boot blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Boot blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Boot blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void BracerBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.BracerBlueprints)
                .Name("Bracer Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Bracer blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Bracer blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Bracer blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Bracer blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Bracer blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void CloakBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.CloakBlueprints)
                .Name("Cloak Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Cloak blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Cloak blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Cloak blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Cloak blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Cloak blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void RingBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.RingBlueprints)
                .Name("Ring Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Ring blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Ring blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Ring blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Ring blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Ring blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }

        private void NeckguardBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Armorsmith, PerkType.NeckguardBlueprints)
                .Name("Neckguard Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Neckguard blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Neckguard blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Armorsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Neckguard blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Armorsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Neckguard blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Neckguard blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Armorsmith, 40);
        }
    }
}
