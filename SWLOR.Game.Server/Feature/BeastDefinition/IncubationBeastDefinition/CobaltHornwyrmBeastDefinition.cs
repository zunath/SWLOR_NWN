using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class CobaltHornwyrmBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.CobaltHornwyrm)
                .Name("Cobalt Hornwyrm")
                .Appearance(AppearanceType.FelldrakeHornedHardpoints)
                .AppearanceScale(0.6f)
                .SoundSetId(30)
                .PortraitId(192)
                .CombatStats(AbilityType.Perception, AbilityType.Vitality)
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
				.HP(126)
				.FP(7)
				.STM(7)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 12)
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
				.HP(140)
				.FP(7)
				.STM(8)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(1)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
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
				.HP(153)
				.FP(8)
				.STM(8)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
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
				.HP(167)
				.FP(8)
				.STM(9)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
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
				.HP(180)
				.FP(8)
				.STM(10)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
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
				.HP(194)
				.FP(9)
				.STM(11)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
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
				.HP(207)
				.FP(9)
				.STM(11)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(10)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 1)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(221)
				.FP(9)
				.STM(12)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(234)
				.FP(9)
				.STM(14)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 14)
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

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(248)
				.FP(10)
				.STM(14)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(261)
				.FP(10)
				.STM(15)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(10)
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

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(275)
				.FP(10)
				.STM(16)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 2)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(288)
				.FP(11)
				.STM(16)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
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

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(302)
				.FP(11)
				.STM(17)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(12)
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

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(315)
				.FP(11)
				.STM(18)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(329)
				.FP(12)
				.STM(19)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(342)
				.FP(12)
				.STM(19)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(356)
				.FP(12)
				.STM(20)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(369)
				.FP(12)
				.STM(21)
				.DMG(13)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 16)
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

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(383)
				.FP(14)
				.STM(21)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 16)
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

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(396)
				.FP(14)
				.STM(23)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(410)
				.FP(14)
				.STM(24)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
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

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(423)
				.FP(15)
				.STM(24)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(437)
				.FP(15)
				.STM(25)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(450)
				.FP(15)
				.STM(26)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 17)
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

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(464)
				.FP(16)
				.STM(27)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 17)
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

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(477)
				.FP(16)
				.STM(27)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(491)
				.FP(16)
				.STM(28)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 36)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(39)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 39)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(504)
				.FP(16)
				.STM(29)
				.DMG(17)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
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

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(518)
				.FP(17)
				.STM(29)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(42)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(531)
				.FP(17)
				.STM(30)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(43)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(545)
				.FP(17)
				.STM(32)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(45)
				.MaxAccuracyBonus(29)
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

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(558)
				.FP(18)
				.STM(32)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(46)
				.MaxAccuracyBonus(30)
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

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(572)
				.FP(18)
				.STM(33)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(47)
				.MaxAccuracyBonus(32)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 47)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(585)
				.FP(18)
				.STM(34)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(48)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 48)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(599)
				.FP(19)
				.STM(35)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 37)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(50)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 50)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(612)
				.FP(19)
				.STM(35)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(52)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 52)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(626)
				.FP(19)
				.STM(36)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(53)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 53)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(639)
				.FP(19)
				.STM(37)
				.DMG(21)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(54)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 54)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(653)
				.FP(20)
				.STM(37)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(55)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 55)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(666)
				.FP(20)
				.STM(38)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(56)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 56)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(680)
				.FP(20)
				.STM(39)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(59)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 59)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(693)
				.FP(21)
				.STM(39)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(60)
				.MaxAccuracyBonus(39)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(707)
				.FP(21)
				.STM(41)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 38)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(61)
				.MaxAccuracyBonus(41)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 61)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(720)
				.FP(21)
				.STM(42)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 39)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(62)
				.MaxAccuracyBonus(42)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 62)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(734)
				.FP(23)
				.STM(43)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 39)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(63)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 63)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(747)
				.FP(23)
				.STM(43)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 39)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(65)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 65)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(761)
				.FP(23)
				.STM(44)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 39)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(66)
				.MaxAccuracyBonus(44)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 66)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(774)
				.FP(23)
				.STM(45)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 39)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(68)
				.MaxAccuracyBonus(45)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 68)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(788)
				.FP(24)
				.STM(45)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 39)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(69)
				.MaxAccuracyBonus(46)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 69)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

    }
}
