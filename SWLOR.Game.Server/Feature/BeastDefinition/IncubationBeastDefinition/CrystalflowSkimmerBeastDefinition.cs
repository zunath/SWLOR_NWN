using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class CrystalflowSkimmerBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.CrystalflowSkimmer)
                .Name("Crystalflow Skimmer")
                .Appearance(AppearanceType.OozeCrystalOozeMediumHalfscan)
                .AppearanceScale(1f)
                .SoundSetId(111)
                .PortraitId(2253)
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
				.HP(68)
				.FP(2)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
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
				.HP(79)
				.FP(2)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
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
				.HP(88)
				.FP(2)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(4)
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
				.HP(97)
				.FP(2)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 2)
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
				.HP(105)
				.FP(2)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(114)
				.FP(3)
				.STM(4)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(123)
				.FP(3)
				.STM(4)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(7)
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

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(132)
				.FP(3)
				.STM(4)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(140)
				.FP(3)
				.STM(4)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(149)
				.FP(3)
				.STM(5)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(158)
				.FP(3)
				.STM(5)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(11)
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

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(167)
				.FP(3)
				.STM(5)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(175)
				.FP(3)
				.STM(5)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(184)
				.FP(3)
				.STM(5)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
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

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(193)
				.FP(3)
				.STM(5)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(201)
				.FP(4)
				.STM(6)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(210)
				.FP(4)
				.STM(6)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(219)
				.FP(4)
				.STM(6)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(228)
				.FP(4)
				.STM(6)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(236)
				.FP(4)
				.STM(7)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
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
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(245)
				.FP(4)
				.STM(7)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(13)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(254)
				.FP(4)
				.STM(7)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(20)
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

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(262)
				.FP(4)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(271)
				.FP(4)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 10)
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

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(280)
				.FP(4)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(289)
				.FP(5)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(297)
				.FP(5)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(306)
				.FP(5)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(315)
				.FP(5)
				.STM(8)
				.DMG(11)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(323)
				.FP(5)
				.STM(9)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(332)
				.FP(5)
				.STM(9)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(341)
				.FP(5)
				.STM(9)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(350)
				.FP(5)
				.STM(10)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(31)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(358)
				.FP(5)
				.STM(10)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(367)
				.FP(5)
				.STM(10)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 22)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(376)
				.FP(5)
				.STM(11)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 22)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(384)
				.FP(5)
				.STM(11)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
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

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(393)
				.FP(5)
				.STM(11)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(402)
				.FP(5)
				.STM(11)
				.DMG(14)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
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

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(411)
				.FP(5)
				.STM(11)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(419)
				.FP(5)
				.STM(11)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 13)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(428)
				.FP(5)
				.STM(11)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 13)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(437)
				.FP(5)
				.STM(12)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(446)
				.FP(5)
				.STM(12)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
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

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(454)
				.FP(5)
				.STM(12)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 28)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(463)
				.FP(6)
				.STM(13)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(42)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(472)
				.FP(6)
				.STM(13)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(44)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 22)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 22)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(480)
				.FP(6)
				.STM(13)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(44)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 22)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 22)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(489)
				.FP(6)
				.STM(13)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(45)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(498)
				.FP(6)
				.STM(14)
				.DMG(18)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(31)
				.MaxEvasionBonus(46)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

    }
}
