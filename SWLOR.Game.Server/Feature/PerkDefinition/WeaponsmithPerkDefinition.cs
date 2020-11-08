using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class WeaponsmithPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();

            WeaponModInstallation(builder);
            VibrobladeBlueprints(builder);
            FinesseVibrobladeBlueprints(builder);
            HeavyVibrobladeBlueprints(builder);
            PolearmBlueprints(builder);
            TwinBladeBlueprints(builder);
            MartialBlueprints(builder);
            BlasterBlueprints(builder);
            ThrowingBlueprints(builder);

            return builder.Build();
        }

        private void WeaponModInstallation(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.WeaponModInstallation)
                .Name("Weapon Mod Installation")

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 1 weapons.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 5)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 2 weapons.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 15)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 3 weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 25)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 4 weapons.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 35)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 5 weapons.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 45);
        }

        private void VibrobladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.VibrobladeBlueprints)
                .Name("Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Vibroblade blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Vibroblade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Vibroblade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void FinesseVibrobladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.FinesseVibrobladeBlueprints)
                .Name("Finesse Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Finesse Vibroblade blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Finesse Vibroblade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Finesse Vibroblade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Finesse Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Finesse Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void HeavyVibrobladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.HeavyVibrobladeBlueprints)
                .Name("Heavy Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Heavy Vibroblade blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Heavy Vibroblade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Heavy Vibroblade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Heavy Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Heavy Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void PolearmBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.PolearmBlueprints)
                .Name("Polearm Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Polearm blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Polearm blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Polearm blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Polearm blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Polearm blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void TwinBladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.TwinBladeBlueprints)
                .Name("Twin Blade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Twin Blade blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Twin Blade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Twin Blade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Twin Blade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Twin Blade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void MartialBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.MartialBlueprints)
                .Name("Martial Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Martial blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Martial blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Martial blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void BlasterBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.BlasterBlueprints)
                .Name("Blaster Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Blaster blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Blaster blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Blaster blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Blaster blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Blaster blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }

        private void ThrowingBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.ThrowingBlueprints)
                .Name("Throwing Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Throwing blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Throwing blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Throwing blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Throwing blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Throwing blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40);
        }
    }
}
