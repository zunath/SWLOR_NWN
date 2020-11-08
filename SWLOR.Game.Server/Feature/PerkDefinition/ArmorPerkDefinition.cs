using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ArmorPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Cloaks(builder);
            Belts(builder);
            Rings(builder);
            Necklaces(builder);
            Breastplates(builder);
            Helmets(builder);
            Bracers(builder);
            Leggings(builder);
            HeavyShields(builder);
            Tunics(builder);
            Caps(builder);
            Gloves(builder);
            Boots(builder);
            LightShields(builder);

            return builder.Build();
        }

        private void Cloaks(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Cloaks)
                .Name("Cloaks")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cloaks")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Belts(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Belts)
                .Name("Belts")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Belts")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Rings(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Rings)
                .Name("Rings")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rings")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Necklaces(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Necklaces)
                .Name("Necklaces")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Necklaces")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Breastplates(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Breastplates)
                .Name("Breastplates")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Breastplates")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Helmets(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Helmets)
                .Name("Helmets")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Helmets")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Bracers(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Bracers)
                .Name("Bracers")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Bracers")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Leggings(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Leggings)
                .Name("Leggings")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Leggings")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void HeavyShields(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.HeavyShields)
                .Name("Heavy Shields")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Heavy Shields")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Tunics(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Tunics)
                .Name("Tunics")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Tunics")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Caps(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Caps)
                .Name("Caps")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Caps")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Gloves(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Gloves)
                .Name("Gloves")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Gloves")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void Boots(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Boots)
                .Name("Boots")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Boots")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

        private void LightShields(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.LightShields)
                .Name("Light Shields")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Light Shields")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40);
        }

    }
}
