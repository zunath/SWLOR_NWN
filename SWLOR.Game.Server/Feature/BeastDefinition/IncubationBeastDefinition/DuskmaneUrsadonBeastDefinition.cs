using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class DuskmaneUrsadonBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.DuskmaneUrsadon)
                .Name("Duskmane Ursadon")
                .Appearance(AppearanceType.BearBlack)
                .AppearanceScale(1f)
                .SoundSetId(11)
                .PortraitId(147)
                .CombatStats(AbilityType.Perception, AbilityType.Might)
                .Role(BeastRoleType.Evasion)


                ;

			Level1();
			Level2();
			Level3();
			Level4();
			Level5();
			Level6();
			Level7();
			Level8();
			Level9();
			Level10();
			Level11();
			Level12();
			Level13();
			Level14();
			Level15();
			Level16();
			Level17();
			Level18();
			Level19();
			Level20();
			Level21();
			Level22();
			Level23();
			Level24();
			Level25();
			Level26();
			Level27();
			Level28();
			Level29();
			Level30();
			Level31();
			Level32();
			Level33();
			Level34();
			Level35();
			Level36();
			Level37();
			Level38();
			Level39();
			Level40();
			Level41();
			Level42();
			Level43();
			Level44();
			Level45();
			Level46();
			Level47();
			Level48();
			Level49();
			Level50();


            return _builder.Build();
        }


		private void Level1()
		{
			_builder
				.AddLevel()
				.HP(124)
				.FP(3)
				.STM(3)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 0)
				.MaxDefenseBonus(CombatDamageType.Force, 0)
				.MaxResistanceBonus(ResistanceType.Fire, 0)
				.MaxResistanceBonus(ResistanceType.Poison, 0)
				.MaxResistanceBonus(ResistanceType.Electrical, 0)
				.MaxResistanceBonus(ResistanceType.Ice, 0)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level2()
		{
			_builder
				.AddLevel()
				.HP(135)
				.FP(3)
				.STM(3)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 1)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 1)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(146)
				.FP(3)
				.STM(5)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(158)
				.FP(3)
				.STM(5)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(169)
				.FP(3)
				.STM(5)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(180)
				.FP(5)
				.STM(6)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(191)
				.FP(5)
				.STM(6)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(203)
				.FP(5)
				.STM(6)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(214)
				.FP(5)
				.STM(6)
				.DMG(6)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(225)
				.FP(5)
				.STM(7)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(236)
				.FP(5)
				.STM(7)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(248)
				.FP(5)
				.STM(7)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(259)
				.FP(5)
				.STM(8)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(270)
				.FP(5)
				.STM(8)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(281)
				.FP(5)
				.STM(8)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(293)
				.FP(6)
				.STM(9)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(304)
				.FP(6)
				.STM(9)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(315)
				.FP(6)
				.STM(9)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(326)
				.FP(6)
				.STM(9)
				.DMG(9)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(338)
				.FP(6)
				.STM(10)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(349)
				.FP(6)
				.STM(10)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(360)
				.FP(6)
				.STM(10)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(371)
				.FP(6)
				.STM(11)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(383)
				.FP(6)
				.STM(11)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(394)
				.FP(6)
				.STM(11)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(405)
				.FP(7)
				.STM(12)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(416)
				.FP(7)
				.STM(12)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(428)
				.FP(7)
				.STM(12)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(439)
				.FP(7)
				.STM(12)
				.DMG(14)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(450)
				.FP(7)
				.STM(14)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(42)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(461)
				.FP(7)
				.STM(14)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(43)
				.MaxDefenseBonus(CombatDamageType.Physical, 28)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(473)
				.FP(7)
				.STM(14)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(45)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(484)
				.FP(7)
				.STM(15)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(46)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(495)
				.FP(7)
				.STM(15)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(32)
				.MaxEvasionBonus(47)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(506)
				.FP(7)
				.STM(15)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(48)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(518)
				.FP(8)
				.STM(16)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(50)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(529)
				.FP(8)
				.STM(16)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(52)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(540)
				.FP(8)
				.STM(16)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(53)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(551)
				.FP(8)
				.STM(16)
				.DMG(18)
				.Delay(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(54)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(563)
				.FP(8)
				.STM(17)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(55)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(574)
				.FP(8)
				.STM(17)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(56)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(585)
				.FP(8)
				.STM(17)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(59)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(596)
				.FP(8)
				.STM(18)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(39)
				.MaxEvasionBonus(60)
				.MaxDefenseBonus(CombatDamageType.Physical, 39)
				.MaxDefenseBonus(CombatDamageType.Force, 39)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(608)
				.FP(8)
				.STM(18)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 32)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(41)
				.MaxEvasionBonus(61)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(619)
				.FP(8)
				.STM(18)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 33)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(42)
				.MaxEvasionBonus(62)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(630)
				.FP(9)
				.STM(19)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 33)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(63)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(641)
				.FP(9)
				.STM(19)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 33)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(65)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(653)
				.FP(9)
				.STM(19)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 33)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(44)
				.MaxEvasionBonus(66)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 44)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(664)
				.FP(9)
				.STM(19)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 33)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(45)
				.MaxEvasionBonus(68)
				.MaxDefenseBonus(CombatDamageType.Physical, 45)
				.MaxDefenseBonus(CombatDamageType.Force, 45)
				.MaxResistanceBonus(ResistanceType.Fire, 34)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 34)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(675)
				.FP(9)
				.STM(20)
				.DMG(23)
				.Delay(22)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 33)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(46)
				.MaxEvasionBonus(69)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxResistanceBonus(ResistanceType.Fire, 35)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 35)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

    }
}
