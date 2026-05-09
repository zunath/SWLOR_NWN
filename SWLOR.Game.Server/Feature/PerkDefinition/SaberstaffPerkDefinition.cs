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
                .Description("While both FP and STM are above 50%, gain +10% Attack and +10% Force Attack.")
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
                .GrantsFeat(FeatType.ConduitFlare1)
                .Description("Deals weapon DMG + 20 to all nearby enemies and has a Will DC16 check to inflict Force Disruption for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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
                .Description("Gain +5% Force Defense and saberstaff attacks restore 1 FP. FP restoration can only trigger once every 4 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Saberstaff attacks restore 2 FP and your Force Defense bonus increases to +10% total.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Saberstaff attacks restore 3 FP and your Force Defense bonus increases to +15% total.")
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
                .RequirementSkill(SkillType.Saberstaff, 5)
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
                .Description("Using a Force ability causes your next saberstaff attack within 8 seconds to deal +15 DMG. Using a saberstaff ability reduces the FP cost of your next Force ability by 2.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void FlowOfTheMaelstrom()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.FlowOfTheMaelstrom)
                .Name("Flow of the Maelstrom")

                .AddPerkLevel()
                .Description("After hitting 3 or more enemies with one saberstaff ability, gain +15% Haste and +10 Attack Deflection for 12 seconds.")
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
                .Description("Deals weapon DMG + 10 and has a Will DC12 check to inflict Force Erosion for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedArc2)
                .Description("Deals weapon DMG + 22 and has a Will DC15 check to inflict Force Erosion for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedArc3)
                .Description("Deals weapon DMG + 34 and has a Will DC18 check to inflict Force Erosion for 18 seconds.")
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
                .GrantsFeat(FeatType.ForceGyre1)
                .Description("Deals weapon DMG + 24 to all nearby enemies and has a Will DC16 check to inflict Force Erosion for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceLens()
        {
            _builder.Create(PerkCategoryType.SaberstaffConduit, PerkType.ForceLens)
                .Name("Force Lens")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceLens1)
                .Description("Allies in an area of effect (sphere) gain +15% Force Defense for 45 seconds. You gain +10 Attack Deflection.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceMomentum()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.ForceMomentum)
                .Name("Force Momentum")

                .AddPerkLevel()
                .Description("Hitting 2 or more enemies with a saberstaff ability restores 2 FP and 2 STM. This can only trigger once every 4 seconds.")
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
                .Description("Gain +20 Attack Deflection and +20% Force Defense for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardedChannel2)
                .Description("Gain +30 Attack Deflection and +30% Force Defense for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardedChannel3)
                .Description("Gain +40 Attack Deflection and +35% Force Defense for 15 seconds.")
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
                .Description("For 20 seconds, saberstaff attacks restore 5 FP and saberstaff combat abilities cost 3 less STM. The effect ends early if FP reaches zero.")
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
                .Description("Deals weapon DMG + 22 to enemies in a cone and has a Will DC14 check to inflict Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Saberstaff, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.MaelstromArc2)
                .Description("Deals weapon DMG + 32 to enemies in a cone and has a Will DC16 check to inflict Disoriented for 15 seconds.")
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
                .Description("Channel for up to 6 seconds, hitting all nearby enemies every 2 seconds for weapon DMG + 25 and restoring 3 FP per enemy hit.")
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
                .Description("Deals weapon DMG + 18 and has a Will DC14 check to inflict Fractured Focus for 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SeverFocus2)
                .Description("Deals weapon DMG + 28 and has a Will DC18 check to inflict Fractured Focus for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Saberstaff, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SpinningDeflection()
        {
            _builder.Create(PerkCategoryType.SaberstaffTempest, PerkType.SpinningDeflection)
                .Name("Spinning Deflection")

                .AddPerkLevel()
                .Description("Gain +10 Attack Deflection. After deflecting an attack, your next Circle Slash deals +8 DMG.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 10 : 0)
                .Price(2)
                .RequirementSkill(SkillType.Saberstaff, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Gain +20 Attack Deflection total. Deflecting an attack restores 4 FP.")
                .IncreasesStat(StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandSaberstaff(creature) ? 20 : 0)
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
                .Description("Saberstaff combat abilities cost 2 less STM while your FP is above 50%.")
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
