using System.Collections.Generic;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class VibrobladePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BerserkerStance();
            BloodFrenzy();
            CoveringStrike();
            DefensiveStance();
            Executioner();
            Fortification();
            Invincible();
            Alacrity();
            Bulwark();
            RendingStrike();
            RiotBlade();
            Rundown();
            FollowThrough();
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
                .Description("While active, grants +25% Attack, +15% Haste, -20% Defense, and -20% Force Defense.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 20);
        }

        private void BloodFrenzy()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.BloodFrenzy)
                .Name("Blood Frenzy")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BloodFrenzyTrait)
                .Description("Defeating an enemy restores 10 STM and grants +8% Haste for 30 seconds.")
                .IncreasesStat(StatType.DefeatedEnemyStaminaRestore, 10)
                .IncreasesStat(StatType.DefeatedEnemyAttackDelayReductionPercent, 8)
                .IncreasesStat(StatType.DefeatedEnemyAttackDelayReductionDurationSeconds, 30)
                .Price(6)
                .RequirementSkill(SkillType.Vibroblade, 50)
                .RequirementQuest(BloodFrenzyQuestDefinition.FinalQuestId);
        }

        private void CoveringStrike()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.CoveringStrike)
                .Name("Covering Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringStrike1)
                .Description("Strike all enemies within 5m for weapon DMG + 15. Enemies hit generate +25% Enmity toward you for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringStrike2)
                .Description("Strike all enemies within 5m for weapon DMG + 25. Enemies hit generate +25% Enmity toward you for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringStrike3)
                .Description("Strike all enemies within 5m for weapon DMG + 30. Enemies hit generate +25% Enmity toward you for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 38);
        }

        private void DefensiveStance()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.DefensiveStance)
                .Name("Defensive Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DefensiveStance1)
                .Description("While active, grants +30% Enmity generation, +20% Defense and +20% Force Defense, -20% Attack, and -20% Force Attack.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 20);
        }

        private void Executioner()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Executioner)
                .Name("Executioner")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ExecutionerTrait)
                .Description("Deal +8% damage to targets below 25% HP.")
                .IncreasesStat(StatType.TargetLowHPDamageThresholdPercent, 25)
                .IncreasesStat(StatType.TargetLowHPDamagePercentAdjustment, 8)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 5)

                .AddPerkLevel()
                .Description("Deal +10% damage to targets below 25% HP.")
                .IncreasesStat(StatType.TargetLowHPDamageThresholdPercent, 25)
                .IncreasesStat(StatType.TargetLowHPDamagePercentAdjustment, 10)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 35);
        }

        private void Fortification()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Fortification)
                .Name("Fortification")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FortificationTrait)
                .Description("Grants +15 Mind Resistance rating, +15 Trauma Resistance rating, and +15 Mobility Resistance rating.")
                .IncreasesStat(StatType.MindResistance, 15)
                .IncreasesStat(StatType.TraumaResistance, 15)
                .IncreasesStat(StatType.MobilityResistance, 15)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 15);
        }

        private void Invincible()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Invincible)
                .Name("Invincible")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Invincible1)
                .Description("For 45 seconds, you take 50% less physical damage and are immune to Knockdown and Dazed.")
                .Price(6)
                .RequirementSkill(SkillType.Vibroblade, 50)
                .RequirementQuest(VibrobladeCapstoneQuestDefinition.InvincibleMasteryQuestId);
        }

        private void Alacrity()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Alacrity)
                .Name("Alacrity")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AlacrityTrait)
                .Description("Restore 4 STM when your shield deflects an attack. This can only trigger once every 6 seconds.")
                .IncreasesStat(StatType.ShieldDeflectionStaminaRestore, creature => EquipmentPredicates.HasOffHandShield(creature) ? 4 : 0)
                .IncreasesStat(StatType.ShieldDeflectionStaminaRestoreCooldownSeconds, creature => EquipmentPredicates.HasOffHandShield(creature) ? 6 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 25);
        }

        private void Bulwark()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Bulwark)
                .Name("Bulwark")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BulwarkTrait)
                .Description("Grants +15 Shield Deflection and +8% Physical Defense while a shield is equipped.")
                .IncreasesStat(StatType.ShieldDeflection, creature => EquipmentPredicates.HasOffHandShield(creature) ? 15 : 0)
                .IncreasesStat(StatType.ShieldEquippedPhysicalDefensePercentAdjustment, 8)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 8)

                .AddPerkLevel()
                .Description("Grants +25 Shield Deflection and +10% Physical Defense while a shield is equipped.")
                .IncreasesStat(StatType.ShieldDeflection, creature => EquipmentPredicates.HasOffHandShield(creature) ? 25 : 0)
                .IncreasesStat(StatType.ShieldEquippedPhysicalDefensePercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 22)

                .AddPerkLevel()
                .Description("Grants +35 Shield Deflection and +12% Physical Defense while a shield is equipped.")
                .IncreasesStat(StatType.ShieldDeflection, creature => EquipmentPredicates.HasOffHandShield(creature) ? 35 : 0)
                .IncreasesStat(StatType.ShieldEquippedPhysicalDefensePercentAdjustment, 12)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 45);
        }

        private void RendingStrike()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.RendingStrike)
                .Name("Rending Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RendingStrike1)
                .Description("Deals weapon DMG + 18. Inflicts Exposed which reduces Defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RendingStrike2)
                .Description("Deals weapon DMG + 32. Inflicts Exposed which reduces Defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 32);
        }

        private void RiotBlade()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.RiotBlade)
                .Name("Riot Blade")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade1)
                .Description("On your next hit, deal weapon DMG + 10.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade2)
                .Description("On your next hit, deal weapon DMG + 15.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade3)
                .Description("On your next hit, deal weapon DMG + 20.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RiotBlade4)
                .Description("On your next hit, deal weapon DMG + 25.")
                .Price(5)
                .RequirementSkill(SkillType.Vibroblade, 40);
        }

        private void Rundown()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Rundown)
                .Name("Rundown")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RundownTrait)
                .Description("Each consecutive melee attack against the same target grants Rundown, giving +1 DMG to auto-attack against that target, up to five stacks.")
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageBonusPerHit, 1)
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageBonusMax, 5)
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageStatusEffectIcon, (int)EffectIconType.RundownStatusEffect)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 8)

                .AddPerkLevel()
                .Description("Each consecutive melee attack against the same target grants Rundown, giving +2 DMG to auto-attack against that target, up to five stacks.")
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageBonusPerHit, 2)
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageBonusMax, 10)
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageStatusEffectIcon, (int)EffectIconType.RundownStatusEffect)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 22)

                .AddPerkLevel()
                .Description("Each consecutive melee attack against the same target grants Rundown, giving +3 DMG to auto-attack against that target, up to five stacks.")
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageBonusPerHit, 3)
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageBonusMax, 15)
                .IncreasesStat(StatType.MeleeRepeatedTargetDamageStatusEffectIcon, (int)EffectIconType.RundownStatusEffect)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 45);
        }

        private void FollowThrough()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.FollowThrough)
                .Name("Follow-Through")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FollowThroughTrait)
                .Description("Every third melee auto-attack deals an additional +10 Damage.")
                .IncreasesStat(StatType.MeleeAutoAttackCycleRequiredCount, 3)
                .IncreasesStat(StatType.MeleeAutoAttackCycleDamage, 10)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 25);
        }

        private void SavageCleave()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.SavageCleave)
                .Name("Savage Cleave")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageCleave1)
                .Description("Deal weapon DMG + 10 in a 5m radius around you.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageCleave2)
                .Description("Deal weapon DMG + 15 in a 5m radius around you and restore 2 STM per secondary target hit.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageCleave3)
                .Description("Deal weapon DMG + 20 in a 5m radius around you and restore 2 STM per secondary target hit.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 38);
        }

        private void SavageReflexes()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.SavageReflexes)
                .Name("Savage Reflexes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageReflexesTrait)
                .Description("Auto-attacks have a 15% chance to deal +10 DMG.")
                .IncreasesStat(StatType.AutoAttackDamageBonusChance, 15)
                .IncreasesStat(StatType.AutoAttackDamageBonus, 10)
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 15);
        }

        private void ShieldBash()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldBash)
                .Name("Shield Bash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash1)
                .Description("On your next hit, deal weapon DMG and an additional instance of damage equal to 4% of your Physical Defense.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash2)
                .Description("On your next hit, deal weapon DMG and an additional instance of damage equal to 6% of your Physical Defense.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash3)
                .Description("On your next hit, deal weapon DMG and an additional instance of damage equal to 8% of your Physical Defense.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldBash4)
                .Description("On your next hit, deal weapon DMG and an additional instance of damage equal to 10% of your Physical Defense.")
                .Price(5)
                .RequirementSkill(SkillType.Vibroblade, 40);
        }

        private void ShieldTraining()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldTraining)
                .Name("Shield Training")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldTrainingTrait)
                .Description("When you deflect an attack with a shield, reduce the active cooldown of Shield Bash by 2 seconds.")
                .IncreasesStat(StatType.DeflectionRecastReductionGroupId, creature => EquipmentPredicates.HasOffHandShield(creature) ? (int)RecastGroup.ShieldBash : 0)
                .IncreasesStat(StatType.DeflectionRecastReductionSeconds, creature => EquipmentPredicates.HasOffHandShield(creature) ? 2 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 5);
        }

        private void ShieldWall()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldWall)
                .Name("Shield Wall")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldWall1)
                .Description("Channel for up to 30 seconds. While channeling you and any allies within 5m gain 20% damage reduction.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShieldWall2)
                .Description("Channel for up to 30 seconds. While channeling you and any allies within 5m gain 35% damage reduction.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 32);
        }

        private void Unbreakable()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Unbreakable)
                .Name("Unbreakable")

                .AddPerkLevel()
                .GrantsFeat(FeatType.UnbreakableTrait)
                .Description("When reduced below 25% HP, gain 40% Physical Defense for 30 seconds. Once per 3 minutes.")
                .IncreasesStat(StatType.LowHPPhysicalDefenseThresholdPercent, 25)
                .IncreasesStat(StatType.LowHPPhysicalDefensePercentAdjustment, 40)
                .IncreasesStat(StatType.LowHPPhysicalDefenseDurationSeconds, 30)
                .IncreasesStat(StatType.LowHPPhysicalDefenseCooldownSeconds, 180)
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 35);
        }

    }
}
