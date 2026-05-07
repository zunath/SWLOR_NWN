using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class VibroknifePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AfflictionMastery();
            AmbushTactics();
            AssassinsFocus();
            CalculatedStrikes();
            CascadeFailure();
            CheapShot();
            CripplingPrecision();
            DeadlyPrecision();
            DebilitatingStance();
            Decoy();
            EnfeeblingStrike();
            EvasiveCombat();
            ExploitWeakness();
            Hamstring();
            Incapacitate();
            MarkedForDeath();
            NerveStrike();
            Opportunist();
            PrecisionStrikes();
            SapVitality();
            ShadowStrike();
            SystemicShutdown();
            ToxicCoating();
            VitalStrike();

            return _builder.Build();
        }

        private void AfflictionMastery()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.AfflictionMastery)
                .Name("Affliction Mastery")

                .AddPerkLevel()
                .Description("Debuffs you apply last +30% longer.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 45);
        }

        private void AmbushTactics()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.AmbushTactics)
                .Name("Ambush Tactics")

                .AddPerkLevel()
                .Description("After dealing a critical hit, your next attack within 8 seconds ignores 20% of defense.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 25);
        }

        private void AssassinsFocus()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.AssassinsFocus)
                .Name("Assassin's Focus")

                .AddPerkLevel()
                .Description("After landing a critical hit, gain +5% Accuracy for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 32);
        }

        private void CalculatedStrikes()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.CalculatedStrikes)
                .Name("Calculated Strikes")

                .AddPerkLevel()
                .Description("Auto-attacks have 15% chance to reduce target's Accuracy by 10% for 6s.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 5);
        }

        private void CascadeFailure()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.CascadeFailure)
                .Name("Cascade Failure")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CascadeFailure1)
                .Description("All enemies within the area of effect (cone) take weapon DMG + 25. Fortitude DC15 check to inflict Vulnerable which reduces defense by 10% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 48);
        }

        private void CheapShot()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.CheapShot)
                .Name("Cheap Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CheapShot1)
                .Description("Deals weapon DMG + 8 to a single target. Fortitude DC10 check to inflict Blind for 6 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CheapShot2)
                .Description("Deals weapon DMG + 16 to a single target. Fortitude DC14 check to inflict Blind for 9 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 20);
        }

        private void CripplingPrecision()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.CripplingPrecision)
                .Name("Crippling Precision")

                .AddPerkLevel()
                .Description("Your critical hits reduce target's Evasion by 15% for 10s.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 25);
        }

        private void DeadlyPrecision()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.DeadlyPrecision)
                .Name("Deadly Precision")

                .AddPerkLevel()
                .Description("While active, grants +15% critical hit chance, -20% evasion, and -15% defense.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 12);
        }

        private void DebilitatingStance()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.DebilitatingStance)
                .Name("Debilitating Stance")

                .AddPerkLevel()
                .Description("While active, your attacks inflict Hindered which slows attack speed by 15% for 8 seconds but reduces your attack by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 30);
        }

        private void Decoy()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.Decoy)
                .Name("Decoy")

                .AddPerkLevel()
                .Description("For 12 seconds, enemies targeting you have -25% Accuracy.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 45);
        }

        private void EnfeeblingStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.EnfeeblingStrike)
                .Name("Enfeebling Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnfeeblingStrike1)
                .Description("Deals weapon DMG + 12. Fortitude DC10 check to inflict Weakened which reduces Attack by 10% for 15 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 8)

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnfeeblingStrike2)
                .Description("Deals weapon DMG + 24. Fortitude DC14 check to inflict Weakened which reduces Attack by 15% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnfeeblingStrike3)
                .Description("Deals weapon DMG + 36. Fortitude DC18 check to inflict Weakened which reduces attack by 20% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 35);
        }

        private void EvasiveCombat()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.EvasiveCombat)
                .Name("Evasive Combat")

                .AddPerkLevel()
                .Description("Increases evasion by 10%, reduces enmity by 15%, and reduces attack by 15% for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 15)

                .AddPerkLevel()
                .Description("Increases evasion by 20%, reduces enmity by 25%, and reduces attack by 25% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 38);
        }

        private void ExploitWeakness()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.ExploitWeakness)
                .Name("Exploit Weakness")

                .AddPerkLevel()
                .Description("Deal +12% damage to enemies affected by any debuff.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 15);
        }

        private void Hamstring()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.Hamstring)
                .Name("Hamstring")

                .AddPerkLevel()
                .Description("Your next attack deals +8 DMG. Reflex DC10 check to inflict Hamstring, slowing attack speed by 20% for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 10)

                .AddPerkLevel()
                .Description("Your next attack deals +18 DMG. Reflex DC14 check to inflict Hamstring, slowing attack speed by 20% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 18)

                .AddPerkLevel()
                .Description("Your next attack deals +28 DMG. Reflex DC18 check to inflict Hamstring, slowing attack speed by 20% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 32);
        }

        private void Incapacitate()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.Incapacitate)
                .Name("Incapacitate")

                .AddPerkLevel()
                .Description("Enemies within the area of effect (sphere) receive the Incapacitate debuff which reduces their evasion by 20% for 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 42);
        }

        private void MarkedForDeath()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.MarkedForDeath)
                .Name("Marked for Death")

                .AddPerkLevel()
                .Description("You mark a single target. Your next 3 attacks against them deal +12 DMG each.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 30);
        }

        private void NerveStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.NerveStrike)
                .Name("Nerve Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.NerveStrike1)
                .Description("Deals weapon DMG + 22. Fortitude DC14 to inflict Disoriented which reduces Accuracy and Evasion by 15% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 28);
        }

        private void Opportunist()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.Opportunist)
                .Name("Opportunist")

                .AddPerkLevel()
                .Description("Grants +15% Critical Rate against targets not facing you.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 22);
        }

        private void PrecisionStrikes()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.PrecisionStrikes)
                .Name("Precision Strikes")

                .AddPerkLevel()
                .Description("Critical hits deal +10% damage.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 5);
        }

        private void SapVitality()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.SapVitality)
                .Name("Sap Vitality")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SapVitality1)
                .Description("Deals weapon DMG + 20. Fortitude DC12 check to inflict Exhausted which reduces defense and force defense by 10% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.SapVitality2)
                .Description("Deals weapon DMG + 35. Fortitude DC16 check to inflict Exhausted which reduces defense and force defense by 15% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 38);
        }

        private void ShadowStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.ShadowStrike)
                .Name("Shadow Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStrike1)
                .Description("Deals weapon DMG + 30 to a single target. Reflex DC15 check to inflict 30% Slow for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStrike2)
                .Description("Deals weapon DMG + 48 to a single target. Reflex DC17 to inflict 40% Slow for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 42);
        }

        private void SystemicShutdown()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.SystemicShutdown)
                .Name("Systemic Shutdown")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SystemicShutdown1)
                .Description("All enemies within the area of effect (sphere) take weapon DMG + 15 and an attempt to inflict Weakened, Hamstring, Exhausted, Disoriented, and Toxin is made.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 50);
        }

        private void ToxicCoating()
        {
            _builder.Create(PerkCategoryType.VibroknifeSaboteur, PerkType.ToxicCoating)
                .Name("Toxic Coating")

                .AddPerkLevel()
                .Description("Your next attack deals +10 DMG. Fortitude DC10 check to inflict Toxin for 30 seconds. Toxin deals damage equal to 1% max HP per second.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroknife, 12)

                .AddPerkLevel()
                .Description("Your next attack deals +22 DMG. Fortitude DC15 check to inflict Toxin for 30 seconds. Toxin deals damage equal to 1% max HP per second.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroknife, 40);
        }

        private void VitalStrike()
        {
            _builder.Create(PerkCategoryType.VibroknifeShadow, PerkType.VitalStrike)
                .Name("Vital Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VitalStrike1)
                .Description("Deals weapon DMG + 55. Fortitude DC20 to inflict Vital Strike debuff which causes all physical attacks to ignore 10% of defense for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroknife, 50);
        }
    }
}
