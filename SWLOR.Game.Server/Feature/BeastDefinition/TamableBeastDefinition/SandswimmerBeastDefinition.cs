using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition
{
    public class SandswimmerBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Sandswimmer)
                .Name("Sandswimmer")
                .Appearance(AppearanceType.Aranea)
                .AppearanceScale(1f)
                .SoundSetId(396)
                .PortraitId(143)
                .CombatStats(AbilityType.Willpower, AbilityType.Vitality)
                .Role(BeastRoleType.Force)

                .CanMutateInto(BeastType.Stegosaurus)
				.MutationWeight(5)
				.MutationRequiresLyaseColor(EnzymeColorType.Black, 2)
				.MutationRequiresIsomeraseColor(EnzymeColorType.Red, 2)
				.MutationRequiresHydrolaseColor(EnzymeColorType.Red, 1)


				.CanMutateInto(BeastType.MushWarrior)
				.MutationWeight(15)
				.MutationRequiresLyaseColor(EnzymeColorType.Black, 2)

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
				.HP(20)
				.FP(2)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 5)
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
				.HP(48)
				.FP(3)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 16)
				.Stat(AbilityType.Willpower, 16)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 5)
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
				.HP(62)
				.FP(3)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 5)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 1)
				.MaxResistanceBonus(ResistanceType.Electrical, 5)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level4()
		{
			_builder
				.AddLevel()
				.HP(76)
				.FP(4)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(3)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
				.MaxResistanceBonus(ResistanceType.Fire, 2)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 6)
				.MaxResistanceBonus(ResistanceType.Ice, 2)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level5()
		{
			_builder
				.AddLevel()
				.HP(90)
				.FP(5)
				.STM(2)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(4)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
				.MaxResistanceBonus(ResistanceType.Fire, 3)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 3)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(104)
				.FP(6)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 17)
				.Stat(AbilityType.Willpower, 17)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 8)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 2)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(118)
				.FP(6)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 1)
				.MaxDefenseBonus(CombatDamageType.Force, 9)
				.MaxResistanceBonus(ResistanceType.Fire, 4)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 4)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(132)
				.FP(7)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 5)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 11)
				.MaxResistanceBonus(ResistanceType.Fire, 5)
				.MaxResistanceBonus(ResistanceType.Poison, 3)
				.MaxResistanceBonus(ResistanceType.Electrical, 11)
				.MaxResistanceBonus(ResistanceType.Ice, 5)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(146)
				.FP(8)
				.STM(3)
				.DMG(5)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
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
				.FP(8)
				.STM(3)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 13)
				.MaxResistanceBonus(ResistanceType.Fire, 6)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 13)
				.MaxResistanceBonus(ResistanceType.Ice, 6)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(174)
				.FP(9)
				.STM(3)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 18)
				.Stat(AbilityType.Willpower, 18)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(9)
				.MaxDefenseBonus(CombatDamageType.Physical, 2)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 4)
				.MaxResistanceBonus(ResistanceType.Electrical, 14)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(188)
				.FP(10)
				.STM(3)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(10)
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

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(202)
				.FP(10)
				.STM(3)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 17)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 5)
				.MaxResistanceBonus(ResistanceType.Electrical, 17)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(216)
				.FP(11)
				.STM(3)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(11)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 18)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(230)
				.FP(12)
				.STM(3)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 19)
				.Stat(AbilityType.Willpower, 19)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(12)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 19)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 6)
				.MaxResistanceBonus(ResistanceType.Electrical, 19)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(244)
				.FP(13)
				.STM(4)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(13)
				.MaxDefenseBonus(CombatDamageType.Physical, 3)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 20)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(258)
				.FP(13)
				.STM(4)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 6)
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

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(272)
				.FP(14)
				.STM(4)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(15)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 23)
				.MaxResistanceBonus(ResistanceType.Fire, 11)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 11)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(286)
				.FP(15)
				.STM(4)
				.DMG(8)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 24)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 24)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(300)
				.FP(15)
				.STM(4)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 20)
				.Stat(AbilityType.Willpower, 20)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 25)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 25)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(314)
				.FP(16)
				.STM(4)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 26)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(328)
				.FP(17)
				.STM(4)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(18)
				.MaxDefenseBonus(CombatDamageType.Physical, 4)
				.MaxDefenseBonus(CombatDamageType.Force, 27)
				.MaxResistanceBonus(ResistanceType.Fire, 13)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 13)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(342)
				.FP(17)
				.STM(4)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 6)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 29)
				.MaxResistanceBonus(ResistanceType.Fire, 14)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 14)
				.MaxResistanceBonus(ResistanceType.Mind, 2)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(356)
				.FP(18)
				.STM(4)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 21)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 6)
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

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(370)
				.FP(19)
				.STM(4)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 31)
				.MaxResistanceBonus(ResistanceType.Fire, 15)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 31)
				.MaxResistanceBonus(ResistanceType.Ice, 15)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(384)
				.FP(20)
				.STM(5)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 5)
				.MaxDefenseBonus(CombatDamageType.Force, 32)
				.MaxResistanceBonus(ResistanceType.Fire, 16)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 32)
				.MaxResistanceBonus(ResistanceType.Ice, 16)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(398)
				.FP(20)
				.STM(5)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(22)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 17)
				.MaxResistanceBonus(ResistanceType.Poison, 11)
				.MaxResistanceBonus(ResistanceType.Electrical, 34)
				.MaxResistanceBonus(ResistanceType.Ice, 17)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(412)
				.FP(21)
				.STM(5)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 22)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 7)
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

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(426)
				.FP(22)
				.STM(5)
				.DMG(12)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 7)
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

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(440)
				.FP(22)
				.STM(5)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 37)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 37)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(454)
				.FP(23)
				.STM(5)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 15)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 38)
				.MaxResistanceBonus(ResistanceType.Fire, 19)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 38)
				.MaxResistanceBonus(ResistanceType.Ice, 19)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(468)
				.FP(24)
				.STM(5)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 40)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 40)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(482)
				.FP(24)
				.STM(5)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 23)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 41)
				.MaxResistanceBonus(ResistanceType.Fire, 20)
				.MaxResistanceBonus(ResistanceType.Poison, 13)
				.MaxResistanceBonus(ResistanceType.Electrical, 41)
				.MaxResistanceBonus(ResistanceType.Ice, 20)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(496)
				.FP(25)
				.STM(5)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 42)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 3)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(510)
				.FP(26)
				.STM(5)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 14)
				.MaxResistanceBonus(ResistanceType.Electrical, 43)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(524)
				.FP(27)
				.STM(6)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 44)
				.MaxResistanceBonus(ResistanceType.Fire, 22)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 44)
				.MaxResistanceBonus(ResistanceType.Ice, 22)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(538)
				.FP(27)
				.STM(6)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 24)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 15)
				.MaxResistanceBonus(ResistanceType.Electrical, 46)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(552)
				.FP(28)
				.STM(6)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 16)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(31)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 47)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 47)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(566)
				.FP(29)
				.STM(6)
				.DMG(15)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 18)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 48)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 48)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(580)
				.FP(29)
				.STM(6)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 49)
				.MaxResistanceBonus(ResistanceType.Fire, 24)
				.MaxResistanceBonus(ResistanceType.Poison, 16)
				.MaxResistanceBonus(ResistanceType.Electrical, 49)
				.MaxResistanceBonus(ResistanceType.Ice, 24)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(594)
				.FP(30)
				.STM(6)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 8)
				.MaxDefenseBonus(CombatDamageType.Force, 50)
				.MaxResistanceBonus(ResistanceType.Fire, 25)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 50)
				.MaxResistanceBonus(ResistanceType.Ice, 25)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(608)
				.FP(31)
				.STM(6)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 52)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 17)
				.MaxResistanceBonus(ResistanceType.Electrical, 52)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 3);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(622)
				.FP(31)
				.STM(6)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 53)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 53)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(636)
				.FP(32)
				.STM(6)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(4)
				.MaxAccuracyBonus(4)
				.MaxEvasionBonus(36)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 54)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 54)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 4)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(650)
				.FP(33)
				.STM(6)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 19)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(37)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 55)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 55)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(664)
				.FP(34)
				.STM(7)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 9)
				.MaxDefenseBonus(CombatDamageType.Force, 56)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 56)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(678)
				.FP(34)
				.STM(7)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(38)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 58)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 19)
				.MaxResistanceBonus(ResistanceType.Electrical, 58)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(692)
				.FP(35)
				.STM(7)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(39)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 59)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 59)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(706)
				.FP(36)
				.STM(7)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 60)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(740)
				.FP(36)
				.STM(7)
				.DMG(19)
				.AttackDelay(ItemPropertyAttackDelay.Delay210)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 20)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(40)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 60)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

    }
}
