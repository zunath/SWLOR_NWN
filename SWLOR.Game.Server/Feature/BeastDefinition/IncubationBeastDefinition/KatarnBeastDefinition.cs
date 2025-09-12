using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class KatarnBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Katarn)
                .Name("Katarn")
                .Appearance(AppearanceType.SWLORKatarn)
                .AppearanceScale(1f)
                .SoundSetId(60)
                .PortraitId(601)
                .CombatStats(AbilityType.Might, AbilityType.Agility)
                .Role(BeastRoleType.Balanced)

                
                
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
				.HP(118)
				.FP(5)
				.STM(5)
				.DMG(8)
				.Stat(AbilityType.Might, 31)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
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
				.HP(136)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxDefenseBonus(CombatDamageType.Fire, 1)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 1)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(154)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
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
				.HP(172)
				.FP(7)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(3)
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

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(190)
				.FP(7)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxDefenseBonus(CombatDamageType.Fire, 3)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 3)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(208)
				.FP(8)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(226)
				.FP(8)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(244)
				.FP(9)
				.STM(9)
				.DMG(8)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 5)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 5)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(262)
				.FP(9)
				.STM(9)
				.DMG(8)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(280)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(298)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(316)
				.FP(11)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(10)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(334)
				.FP(11)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxDefenseBonus(CombatDamageType.Fire, 8)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 8)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(352)
				.FP(12)
				.STM(12)
				.DMG(16)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(370)
				.FP(12)
				.STM(12)
				.DMG(16)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(388)
				.FP(13)
				.STM(13)
				.DMG(16)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(13)
				.MaxAccuracyBonus(13)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(406)
				.FP(13)
				.STM(13)
				.DMG(16)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(424)
				.FP(14)
				.STM(14)
				.DMG(16)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 11)
				.MaxDefenseBonus(CombatDamageType.Electrical, 11)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(442)
				.FP(14)
				.STM(14)
				.DMG(16)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 12)
				.MaxDefenseBonus(CombatDamageType.Electrical, 12)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(460)
				.FP(15)
				.STM(15)
				.DMG(25)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 12)
				.MaxDefenseBonus(CombatDamageType.Electrical, 12)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(478)
				.FP(15)
				.STM(15)
				.DMG(25)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 13)
				.MaxDefenseBonus(CombatDamageType.Electrical, 13)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(496)
				.FP(16)
				.STM(16)
				.DMG(25)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 13)
				.MaxDefenseBonus(CombatDamageType.Electrical, 13)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(514)
				.FP(16)
				.STM(16)
				.DMG(25)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxDefenseBonus(CombatDamageType.Fire, 14)
				.MaxDefenseBonus(CombatDamageType.Poison, 14)
				.MaxDefenseBonus(CombatDamageType.Electrical, 14)
				.MaxDefenseBonus(CombatDamageType.Ice, 14)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(532)
				.FP(17)
				.STM(17)
				.DMG(25)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 15)
				.MaxDefenseBonus(CombatDamageType.Electrical, 15)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 1)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 1)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 1);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(550)
				.FP(17)
				.STM(17)
				.DMG(25)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 15)
				.MaxDefenseBonus(CombatDamageType.Electrical, 15)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(568)
				.FP(18)
				.STM(18)
				.DMG(25)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 16)
				.MaxDefenseBonus(CombatDamageType.Electrical, 16)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(586)
				.FP(18)
				.STM(18)
				.DMG(25)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(22)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 22)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 17)
				.MaxDefenseBonus(CombatDamageType.Electrical, 17)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(604)
				.FP(19)
				.STM(19)
				.DMG(25)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 17)
				.MaxDefenseBonus(CombatDamageType.Electrical, 17)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(622)
				.FP(19)
				.STM(19)
				.DMG(25)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 18)
				.MaxDefenseBonus(CombatDamageType.Electrical, 18)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(640)
				.FP(20)
				.STM(20)
				.DMG(33)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 18)
				.MaxDefenseBonus(CombatDamageType.Electrical, 18)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(658)
				.FP(20)
				.STM(20)
				.DMG(33)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(25)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxDefenseBonus(CombatDamageType.Fire, 19)
				.MaxDefenseBonus(CombatDamageType.Poison, 19)
				.MaxDefenseBonus(CombatDamageType.Electrical, 19)
				.MaxDefenseBonus(CombatDamageType.Ice, 19)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(676)
				.FP(21)
				.STM(21)
				.DMG(33)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 20)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(694)
				.FP(21)
				.STM(21)
				.DMG(33)
				.Stat(AbilityType.Might, 40)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 20)
				.MaxDefenseBonus(CombatDamageType.Electrical, 20)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(712)
				.FP(22)
				.STM(22)
				.DMG(33)
				.Stat(AbilityType.Might, 40)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(28)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 28)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxDefenseBonus(CombatDamageType.Fire, 21)
				.MaxDefenseBonus(CombatDamageType.Poison, 21)
				.MaxDefenseBonus(CombatDamageType.Electrical, 21)
				.MaxDefenseBonus(CombatDamageType.Ice, 21)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(730)
				.FP(22)
				.STM(22)
				.DMG(33)
				.Stat(AbilityType.Might, 40)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 21)
				.MaxDefenseBonus(CombatDamageType.Poison, 21)
				.MaxDefenseBonus(CombatDamageType.Electrical, 21)
				.MaxDefenseBonus(CombatDamageType.Ice, 21)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(748)
				.FP(23)
				.STM(23)
				.DMG(33)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 22)
				.MaxDefenseBonus(CombatDamageType.Poison, 22)
				.MaxDefenseBonus(CombatDamageType.Electrical, 22)
				.MaxDefenseBonus(CombatDamageType.Ice, 22)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(766)
				.FP(23)
				.STM(23)
				.DMG(33)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(30)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxDefenseBonus(CombatDamageType.Fire, 23)
				.MaxDefenseBonus(CombatDamageType.Poison, 23)
				.MaxDefenseBonus(CombatDamageType.Electrical, 23)
				.MaxDefenseBonus(CombatDamageType.Ice, 23)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(784)
				.FP(24)
				.STM(24)
				.DMG(33)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 22)
				.MaxAttackBonus(31)
				.MaxAccuracyBonus(31)
				.MaxEvasionBonus(31)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxDefenseBonus(CombatDamageType.Fire, 23)
				.MaxDefenseBonus(CombatDamageType.Poison, 23)
				.MaxDefenseBonus(CombatDamageType.Electrical, 23)
				.MaxDefenseBonus(CombatDamageType.Ice, 23)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(802)
				.FP(24)
				.STM(24)
				.DMG(33)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(32)
				.MaxAccuracyBonus(32)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 24)
				.MaxDefenseBonus(CombatDamageType.Electrical, 24)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(820)
				.FP(25)
				.STM(25)
				.DMG(43)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxDefenseBonus(CombatDamageType.Fire, 24)
				.MaxDefenseBonus(CombatDamageType.Poison, 24)
				.MaxDefenseBonus(CombatDamageType.Electrical, 24)
				.MaxDefenseBonus(CombatDamageType.Ice, 24)
				.MaxSavingThrowBonus(SavingThrow.Will, 2)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 2)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 2);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(838)
				.FP(25)
				.STM(25)
				.DMG(43)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxDefenseBonus(CombatDamageType.Fire, 25)
				.MaxDefenseBonus(CombatDamageType.Poison, 25)
				.MaxDefenseBonus(CombatDamageType.Electrical, 25)
				.MaxDefenseBonus(CombatDamageType.Ice, 25)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(856)
				.FP(26)
				.STM(26)
				.DMG(43)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxDefenseBonus(CombatDamageType.Fire, 26)
				.MaxDefenseBonus(CombatDamageType.Poison, 26)
				.MaxDefenseBonus(CombatDamageType.Electrical, 26)
				.MaxDefenseBonus(CombatDamageType.Ice, 26)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(874)
				.FP(26)
				.STM(26)
				.DMG(43)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxDefenseBonus(CombatDamageType.Fire, 26)
				.MaxDefenseBonus(CombatDamageType.Poison, 26)
				.MaxDefenseBonus(CombatDamageType.Electrical, 26)
				.MaxDefenseBonus(CombatDamageType.Ice, 26)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(892)
				.FP(27)
				.STM(27)
				.DMG(43)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 21)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxDefenseBonus(CombatDamageType.Fire, 27)
				.MaxDefenseBonus(CombatDamageType.Poison, 27)
				.MaxDefenseBonus(CombatDamageType.Electrical, 27)
				.MaxDefenseBonus(CombatDamageType.Ice, 27)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(910)
				.FP(27)
				.STM(27)
				.DMG(43)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(37)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxDefenseBonus(CombatDamageType.Fire, 28)
				.MaxDefenseBonus(CombatDamageType.Poison, 28)
				.MaxDefenseBonus(CombatDamageType.Electrical, 28)
				.MaxDefenseBonus(CombatDamageType.Ice, 28)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(928)
				.FP(28)
				.STM(28)
				.DMG(43)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 31)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxDefenseBonus(CombatDamageType.Fire, 28)
				.MaxDefenseBonus(CombatDamageType.Poison, 28)
				.MaxDefenseBonus(CombatDamageType.Electrical, 28)
				.MaxDefenseBonus(CombatDamageType.Ice, 28)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(946)
				.FP(28)
				.STM(28)
				.DMG(43)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 29)
				.MaxDefenseBonus(CombatDamageType.Electrical, 29)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(964)
				.FP(29)
				.STM(29)
				.DMG(43)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(39)
				.MaxAccuracyBonus(39)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 39)
				.MaxDefenseBonus(CombatDamageType.Force, 39)
				.MaxDefenseBonus(CombatDamageType.Fire, 29)
				.MaxDefenseBonus(CombatDamageType.Poison, 29)
				.MaxDefenseBonus(CombatDamageType.Electrical, 29)
				.MaxDefenseBonus(CombatDamageType.Ice, 29)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(982)
				.FP(29)
				.STM(29)
				.DMG(43)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(40)
				.MaxAccuracyBonus(40)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxDefenseBonus(CombatDamageType.Fire, 30)
				.MaxDefenseBonus(CombatDamageType.Poison, 30)
				.MaxDefenseBonus(CombatDamageType.Electrical, 30)
				.MaxDefenseBonus(CombatDamageType.Ice, 30)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(1000)
				.FP(30)
				.STM(30)
				.DMG(43)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 22)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(41)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxDefenseBonus(CombatDamageType.Fire, 31)
				.MaxDefenseBonus(CombatDamageType.Poison, 31)
				.MaxDefenseBonus(CombatDamageType.Electrical, 31)
				.MaxDefenseBonus(CombatDamageType.Ice, 31)
				.MaxSavingThrowBonus(SavingThrow.Will, 3)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 3)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 3);
		}

    }
}
