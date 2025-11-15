using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class MartialArtsPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new PerkBuilder();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Knockdown();
            Furor();
            InnerStrength();
            Chi();
            WeaponFocusKatars();
            ImprovedCriticalKatars();
            KatarProficiency();
            KatarMastery();
            ElectricFist();
            StrikingCobra();
            WeaponFocusStaves();
            ImprovedCriticalStaves();
            StaffProficiency();
            StaffMastery();
            Slam();
            LegSweep();
            FlurryStyle();
            CrushingStyle();

            return _builder.Build();
        }

        private void Knockdown()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Knockdown)
                .Name("Knockdown")

                .AddPerkLevel()
                .Description("Your next attack has a 15DC fortitude check to inflict knockdown on your target for 4 seconds. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.Knockdown);
        }

        private void Furor()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Furor)
                .Name("Furor")

                .AddPerkLevel()
                .Description("Grants an additional attack to the user for one minute. [Cross Skill]")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Furor);
        }

        private void InnerStrength()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.InnerStrength)
                .Name("Inner Strength")

                .AddPerkLevel()
                .Description("Improves critical chance by 5%. [Cross Skill]")
                .Price(5)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.InnerStrength1)

                .AddPerkLevel()
                .Description("Improves critical chance by 10%. [Cross Skill]")
                .Price(6)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.InnerStrength2)

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
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyCritModifier(player, item);
                });
        }

        private void Chi()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Chi)
                .Name("Chi")

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 45 points. [Cross Skill]")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.Chi1)

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 115 points. [Cross Skill]")
                .Price(4)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Chi2)

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 170 points. [Cross Skill]")
                .Price(4)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Chi3);
        }

        private void WeaponFocusKatars()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.WeaponFocusKatars)
                .Name("Weapon Focus - Katars")

                .AddPerkLevel()
                .Description("Your accuracy with katars is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.WeaponFocusKatars)

                .AddPerkLevel()
                .Description("Your base damage with katars is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.WeaponSpecializationKatars);
        }

        private void ImprovedCriticalKatars()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ImprovedCriticalKatars)
                .Name("Improved Critical - Katars")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with katars by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .GrantsFeat(FeatType.ImprovedCriticalKatars);
        }

        private void KatarProficiency()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.KatarProficiency)
                .Name("Katar Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Katars.")
                .Price(2)
                .GrantsFeat(FeatType.KatarProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Katars.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 10)
                .GrantsFeat(FeatType.KatarProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Katars.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(FeatType.KatarProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Katars.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 30)
                .GrantsFeat(FeatType.KatarProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Katars.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 40)
                .GrantsFeat(FeatType.KatarProficiency5);
        }

        private void KatarMastery()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.KatarMastery)
                .Name("Katar Mastery")
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
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Katars.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 25)
                .GrantsFeat(FeatType.KatarMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Katars.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 50)
                .GrantsFeat(FeatType.KatarMastery2);
        }

        private void ElectricFist()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ElectricFist)
                .Name("Electric Fist")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 8 DMG and has a 10DC reflex check to inflict Shock for 30 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 17 DMG and has a 15DC reflex check to inflict Shock for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 24 DMG and has a 20DC reflex check to inflict Shock for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist3);
        }

        private void StrikingCobra()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.StrikingCobra)
                .Name("Striking Cobra")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a 10DC reflex check to inflict Poison for 30 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a 15DC reflex check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a 20DC reflex check to inflict Poison for 1 minute.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra3);
        }

        private void WeaponFocusStaves()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.WeaponFocusStaves)
                .Name("Weapon Focus - Staves")

                .AddPerkLevel()
                .Description("Your accuracy with staves is increased by 5.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.WeaponFocusStaves)

                .AddPerkLevel()
                .Description("Your base damage with staves is increased by 2 DMG.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.WeaponSpecializationStaves);
        }

        private void ImprovedCriticalStaves()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.ImprovedCriticalStaves)
                .Name("Improved Critical - Staves")

                .AddPerkLevel()
                .Description("Improves the chance to critically hit with staves by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .GrantsFeat(FeatType.ImprovedCriticalStaff);
        }

        private void StaffProficiency()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.StaffProficiency)
                .Name("Staff Proficiency")

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 1 Staves.")
                .Price(2)
                .GrantsFeat(FeatType.StaffProficiency1)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 2 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 10)
                .GrantsFeat(FeatType.StaffProficiency2)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 3 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(FeatType.StaffProficiency3)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 4 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 30)
                .GrantsFeat(FeatType.StaffProficiency4)

                .AddPerkLevel()
                .Description("Grants the ability to equip tier 5 Staves.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 40)
                .GrantsFeat(FeatType.StaffProficiency5);
        }

        private void StaffMastery()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.StaffMastery)
                .Name("Staff Mastery")
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
                .TriggerPurchase((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })
                .TriggerRefund((player) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    Stat.ApplyAttacksPerRound(player, item);
                })

                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 25)
                .GrantsFeat(FeatType.StaffMastery1)
                
                .AddPerkLevel()
                .Description("Grants an additional attack when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 50)
                .GrantsFeat(FeatType.StaffMastery2);
        }

        private void Slam()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.Slam)
                .Name("Slam")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a 10DC fortitude check to inflict Blindness for 12 seconds.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a 15DC fortitude check to inflict Blindness for 12 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a 20DC fortitude check to inflict Blindness for 12 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam3);
        }

        private void LegSweep()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.LegSweep)
                .Name("Leg Sweep")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4 DMG and has a 5DC reflex check to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 13 DMG and has a 8DC reflex check to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 20 DMG and has a 10DC reflex check to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep3);
        }

        private void FlurryStyle()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.FlurryStyle)
                .Name("Flurry Style")
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

                .AddPerkLevel()
                .Description("Your staff attacks now use Agility for accuracy and Perception for damage. In addition, you gain an additional attack with staves, but all staff attacks are made with a -10% to-hit penalty.")
                .Price(1)
                .RequirementCannotHavePerk(PerkType.CrushingStyle)
                .GrantsFeat(FeatType.FlurryStyle)

                .AddPerkLevel()
                .Description("You gain an additional attack with staves, and no longer suffer a to-hit penalty for attacks made with staves.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .GrantsFeat(FeatType.FlurryMastery);
        }
        private void CrushingStyle()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.CrushingStyle)
                .Name("Crushing Style")

                .AddPerkLevel()
                .Description("Your attacks with a staff now gain a DMG bonus equal to your MGT modifier. In addition, your critical chance is raised by 15%.")
                .Price(1)
                .RequirementCannotHavePerk(PerkType.FlurryStyle)
                .GrantsFeat(FeatType.CrushingStyle)

                .AddPerkLevel()
                .Description("Your MGT DMG bonus is increased to twice your MGT modifier, and critical chance is increased by a further 15%.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 35)
                .GrantsFeat(FeatType.CrushingMastery);
        }

    }
}
