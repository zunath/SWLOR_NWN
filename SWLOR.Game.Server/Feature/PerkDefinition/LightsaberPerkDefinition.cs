using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class LightsaberPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ArcStrike();
            BladeBlitz();
            BrutalAssault();
            BrutalEfficiency();
            Centering();
            DeflectionCounter();
            DeflectionMastery();
            DeflectionRiposte();
            DeflectionTraining();
            DeflectivePresence();
            FerocityStance();
            FocusedStance();
            GuardianMaster();
            GuardiansChallenge();
            GuardiansInfluence();
            ImpenetrableGuard();
            LegSlash();
            Overcharge();
            OverwhelmingDefense();
            OverwhelmingStrike();
            PunishingStrike();
            Purify();
            ReactiveDeflection();
            RippleSlash();
            SaberStorm();
            SecondWind();
            SurgeStrike();
            TauntingDeflection();
            VersatileStrike();

            return _builder.Build();
        }


        private void ArcStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.ArcStrike)
                .Name("Arc Strike")

                .AddPerkLevel()
                .Description("Lightsaber Offense area abilities deal +20 DMG to nearby secondary targets.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseAreaDamageBonus, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0);
        }


        private void BladeBlitz()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.BladeBlitz)
                .Name("Blade Blitz")

                .AddPerkLevel()
                .Description("After dealing a critical hit, your next lightsaber auto-attack within 15 seconds is quickened to your fastest possible swing speed.")
                .IncreasesStat(StatType.CriticalNextAutoAttackNoDelayTriggerSkillType, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)SkillType.Lightsaber : 0)
                .IncreasesStat(StatType.CriticalNextAutoAttackNoDelaySkillType, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)SkillType.Lightsaber : 0)
                .IncreasesStat(StatType.CriticalNextAutoAttackNoDelayDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void BrutalAssault()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.BrutalAssault)
                .Name("Brutal Assault")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BrutalAssault1)
                .Description("Allies within the area of effect (sphere) gain +10% critical hit chance for 1 minute. You do not receive this benefit.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void BrutalEfficiency()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.BrutalEfficiency)
                .Name("Brutal Efficiency")

                .AddPerkLevel()
                .Description("Your attacks deal +15% damage to enemies afflicted by Sunder.")
                .IncreasesStat(StatType.DamageToSunderedTargetPercentAdjustment, 15)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 27)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void Centering()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.Centering)
                .Name("Centering")

                .AddPerkLevel()
                .Description("Using a Lightsaber Offense ability reduces your enmity by 10% and grants +10% Accuracy for 8 seconds. This can trigger once every 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseCenteringAccuracyPercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 10 : 0)
                .IncreasesStat(StatType.LightsaberOffenseCenteringDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 8 : 0)
                .IncreasesStat(StatType.LightsaberOffenseCenteringEnmityReductionPercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 10 : 0)
                .IncreasesStat(StatType.LightsaberOffenseCenteringCooldownSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)

                .AddPerkLevel()
                .Description("Using a Lightsaber Offense ability reduces your enmity by 20% and grants +20% Accuracy for 8 seconds. This can trigger once every 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseCenteringAccuracyPercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)
                .IncreasesStat(StatType.LightsaberOffenseCenteringDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 8 : 0)
                .IncreasesStat(StatType.LightsaberOffenseCenteringEnmityReductionPercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)
                .IncreasesStat(StatType.LightsaberOffenseCenteringCooldownSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0);
        }


        private void DeflectionCounter()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.DeflectionCounter)
                .Name("Deflection Counter")

                .AddPerkLevel()
                .Description("After deflecting an attack, your next hostile Lightsaber ability within 15 seconds activates instantly.")
                .IncreasesStat(StatType.DeflectionNextSkillAbilityNoDelay, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 1 : 0)
                .IncreasesStat(StatType.DeflectionNextSkillAbilityNoDelayWindowSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void DeflectionMastery()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.DeflectionMastery)
                .Name("Deflection Mastery")

                .AddPerkLevel()
                .Description("When you deflect an attack, your defense and force defense increase by 15% for 12 seconds.")
                .IncreasesStat(StatType.DeflectionDefensePercentAdjustment, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .IncreasesStat(StatType.DeflectionForceDefensePercentAdjustment, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void DeflectionRiposte()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.DeflectionRiposte)
                .Name("Deflection Riposte")

                .AddPerkLevel()
                .Description("When you deflect an attack, your next attack receives +20% critical chance. Effect wears off after 15 seconds.")
                .IncreasesStat(StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)
                .IncreasesStat(StatType.DeflectionNextSkillAbilityCriticalRateWindowSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void DeflectionTraining()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.DeflectionTraining)
                .Name("Deflection Training")

                .AddPerkLevel()
                .Description("Grants +8 Attack Deflection.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 8 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants +14 Attack Deflection total.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 14 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants +20 Attack Deflection total.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void DeflectivePresence()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.DeflectivePresence)
                .Name("Deflective Presence")

                .AddPerkLevel()
                .Description("When you deflect an attack, receive the Deflective Presence buff which improves your Enmity acquisition by 20% for 12 seconds.")
                .IncreasesStat(StatType.DeflectionEnmityPercentAdjustment, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void FerocityStance()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.FerocityStance)
                .Name("Ferocity Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FerocityStance1)
                .Description("While active, grants -20% to offhand weapon delay, +10% attack, and -20% to evasion.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void FocusedStance()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.FocusedStance)
                .Name("Focused Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedStance1)
                .Description("While active, against Sundered targets, Lightsaber Offense attacks have +10% Accuracy and +8% critical hit chance. Versatile Strike lengthens an existing Sunder duration by 6 seconds, up to 45 seconds. Area Lightsaber Offense damage is reduced by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void GuardianMaster()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.GuardianMaster)
                .Name("Guardian Master")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardianMaster1)
                .Description("For 45 seconds, successful Attack Deflections restore 4 FP, refresh Deflective Presence, and generate +50% enmity. Your Attack Deflection cap increases by +10 while this lasts.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void GuardiansChallenge()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.GuardiansChallenge)
                .Name("Guardian's Challenge")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansChallenge1)
                .Description("Enemies in a cone take weapon DMG +35 and generate increased enmity toward you.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansChallenge2)
                .Description("Enemies in a line take weapon DMG +35 and generate increased enmity toward you. If this hits two or more enemies, your next successful Attack Deflection within 8 seconds restores 3 FP and generates increased enmity.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void GuardiansInfluence()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.GuardiansInfluence)
                .Name("Guardian's Influence")

                .AddPerkLevel()
                .Description("Lightsaber Defense abilities grant nearby allies +8 Attack Deflection for 12 seconds. You do not receive this benefit.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 8 : 0)
                .IncreasesStat(StatType.LightsaberDefenseGuardiansInfluenceDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 12 : 0);
        }


        private void ImpenetrableGuard()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ImpenetrableGuard)
                .Name("Impenetrable Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImpenetrableGuard1)
                .Description("While active, successful Attack Deflections restore 1 FP and generate +20% enmity. Attack and Force Attack are reduced by 20%.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void LegSlash()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.LegSlash)
                .Name("Leg Slash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LegSlash1)
                .Description("You deal weapon DMG + 10 and inflict Disoriented, reducing Accuracy and Evasion by 15% for 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void Overcharge()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.Overcharge)
                .Name("Overcharge")

                .AddPerkLevel()
                .Description("Your Versatile Strike and Overwhelming Strike abilities now deal +10 DMG and increase their Sunder duration by 50%.")
                .IncreasesStat(StatType.AbilityDamageFlatAdjustmentPerkType, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)PerkType.VersatileStrike : 0)
                .IncreasesStat(StatType.AbilityDamageFlatAdjustmentSecondaryPerkType, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)PerkType.OverwhelmingStrike : 0)
                .IncreasesStat(StatType.AbilityDamageFlatAdjustment, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 10 : 0)
                .IncreasesStat(StatType.AbilityStatusDurationPercentAdjustmentPerkType, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)PerkType.VersatileStrike : 0)
                .IncreasesStat(StatType.AbilityStatusDurationPercentAdjustmentSecondaryPerkType, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)PerkType.OverwhelmingStrike : 0)
                .IncreasesStat(StatType.AbilityStatusDurationPercentAdjustment, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 50 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 47)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void OverwhelmingDefense()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.OverwhelmingDefense)
                .Name("Overwhelming Defense")

                .AddPerkLevel()
                .Description("After deflecting an attack, your next attack deals +20 DMG.")
                .IncreasesStat(StatType.DeflectionNextSkillAbilityDamageBonus, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0)
                .IncreasesStat(StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void OverwhelmingStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.OverwhelmingStrike)
                .Name("Overwhelming Strike")

                .AddPerkLevel()
                .Description("Lightsaber Offense area abilities inflict Sunder on enemies hit, reducing Defense and Force Defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseAreaSunderDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 30 : 0);
        }


        private void PunishingStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.PunishingStrike)
                .Name("Punishing Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PunishingStrike1)
                .Description("Deals weapon DMG + 20 to enemies within the area of effect (sphere) near you and gain increased enmity toward you.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void Purify()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.Purify)
                .Name("Purify")

                .AddPerkLevel()
                .Description("Lightsaber Offense area abilities remove one debuff from you and transfer it to a nearby enemy. This can trigger once every 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffensePurify, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 1 : 0)
                .IncreasesStat(StatType.LightsaberOffensePurifyCooldownSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0);
        }


        private void ReactiveDeflection()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ReactiveDeflection)
                .Name("Reactive Deflection")

                .AddPerkLevel()
                .Description("When you deflect an attack, restore 2 FP.")
                .IncreasesStat(StatType.DeflectionFPRestore, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 2 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("When you deflect an attack, restore 4 FP.")
                .IncreasesStat(StatType.DeflectionFPRestore, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 4 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void RippleSlash()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.RippleSlash)
                .Name("Ripple Slash")

                .AddPerkLevel()
                .Description("Lightsaber Offense area abilities also inflict Disoriented on nearby enemies, reducing Accuracy and Evasion by 15% for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseAreaDisorientedDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 20 : 0);
        }


        private void SaberStorm()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SaberStorm)
                .Name("Saber Storm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberStorm1)
                .Description("Enemies within the area of effect (sphere) take weapon DMG + 30 and suffer Sunder, reducing physical and Force defense by 10% for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void SecondWind()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SecondWind)
                .Name("Second Wind")

                .AddPerkLevel()
                .Description("When you fall below 35% STM, your next Lightsaber Offense ability restores 50% of maximum STM, increased by 1 percentage point per MGT to a maximum of 75%. This can trigger once every 90 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseSecondWindThresholdPercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 35 : 0)
                .IncreasesStat(StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 50 : 0)
                .IncreasesStat(StatType.LightsaberOffenseSecondWindScalingAbility, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? (int)AbilityType.Might + 1 : 0)
                .IncreasesStat(StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 75 : 0)
                .IncreasesStat(StatType.LightsaberOffenseSecondWindCooldownSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 90 : 0);
        }


        private void SurgeStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SurgeStrike)
                .Name("Surge Strike")

                .AddPerkLevel()
                .Description("Lightsaber Offense single-target abilities also inflict Force Disruption, preventing the target from using Force abilities for 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 8 : 0);
        }


        private void TauntingDeflection()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.TauntingDeflection)
                .Name("Taunting Deflection")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TauntingDeflection1)
                .Description("Goads all nearby enemies into attacking you for 30 seconds. While this effect lasts, your successful Attack Deflections restore 2 FP and generate increased enmity.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void VersatileStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.VersatileStrike)
                .Name("Versatile Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VersatileStrike1)
                .Description("Your next attack deals weapon DMG + 10 to your target. Inflicts Sunder which reduces defense and force defense by 10% for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.VersatileStrike2)
                .Description("Your next attack deals weapon DMG + 25 to your target. Inflicts Sunder which reduces defense and force defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.VersatileStrike3)
                .Description("Your next attack deals weapon DMG + 40 to your target. Inflicts Sunder which reduces defense and force defense by 20% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }
    }
}
