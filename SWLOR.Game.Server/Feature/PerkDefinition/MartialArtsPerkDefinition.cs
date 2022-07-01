using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using Item = SWLOR.Game.Server.Service.Item;

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

            return _builder.Build();
        }

        private void Knockdown()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Knockdown)
                .Name("Knockdown")

                .AddPerkLevel()
                .Description("Your next attack has a 60% chance to inflict knockdown on your target for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.Knockdown);
        }

        private void Furor()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Furor)
                .Name("Furor")

                .AddPerkLevel()
                .Description("Grants an additional attack to the user for one minute.")
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
                .Description("Improves critical range by 1.")
                .Price(5)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.InnerStrength1)

                .AddPerkLevel()
                .Description("Improves critical range by 2.")
                .Price(6)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.InnerStrength2)

                .TriggerPurchase((player, type, level) =>
                {
                    CreaturePlugin.SetCriticalRangeModifier(player, -level, 0, true, BaseItem.Gloves);
                })
                .TriggerRefund((player, type, level) =>
                {
                    CreaturePlugin.SetCriticalRangeModifier(player, 0, 0, true, BaseItem.Gloves);
                });
        }

        private void Chi()
        {
            _builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Chi)
                .Name("Chi")

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 45 points.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.Chi1)

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 115 points.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Chi2)

                .AddPerkLevel()
                .Description("Restores your HP by a base amount of 170 points.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Chi3);
        }

        private void WeaponFocusKatars()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.WeaponFocusKatars)
                .Name("Weapon Focus - Katars")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with katars.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.WeaponFocusKatars)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with katars.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.WeaponSpecializationKatars);
        }

        private void ImprovedCriticalKatars()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ImprovedCriticalKatars)
                .Name("Improved Critical - Katars")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using katars.")
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

                    var itemType = GetBaseItemType(item);
                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }
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
                .Description("Your next attack deals an additional 8 DMG and has a 50% chance to inflict Shock for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 17 DMG and has a 75% chance to inflict Shock for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 24 DMG and has a 100% chance to inflict Shock for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist3);
        }

        private void StrikingCobra()
        {
            _builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.StrikingCobra)
                .Name("Striking Cobra")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6 DMG and has a 50% chance to inflict Poison for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a 75% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a 100% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra3);
        }

        private void WeaponFocusStaves()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.WeaponFocusStaves)
                .Name("Weapon Focus - Staves")

                .AddPerkLevel()
                .Description("You gain the Weapon Focus feat which grants a +1 attack bonus when equipped with staves.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.WeaponFocusStaves)

                .AddPerkLevel()
                .Description("You gain the Weapon Specialization feat which grants a +2 damage when equipped with staves.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.WeaponSpecializationStaves);
        }

        private void ImprovedCriticalStaves()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.ImprovedCriticalStaves)
                .Name("Improved Critical - Staves")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a staff.")
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

                    var itemType = GetBaseItemType(item);
                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = level == 1 ? 6 : 11;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        CreaturePlugin.SetBaseAttackBonus(player, 1);
                    }
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
                .Description("Your next attack deals an additional 6 DMG and has a 50% chance to inflict Blindness for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 15 DMG and has a 75% chance to inflict Blindness for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 22 DMG and has a 100% chance to inflict Blindness for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam3);
        }

        private void LegSweep()
        {
            _builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.LegSweep)
                .Name("Leg Sweep")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4 DMG and has a 25% chance to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 13 DMG and has a 40% chance to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 20 DMG and has a 50% chance to inflict knockdown on your target for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.LegSweep3);
        }

    }
}
