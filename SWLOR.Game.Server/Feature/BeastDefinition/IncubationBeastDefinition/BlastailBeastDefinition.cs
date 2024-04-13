using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class BlastailBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Blastail)
                .Name("Blastail")
                .Appearance(AppearanceType.CatCheetahProjectQ)
                .AppearanceScale(1f)
                .SoundSetId(19)
                .PortraitId(601)
                .CombatStats(AbilityType.Agility, AbilityType.Perception)
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
				.HP(108)
				.FP(5)
				.STM(5)
				.DMG(8)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
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
				.HP(116)
				.FP(5)
				.STM(5)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
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
				.HP(124)
				.FP(5)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
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
				.HP(132)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 14)
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
				.HP(140)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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
				.HP(148)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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
				.HP(156)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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
				.HP(164)
				.FP(6)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 22)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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
				.HP(172)
				.FP(7)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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
				.HP(180)
				.FP(7)
				.STM(9)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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
				.HP(188)
				.FP(7)
				.STM(9)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 15)
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
				.HP(196)
				.FP(7)
				.STM(9)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 15)
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
				.HP(204)
				.FP(7)
				.STM(10)
				.DMG(16)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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
				.HP(212)
				.FP(8)
				.STM(10)
				.DMG(16)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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
				.HP(220)
				.FP(8)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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
				.HP(228)
				.FP(8)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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
				.HP(236)
				.FP(8)
				.STM(11)
				.DMG(16)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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
				.HP(244)
				.FP(8)
				.STM(12)
				.DMG(16)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 16)
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
				.HP(252)
				.FP(9)
				.STM(12)
				.DMG(16)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 16)
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
				.HP(260)
				.FP(9)
				.STM(13)
				.DMG(29)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 16)
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
				.HP(268)
				.FP(9)
				.STM(13)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
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
				.HP(276)
				.FP(9)
				.STM(13)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
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
				.HP(284)
				.FP(9)
				.STM(14)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
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
				.HP(292)
				.FP(10)
				.STM(14)
				.DMG(29)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
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
				.HP(300)
				.FP(10)
				.STM(15)
				.DMG(29)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
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
				.HP(308)
				.FP(10)
				.STM(15)
				.DMG(29)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
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
				.HP(316)
				.FP(10)
				.STM(15)
				.DMG(29)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
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
				.HP(324)
				.FP(10)
				.STM(16)
				.DMG(29)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 17)
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
				.HP(332)
				.FP(11)
				.STM(16)
				.DMG(29)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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
				.HP(340)
				.FP(11)
				.STM(17)
				.DMG(37)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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
				.HP(348)
				.FP(11)
				.STM(17)
				.DMG(37)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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
				.HP(356)
				.FP(11)
				.STM(17)
				.DMG(37)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
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
				.HP(364)
				.FP(11)
				.STM(18)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
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
				.HP(372)
				.FP(12)
				.STM(18)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
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
				.HP(380)
				.FP(12)
				.STM(19)
				.DMG(37)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
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
				.HP(388)
				.FP(12)
				.STM(19)
				.DMG(37)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 18)
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
				.HP(396)
				.FP(12)
				.STM(19)
				.DMG(37)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
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
				.HP(404)
				.FP(12)
				.STM(20)
				.DMG(37)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
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
				.HP(412)
				.FP(13)
				.STM(20)
				.DMG(37)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
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
				.HP(420)
				.FP(13)
				.STM(21)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
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
				.HP(428)
				.FP(13)
				.STM(21)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
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
				.HP(436)
				.FP(13)
				.STM(21)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
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
				.HP(444)
				.FP(13)
				.STM(22)
				.DMG(43)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
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
				.HP(452)
				.FP(14)
				.STM(22)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 19)
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
				.HP(460)
				.FP(14)
				.STM(23)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
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
				.HP(468)
				.FP(14)
				.STM(23)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
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
				.HP(476)
				.FP(14)
				.STM(23)
				.DMG(43)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
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
				.HP(484)
				.FP(14)
				.STM(24)
				.DMG(43)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
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
				.HP(492)
				.FP(15)
				.STM(24)
				.DMG(43)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
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
				.HP(500)
				.FP(15)
				.STM(25)
				.DMG(43)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 20)
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
