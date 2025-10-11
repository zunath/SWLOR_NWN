using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Definitions.PerkDefinition
{
    public class TwoHandedPerkDefinition : IPerkListDefinition
    {
        private readonly IAbilityService _abilityService;

        public TwoHandedPerkDefinition(
            IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            Cleave(builder);
            CrescentMoon(builder);
            HardSlash(builder);
            Skewer(builder);
            DoubleThrust(builder);
            SpinningWhirl(builder);
            CrossCut(builder);
            CircleSlash(builder);
            DoubleStrike(builder);
            StrongStyleSaberstaff(builder);

            return builder.Build();
        }

        private void Cleave(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.Cleave)
                .Name("Cleave")

                .AddPerkLevel()
                .Description("Grants a free attack against any opponent who is within melee range when an enemy is killed. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .GrantsFeat(FeatType.Cleave);
        }

        private void CrescentMoon(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.CrescentMoon)
                .Name("Crescent Moon")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a DC10 Fortitude check to inflict stun for 3 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrescentMoon1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a DC15 Fortitude check to inflict stun for 3 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrescentMoon2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a DC20 Fortitude check to inflict stun for 3 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrescentMoon3);
        }

        private void HardSlash(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HardSlash)
                .Name("Hard Slash")

                .AddPerkLevel()
                .Description("Instantly deals 16 DMG to your target.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HardSlash1)

                .AddPerkLevel()
                .Description("Instantly deals 24 DMG to your target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HardSlash2)

                .AddPerkLevel()
                .Description("Instantly deals 38 DMG to your target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HardSlash3);
        }

        private void Skewer(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.Skewer)
                .Name("Skewer")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a DC10 Will check to interrupt the target's abilities and concentration.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Skewer1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a DC15 Will check to interrupt the target's abilities and concentration.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Skewer2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a DC20 Will check to interrupt the target's abilities and concentration.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Skewer3);
        }

        private void DoubleThrust(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.DoubleThrust)
                .Name("Double Thrust")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 11 DMG.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleThrust1)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 19 DMG.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleThrust2)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 29 DMG.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleThrust3);
        }

        private void SpinningWhirl(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.SpinningWhirl)
                .Name("Spinning Whirl")

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 10 DMG each.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl1)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 18 DMG each.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl2)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 28 DMG each.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl3);
        }

        private void CrossCut(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.CrossCut)
                .Name("Cross Cut")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 8 DMG and has a DC10 Reflex check to reduce your target's Evasion by 2 for 1 minute.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrossCut1)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 17 DMG and has a DC15 Reflex check to reduce your target's Evasion by 4 for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrossCut2)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 25 DMG and has a DC20 Reflex check to reduce your target's Evasion by 6 for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CrossCut3);
        }

        private void CircleSlash(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.CircleSlash)
                .Name("Circle Slash")

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 10 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleSlash1)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 18 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleSlash2)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 28 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleSlash3);
        }

        private void DoubleStrike(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.DoubleStrike)
                .Name("Double Strike")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 12 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DoubleStrike1)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 21 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DoubleStrike2)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 29 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DoubleStrike3);
        }

        private void StrongStyleSaberstaff(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.StrongStyleSaberstaff)
                .Name("Strong Style (Saberstaff)")
                .TriggerRefund((player) =>
                {
                    _abilityService.ToggleAbility(player, AbilityToggleType.StrongStyleSaberstaff, false);
                })
                .TriggerPurchase((player) =>
                {
                    _abilityService.ToggleAbility(player, AbilityToggleType.StrongStyleSaberstaff, false);
                })

                .AddPerkLevel()
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .Description("Saberstaff attacks use your Perception stat for accuracy and Might stat for damage while active. Additionally, your saberstaff damage is increased by your MGT modifier while active.")
                .Price(1)
                .GrantsFeat(FeatType.StrongStyleSaberstaff);
        }
    }
}

