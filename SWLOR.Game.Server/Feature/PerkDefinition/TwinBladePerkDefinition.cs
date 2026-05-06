using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class TwinBladePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BindingCross();
            BladeVortex();
            CenterlineGuard();
            CycloneMastery();
            CycloneStance();
            DuelistsChallenge();
            DuelistStance();
            EdgeRhythm();
            FeintingCut();
            FinalForm();
            FlowingFootwork();
            GuardedFlow();
            MirrorStep();
            Momentum();
            PerfectBalance();
            PrecisionArc();
            PunishingAngle();
            ReversalCut();
            SplitGuardStrike();
            StormRelease();
            SweepingAdvance();
            TempestBloom();

            return _builder.Build();
        }

        private void BindingCross()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.BindingCross)
                .Name("Binding Cross")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BindingCross1)
                .Description("Strikes twice for weapon DMG + 10 each. Reflex DC14 check to inflict Hamstring for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BindingCross2)
                .Description("Strikes twice for weapon DMG + 18 each. Reflex DC18 check to inflict Hamstring for 20 seconds and Exposed for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 42);
        }

        private void BladeVortex()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.BladeVortex)
                .Name("Blade Vortex")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BladeVortex1)
                .Description("Deals weapon DMG + 18 to all nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BladeVortex2)
                .Description("Deals weapon DMG + 26 to all nearby enemies and has a Fortitude DC15 check to inflict Exposed for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 35);
        }

        private void CenterlineGuard()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.CenterlineGuard)
                .Name("Centerline Guard")

                .AddPerkLevel()
                .Description("Gain +10 Attack Deflection while wielding a twin blade. After deflecting an attack, your next attack within 8 seconds deals +8 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5);
        }

        private void CycloneMastery()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.CycloneMastery)
                .Name("Cyclone Mastery")

                .AddPerkLevel()
                .Description("Area Twin Blade abilities gain +10% critical chance and restore 1 STM per target hit, up to 5 STM.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 48);
        }

        private void CycloneStance()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.CycloneStance)
                .Name("Cyclone Stance")

                .AddPerkLevel()
                .Description("While active, grants +15% Haste and +10% Attack, but reduces Defense by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 15);
        }

        private void DuelistsChallenge()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.DuelistsChallenge)
                .Name("Duelist's Challenge")

                .AddPerkLevel()
                .Description("Mark a target for 20 seconds. You and the target deal +20% damage to each other, but you gain +20% Defense against that target.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45);
        }

        private void DuelistStance()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.DuelistStance)
                .Name("Duelist Stance")

                .AddPerkLevel()
                .Description("While active, single-target Twin Blade combat abilities deal +15% damage and grant +10 Attack Deflection for 6 seconds, but area Twin Blade abilities deal -15% damage.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 15);
        }

        private void EdgeRhythm()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.EdgeRhythm)
                .Name("Edge Rhythm")

                .AddPerkLevel()
                .Description("Every third auto-attack with a twin blade deals +15 DMG to a nearby enemy.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40);
        }

        private void FeintingCut()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.FeintingCut)
                .Name("Feinting Cut")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FeintingCut1)
                .Description("Deals weapon DMG + 12 and has a Reflex DC12 check to inflict Weakened, reducing Attack by 10% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FeintingCut2)
                .Description("Deals weapon DMG + 22 and has a Reflex DC15 check to inflict Weakened, reducing Attack by 15% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FeintingCut3)
                .Description("Deals weapon DMG + 32 and has a Reflex DC18 check to inflict Weakened, reducing Attack by 20% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 35);
        }

        private void FinalForm()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.FinalForm)
                .Name("Final Form")

                .AddPerkLevel()
                .Description("For 20 seconds, single-target Twin Blade combat abilities deal +25% damage and you gain +25 Attack Deflection.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 50);
        }

        private void FlowingFootwork()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.FlowingFootwork)
                .Name("Flowing Footwork")

                .AddPerkLevel()
                .Description("After using a Twin Blade combat ability, gain +10% Evasion for 8 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 22);
        }

        private void GuardedFlow()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.GuardedFlow)
                .Name("Guarded Flow")

                .AddPerkLevel()
                .Description("Using a single-target Twin Blade ability grants +8 Attack Deflection for 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 28);
        }

        private void MirrorStep()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.MirrorStep)
                .Name("Mirror Step")

                .AddPerkLevel()
                .Description("When hit by a target you damaged within the last 6 seconds, you have a 15% chance for your next Twin Blade ability to have no attack delay.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20);
        }

        private void Momentum()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.Momentum)
                .Name("Momentum")

                .AddPerkLevel()
                .Description("Twin Blade abilities that hit 2 or more enemies grant +5% Haste for 8 seconds, up to +15%.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 12)

                .AddPerkLevel()
                .Description("Momentum can stack up to +25% Haste and restores 2 STM whenever a stack is gained.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 32);
        }

        private void PerfectBalance()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.PerfectBalance)
                .Name("Perfect Balance")

                .AddPerkLevel()
                .Description("Single-target Twin Blade abilities restore 3 STM. Area Twin Blade abilities restore 1 STM per target hit, up to 5 STM. This can only trigger once every 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 48);
        }

        private void PrecisionArc()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.PrecisionArc)
                .Name("Precision Arc")

                .AddPerkLevel()
                .Description("Single-target critical hits reduce the target's Defense by 10% for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 40);
        }

        private void PunishingAngle()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.PunishingAngle)
                .Name("Punishing Angle")

                .AddPerkLevel()
                .Description("Deal +12% damage to targets affected by Weakened or Hamstring.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 32);
        }

        private void ReversalCut()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.ReversalCut)
                .Name("Reversal Cut")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ReversalCut1)
                .Description("Can be used after you are hit. Deals weapon DMG + 40 and has a Reflex DC16 check to inflict Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 38);
        }

        private void SplitGuardStrike()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.SplitGuardStrike)
                .Name("Split Guard Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SplitGuardStrike1)
                .Description("Deals weapon DMG + 10 and grants +15% Defense for 10 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SplitGuardStrike2)
                .Description("Deals weapon DMG + 22 and grants +20% Defense for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SplitGuardStrike3)
                .Description("Deals weapon DMG + 34 and grants +25% Defense for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30);
        }

        private void StormRelease()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.StormRelease)
                .Name("Storm Release")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StormRelease1)
                .Description("Consume all Momentum stacks to deal weapon DMG + 15 per stack to all nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45);
        }

        private void SweepingAdvance()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.SweepingAdvance)
                .Name("Sweeping Advance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SweepingAdvance1)
                .Description("Deals weapon DMG + 24 to enemies in a line. If this hits 3 or more enemies, restore 6 STM and gain +10% Haste for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 38);
        }

        private void TempestBloom()
        {
            _builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.TempestBloom)
                .Name("Tempest Bloom")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempestBloom1)
                .Description("Channel for up to 6 seconds, striking all nearby enemies every 2 seconds for weapon DMG + 20. The final hit has a Reflex DC18 check to inflict Knockdown for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 50);
        }

    }
}
