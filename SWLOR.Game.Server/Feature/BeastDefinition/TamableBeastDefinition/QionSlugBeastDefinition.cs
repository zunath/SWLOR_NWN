using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition
{
    public class QionSlugBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.QionSlug)
                .Name("Qion Slug")
                .Appearance(AppearanceType.SlugHugeCCP)
                .AppearanceScale(0.5f)
                .SoundSetId(397)
                .PortraitId(92)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Tank)

                .CanMutateInto(BeastType.StonecladBehemoth)
				.MutationWeight(5)
				.MutationRequiresLyaseColor(EnzymeColorType.Red, 2)


				.CanMutateInto(BeastType.MutatedBoar)
				.MutationWeight(50)
				.MutationRequiresIsomeraseColor(EnzymeColorType.Blue, 2)
				.MutationRequiresHydrolaseColor(EnzymeColorType.Purple, 2)

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
				.HP(70)
				.FP(4)
				.STM(4)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(0)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 1)
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
				.HP(118)
				.FP(4)
				.STM(4)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(1)
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

		private void Level3()
		{
			_builder
				.AddLevel()
				.HP(142)
				.FP(4)
				.STM(4)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
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
				.HP(166)
				.FP(5)
				.STM(5)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 3)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(190)
				.FP(5)
				.STM(5)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(214)
				.FP(5)
				.STM(5)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 7)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
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
				.HP(238)
				.FP(5)
				.STM(5)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(262)
				.FP(5)
				.STM(5)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(286)
				.FP(6)
				.STM(6)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(310)
				.FP(6)
				.STM(6)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(334)
				.FP(6)
				.STM(6)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(358)
				.FP(6)
				.STM(6)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(382)
				.FP(6)
				.STM(6)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(406)
				.FP(7)
				.STM(7)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(430)
				.FP(7)
				.STM(7)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(454)
				.FP(7)
				.STM(7)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 13)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(478)
				.FP(7)
				.STM(7)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(502)
				.FP(7)
				.STM(7)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 23)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(526)
				.FP(8)
				.STM(8)
				.DMG(10)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 24)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(550)
				.FP(8)
				.STM(8)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 25)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(574)
				.FP(8)
				.STM(8)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(598)
				.FP(8)
				.STM(8)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 27)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(622)
				.FP(8)
				.STM(8)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(646)
				.FP(9)
				.STM(9)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 30)
				.MaxDefenseBonus(CombatDamageType.Force, 30)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(670)
				.FP(9)
				.STM(9)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(694)
				.FP(9)
				.STM(9)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 32)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(718)
				.FP(9)
				.STM(9)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 22)
				.MaxResistanceBonus(ResistanceType.Poison, 22)
				.MaxResistanceBonus(ResistanceType.Electrical, 22)
				.MaxResistanceBonus(ResistanceType.Ice, 22)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(742)
				.FP(9)
				.STM(9)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(766)
				.FP(10)
				.STM(10)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 24)
				.MaxResistanceBonus(ResistanceType.Electrical, 24)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(790)
				.FP(10)
				.STM(10)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 37)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 24)
				.MaxResistanceBonus(ResistanceType.Electrical, 24)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(814)
				.FP(10)
				.STM(10)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 25)
				.MaxResistanceBonus(ResistanceType.Electrical, 25)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(838)
				.FP(10)
				.STM(10)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 26)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(862)
				.FP(10)
				.STM(10)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 41)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 27)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(886)
				.FP(11)
				.STM(11)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 28)
				.MaxResistanceBonus(ResistanceType.Electrical, 28)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(910)
				.FP(11)
				.STM(11)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 29)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(934)
				.FP(11)
				.STM(11)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 31)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 44)
				.MaxDefenseBonus(CombatDamageType.Force, 44)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 29)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(958)
				.FP(11)
				.STM(11)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 30)
				.MaxResistanceBonus(ResistanceType.Electrical, 30)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(982)
				.FP(11)
				.STM(11)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 47)
				.MaxDefenseBonus(CombatDamageType.Force, 47)
				.MaxResistanceBonus(ResistanceType.Fire, 31)
				.MaxResistanceBonus(ResistanceType.Poison, 31)
				.MaxResistanceBonus(ResistanceType.Electrical, 31)
				.MaxResistanceBonus(ResistanceType.Ice, 31)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(1006)
				.FP(12)
				.STM(12)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 48)
				.MaxDefenseBonus(CombatDamageType.Force, 48)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 32)
				.MaxResistanceBonus(ResistanceType.Electrical, 32)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(1030)
				.FP(12)
				.STM(12)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 49)
				.MaxDefenseBonus(CombatDamageType.Force, 49)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 33)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(1054)
				.FP(12)
				.STM(12)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 50)
				.MaxDefenseBonus(CombatDamageType.Force, 50)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 33)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(1078)
				.FP(12)
				.STM(12)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 52)
				.MaxDefenseBonus(CombatDamageType.Force, 52)
				.MaxResistanceBonus(ResistanceType.Fire, 34)
				.MaxResistanceBonus(ResistanceType.Poison, 34)
				.MaxResistanceBonus(ResistanceType.Electrical, 34)
				.MaxResistanceBonus(ResistanceType.Ice, 34)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(1102)
				.FP(12)
				.STM(12)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 33)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 53)
				.MaxDefenseBonus(CombatDamageType.Force, 53)
				.MaxResistanceBonus(ResistanceType.Fire, 35)
				.MaxResistanceBonus(ResistanceType.Poison, 35)
				.MaxResistanceBonus(ResistanceType.Electrical, 35)
				.MaxResistanceBonus(ResistanceType.Ice, 35)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(1126)
				.FP(13)
				.STM(13)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 54)
				.MaxDefenseBonus(CombatDamageType.Force, 54)
				.MaxResistanceBonus(ResistanceType.Fire, 36)
				.MaxResistanceBonus(ResistanceType.Poison, 36)
				.MaxResistanceBonus(ResistanceType.Electrical, 36)
				.MaxResistanceBonus(ResistanceType.Ice, 36)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(1150)
				.FP(13)
				.STM(13)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 55)
				.MaxDefenseBonus(CombatDamageType.Force, 55)
				.MaxResistanceBonus(ResistanceType.Fire, 37)
				.MaxResistanceBonus(ResistanceType.Poison, 37)
				.MaxResistanceBonus(ResistanceType.Electrical, 37)
				.MaxResistanceBonus(ResistanceType.Ice, 37)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(1174)
				.FP(13)
				.STM(13)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 56)
				.MaxDefenseBonus(CombatDamageType.Force, 56)
				.MaxResistanceBonus(ResistanceType.Fire, 38)
				.MaxResistanceBonus(ResistanceType.Poison, 38)
				.MaxResistanceBonus(ResistanceType.Electrical, 38)
				.MaxResistanceBonus(ResistanceType.Ice, 38)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(1198)
				.FP(13)
				.STM(13)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 34)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 58)
				.MaxDefenseBonus(CombatDamageType.Force, 58)
				.MaxResistanceBonus(ResistanceType.Fire, 38)
				.MaxResistanceBonus(ResistanceType.Poison, 38)
				.MaxResistanceBonus(ResistanceType.Electrical, 38)
				.MaxResistanceBonus(ResistanceType.Ice, 38)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(1222)
				.FP(13)
				.STM(13)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 59)
				.MaxDefenseBonus(CombatDamageType.Force, 59)
				.MaxResistanceBonus(ResistanceType.Fire, 39)
				.MaxResistanceBonus(ResistanceType.Poison, 39)
				.MaxResistanceBonus(ResistanceType.Electrical, 39)
				.MaxResistanceBonus(ResistanceType.Ice, 39)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(1246)
				.FP(14)
				.STM(14)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxResistanceBonus(ResistanceType.Fire, 40)
				.MaxResistanceBonus(ResistanceType.Poison, 40)
				.MaxResistanceBonus(ResistanceType.Electrical, 40)
				.MaxResistanceBonus(ResistanceType.Ice, 40)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(1270)
				.FP(14)
				.STM(14)
				.DMG(24)
				.AttackDelay(ItemPropertyAttackDelay.Delay240)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 35)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxResistanceBonus(ResistanceType.Fire, 40)
				.MaxResistanceBonus(ResistanceType.Poison, 40)
				.MaxResistanceBonus(ResistanceType.Electrical, 40)
				.MaxResistanceBonus(ResistanceType.Ice, 40)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

    }
}
