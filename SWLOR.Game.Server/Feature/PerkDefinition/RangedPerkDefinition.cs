using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class RangedPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            RapidShot(builder);
            RapidReload(builder);
            ZenMarksmanship(builder);
            PrecisionAim(builder);
            PointBlankShot(builder);
            WeaponFocusPistols(builder);
            ImprovedCriticalPistols(builder);
            PistolProficiency(builder);
            PistolMastery(builder);
            QuickDraw(builder);
            DoubleShot(builder);
            WeaponFocusThrowingWeapons(builder);
            ImprovedCriticalThrowingWeapons(builder);
            ThrowingWeaponProficiency(builder);
            ThrowingWeaponMastery(builder);
            ExplosiveToss(builder);
            PiercingToss(builder);
            WeaponFocusCannons(builder);
            ImprovedCriticalCannons(builder);
            CannonProficiency(builder);
            CannonMastery(builder);
            FullAuto(builder);
            HammerShot(builder);
            WeaponFocusRifles(builder);
            ImprovedCriticalRifles(builder);
            RifleProficiency(builder);
            RifleMastery(builder);
            TranquilizerShot(builder);
            CripplingShot(builder);

            return builder.Build();
        }

        private void RapidShot(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.RapidShot)
                .Name("Rapid Shot")

                .AddPerkLevel()
                .Description("Gain an extra attack per round when a Ranged weapon is equipped. All attacks within the round suffer a -2 penalty.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15);
        }

        private void RapidReload(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.RapidReload)
                .Name("Rapid Reload")

                .AddPerkLevel()
                .Description("You receive the same number of attacks with a cannon or rifle as you would if you were using a pistol.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15);
        }

        private void ZenMarksmanship(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.ZenMarksmanship)
                .Name("Zen Marksmanship")

                .AddPerkLevel()
                .Description("Wisdom guides your ranged attacks. If your WIS modifier is higher than DEX, it will be used when firing ranged weapons.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 25);
        }

        private void PrecisionAim(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.PrecisionAim)
                .Name("Precision Aim")

                .AddPerkLevel()
                .Description("Your attacks deal +0.5 damage per dexterity modifier.")
                .Price(5)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your attacks deal +0.75 damage per dexterity modifier.")
                .Price(6)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void PointBlankShot(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.PointBlankShot)
                .Name("Point Blank Shot")

                .AddPerkLevel()
                .Description("Grants +1 to your attack roll and damage when your target is within 15 feet.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 10);
        }

        private void WeaponFocusPistols(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.WeaponFocusPistols)
                .Name("Weapon Focus - Pistols")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with pistols.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with pistols.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalPistols(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.ImprovedCriticalPistols)
                .Name("Improved Critical - Pistols")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a pistol.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void PistolProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.PistolProficiency)
                .Name("Pistol Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Pistols.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void PistolMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.PistolMastery)
                .Name("Pistol Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Pistol.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Pistol.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Pistol.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void QuickDraw(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.QuickDraw)
                .Name("Quick Draw")

                .AddPerkLevel()
                .Description("Instantly deals 1d8 damage to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)

                .AddPerkLevel()
                .Description("Instantly deals 2d6 damage to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Instantly deals 3d6 damage to your target.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void DoubleShot(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.DoubleShot)
                .Name("Double Shot")

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 1d4 damage.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 2d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 20)

                .AddPerkLevel()
                .Description("Instantly attacks twice, each for 3d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void WeaponFocusThrowingWeapons(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.WeaponFocusThrowingWeapons)
                .Name("Weapon Focus - Throwing Weapons")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with throwing weapons.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with throwing weapons.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalThrowingWeapons(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ImprovedCriticalThrowingWeapons)
                .Name("Improved Critical - Throwing Weapons")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a throwing weapon.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ThrowingWeaponProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ThrowingWeaponProficiency)
                .Name("Throwing Weapon Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Throwing Weapons.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ThrowingWeaponMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ThrowingWeaponMastery)
                .Name("Throwing Weapon Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Throwing Weapon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Throwing Weapon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Throwing Weapon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ExplosiveToss(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ExplosiveToss)
                .Name("Explosive Toss")

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 enemies within 3 meters of your target for 1d4 damage.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 enemies within 3 meters of your target for 1d6 damage.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 enemies within 3 meters of your target for 1d8 damage.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void PiercingToss(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.PiercingToss)
                .Name("Piercing Toss")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d4 damage and has a 50% chance to inflict Bleed for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d4 damage and has a 75% chance to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d4 damage and has a 100% chance to inflict Bleed for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void WeaponFocusCannons(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedCannon, PerkType.WeaponFocusCannons)
                .Name("Weapon Focus - Cannons")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with cannons.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with cannons.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalCannons(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedCannon, PerkType.ImprovedCriticalCannons)
                .Name("Improved Critical - Cannons")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a cannon.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void CannonProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedCannon, PerkType.CannonProficiency)
                .Name("Cannon Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Cannons.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Cannons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Cannons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Cannons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Cannons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void CannonMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedCannon, PerkType.CannonMastery)
                .Name("Cannon Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Cannon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Cannon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Cannon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void FullAuto(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedCannon, PerkType.FullAuto)
                .Name("Full Auto")

                .AddPerkLevel()
                .Description("Attacks up to 3 enemies in front of you for 1d8 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)

                .AddPerkLevel()
                .Description("Attacks up to 3 enemies in front of you for 2d6 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Attacks up to 3 enemies in front of you for 3d6 of damage each.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void HammerShot(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedCannon, PerkType.HammerShot)
                .Name("Hammer Shot")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d6 damage and has a 50% chance to inflict Breach for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d6 damage and has a 75% chance to inflict Breach for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 20)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d6 damage and has a 100% chance to inflict Breach for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void WeaponFocusRifles(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.WeaponFocusRifles)
                .Name("Weapon Focus - Rifles")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with rifles.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with rifles.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void ImprovedCriticalRifles(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.ImprovedCriticalRifles)
                .Name("Improved Critical - Rifles")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a rifles.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void RifleProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.RifleProficiency)
                .Name("Rifle Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rifles.")
                .Price(2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void RifleMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.RifleMastery)
                .Name("Rifle Mastery")

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void TranquilizerShot(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot")

                .AddPerkLevel()
                .Description("Your next attack will tranquilize your target for up to 12 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)

                .AddPerkLevel()
                .Description("Your next attack will tranquilize your target for up to 24 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack will tranquilize all creatures within 5 meters of your target for up to 12 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard);
        }

        private void CripplingShot(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.CripplingShot)
                .Name("Crippling Shot")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 1d6 damage and has a 50% chance to inflict Bind for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 5)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2d6 damage and has a 75% chance to inflict Bind for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 3d6 damage and has a 100% chance to inflict Bind for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard);
        }
    }
}
