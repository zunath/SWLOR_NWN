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

            return builder.Build();
        }

        private void CloakProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Cloaks)
                .Name("Cloak Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cloaks")
                .Price(2)
                .GrantsFeat(FeatType.CloakProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.CloakProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.CloakProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.CloakProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cloaks")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.CloakProficiency5);
        }

        private void BeltProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Belts)
                .Name("Belt Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Belts")
                .Price(2)
                .GrantsFeat(FeatType.BeltProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BeltProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BeltProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BeltProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Belts")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BeltProficiency5);
        }

        private void RingProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Rings)
                .Name("Ring Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rings")
                .Price(2)
                .GrantsFeat(FeatType.RingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.RingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.RingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.RingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.RingProficiency5);
        }

        private void NecklaceProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.Necklaces)
                .Name("Necklace Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Necklaces")
                .Price(2)
                .GrantsFeat(FeatType.NecklaceProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.NecklaceProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.NecklaceProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.NecklaceProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Necklaces")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.NecklaceProficiency5);
        }

        private void BreastplateProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Breastplates)
                .Name("Breastplate Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Breastplates")
                .Price(2)
                .GrantsFeat(FeatType.BreastplateProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BreastplateProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BreastplateProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BreastplateProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Breastplates")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BreastplateProficiency5);
        }

        private void HelmetProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Helmets)
                .Name("Helmet Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Helmets")
                .Price(2)
                .GrantsFeat(FeatType.HelmetProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.HelmetProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.HelmetProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.HelmetProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Helmets")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.HelmetProficiency5);
        }

        private void BracerProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Bracers)
                .Name("Bracer Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Bracers")
                .Price(2)
                .GrantsFeat(FeatType.BracerProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BracerProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BracerProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BracerProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Bracers")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BracerProficiency5);
        }

        private void LeggingProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Leggings)
                .Name("Legging Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Leggings")
                .Price(2)
                .GrantsFeat(FeatType.LeggingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.LeggingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.LeggingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.LeggingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Leggings")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.LeggingProficiency5);
        }

        private void HeavyShieldProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.Shields)
                .Name("Shield Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Shields")
                .Price(2)
                .GrantsFeat(FeatType.ShieldProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.ShieldProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.ShieldProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.ShieldProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Shields")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.ShieldProficiency5);
        }

        private void TunicProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Tunics)
                .Name("Tunic Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Tunics")
                .Price(2)
                .GrantsFeat(FeatType.TunicProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.TunicProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.TunicProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.TunicProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Tunics")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.TunicProficiency5);
        }

        private void CapProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Caps)
                .Name("Cap Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Caps")
                .Price(2)
                .GrantsFeat(FeatType.CapProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.CapProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.CapProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.CapProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Caps")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.CapProficiency5);
        }

        private void GloveProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Gloves)
                .Name("Glove Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Gloves")
                .Price(2)
                .GrantsFeat(FeatType.GloveProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.GloveProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.GloveProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.GloveProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Gloves")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.GloveProficiency5);
        }

        private void BootProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.Boots)
                .Name("Boot Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Boots")
                .Price(2)
                .GrantsFeat(FeatType.BootProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BootProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BootProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BootProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Boots")
                .Price(2)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BootProficiency5);
        }
    }
}
