using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition
{
    public class WarocasBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Warocas)
                .Name("Warocas")
                .Appearance(AppearanceType.BirdOstrichProjectQ)
                .AppearanceScale(1f)
                .SoundSetId(452)
                .PortraitId(168)
                .CombatStats(AbilityType.Social, AbilityType.Willpower)
                .Role(BeastRoleType.Balanced)

                .CanMutateInto(BeastType.Grutchin)
				.MutationWeight(50)
				.MutationRequiresDayOfWeek(DayOfWeek.Sunday)


				.CanMutateInto(BeastType.BurrowberryPack)
				.MutationWeight(10)
				.MutationRequiresLyaseColor(EnzymeColorType.Green, 2)

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
				.HP(40)
				.FP(5)
				.STM(5)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 12)
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
				.HP(64)
				.FP(6)
				.STM(6)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 12)
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
				.HP(76)
				.FP(6)
				.STM(6)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 12)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 12)
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
				.HP(88)
				.FP(7)
				.STM(7)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
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
				.HP(100)
				.FP(7)
				.STM(7)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 4)
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
				.HP(112)
				.FP(8)
				.STM(8)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(124)
				.FP(8)
				.STM(8)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 4)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(136)
				.FP(9)
				.STM(9)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
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

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(148)
				.FP(9)
				.STM(9)
				.DMG(6)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 13)
				.Stat(AbilityType.Willpower, 13)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 13)
				.MaxAttackBonus(7)
				.MaxAccuracyBonus(7)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(160)
				.FP(10)
				.STM(10)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(8)
				.MaxAccuracyBonus(8)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(172)
				.FP(10)
				.STM(10)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(184)
				.FP(11)
				.STM(11)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(10)
				.MaxAccuracyBonus(10)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(196)
				.FP(11)
				.STM(11)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(208)
				.FP(12)
				.STM(12)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(11)
				.MaxAccuracyBonus(11)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 11)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(220)
				.FP(12)
				.STM(12)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 14)
				.Stat(AbilityType.Willpower, 14)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 14)
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

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(232)
				.FP(13)
				.STM(13)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(13)
				.MaxAccuracyBonus(13)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 13)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(244)
				.FP(13)
				.STM(13)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(256)
				.FP(14)
				.STM(14)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
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

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(268)
				.FP(14)
				.STM(14)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(280)
				.FP(15)
				.STM(15)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(16)
				.MaxAccuracyBonus(16)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(292)
				.FP(15)
				.STM(15)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 15)
				.Stat(AbilityType.Willpower, 15)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(17)
				.MaxAccuracyBonus(17)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 17)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 13)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(304)
				.FP(16)
				.STM(16)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(18)
				.MaxAccuracyBonus(18)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 18)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 13)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(316)
				.FP(16)
				.STM(16)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(19)
				.MaxAccuracyBonus(19)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 19)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(328)
				.FP(17)
				.STM(17)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
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

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(340)
				.FP(17)
				.STM(17)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(20)
				.MaxAccuracyBonus(20)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 15)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(352)
				.FP(18)
				.STM(18)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(21)
				.MaxAccuracyBonus(21)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 21)
				.MaxDefenseBonus(CombatDamageType.Force, 21)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 16)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(364)
				.FP(18)
				.STM(18)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 16)
				.MaxAttackBonus(22)
				.MaxAccuracyBonus(22)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 22)
				.MaxDefenseBonus(CombatDamageType.Force, 22)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(376)
				.FP(19)
				.STM(19)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(388)
				.FP(19)
				.STM(19)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(400)
				.FP(20)
				.STM(20)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(412)
				.FP(20)
				.STM(20)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(424)
				.FP(21)
				.STM(21)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
				.MaxAttackBonus(26)
				.MaxAccuracyBonus(26)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 26)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(436)
				.FP(21)
				.STM(21)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 17)
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

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(448)
				.FP(22)
				.STM(22)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
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

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(460)
				.FP(22)
				.STM(22)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(472)
				.FP(23)
				.STM(23)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(29)
				.MaxAccuracyBonus(29)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 29)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 22)
				.MaxResistanceBonus(ResistanceType.Poison, 22)
				.MaxResistanceBonus(ResistanceType.Electrical, 22)
				.MaxResistanceBonus(ResistanceType.Ice, 22)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(484)
				.FP(23)
				.STM(23)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
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

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(496)
				.FP(24)
				.STM(24)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
				.MaxAttackBonus(31)
				.MaxAccuracyBonus(31)
				.MaxEvasionBonus(31)
				.MaxDefenseBonus(CombatDamageType.Physical, 31)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(508)
				.FP(24)
				.STM(24)
				.DMG(26)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 18)
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

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(520)
				.FP(25)
				.STM(25)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
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

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(532)
				.FP(25)
				.STM(25)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(33)
				.MaxAccuracyBonus(33)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 25)
				.MaxResistanceBonus(ResistanceType.Electrical, 25)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(544)
				.FP(26)
				.STM(26)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(34)
				.MaxAccuracyBonus(34)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 26)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(556)
				.FP(26)
				.STM(26)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(35)
				.MaxAccuracyBonus(35)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 35)
				.MaxDefenseBonus(CombatDamageType.Force, 35)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 26)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(568)
				.FP(27)
				.STM(27)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
				.MaxAttackBonus(36)
				.MaxAccuracyBonus(36)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 36)
				.MaxDefenseBonus(CombatDamageType.Force, 36)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 27)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(580)
				.FP(27)
				.STM(27)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 19)
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

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(592)
				.FP(28)
				.STM(28)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(38)
				.MaxAccuracyBonus(38)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 38)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 28)
				.MaxResistanceBonus(ResistanceType.Electrical, 28)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(604)
				.FP(28)
				.STM(28)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 20)
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

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(616)
				.FP(29)
				.STM(29)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 20)
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

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(628)
				.FP(29)
				.STM(29)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(40)
				.MaxAccuracyBonus(40)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 30)
				.MaxResistanceBonus(ResistanceType.Electrical, 30)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(640)
				.FP(30)
				.STM(30)
				.DMG(33)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 20)
				.MaxAttackBonus(40)
				.MaxAccuracyBonus(40)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 40)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 30)
				.MaxResistanceBonus(ResistanceType.Electrical, 30)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

    }
}
