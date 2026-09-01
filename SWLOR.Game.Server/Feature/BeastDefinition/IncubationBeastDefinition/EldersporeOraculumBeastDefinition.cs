using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class EldersporeOraculumBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.EldersporeOraculum)
                .Name("Elderspore Oraculum")
                .Appearance(AppearanceType.MyconidElderSchazzwozzer)
                .AppearanceScale(0.8f)
                .SoundSetId(853)
                .PortraitId(2045)
                .CombatStats(AbilityType.Social, AbilityType.Willpower)
                .Role(BeastRoleType.Force)


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
				.HP(23)
				.FP(2)
				.STM(2)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(0)
				.MaxDefenseBonus(CombatDamageType.Physical, 0)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
				.MaxResistanceBonus(ResistanceType.Fire, 0)
				.MaxResistanceBonus(ResistanceType.Poison, 0)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 0)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level2()
		{
			_builder
				.AddLevel()
				.HP(41)
				.FP(3)
				.STM(2)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 0)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxResistanceBonus(ResistanceType.Fire, 1)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 1)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(50)
				.FP(3)
				.STM(2)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(59)
				.FP(5)
				.STM(2)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 7)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(68)
				.FP(6)
				.STM(2)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(77)
				.FP(7)
				.STM(3)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(86)
				.FP(7)
				.STM(3)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(95)
				.FP(8)
				.STM(3)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(104)
				.FP(9)
				.STM(3)
				.DMG(7)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(113)
				.FP(9)
				.STM(3)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(122)
				.FP(10)
				.STM(3)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(131)
				.FP(11)
				.STM(3)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(140)
				.FP(11)
				.STM(3)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(149)
				.FP(12)
				.STM(3)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(158)
				.FP(14)
				.STM(3)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(167)
				.FP(15)
				.STM(5)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(176)
				.FP(15)
				.STM(5)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 24)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(185)
				.FP(16)
				.STM(5)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(194)
				.FP(17)
				.STM(5)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(203)
				.FP(17)
				.STM(5)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 28)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(212)
				.FP(18)
				.STM(5)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(221)
				.FP(19)
				.STM(5)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 30)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(230)
				.FP(19)
				.STM(5)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(239)
				.FP(20)
				.STM(5)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 34)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(248)
				.FP(21)
				.STM(5)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 35)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(257)
				.FP(23)
				.STM(6)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 36)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(266)
				.FP(23)
				.STM(6)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 38)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(275)
				.FP(24)
				.STM(6)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 39)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 39)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(284)
				.FP(25)
				.STM(6)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 41)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(293)
				.FP(25)
				.STM(6)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 42)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(302)
				.FP(26)
				.STM(6)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 43)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(311)
				.FP(27)
				.STM(6)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 45)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 45)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(320)
				.FP(27)
				.STM(6)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 46)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(329)
				.FP(28)
				.STM(6)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 47)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 47)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(338)
				.FP(29)
				.STM(6)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 48)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 48)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(347)
				.FP(30)
				.STM(7)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 50)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 50)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(356)
				.FP(30)
				.STM(7)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 52)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 52)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(365)
				.FP(32)
				.STM(7)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 53)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 53)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(374)
				.FP(33)
				.STM(7)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 54)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 54)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(383)
				.FP(33)
				.STM(7)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 55)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 55)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(392)
				.FP(34)
				.STM(7)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 56)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 56)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(401)
				.FP(35)
				.STM(7)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 59)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 59)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(410)
				.FP(35)
				.STM(7)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 60)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(419)
				.FP(36)
				.STM(7)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 61)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 61)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(428)
				.FP(37)
				.STM(7)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(42)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 62)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 62)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 6)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(437)
				.FP(38)
				.STM(8)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(43)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 63)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 63)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 6)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(446)
				.FP(38)
				.STM(8)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 24)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(43)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 65)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 65)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 6)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(455)
				.FP(39)
				.STM(8)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(44)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 66)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 66)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 6)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(464)
				.FP(41)
				.STM(8)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(45)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 68)
				.MaxResistanceBonus(ResistanceType.Fire, 34)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 68)
				.MaxResistanceBonus(ResistanceType.Ice, 34)
				.MaxResistanceBonus(ResistanceType.Mind, 6)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(473)
				.FP(41)
				.STM(8)
				.DMG(27)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 25)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(46)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 69)
				.MaxResistanceBonus(ResistanceType.Fire, 35)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 69)
				.MaxResistanceBonus(ResistanceType.Ice, 35)
				.MaxResistanceBonus(ResistanceType.Mind, 6)
				.MaxResistanceBonus(ResistanceType.Trauma, 5)
				.MaxResistanceBonus(ResistanceType.Mobility, 5);
		}

    }
}
