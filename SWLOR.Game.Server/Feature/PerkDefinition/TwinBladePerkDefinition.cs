using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class TwinBladePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BindingCross();
            BladeVortex();
            CenterlineGuard();
            CrossCut();
            CycloneMastery();
            CycloneStance();
            DuelistsChallenge();
            DuelistStance();
            EdgeRhythm();
            FeintingCut();
            FinalForm();
            FlowingFootwork();
            GuardedFlow();
            MirrorStep();
            Momentum();
            PerfectBalance();
            PrecisionArc();
            PunishingAngle();
            ReversalCut();
            SplitGuardStrike();
            SpinningWhirl();
            StormRelease();
            SweepingAdvance();
            TempestBloom();

            return _builder.Build();
        }

        private void BindingCross()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.BindingCross)
                .Name("Binding Cross")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BindingCross1)
                .Description("Strikes twice for weapon DMG + 10 each. Inflicts Hamstring for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BindingCross2)
                .Description("Strikes twice for weapon DMG + 18 each. Inflicts Hamstring for 20 seconds and Exposed for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 42);
        }

        private void BladeVortex()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.BladeVortex)
                .Name("Blade Vortex")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BladeVortex1)
                .Description("Deals weapon DMG + 18 to all nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BladeVortex2)
                .Description("Deals weapon DMG + 26 to all nearby enemies and inflicts Exposed for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 35);
        }

        private void CenterlineGuard()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.CenterlineGuard)
                .Name("Centerline Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CenterlineGuardTrait)
                .Description("Gain +5 Attack Deflection. After deflecting an attack, your next attack within 18 seconds deals +8 DMG.")
                .IncreasesStat(StatType.AttackDeflection, 5)
                .IncreasesStat(StatType.DeflectionNextSkillAbilitySkillType, (int)SkillType.TwinBlade)
                .IncreasesStat(StatType.DeflectionNextSkillAbilityDamageBonus, 8)
                .IncreasesStat(StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds, 18)
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 2);
        }

        private void CrossCut()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.CrossCut)
                .Name("Cross Cut")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrossCut1)
                .Description("Instantly attacks twice, each for weapon DMG + 8, and inflicts Disoriented for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrossCut2)
                .Description("Instantly attacks twice, each for weapon DMG + 17, and inflicts Disoriented for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrossCut3)
                .Description("Instantly attacks twice, each for weapon DMG + 25, and inflicts Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrossCut4)
                .Description("Instantly attacks twice, each for weapon DMG + 34. Inflicts Disoriented and Hamstring for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 42);
        }

        private void CycloneMastery()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.CycloneMastery)
                .Name("Cyclone Mastery")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CycloneMasteryTrait)
                .Description("Area Twin Blade abilities gain +10% critical chance and restore 1 STM per target hit, up to 5 STM.")
                .IncreasesStat(StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment, 10)
                .IncreasesStat(StatType.TwinBladeAreaAbilityStaminaRestorePerTarget, 1)
                .IncreasesStat(StatType.TwinBladeAreaAbilityStaminaRestoreMax, 5)
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 48);
        }

        private void CycloneStance()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.CycloneStance)
                .Name("Cyclone Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CycloneStance1)
                .Description("While active, grants +15% Haste and +10% Attack, but reduces Defense by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 15);
        }

        private void DuelistsChallenge()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.DuelistsChallenge)
                .Name("Duelist's Challenge")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DuelistsChallenge1)
                .Description("Mark a target for 20 seconds. You and the target deal +20% damage to each other, but you gain +20% Defense against that target.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 45);
        }

        private void DuelistStance()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.DuelistStance)
                .Name("Duelist Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DuelistStance1)
                .Description("While active, single-target Twin Blade combat abilities deal +15% damage and grant +10 Attack Deflection for 6 seconds, but area Twin Blade abilities deal -15% damage.")
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 15);
        }

        private void EdgeRhythm()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.EdgeRhythm)
                .Name("Edge Rhythm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EdgeRhythmTrait)
                .Description("Every third auto-attack with a twin blade deals +15 DMG to a nearby enemy.")
                .IncreasesStat(StatType.AutoAttackCycleDamageSkillType, (int)SkillType.TwinBlade)
                .IncreasesStat(StatType.AutoAttackCycleRequiredCount, 3)
                .IncreasesStat(StatType.AutoAttackCycleDamage, 15)
                .IncreasesStat(StatType.AutoAttackCycleRadiusMeters, 5)
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 40);
        }

        private void FeintingCut()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.FeintingCut)
                .Name("Feinting Cut")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FeintingCut1)
                .Description("Deals weapon DMG + 12 and inflicts Weakened, reducing Attack by 10% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FeintingCut2)
                .Description("Deals weapon DMG + 22 and inflicts Weakened, reducing Attack by 15% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FeintingCut3)
                .Description("Deals weapon DMG + 32 and inflicts Weakened, reducing Attack by 20% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 35);
        }

        private void FinalForm()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.FinalForm)
                .Name("Final Form")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FinalForm1)
                .Description("For 45 seconds, single-target physical combat abilities deal +15% damage and you gain +15 Attack Deflection.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 50);
        }

        private void FlowingFootwork()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.FlowingFootwork)
                .Name("Flowing Footwork")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlowingFootworkTrait)
                .Description("After using a Twin Blade combat ability, gain +10% Evasion for 8 seconds.")
                .IncreasesStat(StatType.AbilityUsedEvasionPercentAdjustmentSkillType, (int)SkillType.TwinBlade)
                .IncreasesStat(StatType.AbilityUsedEvasionPercentAdjustment, 10)
                .IncreasesStat(StatType.AbilityUsedEvasionDurationSeconds, 8)
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 22);
        }

        private void GuardedFlow()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.GuardedFlow)
                .Name("Guarded Flow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardedFlowTrait)
                .Description("Using a single-target Twin Blade ability grants +8 Attack Deflection for 8 seconds.")
                .IncreasesStat(StatType.SingleTargetAbilityAttackDeflectionSkillType, (int)SkillType.TwinBlade)
                .IncreasesStat(StatType.SingleTargetAbilityAttackDeflection, 8)
                .IncreasesStat(StatType.SingleTargetAbilityAttackDeflectionDurationSeconds, 8)
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 28);
        }

        private void MirrorStep()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.MirrorStep)
                .Name("Mirror Step")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MirrorStepTrait)
                .Description("When hit by a target you damaged within the last 6 seconds, you have a 15% chance for your next Twin Blade ability to have no attack delay.")
                .IncreasesStat(StatType.DamageTakenRecentTargetNextAbilityNoDelayChance, 15)
                .IncreasesStat(StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType, (int)SkillType.TwinBlade)
                .IncreasesStat(StatType.DamageTakenRecentTargetWindowSeconds, 6)
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 20);
        }

        private void Momentum()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.Momentum)
                .Name("Momentum")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MomentumTrait)
                .Description("Twin Blade abilities that hit 2 or more enemies grant +5% Haste for 8 seconds, up to +15%.")
                .IncreasesStat(StatType.TwinBladeAreaAbilityMinTargetsHasteThreshold, 2)
                .IncreasesStat(StatType.TwinBladeAreaAbilityHastePercentAdjustment, 5)
                .IncreasesStat(StatType.TwinBladeAreaAbilityHasteDurationSeconds, 8)
                .IncreasesStat(StatType.TwinBladeAreaAbilityHastePercentMax, 15)
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 12)

                .AddPerkLevel()
                .Description("Momentum can stack up to +25% Haste and restores 2 STM whenever a stack is gained.")
                .IncreasesStat(StatType.TwinBladeAreaAbilityMinTargetsHasteThreshold, 2)
                .IncreasesStat(StatType.TwinBladeAreaAbilityHastePercentAdjustment, 5)
                .IncreasesStat(StatType.TwinBladeAreaAbilityHasteDurationSeconds, 8)
                .IncreasesStat(StatType.TwinBladeAreaAbilityHastePercentMax, 25)
                .IncreasesStat(StatType.TwinBladeAreaAbilityStaminaRestoreOnHasteStack, 2)
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 32);
        }

        private void PerfectBalance()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.PerfectBalance)
                .Name("Perfect Balance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PerfectBalanceTrait)
                .Description("Single-target Twin Blade abilities restore 3 STM. Area Twin Blade abilities restore 1 STM per target hit, up to 5 STM. This can only trigger once every 8 seconds.")
                .IncreasesStat(StatType.TwinBladeSingleTargetAbilityStaminaRestore, 3)
                .IncreasesStat(StatType.TwinBladeSingleTargetAbilityStaminaRestoreCooldownSeconds, 8)
                .IncreasesStat(StatType.TwinBladeAreaAbilityCooldownStaminaRestorePerTarget, 1)
                .IncreasesStat(StatType.TwinBladeAreaAbilityCooldownStaminaRestoreMax, 5)
                .IncreasesStat(StatType.TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds, 8)
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 48);
        }

        private void PrecisionArc()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.PrecisionArc)
                .Name("Precision Arc")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PrecisionArcTrait)
                .Description("Single-target critical hits reduce the target's Defense by 10% for 10 seconds.")
                .IncreasesStat(StatType.SingleTargetCriticalTargetDefensePercentAdjustment, -10)
                .IncreasesStat(StatType.SingleTargetCriticalTargetDefenseDurationSeconds, 10)
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 40);
        }

        private void PunishingAngle()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.PunishingAngle)
                .Name("Punishing Angle")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PunishingAngleTrait)
                .Description("Deal +12% damage to targets affected by Weakened or Hamstring.")
                .IncreasesStat(StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment, 12)
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 32);
        }

        private void ReversalCut()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.ReversalCut)
                .Name("Reversal Cut")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ReversalCutTrait)
                .Description("After you are hit, your next Twin Blade Duelist ability within 18 seconds deals +40 DMG and inflicts Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 38)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCut, 1)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType, (int)PerkType.SplitGuardStrike)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType, (int)PerkType.FeintingCut)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType, (int)PerkType.BindingCross)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCutDamageBonus, 40)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCutDazedDurationSeconds, 3)
                .IncreasesStat(StatType.TwinBladeDuelistReversalCutWindowSeconds, 18);
        }

        private void SplitGuardStrike()
        {
            _builder.Create(PerkCategoryType.TwinBladeDuelist, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SplitGuardStrike1)
                .Description("Deals weapon DMG + 10 and grants +15% Defense for 10 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwinBlade, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SplitGuardStrike2)
                .Description("Deals weapon DMG + 22 and grants +20% Defense for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SplitGuardStrike3)
                .Description("Deals weapon DMG + 34 and grants +25% Defense for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 30);
        }

        private void SpinningWhirl()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.SpinningWhirl)
                .Name("Spinning Whirl")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SpinningWhirl1)
                .Description("Attacks up to 3 nearby enemies for weapon DMG + 10 each.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SpinningWhirl2)
                .Description("Attacks up to 3 nearby enemies for weapon DMG + 18 each.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SpinningWhirl3)
                .Description("Attacks up to 3 nearby enemies for weapon DMG + 28 each.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 30);
        }

        private void StormRelease()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.StormRelease)
                .Name("Storm Release")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StormRelease1)
                .Description("Consume all Momentum stacks to deal weapon DMG + 15 per stack to all nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 45);
        }

        private void SweepingAdvance()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.SweepingAdvance)
                .Name("Sweeping Advance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SweepingAdvanceTrait)
                .Description("Twin Blade Cyclone area abilities restore 6 STM and grant +10% Haste for 8 seconds when they hit at least three enemies.")
                .Price(3)
                .RequirementSkill(SkillType.TwinBlade, 38)
                .IncreasesStat(StatType.TwinBladeCycloneSweepingAdvance, 1)
                .IncreasesStat(StatType.TwinBladeCycloneSweepingAdvanceMinimumTargets, 3)
                .IncreasesStat(StatType.TwinBladeCycloneSweepingAdvanceStaminaRestore, 6)
                .IncreasesStat(StatType.TwinBladeCycloneSweepingAdvanceHastePercent, 10)
                .IncreasesStat(StatType.TwinBladeCycloneSweepingAdvanceDurationSeconds, 8);
        }

        private void TempestBloom()
        {
            _builder.Create(PerkCategoryType.TwinBladeCyclone, PerkType.TempestBloom)
                .Name("Tempest Bloom")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempestBloom1)
                .Description("Deal weapon DMG + 20 to nearby enemies. For 45 seconds, pulse every 6 seconds, dealing 8 physical DMG and applying a Tempest mark. Each mark increases physical damage taken by 2% to a maximum of 3 stacks.")
                .Price(4)
                .RequirementSkill(SkillType.TwinBlade, 50);
        }

    }
}

