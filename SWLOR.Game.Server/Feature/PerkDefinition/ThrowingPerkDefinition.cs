using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ThrowingPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BleedersEye();
            BombardiersRhythm();
            BombardierStance();
            ClusterPouch();
            ClusterStorm();
            ConcussiveToss();
            DeadeyeMastery();
            DeadeyeStance();
            DeepWound();
            ExplosiveToss();
            FinishingToss();
            FireburstToss();
            FlashToss();
            MarkedTempo();
            MarkingToss();
            PerfectThrow();
            PiercingToss();
            PinningToss();
            RainOfSteel();
            ReturningGrip();
            RicochetToss();
            SaturationToss();
            SeveringToss();
            ShrapnelCasing();
            VolatilePayload();

            return _builder.Build();
        }

        private void BleedersEye()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.BleedersEye)
                .Name("Bleeder's Eye")

                .AddPerkLevel()
                .Description("Deal +12% Throwing damage to bleeding targets.")
                .IncreasesStat(StatType.DamageToBleedingTargetPercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 12 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 28);
        }

        private void BombardiersRhythm()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.BombardiersRhythm)
                .Name("Bombardier's Rhythm")

                .AddPerkLevel()
                .Description("Each enemy hit by a Throwing area ability grants +2% Attack for 10 seconds, up to +20%.")
                .IncreasesStat(StatType.ThrowingAreaAbilityAttackPercentPerTarget, 2)
                .IncreasesStat(StatType.ThrowingAreaAbilityAttackDurationSeconds, 10)
                .IncreasesStat(StatType.ThrowingAreaAbilityAttackPercentMax, 20)
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 40);
        }

        private void BombardierStance()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.BombardierStance)
                .Name("Bombardier Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BombardierStance1)
                .Description("While active, Throwing area abilities deal +15% damage, but Defense is reduced by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 15);
        }

        private void ClusterPouch()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.ClusterPouch)
                .Name("Cluster Pouch")

                .AddPerkLevel()
                .Description("Throwing combat abilities that hit 3 or more targets restore 4 STM.")
                .IncreasesStat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 3)
                .IncreasesStat(StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 4)
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 22);
        }

        private void ClusterStorm()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.ClusterStorm)
                .Name("Cluster Storm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ClusterStorm1)
                .Description("Throw three explosives at the target area. Each explosive deals weapon DMG + 12 to nearby enemies.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 35);
        }

        private void ConcussiveToss()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.ConcussiveToss)
                .Name("Concussive Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConcussiveToss1)
                .Description("Deals weapon DMG + 14 to enemies in the target area. Inflicts Dazed for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConcussiveToss2)
                .Description("Deals weapon DMG + 26 to enemies in the target area. Inflicts Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 30);
        }

        private void DeadeyeMastery()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.DeadeyeMastery)
                .Name("Deadeye Mastery")

                .AddPerkLevel()
                .Description("Throwing abilities against bleeding or disoriented targets have +15% critical chance.")
                .IncreasesStat(StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 15 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 48);
        }

        private void DeadeyeStance()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.DeadeyeStance)
                .Name("Deadeye Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DeadeyeStance1)
                .Description("While active, grants +15% accuracy and +15% critical chance, but reduces Evasion by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 15);
        }

        private void DeepWound()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.DeepWound)
                .Name("Deep Wound")

                .AddPerkLevel()
                .Description("Bleed effects you apply deal +25% damage and last 10 seconds longer.")
                .IncreasesStat(StatType.OutgoingBleedingDamagePercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 25 : 0)
                .IncreasesStat(StatType.OutgoingBleedingDurationBonusSeconds, creature => EquipmentPredicates.HasThrowing(creature) ? 10 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 42);
        }

        private void ExplosiveToss()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.ExplosiveToss)
                .Name("Explosive Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExplosiveToss1)
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 8.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExplosiveToss2)
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 16.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExplosiveToss3)
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 26.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExplosiveToss4)
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 38 and inflicts Exposed for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 42);
        }

        private void FinishingToss()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.FinishingToss)
                .Name("Finishing Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FinishingToss1)
                .Description("Deals weapon DMG + 40. Targets below 30% HP take an additional +30 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 45);
        }

        private void FireburstToss()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.FireburstToss)
                .Name("Fireburst Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FireburstToss1)
                .Description("Deals weapon DMG + 20 to enemies in the target area and inflicts Exposed for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 25);
        }

        private void FlashToss()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.FlashToss)
                .Name("Flash Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlashToss1)
                .Description("Deals weapon DMG + 6 to enemies in the target area. Inflicts Blind for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlashToss2)
                .Description("Deals weapon DMG + 22 to enemies in the target area. Inflicts Blind for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 38);
        }

        private void MarkedTempo()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.MarkedTempo)
                .Name("Marked Tempo")

                .AddPerkLevel()
                .Description("Critical hits against your marked target restore 6 STM.")
                .IncreasesStat(StatType.CriticalMarkedTargetStaminaRestore, creature => EquipmentPredicates.HasThrowing(creature) ? 6 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 32);
        }

        private void MarkingToss()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.MarkingToss)
                .Name("Marking Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MarkingToss1)
                .Description("Deals weapon DMG + 18 and marks the target for 12 seconds. Throwing damage against the marked target is increased by 10%.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 20);
        }

        private void PerfectThrow()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.PerfectThrow)
                .Name("Perfect Throw")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PerfectThrow1)
                .Description("Deals weapon DMG + 45. If the target is bleeding, also inflicts Hemorrhage, increasing damage taken by 10% for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 50);
        }

        private void PiercingToss()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.PiercingToss)
                .Name("Piercing Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PiercingToss1)
                .Description("Your next attack deals weapon DMG + 12 and inflicts Bleed for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 5)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PiercingToss2)
                .Description("Your next attack deals weapon DMG + 21 and inflicts Bleed for 60 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PiercingToss3)
                .Description("Your next attack deals weapon DMG + 34 and inflicts Bleed for 60 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 30);
        }

        private void PinningToss()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.PinningToss)
                .Name("Pinning Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PinningToss1)
                .Description("Your next attack deals weapon DMG + 8 and inflicts Disoriented for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PinningToss2)
                .Description("Your next attack deals weapon DMG + 18 and inflicts Disoriented for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PinningToss3)
                .Description("Your next attack deals weapon DMG + 30, inflicts Disoriented for 20 seconds, and reduces Evasion by an additional 15%.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 35);
        }

        private void RainOfSteel()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.RainOfSteel)
                .Name("Rain of Steel")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RainOfSteel1)
                .Description("All enemies in a large area of effect (sphere) take weapon DMG + 25 and suffer Bleed for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 50);
        }

        private void ReturningGrip()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.ReturningGrip)
                .Name("Returning Grip")

                .AddPerkLevel()
                .Description("After using a Throwing combat ability, your next auto-attack within 8 seconds deals +8 DMG.")
                .IncreasesStat(StatType.ThrowingAbilityUsedNextAutoAttackDamageBonus, 8)
                .IncreasesStat(StatType.ThrowingAbilityUsedNextAutoAttackDamageDurationSeconds, 8)
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 12);
        }

        private void RicochetToss()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.RicochetToss)
                .Name("Ricochet Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RicochetToss1)
                .Description("Your thrown weapon hits the target and up to 2 additional enemies within 5 meters for weapon DMG + 15 each.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RicochetToss2)
                .Description("Your thrown weapon hits the target and up to 4 additional enemies within 5 meters for weapon DMG + 24 each.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 40);
        }

        private void SaturationToss()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.SaturationToss)
                .Name("Saturation Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaturationToss1)
                .Description("Creates a target area for 12 seconds. Enemies inside take weapon DMG + 10 every 4 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 45);
        }

        private void SeveringToss()
        {
            _builder.Create(PerkCategoryType.ThrowingDeadeye, PerkType.SeveringToss)
                .Name("Severing Toss")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SeveringToss1)
                .Description("Deals weapon DMG + 32 and inflicts Hamstring for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 38);
        }

        private void ShrapnelCasing()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.ShrapnelCasing)
                .Name("Shrapnel Casing")

                .AddPerkLevel()
                .Description("Explosive Toss abilities inflict Bleed for 15 seconds.")
                .IncreasesStat(StatType.ExplosiveTossBleedDurationSeconds, creature => EquipmentPredicates.HasThrowing(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Throwing, 12)

                .AddPerkLevel()
                .Description("Bleed from Explosive Toss abilities lasts 30 seconds and Explosive Toss ignores 10% Defense.")
                .IncreasesStat(StatType.ExplosiveTossBleedDurationSeconds, creature => EquipmentPredicates.HasThrowing(creature) ? 30 : 0)
                .IncreasesStat(StatType.AbilityDefenseIgnorePercentAdjustmentPerkType, creature => EquipmentPredicates.HasThrowing(creature) ? (int)PerkType.ExplosiveToss : 0)
                .IncreasesStat(StatType.AbilityDefenseIgnorePercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Throwing, 32);
        }

        private void VolatilePayload()
        {
            _builder.Create(PerkCategoryType.ThrowingBombardier, PerkType.VolatilePayload)
                .Name("Volatile Payload")

                .AddPerkLevel()
                .Description("Critical hits with Explosive Toss abilities inflict Knockdown for 2 seconds.")
                .IncreasesStat(StatType.CriticalAbilityKnockdownPerkType, creature => EquipmentPredicates.HasThrowing(creature) ? (int)PerkType.ExplosiveToss : 0)
                .IncreasesStat(StatType.CriticalAbilityKnockdownDurationSeconds, creature => EquipmentPredicates.HasThrowing(creature) ? 2 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Throwing, 48);
        }
    }
}

