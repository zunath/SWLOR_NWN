using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class KatarPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AdamantineGuard();
            BreakerReversal();
            CobraReflexes();
            CobraStance();
            CoveringClaws();
            CurrentOverload();
            GuardCounter();
            GuardTraining();
            GuardianReflexes();
            ImpenetrableGrip();
            IronElbows();
            IronWallStance();
            NeuralShock();
            NeurotoxinMastery();
            RedirectingGuard();
            RetaliatoryFlow();
            SerpentsEclipse();
            SpreadingVenom();
            StaticPalm();
            StrikingCobra();
            ToxicRush();
            ToxicTempo();
            TwinFangFlurry();
            TwinGuardStance();
            TwinIntercept();
            VenomRhythm();
            VenomSplash();
            WhirlingGuard();

            return _builder.Build();
        }

        private void AdamantineGuard()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.AdamantineGuard)
                .Name("Adamantine Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AdamantineGuard1)
                .Description("For 45 seconds, gain +25 Guard. Guarded hits reduce damage by an additional 20% and generate 75% more enmity.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 50);
        }

        private void BreakerReversal()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.BreakerReversal)
                .Name("Breaker Reversal")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BreakerReversalTrait)
                .Description("After guarding an attack, your next katar attack deals +35 DMG and inflicts Exposed, reducing Defense by 15% for 12 seconds.")
                .IncreasesStat(StatType.GuardedHitNextKatarAbilityDamageBonus, 35)
                .IncreasesStat(StatType.GuardedHitNextKatarAbilityExposedDurationSeconds, 12)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 45);
        }

        private void CobraReflexes()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.CobraReflexes)
                .Name("Cobra Reflexes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CobraReflexesTrait)
                .Description("Critical hits against poisoned targets restore 4 STM.")
                .IncreasesStat(StatType.CriticalPoisonedTargetStaminaRestore, 4)
                .Price(2)
                .RequirementSkill(SkillType.Katar, 32);
        }

        private void CobraStance()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.CobraStance)
                .Name("Cobra Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CobraStance1)
                .Description("While active, attacks have a 10% chance to inflict Poison for 30 seconds and you gain +10% Attack, but Defense is reduced by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Katar, 15);
        }

        private void CoveringClaws()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.CoveringClaws)
                .Name("Covering Claws")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringClawsTrait)
                .Description("Iron Guard ally-protection abilities cause enemies hit to generate +25% Enmity toward you for 12 seconds.")
                .IncreasesStat(StatType.KatarIronGuardCoveringClaws, 1)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 25);
        }

        private void CurrentOverload()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.CurrentOverload)
                .Name("Current Overload")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CurrentOverload1)
                .Description("Deals weapon DMG + 35. If the target is Poisoned or Disoriented, consume one effect to deal +25 DMG and inflict Stunned for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 42);
        }

        private void GuardCounter()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.GuardCounter)
                .Name("Guard Counter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardCounter1)
                .Description("Your next attack deals weapon DMG + 8. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 16 instead.")
                .Price(2)
                .RequirementSkill(SkillType.Katar, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardCounter2)
                .Description("Your next attack deals weapon DMG + 18. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 30 instead.")
                .Price(2)
                .RequirementSkill(SkillType.Katar, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardCounter3)
                .Description("Your next attack deals weapon DMG + 28. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 45 and inflicts Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 38);
        }

        private void GuardTraining()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.GuardTraining)
                .Name("Guard Training")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardTrainingTrait)
                .Description("Grants a 15% chance to guard against physical attacks, reducing that hit's damage by 20% and generating extra enmity.")
                .IncreasesStat(StatType.Guard, 15)
                .Price(2)
                .RequirementSkill(SkillType.Katar, 2)

                .AddPerkLevel()
                .Description("Guard chance increases to 25% and guarded hits restore 2 STM.")
                .IncreasesStat(StatType.Guard, 25)
                .IncreasesStat(StatType.GuardStaminaRestore, 2)
                .Price(2)
                .RequirementSkill(SkillType.Katar, 15)

                .AddPerkLevel()
                .Description("Guard chance increases to 35% and guarded hits reduce physical damage by 30%.")
                .IncreasesStat(StatType.Guard, 35)
                .IncreasesStat(StatType.GuardStaminaRestore, 2)
                .IncreasesStat(StatType.GuardDamageReductionPercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Katar, 28);
        }

        private void GuardianReflexes()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.GuardianReflexes)
                .Name("Guardian Reflexes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardianReflexesTrait)
                .Description("When reduced below 30% HP, gain +25% guard chance for 30 seconds. This can only trigger once every 3 minutes.")
                .IncreasesStat(StatType.LowHPGuardThresholdPercent, 30)
                .IncreasesStat(StatType.LowHPGuard, 25)
                .IncreasesStat(StatType.LowHPGuardDurationSeconds, 30)
                .IncreasesStat(StatType.LowHPGuardCooldownSeconds, 180)
                .Price(4)
                .RequirementSkill(SkillType.Katar, 48);
        }

        private void ImpenetrableGrip()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.ImpenetrableGrip)
                .Name("Impenetrable Grip")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImpenetrableGripTrait)
                .Description("Gain +20% Knockdown Resistance and +20% Daze Resistance. Guarded hits restore 4 STM.")
                .IncreasesStat(StatType.MobilityResistance, 20)
                .IncreasesStat(StatType.MindResistance, 20)
                .IncreasesStat(StatType.GuardStaminaRestore, 4)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 40);
        }

        private void IronElbows()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.IronElbows)
                .Name("Iron Elbows")

                .AddPerkLevel()
                .GrantsFeat(FeatType.IronElbowsTrait)
                .Description("Iron Guard counterattacks and guard pulses deal +15 DMG to nearby enemies and generate extra enmity.")
                .IncreasesStat(StatType.KatarIronGuardPulseDamageBonus, 15)
                .Price(4)
                .RequirementSkill(SkillType.Katar, 18);
        }

        private void IronWallStance()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.IronWallStance)
                .Name("Iron Wall Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.IronWallStance1)
                .Description("While active, grants +25% Defense, +20% Force Defense, and +30% Enmity generation, but reduces Attack by 25%.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 42);
        }

        private void NeuralShock()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.NeuralShock)
                .Name("Neural Shock")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NeuralShock1)
                .Description("Deals weapon DMG + 20. If the target is Disoriented, they become Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 30);
        }

        private void NeurotoxinMastery()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.NeurotoxinMastery)
                .Name("Neurotoxin Mastery")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NeurotoxinMasteryTrait)
                .Description("Poison effects you apply also reduce the target's Attack by 10%.")
                .IncreasesStat(StatType.OutgoingPoisonAttackPercentAdjustment, -10)
                .Price(4)
                .RequirementSkill(SkillType.Katar, 48);
        }

        private void RedirectingGuard()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.RedirectingGuard)
                .Name("Redirecting Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RedirectingGuardTrait)
                .Description("When you guard an attack, your next katar attack within 10 seconds gains +10% critical chance and deals +10 DMG.")
                .IncreasesStat(StatType.GuardedHitNextSkillAbilitySkillType, (int)SkillType.Katar)
                .IncreasesStat(StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment, 10)
                .IncreasesStat(StatType.GuardedHitNextSkillAbilityDamageBonus, 10)
                .IncreasesStat(StatType.GuardedHitNextSkillAbilityWindowSeconds, 10)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 20);
        }

        private void RetaliatoryFlow()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.RetaliatoryFlow)
                .Name("Retaliatory Flow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RetaliatoryFlowTrait)
                .Description("After you guard a hit, your next Guard Counter within 8 seconds costs 2 less STM and deals +8 DMG.")
                .IncreasesStat(StatType.GuardedHitNextMatchingAbilityPerkType, (int)PerkType.GuardCounter)
                .IncreasesStat(StatType.GuardedHitNextMatchingAbilityDamageBonus, 8)
                .IncreasesStat(StatType.GuardedHitNextMatchingAbilityStaminaCostAdjustment, -2)
                .IncreasesStat(StatType.GuardedHitNextMatchingAbilityWindowSeconds, 8)
                .Price(2)
                .RequirementSkill(SkillType.Katar, 32);
        }

        private void SerpentsEclipse()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.SerpentsEclipse)
                .Name("Serpent's Eclipse")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SerpentsEclipse1)
                .Description("All enemies in an area of effect (sphere) take weapon DMG + 20 poison damage and suffer Poison and Disoriented for 45 seconds. Enemies already affected by either effect take +15 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 50);
        }

        private void SpreadingVenom()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.SpreadingVenom)
                .Name("Spreading Venom")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SpreadingVenomTrait)
                .Description("When a poisoned target dies, the nearest enemy within 5 meters becomes poisoned for 30 seconds.")
                .IncreasesStat(StatType.PoisonedDefeatedEnemySpreadRadiusMeters, 5)
                .IncreasesStat(StatType.PoisonedDefeatedEnemySpreadDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Katar, 40);
        }

        private void StaticPalm()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.StaticPalm)
                .Name("Static Palm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StaticPalm1)
                .Description("Your next attack deals weapon DMG + 8 and inflicts Disoriented for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.StaticPalm2)
                .Description("Your next attack deals weapon DMG + 18 and inflicts Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.StaticPalm3)
                .Description("Your next attack deals weapon DMG + 28 and inflicts Disoriented for 15 seconds. Poisoned targets also become Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 38);
        }

        private void StrikingCobra()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.StrikingCobra)
                .Name("Striking Cobra")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StrikingCobra1)
                .Description("Your next attack deals weapon DMG + 8 and inflicts Poison for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.StrikingCobra2)
                .Description("Your next attack deals weapon DMG + 18 and inflicts Poison for 60 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.StrikingCobra3)
                .Description("Your next attack deals weapon DMG + 28 and inflicts Poison for 60 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 35);
        }

        private void ToxicRush()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.ToxicRush)
                .Name("Toxic Rush")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ToxicRushTrait)
                .Description("Damaging poisoned targets grants +4% Haste and +3% Attack for 6 seconds, stacking up to +20% Haste and +15% Attack. At maximum stacks, attacks against poisoned targets restore 2 STM.")
                .IncreasesStat(StatType.KatarToxicRushHastePercentPerStack, 4)
                .IncreasesStat(StatType.KatarToxicRushAttackPercentPerStack, 3)
                .IncreasesStat(StatType.KatarToxicRushMaximumStacks, 5)
                .IncreasesStat(StatType.KatarToxicRushDurationSeconds, 6)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 45);
        }

        private void ToxicTempo()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.ToxicTempo)
                .Name("Toxic Tempo")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ToxicTempoTrait)
                .Description("Katar abilities deal +8% damage to targets affected by Poison or Disoriented.")
                .IncreasesStat(StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment, 8)
                .Price(2)
                .RequirementSkill(SkillType.Katar, 22);
        }

        private void TwinFangFlurry()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.TwinFangFlurry)
                .Name("Twin Fang Flurry")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinFangFlurryTrait)
                .Description("Single-target Venom Current abilities strike a second time for +10 DMG. If the target is poisoned, the second strike inflicts Bleed for 30 seconds.")
                .IncreasesStat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 10)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 25);
        }

        private void TwinGuardStance()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.TwinGuardStance)
                .Name("Twin Guard Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinGuardStance1)
                .Description("While active, grants +15% Defense and +20% Enmity generation, but reduces Attack by 15%.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 12);
        }

        private void TwinIntercept()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.TwinIntercept)
                .Name("Twin Intercept")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinIntercept1)
                .Description("Target an ally within 6 meters. They gain a damage shield equal to 20% of your maximum HP and +15% Defense for 8 seconds. You gain extra enmity toward enemies near that ally.")
                .Price(3)
                .RequirementSkill(SkillType.Katar, 30);
        }

        private void VenomRhythm()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.VenomRhythm)
                .Name("Venom Rhythm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VenomRhythmTrait)
                .Description("Attacks against poisoned targets have a 15% chance to deal +6 DMG.")
                .IncreasesStat(StatType.DamageToPoisonedTargetFlatBonusChance, 15)
                .IncreasesStat(StatType.DamageToPoisonedTargetFlatBonus, 6)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 12);
        }

        private void VenomSplash()
        {
            _builder.Create(PerkCategoryType.KatarVenomCurrent, PerkType.VenomSplash)
                .Name("Venom Splash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VenomSplashTrait)
                .Description("Venom Current strike abilities spread Poison to nearby enemies when they hit a poisoned target.")
                .IncreasesStat(StatType.KatarVenomCurrentPoisonSpreadRadiusMeters, 5)
                .IncreasesStat(StatType.KatarVenomCurrentPoisonSpreadDurationSeconds, 30)
                .Price(3)
                .RequirementSkill(SkillType.Katar, 28);
        }

        private void WhirlingGuard()
        {
            _builder.Create(PerkCategoryType.KatarIronGuard, PerkType.WhirlingGuard)
                .Name("Whirling Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.WhirlingGuard1)
                .Description("For 12 seconds, gain +20% guard chance and deal 8 DMG back to attackers whenever you guard a hit.")
                .Price(4)
                .RequirementSkill(SkillType.Katar, 35);
        }
    }
}

