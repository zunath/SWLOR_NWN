using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class LightsaberPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForceSheath();
            Overpower();
            FastStrikes();
            ShatteringStrike();
            SunderingSweep();
            WeakPoints();
            ImbuementStance();
            HighGround();
            FocusShift();
            Epicenter();
            SaberWard();
            MentalFortress();
            DeflectingReturn();
            GuardiansChallenge();
            SurroundedNotOutmatched();
            ForceLink();
            ImmovableStance();
            Reprisal();
            CenterOfTheStorm();
            AegisEternal();

            return _builder.Build();
        }

        private void ForceSheath()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ForceSheath)
                .Name("Force Sheath")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceSheath1)
                .Description("On your next hit, deal + 12 Force DMG.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceSheath2)
                .Description("On your next hit, deal + 15 Force DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceSheath3)
                .Description("On your next hit, deal + 18 Force DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceSheath4)
                .Description("On your next hit, deal + 21 Force DMG.")
                .Price(5)
                .RequirementSkill(SkillType.Lightsaber, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void Overpower()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.Overpower)
                .Name("Overpower")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OverpowerTrait)
                .Description("Spending at least 5 FP on a hostile combat ability increases your Force Attack by 3% for 30 seconds, stacking up to 9%.")
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackPercent, 3)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, 9)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackMinFPCost, 5)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Spending at least 5 FP on a hostile combat ability increases your Force Attack by 10% for 30 seconds.")
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackPercent, 10)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, 10)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackMinFPCost, 5)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Spending at least 5 FP on a hostile combat ability increases your Force Attack by 15% for 30 seconds.")
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackPercent, 15)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackMaxPercent, 15)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackMinFPCost, 5)
                .IncreasesStat(StatType.HostileAbilityFPSpendForceAttackDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void FastStrikes()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.FastStrikes)
                .Name("Fast Strikes")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FastStrikesTrait)
                .Description("After landing a critical hit, your next auto-attack within 16 seconds is quickened to your fastest possible swing speed.")
                .IncreasesStat(StatType.CriticalNextAutoAttackNoDelayTriggerSkillType, (int)SkillType.Lightsaber)
                .IncreasesStat(StatType.CriticalNextAutoAttackNoDelaySkillType, (int)SkillType.Lightsaber)
                .IncreasesStat(StatType.CriticalNextAutoAttackNoDelayDurationSeconds, 16)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ShatteringStrike()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ShatteringStrike)
                .Name("Shattering Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShatteringStrike1)
                .Description("Deals weapon DMG + 18 and inflicts Sunder, reducing Defense and Force Defense by 10% for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShatteringStrike2)
                .Description("Deals weapon DMG + 28 and inflicts Sunder, reducing Defense and Force Defense by 12% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SunderingSweep()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.SunderingSweep)
                .Name("Sundering Sweep")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SunderingSweep1)
                .Description("Deals weapon DMG + 8 to enemies within 3m of you. If at least one struck enemy already has Sunder, spreads Sunder from that enemy to one other enemy in melee range for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SunderingSweep2)
                .Description("Deals weapon DMG + 12 to enemies within 3m of you. If at least one struck enemy already has Sunder, spreads Sunder from that enemy to one other enemy in melee range for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SunderingSweep3)
                .Description("Deals weapon DMG + 16 to enemies within 3m of you. If at least one struck enemy already has Sunder, spreads Sunder from that enemy to one other enemy in melee range for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void WeakPoints()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.WeakPoints)
                .Name("Weak Points")

                .AddPerkLevel()
                .GrantsFeat(FeatType.WeakPointsTrait)
                .Description("Gain + 10% Critical Rate against Sundered targets.")
                .IncreasesStat(StatType.CriticalRateAgainstSunderedTargetPercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ImbuementStance()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.ImbuementStance)
                .Name("Imbuement Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImbuementStance1)
                .Description("While active, your hostile auto-attacks deal Force damage instead of physical and cost 2 FP each.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void HighGround()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.HighGround)
                .Name("High Ground")

                .AddPerkLevel()
                .GrantsFeat(FeatType.HighGroundTrait)
                .Description("Landing an auto-attack on any Sundered target restores 2 FP.")
                .IncreasesStat(StatType.AutoAttackSunderedTargetFPRestore, 2)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void FocusShift()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.FocusShift)
                .Name("Focus Shift")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusShiftTrait)
                .Description("Gain + 15% Attack while below 30% FP.")
                .IncreasesStat(StatType.LowFPAttackPercentAdjustment, 15)
                .IncreasesStat(StatType.LowFPAttackThresholdPercent, 30)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void Epicenter()
        {
            _builder.Create(PerkCategoryType.LightsaberDefense, PerkType.Epicenter)
                .Name("Epicenter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Epicenter1)
                .Description("Inflicts Knockdown on enemies within 6m of you for 6 seconds, dealing 25 Force DMG and inflicting Sunder. Enemies that already had Sunder when struck take an additional 15 Force DMG.")
                .Price(6)
                .RequirementSkill(SkillType.Lightsaber, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementQuest(LightsaberCapstoneQuestDefinition.SaberStormMasteryQuestId);
        }

        private void SaberWard()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SaberWard)
                .Name("Saber Ward")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberWard1)
                .Description("For 30 seconds, convert 15% of incoming physical damage to Force damage and gain + 3% Defense and + 4% Force Defense. Only the highest rank of Saber Ward applies.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberWard2)
                .Description("For 30 seconds, convert 20% of incoming physical damage to Force damage and gain + 4% Defense and + 5% Force Defense. Only the highest rank of Saber Ward applies.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberWard3)
                .Description("For 30 seconds, convert 25% of incoming physical damage to Force damage and gain + 5% Defense and + 7% Force Defense. Only the highest rank of Saber Ward applies.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberWard4)
                .Description("For 30 seconds, convert 30% of incoming physical damage to Force damage and gain + 6% Defense and + 9% Force Defense. Only the highest rank of Saber Ward applies.")
                .Price(5)
                .RequirementSkill(SkillType.Lightsaber, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void MentalFortress()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.MentalFortress)
                .Name("Mental Fortress")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MentalFortressTrait)
                .Description("Gain + 10% Force Defense.")
                .IncreasesStat(StatType.ForceDefensePercentAdjustment, 10)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Gain + 12% Force Defense.")
                .IncreasesStat(StatType.ForceDefensePercentAdjustment, 12)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void DeflectingReturn()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.DeflectingReturn)
                .Name("Deflecting Return")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DeflectingReturnTrait)
                .Description("Gain +4 Ranged Deflection. When your Ranged Deflection negates a directly targeted ranged weapon auto-attack, reflect 8% of the attack's damage back to its source, up to 25% of your normal weapon damage. Triggers at most once every 6 seconds.")
                .IncreasesStat(StatType.RangedDeflection, 4)
                .IncreasesStat(StatType.RangedDeflectionReflectionPercent, 8)
                .IncreasesStat(StatType.RangedDeflectionReflectionCapPercent, 25)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Gain +6 Ranged Deflection. When your Ranged Deflection negates a directly targeted ranged weapon auto-attack, reflect 12% of the attack's damage back to its source, up to 40% of your normal weapon damage. Triggers at most once every 6 seconds.")
                .IncreasesStat(StatType.RangedDeflection, 6)
                .IncreasesStat(StatType.RangedDeflectionReflectionPercent, 12)
                .IncreasesStat(StatType.RangedDeflectionReflectionCapPercent, 40)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Gain +8 Ranged Deflection. When your Ranged Deflection negates a directly targeted ranged weapon auto-attack, reflect 16% of the attack's damage back to its source, up to 50% of your normal weapon damage. Triggers at most once every 6 seconds.")
                .IncreasesStat(StatType.RangedDeflection, 8)
                .IncreasesStat(StatType.RangedDeflectionReflectionPercent, 16)
                .IncreasesStat(StatType.RangedDeflectionReflectionCapPercent, 50)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void GuardiansChallenge()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.GuardiansChallenge)
                .Name("Guardian's Challenge")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansChallenge1)
                .Description("Deals weapon DMG + 12 to enemies in an 8m x 3m line. For each struck enemy that damaged you within the last 30 seconds, gain +20% Enmity toward it for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardiansChallenge2)
                .Description("Deals weapon DMG + 24 to enemies in an 8m x 3m line. For each struck enemy that damaged you within the last 30 seconds, gain +30% Enmity toward it for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SurroundedNotOutmatched()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SurroundedNotOutmatched)
                .Name("Surrounded, Not Outmatched")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SurroundedNotOutmatchedTrait)
                .Description("Each distinct enemy attacking you grants a stack of Embattled, up to 5 stacks. Each stack grants + 2% Defense and + 2% Force Defense.")
                .IncreasesStat(StatType.EmbattledStackDefensePercent, 2)
                .IncreasesStat(StatType.EmbattledStackForceDefensePercent, 2)
                .IncreasesStat(StatType.EmbattledMaxStacks, 5)
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceLink()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.SaberForceLink)
                .Name("Force Link")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceLink1)
                .Description("Target ally becomes your Link for 45 seconds. While within 20m, 45% of the Link's incoming damage is redirected to you as Force damage. Only one ward or guard link may protect a target.")
                .Price(3)
                .RequirementSkill(SkillType.Lightsaber, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ImmovableStance()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.ImmovableStance)
                .Name("Immovable Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ImmovableStance1)
                .Description("While active, generate + 30% Enmity and gain + 8 Mobility Resistance, but your Attack and Force Attack are reduced by 25%.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void Reprisal()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.Reprisal)
                .Name("Reprisal")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Reprisal1)
                .Description("Deals weapon DMG + 16. If the target has damaged you within the last 30 seconds, inflict Dazed for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Reprisal2)
                .Description("Deals weapon DMG + 30. If the target has damaged you within the last 30 seconds, inflict Dazed for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Lightsaber, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void CenterOfTheStorm()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.CenterOfTheStorm)
                .Name("Center of the Storm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CenterOfTheStormTrait)
                .Description("While you have 3 or more stacks of Embattled, gain + 10 Mobility Resistance and increase Deflecting Return's reflected damage by 4%.")
                .IncreasesStat(StatType.EmbattledHighStackThreshold, 3)
                .IncreasesStat(StatType.EmbattledHighStackMobilityResistance, 10)
                .IncreasesStat(StatType.EmbattledHighStackDeflectionReflectionBonusPercent, 4)
                .Price(2)
                .RequirementSkill(SkillType.Lightsaber, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void AegisEternal()
        {
            _builder.Create(PerkCategoryType.LightsaberOffense, PerkType.AegisEternal)
                .Name("Aegis Eternal")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AegisEternal1)
                .Description("For 30 seconds, enter Perfect Aegis, replacing Saber Ward: convert 40% of incoming physical damage to Force damage and gain + 8% Defense, + 12% Force Defense, and + 25% Enmity. Embattled is treated as 5 stacks and Deflecting Return reflects 24% of damage, up to 75% of your normal weapon damage.")
                .Price(6)
                .RequirementSkill(SkillType.Lightsaber, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementQuest(LightsaberCapstoneQuestDefinition.GuardianMasterMasteryQuestId);
        }

    }
}
