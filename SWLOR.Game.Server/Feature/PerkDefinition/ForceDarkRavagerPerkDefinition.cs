using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceDarkRavagerPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForceSpark();
            ForceLightning();
            UnstablePressure();
            ForceDrain();
            FuryStance();
            DevouringStrike();
            CruelMomentum();
            HungerOfTheDark();

            return _builder.Build();
        }

        private void ForceSpark()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.ForceSpark)
                .Name("Force Spark")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals 16 force DMG plus WIL scaling to one target and reduces Evasion by 4% for 30 seconds.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark1)

                .AddPerkLevel()
                .Description("Deals 30 force DMG plus WIL scaling to one target and reduces Evasion by 6% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark2);
        }

        private void ForceLightning()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.ForceLightning)
                .Name("Force Lightning")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals 10 force DMG plus WIL scaling to one target, then arcs to up to two enemies within 5m for 50% damage. Affected targets suffer Shock for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning1)

                .AddPerkLevel()
                .Description("Deals 18 force DMG plus WIL scaling to one target, then arcs to up to three enemies within 5m for 50% damage. Affected targets suffer Shock for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning2)

                .AddPerkLevel()
                .Description("Deals 40 force DMG plus WIL scaling to one target, then arcs to up to three enemies within 5m for 50% damage. Affected targets suffer Shock for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning3);
        }

        private void UnstablePressure()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.UnstablePressure)
                .Name("Unstable Pressure")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .GrantsFeat(FeatType.UnstablePressureTrait)
                .Description("Force Spark and Force Lightning mark affected enemies with unstable pressure for 30 seconds, reducing Evasion by 5%. Enemies below 35% HP also suffer +5% force damage taken while marked.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.SparkLightningPressureEvasionPenaltyPercent, 5)
                .IncreasesStat(StatType.SparkLightningPressureLowHPForceDamageTakenPercent, 5)
                .IncreasesStat(StatType.SparkLightningPressureLowHPThresholdPercent, 35)
                .IncreasesStat(StatType.SparkLightningPressureDurationSeconds, 30);
        }

        private void ForceDrain()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.ForceDrain)
                .Name("Force Drain")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals 14 force DMG plus WIL scaling to one target and heals you for 30% of damage dealt. If the target is below 50% HP, healing increases to 40%.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain1)

                .AddPerkLevel()
                .Description("Deals 24 force DMG plus WIL scaling to one target and heals you for 35% of damage dealt. If the target is below 50% HP, healing increases to 45%.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain2)

                .AddPerkLevel()
                .Description("Deals 36 force DMG plus WIL scaling to one target and heals you for 40% of damage dealt. If the target is below 50% HP, healing increases to 50%.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain3);
        }

        private void FuryStance()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.FuryStance)
                .Name("Fury Stance")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("While active, gain +8% weapon and force damage and +10% critical damage, but take 5% more damage and suffer -5% Defense and Force Defense. Only one stance may be active.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.FuryStance1)

                .AddPerkLevel()
                .Description("While active, gain +12% weapon and force damage and +15% critical damage, but take 5% more damage and suffer -5% Defense and Force Defense. Only one stance may be active.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.FuryStance2);
        }

        private void DevouringStrike()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.DevouringStrike)
                .Name("Devouring Strike")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DevouringStrikeTrait)
                .Description("Alter powers that damage enemies deal 15% more damage to targets below 35% HP.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.DarkForceTargetLowHPDamageThresholdPercent, 35)
                .IncreasesStat(StatType.DarkForceTargetLowHPDamagePercentAdjustment, 15);
        }

        private void CruelMomentum()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.CruelMomentum)
                .Name("Cruel Momentum")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CruelMomentumTrait)
                .Description("When an enemy you damaged within the last 6 seconds is defeated, restore 2 FP and gain +5% Force ability Accuracy for 30 seconds. This can trigger once every 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.CruelMomentum, 1);
        }

        private void HungerOfTheDark()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.HungerOfTheDark)
                .Name("Hunger of the Dark")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("For 45 seconds, Dark damage you deal heals you for 12% of damage dealt and defeated enemies restore 3 FP.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.HungerOfTheDark1)
                .RequirementQuest(ForceCapstoneQuestDefinition.HungerOfTheDarkMasteryQuestId);
        }

    }
}
