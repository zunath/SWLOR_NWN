using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class WeaponProficienciesDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            LongswordProficiency(builder);
            KnucklesProficiency(builder);
            DaggerProficiency(builder);
            StaffProficiency(builder);
            RodProficiency(builder);
            RapierProficiency(builder);
            KatanaProficiency(builder);
            GunbladeProficiency(builder);
            RifleProficiency(builder);
            GreatSwordProficiency(builder);

            return builder.Build();
        }

        private static void LongswordProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.LongswordProficiency)
                .Name("Longsword Proficiency")
                .Description("Grants the ability to equip longswords.")
                
                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 longswords.")
                .Price(2)
                
                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 longswords.")
                .Price(2)
                .RequirementSkill(SkillType.Longsword, 10)
                
                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 longswords.")
                .Price(2)
                .RequirementSkill(SkillType.Longsword, 20)
                
                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 longswords.")
                .Price(2)
                .RequirementSkill(SkillType.Longsword, 30)
                
                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 longswords.")
                .Price(2)
                .RequirementSkill(SkillType.Longsword, 40);
        }

        private static void KnucklesProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Monk, PerkType.KnucklesProficiency)
                .Name("Knuckles Proficiency")
                .Description("Grants the ability to equip knuckles.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 knuckles.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.Knuckles, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.Knuckles, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.Knuckles, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 knuckles.")
                .Price(2)
                .RequirementSkill(SkillType.Knuckles, 40);
        }


        private static void DaggerProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Thief, PerkType.DaggerProficiency)
                .Name("Dagger Proficiency")
                .Description("Grants the ability to equip daggers.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 daggers.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 daggers.")
                .Price(2)
                .RequirementSkill(SkillType.Dagger, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 daggers.")
                .Price(2)
                .RequirementSkill(SkillType.Dagger, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 daggers.")
                .Price(2)
                .RequirementSkill(SkillType.Dagger, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 daggers.")
                .Price(2)
                .RequirementSkill(SkillType.Dagger, 40);
        }


        private static void StaffProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.StaffProficiency)
                .Name("Staff Proficiency")
                .Description("Grants the ability to equip staves.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 staves.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 staves.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 staves.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 staves.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 staves.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 40);
        }


        private static void RodProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.RodProficiency)
                .Name("Rod Proficiency")
                .Description("Grants the ability to equip rods.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 rods.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 rods.")
                .Price(2)
                .RequirementSkill(SkillType.Rod, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 rods.")
                .Price(2)
                .RequirementSkill(SkillType.Rod, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 rods.")
                .Price(2)
                .RequirementSkill(SkillType.Rod, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 rods.")
                .Price(2)
                .RequirementSkill(SkillType.Rod, 40);
        }

        private static void RapierProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.RapierProficiency)
                .Name("Rapier Proficiency")
                .Description("Grants the ability to equip rapiers.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 rapiers.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 rapiers.")
                .Price(2)
                .RequirementSkill(SkillType.Rapier, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 rapiers.")
                .Price(2)
                .RequirementSkill(SkillType.Rapier, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 rapiers.")
                .Price(2)
                .RequirementSkill(SkillType.Rapier, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 rapiers.")
                .Price(2)
                .RequirementSkill(SkillType.Rapier, 40);
        }

        private static void KatanaProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Ninja, PerkType.KatanaProficiency)
                .Name("Katana Proficiency")
                .Description("Grants the ability to equip katanas.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 katanas.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 katanas.")
                .Price(2)
                .RequirementSkill(SkillType.Katana, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 katanas.")
                .Price(2)
                .RequirementSkill(SkillType.Katana, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 katanas.")
                .Price(2)
                .RequirementSkill(SkillType.Katana, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 katanas.")
                .Price(2)
                .RequirementSkill(SkillType.Katana, 40);
        }


        private static void GunbladeProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Specialist, PerkType.GunbladeProficiency)
                .Name("Gunblade Proficiency")
                .Description("Grants the ability to equip gunblades.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 gunblades.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 gunblades.")
                .Price(2)
                .RequirementSkill(SkillType.Gunblade, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 gunblades.")
                .Price(2)
                .RequirementSkill(SkillType.Gunblade, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 gunblades.")
                .Price(2)
                .RequirementSkill(SkillType.Gunblade, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 gunblades.")
                .Price(2)
                .RequirementSkill(SkillType.Gunblade, 40);
        }


        private static void RifleProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Sniper, PerkType.RifleProficiency)
                .Name("Rifle Proficiency")
                .Description("Grants the ability to equip rifles.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 rifles.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 40);
        }


        private static void GreatSwordProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.DarkKnight, PerkType.GreatSwordProficiency)
                .Name("Great Sword Proficiency")
                .Description("Grants the ability to equip great swords.")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 great swords.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 great swords.")
                .Price(2)
                .RequirementSkill(SkillType.GreatSword, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 great swords.")
                .Price(2)
                .RequirementSkill(SkillType.GreatSword, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 great swords.")
                .Price(2)
                .RequirementSkill(SkillType.GreatSword, 30)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 great swords.")
                .Price(2)
                .RequirementSkill(SkillType.GreatSword, 40);
        }
    }
}
