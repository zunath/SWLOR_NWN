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
    public class VibrobladePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BerserkerStance();
            BloodFrenzy();
            Carve();
            CoveringStrike();
            CrimsonFury();
            DefensiveStance();
            Executioner();
            FortifiedPosition();
            GuardiansRiposte();
            Invincible();
            RendingStrike();
            SavageCleave();
            SavageReflexes();
            ShieldTraining();
            ShieldWall();
            Unbreakable();
            WhirlwindAssault();

            return _builder.Build();
        }

        private void BerserkerStance()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.BerserkerStance)
                .Name("Berserker Stance")

                .AddPerkLevel()
                .Description("While active, grants +15% Attack, +10% Haste, -20% Defense, and -20% Force Defense.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 15)

                .AddPerkLevel()
                .Description("While active, grants +25% Attack, +15% Haste, -20% Defense, and -20% Force Defense.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 48);
        }

        private void BloodFrenzy()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.BloodFrenzy)
                .Name("Blood Frenzy")

                .AddPerkLevel()
                .Description("Defeating an enemy restores 15 STM and grants +10% Haste for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 50);
        }

        private void Carve()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Carve)
                .Name("Carve")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Carve1)
                .Description("Deals weapon DMG + 35, applies Hemorrhage which increases the damage your target takes by 10% for 12 seconds")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 45);
        }

        private void CoveringStrike()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.CoveringStrike)
                .Name("Covering Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoveringStrike1)
                .Description("Strike in a line for weapon DMG + 20. Enemies hit generate +25% Enmity toward you for 12s.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 32);
        }

        private void CrimsonFury()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.CrimsonFury)
                .Name("Crimson Fury")

                .AddPerkLevel()
                .Description("Each bleeding enemy within 10m grants you +3% Attack (max +15%).")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 42);
        }

        private void DefensiveStance()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.DefensiveStance)
                .Name("Defensive Stance")

                .AddPerkLevel()
                .Description("While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 22)

                .AddPerkLevel()
                .Description("While active, grants +30% to Enmity generation, +20% Defense, +20% Force Defense, -20% Attack, and -20% Force Attack")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 45);
        }

        private void Executioner()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.Executioner)
                .Name("Executioner")

                .AddPerkLevel()
                .Description("Deal +15% damage to targets below 30% HP.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 32);
        }

        private void FortifiedPosition()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.FortifiedPosition)
                .Name("Fortified Position")

                .AddPerkLevel()
                .Description("Grants +2 Will, +2 Fortitude, +2 Reflex with shield equipped.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 15)

                .AddPerkLevel()
                .Description("Grants +4 Will, +4 Fortitude, +4 Reflex with shield equipped total.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 38);
        }

        private void GuardiansRiposte()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.GuardiansRiposte)
                .Name("Guardian's Riposte")

                .AddPerkLevel()
                .Description("Receive Guardian's Riposte after deflecting an attack with a shield. Your next attack within 12s deals +10 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 30);
        }

        private void Invincible()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Invincible)
                .Name("Invincible")

                .AddPerkLevel()
                .Description("You become invulnerable to physical damage for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 50);
        }

        private void RendingStrike()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.RendingStrike)
                .Name("Rending Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RendingStrike1)
                .Description("Deals weapon DMG + 18. Fortitude DC12 check to inflict Exposed which reduces Defense by 15% for 10s.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 22)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RendingStrike2)
                .Description("Deals weapon DMG + 32. Fortitude DC16 check to inflict Exposed which reduces Defense by 25% for 12s.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 38);
        }

        private void SavageCleave()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.SavageCleave)
                .Name("Savage Cleave")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SavageCleave1)
                .Description("Strike all enemies in front for weapon DMG + 25.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 25);
        }

        private void SavageReflexes()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.SavageReflexes)
                .Name("Savage Reflexes")

                .AddPerkLevel()
                .Description("Auto-attacks have 10% chance to deal +8 DMG.")
                .Price(2)
                .RequirementSkill(SkillType.Vibroblade, 5);
        }

        private void ShieldTraining()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldTraining)
                .Name("Shield Training")

                .AddPerkLevel()
                .Description("When you successfully deflect an attack with a shield, gain +3% Evasion and +3% Enmity for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 5);
        }

        private void ShieldWall()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.ShieldWall)
                .Name("Shield Wall")

                .AddPerkLevel()
                .Description("Channel for up to 6s. Allies within 5m gain +15% Physical Defense, you gain +25% Enmity for 1 minute.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 25);
        }

        private void Unbreakable()
        {
            _builder.Create(PerkCategoryType.VibrobladeDefense, PerkType.Unbreakable)
                .Name("Unbreakable")

                .AddPerkLevel()
                .Description("When reduced below 25% HP with shield equipped, gain +40% Physical Defense for 10s. Once per 5min.")
                .Price(4)
                .RequirementSkill(SkillType.Vibroblade, 42);
        }

        private void WhirlwindAssault()
        {
            _builder.Create(PerkCategoryType.VibrobladeOffense, PerkType.WhirlwindAssault)
                .Name("Whirlwind Assault")

                .AddPerkLevel()
                .GrantsFeat(FeatType.WhirlwindAssault1)
                .Description("Deal weapon DMG + 12 to all nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.WhirlwindAssault2)
                .Description("Deal weapon DMG + 20 to all nearby enemies.")
                .Price(3)
                .RequirementSkill(SkillType.Vibroblade, 30);
        }
    }
}
