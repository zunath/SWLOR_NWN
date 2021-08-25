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
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.CloakProficiency)
                .Name("Cloak Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cloaks")
                .Price(1)
                .GrantsFeat(FeatType.CloakProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.CloakProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.CloakProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.CloakProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.CloakProficiency5)
                .AddPerkLevel()

                .Description("Grants the ability to equip tier 6 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.CloakProficiency6)

                .Description("Grants the ability to equip tier 7 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.CloakProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.CloakProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.CloakProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Cloaks")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.CloakProficiency10);
        }

        private void BeltProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.BeltProficiency)
                .Name("Belt Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Belts")
                .Price(1)
                .GrantsFeat(FeatType.BeltProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.BeltProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BeltProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.BeltProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BeltProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.BeltProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BeltProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.BeltProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BeltProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Belts")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.BeltProficiency10);
        }

        private void RingProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.RingProficiency)
                .Name("Ring Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rings")
                .Price(1)
                .GrantsFeat(FeatType.RingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.RingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.RingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.RingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.RingProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.RingProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.RingProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.RingProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.RingProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Rings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.RingProficiency10);
        }

        private void NecklaceProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorGeneral, PerkType.NecklaceProficiency)
                .Name("Necklace Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Necklaces")
                .Price(1)
                .GrantsFeat(FeatType.NecklaceProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.NecklaceProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.NecklaceProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.NecklaceProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.NecklaceProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.NecklaceProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.NecklaceProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.NecklaceProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.NecklaceProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Necklaces")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.NecklaceProficiency10);
        }

        private void BreastplateProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.BreastplateProficiency)
                .Name("Breastplate Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Breastplates")
                .Price(1)
                .GrantsFeat(FeatType.BreastplateProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.BreastplateProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BreastplateProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.BreastplateProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BreastplateProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.BreastplateProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BreastplateProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.BreastplateProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BreastplateProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Breastplates")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.BreastplateProficiency10);
        }

        private void HelmetProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.HelmetProficiency)
                .Name("Helmet Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Helmets")
                .Price(1)
                .GrantsFeat(FeatType.HelmetProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.HelmetProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.HelmetProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.HelmetProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.HelmetProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.HelmetProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.HelmetProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.HelmetProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.HelmetProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Helmets")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.HelmetProficiency10);
        }

        private void BracerProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.BracerProficiency)
                .Name("Bracer Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Bracers")
                .Price(1)
                .GrantsFeat(FeatType.BracerProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.BracerProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BracerProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.BracerProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BracerProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.BracerProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BracerProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.BracerProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BracerProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Bracers")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.BracerProficiency10);
        }

        private void LeggingProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.LeggingProficiency)
                .Name("Legging Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Leggings")
                .Price(1)
                .GrantsFeat(FeatType.LeggingProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.LeggingProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.LeggingProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.LeggingProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.LeggingProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.LeggingProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.LeggingProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.LeggingProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.LeggingProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Leggings")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.LeggingProficiency10);
        }

        private void HeavyShieldProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorHeavy, PerkType.ShieldProficiency)
                .Name("Shield Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Shields")
                .Price(1)
                .GrantsFeat(FeatType.ShieldProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.ShieldProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.ShieldProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.ShieldProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.ShieldProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.ShieldProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.ShieldProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.ShieldProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.ShieldProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Shields")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.ShieldProficiency10);
        }

        private void TunicProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.TunicProficiency)
                .Name("Tunic Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Tunics")
                .Price(1)
                .GrantsFeat(FeatType.TunicProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.TunicProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.TunicProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.TunicProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.TunicProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.TunicProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.TunicProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.TunicProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.TunicProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Tunics")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.TunicProficiency10);
        }

        private void CapProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.CapProficiency)
                .Name("Cap Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Caps")
                .Price(1)
                .GrantsFeat(FeatType.CapProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.CapProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.CapProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.CapProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.CapProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.CapProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.CapProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.CapProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.CapProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Caps")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.CapProficiency10);
        }

        private void GloveProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.GloveProficiency)
                .Name("Glove Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Gloves")
                .Price(1)
                .GrantsFeat(FeatType.GloveProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.GloveProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.GloveProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.GloveProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.GloveProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.GloveProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.GloveProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.GloveProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.GloveProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Gloves")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.GloveProficiency10);
        }

        private void BootProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ArmorLight, PerkType.BootProficiency)
                .Name("Boot Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Boots")
                .Price(1)
                .GrantsFeat(FeatType.BootProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 5)
                .GrantsFeat(FeatType.BootProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 10)
                .GrantsFeat(FeatType.BootProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 15)
                .GrantsFeat(FeatType.BootProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 20)
                .GrantsFeat(FeatType.BootProficiency5)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 6 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 25)
                .GrantsFeat(FeatType.BootProficiency6)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 7 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 30)
                .GrantsFeat(FeatType.BootProficiency7)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 8 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 35)
                .GrantsFeat(FeatType.BootProficiency8)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 9 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 40)
                .GrantsFeat(FeatType.BootProficiency9)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 10 Boots")
                .Price(1)
                .RequirementSkill(SkillType.Armor, 45)
                .GrantsFeat(FeatType.BootProficiency10);
        }
    }

}
