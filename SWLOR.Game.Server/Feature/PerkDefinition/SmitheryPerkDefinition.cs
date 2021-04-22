using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class SmitheryPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();

            OneHandedBlueprints(builder);
            TwoHandedBlueprints(builder);
            MartialBlueprints(builder);
            RangedBlueprints(builder);

            ArmorBlueprints(builder);
            AccessoryBlueprints(builder);

            return builder.Build();
        }

        private void OneHandedBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.OneHandedBlueprints)
                .Name("Vibroblade Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 One-Handed blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.OneHandedBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 One-Handed blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.OneHandedBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 One-Handed blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.OneHandedBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 One-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.OneHandedBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 One-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.OneHandedBlueprints5);
        }

        private void TwoHandedBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.TwoHandedBlueprints)
                .Name("Two-Handed Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Two-Handed blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.TwoHandedBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Two-Handed blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.TwoHandedBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Two-Handed blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.TwoHandedBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Two-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.TwoHandedBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Two-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.TwoHandedBlueprints5);
        }

        private void MartialBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.MartialBlueprints)
                .Name("Martial Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Martial blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.MartialBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Martial blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.MartialBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Martial blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.MartialBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.MartialBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.MartialBlueprints5);
        }

        private void RangedBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.RangedBlueprints)
                .Name("Ranged Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Ranged blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.RangedBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Ranged blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.RangedBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Ranged blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.RangedBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Ranged blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.RangedBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Ranged blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.RangedBlueprints5);
        }

        private void ArmorBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.ArmorBlueprints)
                .Name("Armor Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Armor blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.ArmorBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Armor blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.ArmorBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Armor blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.ArmorBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.ArmorBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.ArmorBlueprints5);
        }

        private void AccessoryBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.AccessoryBlueprints)
                .Name("Accessory Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Accessory blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.AccessoryBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Accessory blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(FeatType.AccessoryBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Accessory blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(FeatType.AccessoryBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Accessory blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(FeatType.AccessoryBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Accessory blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(FeatType.AccessoryBlueprints5);
        }


    }
}
