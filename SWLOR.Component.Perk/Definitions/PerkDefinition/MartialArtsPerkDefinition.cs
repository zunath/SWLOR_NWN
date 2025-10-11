using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Definitions.PerkDefinition
{
    public class MartialArtsPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            Knockdown(builder);
            InnerStrength(builder);
            Chi(builder);
            ElectricFist(builder);
            StrikingCobra(builder);
            Slam(builder);
            LegSweep(builder);
            CrushingStyle(builder);

            return builder.Build();
        }

        private void Knockdown(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Knockdown)
                .Name("Knockdown")

                .AddPerkLevel()
                .Description("Your next attack has a 12DC fortitude check to inflict knockdown on your target for 4 seconds. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.Knockdown);
        }

        private void InnerStrength(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.InnerStrength)
                .Name("Inner Strength")

                .AddPerkLevel()
                .Description("Improves critical chance by 5%. [Cross Skill]")
                .Price(5)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.InnerStrength1)

                .AddPerkLevel()
                .Description("Improves critical chance by 10%. [Cross Skill]")
                .Price(6)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.InnerStrength2);
        }

        private void Chi(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Chi)
                .Name("Chi")

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 45 points. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.Chi1)

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 115 points. [Cross Skill]")
                .Price(4)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Chi2)

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 170 points. [Cross Skill]")
                .Price(4)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Chi3);
        }

        private void ElectricFist(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ElectricFist)
                .Name("Electric Fist")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 DMG and has a 10DC reflex check to inflict Shock for 30 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 17 DMG and has a 15DC reflex check to inflict Shock for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 24 DMG and has a 20DC reflex check to inflict Shock for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist3);
        }

        private void StrikingCobra(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.StrikingCobra)
                .Name("Striking Cobra")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a 10DC reflex check to inflict Poison for 30 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a 15DC reflex check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a 20DC reflex check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra3);
        }

        private void Slam(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.Slam)
                .Name("Slam")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a 10DC fortitude check to inflict Blindness for 12 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a 15DC fortitude check to inflict Blindness for 12 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a 20DC fortitude check to inflict Blindness for 12 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam3);
        }

        private void LegSweep(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.LegSweep)
                .Name("Leg Sweep")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4 DMG and has a 5DC reflex check to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 13 DMG and has a 8DC reflex check to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 20 DMG and has a 10DC reflex check to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep3);
        }

        private void CrushingStyle(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.CrushingStyle)
                .Name("Crushing Style")

                .AddPerkLevel()
                .Description("Your attacks with a staff now gain a DMG bonus equal to your MGT modifier. In addition, your critical chance is raised by 15%.")
                .Price(1)
                .RequirementCannotHavePerk(PerkType.FlurryStyle)
                .GrantsFeat(FeatType.CrushingStyle)

                .AddPerkLevel()
                .Description("Your MGT DMG bonus is increased to twice your MGT modifier, and critical chance is increased by a further 15%.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .GrantsFeat(FeatType.CrushingMastery);
        }

    }
}
