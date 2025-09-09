using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class OrbakWaterHorseBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.OrbakWaterHorse)
                .Name("Orbak Water Horse")
                .Appearance(AppearanceType.HorseEachuisgeCCCphenixrising)
                .AppearanceScale(1f)
                .SoundSetId(259)
                .PortraitId(1277)
                .CombatStats(AbilityType.Might, AbilityType.Agility)
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
				.HP(107)
				.FP(5)
				.STM(5)
				.DMG(8)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(0)
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
				.HP(114)
				.FP(5)
				.STM(5)
				.DMG(8)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 0)
				.MaxDefenseBonus(CombatDamageType.Electrical, 0)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(121)
				.FP(5)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 0)
				.MaxDefenseBonus(CombatDamageType.Electrical, 0)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(128)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxDefenseBonus(CombatDamageType.Fire, 3)
				.MaxDefenseBonus(CombatDamageType.Poison, 0)
				.MaxDefenseBonus(CombatDamageType.Electrical, 0)
				.MaxDefenseBonus(CombatDamageType.Ice, 3)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(135)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(142)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxDefenseBonus(CombatDamageType.Fire, 5)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 5)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(149)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(156)
				.FP(6)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(163)
				.FP(7)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(170)
				.FP(7)
				.STM(9)
				.DMG(16)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(13)
				.MaxAccuracyBonus(13)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 8)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 8)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(177)
				.FP(7)
				.STM(9)
				.DMG(16)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(184)
				.FP(7)
				.STM(9)
				.DMG(16)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(191)
				.FP(7)
				.STM(10)
				.DMG(16)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(198)
				.FP(8)
				.STM(10)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(205)
				.FP(8)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(212)
				.FP(8)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(219)
				.FP(8)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxDefenseBonus(CombatDamageType.Fire, 14)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 14)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(226)
				.FP(8)
				.STM(12)
				.DMG(16)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(233)
				.FP(9)
				.STM(12)
				.DMG(16)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(240)
				.FP(9)
				.STM(13)
				.DMG(29)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(25)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(247)
				.FP(9)
				.STM(13)
				.DMG(29)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(254)
				.FP(9)
				.STM(13)
				.DMG(29)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(261)
				.FP(9)
				.STM(14)
				.DMG(29)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxDefenseBonus(CombatDamageType.Fire, 19)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 19)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(268)
				.FP(10)
				.STM(14)
				.DMG(29)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(30)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(275)
				.FP(10)
				.STM(15)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(31)
				.MaxAccuracyBonus(31)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(282)
				.FP(10)
				.STM(15)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(32)
				.MaxAccuracyBonus(32)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxDefenseBonus(CombatDamageType.Fire, 21)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 21)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(289)
				.FP(10)
				.STM(15)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 22)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 22)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(296)
				.FP(10)
				.STM(16)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 23)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 23)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(303)
				.FP(11)
				.STM(16)
				.DMG(29)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(310)
				.FP(11)
				.STM(17)
				.DMG(37)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(37)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(317)
				.FP(11)
				.STM(17)
				.DMG(37)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxDefenseBonus(CombatDamageType.Fire, 25)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 25)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(324)
				.FP(11)
				.STM(17)
				.DMG(37)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(40)
				.MaxAccuracyBonus(40)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 26)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 26)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(331)
				.FP(11)
				.STM(18)
				.DMG(37)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(41)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 27)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 27)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(338)
				.FP(12)
				.STM(18)
				.DMG(37)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(42)
				.MaxAccuracyBonus(42)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxDefenseBonus(CombatDamageType.Fire, 28)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 28)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(345)
				.FP(12)
				.STM(19)
				.DMG(37)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(43)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(352)
				.FP(12)
				.STM(19)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(44)
				.MaxAccuracyBonus(44)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(359)
				.FP(12)
				.STM(19)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(46)
				.MaxAccuracyBonus(46)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 30)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 30)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(366)
				.FP(12)
				.STM(20)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(47)
				.MaxAccuracyBonus(47)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 31)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 31)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(373)
				.FP(13)
				.STM(20)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(48)
				.MaxAccuracyBonus(48)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 32)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 32)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(380)
				.FP(13)
				.STM(21)
				.DMG(43)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(49)
				.MaxAccuracyBonus(49)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 33)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 33)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(387)
				.FP(13)
				.STM(21)
				.DMG(43)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(50)
				.MaxAccuracyBonus(50)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxDefenseBonus(CombatDamageType.Fire, 33)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 33)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(394)
				.FP(13)
				.STM(21)
				.DMG(43)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(52)
				.MaxAccuracyBonus(52)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 34)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 34)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(401)
				.FP(13)
				.STM(22)
				.DMG(43)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(53)
				.MaxAccuracyBonus(53)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 35)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 35)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(408)
				.FP(14)
				.STM(22)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(54)
				.MaxAccuracyBonus(54)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxDefenseBonus(CombatDamageType.Fire, 36)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 36)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 4);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(415)
				.FP(14)
				.STM(23)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 31)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(55)
				.MaxAccuracyBonus(55)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxDefenseBonus(CombatDamageType.Fire, 37)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 37)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 5);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(422)
				.FP(14)
				.STM(23)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 31)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(56)
				.MaxAccuracyBonus(56)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxDefenseBonus(CombatDamageType.Fire, 38)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 38)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 5);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(429)
				.FP(14)
				.STM(23)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 31)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(58)
				.MaxAccuracyBonus(58)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 38)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 38)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 5);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(436)
				.FP(14)
				.STM(24)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 31)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(59)
				.MaxAccuracyBonus(59)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 39)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 39)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 5);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(443)
				.FP(15)
				.STM(24)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 31)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(60)
				.MaxAccuracyBonus(60)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxDefenseBonus(CombatDamageType.Fire, 40)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 40)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 5);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(450)
				.FP(15)
				.STM(25)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 31)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(61)
				.MaxAccuracyBonus(61)
				.MaxEvasionBonus(31)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxDefenseBonus(CombatDamageType.Fire, 41)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 41)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 5);
		}

    }
}
