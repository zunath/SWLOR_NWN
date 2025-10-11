using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Definitions.PerkDefinition
{
    public class OneHandedPerkDefinition : IPerkListDefinition
    {
        private readonly IAbilityService _abilityService;

        public OneHandedPerkDefinition(
            IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            Doublehand(builder);
            DualWield(builder);
            HackingBlade(builder);
            RiotBlade(builder);
            PoisonStab(builder);
            Backstab(builder);
            SaberStrike(builder);
            StrongStyleLightsaber(builder);
            ShieldBash(builder);
            Bulwark(builder);
            Alacrity(builder);
            Clarity(builder);

            return builder.Build();
        }

        private void Doublehand(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.Doublehand)
                .Name("Doublehand")

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 2 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Doublehand1)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 6 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.Doublehand2)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 10 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.Doublehand3)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 14 when no off-hand item is equipped. [Cross Skill]")
                .Price(1)
                .DroidAISlots(5)
                .RequirementSkill(SkillType.OneHanded, 30)
                .GrantsFeat(FeatType.Doublehand4)

                .AddPerkLevel()
                .Description("Increases DMG of one-handed weapons by 19 when no off-hand item is equipped. [Cross Skill]")
                .Price(2)
                .DroidAISlots(6)
                .RequirementSkill(SkillType.OneHanded, 40)
                .GrantsFeat(FeatType.Doublehand5);
        }

        private void DualWield(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedGeneral, PerkType.DualWield)
                .Name("Dual Wield")

                .AddPerkLevel()
                .Description("Enables the use of two one-handed weapons at the same time at -10%/-10% to hit. [Cross Skill]")
                .Price(4)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.OneHanded, 15)
                .GrantsFeat(FeatType.DualWield);
        }



        private void ShieldBash(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.ShieldBash)
                .Name("Shield Bash")

                .AddPerkLevel()
                .Description("Bashes an enemy for 8 DMG and has a DC12 Will check to inflict Dazed for 3 seconds.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.OneHanded, 5)
                .GrantsFeat(FeatType.ShieldBash1)

                .AddPerkLevel()
                .Description("Bashes an enemy for 16 DMG and has a DC14 Will check to inflict Dazed for 3 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .GrantsFeat(FeatType.ShieldBash2)

                .AddPerkLevel()
                .Description("Bashes an enemy for 24 DMG and has a DC16 Will check to inflict Dazed for 3 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 35)
                .GrantsFeat(FeatType.ShieldBash3);
        }

        private void Bulwark(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.Bulwark)
                .Name("Bulwark")

                .AddPerkLevel()
                .Description("While equipped with a shield, you automatically attempt to deflect ranged attacks once per round.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 10)
                .GrantsFeat(FeatType.Bulwark);
        }
        private void Alacrity(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.Alacrity)
                .Name("Alacrity")

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 4 STM when damaged while equipped with a shield.")
                .Price(2)
                .DroidAISlots(3)
                .RequirementCannotHavePerk(PerkType.Clarity)
                .RequirementSkill(SkillType.OneHanded, 25);
        }

        private void Clarity(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedShield, PerkType.Clarity)
                .Name("Clarity")

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 4 FP when damaged while equipped with a shield.")
                .Price(2)
                .RequirementCannotHavePerk(PerkType.Alacrity)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementSkill(SkillType.OneHanded, 25);
        }

        private void HackingBlade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.HackingBlade)
                .Name("Hacking Blade")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a DC10 Fortitude check to inflict Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a DC15 Fortitude check to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a DC20 Fortitude check to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.HackingBlade3);
        }

        private void RiotBlade(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedVibroblade, PerkType.RiotBlade)
                .Name("Riot Blade")

                .AddPerkLevel()
                .Description("Instantly deals 10 DMG to your target.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade1)

                .AddPerkLevel()
                .Description("Instantly deals 20 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade2)

                .AddPerkLevel()
                .Description("Instantly deals 30 DMG to your target.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RiotBlade3);
        }

        private void PoisonStab(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.PoisonStab)
                .Name("Poison Stab")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 DMG and has a DC10 Fortitude check to inflict Poison for 30 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 18 DMG and has a DC15 Fortitude check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 28 DMG and has a DC20 Fortitude check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PoisonStab3);
        }

        private void Backstab(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedFinesseVibroblade, PerkType.Backstab)
                .Name("Backstab")

                .AddPerkLevel()
                .Description("Deals 14 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab1)

                .AddPerkLevel()
                .Description("Deals 30 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab2)

                .AddPerkLevel()
                .Description("Deals 45 DMG to your target when dealt from behind. Damage is halved if not behind target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Backstab3);
        }


        private void SaberStrike(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.SaberStrike)
                .Name("Saber Strike")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a DC10 Fortitude check to inflict Breach, reducing evasion for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.OneHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a DC15 Fortitude check to inflict Breach, reducing evasion for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a DC20 Fortitude check to inflict Breach, reducing evasion for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.OneHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberStrike3);
        }

        private void StrongStyleLightsaber(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.OneHandedLightsaber, PerkType.StrongStyleLightsaber)
                .Name("Strong Style (Lightsaber)")
                .TriggerRefund((player) =>
                {
                    _abilityService.ToggleAbility(player, AbilityToggleType.StrongStyleLightsaber, false);
                })
                .TriggerPurchase((player) =>
                {
                    _abilityService.ToggleAbility(player, AbilityToggleType.StrongStyleLightsaber, false);
                })

                .AddPerkLevel()
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .Description("While active, attacks with a lightsaber use PER to-hit and MGT for damage, and gain bonus damage equal to half your MGT modifier.")
                .Price(1)
                .GrantsFeat(FeatType.StrongStyleLightsaber);
        }
    }
}

