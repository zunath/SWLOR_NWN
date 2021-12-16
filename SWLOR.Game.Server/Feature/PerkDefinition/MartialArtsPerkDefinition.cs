using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Item = SWLOR.Game.Server.Service.Item;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class MartialArtsPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Knockdown(builder);
            FlurryOfBlows(builder);
            InnerStrength(builder);
            MartialFinesse(builder);
            WeaponFocusKatars(builder);
            ImprovedCriticalKatars(builder);
            KatarProficiency(builder);
            KatarMastery(builder);
            ElectricFist(builder);
            StrikingCobra(builder);
            WeaponFocusStaves(builder);
            ImprovedCriticalStaves(builder);
            StaffProficiency(builder);
            StaffMastery(builder);
            Slam(builder);
            SpinningWhirl(builder);

            return builder.Build();
        }

        private void Knockdown(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.Knockdown)
                .Name("Knockdown")

                .AddPerkLevel()
                .Description("Your next attack has a 60% chance to inflict knockdown on your target for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.Knockdown);
        }

        private void FlurryOfBlows(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.FlurryOfBlows)
                .Name("Flurry of Blows")

                .AddPerkLevel()
                .Description("Grants the Flurry of Blows feat. You receive an extra attack per round when fighting with unarmed attacks or a staff. However, all attacks in that round suffer a -2 attack penalty.")
                .Price(4)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlurryOfBlows);
        }

        private void InnerStrength(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.InnerStrength)
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

        private void MartialFinesse(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsGeneral, PerkType.MartialFinesse)
                .Name("Martial Finesse")

                .AddPerkLevel()
                .Description("You make melee attack rolls with your PER score if it is higher than your MGT score.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 10)
                .GrantsFeat(FeatType.MartialFinesse);
        }

        private void WeaponFocusKatars(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.WeaponFocusKatars)
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
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WeaponSpecializationKatars);
        }

        private void ImprovedCriticalKatars(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ImprovedCriticalKatars)
                .Name("Improved Critical - Katars")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using katars.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ImprovedCriticalKatars);
        }

        private void KatarProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.KatarProficiency)
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

        private void KatarMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.KatarMastery)
                .Name("Katar Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + level;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + 1;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.KatarBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Katars.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KatarMastery1)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Katars.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KatarMastery2)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Katars.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KatarMastery3);
        }

        private void ElectricFist(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.ElectricFist)
                .Name("Electric Fist")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4.0 DMG and has a 50% chance to inflict Shock for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.ElectricFist1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6.0 DMG and has a 75% chance to inflict Shock for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 9.5 DMG and has a 100% chance to inflict Shock for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ElectricFist3);
        }

        private void StrikingCobra(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsKatars, PerkType.StrikingCobra)
                .Name("Striking Cobra")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4.5 DMG and has a 50% chance to inflict Poison for 30 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.StrikingCobra1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 6.5 DMG and has a 75% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(FeatType.StrikingCobra2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 10.0 DMG and has a 100% chance to inflict Poison for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StrikingCobra3);
        }

        private void WeaponFocusStaves(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.WeaponFocusStaves)
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
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WeaponSpecializationStaves);
        }

        private void ImprovedCriticalStaves(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.ImprovedCriticalStaves)
                .Name("Improved Critical - Staves")

                .AddPerkLevel()
                .Description("Improves the critical hit chance when using a staff.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ImprovedCriticalStaff);
        }

        private void StaffProficiency(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.StaffProficiency)
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

        private void StaffMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.StaffMastery)
                .Name("Staff Mastery")
                .TriggerEquippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + level;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerUnequippedItem((player, item, slot, type, level) =>
                {
                    if (slot != InventorySlot.RightHand) return;

                    var itemType = GetBaseItemType(item);
                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }

                })
                .TriggerPurchase((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) + 1;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })
                .TriggerRefund((player, type, level) =>
                {
                    var item = GetItemInSlot(InventorySlot.RightHand, player);
                    var itemType = GetBaseItemType(item);

                    if (Item.StaffBaseItemTypes.Contains(itemType))
                    {
                        var bab = GetBaseAttackBonus(player) - level;
                        CreaturePlugin.SetBaseAttackBonus(player, bab);
                    }
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StaffMastery1)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StaffMastery2)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a Staff.")
                .Price(8)
                .RequirementSkill(SkillType.MartialArts, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StaffMastery3);
        }

        private void Slam(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.Slam)
                .Name("Slam")

                .AddPerkLevel()
                .Description("Your next attack deals an additional 2.0 DMG and has a 50% chance to inflict Blindness for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 15)
                .GrantsFeat(FeatType.Slam1)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 4.5 DMG and has a 75% chance to inflict Blindness for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam2)

                .AddPerkLevel()
                .Description("Your next attack deals an additional 7.0 DMG and has a 100% chance to inflict Blindness for 1 minute.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Slam3);
        }

        private void SpinningWhirl(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.MartialArtsStaff, PerkType.SpinningWhirl)
                .Name("Spinning Whirl")

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 2.0 DMG each.")
                .Price(2)
                .RequirementSkill(SkillType.MartialArts, 5)
                .GrantsFeat(FeatType.SpinningWhirl1)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 4.5 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 20)
                .GrantsFeat(FeatType.SpinningWhirl2)

                .AddPerkLevel()
                .Description("Attacks up to 3 nearby enemies for 7.0 DMG each.")
                .Price(3)
                .RequirementSkill(SkillType.MartialArts, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SpinningWhirl3);
        }
    }
}
