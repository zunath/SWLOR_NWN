using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class VibroknifePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AfflictionMastery();
            AmbushTactics();
            Backstab();
            AssassinsFocus();
            CalculatedStrikes();
            CascadeFailure();
            CheapShot();
            CripplingPrecision();
            DeadlyPrecision();
            DebilitatingStance();
            EnfeeblingStrike();
            EvasiveCombat();
            ExploitWeakness();
            Hamstring();
            Incapacitate();
            MarkedForDeath();
            NerveStrike();
            Opportunist();
            PrecisionStrikes();
            SapVitality();
            ShadowStrike();
            SmokeBomb();
            SystemicShutdown();
            ToxicCoating();
            VitalStrike();

            return _builder.Build();
        }

        private void AfflictionMastery()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.AfflictionMastery)
                .Name("Affliction Mastery")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AfflictionMasteryTrait)
                .Description("Debuffs you apply last +30% longer.")
                .IncreasesStat(StatType.OutgoingDebuffDurationPercentAdjustment, 30)
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 45);
        }

        private void AmbushTactics()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.AmbushTactics)
                .Name("Ambush Tactics")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AmbushTacticsTrait)
                .Description("After dealing a critical hit, your next attack within 18 seconds ignores 20% of defense.")
                .IncreasesStat(StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment, 20)
                .IncreasesStat(StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds, 18)
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 25);
        }

        private void Backstab()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.Backstab)
                .Name("Backstab")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Backstab1)
                .Description("Deals weapon DMG + 14. From behind your target, deals +20 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Backstab2)
                .Description("Deals weapon DMG + 28. From behind your target, deals +40 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Backstab3)
                .Description("Deals weapon DMG + 42. From behind your target, deals +60 DMG and knocks down for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 35);
        }

        private void AssassinsFocus()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.AssassinsFocus)
                .Name("Assassin's Focus")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AssassinsFocusTrait)
                .Description("After landing a critical hit, gain +5% Accuracy for 30 seconds.")
                .IncreasesStat(StatType.CriticalAccuracyPercentAdjustment, 5)
                .IncreasesStat(StatType.CriticalAccuracyDurationSeconds, 30)
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 32);
        }

        private void CalculatedStrikes()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.CalculatedStrikes)
                .Name("Calculated Strikes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CalculatedStrikesTrait)
                .Description("Auto-attacks have 15% chance to reduce target's Accuracy by 10% for 6s.")
                .IncreasesStat(StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 15)
                .IncreasesStat(StatType.AutoAttackTargetAccuracyPercentAdjustment, -10)
                .IncreasesStat(StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6)
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 2);
        }

        private void CascadeFailure()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.CascadeFailure)
                .Name("Cascade Failure")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CascadeFailureTrait)
                .Description("Incapacitate also hits enemies in a cone and inflicts Vulnerable, reducing Defense by 10% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 48)
                .IncreasesStat(StatType.VibroknifeSaboteurCascadeFailure, 1);
        }

        private void CheapShot()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.CheapShot)
                .Name("Cheap Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CheapShot1)
                .Description("Deals weapon DMG + 8 to a single target. Inflicts Blind for 6 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CheapShot2)
                .Description("Deals weapon DMG + 16 to a single target. Inflicts Blind for 9 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 20);
        }

        private void CripplingPrecision()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.CripplingPrecision)
                .Name("Crippling Precision")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingPrecisionTrait)
                .Description("Your critical hits reduce target's Evasion by 15% for 10s.")
                .IncreasesStat(StatType.CriticalTargetEvasionPercentAdjustment, -15)
                .IncreasesStat(StatType.CriticalTargetEvasionDurationSeconds, 10)
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 25);
        }

        private void DeadlyPrecision()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.DeadlyPrecision)
                .Name("Deadly Precision")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DeadlyPrecision1)
                .Description("While active, grants +15% critical hit chance, -20% evasion, and -15% defense.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 12);
        }

        private void DebilitatingStance()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.DebilitatingStance)
                .Name("Debilitating Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DebilitatingStance1)
                .Description("While active, your attacks inflict Hamstring, reducing movement speed by 20% for 8 seconds, but reduces your attack by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 30);
        }

        private void EnfeeblingStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.EnfeeblingStrike)
                .Name("Enfeebling Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnfeeblingStrike1)
                .Description("Deals weapon DMG + 12. Inflicts Weakened which reduces Attack by 10% for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnfeeblingStrike2)
                .Description("Deals weapon DMG + 24. Inflicts Weakened which reduces Attack by 15% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnfeeblingStrike3)
                .Description("Deals weapon DMG + 36. Inflicts Weakened which reduces attack by 20% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 35);
        }

        private void EvasiveCombat()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.EvasiveCombat)
                .Name("Evasive Combat")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EvasiveCombatTrait)
                .Description("Vibroknife Shadow evasive abilities grant +10% Evasion, reduce enmity by 15%, and reduce Attack by 15% for 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 15)
                .IncreasesStat(StatType.VibroknifeShadowEvasiveCombatRank, 1)

                .AddPerkLevel()
                .Description("Vibroknife Shadow evasive abilities grant +20% Evasion, reduce enmity by 25%, and reduce Attack by 15% for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 38)
                .IncreasesStat(StatType.VibroknifeShadowEvasiveCombatRank, 2);
        }

        private void ExploitWeakness()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.ExploitWeakness)
                .Name("Exploit Weakness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExploitWeaknessTrait)
                .Description("Deal +12% damage to enemies affected by any debuff.")
                .IncreasesStat(StatType.DamageToDebuffedTargetPercentAdjustment, 12)
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 15);
        }

        private void Hamstring()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.Hamstring)
                .Name("Hamstring")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Hamstring1)
                .Description("Your next attack deals +8 DMG. Inflicts Hamstring, reducing movement speed by 20% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Hamstring2)
                .Description("Your next attack deals +18 DMG. Inflicts Hamstring, reducing movement speed by 20% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Hamstring3)
                .Description("Your next attack deals +28 DMG. Inflicts Hamstring, reducing movement speed by 20% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 32);
        }

        private void Incapacitate()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.Incapacitate)
                .Name("Incapacitate")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Incapacitate1)
                .Description("Enemies within the area of effect (sphere) receive the Incapacitate debuff which reduces their evasion by 20% for 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 42);
        }

        private void MarkedForDeath()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.MarkedForDeath)
                .Name("Marked for Death")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MarkedForDeathTrait)
                .Description("Vibroknife Shadow single-target abilities mark the target. Your next three attacks against that target deal +12 DMG each.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 30)
                .IncreasesStat(StatType.VibroknifeShadowMarkedForDeath, 1);
        }

        private void NerveStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.NerveStrike)
                .Name("Nerve Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NerveStrike1)
                .Description("Deals weapon DMG + 22. Inflicts Disoriented which reduces Accuracy and Evasion by 15% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 28);
        }

        private void Opportunist()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.Opportunist)
                .Name("Opportunist")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OpportunistTrait)
                .Description("Grants +15% Critical Rate against targets not facing you.")
                .IncreasesStat(StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment, 15)
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 22);
        }

        private void PrecisionStrikes()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.PrecisionStrikes)
                .Name("Precision Strikes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PrecisionStrikesTrait)
                .Description("Critical hits deal +10% damage.")
                .IncreasesStat(StatType.CriticalDamagePercentAdjustment, 10)
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 2);
        }

        private void SapVitality()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.SapVitality)
                .Name("Sap Vitality")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SapVitalityTrait)
                .Description("Vibroknife Saboteur abilities that inflict Hamstring, Disoriented, or Incapacitate also inflict Exhausted, reducing Defense and Force Defense by 10% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 22)
                .IncreasesStat(StatType.VibroknifeSaboteurSapVitalityRank, 1)

                .AddPerkLevel()
                .Description("Vibroknife Saboteur abilities that inflict Hamstring, Disoriented, or Incapacitate also inflict Exhausted, reducing Defense and Force Defense by 15% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 38)
                .IncreasesStat(StatType.VibroknifeSaboteurSapVitalityRank, 2);
        }

        private void ShadowStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.ShadowStrike)
                .Name("Shadow Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStrike1)
                .Description("Deals weapon DMG + 30 to a single target. Inflicts 30% Slow for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStrike2)
                .Description("Deals weapon DMG + 48 to a single target. Inflicts 40% Slow for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 42);
        }

        private void SmokeBomb()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.SmokeBomb)
                .Name("Smoke Bomb")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SmokeBomb1)
                .Description("All enemies in the selected area are afflicted with Smoke Bomb, reducing Accuracy by 20% for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 40)

                .AddPerkLevel()
                .Description("Smoke Bomb leaves a decoy behind when it ends, causing enemies targeting you to suffer -25% Accuracy for 12 seconds.")
                .IncreasesStat(StatType.SmokeBombDecoyOnExpire, 1)
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 45);
        }

        private void SystemicShutdown()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.SystemicShutdown)
                .Name("Systemic Shutdown")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SystemicShutdown1)
                .Description("All enemies within the area of effect (sphere) take weapon DMG + 15 and are inflicted with Weakened, Hamstring, Exhausted, Disoriented, and Toxin for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 50);
        }

        private void ToxicCoating()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.ToxicCoating)
                .Name("Toxic Coating")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ToxicCoatingTrait)
                .Description("Vibroknife Saboteur strike abilities deal +10 DMG and inflict Toxin for 30 seconds. Toxin deals damage equal to 1% of maximum HP per second.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 12)
                .IncreasesStat(StatType.VibroknifeSaboteurToxicCoatingRank, 1)

                .AddPerkLevel()
                .Description("Vibroknife Saboteur strike abilities deal +22 DMG and inflict Toxin for 30 seconds. Toxin deals damage equal to 1% of maximum HP per second.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 40)
                .IncreasesStat(StatType.VibroknifeSaboteurToxicCoatingRank, 2);
        }

        private void VitalStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.VitalStrike)
                .Name("Vital Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VitalStrike1)
                .Description("Deals weapon DMG + 35. On hit, the target's physical defense is reduced by 10% for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 50);
        }
    }
}

