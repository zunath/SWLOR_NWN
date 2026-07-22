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
    public sealed class BeastDamagePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Bite();
            HuntersFocus();
            RendingClaw();
            Pounce();
            PredatorsMark();
            ExposePrey();
            BloodFrenzy();
            ExecutePrey();
            ApexBite();

            return _builder.Build();
        }

        private void Bite()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.Bite)
                .Name("Bite")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +12 physical DMG.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +22 physical DMG.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite2)

                .AddPerkLevel()
                .Description("The beast's next attack deals +36 physical DMG.")
                .Price(3)
                .RequirementBeastLevel(38)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite3);
        }

        private void HuntersFocus()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.HuntersFocus)
                .Name("Hunter's Focus")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.HuntersFocusTrait)
                .Description("The beast gains +5% hit chance and +5% critical chance.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Damage)
                .IncreasesStat(StatType.AccuracyPercentAdjustment, 5)
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, 5)

                .AddPerkLevel()
                .Description("The beast gains +10% hit chance and +10% critical chance.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .IncreasesStat(StatType.AccuracyPercentAdjustment, 10)
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, 10);
        }

        private void RendingClaw()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.RendingClaw)
                .Name("Rending Claw")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +10 physical DMG and attempts to inflict Bleed for 12 seconds.")
                .Price(3)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.RendingClaw1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +18 physical DMG and attempts to inflict Bleed for 12 seconds.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.RendingClaw2)

                .AddPerkLevel()
                .Description("The beast's next attack deals +28 physical DMG and attempts to inflict Bleed for 12 seconds.")
                .Price(4)
                .RequirementBeastLevel(42)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.RendingClaw3);
        }

        private void Pounce()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.Pounce)
                .Name("Pounce")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast leaps to its target, deals +14 physical DMG, and interrupts activation.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Pounce1)

                .AddPerkLevel()
                .Description("The beast leaps to its target, deals +24 physical DMG, and interrupts activation.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Pounce2);
        }

        private void PredatorsMark()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.PredatorsMark)
                .Name("Predator's Mark")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PredatorsMarkTrait)
                .Description("When your beast uses a Damage active ability, it marks the target for 30 seconds. The beast deals +10% damage to marked targets.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Damage)
                .IncreasesStat(StatType.PredatorsMarkDamageTakenFromBeastPercent, 10)
                .IncreasesStat(StatType.PredatorsMarkDurationSeconds, 30)

                .AddPerkLevel()
                .Description("Predator's Mark also causes damage against marked targets to grant the beast +5% Haste and +2% hit chance for 30 seconds, stacking up to +20% Haste and +8% hit chance.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .IncreasesStat(StatType.PredatorsMarkDamageTakenFromBeastPercent, 10)
                .IncreasesStat(StatType.PredatorsMarkDurationSeconds, 30)
                .IncreasesStat(StatType.PredatorsMarkHastePercentPerStack, 5)
                .IncreasesStat(StatType.PredatorsMarkAbilityHitChancePercentPerStack, 2)
                .IncreasesStat(StatType.PredatorsMarkFollowUpDurationSeconds, 30)
                .IncreasesStat(StatType.PredatorsMarkFollowUpMaximumStacks, 4);
        }

        private void ExposePrey()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.ExposePrey)
                .Name("Expose Prey")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +20 physical DMG and attempts to inflict Exposed for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ExposePrey1);
        }

        private void BloodFrenzy()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.BeastBloodFrenzy)
                .Name("Blood Frenzy")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BeastBloodFrenzyTrait)
                .Description("When the beast damages a bleeding target, it has a 20% chance to restore 1 STM.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Damage)
                .IncreasesStat(StatType.DamageDealtBleedingTargetStaminaRestoreChance, 20)
                .IncreasesStat(StatType.DamageDealtBleedingTargetStaminaRestore, 1)

                .AddPerkLevel()
                .Description("When the beast damages a bleeding target, it has a 30% chance to restore 1 STM.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Damage)
                .IncreasesStat(StatType.DamageDealtBleedingTargetStaminaRestoreChance, 30)
                .IncreasesStat(StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        }

        private void ExecutePrey()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.ExecutePrey)
                .Name("Execute Prey")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +30 physical DMG, increased by 50% against targets below 35% HP.")
                .Price(3)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ExecutePrey1);
        }

        private void ApexBite()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.ApexBite)
                .Name("Apex Bite")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +45 physical DMG and gains +25% critical chance.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ApexBite1)
                .RequirementQuest(BeastMasteryCapstoneQuestDefinition.ApexBiteMasteryQuestId);
        }

    }
}
