using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class TwoHandedPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            PowerAttack(builder);
            SuperiorWeaponFocus(builder);
            IncreasedMultiplier(builder);
            Cleave(builder);
            WeaponFocusHeavyVibroblades(builder);
            ImprovedCriticalHeavyVibroblades(builder);
            HeavyVibrobladeProficiency(builder);
            HeavyVibrobladeMastery(builder);
            CrescentMoon(builder);
            HardSlash(builder);
            WeaponFocusPolearms(builder);
            ImprovedCriticalPolearms(builder);
            PolearmProficiency(builder);
            PolearmMastery(builder);
            Skewer(builder);
            DoubleThrust(builder);
            WeaponFocusTwinBlades(builder);
            ImprovedCriticalTwinBlades(builder);
            TwinBladeProficiency(builder);
            TwinBladeMastery(builder);
            LegSweep(builder);
            CrossCut(builder);
            WeaponFocusSaberstaffs(builder);
            ImprovedCriticalSaberstaffs(builder);
            SaberstaffProficiency(builder);
            SaberstaffMastery(builder);
            CircleSlash(builder);
            DoubleStrike(builder);

            return builder.Build();
        }

        private void PowerAttack(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.PowerAttack)
                .Name("Power Attack")

                .AddPerkLevel()
                .Description("Grants the Power Attack feat which grants a +5 bonus to damage roll at the cost of -5 to attack roll.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)

                .AddPerkLevel()
                .Description("Grants the Improved Power Attack feat which grants a +10 bonus to damage roll at the cost of -10 to attack roll.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void SuperiorWeaponFocus(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.SuperiorWeaponFocus)
                .Name("Superior Weapon Focus")

                .AddPerkLevel()
                .Description("Two-Handed weapons gain +1 bonus to all attack rolls.")
                .Price(5)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void IncreasedMultiplier(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.IncreasedMultiplier)
                .Name("Increased Multiplier")

                .AddPerkLevel()
                .Description("Two-Handed weapons gain x1 to all critical hits.")
                .Price(6)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void Cleave(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedGeneral, PerkType.Cleave)
                .Name("Cleave")

                .AddPerkLevel()
                .Description("Grants a free attack against any opponent who is within melee range when an enemy is killed.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 10);
        }

        private void WeaponFocusHeavyVibroblades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.WeaponFocusHeavyVibroblades)
                .Name("Weapon Focus - Heavy Vibroblades")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with heavy vibroblades.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with heavy vibroblades.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalHeavyVibroblades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.ImprovedCriticalHeavyVibroblades)
                .Name("Improved Critical - Heavy Vibroblades")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a heavy vibroblade.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void HeavyVibrobladeProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HeavyVibrobladeProficiency)
                .Name("Heavy Vibroblade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Heavy Vibroblades.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Heavy Vibroblades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void HeavyVibrobladeMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HeavyVibrobladeMastery)
                .Name("Heavy Vibroblade Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Heavy Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Heavy Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Heavy Vibroblade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void CrescentMoon(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.CrescentMoon)
                .Name("Crescent Moon")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d4 damage and inflicts stun for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d4 damage and inflicts stun for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4d4 damage and inflicts stun for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void HardSlash(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedHeavyVibroblade, PerkType.HardSlash)
                .Name("Hard Slash")

                .AddPerkLevel()
                .Description("Instantly deals 1d12 damage to your target.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .Description("Instantly deals 2d8 damage to your target.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20)

                .AddPerkLevel()
                .Description("Instantly deals 3d8 damage to your target.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void WeaponFocusPolearms(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.WeaponFocusPolearms)
                .Name("Weapon Focus - Polearms")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with polearms.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with polearms.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalPolearms(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.ImprovedCriticalPolearms)
                .Name("Improved Critical - Polearms")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a polearm.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void PolearmProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.PolearmProficiency)
                .Name("Polearm Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 polearms.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 polearms.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void PolearmMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.PolearmMastery)
                .Name("Polearm Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Polearm.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Polearm.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Polearm.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void Skewer(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.Skewer)
                .Name("Skewer")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d6 damage and has a 45% chance to interrupt the concentration of your target.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d6 damage and has a 75% chance to interrupt the concentration of your target.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d6 damage and has a 100% chance to interrupt the concentration of your target.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void DoubleThrust(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedPolearm, PerkType.DoubleThrust)
                .Name("Double Thrust")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 1d4 damage.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 2d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 3d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void WeaponFocusTwinBlades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.WeaponFocusTwinBlades)
                .Name("Weapon Focus - Twin Blades")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with twin blades.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with twin blades.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalTwinBlades(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.ImprovedCriticalTwinBlades)
                .Name("Improved Critical - Twin Blades")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a twin blades.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void TwinBladeProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.TwinBladeProficiency)
                .Name("Twin Blade Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 twin blades.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 twin blades.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void TwinBladeMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.TwinBladeMastery)
                .Name("Twin Blade Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Twin Blade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Twin Blade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Twin Blade.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void LegSweep(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.LegSweep)
                .Name("Leg Sweep")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d8 damage and has a 25% chance to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d8 damage and has a 40% chance to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d8 damage and has a 50% chance to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void CrossCut(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedTwinBlade, PerkType.CrossCut)
                .Name("Cross Cut")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 1d4. Successful hits reduce your target's AC by 2 for 1 minute.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 2d4. Successful hits reduce your target's AC by 4 for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 3d4. Successful hits reduce your target's AC by 6 for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void WeaponFocusSaberstaffs(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.WeaponFocusSaberstaffs)
                .Name("Weapon Focus - Saberstaffs")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with saberstaffs.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with saberstaffs.")
                .Price(4)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ImprovedCriticalSaberstaffs(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.ImprovedCriticalSaberstaffs)
                .Name("Improved Critical - Saberstaffs")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a saberstaff.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SaberstaffProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.SaberstaffProficiency)
                .Name("Saberstaff Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Saberstaffs.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Saberstaffs.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void SaberstaffMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.SaberstaffMastery)
                .Name("Saberstaff Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Saberstaff.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Saberstaff.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Saberstaff.")
                .Price(8)
                .RequirementSkill(SkillType.TwoHanded, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void CircleSlash(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.CircleSlash)
                .Name("Circle Slash")

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 1d8 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 2d6 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 3d6 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void DoubleStrike(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.TwoHandedSaberstaff, PerkType.DoubleStrike)
                .Name("Double Strike")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 1d4 damage.")
                .Price(2)
                .RequirementSkill(SkillType.TwoHanded, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 2d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 3d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.TwoHanded, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }
    }
}
