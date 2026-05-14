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
                .Description("Rifle abilities against Exposed or Sundered targets ignore an additional 15% Defense.")
                .IncreasesStat(StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 15 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 48);
        }

        private void BreachRound()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.BreachRound)
                .Name("Breach Round")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BreachRound1)
                .Description("Deals weapon DMG + 35 and ignores 25% of the target's Defense.")
                .IncreasesStat(StatType.AbilityDefenseIgnorePercentAdjustmentPerkType, (int)PerkType.BreachRound)
                .IncreasesStat(StatType.AbilityDefenseIgnorePercentAdjustment, 25)
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 35);
        }

        private void ContainmentNet()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.ContainmentNet)
                .Name("Containment Net")

                .AddPerkLevel()
                .Description("Enemies affected by your Disoriented effects suffer an additional -10% Evasion and -10% Attack.")
                .IncreasesStat(StatType.OutgoingDisorientedAttackPercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? -10 : 0)
                .IncreasesStat(StatType.OutgoingDisorientedEvasionPercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? -10 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 40);
        }

        private void CripplingShot()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.CripplingShot)
                .Name("Crippling Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingShot1)
                .Description("Your next attack deals weapon DMG + 12 and inflicts Disoriented for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingShot2)
                .Description("Your next attack deals weapon DMG + 22 and inflicts Disoriented for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingShot3)
                .Description("Your next attack deals weapon DMG + 34 and inflicts Disoriented for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 35);
        }

        private void DeadCenter()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.DeadCenter)
                .Name("Dead Center")

                .AddPerkLevel()
                .Description("Rifle critical hits restore 4 STM and cause your next Aimed Shot within 8 seconds to deal +10 DMG. This can only trigger once every 6 seconds.")
                .IncreasesStat(StatType.CriticalStaminaRestore, creature => EquipmentPredicates.HasRifle(creature) ? 4 : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.CriticalStaminaRestoreCooldownSeconds, creature => EquipmentPredicates.HasRifle(creature) ? 6 : 0)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusTriggerSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusPerkType, creature => EquipmentPredicates.HasRifle(creature) ? (int)PerkType.AimedShot : 0)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonus, creature => EquipmentPredicates.HasRifle(creature) ? 10 : 0)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusDurationSeconds, creature => EquipmentPredicates.HasRifle(creature) ? 8 : 0)
                .IncreasesStat(StatType.CriticalNextAbilityDamageBonusCooldownSeconds, creature => EquipmentPredicates.HasRifle(creature) ? 6 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 38);
        }

        private void ExposeWeakPoint()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.ExposeWeakPoint)
                .Name("Expose Weak Point")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExposeWeakPoint1)
                .Description("Deals weapon DMG + 20 and marks the target for 12 seconds. Physical attacks against the marked target deal +10% damage.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 30);
        }

        private void FieldSedatives()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.FieldSedatives)
                .Name("Field Sedatives")

                .AddPerkLevel()
                .Description("After a tranquilizer effect ends, the target's Attack is reduced by 10% for 10 seconds.")
                .IncreasesStat(StatType.TranquilizeExpiredAttackPercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? -10 : 0)
                .IncreasesStat(StatType.TranquilizeExpiredAttackDurationSeconds, creature => EquipmentPredicates.HasRifle(creature) ? 10 : 0)
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
                .GrantsFeat(FeatType.KillZone1)
                .Description("For 20 seconds, repeated attacks against the same target stack +4% rifle damage, up to +20%. Switching targets clears this bonus.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 45);
        }

        private void NeutralizingShot()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.NeutralizingShot)
                .Name("Neutralizing Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NeutralizingShot1)
                .Description("Deals weapon DMG + 30, removes one beneficial combat effect, and inflicts Disoriented for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 42);
        }

        private void OneShot()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.OneShot)
                .Name("One Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OneShot1)
                .Description("Deals weapon DMG + 100 to one target. If this defeats the target, restore 25 STM and gain +15% Attack for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 50);
        }

        private void Overwatch()
        {
            _builder.Create(PerkCategoryType.RiflePacification, PerkType.Overwatch)
                .Name("Overwatch")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Overwatch1)
                .Description("Deal weapon DMG + 20 and interrupt your target's current ability activation. Inflicts Foggy Mind for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 30);
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
                .GrantsFeat(FeatType.PinningFire1)
                .Description("Deals weapon DMG + 10 and inflicts Dazed for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PinningFire2)
                .Description("Deals weapon DMG + 18 to enemies in a line. Inflicts Knockdown for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 28);
        }

        private void ScopeCalibration()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.ScopeCalibration)
                .Name("Scope Calibration")

                .AddPerkLevel()
                .Description("Rifle critical hits deal +15% damage.")
                .IncreasesStat(StatType.CriticalDamagePercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 15 : 0)
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
                .Description("Deal +10% rifle damage to enemies affected by Disoriented, Dazed, or tranquilizer effects.")
                .IncreasesStat(StatType.DamageToDisorientedDazedTargetPercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 10 : 0)
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
                .Description("All enemies in a cone take weapon DMG + 25 and are tranquilized for up to 12 seconds. Damage breaks the effect prematurely.")
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 50);
        }

        private void SteadyAim()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.SteadyAim)
                .Name("Steady Aim")

                .AddPerkLevel()
                .Description("Rifle combat abilities gain +5% accuracy and +5% critical chance.")
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustmentSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 5 : 0)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustmentSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 5 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Rifle, 5)

                .AddPerkLevel()
                .Description("Aimed Shot cooldowns are reduced by 5 seconds and gain an additional +10% accuracy.")
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustmentSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 5 : 0)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustmentSkillType, creature => EquipmentPredicates.HasRifle(creature) ? (int)SkillType.Rifle : 0)
                .IncreasesStat(StatType.AbilityCriticalRatePercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 5 : 0)
                .IncreasesStat(StatType.AbilityHitChancePercentAdjustmentPerkType, creature => EquipmentPredicates.HasRifle(creature) ? (int)PerkType.AimedShot : 0)
                .IncreasesStat(StatType.TargetedAbilityHitChancePercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 10 : 0)
                .IncreasesStat(StatType.AbilityRecastDelayFlatAdjustmentPerkType, creature => EquipmentPredicates.HasRifle(creature) ? (int)PerkType.AimedShot : 0)
                .IncreasesStat(StatType.AbilityRecastDelayFlatAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? -5 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Rifle, 32);
        }

        private void SuppressiveLine()
        {
            _builder.Create(PerkCategoryType.RifleMarksman, PerkType.SuppressiveLine)
                .Name("Suppressive Line")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SuppressiveLine1)
                .Description("Deals weapon DMG + 22 to enemies in a line. Inflicts Disoriented for 12 seconds.")
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
                .RequirementSkill(SkillType.Rifle, 5)

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
                .Description("Rifle damage increases by 15% against enemies affected by any control effect.")
                .IncreasesStat(StatType.DamageToControlTargetPercentAdjustment, creature => EquipmentPredicates.HasRifle(creature) ? 15 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Rifle, 48);
        }
    }
}

