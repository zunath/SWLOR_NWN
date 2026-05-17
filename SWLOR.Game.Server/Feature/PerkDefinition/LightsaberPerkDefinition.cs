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
            ThunderousChallenge();
            VersatileStrike();

            return _builder.Build();
        }


        private void ArcStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.ArcStrike)
                .Name("Arc Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ArcStrike1)
                .Description("You deal weapon DMG + 20 to all enemies in the area of effect (cone) in front of you.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void BladeBlitz()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.BladeBlitz)
                .Name("Blade Blitz")

                .AddPerkLevel()
                .Description("After dealing a critical hit, your next auto-attack uses the default minimum delay.")
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
                .GrantsFeat(FeatType.Centering1)
                .Description("Reduces enmity by 25% and increases accuracy by 10% for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Centering2)
                .Description("Reduces enmity by 50% and increases accuracy by 20% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void DeflectionCounter()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.DeflectionCounter)
                .Name("Deflection Counter")

                .AddPerkLevel()
                .Description("After deflecting an attack, your next attack has no delay.")
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
                .Description("Grants +15 Attack Deflection.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 15 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants +25 Attack Deflection.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 25 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants +35 Attack Deflection.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 35 : 0)
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
                .Description("While active, gain +10% Attack.")
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
                .Description("Grants the ability Guardian's Wrath which guarantees all attacks toward you will be deflected for 30 seconds. Additionally, increases your natural attack deflection cap to 75% when equipped with a lightsaber.")
                .IncreasesStat(StatType.AttackDeflectionChanceCap, creature => EquipmentPredicates.HasMainHandLightsaber(creature) ? 75 : 0)
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
                .Description("All enemies within the area of effect (cone) take weapon DMG + 35 and gain increased enmity toward you.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void GuardiansInfluence()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.GuardiansInfluence)
                .Name("Guardian's Influence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansInfluence1)
                .Description("Allies within the area of effect (sphere) gain +15 attack deflection chance for 1 minute. You do not receive this benefit.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void ImpenetrableGuard()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ImpenetrableGuard)
                .Name("Impenetrable Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImpenetrableGuard1)
                .Description("While active, grants +15% attack deflection, +10% enmity generation, -20% attack, -20% force attack.")
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
                .GrantsFeat(FeatType.OverwhelmingStrike1)
                .Description("You deal weapon DMG + 15 to all enemies in the area of effect (cone) in front of you. Inflicts Sunder which reduces defense and force defense by 15% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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
                .GrantsFeat(FeatType.Purify1)
                .Description("One debuff is removed from you. A nearby enemy is inflicted with the removed debuff.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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
                .GrantsFeat(FeatType.RippleSlash1)
                .Description("Your next attack deals weapon DMG + 30 to your target. Inflicts Disoriented on nearby enemies, reducing Accuracy and Evasion by 15% for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void SaberStorm()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SaberStorm)
                .Name("Saber Storm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberStorm1)
                .Description("Enemies within the area of effect (sphere) around you are dealt weapon DMG + 60. Inflicts Sunder which reduces defense and force defense by 25% for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void SecondWind()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SecondWind)
                .Name("Second Wind")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SecondWind1)
                .Description("Restores 50% of max STM, increased by 1 percentage point per MGT to a maximum of 75%.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void SurgeStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SurgeStrike)
                .Name("Surge Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SurgeStrike1)
                .Description("Your next attack deals weapon DMG + 15. Inflicts Force Disruption, preventing the target from using Force abilities for 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void TauntingDeflection()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.TauntingDeflection)
                .Name("Taunting Deflection")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TauntingDeflection1)
                .Description("Goads all nearby enemies into attacking you and grants the buff Taunting Deflection, which increases your attack deflection by 10 for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }


        private void ThunderousChallenge()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ThunderousChallenge)
                .Name("Thunderous Challenge")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ThunderousChallenge1)
                .Description("Deals weapon DMG + 35 to enemies within the area of effect (line) from your position and gain increased enmity toward you.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 45)
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
                .RequirementSkill(SkillType.Lightsaber, 5)
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

