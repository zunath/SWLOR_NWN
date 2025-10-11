using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Definitions.PerkDefinition
{
    public class RangedPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            QuickDraw(builder);
            DoubleShot(builder);
            ExplosiveToss(builder);
            PiercingToss(builder);
            TranquilizerShot(builder);
            CripplingShot(builder);
            ZenMarksmanship(builder);

            return builder.Build();
        }

        private void QuickDraw(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.QuickDraw)
                .Name("Quick Draw")

                .AddPerkLevel()
                .Description("Instantly deals 10 DMG to your target.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.QuickDraw1)

                .AddPerkLevel()
                .Description("Instantly deals 20 DMG to your target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.QuickDraw2)

                .AddPerkLevel()
                .Description("Instantly deals 30 DMG to your target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.QuickDraw3);
        }

        private void DoubleShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.DoubleShot)
                .Name("Double Shot")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 x 2 DMG.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleShot1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 18 x 2 DMG.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleShot2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 28 x 2 DMG.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleShot3);
        }

        private void ExplosiveToss(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ExplosiveToss)
                .Name("Explosive Toss")

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for 8 DMG.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ExplosiveToss1)

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for 16 DMG.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ExplosiveToss2)

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for 26 DMG.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ExplosiveToss3);
        }

        private void PiercingToss(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.PiercingToss)
                .Name("Piercing Toss")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a 10DC reflex check to inflict Bleed for 30 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PiercingToss1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a 15DC reflex check to inflict Bleed for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PiercingToss2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a 20DC reflex check to inflict Bleed for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PiercingToss3);
        }
        
        private void TranquilizerShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot")

                .AddPerkLevel()
                .Description("Your next attack will tranquilize your target for up to 12 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TranquilizerShot1)

                .AddPerkLevel()
                .Description("Your next attack will tranquilize your target for up to 24 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TranquilizerShot2)

                .AddPerkLevel()
                .Description("Your next attack will tranquilize up to three creatures in a cone for up to 12 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TranquilizerShot3);
        }

        private void CripplingShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.CripplingShot)
                .Name("Crippling Shot")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a 10DC reflex check to inflict Bind for 6 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CripplingShot1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a 15DC reflex check to inflict Bind for 6 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CripplingShot2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a 20DC reflex check to inflict Bind for 6 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CripplingShot3);
        }

        private void ZenMarksmanship(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.ZenMarksmanship)
                .Name("Zen Marksmanship")

                .AddPerkLevel()
                .Description("Your ranged attacks now use Willpower for damage if your Willpower is higher than your weapon's damage stat.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ZenMarksmanship);
        }
    }
}

