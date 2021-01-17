using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature
{
    public static class ScavengePoint
    {
        /// <summary>
        /// 
        /// </summary>
        [NWNEventHandler("scav_opened")]
        public static void OnOpened()
        {
            var user = GetLastOpenedBy();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var placeable = OBJECT_SELF;
            if (GetLocalBool(placeable, "FULLY_HARVESTED"))
            {
                SendMessageToPC(user, "This object has already been searched through.");
                return;
            }
            
            var scavengingLevel = Perk.GetEffectivePerkLevel(user, PerkType.Scavenging);
            var hardLookLevel = Perk.GetEffectivePerkLevel(user, PerkType.HardLook);
            var requiredLevel = GetLocalInt(placeable, "SCAVENGE_POINT_LEVEL");
            var lootTableName = GetLocalString(placeable, "SCAVENGE_POINT_LOOT_TABLE_NAME");

            // Loot table doesn't exist.
            if (!Loot.LootTableExists(lootTableName))
            {
                SendMessageToPC(user, $"The assigned loot table '{lootTableName}' does not exist. Please inform an admin that this scavenge site is broken.");
                AssignCommand(user, () => ActionInteractObject(placeable));
                return;
            }
            
            // Perk level isn't high enough.
            if (scavengingLevel < requiredLevel)
            {
                SendMessageToPC(user, $"You aren't skilled enough to scavenge through this. (Required level: {requiredLevel})");
                AssignCommand(user, () => ActionInteractObject(placeable));
                return;
            }

            var attempts = 1;

            // Chance for a second attempt based on the hard look perk level.
            if (Random.D100(1) <= hardLookLevel * 10)
            {
                attempts++;
            }

            AssignCommand(user, () => ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 2.0f));

            var lootTable = Loot.GetLootTableByName(lootTableName);
            var dc = 6;
            var xp = 0;
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                var roll = Random.D20(1);

                if (roll >= dc)
                {
                    FloatingTextStringOnCreature(ColorToken.SkillCheck($"Search *success*: ({roll} vs. DC: {dc})"), user, false);

                    var item = lootTable.GetRandomItem();
                    var quantity = Random.Next(item.MaxQuantity) + 1;
                    CreateItemOnObject(item.Resref, placeable, quantity);
                    xp = 200;
                }
                else
                {
                    FloatingTextStringOnCreature(ColorToken.SkillCheck($"Search *failure*: ({roll} vs DC: {dc})"), user, false);
                    xp = 50;
                }

                dc += Random.D3(1);
            }

            Skill.GiveSkillXP(user, SkillType.Gathering, xp);
            
            SetLocalBool(placeable, "FULLY_HARVESTED", true);
        }

        /// <summary>
        /// When an item is added to a scavenge point, return it to the user.
        /// When an item is removed from a scavenge point, if there are no more items in the inventory, destroy the placeable.
        /// </summary>
        [NWNEventHandler("scav_disturbed")]
        public static void OnDisturbed()
        {
            var user = GetLastDisturbed();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();
            var placeable = OBJECT_SELF;

            if (type == DisturbType.Added)
            {
                Item.ReturnItem(user, item);
            }
            else
            {
                var firstItem = GetFirstItemInInventory(placeable);
                if (!GetIsObjectValid(firstItem))
                {
                    DestroyObject(placeable);
                }
            }

        }

        /// <summary>
        /// When a scavenge site is closed by a player, if there are no more items in the inventory, destroy the scavenge point.
        /// </summary>
        [NWNEventHandler("scav_closed")]
        public static void OnClosed()
        {
            var placeable = OBJECT_SELF;
            var user = GetLastClosedBy();

            if (!GetIsPC(user) || GetIsDM(user)) return;

            var firstItem = GetFirstItemInInventory(placeable);
            if (!GetIsObjectValid(firstItem))
            {
                DestroyObject(placeable);
            }

        }
    }
}
