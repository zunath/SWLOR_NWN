using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition
{
    public class GranitebackUrsavarBeastDefinition: IBeastListDefinition
    {
        private readonly BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {
            _builder.Create(BeastType.GranitebackUrsavar)
                .Name("Graniteback Ursavar")
                .Appearance(AppearanceType.BearGrizzly)
                .AppearanceScale(1f)
                .SoundSetId(23)
                .PortraitId(712)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Tank)


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
				.HP(194)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 17)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 10)
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
				.HP(218)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 26)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 10)
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
				.HP(243)
				.FP(5)
				.STM(5)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 29)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 7)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 6)
				.MaxDefenseBonus(CombatDamageType.Force, 6)
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
				.HP(268)
				.FP(6)
				.STM(6)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 25)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 10)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(0)
				.MaxAccuracyBonus(0)
				.MaxEvasionBonus(2)
				.MaxDefenseBonus(CombatDamageType.Physical, 7)
				.MaxDefenseBonus(CombatDamageType.Force, 7)
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
				.HP(293)
				.FP(6)
				.STM(6)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 18)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(3)
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

		private void Level6()
		{
			_builder
				.AddLevel()
				.HP(317)
				.FP(6)
				.STM(6)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
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

		private void Level7()
		{
			_builder
				.AddLevel()
				.HP(342)
				.FP(6)
				.STM(6)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(5)
				.MaxDefenseBonus(CombatDamageType.Physical, 10)
				.MaxDefenseBonus(CombatDamageType.Force, 10)
				.MaxResistanceBonus(ResistanceType.Fire, 7)
				.MaxResistanceBonus(ResistanceType.Poison, 7)
				.MaxResistanceBonus(ResistanceType.Electrical, 7)
				.MaxResistanceBonus(ResistanceType.Ice, 7)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level8()
		{
			_builder
				.AddLevel()
				.HP(367)
				.FP(6)
				.STM(6)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 27)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(6)
				.MaxDefenseBonus(CombatDamageType.Physical, 12)
				.MaxDefenseBonus(CombatDamageType.Force, 12)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 0)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level9()
		{
			_builder
				.AddLevel()
				.HP(392)
				.FP(7)
				.STM(7)
				.DMG(6)
				.Delay(23)
				.Stat(AbilityType.Might, 19)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 14)
				.MaxDefenseBonus(CombatDamageType.Force, 14)
				.MaxResistanceBonus(ResistanceType.Fire, 8)
				.MaxResistanceBonus(ResistanceType.Poison, 8)
				.MaxResistanceBonus(ResistanceType.Electrical, 8)
				.MaxResistanceBonus(ResistanceType.Ice, 8)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level10()
		{
			_builder
				.AddLevel()
				.HP(416)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 30)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 8)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(7)
				.MaxDefenseBonus(CombatDamageType.Physical, 15)
				.MaxDefenseBonus(CombatDamageType.Force, 15)
				.MaxResistanceBonus(ResistanceType.Fire, 9)
				.MaxResistanceBonus(ResistanceType.Poison, 9)
				.MaxResistanceBonus(ResistanceType.Electrical, 9)
				.MaxResistanceBonus(ResistanceType.Ice, 9)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level11()
		{
			_builder
				.AddLevel()
				.HP(441)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(8)
				.MaxDefenseBonus(CombatDamageType.Physical, 16)
				.MaxDefenseBonus(CombatDamageType.Force, 16)
				.MaxResistanceBonus(ResistanceType.Fire, 10)
				.MaxResistanceBonus(ResistanceType.Poison, 10)
				.MaxResistanceBonus(ResistanceType.Electrical, 10)
				.MaxResistanceBonus(ResistanceType.Ice, 10)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level12()
		{
			_builder
				.AddLevel()
				.HP(466)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 26)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 11)
				.Stat(AbilityType.Social, 9)
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
				.MaxResistanceBonus(ResistanceType.Mobility, 0);
		}

		private void Level13()
		{
			_builder
				.AddLevel()
				.HP(491)
				.FP(7)
				.STM(7)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 20)
				.Stat(AbilityType.Perception, 28)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
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

		private void Level14()
		{
			_builder
				.AddLevel()
				.HP(515)
				.FP(8)
				.STM(8)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(1)
				.MaxAccuracyBonus(1)
				.MaxEvasionBonus(10)
				.MaxDefenseBonus(CombatDamageType.Physical, 20)
				.MaxDefenseBonus(CombatDamageType.Force, 20)
				.MaxResistanceBonus(ResistanceType.Fire, 12)
				.MaxResistanceBonus(ResistanceType.Poison, 12)
				.MaxResistanceBonus(ResistanceType.Electrical, 12)
				.MaxResistanceBonus(ResistanceType.Ice, 12)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level15()
		{
			_builder
				.AddLevel()
				.HP(540)
				.FP(8)
				.STM(8)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
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

		private void Level16()
		{
			_builder
				.AddLevel()
				.HP(565)
				.FP(8)
				.STM(8)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 21)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
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

		private void Level17()
		{
			_builder
				.AddLevel()
				.HP(590)
				.FP(8)
				.STM(8)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 32)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 9)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(11)
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

		private void Level18()
		{
			_builder
				.AddLevel()
				.HP(614)
				.FP(8)
				.STM(8)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(12)
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

		private void Level19()
		{
			_builder
				.AddLevel()
				.HP(639)
				.FP(9)
				.STM(9)
				.DMG(10)
				.Delay(23)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 29)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(14)
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

		private void Level20()
		{
			_builder
				.AddLevel()
				.HP(664)
				.FP(9)
				.STM(9)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 23)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 27)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 12)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(14)
				.MaxDefenseBonus(CombatDamageType.Physical, 28)
				.MaxDefenseBonus(CombatDamageType.Force, 28)
				.MaxResistanceBonus(ResistanceType.Fire, 18)
				.MaxResistanceBonus(ResistanceType.Poison, 18)
				.MaxResistanceBonus(ResistanceType.Electrical, 18)
				.MaxResistanceBonus(ResistanceType.Ice, 18)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level21()
		{
			_builder
				.AddLevel()
				.HP(689)
				.FP(9)
				.STM(9)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(15)
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

		private void Level22()
		{
			_builder
				.AddLevel()
				.HP(713)
				.FP(9)
				.STM(9)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
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

		private void Level23()
		{
			_builder
				.AddLevel()
				.HP(738)
				.FP(9)
				.STM(9)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(16)
				.MaxDefenseBonus(CombatDamageType.Physical, 33)
				.MaxDefenseBonus(CombatDamageType.Force, 33)
				.MaxResistanceBonus(ResistanceType.Fire, 21)
				.MaxResistanceBonus(ResistanceType.Poison, 21)
				.MaxResistanceBonus(ResistanceType.Electrical, 21)
				.MaxResistanceBonus(ResistanceType.Ice, 21)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level24()
		{
			_builder
				.AddLevel()
				.HP(763)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 24)
				.Stat(AbilityType.Perception, 30)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 33)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 10)
				.MaxAttackBonus(2)
				.MaxAccuracyBonus(2)
				.MaxEvasionBonus(17)
				.MaxDefenseBonus(CombatDamageType.Physical, 34)
				.MaxDefenseBonus(CombatDamageType.Force, 34)
				.MaxResistanceBonus(ResistanceType.Fire, 23)
				.MaxResistanceBonus(ResistanceType.Poison, 23)
				.MaxResistanceBonus(ResistanceType.Electrical, 23)
				.MaxResistanceBonus(ResistanceType.Ice, 23)
				.MaxResistanceBonus(ResistanceType.Mind, 0)
				.MaxResistanceBonus(ResistanceType.Trauma, 1)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level25()
		{
			_builder
				.AddLevel()
				.HP(788)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
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

		private void Level26()
		{
			_builder
				.AddLevel()
				.HP(812)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
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

		private void Level27()
		{
			_builder
				.AddLevel()
				.HP(837)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
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

		private void Level28()
		{
			_builder
				.AddLevel()
				.HP(862)
				.FP(10)
				.STM(10)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 25)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 28)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 14)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(19)
				.MaxDefenseBonus(CombatDamageType.Physical, 39)
				.MaxDefenseBonus(CombatDamageType.Force, 39)
				.MaxResistanceBonus(ResistanceType.Fire, 26)
				.MaxResistanceBonus(ResistanceType.Poison, 26)
				.MaxResistanceBonus(ResistanceType.Electrical, 26)
				.MaxResistanceBonus(ResistanceType.Ice, 26)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level29()
		{
			_builder
				.AddLevel()
				.HP(887)
				.FP(11)
				.STM(11)
				.DMG(16)
				.Delay(23)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 32)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
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

		private void Level30()
		{
			_builder
				.AddLevel()
				.HP(911)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(20)
				.MaxDefenseBonus(CombatDamageType.Physical, 42)
				.MaxDefenseBonus(CombatDamageType.Force, 42)
				.MaxResistanceBonus(ResistanceType.Fire, 27)
				.MaxResistanceBonus(ResistanceType.Poison, 27)
				.MaxResistanceBonus(ResistanceType.Electrical, 27)
				.MaxResistanceBonus(ResistanceType.Ice, 27)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level31()
		{
			_builder
				.AddLevel()
				.HP(936)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 34)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 11)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(21)
				.MaxDefenseBonus(CombatDamageType.Physical, 43)
				.MaxDefenseBonus(CombatDamageType.Force, 43)
				.MaxResistanceBonus(ResistanceType.Fire, 28)
				.MaxResistanceBonus(ResistanceType.Poison, 28)
				.MaxResistanceBonus(ResistanceType.Electrical, 28)
				.MaxResistanceBonus(ResistanceType.Ice, 28)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level32()
		{
			_builder
				.AddLevel()
				.HP(961)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 26)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 45)
				.MaxDefenseBonus(CombatDamageType.Force, 45)
				.MaxResistanceBonus(ResistanceType.Fire, 29)
				.MaxResistanceBonus(ResistanceType.Poison, 29)
				.MaxResistanceBonus(ResistanceType.Electrical, 29)
				.MaxResistanceBonus(ResistanceType.Ice, 29)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level33()
		{
			_builder
				.AddLevel()
				.HP(986)
				.FP(11)
				.STM(11)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(23)
				.MaxDefenseBonus(CombatDamageType.Physical, 46)
				.MaxDefenseBonus(CombatDamageType.Force, 46)
				.MaxResistanceBonus(ResistanceType.Fire, 30)
				.MaxResistanceBonus(ResistanceType.Poison, 30)
				.MaxResistanceBonus(ResistanceType.Electrical, 30)
				.MaxResistanceBonus(ResistanceType.Ice, 30)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level34()
		{
			_builder
				.AddLevel()
				.HP(1010)
				.FP(12)
				.STM(12)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(3)
				.MaxAccuracyBonus(3)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 47)
				.MaxDefenseBonus(CombatDamageType.Force, 47)
				.MaxResistanceBonus(ResistanceType.Fire, 32)
				.MaxResistanceBonus(ResistanceType.Poison, 32)
				.MaxResistanceBonus(ResistanceType.Electrical, 32)
				.MaxResistanceBonus(ResistanceType.Ice, 32)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level35()
		{
			_builder
				.AddLevel()
				.HP(1035)
				.FP(12)
				.STM(12)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 27)
				.Stat(AbilityType.Perception, 33)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(24)
				.MaxDefenseBonus(CombatDamageType.Physical, 48)
				.MaxDefenseBonus(CombatDamageType.Force, 48)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 33)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level36()
		{
			_builder
				.AddLevel()
				.HP(1060)
				.FP(12)
				.STM(12)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 29)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 15)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(25)
				.MaxDefenseBonus(CombatDamageType.Physical, 50)
				.MaxDefenseBonus(CombatDamageType.Force, 50)
				.MaxResistanceBonus(ResistanceType.Fire, 33)
				.MaxResistanceBonus(ResistanceType.Poison, 33)
				.MaxResistanceBonus(ResistanceType.Electrical, 33)
				.MaxResistanceBonus(ResistanceType.Ice, 33)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 1);
		}

		private void Level37()
		{
			_builder
				.AddLevel()
				.HP(1085)
				.FP(12)
				.STM(12)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 52)
				.MaxDefenseBonus(CombatDamageType.Force, 52)
				.MaxResistanceBonus(ResistanceType.Fire, 34)
				.MaxResistanceBonus(ResistanceType.Poison, 34)
				.MaxResistanceBonus(ResistanceType.Electrical, 34)
				.MaxResistanceBonus(ResistanceType.Ice, 34)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level38()
		{
			_builder
				.AddLevel()
				.HP(1109)
				.FP(12)
				.STM(12)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 35)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 12)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(26)
				.MaxDefenseBonus(CombatDamageType.Physical, 53)
				.MaxDefenseBonus(CombatDamageType.Force, 53)
				.MaxResistanceBonus(ResistanceType.Fire, 35)
				.MaxResistanceBonus(ResistanceType.Poison, 35)
				.MaxResistanceBonus(ResistanceType.Electrical, 35)
				.MaxResistanceBonus(ResistanceType.Ice, 35)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level39()
		{
			_builder
				.AddLevel()
				.HP(1134)
				.FP(14)
				.STM(14)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 28)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 54)
				.MaxDefenseBonus(CombatDamageType.Force, 54)
				.MaxResistanceBonus(ResistanceType.Fire, 36)
				.MaxResistanceBonus(ResistanceType.Poison, 36)
				.MaxResistanceBonus(ResistanceType.Electrical, 36)
				.MaxResistanceBonus(ResistanceType.Ice, 36)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level40()
		{
			_builder
				.AddLevel()
				.HP(1159)
				.FP(14)
				.STM(14)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 34)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(27)
				.MaxDefenseBonus(CombatDamageType.Physical, 55)
				.MaxDefenseBonus(CombatDamageType.Force, 55)
				.MaxResistanceBonus(ResistanceType.Fire, 37)
				.MaxResistanceBonus(ResistanceType.Poison, 37)
				.MaxResistanceBonus(ResistanceType.Electrical, 37)
				.MaxResistanceBonus(ResistanceType.Ice, 37)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 2)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level41()
		{
			_builder
				.AddLevel()
				.HP(1184)
				.FP(14)
				.STM(14)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(28)
				.MaxDefenseBonus(CombatDamageType.Physical, 56)
				.MaxDefenseBonus(CombatDamageType.Force, 56)
				.MaxResistanceBonus(ResistanceType.Fire, 37)
				.MaxResistanceBonus(ResistanceType.Poison, 37)
				.MaxResistanceBonus(ResistanceType.Electrical, 37)
				.MaxResistanceBonus(ResistanceType.Ice, 37)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level42()
		{
			_builder
				.AddLevel()
				.HP(1208)
				.FP(14)
				.STM(14)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 59)
				.MaxDefenseBonus(CombatDamageType.Force, 59)
				.MaxResistanceBonus(ResistanceType.Fire, 38)
				.MaxResistanceBonus(ResistanceType.Poison, 38)
				.MaxResistanceBonus(ResistanceType.Electrical, 38)
				.MaxResistanceBonus(ResistanceType.Ice, 38)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level43()
		{
			_builder
				.AddLevel()
				.HP(1233)
				.FP(14)
				.STM(14)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 29)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(29)
				.MaxDefenseBonus(CombatDamageType.Physical, 60)
				.MaxDefenseBonus(CombatDamageType.Force, 60)
				.MaxResistanceBonus(ResistanceType.Fire, 39)
				.MaxResistanceBonus(ResistanceType.Poison, 39)
				.MaxResistanceBonus(ResistanceType.Electrical, 39)
				.MaxResistanceBonus(ResistanceType.Ice, 39)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level44()
		{
			_builder
				.AddLevel()
				.HP(1258)
				.FP(15)
				.STM(15)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 30)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 16)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(5)
				.MaxAccuracyBonus(5)
				.MaxEvasionBonus(30)
				.MaxDefenseBonus(CombatDamageType.Physical, 61)
				.MaxDefenseBonus(CombatDamageType.Force, 61)
				.MaxResistanceBonus(ResistanceType.Fire, 41)
				.MaxResistanceBonus(ResistanceType.Poison, 41)
				.MaxResistanceBonus(ResistanceType.Electrical, 41)
				.MaxResistanceBonus(ResistanceType.Ice, 41)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level45()
		{
			_builder
				.AddLevel()
				.HP(1283)
				.FP(15)
				.STM(15)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 36)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 14)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 62)
				.MaxDefenseBonus(CombatDamageType.Force, 62)
				.MaxResistanceBonus(ResistanceType.Fire, 42)
				.MaxResistanceBonus(ResistanceType.Poison, 42)
				.MaxResistanceBonus(ResistanceType.Electrical, 42)
				.MaxResistanceBonus(ResistanceType.Ice, 42)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level46()
		{
			_builder
				.AddLevel()
				.HP(1307)
				.FP(15)
				.STM(15)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 35)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(32)
				.MaxDefenseBonus(CombatDamageType.Physical, 63)
				.MaxDefenseBonus(CombatDamageType.Force, 63)
				.MaxResistanceBonus(ResistanceType.Fire, 43)
				.MaxResistanceBonus(ResistanceType.Poison, 43)
				.MaxResistanceBonus(ResistanceType.Electrical, 43)
				.MaxResistanceBonus(ResistanceType.Ice, 43)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level47()
		{
			_builder
				.AddLevel()
				.HP(1332)
				.FP(15)
				.STM(15)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 30)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 65)
				.MaxDefenseBonus(CombatDamageType.Force, 65)
				.MaxResistanceBonus(ResistanceType.Fire, 43)
				.MaxResistanceBonus(ResistanceType.Poison, 43)
				.MaxResistanceBonus(ResistanceType.Electrical, 43)
				.MaxResistanceBonus(ResistanceType.Ice, 43)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level48()
		{
			_builder
				.AddLevel()
				.HP(1357)
				.FP(15)
				.STM(15)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(33)
				.MaxDefenseBonus(CombatDamageType.Physical, 66)
				.MaxDefenseBonus(CombatDamageType.Force, 66)
				.MaxResistanceBonus(ResistanceType.Fire, 44)
				.MaxResistanceBonus(ResistanceType.Poison, 44)
				.MaxResistanceBonus(ResistanceType.Electrical, 44)
				.MaxResistanceBonus(ResistanceType.Ice, 44)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level49()
		{
			_builder
				.AddLevel()
				.HP(1382)
				.FP(16)
				.STM(16)
				.DMG(19)
				.Delay(23)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(34)
				.MaxDefenseBonus(CombatDamageType.Physical, 68)
				.MaxDefenseBonus(CombatDamageType.Force, 68)
				.MaxResistanceBonus(ResistanceType.Fire, 45)
				.MaxResistanceBonus(ResistanceType.Poison, 45)
				.MaxResistanceBonus(ResistanceType.Electrical, 45)
				.MaxResistanceBonus(ResistanceType.Ice, 45)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

		private void Level50()
		{
			_builder
				.AddLevel()
				.HP(1406)
				.FP(16)
				.STM(16)
				.DMG(25)
				.Delay(23)
				.Stat(AbilityType.Might, 32)
				.Stat(AbilityType.Perception, 36)
				.Stat(AbilityType.Vitality, 32)
				.Stat(AbilityType.Willpower, 37)
				.Stat(AbilityType.Agility, 17)
				.Stat(AbilityType.Social, 15)
				.MaxAttackBonus(6)
				.MaxAccuracyBonus(6)
				.MaxEvasionBonus(35)
				.MaxDefenseBonus(CombatDamageType.Physical, 69)
				.MaxDefenseBonus(CombatDamageType.Force, 69)
				.MaxResistanceBonus(ResistanceType.Fire, 46)
				.MaxResistanceBonus(ResistanceType.Poison, 46)
				.MaxResistanceBonus(ResistanceType.Electrical, 46)
				.MaxResistanceBonus(ResistanceType.Ice, 46)
				.MaxResistanceBonus(ResistanceType.Mind, 1)
				.MaxResistanceBonus(ResistanceType.Trauma, 3)
				.MaxResistanceBonus(ResistanceType.Mobility, 2);
		}

    }
}
