using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class BrassjawPyraliskBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.BrassjawPyralisk)
                .Name("Brassjaw Pyralisk")
                .Appearance(AppearanceType.DragonMetBrass)
                .AppearanceScale(0.5f)
                .SoundSetId(108)
                .PortraitId(349)
                .CombatStats(AbilityType.Perception, AbilityType.Agility)
                .Role(BeastRoleType.Damage)


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
				.HP(34)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(0)
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
				.HP(56)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 0)
				.MaxResistanceBonus(ResistanceType.Electrical, 0)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(68)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 0)
				.MaxResistanceBonus(ResistanceType.Electrical, 0)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(79)
				.FP(7)
				.STM(7)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 0)
				.MaxResistanceBonus(ResistanceType.Electrical, 0)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(90)
				.FP(7)
				.STM(8)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(101)
				.FP(7)
				.STM(8)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(113)
				.FP(7)
				.STM(8)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(10)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(124)
				.FP(7)
				.STM(9)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(135)
				.FP(8)
				.STM(9)
				.DMG(8)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(146)
				.FP(8)
				.STM(10)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(158)
				.FP(8)
				.STM(10)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(169)
				.FP(8)
				.STM(10)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(180)
				.FP(8)
				.STM(11)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(191)
				.FP(9)
				.STM(11)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(203)
				.FP(9)
				.STM(12)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(214)
				.FP(9)
				.STM(12)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(225)
				.FP(9)
				.STM(12)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(236)
				.FP(9)
				.STM(14)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(248)
				.FP(10)
				.STM(14)
				.DMG(15)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(259)
				.FP(10)
				.STM(15)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(28)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(270)
				.FP(10)
				.STM(15)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(281)
				.FP(10)
				.STM(15)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(30)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(293)
				.FP(10)
				.STM(16)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(304)
				.FP(11)
				.STM(16)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(315)
				.FP(11)
				.STM(17)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(326)
				.FP(11)
				.STM(17)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(338)
				.FP(11)
				.STM(17)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(349)
				.FP(11)
				.STM(18)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(39)
				.MaxAccuracyBonus(39)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(360)
				.FP(12)
				.STM(18)
				.DMG(28)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(41)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(371)
				.FP(12)
				.STM(19)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(42)
				.MaxAccuracyBonus(42)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(383)
				.FP(12)
				.STM(19)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(43)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(394)
				.FP(12)
				.STM(19)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(45)
				.MaxAccuracyBonus(45)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(405)
				.FP(12)
				.STM(20)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(46)
				.MaxAccuracyBonus(46)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(416)
				.FP(14)
				.STM(20)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(47)
				.MaxAccuracyBonus(47)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(428)
				.FP(14)
				.STM(21)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(48)
				.MaxAccuracyBonus(48)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(439)
				.FP(14)
				.STM(21)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(50)
				.MaxAccuracyBonus(50)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(450)
				.FP(14)
				.STM(21)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(52)
				.MaxAccuracyBonus(52)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 34)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 34)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(461)
				.FP(14)
				.STM(23)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(53)
				.MaxAccuracyBonus(53)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 35)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 35)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(473)
				.FP(15)
				.STM(23)
				.DMG(36)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(54)
				.MaxAccuracyBonus(54)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 36)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 36)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(484)
				.FP(15)
				.STM(24)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(55)
				.MaxAccuracyBonus(55)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 37)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 37)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(495)
				.FP(15)
				.STM(24)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(56)
				.MaxAccuracyBonus(56)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 37)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 37)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(506)
				.FP(15)
				.STM(24)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(59)
				.MaxAccuracyBonus(59)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 38)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 38)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(518)
				.FP(15)
				.STM(25)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(60)
				.MaxAccuracyBonus(60)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 39)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 39)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(529)
				.FP(16)
				.STM(25)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(61)
				.MaxAccuracyBonus(61)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 41)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 41)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(540)
				.FP(16)
				.STM(26)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(62)
				.MaxAccuracyBonus(62)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 42)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 42)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(551)
				.FP(16)
				.STM(26)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(63)
				.MaxAccuracyBonus(63)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 43)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 43)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(563)
				.FP(16)
				.STM(26)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(65)
				.MaxAccuracyBonus(65)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 43)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 43)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(574)
				.FP(16)
				.STM(27)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(66)
				.MaxAccuracyBonus(66)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 44)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 44)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(585)
				.FP(17)
				.STM(27)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(68)
				.MaxAccuracyBonus(68)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 45)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 45)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(596)
				.FP(17)
				.STM(28)
				.DMG(41)
				.Delay(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(69)
				.MaxAccuracyBonus(69)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 46)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 46)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 6);
		}

    }
}
