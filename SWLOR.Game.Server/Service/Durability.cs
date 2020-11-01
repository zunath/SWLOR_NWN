//using System;
//using SWLOR.Game.Server.Core;
//using SWLOR.Game.Server.Core.NWNX;
//using SWLOR.Game.Server.Core.NWScript.Enum;
//using SWLOR.Game.Server.Core.NWScript.Enum.Item;
//using static SWLOR.Game.Server.Core.NWScript.NWScript;

//namespace SWLOR.Game.Server.Service
//{
//    public static class Durability
//    {
//        private const float DefaultDurability = 5.0f;

//        [NWNEventHandler("")]
//        public static void ReduceDurabilityOnHit()
//        {
//            var target = OBJECT_SELF;
//            if (!GetIsObjectValid(target)) return;
//            var item = GetSpellCastItem();
//            var itemType = GetBaseItemType(item);

//            var decayItem = item;

//            if (itemType == BaseItem.Arrow ||
//                itemType == BaseItem.Bolt ||
//                itemType == BaseItem.Bullet)
//            {
//                decayItem = GetItemInSlot(InventorySlot.RightHand, target);
//            }

//            if (itemType == BaseItem.Armor)
//            {
//                // Distribute durability hits across items in all clothing slots.
//                // PCs could have any number of these filled. Pick one at random, 
//                // defaulting to armor. 
//                //
//                // Because randomising from a huge possible number of combinations
//                // isn't simple, just do a % check for each item in turn.  Items
//                // are broadly in order of "most likely to get hit" as items near
//                // the top have a slightly higher chance of being chosen.                
//                if (GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Chest, target);
//                }
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Cloak, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Cloak, target);
//                }
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Head, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Head, target);
//                }
//                // Gloves only decay from this code if they are not being used as a weapon.
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Arms, target)) && 
//                         GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Arms, target);
//                }
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Boots, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Boots, target);
//                }
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Belt, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Belt, target);
//                }
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.Neck, target)) && d100() > 85)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.Neck, target);
//                }
//                // Rings are very small, so less likely. 
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.LeftRing, target)) && d100() > 95)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.LeftRing, target);
//                }
//                else if (GetIsObjectValid(GetItemInSlot(InventorySlot.RightRing, target)) && d100() > 95)
//                {
//                    decayItem = GetItemInSlot(InventorySlot.RightRing, target);
//                }
//            }

//            RunItemDecay(target, decayItem, 0.01f);
//        }


//        private static string FormatDurability(float durability)
//        {
//            return durability.ToString("0.00");
//        }

//        public static void RunItemRepair(uint player, uint item, float amount, float maxReductionAmount)
//        {
//            // Prevent repairing for less than 0.01
//            if (amount < 0.01f) return;

//            var maxDurability = GetMaxDurability(item) - maxReductionAmount;
//            var durability = GetDurability(item) + amount;

//            if (maxDurability < 0.01f)
//                maxDurability = 0.01f;
//            if (durability > maxDurability)
//                durability = maxDurability;

//            SetMaxDurability(item, maxDurability);
//            SetDurability(item, durability);
//            var durMessage = FormatDurability(durability) + " / " + FormatDurability(maxDurability);
//            SendMessageToPC(player, ColorToken.Green("You repaired your " + GetName(item) + ". (" + durMessage + ")"));
//        }

//        public static void RunItemDecay(uint player, uint item, float reduceAmount)
//        {
//            var itemType = GetBaseItemType(item);
//            if (reduceAmount <= 0) return;
//            if (GetPlotFlag(player) ||
//                GetPlotFlag(item) ||
//                GetLocalBool(item, "UNBREAKABLE") ||
//                !GetIsObjectValid(item) ||
//                itemType == BaseItem.CreatureItem ||  // Creature skin
//                itemType == BaseItem.CreatureBludgeWeapon ||  // Creature bludgeoning weapon
//                itemType == BaseItem.CreaturePierceWeapon ||  // Creature piercing weapon
//                itemType == BaseItem.CreatureSlashWeapon ||  // Creature slashing weapon
//                itemType == BaseItem.CreatureSlashPierceWeapon)    // Creature slashing/piercing weapon
//                return;

//            var durability = GetDurability(item);
//            var sItemName = GetName(item);
//            var apr = Creature.GetAttacksPerRound(player, true);
//            // Reduce by 0.001 each time it's run. Player only receives notifications when it drops a full point.
//            // I.E: Dropping from 29.001 to 29.
//            // Note that players only see two decimal places in-game on purpose.
//            durability -= reduceAmount / apr;
//            var displayMessage = Math.Abs(durability % 1) < 0.05f;

//            if (displayMessage)
//            {
//                SendMessageToPC(player, ColorToken.Red("Your " + sItemName + " has been damaged. (" + FormatDurability(durability) + " / " + GetMaxDurability(item)));
//            }

//            if (durability <= 0.00f)
//            {
//                DestroyObject(item);
//                SendMessageToPC(player, ColorToken.Red("Your " + sItemName + " has broken!"));
//            }
//            else
//            {

//                SetDurability(item, durability);
//            }
//        }


//        public static float GetMaxDurability(uint item)
//        {
//            var maxDurability = GetLocalFloat(item, "DURABILITY_MAX");
//            return maxDurability <= 0 ? DefaultDurability : maxDurability;
//        }

//        public static void SetMaxDurability(uint item, float value)
//        {
//            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
//            if (value <= 0) value = DefaultDurability;

//            item.SetLocalFloat("DURABILITY_MAX", value);
//            InitializeDurability(item);
//        }

//        public static float GetDurability(uint item)
//        {
//            int durability = item.GetItemPropertyValueAndRemove(ItemPropertyType.Durability);

//            if (durability <= -1)
//            {
//                InitializeDurability(item);
//                return GetLocalFloat(item, "DURABILITY_CURRENT");
//            }

//            SetDurability(item, durability);
//            return durability;
//        }

//        public static void SetDurability(uint item, float value)
//        {
//            if (value < 0.0f) value = 0.0f;

//            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
//            InitializeDurability(item);
//            SetLocalFloat(item, "DURABILITY_CURRENT", value);
//        }
//    }
//}
