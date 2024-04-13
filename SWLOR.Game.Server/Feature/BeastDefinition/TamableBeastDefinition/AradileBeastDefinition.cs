using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition
{
    public class AradileBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Aradile)
                .Name("Aradile")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .AppearanceScale(1f)
                .SoundSetId(442)
                .PortraitId(4279)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Bruiser)

                .CanMutateInto(BeastType.Bearbug)
				.MutationWeight(10)
				.MutationRequiresLyaseColor(EnzymeColorType.Blue, 1)

                .CanMutateInto(BeastType.Wraid)
				.MutationWeight(50)

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
				.HP(50)
				.FP(6)
				.STM(6)
				.DMG(8)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(0)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
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
				.HP(86)
				.FP(6)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
				.MaxDefenseBonus(CombatDamageType.Fire, 1)
				.MaxDefenseBonus(CombatDamageType.Poison, 0)
				.MaxDefenseBonus(CombatDamageType.Electrical, 0)
				.MaxDefenseBonus(CombatDamageType.Ice, 1)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(104)
				.FP(7)
				.STM(7)
				.DMG(8)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxDefenseBonus(CombatDamageType.Fire, 1)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 1)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(122)
				.FP(7)
				.STM(8)
				.DMG(8)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(140)
				.FP(7)
				.STM(9)
				.DMG(8)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(158)
				.FP(8)
				.STM(10)
				.DMG(8)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxDefenseBonus(CombatDamageType.Fire, 2)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 2)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(176)
				.FP(8)
				.STM(10)
				.DMG(8)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxDefenseBonus(CombatDamageType.Fire, 3)
				.MaxDefenseBonus(CombatDamageType.Poison, 1)
				.MaxDefenseBonus(CombatDamageType.Electrical, 1)
				.MaxDefenseBonus(CombatDamageType.Ice, 3)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(194)
				.FP(8)
				.STM(11)
				.DMG(8)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxDefenseBonus(CombatDamageType.Fire, 3)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 3)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(212)
				.FP(8)
				.STM(12)
				.DMG(8)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(230)
				.FP(9)
				.STM(12)
				.DMG(13)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(13)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(248)
				.FP(9)
				.STM(13)
				.DMG(13)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 4)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 4)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(266)
				.FP(9)
				.STM(14)
				.DMG(13)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxDefenseBonus(CombatDamageType.Fire, 5)
				.MaxDefenseBonus(CombatDamageType.Poison, 2)
				.MaxDefenseBonus(CombatDamageType.Electrical, 2)
				.MaxDefenseBonus(CombatDamageType.Ice, 5)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(284)
				.FP(10)
				.STM(14)
				.DMG(13)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxDefenseBonus(CombatDamageType.Fire, 5)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 5)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(302)
				.FP(10)
				.STM(15)
				.DMG(13)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(320)
				.FP(10)
				.STM(16)
				.DMG(13)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxDefenseBonus(CombatDamageType.Fire, 6)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 6)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(338)
				.FP(11)
				.STM(17)
				.DMG(13)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(13)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(356)
				.FP(11)
				.STM(17)
				.DMG(13)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 3)
				.MaxDefenseBonus(CombatDamageType.Electrical, 3)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(374)
				.FP(11)
				.STM(18)
				.DMG(13)
				.Stat(AbilityType.Might, 22)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxDefenseBonus(CombatDamageType.Fire, 7)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 7)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(392)
				.FP(11)
				.STM(19)
				.DMG(13)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 8)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 8)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(410)
				.FP(12)
				.STM(19)
				.DMG(18)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(25)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxDefenseBonus(CombatDamageType.Fire, 8)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 8)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(428)
				.FP(12)
				.STM(20)
				.DMG(18)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(446)
				.FP(12)
				.STM(21)
				.DMG(18)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 4)
				.MaxDefenseBonus(CombatDamageType.Electrical, 4)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(464)
				.FP(13)
				.STM(21)
				.DMG(18)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxDefenseBonus(CombatDamageType.Fire, 9)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 9)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(482)
				.FP(13)
				.STM(22)
				.DMG(18)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(30)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(500)
				.FP(13)
				.STM(23)
				.DMG(18)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(31)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxDefenseBonus(CombatDamageType.Fire, 10)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 10)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(518)
				.FP(14)
				.STM(24)
				.DMG(18)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(32)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 5)
				.MaxDefenseBonus(CombatDamageType.Electrical, 5)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(536)
				.FP(14)
				.STM(24)
				.DMG(18)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(554)
				.FP(14)
				.STM(25)
				.DMG(18)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxDefenseBonus(CombatDamageType.Fire, 11)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 11)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(572)
				.FP(14)
				.STM(26)
				.DMG(18)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(590)
				.FP(15)
				.STM(26)
				.DMG(22)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(37)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxDefenseBonus(CombatDamageType.Fire, 12)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 12)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(608)
				.FP(15)
				.STM(27)
				.DMG(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 6)
				.MaxDefenseBonus(CombatDamageType.Electrical, 6)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(626)
				.FP(15)
				.STM(28)
				.DMG(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(40)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(644)
				.FP(16)
				.STM(28)
				.DMG(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxDefenseBonus(CombatDamageType.Fire, 13)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 13)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(662)
				.FP(16)
				.STM(29)
				.DMG(22)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(42)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxDefenseBonus(CombatDamageType.Fire, 14)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 14)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(680)
				.FP(16)
				.STM(30)
				.DMG(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(43)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxDefenseBonus(CombatDamageType.Fire, 14)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 14)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(698)
				.FP(17)
				.STM(31)
				.DMG(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(44)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 7)
				.MaxDefenseBonus(CombatDamageType.Electrical, 7)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(716)
				.FP(17)
				.STM(31)
				.DMG(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(46)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 15)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 15)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(734)
				.FP(17)
				.STM(32)
				.DMG(22)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(47)
				.MaxAccuracyBonus(31)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 47)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(752)
				.FP(17)
				.STM(33)
				.DMG(22)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(48)
				.MaxAccuracyBonus(32)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 48)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(770)
				.FP(18)
				.STM(33)
				.DMG(27)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(49)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 49)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxDefenseBonus(CombatDamageType.Fire, 16)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 16)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(788)
				.FP(18)
				.STM(34)
				.DMG(27)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(50)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 50)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 8)
				.MaxDefenseBonus(CombatDamageType.Electrical, 8)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(806)
				.FP(18)
				.STM(35)
				.DMG(27)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(52)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 52)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 17)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 17)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(824)
				.FP(19)
				.STM(35)
				.DMG(27)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(53)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 53)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(842)
				.FP(19)
				.STM(36)
				.DMG(27)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(54)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 54)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(860)
				.FP(19)
				.STM(37)
				.DMG(27)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(55)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 55)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxDefenseBonus(CombatDamageType.Fire, 18)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 18)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(878)
				.FP(20)
				.STM(38)
				.DMG(27)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(56)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 56)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxDefenseBonus(CombatDamageType.Fire, 19)
				.MaxDefenseBonus(CombatDamageType.Poison, 9)
				.MaxDefenseBonus(CombatDamageType.Electrical, 9)
				.MaxDefenseBonus(CombatDamageType.Ice, 19)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(896)
				.FP(20)
				.STM(38)
				.DMG(27)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(58)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 58)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 19)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 19)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(914)
				.FP(20)
				.STM(39)
				.DMG(27)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(59)
				.MaxAccuracyBonus(39)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 59)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(932)
				.FP(20)
				.STM(40)
				.DMG(27)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(60)
				.MaxAccuracyBonus(40)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(950)
				.FP(21)
				.STM(40)
				.DMG(27)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(60)
				.MaxAccuracyBonus(40)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxDefenseBonus(CombatDamageType.Fire, 20)
				.MaxDefenseBonus(CombatDamageType.Poison, 10)
				.MaxDefenseBonus(CombatDamageType.Electrical, 10)
				.MaxDefenseBonus(CombatDamageType.Ice, 20)
				.MaxSavingThrowBonus(SavingThrow.Will, 0)
				.MaxSavingThrowBonus(SavingThrow.Fortitude, 0)
				.MaxSavingThrowBonus(SavingThrow.Reflex, 0);
		}

    }
}
