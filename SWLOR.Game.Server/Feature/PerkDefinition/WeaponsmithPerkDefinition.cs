using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
                .GrantsFeat(Feat.WeaponModInstallation1)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 2 weapons.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 15)
                .GrantsFeat(Feat.WeaponModInstallation2)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 3 weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 25)
                .GrantsFeat(Feat.WeaponModInstallation3)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 4 weapons.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 35)
                .GrantsFeat(Feat.WeaponModInstallation4)

                .AddPerkLevel()
                .Description("Enables you to install mods into tier 5 weapons.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 45)
                .GrantsFeat(Feat.WeaponModInstallation5);
        }

        private void VibrobladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.VibrobladeBlueprints)
                .Name("Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Vibroblade blueprints.")
                .Price(1)
                .GrantsFeat(Feat.VibrobladeBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Vibroblade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.VibrobladeBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Vibroblade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.VibrobladeBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.VibrobladeBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.VibrobladeBlueprints5);
        }

        private void FinesseVibrobladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.FinesseVibrobladeBlueprints)
                .Name("Finesse Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Finesse Vibroblade blueprints.")
                .Price(1)
                .GrantsFeat(Feat.FinesseVibrobladeBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Finesse Vibroblade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.FinesseVibrobladeBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Finesse Vibroblade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.FinesseVibrobladeBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Finesse Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.FinesseVibrobladeBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Finesse Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.FinesseVibrobladeBlueprints5);
        }

        private void HeavyVibrobladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.HeavyVibrobladeBlueprints)
                .Name("Heavy Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Heavy Vibroblade blueprints.")
                .Price(1)
                .GrantsFeat(Feat.HeavyVibrobladeBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Heavy Vibroblade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.HeavyVibrobladeBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Heavy Vibroblade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.HeavyVibrobladeBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Heavy Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.HeavyVibrobladeBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Heavy Vibroblade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.HeavyVibrobladeBlueprints5);
        }

        private void PolearmBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.PolearmBlueprints)
                .Name("Polearm Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Polearm blueprints.")
                .Price(1)
                .GrantsFeat(Feat.PolearmBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Polearm blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.PolearmBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Polearm blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.PolearmBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Polearm blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.PolearmBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Polearm blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.PolearmBlueprints5);
        }

        private void TwinBladeBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.TwinBladeBlueprints)
                .Name("Twin Blade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Twin Blade blueprints.")
                .Price(1)
                .GrantsFeat(Feat.TwinBladeBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Twin Blade blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.TwinBladeBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Twin Blade blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.TwinBladeBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Twin Blade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.TwinBladeBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Twin Blade blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.TwinBladeBlueprints5);
        }

        private void MartialBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.MartialBlueprints)
                .Name("Martial Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Martial blueprints.")
                .Price(1)
                .GrantsFeat(Feat.MartialBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Martial blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.MartialBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Martial blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.MartialBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.MartialBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.MartialBlueprints5);
        }

        private void BlasterBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.BlasterBlueprints)
                .Name("Blaster Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Blaster blueprints.")
                .Price(1)
                .GrantsFeat(Feat.BlasterBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Blaster blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.BlasterBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Blaster blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.BlasterBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Blaster blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.BlasterBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Blaster blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.BlasterBlueprints5);
        }

        private void ThrowingBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.ThrowingBlueprints)
                .Name("Throwing Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Throwing blueprints.")
                .Price(1)
                .GrantsFeat(Feat.ThrowingBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Throwing blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Weaponsmith, 10)
                .GrantsFeat(Feat.ThrowingBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Throwing blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Weaponsmith, 20)
                .GrantsFeat(Feat.ThrowingBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Throwing blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 30)
                .GrantsFeat(Feat.ThrowingBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Throwing blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Weaponsmith, 40)
                .GrantsFeat(Feat.ThrowingBlueprints5);
        }
    }
}
