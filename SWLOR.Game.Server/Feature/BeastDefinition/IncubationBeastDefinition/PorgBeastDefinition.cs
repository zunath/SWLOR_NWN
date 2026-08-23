using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class PorgBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.Porg)
                .Name("Porg")
                .Appearance(AppearanceType.SWLORPorg)
                .AppearanceScale(1f)
                .SoundSetId(452)
                .PortraitId(564)
                .CombatStats(AbilityType.Social, AbilityType.Willpower)
                .Role(BeastRoleType.Force)

                .CanMutateInto(BeastType.CloudcallAurelith)
				.MutationWeight(90)
				.MutationRequiresIsomeraseColor(EnzymeColorType.Red, 3)
				.MutationRequiresHydrolaseColor(EnzymeColorType.Blue, 3)
				.MutationRequiresDayOfWeek(DayOfWeek.Tuesday, DayOfWeek.Thursday)


				.CanMutateInto(BeastType.VoidmireEcho)
				.MutationWeight(10)
				.MutationRequiresIsomeraseColor(EnzymeColorType.Red, 3)
				.MutationRequiresHydrolaseColor(EnzymeColorType.Blue, 3)
				.MutationRequiresDayOfWeek(DayOfWeek.Tuesday, DayOfWeek.Thursday)

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
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 8)
				.Stat(AbilityType.Vitality, 5)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 8)
				.Stat(AbilityType.Social, 22)
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
				.HP(40)
				.FP(3)
				.STM(2)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 5)
				.Stat(AbilityType.Willpower, 21)
				.Stat(AbilityType.Agility, 8)
				.Stat(AbilityType.Social, 22)
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
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 5)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 8)
				.Stat(AbilityType.Social, 22)
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
				.HP(60)
				.FP(4)
				.STM(2)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 8)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 22)
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
				.HP(70)
				.FP(5)
				.STM(2)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 9)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 22)
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
				.HP(80)
				.FP(6)
				.STM(3)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 22)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 22)
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
				.HP(90)
				.FP(6)
				.STM(3)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 22)
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
				.HP(100)
				.FP(7)
				.STM(3)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 22)
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
				.HP(110)
				.FP(8)
				.STM(3)
				.DMG(4)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 10)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 23)
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
				.HP(120)
				.FP(8)
				.STM(3)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 6)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 9)
				.Stat(AbilityType.Social, 23)
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
				.HP(130)
				.FP(9)
				.STM(3)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 23)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(140)
				.FP(10)
				.STM(3)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 9)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(150)
				.FP(10)
				.STM(3)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 11)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(160)
				.FP(11)
				.STM(3)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(170)
				.FP(12)
				.STM(3)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 24)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(180)
				.FP(13)
				.STM(4)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 12)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(190)
				.FP(13)
				.STM(4)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 7)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 23)
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
				.HP(200)
				.FP(14)
				.STM(4)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(210)
				.FP(15)
				.STM(4)
				.DMG(7)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(220)
				.FP(15)
				.STM(4)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 10)
				.Stat(AbilityType.Perception, 13)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 25)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(230)
				.FP(16)
				.STM(4)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(240)
				.FP(17)
				.STM(4)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(250)
				.FP(17)
				.STM(4)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(260)
				.FP(18)
				.STM(4)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 14)
				.Stat(AbilityType.Vitality, 8)
				.Stat(AbilityType.Willpower, 26)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 23)
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
				.HP(270)
				.FP(19)
				.STM(4)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(280)
				.FP(20)
				.STM(5)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(290)
				.FP(20)
				.STM(5)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(300)
				.FP(21)
				.STM(5)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 11)
				.Stat(AbilityType.Perception, 15)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 27)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(310)
				.FP(22)
				.STM(5)
				.DMG(11)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(320)
				.FP(22)
				.STM(5)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(330)
				.FP(23)
				.STM(5)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 9)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 24)
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
				.HP(340)
				.FP(24)
				.STM(5)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 16)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(350)
				.FP(24)
				.STM(5)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 28)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(360)
				.FP(25)
				.STM(5)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(370)
				.FP(26)
				.STM(5)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 17)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(380)
				.FP(27)
				.STM(6)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 12)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(390)
				.FP(27)
				.STM(6)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(400)
				.FP(28)
				.STM(6)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 10)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 13)
				.Stat(AbilityType.Social, 24)
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
				.HP(410)
				.FP(29)
				.STM(6)
				.DMG(13)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 18)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
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
				.HP(420)
				.FP(29)
				.STM(6)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 24)
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
				.HP(430)
				.FP(30)
				.STM(6)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 25)
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
				.HP(440)
				.FP(31)
				.STM(6)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 25)
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
				.HP(450)
				.FP(31)
				.STM(6)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 19)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 31)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 25)
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
				.HP(460)
				.FP(32)
				.STM(6)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 13)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 31)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 25)
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
				.HP(470)
				.FP(33)
				.STM(6)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 11)
				.Stat(AbilityType.Willpower, 31)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 25)
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
				.HP(480)
				.FP(34)
				.STM(7)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 31)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 25)
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
				.HP(490)
				.FP(34)
				.STM(7)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 20)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 25)
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
				.HP(500)
				.FP(35)
				.STM(7)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 25)
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
				.HP(510)
				.FP(36)
				.STM(7)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 25)
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
				.HP(520)
				.FP(36)
				.STM(7)
				.DMG(17)
				.Delay(20)
				.Stat(AbilityType.Might, 14)
				.Stat(AbilityType.Perception, 21)
				.Stat(AbilityType.Vitality, 12)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 25)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(41)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 61)
				.MaxResistanceBonus(ResistanceType.Fire, 31)
				.MaxResistanceBonus(ResistanceType.Poison, 20)
				.MaxResistanceBonus(ResistanceType.Electrical, 61)
				.MaxResistanceBonus(ResistanceType.Ice, 31)
				.MaxResistanceBonus(ResistanceType.Mind, 5)
				.MaxResistanceBonus(ResistanceType.Trauma, 4)
				.MaxResistanceBonus(ResistanceType.Mobility, 4);
		}

    }
}
