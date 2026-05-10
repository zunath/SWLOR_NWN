using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class StaffPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        private static int AbilityStatValue(AbilityType ability) => (int)ability + 1;

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Bonecrusher();
            BreakPosture();
            CrusherStance();
            CrushingStyle();
            CrushingMastery();
            FlurryStyle();
            FlowingDefense();
            GroundQuake();
            GuardingStep();
            HeavyHands();
            LegSweep();
            LineBreaker();
            PatientSentinel();
            PerfectFootwork();
            RibBreaker();
            SentinelGuard();
            SentinelStance();
            ShelterCircle();
            Slam();
            SkullRattle();
            StaffParry();
            SweepingGuard();
            UnmovingCenter();
            Worldbreaker();

            return _builder.Build();
        }

        private void Bonecrusher()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.Bonecrusher)
                .Name("Bonecrusher")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Bonecrusher1)
                .Description("Deals weapon DMG + 50. If the target is Knocked down, they become Stunned for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 45);
        }

        private void BreakPosture()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.BreakPosture)
                .Name("Break Posture")

                .AddPerkLevel()
                .Description("Critical staff hits inflict Exposed, reducing Defense by 10% for 10 seconds.")
                .IncreasesStat(StatType.CriticalTargetDefensePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? -10 : 0)
                .IncreasesStat(StatType.CriticalTargetDefenseDurationSeconds, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Staff, 40);
        }

        private void CrusherStance()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.CrusherStance)
                .Name("Crusher Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrusherStance1)
                .Description("While active, grants +20% Attack and +15% critical chance, but reduces Defense by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 15);
        }

        private void CrushingStyle()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.CrushingStyle)
                .Name("Crushing Style")

                .AddPerkLevel()
                .Description("You gain bonus damage with staves equal to your MGT modifier and +10% critical chance.")
                .IncreasesStat(StatType.StaffMightModifierDamageMultiplier, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 1 : 0)
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Staff, 5);
        }

        private void CrushingMastery()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.CrushingMastery)
                .Name("Crushing Mastery")

                .AddPerkLevel()
                .Description("Critical staff hits deal +10% damage and restore 2 STM. This can only trigger once every 6 seconds.")
                .IncreasesStat(StatType.CriticalDamagePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestore, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 2 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreSkillType, creature => EquipmentPredicates.HasMainHandStaff(creature) ? (int)SkillType.Staff : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreCooldownSeconds, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 6 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Staff, 12)

                .AddPerkLevel()
                .Description("Bonus damage with staves increases to 2x your MGT modifier and critical chance increases by an additional 10%.")
                .IncreasesStat(StatType.CriticalDamagePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestore, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 2 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreSkillType, creature => EquipmentPredicates.HasMainHandStaff(creature) ? (int)SkillType.Staff : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreCooldownSeconds, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 6 : 0)
                .IncreasesStat(StatType.StaffMightModifierDamageMultiplier, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 1 : 0)
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Staff, 32)

                .AddPerkLevel()
                .Description("Staff critical hits deal +20% damage and restore 4 STM. This can only trigger once every 6 seconds.")
                .IncreasesStat(StatType.CriticalDamagePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 20 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestore, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 4 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreSkillType, creature => EquipmentPredicates.HasMainHandStaff(creature) ? (int)SkillType.Staff : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreCooldownSeconds, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 6 : 0)
                .IncreasesStat(StatType.StaffMightModifierDamageMultiplier, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 1 : 0)
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Staff, 48);
        }

        private void FlurryStyle()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.FlurryStyle)
                .Name("Flurry Style")

                .AddPerkLevel()
                .Description("Staves use AGI to-hit and PER for damage. Staff attack delay is reduced by 10%.")
                .IncreasesStat(StatType.StaffDamageAbilityOverride, creature => EquipmentPredicates.HasMainHandStaff(creature) ? AbilityStatValue(AbilityType.Perception) : 0)
                .IncreasesStat(StatType.StaffAccuracyAbilityOverride, creature => EquipmentPredicates.HasMainHandStaff(creature) ? AbilityStatValue(AbilityType.Agility) : 0)
                .IncreasesStat(StatType.AttackDelayReductionPercent, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Staff, 5);
        }

        private void FlowingDefense()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.FlowingDefense)
                .Name("Flowing Defense")

                .AddPerkLevel()
                .Description("After dodging or deflecting an attack, your next Staff ability costs 2 less STM.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 32);
        }

        private void GroundQuake()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.GroundQuake)
                .Name("Ground Quake")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GroundQuake1)
                .Description("Deals weapon DMG + 18 to nearby enemies. Inflicts Knockdown for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GroundQuake2)
                .Description("Deals weapon DMG + 28 to nearby enemies. Inflicts Knockdown for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 35);
        }

        private void GuardingStep()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.GuardingStep)
                .Name("Guarding Step")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardingStep1)
                .Description("Gain +25% Evasion and +20% Defense for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 20);
        }

        private void HeavyHands()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.HeavyHands)
                .Name("Heavy Hands")

                .AddPerkLevel()
                .Description("Staff combat abilities deal +10% damage to targets affected by Knockdown or Blind.")
                .IncreasesStat(StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Staff, 22);
        }

        private void LegSweep()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.LegSweep)
                .Name("Leg Sweep")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LegSweep1)
                .Description("Deals weapon DMG + 6 and inflicts Knockdown for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LegSweep2)
                .Description("Deals weapon DMG + 16 and inflicts Knockdown for 3 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LegSweep3)
                .Description("Deals weapon DMG + 26 and inflicts Knockdown for 4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 35);
        }

        private void LineBreaker()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.LineBreaker)
                .Name("Line Breaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LineBreaker1)
                .Description("Deals weapon DMG + 18 to enemies in a line. Inflicts Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 25);
        }

        private void PatientSentinel()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.PatientSentinel)
                .Name("Patient Sentinel")

                .AddPerkLevel()
                .Description("If you have not used a combat ability for 6 seconds, your next Staff ability gains +15% accuracy and deals +15 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 40);
        }

        private void PerfectFootwork()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.PerfectFootwork)
                .Name("Perfect Footwork")

                .AddPerkLevel()
                .Description("When reduced below 40% HP, gain +30% Evasion for 10 seconds. This can only trigger once every 3 minutes.")
                .IncreasesStat(StatType.LowHPEvasionThresholdPercent, 40)
                .IncreasesStat(StatType.LowHPEvasionPercentAdjustment, 30)
                .IncreasesStat(StatType.LowHPEvasionDurationSeconds, 10)
                .IncreasesStat(StatType.LowHPEvasionCooldownSeconds, 180)
                .Price(4)
                .RequirementSkill(SkillType.Staff, 48);
        }

        private void RibBreaker()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.RibBreaker)
                .Name("Rib Breaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RibBreaker1)
                .Description("Deals weapon DMG + 18 and inflicts Weakened, reducing Attack by 10% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RibBreaker2)
                .Description("Deals weapon DMG + 30 and inflicts Weakened, reducing Attack by 15% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RibBreaker3)
                .Description("Deals weapon DMG + 42 and inflicts Weakened, reducing Attack by 20% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 42);
        }

        private void SentinelGuard()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.SentinelGuard)
                .Name("Sentinel Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SentinelGuard1)
                .Description("For 12 seconds, allies within 5 meters gain +10 Attack Deflection and you generate extra enmity.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 30);
        }

        private void SentinelStance()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.SentinelStance)
                .Name("Sentinel Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SentinelStance1)
                .Description("While active, grants +15% Evasion and +15 Attack Deflection, but reduces Attack by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 15);
        }

        private void ShelterCircle()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.ShelterCircle)
                .Name("Shelter Circle")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShelterCircle1)
                .Description("Allies in an area of effect (sphere) gain +20% Defense and +20% Evasion for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 45);
        }

        private void Slam()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.Slam)
                .Name("Slam")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Slam1)
                .Description("Deals weapon DMG + 8 and inflicts Blind for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Slam2)
                .Description("Deals weapon DMG + 20 and inflicts Blind for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Slam3)
                .Description("Deals weapon DMG + 32 and inflicts Blind for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 28);
        }

        private void SkullRattle()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.SkullRattle)
                .Name("Skull Rattle")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SkullRattle1)
                .Description("Deals weapon DMG + 34 and inflicts Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 38);
        }

        private void StaffParry()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.StaffParry)
                .Name("Staff Parry")

                .AddPerkLevel()
                .Description("Gain +10 Attack Deflection while wielding a staff.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Staff, 8)

                .AddPerkLevel()
                .Description("Gain +20 Attack Deflection total while wielding a staff.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 20 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Staff, 18)

                .AddPerkLevel()
                .Description("Gain +30 Attack Deflection total while wielding a staff. Deflecting attacks restores 2 STM.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 30 : 0)
                .IncreasesStat(StatType.DeflectionStaminaRestore, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 2 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Staff, 28)

                .AddPerkLevel()
                .Description("Gain +40 Attack Deflection total while wielding a staff. Deflecting attacks restores 4 STM.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 40 : 0)
                .IncreasesStat(StatType.DeflectionStaminaRestore, creature => EquipmentPredicates.HasMainHandStaff(creature) ? 4 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Staff, 42);
        }

        private void SweepingGuard()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.SweepingGuard)
                .Name("Sweeping Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SweepingGuard1)
                .Description("Deals weapon DMG + 18 to all nearby enemies. Inflicts Knockdown for 2 seconds. You gain +20% Defense for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 38);
        }

        private void UnmovingCenter()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.UnmovingCenter)
                .Name("Unmoving Center")

                .AddPerkLevel()
                .GrantsFeat(FeatType.UnmovingCenter1)
                .Description("For 20 seconds, you cannot be Knocked down or Dazed, gain +50 Attack Deflection, and staff attacks generate extra enmity.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 50);
        }

        private void Worldbreaker()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.Worldbreaker)
                .Name("Worldbreaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Worldbreaker1)
                .Description("Strike the ground. Enemies in an area of effect (sphere) take weapon DMG + 45 and suffer Knockdown for 4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 50);
        }
    }
}

