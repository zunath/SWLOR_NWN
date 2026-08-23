using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class OchreMawBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.OchreMaw)
                .Name("Ochre Maw")
                .Appearance(AppearanceType.OozeOchreJellyLarge)
                .AppearanceScale(1f)
                .SoundSetId(109)
                .PortraitId(735)
                .CombatStats(AbilityType.Perception, AbilityType.Might)
                .Role(BeastRoleType.Bruiser)


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
				.HP(62)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(0)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
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
				.HP(80)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
				.MaxResistanceBonus(ResistanceType.Fire, 1)
				.MaxResistanceBonus(ResistanceType.Poison, 0)
				.MaxResistanceBonus(ResistanceType.Electrical, 0)
				.MaxResistanceBonus(ResistanceType.Ice, 1)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(91)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 1)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 1)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(102)
				.FP(5)
				.STM(6)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(113)
				.FP(5)
				.STM(7)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(124)
				.FP(6)
				.STM(8)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(135)
				.FP(6)
				.STM(8)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(146)
				.FP(6)
				.STM(8)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(157)
				.FP(6)
				.STM(9)
				.DMG(6)
				.Delay(24)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(169)
				.FP(7)
				.STM(9)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(10)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(180)
				.FP(7)
				.STM(10)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(191)
				.FP(7)
				.STM(11)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(202)
				.FP(8)
				.STM(11)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(13)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(213)
				.FP(8)
				.STM(11)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(224)
				.FP(8)
				.STM(12)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(235)
				.FP(8)
				.STM(13)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(246)
				.FP(8)
				.STM(13)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(257)
				.FP(8)
				.STM(14)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(268)
				.FP(8)
				.STM(14)
				.DMG(10)
				.Delay(24)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(280)
				.FP(9)
				.STM(14)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(291)
				.FP(9)
				.STM(15)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(13)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(302)
				.FP(9)
				.STM(16)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(313)
				.FP(10)
				.STM(16)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(22)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 22)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(324)
				.FP(10)
				.STM(17)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(335)
				.FP(10)
				.STM(17)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(346)
				.FP(11)
				.STM(18)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(357)
				.FP(11)
				.STM(18)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(368)
				.FP(11)
				.STM(19)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(379)
				.FP(11)
				.STM(20)
				.DMG(14)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(391)
				.FP(11)
				.STM(20)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(28)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 28)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(402)
				.FP(11)
				.STM(20)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(413)
				.FP(11)
				.STM(21)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(30)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(424)
				.FP(12)
				.STM(21)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(31)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(435)
				.FP(12)
				.STM(22)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(32)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(446)
				.FP(12)
				.STM(23)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(32)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(457)
				.FP(13)
				.STM(23)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(468)
				.FP(13)
				.STM(23)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(479)
				.FP(13)
				.STM(24)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(490)
				.FP(13)
				.STM(25)
				.DMG(17)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(502)
				.FP(14)
				.STM(25)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(37)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(513)
				.FP(14)
				.STM(26)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(524)
				.FP(14)
				.STM(26)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(39)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 39)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(535)
				.FP(14)
				.STM(26)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(40)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(546)
				.FP(14)
				.STM(27)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(557)
				.FP(14)
				.STM(28)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(568)
				.FP(15)
				.STM(29)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(42)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(579)
				.FP(15)
				.STM(29)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(44)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(590)
				.FP(15)
				.STM(29)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(44)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(601)
				.FP(15)
				.STM(30)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(45)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 45)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(613)
				.FP(16)
				.STM(30)
				.DMG(20)
				.Delay(24)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(46)
				.MaxAccuracyBonus(31)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

    }
}
