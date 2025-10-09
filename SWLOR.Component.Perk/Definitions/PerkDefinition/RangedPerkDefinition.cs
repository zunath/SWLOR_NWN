using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Definitions.PerkDefinition
{
    public class RangedPerkDefinition : IPerkListDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public RangedPerkDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency

        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            RapidReload(builder);
            PrecisionAim(builder);
            PointBlankShot(builder);
            WeaponFocusPistols(builder);
            ImprovedCriticalPistols(builder);
            PistolProficiency(builder);
            QuickDraw(builder);
            DoubleShot(builder);
            WeaponFocusThrowingWeapons(builder);
            ImprovedCriticalThrowingWeapons(builder);
            ThrowingWeaponProficiency(builder);
            ExplosiveToss(builder);
            PiercingToss(builder);
            WeaponFocusRifles(builder);
            ImprovedCriticalRifles(builder);
            RifleProficiency(builder);
            TranquilizerShot(builder);
            CripplingShot(builder);
            ZenMarksmanship(builder);

            return builder.Build();
        }

        private void RapidReload(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.RapidReload)
                .Name("Rapid Reload")

                .AddPerkLevel()
                .Description("Rifles can now gain additional attacks per round (via Rifle Mastery). While equipped with a rifle, critical damage is increased by 50%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)
                .GrantsFeat(FeatType.RapidReload);
        }

        private void PrecisionAim(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.PrecisionAim)
                .Name("Precision Aim")

                .AddPerkLevel()
                .Description("Improves critical chance by 2%. [Cross Skill]")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PrecisionAim1)

                .AddPerkLevel()
                .Description("Improves critical chance by 4%. [Cross Skill]")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PrecisionAim2);
        }

        private void PointBlankShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.PointBlankShot)
                .Name("Point Blank Shot")

                .AddPerkLevel()
                .Description("While a target is within 5 meters, you gain +5 accuracy and +1 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .GrantsFeat(FeatType.PointBlankShot);
        }

        private void WeaponFocusPistols(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.WeaponFocusPistols)
                .Name("Weapon Focus - Pistols")

                .AddPerkLevel()
                .Description("Your accuracy with pistols is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .GrantsFeat(FeatType.WeaponFocusPistol)

                .AddPerkLevel()
                .Description("Your base damage with damage is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .GrantsFeat(FeatType.WeaponSpecializationPistol);
        }

        private void ImprovedCriticalPistols(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.ImprovedCriticalPistols)
                .Name("Improved Critical - Pistols")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with pistols by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ImprovedCriticalPistol);
        }

        private void PistolProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.PistolProficiency)
                .Name("Pistol Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Pistols.")
                .Price(2)
                .GrantsFeat(FeatType.PistolProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)
                .GrantsFeat(FeatType.PistolProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)
                .GrantsFeat(FeatType.PistolProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .GrantsFeat(FeatType.PistolProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Pistols.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .GrantsFeat(FeatType.PistolProficiency5);
        }

        private void QuickDraw(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.QuickDraw)
                .Name("Quick Draw")

                .AddPerkLevel()
                .Description("Instantly deals 10 DMG to your target.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.QuickDraw1)

                .AddPerkLevel()
                .Description("Instantly deals 20 DMG to your target.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.QuickDraw2)

                .AddPerkLevel()
                .Description("Instantly deals 30 DMG to your target.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.QuickDraw3);
        }

        private void DoubleShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedPistol, PerkType.DoubleShot)
                .Name("Double Shot")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 x 2 DMG.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleShot1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 18 x 2 DMG.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleShot2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 28 x 2 DMG.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DoubleShot3);
        }

        private void WeaponFocusThrowingWeapons(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.WeaponFocusThrowingWeapons)
                .Name("Weapon Focus - Throwing Weapons")

                .AddPerkLevel()
                .Description("Your accuracy with throwing weapons is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .GrantsFeat(FeatType.WeaponFocusThrowingWeapons)

                .AddPerkLevel()
                .Description("Your base damage with throwing weapons is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .GrantsFeat(FeatType.WeaponSpecializationThrowingWeapons);
        }

        private void ImprovedCriticalThrowingWeapons(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ImprovedCriticalThrowingWeapons)
                .Name("Improved Critical - Throwing Weapons")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with throwing weapons by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ImprovedCriticalThrowingWeapons);
        }

        private void ThrowingWeaponProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ThrowingWeaponProficiency)
                .Name("Throwing Weapon Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Throwing Weapons.")
                .Price(2)
                .GrantsFeat(FeatType.ThrowingWeaponProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)
                .GrantsFeat(FeatType.ThrowingWeaponProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)
                .GrantsFeat(FeatType.ThrowingWeaponProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .GrantsFeat(FeatType.ThrowingWeaponProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Throwing Weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .GrantsFeat(FeatType.ThrowingWeaponProficiency5);
        }

        private void ExplosiveToss(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.ExplosiveToss)
                .Name("Explosive Toss")

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for 8 DMG.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ExplosiveToss1)

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for 16 DMG.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ExplosiveToss2)

                .AddPerkLevel()
                .Description("Your next attack damages up to 3 creatures within 3 meters of your target for 26 DMG.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ExplosiveToss3);
        }

        private void PiercingToss(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedThrowing, PerkType.PiercingToss)
                .Name("Piercing Toss")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a 10DC reflex check to inflict Bleed for 30 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PiercingToss1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a 15DC reflex check to inflict Bleed for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PiercingToss2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a 20DC reflex check to inflict Bleed for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PiercingToss3);
        }
        
        private void WeaponFocusRifles(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.WeaponFocusRifles)
                .Name("Weapon Focus - Rifles")

                .AddPerkLevel()
                .Description("Your accuracy with rifles is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .GrantsFeat(FeatType.WeaponFocusRifles)

                .AddPerkLevel()
                .Description("Your base damage with rifles is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.Ranged, 15)
                .GrantsFeat(FeatType.WeaponSpecializationRifles);
        }

        private void ImprovedCriticalRifles(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.ImprovedCriticalRifles)
                .Name("Improved Critical - Rifles")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with rifles by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ImprovedCriticalRifles);
        }

        private void RifleProficiency(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.RifleProficiency)
                .Name("Rifle Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Rifles.")
                .Price(2)
                .GrantsFeat(FeatType.RifleProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 10)
                .GrantsFeat(FeatType.RifleProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 20)
                .GrantsFeat(FeatType.RifleProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 30)
                .GrantsFeat(FeatType.RifleProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Rifles.")
                .Price(2)
                .RequirementSkill(SkillType.Ranged, 40)
                .GrantsFeat(FeatType.RifleProficiency5);
        }

        private void TranquilizerShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.TranquilizerShot)
                .Name("Tranquilizer Shot")

                .AddPerkLevel()
                .Description("Your next attack will tranquilize your target for up to 12 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TranquilizerShot1)

                .AddPerkLevel()
                .Description("Your next attack will tranquilize your target for up to 24 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TranquilizerShot2)

                .AddPerkLevel()
                .Description("Your next attack will tranquilize up to three creatures in a cone for up to 12 seconds. Damage will break the effect prematurely.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TranquilizerShot3);
        }

        private void CripplingShot(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedRifle, PerkType.CripplingShot)
                .Name("Crippling Shot")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 12 DMG and has a 10DC reflex check to inflict Bind for 6 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CripplingShot1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 21 DMG and has a 15DC reflex check to inflict Bind for 6 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.Ranged, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CripplingShot2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 34 DMG and has a 20DC reflex check to inflict Bind for 6 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.Ranged, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CripplingShot3);
        }

        private void ZenMarksmanship(IPerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RangedGeneral, PerkType.ZenMarksmanship)
                .Name("Zen Marksmanship")

                .AddPerkLevel()
                .Description("Your ranged attacks now use Willpower for damage if your Willpower is higher than your weapon's damage stat.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ZenMarksmanship);
        }
    }
}

