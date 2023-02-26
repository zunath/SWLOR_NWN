using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Feature.BeastDefinition
{
    public class SsurianBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Ssurian)
                .Appearance(AppearanceType.CrocAztec)
                .SoundSetId(250)
                .PortraitId(315)
                .CombatStats(AbilityType.Vitality, AbilityType.Perception)
                .Role(BeastRoleType.Tank);

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
				.HP(70)
				.FP(4)
				.STM(4)
				.DMG(6)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(0)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
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
				.HP(120)
				.FP(4)
				.STM(4)
				.DMG(6)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(145)
				.FP(4)
				.STM(4)
				.DMG(6)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(170)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 3)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 3)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(195)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(220)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxDefenseBonus(CombatDamageType.Fire, 5)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 5)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(245)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(270)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(295)
				.FP(6)
				.STM(6)
				.DMG(6)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(320)
				.FP(6)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 8)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 8)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(345)
				.FP(6)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(370)
				.FP(6)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 4)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(395)
				.FP(6)
				.STM(6)
				.DMG(10)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 11)
				.MaxDefenseBonus(CombatDamageType.Electrical, 11)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(420)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 11)
				.MaxDefenseBonus(CombatDamageType.Electrical, 11)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(445)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 12)
				.MaxDefenseBonus(CombatDamageType.Electrical, 12)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(470)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 13)
				.MaxDefenseBonus(CombatDamageType.Electrical, 13)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(495)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxDefenseBonus(CombatDamageType.Fire, 14)
				.MaxDefenseBonus(CombatDamageType.Poison, 14)
				.MaxDefenseBonus(CombatDamageType.Electrical, 14)
				.MaxDefenseBonus(CombatDamageType.Ice, 14)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(520)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 15)
				.MaxDefenseBonus(CombatDamageType.Electrical, 15)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(545)
				.FP(8)
				.STM(8)
				.DMG(10)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 16)
				.MaxDefenseBonus(CombatDamageType.Electrical, 16)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(570)
				.FP(8)
				.STM(8)
				.DMG(15)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 16)
				.MaxDefenseBonus(CombatDamageType.Electrical, 16)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(595)
				.FP(8)
				.STM(8)
				.DMG(15)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 17)
				.MaxDefenseBonus(CombatDamageType.Electrical, 17)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(620)
				.FP(8)
				.STM(8)
				.DMG(15)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 18)
				.MaxDefenseBonus(CombatDamageType.Electrical, 18)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(645)
				.FP(8)
				.STM(8)
				.DMG(15)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 19)
				.MaxDefenseBonus(CombatDamageType.Poison, 19)
				.MaxDefenseBonus(CombatDamageType.Electrical, 19)
				.MaxDefenseBonus(CombatDamageType.Ice, 19)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(670)
				.FP(9)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 20)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(695)
				.FP(9)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 20)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(720)
				.FP(9)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxDefenseBonus(CombatDamageType.Fire, 21)
				.MaxDefenseBonus(CombatDamageType.Poison, 21)
				.MaxDefenseBonus(CombatDamageType.Electrical, 21)
				.MaxDefenseBonus(CombatDamageType.Ice, 21)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(745)
				.FP(9)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxDefenseBonus(CombatDamageType.Fire, 22)
				.MaxDefenseBonus(CombatDamageType.Poison, 22)
				.MaxDefenseBonus(CombatDamageType.Electrical, 22)
				.MaxDefenseBonus(CombatDamageType.Ice, 22)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(770)
				.FP(9)
				.STM(9)
				.DMG(15)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxDefenseBonus(CombatDamageType.Fire, 23)
				.MaxDefenseBonus(CombatDamageType.Poison, 23)
				.MaxDefenseBonus(CombatDamageType.Electrical, 23)
				.MaxDefenseBonus(CombatDamageType.Ice, 23)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(795)
				.FP(10)
				.STM(10)
				.DMG(15)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 24)
				.MaxDefenseBonus(CombatDamageType.Electrical, 24)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(820)
				.FP(10)
				.STM(10)
				.DMG(19)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 24)
				.MaxDefenseBonus(CombatDamageType.Electrical, 24)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(845)
				.FP(10)
				.STM(10)
				.DMG(19)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxDefenseBonus(CombatDamageType.Fire, 25)
				.MaxDefenseBonus(CombatDamageType.Poison, 25)
				.MaxDefenseBonus(CombatDamageType.Electrical, 25)
				.MaxDefenseBonus(CombatDamageType.Ice, 25)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(870)
				.FP(10)
				.STM(10)
				.DMG(19)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxDefenseBonus(CombatDamageType.Fire, 26)
				.MaxDefenseBonus(CombatDamageType.Poison, 26)
				.MaxDefenseBonus(CombatDamageType.Electrical, 26)
				.MaxDefenseBonus(CombatDamageType.Ice, 26)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(895)
				.FP(10)
				.STM(10)
				.DMG(19)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxDefenseBonus(CombatDamageType.Fire, 27)
				.MaxDefenseBonus(CombatDamageType.Poison, 27)
				.MaxDefenseBonus(CombatDamageType.Electrical, 27)
				.MaxDefenseBonus(CombatDamageType.Ice, 27)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(920)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxDefenseBonus(CombatDamageType.Fire, 28)
				.MaxDefenseBonus(CombatDamageType.Poison, 28)
				.MaxDefenseBonus(CombatDamageType.Electrical, 28)
				.MaxDefenseBonus(CombatDamageType.Ice, 28)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(945)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 29)
				.MaxDefenseBonus(CombatDamageType.Electrical, 29)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(970)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 44)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 29)
				.MaxDefenseBonus(CombatDamageType.Electrical, 29)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(995)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxDefenseBonus(CombatDamageType.Fire, 30)
				.MaxDefenseBonus(CombatDamageType.Poison, 30)
				.MaxDefenseBonus(CombatDamageType.Electrical, 30)
				.MaxDefenseBonus(CombatDamageType.Ice, 30)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(1020)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 47)
				.MaxDefenseBonus(CombatDamageType.Force, 47)
				.MaxDefenseBonus(CombatDamageType.Fire, 31)
				.MaxDefenseBonus(CombatDamageType.Poison, 31)
				.MaxDefenseBonus(CombatDamageType.Electrical, 31)
				.MaxDefenseBonus(CombatDamageType.Ice, 31)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(1045)
				.FP(12)
				.STM(12)
				.DMG(19)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 48)
				.MaxDefenseBonus(CombatDamageType.Force, 48)
				.MaxDefenseBonus(CombatDamageType.Fire, 32)
				.MaxDefenseBonus(CombatDamageType.Poison, 32)
				.MaxDefenseBonus(CombatDamageType.Electrical, 32)
				.MaxDefenseBonus(CombatDamageType.Ice, 32)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(1070)
				.FP(12)
				.STM(12)
				.DMG(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 49)
				.MaxDefenseBonus(CombatDamageType.Force, 49)
				.MaxDefenseBonus(CombatDamageType.Fire, 33)
				.MaxDefenseBonus(CombatDamageType.Poison, 33)
				.MaxDefenseBonus(CombatDamageType.Electrical, 33)
				.MaxDefenseBonus(CombatDamageType.Ice, 33)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(1095)
				.FP(12)
				.STM(12)
				.DMG(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 50)
				.MaxDefenseBonus(CombatDamageType.Force, 50)
				.MaxDefenseBonus(CombatDamageType.Fire, 33)
				.MaxDefenseBonus(CombatDamageType.Poison, 33)
				.MaxDefenseBonus(CombatDamageType.Electrical, 33)
				.MaxDefenseBonus(CombatDamageType.Ice, 33)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(1120)
				.FP(12)
				.STM(12)
				.DMG(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 52)
				.MaxDefenseBonus(CombatDamageType.Force, 52)
				.MaxDefenseBonus(CombatDamageType.Fire, 34)
				.MaxDefenseBonus(CombatDamageType.Poison, 34)
				.MaxDefenseBonus(CombatDamageType.Electrical, 34)
				.MaxDefenseBonus(CombatDamageType.Ice, 34)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(1145)
				.FP(12)
				.STM(12)
				.DMG(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 53)
				.MaxDefenseBonus(CombatDamageType.Force, 53)
				.MaxDefenseBonus(CombatDamageType.Fire, 35)
				.MaxDefenseBonus(CombatDamageType.Poison, 35)
				.MaxDefenseBonus(CombatDamageType.Electrical, 35)
				.MaxDefenseBonus(CombatDamageType.Ice, 35)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(1170)
				.FP(13)
				.STM(13)
				.DMG(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 54)
				.MaxDefenseBonus(CombatDamageType.Force, 54)
				.MaxDefenseBonus(CombatDamageType.Fire, 36)
				.MaxDefenseBonus(CombatDamageType.Poison, 36)
				.MaxDefenseBonus(CombatDamageType.Electrical, 36)
				.MaxDefenseBonus(CombatDamageType.Ice, 36)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(1195)
				.FP(13)
				.STM(13)
				.DMG(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 55)
				.MaxDefenseBonus(CombatDamageType.Force, 55)
				.MaxDefenseBonus(CombatDamageType.Fire, 37)
				.MaxDefenseBonus(CombatDamageType.Poison, 37)
				.MaxDefenseBonus(CombatDamageType.Electrical, 37)
				.MaxDefenseBonus(CombatDamageType.Ice, 37)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(1220)
				.FP(13)
				.STM(13)
				.DMG(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 56)
				.MaxDefenseBonus(CombatDamageType.Force, 56)
				.MaxDefenseBonus(CombatDamageType.Fire, 38)
				.MaxDefenseBonus(CombatDamageType.Poison, 38)
				.MaxDefenseBonus(CombatDamageType.Electrical, 38)
				.MaxDefenseBonus(CombatDamageType.Ice, 38)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(1245)
				.FP(13)
				.STM(13)
				.DMG(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 58)
				.MaxDefenseBonus(CombatDamageType.Force, 58)
				.MaxDefenseBonus(CombatDamageType.Fire, 38)
				.MaxDefenseBonus(CombatDamageType.Poison, 38)
				.MaxDefenseBonus(CombatDamageType.Electrical, 38)
				.MaxDefenseBonus(CombatDamageType.Ice, 38)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(1270)
				.FP(13)
				.STM(13)
				.DMG(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 59)
				.MaxDefenseBonus(CombatDamageType.Force, 59)
				.MaxDefenseBonus(CombatDamageType.Fire, 39)
				.MaxDefenseBonus(CombatDamageType.Poison, 39)
				.MaxDefenseBonus(CombatDamageType.Electrical, 39)
				.MaxDefenseBonus(CombatDamageType.Ice, 39)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(1295)
				.FP(14)
				.STM(14)
				.DMG(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxDefenseBonus(CombatDamageType.Fire, 40)
				.MaxDefenseBonus(CombatDamageType.Poison, 40)
				.MaxDefenseBonus(CombatDamageType.Electrical, 40)
				.MaxDefenseBonus(CombatDamageType.Ice, 40)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(1330)
				.FP(14)
				.STM(14)
				.DMG(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 23)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxDefenseBonus(CombatDamageType.Fire, 40)
				.MaxDefenseBonus(CombatDamageType.Poison, 40)
				.MaxDefenseBonus(CombatDamageType.Electrical, 40)
				.MaxDefenseBonus(CombatDamageType.Ice, 40)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

    }
}
