using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Durability
    {
        private const float DefaultDurability = 5.0f;

        /// <summary>
        /// When a weapon or armor is hit, run through the durability decay process.
        /// </summary>
        [NWNEventHandler("item_on_hit")]
        public static void ReduceDurabilityOnHit()
        {
            var target = OBJECT_SELF;
            if (!GetIsObjectValid(target)) return;
            var item = GetSpellCastItem();
            var itemType = GetBaseItemType(item);

            var decayItem = item;

            if (itemType == BaseItem.Arrow ||
                itemType == BaseItem.Bolt ||
                itemType == BaseItem.Bullet)
            {
                decayItem = GetItemInSlot(InventorySlot.RightHand, target);
            }

            if (itemType == BaseItem.Armor)
            {
                // Distribute durability hits across items in all clothing slots.
                // PCs could have any number of these filled. Pick one at random, 
                // defaulting to armor. 
                //
                // Because randomising from a huge possible number of combinations
                // isn't simple, just do a % check for each item in turn.  Items
                // are broadly in order of "most likely to get hit" as items near
                // the top have a slightly higher chance of being chosen.                
                if (GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Chest, target);
                }
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Cloak, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Cloak, target);
                }
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Head, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Head, target);
                }
                // Gloves only decay from this code if they are not being used as a weapon.
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Arms, target)) &&
                         GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Arms, target);
                }
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Boots, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Boots, target);
                }
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Belt, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Belt, target);
                }
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Neck, target)) && d100() > 85)
                {
                    decayItem = GetItemInSlot(InventorySlot.Neck, target);
                }
                // Rings are very small, so less likely. 
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.LeftRing, target)) && d100() > 95)
                {
                    decayItem = GetItemInSlot(InventorySlot.LeftRing, target);
                }
                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.RightRing, target)) && d100() > 95)
                {
                    decayItem = GetItemInSlot(InventorySlot.RightRing, target);
                }
            }

            RunItemDecay(target, decayItem, 0.01f);
        }

        /// <summary>
        /// Formats durability so that it only shows two decimal places.
        /// </summary>
        /// <param name="durability">The durability to format.</param>
        /// <returns>A formatted durability string.</returns>
        private static string FormatDurability(float durability)
        {
            return durability.ToString("0.00");
        }

        /// <summary>
        /// Runs through the item repair process for a given player and item.
        /// </summary>
        /// <param name="player">The player doing the repair.</param>
        /// <param name="item">The item being repaired.</param>
        /// <param name="amount">The amount to restore by.</param>
        /// <param name="maxReductionAmount">The maximum amount of durability to lose on repair completion.</param>
        public static void RunItemRepair(uint player, uint item, float amount, float maxReductionAmount)
        {
            // Prevent repairing for less than 0.01
            if (amount < 0.01f) return;

            var maxDurability = GetMaxDurability(item) - maxReductionAmount;
            var durability = GetDurability(item) + amount;

            if (maxDurability < 0.01f)
                maxDurability = 0.01f;
            if (durability > maxDurability)
                durability = maxDurability;

            SetMaxDurability(item, maxDurability);
            SetDurability(item, durability);
            var durMessage = FormatDurability(durability) + " / " + FormatDurability(maxDurability);
            SendMessageToPC(player, ColorToken.Green("You repaired your " + GetName(item) + ". (" + durMessage + ")"));
        }

        /// <summary>
        /// Runs through the item repair process for a given player and item.
        /// </summary>
        /// <param name="player">The player doing the repair.</param>
        /// <param name="item">The item being repaired.</param>
        /// <param name="reduceAmount">The amount to restore by.</param>
        public static void RunItemDecay(uint player, uint item, float reduceAmount)
        {
            var itemType = GetBaseItemType(item);
            if (reduceAmount <= 0) return;
            if (GetPlotFlag(player) ||
                GetPlotFlag(item) ||
                GetLocalBool(item, "UNBREAKABLE") ||
                !GetIsObjectValid(item) ||
                itemType == BaseItem.CreatureItem ||  // Creature skin
                itemType == BaseItem.CreatureBludgeWeapon ||  // Creature bludgeoning weapon
                itemType == BaseItem.CreaturePierceWeapon ||  // Creature piercing weapon
                itemType == BaseItem.CreatureSlashWeapon ||  // Creature slashing weapon
                itemType == BaseItem.CreatureSlashPierceWeapon)    // Creature slashing/piercing weapon
                return;

            var durability = GetDurability(item);
            var sItemName = GetName(item);
            var apr = Creature.GetAttacksPerRound(player, true);
            // Reduce by 0.001 each time it's run. Player only receives notifications when it drops a full point.
            // I.E: Dropping from 29.001 to 29.
            // Note that players only see two decimal places in-game on purpose.
            durability -= reduceAmount / apr;
            var displayMessage = Math.Abs(durability % 1) < 0.05f;

            if (displayMessage)
            {
                SendMessageToPC(player, ColorToken.Red("Your " + sItemName + " has been damaged. (" + FormatDurability(durability) + " / " + GetMaxDurability(item)));
            }

            if (durability <= 0.00f)
            {
                DestroyObject(item);
                SendMessageToPC(player, ColorToken.Red("Your " + sItemName + " has broken!"));
            }
            else
            {

                SetDurability(item, durability);
            }
        }

        /// <summary>
        /// Initializes the max and current durability values on an item.
        /// </summary>
        /// <param name="item">The item to initialize.</param>
        private static void InitializeDurability(uint item)
        {
            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
            if (GetLocalInt(item, "DURABILITY_INITIALIZE") <= 0 &&
                GetLocalFloat(item, "DURABILITY_CURRENT") <= 0.0f)
            {
                var durability = GetMaxDurability(item) <= 0 ? DefaultDurability : GetMaxDurability(item);
                SetLocalFloat(item, "DURABILITY_CURRENT", durability);
                if (GetLocalFloat(item, "DURABILITY_MAX") <= 0.0f)
                {
                    var maxDurability = DefaultDurability;
                    for(var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.MaxDurability)
                        {
                            maxDurability = GetItemPropertyCostTableValue(ip);
                            break;
                        }
                    }

                    SetLocalFloat(item, "DURABILITY_MAX", maxDurability);
                }
            }
            SetLocalInt(item, "DURABILITY_INITIALIZED", 1);
        }

        /// <summary>
        /// Retrieves an item's max durability value.
        /// </summary>
        /// <param name="item">The item to retrieve from</param>
        /// <returns>The max durability on the item.</returns>
        public static float GetMaxDurability(uint item)
        {
            var maxDurability = GetLocalFloat(item, "DURABILITY_MAX");
            return maxDurability <= 0 ? DefaultDurability : maxDurability;
        }

        /// <summary>
        /// Sets the max durability value for an item.
        /// </summary>
        /// <param name="item">The item to change.</param>
        /// <param name="value">The new max durability value to set.</param>
        public static void SetMaxDurability(uint item, float value)
        {
            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
            if (value <= 0) value = DefaultDurability;

            SetLocalFloat(item, "DURABILITY_MAX", value);
            InitializeDurability(item);
        }

        /// <summary>
        /// Retrieves the current durabiltiy value of an item.
        /// </summary>
        /// <param name="item">The item to retrieve durability from</param>
        /// <returns>The current durability of the specified item.</returns>
        public static float GetDurability(uint item)
        {
            int durability = GetItemPropertyValueAndRemove(item, ItemPropertyType.Durability);

            if (durability <= -1)
            {
                InitializeDurability(item);
                return GetLocalFloat(item, "DURABILITY_CURRENT");
            }

            SetDurability(item, durability);
            return durability;
        }

        /// <summary>
        /// Sets the current durability value of an item.
        /// </summary>
        /// <param name="item">The item to change.</param>
        /// <param name="value">The new current durability value to set.</param>
        public static void SetDurability(uint item, float value)
        {
            if (value < 0.0f) value = 0.0f;

            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
            InitializeDurability(item);
            SetLocalFloat(item, "DURABILITY_CURRENT", value);
        }
    }
}
