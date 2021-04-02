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
                .GrantsFeat(Feat.OneHandedBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 One-Handed blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(Feat.OneHandedBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 One-Handed blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(Feat.OneHandedBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 One-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(Feat.OneHandedBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 One-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(Feat.OneHandedBlueprints5);
        }

        private void TwoHandedBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.TwoHandedBlueprints)
                .Name("Two-Handed Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Two-Handed blueprints.")
                .Price(1)
                .GrantsFeat(Feat.TwoHandedBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Two-Handed blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(Feat.TwoHandedBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Two-Handed blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(Feat.TwoHandedBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Two-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(Feat.TwoHandedBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Two-Handed blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(Feat.TwoHandedBlueprints5);
        }

        private void MartialBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.MartialBlueprints)
                .Name("Martial Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Martial blueprints.")
                .Price(1)
                .GrantsFeat(Feat.MartialBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Martial blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(Feat.MartialBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Martial blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(Feat.MartialBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(Feat.MartialBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Martial blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(Feat.MartialBlueprints5);
        }

        private void RangedBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.RangedBlueprints)
                .Name("Ranged Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Ranged blueprints.")
                .Price(1)
                .GrantsFeat(Feat.RangedBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Ranged blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(Feat.RangedBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Ranged blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(Feat.RangedBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Ranged blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(Feat.RangedBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Ranged blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(Feat.RangedBlueprints5);
        }

        private void ArmorBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.ArmorBlueprints)
                .Name("Armor Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Armor blueprints.")
                .Price(1)
                .GrantsFeat(Feat.ArmorBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Armor blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(Feat.ArmorBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Armor blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(Feat.ArmorBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(Feat.ArmorBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Armor blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(Feat.ArmorBlueprints5);
        }

        private void AccessoryBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Smithery, PerkType.AccessoryBlueprints)
                .Name("Accessory Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Accessory blueprints.")
                .Price(1)
                .GrantsFeat(Feat.AccessoryBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Accessory blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Smithery, 10)
                .GrantsFeat(Feat.AccessoryBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Accessory blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Smithery, 20)
                .GrantsFeat(Feat.AccessoryBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Accessory blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 30)
                .GrantsFeat(Feat.AccessoryBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Accessory blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Smithery, 40)
                .GrantsFeat(Feat.AccessoryBlueprints5);
        }


    }
}
