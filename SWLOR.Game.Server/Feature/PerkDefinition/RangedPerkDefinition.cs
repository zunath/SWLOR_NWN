using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class RangedPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            RapidShot();
            DirtyBlow();
            RapidReload();
            PrecisionAim();
            PointBlankShot();
            WeaponFocusPistols();
            ImprovedCriticalPistols();
            PistolProficiency();
            PistolMastery();
            QuickDraw();
            DoubleShot();
            WeaponFocusThrowingWeapons();
            ImprovedCriticalThrowingWeapons();
            ThrowingWeaponProficiency();
            ThrowingWeaponMastery();
            ExplosiveToss();
            PiercingToss();
            WeaponFocusRifles();
            ImprovedCriticalRifles();
            RifleProficiency();
            RifleMastery();
            TranquilizerShot();
            CripplingShot();
            ZenMarksmanship();

            return _builder.Build();
        }

        private void RapidShot()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.RapidShot)
                .Name("Rapid Shot")

                .AddPerkLevel()
                .Description("Grants an additional attack with pistols.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)

                .AddPerkLevel()
                .Description("Grants an additional attack with pistols, for a total of two attacks.")
                .Price(5)
                .RequirementSkill(SkillType.Ranged, 40)
                
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                });
        }

        private void DirtyBlow()
        {
            _builder.Create(PerkCategoryType.RangedGeneral, PerkType.DirtyBlow)
                .Name("Dirty Blow")

                .AddPerkLevel()
                .Description("While equipped with a pistol or shurikens, your critical chance increases by 10%.")
                .Price(4)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.DirtyBlow)

                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyCritModifier(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyCritModifier(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                });


        }

        private void RapidReload()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.RapidReload)
                .Name("Rapid Reload")

                .AddPerkLevel()
                .Description("Rifles can now gain additional attacks per round (via Rifle Mastery). While equipped with a rifle, critical damage is increased by 50%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 15)
                .GrantsFeat(FeatType.RapidReload);
        }

        private void PrecisionAim()
        {
            _builder.Create(PerkCategoryType.RangedGeneral, PerkType.PrecisionAim)
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

        private void PointBlankShot()
        {
            _builder.Create(PerkCategoryType.RangedGeneral, PerkType.PointBlankShot)
                .Name("Point Blank Shot")

                .AddPerkLevel()
                .Description("While a target is within 5 meters, you gain +5 accuracy and +1 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 5)
                .GrantsFeat(FeatType.PointBlankShot);
        }

        private void WeaponFocusPistols()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.WeaponFocusPistols)
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

        private void ImprovedCriticalPistols()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.ImprovedCriticalPistols)
                .Name("Improved Critical - Pistols")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with pistols by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ImprovedCriticalPistol);
        }

        private void PistolProficiency()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.PistolProficiency)
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

        private void PistolMastery()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.PistolMastery)
                .Name("Pistol Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Pistol.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.PistolMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Pistol.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .GrantsFeat(FeatType.PistolMastery2);
        }

        private void QuickDraw()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.QuickDraw)
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

        private void DoubleShot()
        {
            _builder.Create(PerkCategoryType.RangedPistol, PerkType.DoubleShot)
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

        private void WeaponFocusThrowingWeapons()
        {
            _builder.Create(PerkCategoryType.RangedThrowing, PerkType.WeaponFocusThrowingWeapons)
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

        private void ImprovedCriticalThrowingWeapons()
        {
            _builder.Create(PerkCategoryType.RangedThrowing, PerkType.ImprovedCriticalThrowingWeapons)
                .Name("Improved Critical - Throwing Weapons")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with throwing weapons by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ImprovedCriticalThrowingWeapons);
        }

        private void ThrowingWeaponProficiency()
        {
            _builder.Create(PerkCategoryType.RangedThrowing, PerkType.ThrowingWeaponProficiency)
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

        private void ThrowingWeaponMastery()
        {
            _builder.Create(PerkCategoryType.RangedThrowing, PerkType.ThrowingWeaponMastery)
                .Name("Throwing Weapon Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Throwing Weapon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ThrowingWeaponMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Throwing Weapon.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .GrantsFeat(FeatType.ThrowingWeaponMastery2);
        }

        private void ExplosiveToss()
        {
            _builder.Create(PerkCategoryType.RangedThrowing, PerkType.ExplosiveToss)
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

        private void PiercingToss()
        {
            _builder.Create(PerkCategoryType.RangedThrowing, PerkType.PiercingToss)
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
        
        private void WeaponFocusRifles()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.WeaponFocusRifles)
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

        private void ImprovedCriticalRifles()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.ImprovedCriticalRifles)
                .Name("Improved Critical - Rifles")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with rifles by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Ranged, 25)
                .GrantsFeat(FeatType.ImprovedCriticalRifles);
        }

        private void RifleProficiency()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.RifleProficiency)
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

        private void RifleMastery()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.RifleMastery)
                .Name("Rifle Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    Stat.ApplyAttacksPerRound(player, OBJECT_INVALID);
                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 25)
                .RequirementMustHavePerk(PerkType.RapidReload)
                .GrantsFeat(FeatType.RifleMastery1)

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Ranged, 50)
                .GrantsFeat(FeatType.RifleMastery2);
        }

        private void TranquilizerShot()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.TranquilizerShot)
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

        private void CripplingShot()
        {
            _builder.Create(PerkCategoryType.RangedRifle, PerkType.CripplingShot)
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

        private void ZenMarksmanship()
        {
            _builder.Create(PerkCategoryType.RangedGeneral, PerkType.ZenMarksmanship)
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
