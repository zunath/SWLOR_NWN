using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Claw();
            BolsterAttack();
            Hasten();
            PoisonBreath();
            IceBreath();
            EnduranceLink();
            Bite();
            FlameBreath();
            ShockingSlash();
            EvasiveManeuver();
            Assault();
            Sniff();
            ForceTouch();
            Innervate();
            ForceLink();
            DiseasedTouch();
            Clip();
            SpinningClaw();
            BeastSpeed();
            BolsterArmor();
            Anger();
            FocusAttention();

            return _builder.Build();
        }



        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void OnEnduranceLinkHit()
        {
            var beast = OBJECT_SELF;
            var item = GetSpellCastItem();

            if (!BeastMastery.IsPlayerBeast(beast) || GetResRef(item) != BeastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = Perk.GetPerkLevel(beast, PerkType.EnduranceLink) * 10;

                if (Random.D100(1) <= chance)
                {
                    Stat.RestoreStamina(player, 1);
                }
            }
        }


        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void OnForceLinkHit()
        {
            var beast = OBJECT_SELF;
            var item = GetSpellCastItem();

            if (!BeastMastery.IsPlayerBeast(beast) || GetResRef(item) != BeastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = Perk.GetPerkLevel(beast, PerkType.ForceLink) * 10;

                if (Random.D100(1) <= chance)
                {
                    Stat.RestoreFP(player, 1);
                }
            }
        }


        private void Claw()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.Claw)
                .Name("Claw")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 8 physical DMG and inflicts Bleed for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 11 physical DMG and inflicts Bleed for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and inflicts Bleed for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 17 physical DMG and inflicts Bleed for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 physical DMG and inflicts Bleed for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Claw5);
        }


        private void BolsterAttack()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.BolsterAttack)
                .Name("Bolster Attack")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 5 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack1)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 10 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack2)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 15 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack3)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 20 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack4)

                .AddPerkLevel()
                .Description("Increases the beast's attack by 25 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.BolsterAttack5);
        }


        private void Hasten()
        {
            _builder.Create(PerkCategoryType.BeastBalanced, PerkType.Hasten)
                .Name("Hasten")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Reduces the beast's attack delay by 10% for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten1)

                .AddPerkLevel()
                .Description("Reduces the beast's attack delay by 20% for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten2)

                .AddPerkLevel()
                .Description("Reduces the beast's and beastmaster's attack delay by 30% for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Balanced)
                .GrantsFeat(FeatType.Hasten3);
        }


        private void PoisonBreath()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.PoisonBreath)
                .Name("Poison Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 poison DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath1)

                .AddPerkLevel()
                .Description("Deals 12 poison DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath2)

                .AddPerkLevel()
                .Description("Deals 16 poison DMG to all targets within a cone in front of the beast. Also inflicts Poison.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath3)

                .AddPerkLevel()
                .Description("Deals 20 poison DMG to all targets within a cone in front of the beast. Also inflicts Poison.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath4)

                .AddPerkLevel()
                .Description("Deals 24 poison DMG to all targets within a cone in front of the beast. Also inflicts Poison.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath5);
        }


        private void IceBreath()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.IceBreath)
                .Name("Ice Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 ice DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath1)

                .AddPerkLevel()
                .Description("Deals 12 ice DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath2)

                .AddPerkLevel()
                .Description("Deals 16 ice DMG to all targets within a cone in front of the beast. Also inflicts Freezing.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath3)

                .AddPerkLevel()
                .Description("Deals 20 ice DMG to all targets within a cone in front of the beast. Also inflicts Freezing.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath4)

                .AddPerkLevel()
                .Description("Deals 24 ice DMG to all targets within a cone in front of the beast. Also inflicts Freezing.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath5);
        }


        private void EnduranceLink()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.EnduranceLink)
                .Name("Endurance Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink3);
        }


        private void Bite()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.Bite)
                .Name("Bite")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 physical DMG.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 physical DMG.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 24 physical DMG.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 28 physical DMG.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.Bite5);
        }


        private void FlameBreath()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.FlameBreath)
                .Name("Flame Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 fire DMG to all targets within a cone in front of the beast.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath1)

                .AddPerkLevel()
                .Description("Deals 12 fire DMG to all targets within a cone in front of the beast.")
                .Price(2)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath2)

                .AddPerkLevel()
                .Description("Deals 16 fire DMG to all targets within a cone in front of the beast. Also inflicts Burning.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath3)

                .AddPerkLevel()
                .Description("Deals 20 fire DMG to all targets within a cone in front of the beast. Also inflicts Burning.")
                .Price(3)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath4)

                .AddPerkLevel()
                .Description("Deals 24 fire DMG to all targets within a cone in front of the beast. Also inflicts Burning.")
                .Price(3)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.FlameBreath5);
        }


        private void ShockingSlash()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.ShockingSlash)
                .Name("Shocking Slash")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 electrical DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash1)

                .AddPerkLevel()
                .Description("Deals 12 electrical DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash2)

                .AddPerkLevel()
                .Description("Deals 16 electrical DMG to all targets within a cone in front of the beast. Also inflicts Shock.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash3)

                .AddPerkLevel()
                .Description("Deals 20 electrical DMG to all targets within a cone in front of the beast. Also inflicts Shock.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash4)

                .AddPerkLevel()
                .Description("Deals 24 electrical DMG to all targets within a cone in front of the beast. Also inflicts Shock.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Damage)
                .GrantsFeat(FeatType.ShockingSlash5);
        }


        private void EvasiveManeuver()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 5 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver1)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 10 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver2)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 15 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver3)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 20 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver4)

                .AddPerkLevel()
                .Description("Increases the beast's evasion by 25 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.EvasiveManeuver5);
        }


        private void Assault()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.Assault)
                .Name("Assault")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 10 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 22 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 26 physical DMG and increases the beast's evasion by 10 for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Assault5);
        }


        private void Sniff()
        {
            _builder.Create(PerkCategoryType.BeastEvasion, PerkType.Sniff)
                .Name("Sniff")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 8.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Sniff1)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 15.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Sniff2)

                .AddPerkLevel()
                .Description("Increases the chance to find rare items by 25.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Evasion)
                .GrantsFeat(FeatType.Sniff3);
        }


        private void ForceTouch()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceTouch)
                .Name("Force Touch")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 force DMG.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 force DMG.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 force DMG.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 24 force DMG.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 28 force DMG.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch5);
        }


        private void Innervate()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.Innervate)
                .Name("Innervate")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast restores 30 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate1)

                .AddPerkLevel()
                .Description("The beast restores 40 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate2)

                .AddPerkLevel()
                .Description("The beast restores 60 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate3)

                .AddPerkLevel()
                .Description("The beast restores 80 HP to a single target.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate4)

                .AddPerkLevel()
                .Description("The beast restores 120 HP to a single target.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate5);
        }

        private void ForceLink()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceLink)
                .Name("Force Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink3);
        }


        private void DiseasedTouch()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.DiseasedTouch)
                .Name("Diseased Touch")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 8 poison DMG and inflicts Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(5)
                .GrantsFeat(FeatType.DiseasedTouch1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 11 poison DMG and inflicts Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(15)
                .GrantsFeat(FeatType.DiseasedTouch2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 poison DMG and inflicts Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)
                .GrantsFeat(FeatType.DiseasedTouch3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 17 poison DMG and inflicts Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .GrantsFeat(FeatType.DiseasedTouch4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 poison DMG and inflicts Disease for 30 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .GrantsFeat(FeatType.DiseasedTouch5);
        }


        private void Clip()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.Clip)
                .Name("Clip")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 10 physical DMG and inflicts Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(5)
                .GrantsFeat(FeatType.Clip1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 physical DMG and inflicts Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(15)
                .GrantsFeat(FeatType.Clip2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 14 physical DMG and inflicts Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)
                .GrantsFeat(FeatType.Clip3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 physical DMG and inflicts Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(35)
                .GrantsFeat(FeatType.Clip4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 18 physical DMG and inflicts Stun for 3 seconds.")
                .Price(2)
                .RequirementBeastLevel(45)
                .GrantsFeat(FeatType.Clip5);
        }


        private void SpinningClaw()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.SpinningClaw)
                .Name("Spinning Claw")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 8 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(5)
                .GrantsFeat(FeatType.SpinningClaw1)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 12 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(15)
                .GrantsFeat(FeatType.SpinningClaw2)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 15 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(25)
                .GrantsFeat(FeatType.SpinningClaw3)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 18 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(35)
                .GrantsFeat(FeatType.SpinningClaw4)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby enemies for 21 physical DMG each.")
                .Price(2)
                .RequirementBeastLevel(45)
                .GrantsFeat(FeatType.SpinningClaw5);
        }


        private void BeastSpeed()
        {
            _builder.Create(PerkCategoryType.BeastGeneral, PerkType.BeastSpeed)
                .Name("Beast Speed")
                .GroupType(PerkGroupType.Beast)
                .TriggerPurchase((player) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, player);
                    if (!BeastMastery.IsPlayerBeast(beast))
                        return;

                    // todo: Adjust active beast's attack delay
                })
                .TriggerRefund((player) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, player);
                    if (!BeastMastery.IsPlayerBeast(beast))
                        return;

                    // todo: Adjust active beast's attack delay
                })

                .AddPerkLevel()
                .Description("The beast's attack delay reduces by 10%.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, 10)
                .Price(3)
                .RequirementBeastLevel(15)

                .AddPerkLevel()
                .Description("The beast's attack delay reduces by 20%.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, 20)
                .Price(3)
                .RequirementBeastLevel(30)

                .AddPerkLevel()
                .Description("The beast's attack delay reduces by 30%.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, 30)
                .Price(3)
                .RequirementBeastLevel(45);
        }


        private void BolsterArmor()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.BolsterArmor)
                .Name("Bolster Armor")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Increases the beast's Physical Defense by 5 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor1)

                .AddPerkLevel()
                .Description("Increases the beast's Physical Defense by 10 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor2)

                .AddPerkLevel()
                .Description("Increases the beast's Physical Defense by 15 for 5 minutes.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor3)

                .AddPerkLevel()
                .Description("Increases the beast's Physical Defense by 20 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor4)

                .AddPerkLevel()
                .Description("Increases the beast's Physical Defense by 25 for 5 minutes.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.BolsterArmor5);
        }


        private void Anger()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.Anger)
                .Name("Anger")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger1)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger2)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger3)

                .AddPerkLevel()
                .Description("Goads all enemies within range into attacking the beast.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger4)

                .AddPerkLevel()
                .Description("Goads all enemies within range into attacking the beast.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger5);
        }


        private void FocusAttention()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.FocusAttention)
                .Name("Focus Attention")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 10%.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention1)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 20%.")
                .Price(2)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention2)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 30%.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention3)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 40%.")
                .Price(3)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention4)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 50%.")
                .Price(3)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.FocusAttention5);
        }
    }
}

