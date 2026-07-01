using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class RiflePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AimedShot();
            BallisticMastery();
            BreachRound();
            ContainmentNet();
            CripplingShot();
            DeadCenter();
            ExposeWeakPoint();
            FieldSedatives();
            Headshot();
            KillZone();
            NeutralizingShot();
            OneShot();
            Overwatch();
            PacificationField();
            PiercingRound();
            PinningFire();
            ScopeCalibration();
            SniperStance();
            SoftTarget();
            SpotterStance();
            StasisVolley();
            SteadyAim();
            SuppressiveLine();
            TranqCone();
            TranquilizerShot();
            VeteranTracker();

            return _builder.Build();
        }

        private void AimedShot()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.AimedShot)
                .Name("Aimed Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AimedShot1)
                .Description("Deals weapon DMG + 18. If the target is farther than 8 meters away, deal an additional +10 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AimedShot2)
                .Description("Deals weapon DMG + 32. If the target is farther than 8 meters away, deal an additional +16 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.AimedShot3)
                .Description("Deals weapon DMG + 46. If the target is farther than 8 meters away, deal an additional +24 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 28);
        }

        private void BallisticMastery()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.BallisticMastery)
                .Name("Ballistic Mastery")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BallisticMasteryTrait)
                .Description("Rifle abilities against Exposed or Sundered targets ignore an additional 15% Defense.")
                .IncreasesStat(StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment, 15)
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 48);
        }

        private void BreachRound()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.BreachRound)
                .Name("Breach Round")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BreachRoundTrait)
                .Description("Ranged weapon attacks, including combat abilities, ignore 8% of the target's Defense and deal +6 DMG.")
                .IncreasesStat(StatType.RangedAttackDefenseIgnorePercentAdjustment, 8)
                .IncreasesStat(StatType.RangedAttackDamageFlatAdjustment, 6)
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 35);
        }

        private void ContainmentNet()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.ContainmentNet)
                .Name("Containment Net")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ContainmentNetTrait)
                .Description("Enemies affected by your Disoriented effects suffer an additional -10% Evasion and -10% Attack.")
                .IncreasesStat(StatType.OutgoingDisorientedAttackPercentAdjustment, -10)
                .IncreasesStat(StatType.OutgoingDisorientedEvasionPercentAdjustment, -10)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 40);
        }

        private void CripplingShot()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.CripplingShot)
                .Name("Crippling Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingShot1)
                .Description("Deals weapon DMG + 12 and inflicts Disoriented for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingShot2)
                .Description("Deals weapon DMG + 22 and inflicts Disoriented for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingShot3)
                .Description("Deals weapon DMG + 34 and inflicts Disoriented for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 35);
        }

        private void DeadCenter()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.DeadCenter)
                .Name("Dead Center")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DeadCenterTrait)
                .Description("Rifle critical hits restore 4 STM and cause your next Aimed Shot within 18 seconds to deal +10 DMG. This can only trigger once every 6 seconds.")
                .IncreasesStat(StatType.CriticalStaminaRestore, 4)
                .IncreasesStat(StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.CriticalStaminaRestoreCooldownSeconds, 6)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusTriggerSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusPerkType, (int)PerkType.AimedShot)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonus, 10)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusDurationSeconds, 18)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusCooldownSeconds, 6)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 38);
        }

        private void ExposeWeakPoint()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.ExposeWeakPoint)
                .Name("Expose Weak Point")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExposeWeakPointTrait)
                .Description("Aimed Shot marks the target for 12 seconds. Physical attacks against marked targets deal +10% damage.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 30)
                .IncreasesStat(StatType.RifleMarksmanExposeWeakPoint, (int)PerkType.AimedShot);
        }

        private void FieldSedatives()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.FieldSedatives)
                .Name("Field Sedatives")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FieldSedativesTrait)
                .Description("After a tranquilizer effect ends, the target's Attack is reduced by 10% for 10 seconds.")
                .IncreasesStat(StatType.TranquilizeExpiredAttackPercentAdjustment, -10)
                .IncreasesStat(StatType.TranquilizeExpiredAttackDurationSeconds, 10)
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 32);
        }

        private void Headshot()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.Headshot)
                .Name("Headshot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Headshot1)
                .Description("Deals weapon DMG + 60. Targets below 50% HP become Dazed for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 42);
        }

        private void KillZone()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.KillZone)
                .Name("Kill Zone")

                .AddPerkLevel()
                .GrantsFeat(FeatType.KillZoneTrait)
                .Description("Repeated rifle attacks against the same target stack +4% rifle damage for 20 seconds, up to +20%. Switching targets clears this bonus.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 45)
                .IncreasesStat(StatType.RepeatedTargetDamageSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.RepeatedTargetDamagePercentPerHit, 4)
                .IncreasesStat(StatType.RepeatedTargetDamagePercentMax, 20);
        }

        private void NeutralizingShot()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.NeutralizingShot)
                .Name("Neutralizing Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NeutralizingShotTrait)
                .Description("Tranq Cone, Pacification Field, and Stasis Volley remove one beneficial combat effect from affected enemies and inflict Disoriented for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 42)
                .IncreasesStat(StatType.RiflePacificationNeutralizingShot, 1);
        }

        private void OneShot()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.OneShot)
                .Name("One Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OneShot1)
                .Description("Deals weapon DMG + 70. On hit, the target is Marked for 45 seconds and takes 10% more physical ability damage.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 50);
        }

        private void Overwatch()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.Overwatch)
                .Name("Overwatch")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OverwatchTrait)
                .Description("Pacification control shots interrupt the target's current ability activation and inflict Foggy Mind for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 30)
                .IncreasesStat(StatType.RiflePacificationOverwatch, 1);
        }

        private void PacificationField()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.PacificationField)
                .Name("Pacification Field")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PacificationField1)
                .Description("Creates a field for 15 seconds. Enemies inside suffer -10% Attack and become Dazed for 2 seconds every 5 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 45);
        }

        private void PiercingRound()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.PiercingRound)
                .Name("Piercing Round")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PiercingRound1)
                .Description("Deals weapon DMG + 14 and inflicts Sunder, reducing Defense by 10% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PiercingRound2)
                .Description("Deals weapon DMG + 26 and inflicts Sunder, reducing Defense by 15% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PiercingRound3)
                .Description("Deals weapon DMG + 38 and inflicts Sunder, reducing Defense by 20% for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 40);
        }

        private void PinningFire()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.PinningFire)
                .Name("Pinning Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PinningFireTrait)
                .Description("Pacification control shots also inflict Dazed for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 12)
                .IncreasesStat(StatType.RiflePacificationPinningFireRank, 1)

                .AddPerkLevel()
                .Description("Pacification control shots also inflict Knockdown for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 28)
                .IncreasesStat(StatType.RiflePacificationPinningFireRank, 2);
        }

        private void ScopeCalibration()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.ScopeCalibration)
                .Name("Scope Calibration")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ScopeCalibrationTrait)
                .Description("Ranged critical hits deal +15% damage.")
                .IncreasesStat(StatType.RangedCriticalDamagePercentAdjustment, 15)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 20);
        }

        private void SniperStance()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.SniperStance)
                .Name("Sniper Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SniperStance1)
                .Description("While active, grants +20% Attack and +15% critical damage, but reduces Evasion and Defense by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 15);
        }

        private void SoftTarget()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.SoftTarget)
                .Name("Soft Target")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SoftTargetTrait)
                .Description("Deal +10% rifle damage to enemies affected by Disoriented, Dazed, or tranquilizer effects.")
                .IncreasesStat(StatType.DamageToDisorientedDazedTargetPercentAdjustment, 10)
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 22);
        }

        private void SpotterStance()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.SpotterStance)
                .Name("Spotter Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SpotterStance1)
                .Description("While active, grants +15% Accuracy and +15% Evasion against ranged attacks, but reduces Haste by 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 15);
        }

        private void StasisVolley()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.StasisVolley)
                .Name("Stasis Volley")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StasisVolley1)
                .Description("Enemies in a cone take weapon DMG + 20 and are briefly tranquilized, breaking on damage. On hit, targets suffer -10% Attack for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 50);
        }

        private void SteadyAim()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.SteadyAim)
                .Name("Steady Aim")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SteadyAimTrait)
                .Description("Rifle combat abilities gain +5% accuracy and +5% critical chance.")
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustmentSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustment, 5)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustmentSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustment, 5)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 2)

                .AddPerkLevel()
                .Description("Rifle combat abilities gain +15% accuracy and +5% critical chance. Aimed Shot cooldowns are reduced by 5 seconds.")
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustmentSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustment, 15)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustmentSkillType, (int)SkillType.Rifle)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustment, 5)
                .IncreasesStat(StatType.AbilityRecastDelayFlatAdjustmentPerkType, (int)PerkType.AimedShot)
                .IncreasesStat(StatType.AbilityRecastDelayFlatAdjustment, -5)
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 32);
        }

        private void SuppressiveLine()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.SuppressiveLine)
                .Name("Suppressive Line")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SuppressiveLine1)
                .Description("Channel for up to 6s. Deals weapon DMG + 6 to enemies in a 20m line every 2 seconds. Refreshes Disoriented for 4 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 25);
        }

        private void TranqCone()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.TranqCone)
                .Name("Tranq Cone")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TranqCone1)
                .Description("Tranquilizes up to 3 enemies in a cone for up to 8 seconds. Damage breaks the effect prematurely.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.TranqCone2)
                .Description("Tranquilizes up to 5 enemies in a cone for up to 10 seconds. Damage breaks the effect prematurely.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 38);
        }

        private void TranquilizerShot()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TranquilizerShot1)
                .Description("Your next attack tranquilizes the target for up to 8 seconds. Damage breaks the effect prematurely.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.TranquilizerShot2)
                .Description("Your next attack tranquilizes the target for up to 14 seconds. Damage breaks the effect prematurely.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 18);
        }

        private void VeteranTracker()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.VeteranTracker)
                .Name("Veteran Tracker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VeteranTrackerTrait)
                .Description("Rifle damage increases by 15% against enemies affected by any control effect.")
                .IncreasesStat(StatType.DamageToControlTargetPercentAdjustment, 15)
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 48);
        }
    }
}

