using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ArmorPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            CloakProficiency(builder);
            BeltProficiency(builder);
            RingProficiency(builder);
            NecklaceProficiency(builder);
            BreastplateProficiency(builder);
            HelmetProficiency(builder);
            BracerProficiency(builder);
            LeggingProficiency(builder);
            HeavyShieldProficiency(builder);
            TunicProficiency(builder);
            CapProficiency(builder);
            GloveProficiency(builder);
            BootProficiency(builder);
            LightShieldProficiency(builder);

            return builder.Build();
        }

        private void CloakProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Cloaks)
                .Name("Cloak Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cloaks")
                .Price(2)
                .GrantsFeat(Feat.CloakProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.CloakProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.CloakProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.CloakProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.CloakProficiency5);
        }

        private void BeltProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Belts)
                .Name("Belt Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Belts")
                .Price(2)
                .GrantsFeat(Feat.BeltProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.BeltProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.BeltProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.BeltProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.BeltProficiency5);
        }

        private void RingProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Rings)
                .Name("Ring Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rings")
                .Price(2)
                .GrantsFeat(Feat.RingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.RingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.RingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.RingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.RingProficiency5);
        }

        private void NecklaceProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Necklaces)
                .Name("Necklace Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Necklaces")
                .Price(2)
                .GrantsFeat(Feat.NecklaceProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.NecklaceProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.NecklaceProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.NecklaceProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.NecklaceProficiency5);
        }

        private void BreastplateProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Breastplates)
                .Name("Breastplate Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Breastplates")
                .Price(2)
                .GrantsFeat(Feat.BreastplateProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.BreastplateProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.BreastplateProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.BreastplateProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.BreastplateProficiency5);
        }

        private void HelmetProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Helmets)
                .Name("Helmet Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Helmets")
                .Price(2)
                .GrantsFeat(Feat.HelmetProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.HelmetProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.HelmetProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.HelmetProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.HelmetProficiency5);
        }

        private void BracerProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Bracers)
                .Name("Bracer Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Bracers")
                .Price(2)
                .GrantsFeat(Feat.BracerProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.BracerProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.BracerProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.BracerProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.BracerProficiency5);
        }

        private void LeggingProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Leggings)
                .Name("Legging Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Leggings")
                .Price(2)
                .GrantsFeat(Feat.LeggingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.LeggingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.LeggingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.LeggingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.LeggingProficiency5);
        }

        private void HeavyShieldProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.HeavyShields)
                .Name("Heavy Shield Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Heavy Shields")
                .Price(2)
                .GrantsFeat(Feat.HeavyShieldProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.HeavyShieldProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.HeavyShieldProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.HeavyShieldProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Heavy Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.HeavyShieldProficiency5);
        }

        private void TunicProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Tunics)
                .Name("Tunic Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Tunics")
                .Price(2)
                .GrantsFeat(Feat.TunicProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.TunicProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.TunicProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.TunicProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.TunicProficiency5);
        }

        private void CapProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Caps)
                .Name("Cap Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Caps")
                .Price(2)
                .GrantsFeat(Feat.CapProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.CapProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.CapProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.CapProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.CapProficiency5);
        }

        private void GloveProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Gloves)
                .Name("Glove Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Gloves")
                .Price(2)
                .GrantsFeat(Feat.GloveProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.GloveProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.GloveProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.GloveProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.GloveProficiency5);
        }

        private void BootProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Boots)
                .Name("Boot Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Boots")
                .Price(2)
                .GrantsFeat(Feat.BootProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.BootProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.BootProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.BootProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.BootProficiency5);
        }

        private void LightShieldProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.LightShields)
                .Name("Light Shield Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Light Shields")
                .Price(2)
                .GrantsFeat(Feat.LightShieldProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(Feat.LightShieldProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(Feat.LightShieldProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(Feat.LightShieldProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Light Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(Feat.LightShieldProficiency5);
        }

    }
}
