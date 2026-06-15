using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class SaberstaffPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BalancedAttunement();
            CircleSlash();
            ConduitFlare();
            ConduitStance();
            ConduitTraining();
            DoubleStrike();
            EnergizedForms();
            FlowOfTheMaelstrom();
            FocusedArc();
            ForceCapacitor();
            ForceGyre();
            ForceLens();
            ForceMomentum();
            GuardedChannel();
            InfiniteConduit();
            MaelstromArc();
            SaberCyclone();
            SeverFocus();
            SpinningDeflection();
            TempestFocus();
            TempestRelease();
            TempestStance();

            return _builder.Build();
        }

        private void BalancedAttunement()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.BalancedAttunement)
                .Name("Balanced Attunement")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BalancedAttunementTrait)
                .Description("While both FP and STM are above 50%, gain +10% Attack and +10% Force Attack.")
                .IncreasesStat(StatType.HighFPAndStaminaAttackThresholdPercent, 50)
                .IncreasesStat(StatType.HighFPAndStaminaAttackPercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void CircleSlash()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.CircleSlash)
                .Name("Circle Slash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CircleSlash1)
                .Description("Attacks up to 3 nearby enemies for weapon DMG + 10 each.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CircleSlash2)
                .Description("Attacks up to 3 nearby enemies for weapon DMG + 18 each.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CircleSlash3)
                .Description("Attacks up to 3 nearby enemies for weapon DMG + 28 each.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ConduitFlare()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.ConduitFlare)
                .Name("Conduit Flare")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConduitFlareTrait)
                .Description("Conduit offensive abilities deal +20 DMG to nearby enemies and inflict Force Disruption for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.SaberstaffConduitAreaConduitFlare, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 1 : 0)
                .IncreasesStat(StatType.SaberstaffConduitFlareDamageBonus, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 20 : 0)
                .IncreasesStat(StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0);
        }

        private void ConduitStance()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.ConduitStance)
                .Name("Conduit Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConduitStance1)
                .Description("While active, grants +15% Force Attack and +15% Force Defense, but reduces Attack by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ConduitTraining()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.ConduitTraining)
                .Name("Conduit Training")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConduitTrainingTrait)
                .Description("Gain +5% Force Defense and saberstaff attacks restore 1 FP. FP restoration can only trigger once every 4 seconds.")
                .IncreasesStat(StatType.ForceDefensePercentAdjustment, 5)
                .IncreasesStat(StatType.AutoAttackFPRestore, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 1 : 0)
                .IncreasesStat(StatType.AutoAttackFPRestoreCooldownSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 4 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Saberstaff attacks restore 2 FP and your Force Defense bonus increases to +10% total.")
                .IncreasesStat(StatType.ForceDefensePercentAdjustment, 10)
                .IncreasesStat(StatType.AutoAttackFPRestore, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 2 : 0)
                .IncreasesStat(StatType.AutoAttackFPRestoreCooldownSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 4 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Saberstaff attacks restore 3 FP and your Force Defense bonus increases to +15% total.")
                .IncreasesStat(StatType.ForceDefensePercentAdjustment, 15)
                .IncreasesStat(StatType.AutoAttackFPRestore, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 3 : 0)
                .IncreasesStat(StatType.AutoAttackFPRestoreCooldownSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 4 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void DoubleStrike()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.DoubleStrike)
                .Name("Double Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleStrike1)
                .Description("Instantly attacks twice, each for weapon DMG + 12.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleStrike2)
                .Description("Instantly attacks twice, each for weapon DMG + 21.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleStrike3)
                .Description("Instantly attacks twice, each for weapon DMG + 29.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.DoubleStrike4)
                .Description("Instantly attacks twice, each for weapon DMG + 38. Targets affected by Force Erosion take +15 DMG from each strike.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void EnergizedForms()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.EnergizedForms)
                .Name("Energized Forms")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnergizedFormsTrait)
                .Description("Using a Force ability causes your next saberstaff attack within 8 seconds to deal +15 DMG. Using a saberstaff ability reduces the FP cost of your next Force ability by 2.")
                .IncreasesStat(StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)SkillType.Force : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)SkillType.Saberstaff : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillAutoAttackDamageBonus, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 15 : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)SkillType.Saberstaff : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)SkillType.Force : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillFPCostAdjustment, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? -2 : 0)
                .IncreasesStat(StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void FlowOfTheMaelstrom()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.FlowOfTheMaelstrom)
                .Name("Flow of the Maelstrom")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FlowOfTheMaelstromTrait)
                .Description("After hitting 3 or more enemies with one saberstaff ability, gain +15% Haste and +8 Attack Deflection for 12 seconds.")
                .IncreasesStat(StatType.SaberstaffAreaAbilityMinTargetsBuffThreshold, 3)
                .IncreasesStat(StatType.SaberstaffAreaAbilityHastePercentAdjustment, 15)
                .IncreasesStat(StatType.SaberstaffAreaAbilityAttackDeflection, 8)
                .IncreasesStat(StatType.SaberstaffAreaAbilityBuffDurationSeconds, 12)
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void FocusedArc()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.FocusedArc)
                .Name("Focused Arc")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedArc1)
                .Description("Deals weapon DMG + 10 and inflicts Force Erosion for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedArc2)
                .Description("Deals weapon DMG + 22 and inflicts Force Erosion for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedArc3)
                .Description("Deals weapon DMG + 34 and inflicts Force Erosion for 18 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceCapacitor()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.ForceCapacitor)
                .Name("Force Capacitor")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceCapacitor1)
                .Description("For 20 seconds, 25% of STM spent on saberstaff abilities is restored as FP and 25% of FP spent on Force abilities is restored as STM.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceGyre()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.ForceGyre)
                .Name("Force Gyre")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceGyreTrait)
                .Description("Tempest area abilities inflict Force Erosion for 12 seconds on enemies hit.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.SaberstaffTempestForceGyre, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 1 : 0)
                .IncreasesStat(StatType.SaberstaffTempestForceGyreDurationSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 12 : 0);
        }

        private void ForceLens()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.ForceLens)
                .Name("Force Lens")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceLensTrait)
                .Description("Conduit defensive abilities grant allies +15% Force Defense for 20 seconds and grant you +8 Attack Deflection.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.SaberstaffConduitForceLens, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 1 : 0);
        }

        private void ForceMomentum()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.ForceMomentum)
                .Name("Force Momentum")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceMomentumTrait)
                .Description("Hitting 2 or more enemies with a saberstaff ability restores 2 FP and 2 STM. This can only trigger once every 4 seconds.")
                .IncreasesStat(StatType.SaberstaffAreaAbilityMinTargetsResourceRestoreThreshold, 2)
                .IncreasesStat(StatType.SaberstaffAreaAbilityFPRestore, 2)
                .IncreasesStat(StatType.SaberstaffAreaAbilityStaminaRestore, 2)
                .IncreasesStat(StatType.SaberstaffAreaAbilityResourceRestoreCooldownSeconds, 4)
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void GuardedChannel()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.GuardedChannel)
                .Name("Guarded Channel")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardedChannel1)
                .Description("Gain +12 Attack Deflection and +20% Force Defense for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardedChannel2)
                .Description("Gain +22 Attack Deflection and +30% Force Defense for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardedChannel3)
                .Description("Gain +30 Attack Deflection and +35% Force Defense for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void InfiniteConduit()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.InfiniteConduit)
                .Name("Infinite Conduit")

                .AddPerkLevel()
                .GrantsFeat(FeatType.InfiniteConduit1)
                .Description("For 45 seconds, attacks restore 2 FP and combat abilities cost 2 less STM.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void MaelstromArc()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.MaelstromArc)
                .Name("Maelstrom Arc")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MaelstromArc1)
                .Description("Deals weapon DMG + 22 to enemies in a cone and inflicts Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.MaelstromArc2)
                .Description("Deals weapon DMG + 32 to enemies in a cone and inflicts Disoriented for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SaberCyclone()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.SaberCyclone)
                .Name("Saber Cyclone")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SaberCyclone1)
                .Description("Deal weapon DMG + 18 to nearby enemies. For 45 seconds, pulse every 6 seconds, dealing 8 force DMG to nearby enemies and restoring 1 FP per enemy hit, up to 5 FP per pulse.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SeverFocus()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.SeverFocus)
                .Name("Sever Focus")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SeverFocus1)
                .Description("Deals weapon DMG + 18 and inflicts Fractured Focus for 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SeverFocus2)
                .Description("Deals weapon DMG + 28 and inflicts Fractured Focus for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SpinningDeflection()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.SpinningDeflection)
                .Name("Spinning Deflection")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SpinningDeflectionTrait)
                .Description("Gain +8 Attack Deflection. After deflecting an attack, your next Circle Slash deals +8 DMG.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .IncreasesStat(StatType.DeflectionNextAbilityDamageBonusPerkType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)PerkType.CircleSlash : 0)
                .IncreasesStat(StatType.DeflectionNextAbilityDamageBonus, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .IncreasesStat(StatType.DeflectionNextAbilityDamageBonusDurationSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Gain +16 Attack Deflection total. Deflecting an attack restores 4 FP.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 16 : 0)
                .IncreasesStat(StatType.DeflectionNextAbilityDamageBonusPerkType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)PerkType.CircleSlash : 0)
                .IncreasesStat(StatType.DeflectionNextAbilityDamageBonus, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .IncreasesStat(StatType.DeflectionNextAbilityDamageBonusDurationSeconds, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 8 : 0)
                .IncreasesStat(StatType.DeflectionFPRestore, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 4 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void TempestFocus()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.TempestFocus)
                .Name("Tempest Focus")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempestFocusTrait)
                .Description("Saberstaff combat abilities cost 2 less STM while your FP is above 50%.")
                .IncreasesStat(StatType.HighResourceAbilityStaminaCostSkillType, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? (int)SkillType.Saberstaff : 0)
                .IncreasesStat(StatType.HighResourceAbilityStaminaCostThresholdPercent, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 50 : 0)
                .IncreasesStat(StatType.HighResourceAbilityStaminaCostAdjustment, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? -2 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void TempestRelease()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.TempestRelease)
                .Name("Tempest Release")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempestRelease1)
                .Description("Deals weapon DMG + 20 to all nearby enemies. Damage increases by +2 per 10 FP you currently have, up to +20 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void TempestStance()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.TempestStance)
                .Name("Tempest Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempestStance1)
                .Description("While active, grants +15% Haste and +10% Force Attack, but reduces Defense by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

    }
}
