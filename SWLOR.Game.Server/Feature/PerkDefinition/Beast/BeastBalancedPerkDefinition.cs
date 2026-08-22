using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition.Beast
{
    public sealed class BeastBalancedPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Claw();
            BolsterAttack();
            GuardedBite();
            PackRhythm();
            Hasten();
            CoordinatedStrike();
            PackRecovery();
            AlphaRhythm();

            return _builder.Build();
        }

        private void Claw()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.Claw)
                .Name("Claw")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +10 physical DMG and attempts to inflict Bleed for 12 seconds.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +18 physical DMG and attempts to inflict Bleed for 12 seconds.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw2)

                .AddPerkLevel()
                .Description("The beast's next attack deals +28 physical DMG and attempts to inflict Bleed for 12 seconds.")
                .Price(3)
                .RequirementBeastLevel(38)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw3);
        }

        private void BolsterAttack()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.BolsterAttack)
                .Name("Bolster Attack")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast deals 5% more damage for 3 minutes.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack1)

                .AddPerkLevel()
                .Description("The beast deals 8% more damage for 3 minutes.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack2)

                .AddPerkLevel()
                .Description("The beast deals 12% more damage for 3 minutes.")
                .Price(4)
                .RequirementBeastLevel(42)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack3);
        }

        private void GuardedBite()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.GuardedBite)
                .Name("Guarded Bite")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +12 physical DMG and causes the beast to take 5% less physical damage for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.GuardedBite1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +22 physical DMG and causes the beast to take 8% less physical damage for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.GuardedBite2)

                .AddPerkLevel()
                .Description("The beast's next attack deals +34 physical DMG and causes the beast to take 12% less physical damage for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.GuardedBite3);
        }

        private void PackRhythm()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.PackRhythm)
                .Name("Pack Rhythm")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PackRhythmTrait)
                .Description("When the beast uses an ability, its master gains +3% physical and force ability hit chance for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .IncreasesStat(StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 3)
                .IncreasesStat(StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 30)

                .AddPerkLevel()
                .Description("When the beast uses an ability, its master gains +6% physical and force ability hit chance for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .IncreasesStat(StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 6)
                .IncreasesStat(StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds, 30);
        }

        private void Hasten()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.Hasten)
                .Name("Hasten")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast gains +15% Haste for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten1)

                .AddPerkLevel()
                .Description("The beast gains +25% Haste for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten2);
        }

        private void CoordinatedStrike()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.CoordinatedStrike)
                .Name("Coordinated Strike")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +18 physical DMG. If its master damaged the target within 6 seconds, damage increases by 25%.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.CoordinatedStrike1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +30 physical DMG. If its master damaged the target within 6 seconds, damage increases by 25%.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.CoordinatedStrike2);
        }

        private void PackRecovery()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.PackRecovery)
                .Name("Pack Recovery")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PackRecoveryTrait)
                .Description("When your beast uses a Balanced active ability, the beast and master each restore 1 STM. This can trigger once every 8 seconds.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .IncreasesStat(StatType.BeastBalancedAbilityStaminaRestore, 1)
                .IncreasesStat(StatType.BeastBalancedAbilityStaminaRestoreCategoryId, (int)PerkCategoryType.BeastBalanced)
                .IncreasesStat(StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds, 8);
        }

        private void AlphaRhythm()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.AlphaRhythm)
                .Name("Alpha Rhythm")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("For 30 seconds, the beast deals 10% more damage and takes 10% less damage, and its master gains +8% physical and force ability hit chance.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.AlphaRhythm1)
                .RequirementQuest(BeastMasteryCapstoneQuestDefinition.AlphaRhythmMasteryQuestId);
        }

    }
}
