using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class RubybackDrakonBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.RubybackDrakon)
                .Name("Rubyback Drakon")
                .Appearance(AppearanceType.DragonkinRed2AdamMillerbloodsong)
                .AppearanceScale(0.5f)
                .SoundSetId(43)
                .PortraitId(13)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
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
				.HP(136)
				.FP(6)
				.STM(6)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 33)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(0)
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
				.HP(160)
				.FP(7)
				.STM(7)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(2)
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
				.HP(183)
				.FP(7)
				.STM(7)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 10)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 21)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(2)
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

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(207)
				.FP(8)
				.STM(8)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 24)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 3)
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
				.HP(231)
				.FP(8)
				.STM(8)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 34)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(254)
				.FP(9)
				.STM(9)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(278)
				.FP(9)
				.STM(9)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(302)
				.FP(10)
				.STM(10)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(325)
				.FP(10)
				.STM(10)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 35)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(349)
				.FP(11)
				.STM(11)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 11)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 23)
				.MaxAttackBonus(9)
				.MaxAccuracyBonus(9)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(372)
				.FP(11)
				.STM(11)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(10)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(396)
				.FP(12)
				.STM(12)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 25)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(420)
				.FP(12)
				.STM(12)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 36)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(443)
				.FP(14)
				.STM(14)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(12)
				.MaxAccuracyBonus(12)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(467)
				.FP(14)
				.STM(14)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(14)
				.MaxAccuracyBonus(14)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(491)
				.FP(15)
				.STM(15)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 37)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(15)
				.MaxAccuracyBonus(15)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(514)
				.FP(15)
				.STM(15)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 24)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(538)
				.FP(16)
				.STM(16)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(561)
				.FP(16)
				.STM(16)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(585)
				.FP(17)
				.STM(17)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 38)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 26)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(609)
				.FP(17)
				.STM(17)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(632)
				.FP(18)
				.STM(18)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(656)
				.FP(18)
				.STM(18)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(680)
				.FP(19)
				.STM(19)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 39)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(703)
				.FP(19)
				.STM(19)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(23)
				.MaxAccuracyBonus(23)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(727)
				.FP(20)
				.STM(20)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(24)
				.MaxAccuracyBonus(24)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(750)
				.FP(20)
				.STM(20)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(25)
				.MaxAccuracyBonus(25)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(774)
				.FP(21)
				.STM(21)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 41)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 27)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(798)
				.FP(21)
				.STM(21)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(821)
				.FP(23)
				.STM(23)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(27)
				.MaxAccuracyBonus(27)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(845)
				.FP(23)
				.STM(23)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 26)
				.MaxAttackBonus(28)
				.MaxAccuracyBonus(28)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 28)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(869)
				.FP(24)
				.STM(24)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 42)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(892)
				.FP(24)
				.STM(24)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(30)
				.MaxAccuracyBonus(30)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(916)
				.FP(25)
				.STM(25)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(32)
				.MaxAccuracyBonus(32)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 24)
				.MaxResistanceBonus(ResistanceType.Electrical, 24)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(939)
				.FP(25)
				.STM(25)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 43)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 24)
				.MaxResistanceBonus(ResistanceType.Electrical, 24)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(963)
				.FP(26)
				.STM(26)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 28)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 25)
				.MaxResistanceBonus(ResistanceType.Electrical, 25)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(987)
				.FP(26)
				.STM(26)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 26)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(1010)
				.FP(27)
				.STM(27)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 27)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 26)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(1034)
				.FP(27)
				.STM(27)
				.DMG(32)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 44)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 27)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(1058)
				.FP(28)
				.STM(28)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 45)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(37)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 27)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(1081)
				.FP(28)
				.STM(28)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 45)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(37)
				.MaxAccuracyBonus(37)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 28)
				.MaxResistanceBonus(ResistanceType.Electrical, 28)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(1105)
				.FP(29)
				.STM(29)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 45)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 29)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(1128)
				.FP(29)
				.STM(29)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 45)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(39)
				.MaxAccuracyBonus(39)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 39)
				.MaxDefenseBonus(CombatDamageType.Force, 39)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 29)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(1152)
				.FP(30)
				.STM(30)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 46)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 29)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(41)
				.MaxAccuracyBonus(41)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 30)
				.MaxResistanceBonus(ResistanceType.Electrical, 30)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(1176)
				.FP(30)
				.STM(30)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 46)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 28)
				.MaxAttackBonus(42)
				.MaxAccuracyBonus(42)
				.MaxEvasionBonus(42)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 32)
				.MaxResistanceBonus(ResistanceType.Electrical, 32)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(1199)
				.FP(32)
				.STM(32)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 46)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 29)
				.MaxAttackBonus(43)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(43)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 32)
				.MaxResistanceBonus(ResistanceType.Electrical, 32)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(1223)
				.FP(32)
				.STM(32)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 46)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 29)
				.MaxAttackBonus(43)
				.MaxAccuracyBonus(43)
				.MaxEvasionBonus(43)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 33)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(1247)
				.FP(33)
				.STM(33)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 47)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 29)
				.MaxAttackBonus(44)
				.MaxAccuracyBonus(44)
				.MaxEvasionBonus(44)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 44)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 33)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(1270)
				.FP(33)
				.STM(33)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 47)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 29)
				.MaxAttackBonus(45)
				.MaxAccuracyBonus(45)
				.MaxEvasionBonus(45)
				.MaxDefenseBonus(CombatDamageType.Physical, 45)
				.MaxDefenseBonus(CombatDamageType.Force, 45)
				.MaxResistanceBonus(ResistanceType.Fire, 34)
				.MaxResistanceBonus(ResistanceType.Poison, 34)
				.MaxResistanceBonus(ResistanceType.Electrical, 34)
				.MaxResistanceBonus(ResistanceType.Ice, 34)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(1294)
				.FP(34)
				.STM(34)
				.DMG(41)
				.AttackDelay(ItemPropertyAttackDelay.Delay220)
				.Stat(AbilityType.Might, 47)
				.Stat(AbilityType.Perception, 23)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 30)
				.Stat(AbilityType.Social, 29)
				.MaxAttackBonus(46)
				.MaxAccuracyBonus(46)
				.MaxEvasionBonus(46)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxResistanceBonus(ResistanceType.Fire, 35)
				.MaxResistanceBonus(ResistanceType.Poison, 35)
				.MaxResistanceBonus(ResistanceType.Electrical, 35)
				.MaxResistanceBonus(ResistanceType.Ice, 35)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

    }
}
