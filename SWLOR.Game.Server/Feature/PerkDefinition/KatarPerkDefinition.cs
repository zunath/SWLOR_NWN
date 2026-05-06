using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class KatarPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AdamantineGuard();
            BreakerReversal();
            CobraReflexes();
            CobraStance();
            CoveringClaws();
            CurrentOverload();
            GuardCounter();
            GuardTraining();
            GuardianReflexes();
            ImpenetrableGrip();
            IronElbows();
            IronWallStance();
            NeuralShock();
            NeurotoxinMastery();
            RedirectingGuard();
            RetaliatoryFlow();
            SerpentsEclipse();
            SpreadingVenom();
            StaticPalm();
            ToxicRush();
            ToxicTempo();
            TwinFangFlurry();
            TwinGuardStance();
            TwinIntercept();
            VenomRhythm();
            VenomSplash();
            WhirlingGuard();

            return _builder.Build();
        }

        private void AdamantineGuard()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.AdamantineGuard)
                .Name("Adamantine Guard")

                .AddPerkLevel()
                .Description("For 20 seconds, gain +40% guard chance. Guarded hits reduce physical damage by 40% and generate greatly increased enmity.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 50);
        }

        private void BreakerReversal()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.BreakerReversal)
                .Name("Breaker Reversal")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BreakerReversal1)
                .Description("After guarding an attack, your next katar attack deals weapon DMG + 35 and inflicts Exposed, reducing Defense by 15% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45);
        }

        private void CobraReflexes()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.CobraReflexes)
                .Name("Cobra Reflexes")

                .AddPerkLevel()
                .Description("Critical hits against poisoned targets restore 4 STM.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 32);
        }

        private void CobraStance()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.CobraStance)
                .Name("Cobra Stance")

                .AddPerkLevel()
                .Description("While active, attacks have a 10% chance to inflict Poison for 30 seconds and you gain +10% Attack, but Defense is reduced by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 15);
        }

        private void CoveringClaws()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.CoveringClaws)
                .Name("Covering Claws")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringClaws1)
                .Description("Strike enemies in a cone for weapon DMG + 20. Enemies hit generate +25% Enmity toward you for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25);
        }

        private void CurrentOverload()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.CurrentOverload)
                .Name("Current Overload")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CurrentOverload1)
                .Description("Deals weapon DMG + 35. If the target is Poisoned or Disoriented, consume one effect to deal +25 DMG and make a Reflex DC18 check to inflict Stunned for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 42);
        }

        private void GuardCounter()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.GuardCounter)
                .Name("Guard Counter")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardCounter1)
                .Description("Your next attack deals weapon DMG + 8. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 16 instead.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardCounter2)
                .Description("Your next attack deals weapon DMG + 18. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 30 instead.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GuardCounter3)
                .Description("Your next attack deals weapon DMG + 28. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 45 and has a Reflex DC16 check to inflict Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 38);
        }

        private void GuardTraining()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.GuardTraining)
                .Name("Guard Training")

                .AddPerkLevel()
                .Description("Dual wielding katars grants a 15% chance to guard against physical attacks, reducing that hit's damage by 20% and generating extra enmity.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 5)

                .AddPerkLevel()
                .Description("Guard chance increases to 25% and guarded hits restore 2 STM.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 15)

                .AddPerkLevel()
                .Description("Guard chance increases to 35% and guarded hits reduce physical damage by 30%.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 28);
        }

        private void GuardianReflexes()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.GuardianReflexes)
                .Name("Guardian Reflexes")

                .AddPerkLevel()
                .Description("When reduced below 35% HP, gain +25% guard chance for 12 seconds. This can only trigger once every 3 minutes.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 48);
        }

        private void ImpenetrableGrip()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ImpenetrableGrip)
                .Name("Impenetrable Grip")

                .AddPerkLevel()
                .Description("While dual wielding katars, gain +20% resistance to Knockdown and Dazed effects. Guarded hits restore 4 STM.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 40);
        }

        private void IronElbows()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.IronElbows)
                .Name("Iron Elbows")

                .AddPerkLevel()
                .GrantsFeat(FeatType.IronElbows1)
                .Description("Deals weapon DMG + 15 to all nearby enemies and generates extra enmity.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 18);
        }

        private void IronWallStance()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.IronWallStance)
                .Name("Iron Wall Stance")

                .AddPerkLevel()
                .Description("While active, grants +25% Defense, +20% Force Defense, and +30% Enmity generation, but reduces Attack by 25%.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 42);
        }

        private void NeuralShock()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.NeuralShock)
                .Name("Neural Shock")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NeuralShock1)
                .Description("Deals weapon DMG + 20. If the target is Disoriented, they make a Reflex DC16 check or become Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30);
        }

        private void NeurotoxinMastery()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.NeurotoxinMastery)
                .Name("Neurotoxin Mastery")

                .AddPerkLevel()
                .Description("Poison effects you apply also reduce the target's Attack by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 48);
        }

        private void RedirectingGuard()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.RedirectingGuard)
                .Name("Redirecting Guard")

                .AddPerkLevel()
                .Description("When you guard an attack, your next katar attack within 10 seconds gains +10% critical chance and deals +10 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20);
        }

        private void RetaliatoryFlow()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.RetaliatoryFlow)
                .Name("Retaliatory Flow")

                .AddPerkLevel()
                .Description("After you guard a hit, your next Guard Counter within 8 seconds costs 2 less STM and deals +8 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 32);
        }

        private void SerpentsEclipse()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.SerpentsEclipse)
                .Name("Serpent's Eclipse")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SerpentsEclipse1)
                .Description("All enemies in an area of effect (sphere) take weapon DMG + 25. Fortitude DC18 check to inflict Poison and Reflex DC18 check to inflict Disoriented. Enemies already affected by either effect take +30 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 50);
        }

        private void SpreadingVenom()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.SpreadingVenom)
                .Name("Spreading Venom")

                .AddPerkLevel()
                .Description("When a poisoned target dies, the nearest enemy within 5 meters makes a Fortitude DC14 check or becomes poisoned for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 40);
        }

        private void StaticPalm()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.StaticPalm)
                .Name("Static Palm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.StaticPalm1)
                .Description("Your next attack deals weapon DMG + 8 and has a Reflex DC12 check to inflict Disoriented for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.StaticPalm2)
                .Description("Your next attack deals weapon DMG + 18 and has a Reflex DC15 check to inflict Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.StaticPalm3)
                .Description("Your next attack deals weapon DMG + 28 and has a Reflex DC18 check to inflict Disoriented for 15 seconds. Poisoned targets also make a Reflex DC18 check or become Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 38);
        }

        private void ToxicRush()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ToxicRush)
                .Name("Toxic Rush")

                .AddPerkLevel()
                .Description("Gain +20% Haste and +15% Attack for 20 seconds. Attacks against poisoned targets restore 2 STM during this effect.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45);
        }

        private void ToxicTempo()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ToxicTempo)
                .Name("Toxic Tempo")

                .AddPerkLevel()
                .Description("Katar abilities deal +8% damage to targets affected by Poison or Disoriented.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 22);
        }

        private void TwinFangFlurry()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.TwinFangFlurry)
                .Name("Twin Fang Flurry")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinFangFlurry1)
                .Description("Strike twice for weapon DMG + 10 each. If the target is poisoned, the second strike has a Fortitude DC15 check to inflict Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25);
        }

        private void TwinGuardStance()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.TwinGuardStance)
                .Name("Twin Guard Stance")

                .AddPerkLevel()
                .Description("While active, grants +15% Defense and +20% Enmity generation, but reduces Attack by 15%.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 12);
        }

        private void TwinIntercept()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.TwinIntercept)
                .Name("Twin Intercept")

                .AddPerkLevel()
                .Description("Target an ally within 6 meters. They gain a damage shield equal to 20% of your maximum HP and +15% Defense for 8 seconds. You gain extra enmity toward enemies near that ally.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30);
        }

        private void VenomRhythm()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.VenomRhythm)
                .Name("Venom Rhythm")

                .AddPerkLevel()
                .Description("Attacks against poisoned targets have a 15% chance to deal +6 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 12);
        }

        private void VenomSplash()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.VenomSplash)
                .Name("Venom Splash")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VenomSplash1)
                .Description("Deals weapon DMG + 18 to enemies in a cone and has a Fortitude DC15 check to inflict Poison for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 28);
        }

        private void WhirlingGuard()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.WhirlingGuard)
                .Name("Whirling Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.WhirlingGuard1)
                .Description("For 12 seconds, gain +20% guard chance and deal 8 DMG back to attackers whenever you guard a hit.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 35);
        }
    }
}
