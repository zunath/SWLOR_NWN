using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class SpearPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AdaptivePrecisionStrike();
            BreachStrike();
            CalmingStance();
            CripplingDefense();
            DisablingStrike();
            DisruptionExpert();
            DisruptionField();
            ErosionStrike();
            FlankingBarrage();
            Flanking();
            FlankingStance();
            ForceNullification();
            ForcePiercing();
            ForceSuppression();
            ForceWarding();
            Forcebane();
            FractureStrike();
            HamperingBarrage();
            ImprovedAttentiveness();
            InterruptionStrike();
            LateralStrike();
            OpportunistFlow();
            PerceptiveStance();
            RestorationStrike();
            SideAssault();
            SweepingFlank();
            TotalForceDenial();

            return _builder.Build();
        }


        private void AdaptivePrecisionStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.AdaptivePrecisionStrike)
                .Name("Adaptive Precision Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AdaptivePrecisionStrikeTrait)
                .Description("Attacks from the side have a 5% chance to bypass 35% of your target's Evasion. This chance increases by 1% per PER. (Maximum 30%)")
                .IncreasesStat(StatType.SideAttackEvasionIgnoreChance, 5)
                .IncreasesStat(StatType.SideAttackEvasionIgnoreChanceScalingAbility, (int)AbilityType.Perception + 1)
                .IncreasesStat(StatType.SideAttackEvasionIgnoreChanceMaximum, 30)
                .IncreasesStat(StatType.SideAttackEvasionIgnorePercent, 35)
                .Price(4)
                .RequirementSkill(SkillType.Spear, 48);
        }


        private void BreachStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.BreachStrike)
                .Name("Breach Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BreachStrikeTrait)
                .Description("Spear Damage flanking abilities inflict Breach, reducing Evasion and Defense by 20% for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 18)
                .IncreasesStat(StatType.SpearDamageBreachStrike, 1);
        }


        private void CalmingStance()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.CalmingStance)
                .Name("Calming Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CalmingStance1)
                .Description("While active, your STM regenerates by 3 every second. Your attack, force attack, defense, and force defense are reduced by 40%.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 45);
        }


        private void CripplingDefense()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.CripplingDefense)
                .Name("Crippling Defense")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CripplingDefenseTrait)
                .Description("Spear Damage area abilities reduce affected targets' Physical Defense and Force Defense by 15% for 45 seconds. Restore 15 STM when this affects at least two enemies.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 50)
                .IncreasesStat(StatType.SpearDamageCripplingDefense, 1)
                .IncreasesStat(StatType.SpearDamageCripplingDefenseStaminaRestore, 15)
                .IncreasesStat(StatType.SpearDamageCripplingDefenseMinimumTargets, 2);
        }


        private void DisablingStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.DisablingStrike)
                .Name("Disabling Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisablingStrike1)
                .Description("Your next attack deals +12 DMG and inflicts Force Disruption and Foggy Mind for 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisablingStrike2)
                .Description("Your next attack deals +18 DMG and inflicts Force Disruption and Foggy Mind for 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisablingStrike3)
                .Description("Your next attack deals +26 DMG and inflicts Force Disruption and Foggy Mind for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 40);
        }


        private void DisruptionExpert()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.DisruptionExpert)
                .Name("Disruption Expert")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisruptionExpertTrait)
                .Description("Your Force Disruption effects last 50% longer and reduce Force Defense by an additional 10%.")
                .IncreasesStat(StatType.OutgoingForceDisruptionDurationPercentAdjustment, 50)
                .IncreasesStat(StatType.OutgoingForceDisruptionForceDefensePercentAdjustment, -10)
                .Price(4)
                .RequirementSkill(SkillType.Spear, 42);
        }


        private void DisruptionField()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.DisruptionField)
                .Name("Disruption Field")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DisruptionField1)
                .Description("Forms a visible 5m disruption field at a targeted location. Enemies within the sphere lose 5% FP and 5% STM per second for 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 25);
        }


        private void ErosionStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ErosionStrike)
                .Name("Erosion Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ErosionStrikeTrait)
                .Description("When you damage your target, they receive Force Erosion which reduces Force Defense by 10% for 12 seconds.")
                .IncreasesStat(StatType.DamageDealtForceErosionDurationSeconds, 12)
                .Price(2)
                .RequirementSkill(SkillType.Spear, 2)

                .AddPerkLevel()
                .Description("The Force Erosion effect additionally reduces FP and STM by 2 every second.")
                .IncreasesStat(StatType.DamageDealtForceErosionDurationSeconds, 12)
                .IncreasesStat(StatType.DamageDealtForceErosionFPLossPerTick, 2)
                .IncreasesStat(StatType.DamageDealtForceErosionStaminaLossPerTick, 2)
                .Price(2)
                .RequirementSkill(SkillType.Spear, 32);
        }


        private void FlankingBarrage()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.FlankingBarrage)
                .Name("Flanking Barrage")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlankingBarrage1)
                .Description("Deal weapon DMG + 16 to your target. From the side, deal +20 DMG and reduce their Attack by 12% for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 20);
        }


        private void Flanking()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.Flanking)
                .Name("Flanking")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlankingTrait)
                .Description("Attacks from the side deal +10% damage.")
                .IncreasesStat(StatType.SideAttackDamagePercentAdjustment, 10)
                .Price(3)
                .RequirementSkill(SkillType.Spear, 2)

                .AddPerkLevel()
                .Description("Attacks from the side have +10% accuracy and +8% critical chance.")
                .IncreasesStat(StatType.SideAttackDamagePercentAdjustment, 10)
                .IncreasesStat(StatType.SideAttackHitChancePercentAdjustment, 10)
                .IncreasesStat(StatType.SideAttackCriticalRatePercentAdjustment, 8)
                .Price(2)
                .RequirementSkill(SkillType.Spear, 32);
        }


        private void FlankingStance()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.FlankingStance)
                .Name("Flanking Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlankingStance1)
                .Description("While active, attacks from the side deal +20% damage and have +15% accuracy. Your defense and force defense are reduced by 25%.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 12);
        }


        private void ForceNullification()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForceNullification)
                .Name("Force Nullification")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceNullificationTrait)
                .Description("Spear Disabler interrupt abilities disable the target's Force abilities for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 30)
                .IncreasesStat(StatType.SpearDisablerForceNullification, 1);
        }


        private void ForcePiercing()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForcePiercing)
                .Name("Force Piercing")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForcePiercingTrait)
                .Description("Critical hit chance increases by 5%. Additionally, critical hits reduce FP and STM by 10% of the damage dealt.")
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, 5)
                .IncreasesStat(StatType.CriticalTargetFPLossPercentOfDamage, 10)
                .IncreasesStat(StatType.CriticalTargetStaminaLossPercentOfDamage, 10)
                .Price(4)
                .RequirementSkill(SkillType.Spear, 18);
        }


        private void ForceSuppression()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForceSuppression)
                .Name("Force Suppression")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceSuppression1)
                .Description("Deals weapon DMG + 20 and reduces your target's Attack by 10% and Force Attack by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 20);
        }


        private void ForceWarding()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.ForceWarding)
                .Name("Force Warding")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceWardingTrait)
                .Description("Increases Force Evasion by 15%.")
                .IncreasesStat(StatType.IncomingAbilityHitChancePercentAdjustmentSkillType, (int)SkillType.Force)
                .IncreasesStat(StatType.IncomingAbilityHitChancePercentAdjustment, -15)
                .Price(3)
                .RequirementSkill(SkillType.Spear, 45)

                .AddPerkLevel()
                .Description("When a Force ability is evaded, you receive the Force Warding buff which increases your Force Defense by 30% for 20 seconds and restores 15 STM. This can only trigger once every 30 seconds.")
                .IncreasesStat(StatType.IncomingAbilityHitChancePercentAdjustmentSkillType, (int)SkillType.Force)
                .IncreasesStat(StatType.IncomingAbilityHitChancePercentAdjustment, -15)
                .IncreasesStat(StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 30)
                .IncreasesStat(StatType.ForceAbilityEvadedDurationSeconds, 20)
                .IncreasesStat(StatType.ForceAbilityEvadedStaminaRestore, 15)
                .IncreasesStat(StatType.ForceAbilityEvadedCooldownSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Spear, 48);
        }


        private void Forcebane()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.Forcebane)
                .Name("Forcebane")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForcebaneTrait)
                .Description("Spear Disabler suppression abilities reduce affected targets' FP recovery by 75% for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 50)
                .IncreasesStat(StatType.SpearDisablerForcebane, 1);
        }


        private void FractureStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.FractureStrike)
                .Name("Fracture Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FractureStrikeTrait)
                .Description("Disruption Field and Total Force Denial inflict Fractured Focus, doubling affected targets' FP costs for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 38)
                .IncreasesStat(StatType.SpearDisablerFractureStrike, 1);
        }


        private void HamperingBarrage()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.HamperingBarrage)
                .Name("Hampering Barrage")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HamperingBarrage1)
                .Description("Deal weapon DMG + 30 to all enemies within area of effect (cone). Inflicts Disoriented for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 40);
        }


        private void ImprovedAttentiveness()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.ImprovedAttentiveness)
                .Name("Improved Attentiveness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImprovedAttentivenessTrait)
                .Description("While one of your Spear Damage stances is active, party members other than you gain +5% physical and Force ability hit chance.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 28)
                .IncreasesStat(StatType.SpearDamageImprovedAttentiveness, 1);
        }


        private void InterruptionStrike()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.InterruptionStrike)
                .Name("Interruption Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.InterruptionStrike1)
                .Description("Interrupts your target's ability activation and inflicts Foggy Mind for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.InterruptionStrike2)
                .Description("Deals weapon DMG + 20, interrupts your target's ability activation, and inflicts Foggy Mind for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 28);
        }


        private void LateralStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.LateralStrike)
                .Name("Lateral Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LateralStrikeTrait)
                .Description("Spear attacks restore 2 STM. Side attacks restore an additional 2 STM. Each restore can only trigger once every 4 seconds.")
                .IncreasesStat(StatType.DamageDealtStaminaRestoreSkillType, (int)SkillType.Spear)
                .IncreasesStat(StatType.DamageDealtStaminaRestore, 2)
                .IncreasesStat(StatType.DamageDealtStaminaRestoreCooldownSeconds, 4)
                .IncreasesStat(StatType.SideAttackStaminaRestore, 2)
                .IncreasesStat(StatType.SideAttackStaminaRestoreCooldownSeconds, 4)
                .Price(3)
                .RequirementSkill(SkillType.Spear, 8)

                .AddPerkLevel()
                .Description("Spear attacks restore 3 STM. Side attacks restore an additional 3 STM. Each restore can only trigger once every 4 seconds.")
                .IncreasesStat(StatType.DamageDealtStaminaRestoreSkillType, (int)SkillType.Spear)
                .IncreasesStat(StatType.DamageDealtStaminaRestore, 3)
                .IncreasesStat(StatType.DamageDealtStaminaRestoreCooldownSeconds, 4)
                .IncreasesStat(StatType.SideAttackStaminaRestore, 3)
                .IncreasesStat(StatType.SideAttackStaminaRestoreCooldownSeconds, 4)
                .Price(2)
                .RequirementSkill(SkillType.Spear, 22);
        }


        private void OpportunistFlow()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.OpportunistsFlow)
                .Name("Opportunist's Flow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OpportunistsFlowTrait)
                .Description("After dealing Spear damage, your next attack's delay is 10% quicker for 18 seconds. Side attacks grant an additional 10%.")
                .IncreasesStat(StatType.DamageDealtAttackDelayReductionSkillType, (int)SkillType.Spear)
                .IncreasesStat(StatType.DamageDealtAttackDelayReductionPercent, 10)
                .IncreasesStat(StatType.DamageDealtAttackDelayReductionDurationSeconds, 18)
                .IncreasesStat(StatType.SideAttackDelayReductionPercent, 10)
                .IncreasesStat(StatType.SideAttackDelayReductionDurationSeconds, 18)
                .Price(4)
                .RequirementSkill(SkillType.Spear, 35);
        }


        private void PerceptiveStance()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.PerceptiveStance)
                .Name("Perceptive Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PerceptiveStance1)
                .Description("While active, gain +10% critical chance and +15% critical damage. Additionally, attacks have a 10% chance to interrupt ability activation. Chance to interrupt increases by 1% per PER. (Maximum 30%)")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 15);
        }


        private void RestorationStrike()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.RestorationStrike)
                .Name("Restoration Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RestorationStrikeTrait)
                .Description("Critical hit chance increases by 10%. Critical hits restore 4 STM once every 6 seconds. If you were at the side of your target, critical hits have a 35% chance to restore an additional 8 STM.")
                .IncreasesStat(StatType.CriticalRatePercentAdjustment, 10)
                .IncreasesStat(StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Spear)
                .IncreasesStat(StatType.CriticalStaminaRestore, 4)
                .IncreasesStat(StatType.CriticalStaminaRestoreCooldownSeconds, 6)
                .IncreasesStat(StatType.CriticalSideAttackStaminaRestoreChance, 35)
                .IncreasesStat(StatType.CriticalSideAttackStaminaRestore, 8)
                .Price(3)
                .RequirementSkill(SkillType.Spear, 38);
        }


        private void SideAssault()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.SideAssault)
                .Name("Side Assault")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SideAssault1)
                .Description("Your next attack deals +12 DMG. If you are facing the side of your target, this increases to +16 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.Spear, 15)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SideAssault2)
                .Description("Your next attack deals +25 DMG. If you are facing the side of your target, this increases to +35 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SideAssault3)
                .Description("Your next attack deals +35 DMG. If you are facing the side of your target, this increases to +50 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 42);
        }


        private void SweepingFlank()
        {
            _builder.Create(PerkCategoryType.SpearDamage, PerkType.SweepingFlank)
                .Name("Sweeping Flank")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SweepingFlank1)
                .Description("Deal weapon DMG + 18 to all enemies within area of effect (cone). Inflicts Exposed, which reduces defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Spear, 25);
        }


        private void TotalForceDenial()
        {
            _builder.Create(PerkCategoryType.SpearDisabler, PerkType.TotalForceDenial)
                .Name("Total Force Denial")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TotalForceDenial1)
                .Description("Deal weapon DMG + 28 to all enemies in area of effect (cone) and inflicts Force Disruption and Foggy Mind for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Spear, 35);
        }
    }
}

