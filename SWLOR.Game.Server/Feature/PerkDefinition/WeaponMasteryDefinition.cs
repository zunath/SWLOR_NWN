using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class WeaponMasteryDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            LongswordMastery(builder);
            KnucklesMastery(builder);
            DaggerMastery(builder);
            StaffMastery(builder);
            RodMastery(builder);
            RapierMastery(builder);
            KatanaMastery(builder);
            GunbladeMastery(builder);
            RifleMastery(builder);
            GreatSwordMastery(builder);

            return builder.Build();
        }

        private static void ModifyBAB(uint player, uint item, InventorySlot inventorySlot, int level, bool isApplying, BaseItem requiredItemType, InventorySlot requiredSlot)
        {
            var baseItemType = GetBaseItemType(item);
            if (baseItemType != requiredItemType) return;
            if (inventorySlot != requiredSlot) return;

            var amount = isApplying ? level : -level;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            Stat.AdjustBAB(dbPlayer, player, amount);
            DB.Set(playerId, dbPlayer);
        }

        private static void LongswordMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Knight, PerkType.LongswordMastery)
                .Name("Longsword Mastery")
                .Description("Grants increased BAB when equipped with a longsword.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.Longsword, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.Longsword, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a longsword.")
                .Price(8)
                .RequirementSkill(SkillType.Longsword, 20)
                .RequirementSkill(SkillType.Chivalry, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a longsword.")
                .Price(8)
                .RequirementSkill(SkillType.Longsword, 30)
                .RequirementSkill(SkillType.Chivalry, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a longsword.")
                .Price(8)
                .RequirementSkill(SkillType.Longsword, 40)
                .RequirementSkill(SkillType.Chivalry, 50);
        }

        private static void KnucklesMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Monk, PerkType.KnucklesMastery)
                .Name("Knuckles Mastery")
                .Description("Grants increased BAB when equipped with knuckles.")

                // Knuckles BAB calculations work a little backwards compared to the other item types.
                // We want to add BAB when the player is newly bare-handed.
                // We want to remove BAB when the player equips any item into their right or left hands.
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    // There are items in the right or left hands right now. Exit early
                    if (GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, player)) ||
                        GetIsObjectValid(GetItemInSlot(InventorySlot.LeftHand, player))) return;

                    // Nothing was equipped, but we're about to put an item in either hand.
                    // For this scenario, we need to reduce the BAB because the player is about to have a weapon equipped.
                    if(inventorySlot == InventorySlot.RightHand || inventorySlot == InventorySlot.LeftHand)
                        ModifyBAB(player, item, InventorySlot.Invalid, level, false, GetBaseItemType(item), InventorySlot.Invalid);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    var rightHand = GetItemInSlot(InventorySlot.RightHand, player);
                    var leftHand = GetItemInSlot(InventorySlot.LeftHand, player);
                    var rightValid = GetIsObjectValid(rightHand);
                    var leftValid = GetIsObjectValid(leftHand);

                    // The item being removed is in either the right or left hand and the OTHER hand is empty.
                    // We need to apply the BAB bonus.
                    if ((rightHand == item && !leftValid) ||
                        (leftHand == item && !rightValid))
                    {
                        ModifyBAB(player, item, InventorySlot.Invalid, level, true, GetBaseItemType(item), InventorySlot.Invalid);
                    }
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with knuckles.")
                .Price(8)
                .RequirementSkill(SkillType.Knuckles, 20)
                .RequirementSkill(SkillType.Chi, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with knuckles.")
                .Price(8)
                .RequirementSkill(SkillType.Knuckles, 30)
                .RequirementSkill(SkillType.Chi, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with knuckles.")
                .Price(8)
                .RequirementSkill(SkillType.Knuckles, 40)
                .RequirementSkill(SkillType.Chi, 50);
        }


        private static void DaggerMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Thief, PerkType.DaggerMastery)
                .Name("Dagger Mastery")
                .Description("Grants increased BAB when equipped with a dagger.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.Dagger, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.Dagger, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a dagger.")
                .Price(8)
                .RequirementSkill(SkillType.Dagger, 20)
                .RequirementSkill(SkillType.Thievery, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a dagger.")
                .Price(8)
                .RequirementSkill(SkillType.Dagger, 30)
                .RequirementSkill(SkillType.Thievery, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a dagger.")
                .Price(8)
                .RequirementSkill(SkillType.Dagger, 40)
                .RequirementSkill(SkillType.Thievery, 50);
        }


        private static void StaffMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.BlackMage, PerkType.StaffMastery)
                .Name("Staff Mastery")
                .Description("Grants increased BAB when equipped with a staff.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.QuarterStaff, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.QuarterStaff, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a staff.")
                .Price(8)
                .RequirementSkill(SkillType.Staff, 20)
                .RequirementSkill(SkillType.BlackMagic, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a staff.")
                .Price(8)
                .RequirementSkill(SkillType.Staff, 30)
                .RequirementSkill(SkillType.BlackMagic, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a staff.")
                .Price(8)
                .RequirementSkill(SkillType.Staff, 40)
                .RequirementSkill(SkillType.BlackMagic, 50);
        }


        private static void RodMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.WhiteMage, PerkType.RodMastery)
                .Name("Rod Mastery")
                .Description("Grants increased BAB when equipped with a rod.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.LightMace, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.LightMace, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a rod.")
                .Price(8)
                .RequirementSkill(SkillType.Rod, 20)
                .RequirementSkill(SkillType.WhiteMagic, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a rod.")
                .Price(8)
                .RequirementSkill(SkillType.Rod, 30)
                .RequirementSkill(SkillType.WhiteMagic, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a rod.")
                .Price(8)
                .RequirementSkill(SkillType.Rod, 40)
                .RequirementSkill(SkillType.WhiteMagic, 50);
        }


        private static void RapierMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.RedMage, PerkType.RapierMastery)
                .Name("Rapier Mastery")
                .Description("Grants increased BAB when equipped with a rapier.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.Rapier, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.Rapier, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a rapier.")
                .Price(8)
                .RequirementSkill(SkillType.Rapier, 20)
                .RequirementSkill(SkillType.RedMagic, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a rapier.")
                .Price(8)
                .RequirementSkill(SkillType.Rapier, 30)
                .RequirementSkill(SkillType.RedMagic, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a rapier.")
                .Price(8)
                .RequirementSkill(SkillType.Rapier, 40)
                .RequirementSkill(SkillType.RedMagic, 50);
        }

        private static void KatanaMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Ninja, PerkType.KatanaMastery)
                .Name("Katana Mastery")
                .Description("Grants increased BAB when equipped with a katana.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.Katana, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.Katana, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a katana.")
                .Price(8)
                .RequirementSkill(SkillType.Katana, 20)
                .RequirementSkill(SkillType.Ninjitsu, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a katana.")
                .Price(8)
                .RequirementSkill(SkillType.Katana, 30)
                .RequirementSkill(SkillType.Ninjitsu, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a katana.")
                .Price(8)
                .RequirementSkill(SkillType.Katana, 40)
                .RequirementSkill(SkillType.Ninjitsu, 50);
        }


        private static void GunbladeMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Specialist, PerkType.GunbladeMastery)
                .Name("Gunblade Mastery")
                .Description("Grants increased BAB when equipped with a gunblade.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.Gunblade, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.Gunblade, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a gunblade.")
                .Price(8)
                .RequirementSkill(SkillType.Gunblade, 20)
                .RequirementSkill(SkillType.Swordplay, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a gunblade.")
                .Price(8)
                .RequirementSkill(SkillType.Gunblade, 30)
                .RequirementSkill(SkillType.Swordplay, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a gunblade.")
                .Price(8)
                .RequirementSkill(SkillType.Gunblade, 40)
                .RequirementSkill(SkillType.Swordplay, 50);
        }

        private static void RifleMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Sniper, PerkType.RifleMastery)
                .Name("Rifle Mastery")
                .Description("Grants increased BAB when equipped with a rifle.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.Rifle, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.Rifle, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Rifle, 20)
                .RequirementSkill(SkillType.Marksmanship, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Rifle, 30)
                .RequirementSkill(SkillType.Marksmanship, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a rifle.")
                .Price(8)
                .RequirementSkill(SkillType.Rifle, 40)
                .RequirementSkill(SkillType.Marksmanship, 50);
        }

        private static void GreatSwordMastery(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.DarkKnight, PerkType.GreatSwordMastery)
                .Name("Great Sword Mastery")
                .Description("Grants increased BAB when equipped with a great sword.")
                .TriggerEquippedItem((player, item, inventorySlot, type, level) =>
                {
                    ModifyBAB(player, item, inventorySlot, level, true, BaseItem.GreatSword, InventorySlot.RightHand);
                })
                .TriggerUnequippedItem((player, item, type, level) =>
                {
                    ModifyBAB(player, item, InventorySlot.Invalid, level, false, BaseItem.GreatSword, InventorySlot.Invalid);
                })

                .AddPerkLevel()
                .Description("Grants +1 BAB when equipped with a great sword.")
                .Price(8)
                .RequirementSkill(SkillType.GreatSword, 20)
                .RequirementSkill(SkillType.Darkness, 25)

                .AddPerkLevel()
                .Description("Grants +2 BAB when equipped with a great sword.")
                .Price(8)
                .RequirementSkill(SkillType.GreatSword, 30)
                .RequirementSkill(SkillType.Darkness, 40)

                .AddPerkLevel()
                .Description("Grants +3 BAB when equipped with a great sword.")
                .Price(8)
                .RequirementSkill(SkillType.GreatSword, 40)
                .RequirementSkill(SkillType.Darkness, 50);
        }
    }
}
