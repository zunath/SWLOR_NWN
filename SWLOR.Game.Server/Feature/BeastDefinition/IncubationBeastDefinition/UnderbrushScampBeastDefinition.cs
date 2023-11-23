using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class UnderbrushScampBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.UnderbrushScamp)
                .Name("Underbrush Scamp")
                .Appearance(AppearanceType.FoxRed)
                .SoundSetId(252)
                .PortraitId(189)
                .CombatStats(AbilityType.Perception, AbilityType.Agility)
                .Role(BeastRoleType.Evasion);

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
				.HP(30)
				.FP(3)
				.STM(3)
				.DMG(6)
				.Stat(AbilityType.Might, 6)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 9)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 0)
				.MaxDefenseBonus(CombatDamageType.Force, 0)
				.MaxDefenseBonus(CombatDamageType.Fire, 0)
				.MaxDefenseBonus(CombatDamageType.Poison, 0)
				.MaxDefenseBonus(CombatDamageType.Electrical, 0)
				.MaxDefenseBonus(CombatDamageType.Ice, 0)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level2()
		{
			_builder
				.AddLevel()
				.HP(42)
				.FP(3)
				.STM(3)
				.DMG(6)
				.Stat(AbilityType.Might, 6)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 9)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 0)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxDefenseBonus(CombatDamageType.Fire, 1)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 1)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(48)
				.FP(3)
				.STM(4)
				.DMG(6)
				.Stat(AbilityType.Might, 6)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(54)
				.FP(3)
				.STM(4)
				.DMG(6)
				.Stat(AbilityType.Might, 6)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(60)
				.FP(3)
				.STM(4)
				.DMG(6)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 3)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 3)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(66)
				.FP(4)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(72)
				.FP(4)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(78)
				.FP(4)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxDefenseBonus(CombatDamageType.Fire, 5)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 5)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(84)
				.FP(4)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 11)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(90)
				.FP(4)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 12)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(96)
				.FP(4)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 13)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(102)
				.FP(4)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 14)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(108)
				.FP(4)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxDefenseBonus(CombatDamageType.Fire, 8)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 16)
				.MaxDefenseBonus(CombatDamageType.Ice, 8)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(114)
				.FP(4)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 17)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(120)
				.FP(4)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 18)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(126)
				.FP(5)
				.STM(8)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 19)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(132)
				.FP(5)
				.STM(8)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 20)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(138)
				.FP(5)
				.STM(8)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 22)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(144)
				.FP(5)
				.STM(8)
				.DMG(10)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 23)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(150)
				.FP(5)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 24)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(156)
				.FP(5)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 25)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(162)
				.FP(5)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 26)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(168)
				.FP(5)
				.STM(10)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxDefenseBonus(CombatDamageType.Fire, 14)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 28)
				.MaxDefenseBonus(CombatDamageType.Ice, 14)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(174)
				.FP(5)
				.STM(10)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 29)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(180)
				.FP(5)
				.STM(10)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 30)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(186)
				.FP(6)
				.STM(11)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 11)
				.MaxDefenseBonus(CombatDamageType.Electrical, 31)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(192)
				.FP(6)
				.STM(11)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 11)
				.MaxDefenseBonus(CombatDamageType.Electrical, 33)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(198)
				.FP(6)
				.STM(11)
				.DMG(15)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 11)
				.MaxDefenseBonus(CombatDamageType.Electrical, 34)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(204)
				.FP(6)
				.STM(11)
				.DMG(15)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 12)
				.MaxDefenseBonus(CombatDamageType.Electrical, 35)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(210)
				.FP(6)
				.STM(12)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 12)
				.MaxDefenseBonus(CombatDamageType.Electrical, 36)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(216)
				.FP(6)
				.STM(12)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxDefenseBonus(CombatDamageType.Fire, 19)
				.MaxDefenseBonus(CombatDamageType.Poison, 13)
				.MaxDefenseBonus(CombatDamageType.Electrical, 37)
				.MaxDefenseBonus(CombatDamageType.Ice, 19)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(222)
				.FP(6)
				.STM(12)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 39)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 13)
				.MaxDefenseBonus(CombatDamageType.Electrical, 39)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(228)
				.FP(6)
				.STM(13)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 13)
				.MaxDefenseBonus(CombatDamageType.Electrical, 40)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(234)
				.FP(6)
				.STM(13)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxDefenseBonus(CombatDamageType.Fire, 21)
				.MaxDefenseBonus(CombatDamageType.Poison, 14)
				.MaxDefenseBonus(CombatDamageType.Electrical, 41)
				.MaxDefenseBonus(CombatDamageType.Ice, 21)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(240)
				.FP(6)
				.STM(13)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxDefenseBonus(CombatDamageType.Fire, 21)
				.MaxDefenseBonus(CombatDamageType.Poison, 14)
				.MaxDefenseBonus(CombatDamageType.Electrical, 42)
				.MaxDefenseBonus(CombatDamageType.Ice, 21)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(246)
				.FP(7)
				.STM(14)
				.DMG(19)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxDefenseBonus(CombatDamageType.Fire, 22)
				.MaxDefenseBonus(CombatDamageType.Poison, 15)
				.MaxDefenseBonus(CombatDamageType.Electrical, 43)
				.MaxDefenseBonus(CombatDamageType.Ice, 22)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(252)
				.FP(7)
				.STM(14)
				.DMG(19)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(31)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 45)
				.MaxDefenseBonus(CombatDamageType.Fire, 23)
				.MaxDefenseBonus(CombatDamageType.Poison, 15)
				.MaxDefenseBonus(CombatDamageType.Electrical, 45)
				.MaxDefenseBonus(CombatDamageType.Ice, 23)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(258)
				.FP(7)
				.STM(14)
				.DMG(19)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxDefenseBonus(CombatDamageType.Fire, 23)
				.MaxDefenseBonus(CombatDamageType.Poison, 16)
				.MaxDefenseBonus(CombatDamageType.Electrical, 46)
				.MaxDefenseBonus(CombatDamageType.Ice, 23)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(264)
				.FP(7)
				.STM(14)
				.DMG(19)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 47)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 16)
				.MaxDefenseBonus(CombatDamageType.Electrical, 47)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(270)
				.FP(7)
				.STM(15)
				.DMG(24)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 48)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 16)
				.MaxDefenseBonus(CombatDamageType.Electrical, 48)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(276)
				.FP(7)
				.STM(15)
				.DMG(24)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 49)
				.MaxDefenseBonus(CombatDamageType.Fire, 25)
				.MaxDefenseBonus(CombatDamageType.Poison, 17)
				.MaxDefenseBonus(CombatDamageType.Electrical, 49)
				.MaxDefenseBonus(CombatDamageType.Ice, 25)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(282)
				.FP(7)
				.STM(15)
				.DMG(24)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 51)
				.MaxDefenseBonus(CombatDamageType.Fire, 26)
				.MaxDefenseBonus(CombatDamageType.Poison, 17)
				.MaxDefenseBonus(CombatDamageType.Electrical, 51)
				.MaxDefenseBonus(CombatDamageType.Ice, 26)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(288)
				.FP(7)
				.STM(16)
				.DMG(24)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 52)
				.MaxDefenseBonus(CombatDamageType.Fire, 26)
				.MaxDefenseBonus(CombatDamageType.Poison, 18)
				.MaxDefenseBonus(CombatDamageType.Electrical, 52)
				.MaxDefenseBonus(CombatDamageType.Ice, 26)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(294)
				.FP(7)
				.STM(16)
				.DMG(24)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 53)
				.MaxDefenseBonus(CombatDamageType.Fire, 27)
				.MaxDefenseBonus(CombatDamageType.Poison, 18)
				.MaxDefenseBonus(CombatDamageType.Electrical, 53)
				.MaxDefenseBonus(CombatDamageType.Ice, 27)
				.MaxSavingThrowBonus(SavingThrow.Will, 4)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(300)
				.FP(7)
				.STM(16)
				.DMG(24)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 54)
				.MaxDefenseBonus(CombatDamageType.Fire, 28)
				.MaxDefenseBonus(CombatDamageType.Poison, 18)
				.MaxDefenseBonus(CombatDamageType.Electrical, 54)
				.MaxDefenseBonus(CombatDamageType.Ice, 28)
				.MaxSavingThrowBonus(SavingThrow.Will, 5)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(306)
				.FP(8)
				.STM(17)
				.DMG(24)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 55)
				.MaxDefenseBonus(CombatDamageType.Fire, 28)
				.MaxDefenseBonus(CombatDamageType.Poison, 19)
				.MaxDefenseBonus(CombatDamageType.Electrical, 55)
				.MaxDefenseBonus(CombatDamageType.Ice, 28)
				.MaxSavingThrowBonus(SavingThrow.Will, 5)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(312)
				.FP(8)
				.STM(17)
				.DMG(24)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 57)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 19)
				.MaxDefenseBonus(CombatDamageType.Electrical, 57)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 5)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(318)
				.FP(8)
				.STM(17)
				.DMG(24)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 58)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 58)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 5)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(324)
				.FP(8)
				.STM(17)
				.DMG(24)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 59)
				.MaxDefenseBonus(CombatDamageType.Fire, 30)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 59)
				.MaxDefenseBonus(CombatDamageType.Ice, 30)
				.MaxSavingThrowBonus(SavingThrow.Will, 5)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(330)
				.FP(8)
				.STM(18)
				.DMG(24)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(42)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxDefenseBonus(CombatDamageType.Fire, 31)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 60)
				.MaxDefenseBonus(CombatDamageType.Ice, 31)
				.MaxSavingThrowBonus(SavingThrow.Will, 5)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 4)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

    }
}
