using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class VibrobladePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BerserkerStance();
            Bloodseeker();
            BloodFrenzy();
            Carve();
            CoveringStrike();
            CrimsonFury();
            DefensiveStance();
            Executioner();
            FortifiedPosition();
            GuardiansRiposte();
            HackingBlade();
            Invincible();
            Alacrity();
            Bulwark();
            RendingStrike();
            RiotBlade();
            SavageCleave();
            SavageReflexes();
            ShieldBash();
            ShieldTraining();
            ShieldWall();
            Unbreakable();

            return _builder.Build();
        }

        private void BerserkerStance()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.BerserkerStance)
                .Name("Berserker Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BerserkerStance1)
                .Description("While active, grants +15% Attack, +10% Haste, -20% Defense, and -20% Force Defense.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 15)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BerserkerStance2)
                .Description("While active, grants +25% Attack, +15% Haste, -20% Defense, and -20% Force Defense.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 48);
        }

        private void BloodFrenzy()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.BloodFrenzy)
                .Name("Blood Frenzy")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BloodFrenzyTrait)
                .Description("Defeating an enemy restores 15 STM and grants +10% Haste for 30 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyStaminaRestore, 15)
                .IncreasesStat(StatType.DefeatedEnemyAttackDelayReductionPercent, 10)
                .IncreasesStat(StatType.DefeatedEnemyAttackDelayReductionDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 50)
                .RequirementQuest(BloodFrenzyQuestDefinition.FinalQuestId);
        }

        private void Bloodseeker()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Bloodseeker)
                .Name("Bloodseeker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BloodseekerTrait)
                .Description("Gain +10% Attack against bleeding targets.")
                .IncreasesStat(StatType.AttackToBleedingTargetPercentAdjustment, 10)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 40);
        }

        private void Carve()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Carve)
                .Name("Carve")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Carve1)
                .Description("Deals weapon DMG + 35, applies Hemorrhage which increases the damage your target takes by 10% for 12 seconds")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 45);
        }

        private void CoveringStrike()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.CoveringStrike)
                .Name("Covering Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringStrike1)
                .Description("Strike in a line for weapon DMG + 20. Enemies hit generate +25% Enmity toward you for 12s.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 32);
        }

        private void CrimsonFury()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.CrimsonFury)
                .Name("Crimson Fury")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrimsonFuryTrait)
                .Description("Each bleeding enemy within 10m grants you +3% Attack (max +15%).")
                .IncreasesStat(StatType.NearbyStatusTargetAttackPercentPerTarget, 3)
                .IncreasesStat(StatType.NearbyStatusTargetAttackRadiusMeters, 10)
                .IncreasesStat(StatType.NearbyStatusTargetAttackPercentMaximum, 15)
                .IncreasesStat(StatType.NearbyStatusTargetAttackStatusCategory, (int)StatusEffectCategory.Bleeding)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 42);
        }

        private void DefensiveStance()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.DefensiveStance)
                .Name("Defensive Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DefensiveStance1)
                .Description("While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DefensiveStance2)
                .Description("While active, grants +30% to Enmity generation, +20% Defense, +20% Force Defense, -20% Attack, and -20% Force Attack")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 45);
        }

        private void Executioner()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Executioner)
                .Name("Executioner")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExecutionerTrait)
                .Description("Deal +15% damage to targets below 30% HP.")
                .IncreasesStat(StatType.TargetLowHPDamageThresholdPercent, 30)
                .IncreasesStat(StatType.TargetLowHPDamagePercentAdjustment, 15)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 32);
        }

        private void FortifiedPosition()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.FortifiedPosition)
                .Name("Fortified Position")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortifiedPositionTrait)
                .Description("Grants +8 Mind Resistance rating, +8 Trauma Resistance rating, and +8 Mobility Resistance rating.")
                .IncreasesStat(StatType.MindResistance, 2)
                .IncreasesStat(StatType.TraumaResistance, 2)
                .IncreasesStat(StatType.MobilityResistance, 2)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 15)

                .AddPerkLevel()
                .Description("Grants +15 Mind Resistance rating, +15 Trauma Resistance rating, and +15 Mobility Resistance rating total.")
                .IncreasesStat(StatType.MindResistance, 4)
                .IncreasesStat(StatType.TraumaResistance, 4)
                .IncreasesStat(StatType.MobilityResistance, 4)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 38);
        }

        private void GuardiansRiposte()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.GuardiansRiposte)
                .Name("Guardian's Riposte")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansRiposteTrait)
                .Description("Receive Guardian's Riposte after deflecting an attack with a shield. Your next attack within 18 seconds deals +10 DMG.")
                .IncreasesStat(StatType.DeflectionNextSkillAbilityDamageBonus, creature => EquipmentPredicates.HasOffHandShield(creature) ? 10 : 0)
                .IncreasesStat(StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds, creature => EquipmentPredicates.HasOffHandShield(creature) ? 18 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 30);
        }

        private void HackingBlade()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.HackingBlade)
                .Name("Hacking Blade")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HackingBlade1)
                .Description("Your next attack deals an additional 8 DMG and inflicts Bleed for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.HackingBlade2)
                .Description("Your next attack deals an additional 18 DMG and inflicts Bleed for 60 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.HackingBlade3)
                .Description("Your next attack deals an additional 28 DMG and inflicts Bleed for 60 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 35);
        }

        private void Invincible()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Invincible)
                .Name("Invincible")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Invincible1)
                .Description("For 45 seconds, you take 50% less physical damage and are immune to Knockdown and Daze.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 50);
        }

        private void Alacrity()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Alacrity)
                .Name("Alacrity")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AlacrityTrait)
                .Description("Restore 4 STM when your shield deflects an attack.")
                .IncreasesStat(StatType.DeflectionStaminaRestore, creature => EquipmentPredicates.HasOffHandShield(creature) ? 4 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 20);
        }

        private void Bulwark()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Bulwark)
                .Name("Bulwark")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkTrait)
                .Description("Grants +15 Shield Deflection.")
                .IncreasesStat(StatType.ShieldDeflection, creature => EquipmentPredicates.HasOffHandShield(creature) ? 15 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 10)

                .AddPerkLevel()
                .Description("Grants +25 Shield Deflection total.")
                .IncreasesStat(StatType.ShieldDeflection, creature => EquipmentPredicates.HasOffHandShield(creature) ? 25 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 28)

                .AddPerkLevel()
                .Description("Grants +35 Shield Deflection total.")
                .IncreasesStat(StatType.ShieldDeflection, creature => EquipmentPredicates.HasOffHandShield(creature) ? 35 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 40);
        }

        private void RendingStrike()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.RendingStrike)
                .Name("Rending Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RendingStrike1)
                .Description("Deals weapon DMG + 18. Inflicts Exposed which reduces Defense by 15% for 10s.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RendingStrike2)
                .Description("Deals weapon DMG + 32. Inflicts Exposed which reduces Defense by 25% for 12s.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 38);
        }

        private void RiotBlade()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.RiotBlade)
                .Name("Riot Blade")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade1)
                .Description("Instantly deals weapon DMG + 15 to your target.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 10)

                .AddPerkLevel()
                .Description("Riot Blade also deals +12 DMG to nearby enemies.")
                .IncreasesStat(StatType.RiotBladeSecondaryDamageBonus, 12)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade2)
                .Description("Instantly deals weapon DMG + 30 to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade3)
                .Description("Instantly deals weapon DMG + 45 to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 28);
        }

        private void SavageCleave()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.SavageCleave)
                .Name("Savage Cleave")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageCleave1)
                .Description("Strike all enemies in front for weapon DMG + 25.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageCleave2)
                .Description("Savage Cleave deals +20 DMG to nearby enemies and restores 2 STM per secondary target hit, up to 8 STM.")
                .IncreasesStat(StatType.SavageCleaveSecondaryDamageBonus, 20)
                .IncreasesStat(StatType.SavageCleaveSecondaryTargetStaminaRestore, 2)
                .IncreasesStat(StatType.SavageCleaveSecondaryTargetStaminaRestoreMaximum, 8)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 30);
        }

        private void SavageReflexes()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.SavageReflexes)
                .Name("Savage Reflexes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageReflexesTrait)
                .Description("Auto-attacks have 10% chance to deal +8 DMG.")
                .IncreasesStat(StatType.AutoAttackDamageBonusChance, 10)
                .IncreasesStat(StatType.AutoAttackDamageBonus, 8)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 2);
        }

        private void ShieldBash()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldBash)
                .Name("Shield Bash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash1)
                .Description("Bashes your current attack target for 12 DMG and inflicts Dazed for 3 seconds. Requires an active attack target.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash2)
                .Description("Bashes your current attack target for 24 DMG and inflicts Dazed for 6 seconds. Requires an active attack target.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash3)
                .Description("Bashes your current attack target for 36 DMG, inflicts Stunned for 3 seconds, and inflicts Dazed for 6 seconds. Requires an active attack target.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 35);
        }

        private void ShieldTraining()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldTraining)
                .Name("Shield Training")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldTrainingTrait)
                .Description("When you successfully deflect an attack with a shield, gain +3% Evasion and +3% Enmity for 10 seconds.")
                .IncreasesStat(StatType.DeflectionEvasionPercentAdjustment, creature => EquipmentPredicates.HasOffHandShield(creature) ? 3 : 0)
                .IncreasesStat(StatType.DeflectionEvasionEnmityPercentAdjustment, creature => EquipmentPredicates.HasOffHandShield(creature) ? 3 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 2);
        }

        private void ShieldWall()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldWall)
                .Name("Shield Wall")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldWall1)
                .Description("Channel for up to 6s. Allies within 5m gain +15% Physical Defense, you gain +25% Enmity for 1 minute.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 25);
        }

        private void Unbreakable()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Unbreakable)
                .Name("Unbreakable")

                .AddPerkLevel()
                .GrantsFeat(FeatType.UnbreakableTrait)
                .Description("When reduced below 25% HP, gain +40% Physical Defense for 10s. Once per 5min.")
                .IncreasesStat(StatType.LowHPPhysicalDefenseThresholdPercent, 25)
                .IncreasesStat(StatType.LowHPPhysicalDefensePercentAdjustment, 40)
                .IncreasesStat(StatType.LowHPPhysicalDefenseDurationSeconds, 10)
                .IncreasesStat(StatType.LowHPPhysicalDefenseCooldownSeconds, 300)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 42);
        }

    }
}

