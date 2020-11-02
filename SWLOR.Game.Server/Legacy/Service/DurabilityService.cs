using System;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Legacy.Event.Feat;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class DurabilityService
    {
        private const float DefaultDurability = 5.0f;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnHitCastSpell>(message => OnHitCastSpell());
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
        }

        private static void InitializeDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
            if (item.GetLocalInt("DURABILITY_INITIALIZE") <= 0 &&
                item.GetLocalFloat("DURABILITY_CURRENT") <= 0.0f)
            {
                var durability = GetMaxDurability(item) <= 0 ? DefaultDurability : GetMaxDurability(item);
                item.SetLocalFloat("DURABILITY_CURRENT", durability);
                if (item.GetLocalFloat("DURABILITY_MAX") <= 0.0f)
                {
                    var maxDurability = DefaultDurability;
                    foreach (var ip in item.ItemProperties)
                    {
                        if (GetItemPropertyType(ip) == ItemPropertyType.MaxDurability)
                        {
                            maxDurability = GetItemPropertyCostTableValue(ip);
                            break;
                        }
                    }

                    item.SetLocalFloat("DURABILITY_MAX", maxDurability);
                }
            }
            item.SetLocalInt("DURABILITY_INITIALIZED", 1);
        }

        public static float GetMaxDurability(NWItem item)
        {   
            var maxDurability = item.GetItemPropertyValueAndRemove(ItemPropertyType.MaxDurability);
            if (maxDurability <= -1) return item.GetLocalFloat("DURABILITY_MAX") <= 0 ? DefaultDurability : item.GetLocalFloat("DURABILITY_MAX");
            SetMaxDurability(item, maxDurability);
            return maxDurability;
        }

        public static void SetMaxDurability(NWItem item, float value)
        {
            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
            if (value <= 0) value = DefaultDurability;

            item.SetLocalFloat("DURABILITY_MAX", value);
            InitializeDurability(item);
        }

        public static float GetDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var durability = item.GetItemPropertyValueAndRemove(ItemPropertyType.Durability);

            if (durability <= -1)
            {
                InitializeDurability(item);
                return item.GetLocalFloat("DURABILITY_CURRENT");
            }

            SetDurability(item, durability);
            return durability;
        }

        public static void SetDurability(NWItem item, float value)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (value < 0.0f) value = 0.0f;

            SetLocalBool(item, "DURABILITY_OVERRIDE", true);
            InitializeDurability(item);
            item.SetLocalFloat("DURABILITY_CURRENT", value);
        }
        
        private static void OnModuleEquipItem()
        {
            NWPlayer oPC = (GetPCItemLastEquippedBy());

            // Don't run heavy code when customizing equipment.
            if (GetLocalBool(oPC, "IS_CUSTOMIZING_ITEM")) return;
            
            NWItem oItem = (GetPCItemLastEquipped());
            var durability = GetDurability(oItem);

            if (durability <= 0 && durability != -1 && oItem.IsValid)
            {
                oPC.AssignCommand(() =>
                {
                    ClearAllActions();
                    ActionUnequipItem(oItem.Object);
                });

                oPC.FloatingText(ColorToken.Red("That item is broken and must be repaired before you can use it."));
            }
        
        }

        public static string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != ObjectType.Item) return existingDescription;

            NWItem examinedItem = (examinedObject.Object);
            if (examinedItem.GetLocalFloat("DURABILITY_MAX") <= 0f) return existingDescription;

            var description = ColorToken.Orange("Durability: ");
            var durability = GetDurability(examinedItem);
            if (durability <= 0.0f) description += ColorToken.Red(Convert.ToString(durability));
            else description += ColorToken.White(FormatDurability(durability));

            description += ColorToken.White(" / " + FormatDurability(GetMaxDurability(examinedItem)));

            return existingDescription + "\n\n" + description;
        }

        public static void RunItemDecay(NWPlayer player, NWItem item)
        {
            RunItemDecay(player, item, 0.01f);
        }

        public static void RunItemDecay(NWPlayer player, NWItem item, float reduceAmount)
        {
            if (reduceAmount <= 0) return;
            if (player.IsPlot ||
                item.IsPlot ||
                item.GetLocalInt("UNBREAKABLE") == 1 ||
                !item.IsValid ||
                item.BaseItemType == BaseItem.CreatureItem ||  // Creature skin
                item.BaseItemType == BaseItem.CreatureBludgeWeapon ||  // Creature bludgeoning weapon
                item.BaseItemType == BaseItem.CreaturePierceWeapon ||  // Creature piercing weapon
                item.BaseItemType == BaseItem.CreatureSlashWeapon ||  // Creature slashing weapon
                item.BaseItemType == BaseItem.CreatureSlashPierceWeapon)    // Creature slashing/piercing weapon
                return;
            
            var durability = GetDurability(item);
            var sItemName = item.Name;
            var apr = Creature.GetAttacksPerRound(player, true);
            // Reduce by 0.001 each time it's run. Player only receives notifications when it drops a full point.
            // I.E: Dropping from 29.001 to 29.
            // Note that players only see two decimal places in-game on purpose.
            durability -= reduceAmount / apr;
            var displayMessage = Math.Abs(durability % 1) < 0.05f;

            if (displayMessage)
            {
                player.SendMessage(ColorToken.Red("Your " + sItemName + " has been damaged. (" + FormatDurability(durability) + " / " + GetMaxDurability(item)));
            }

            if (durability <= 0.00f)
            {
                item.Destroy();
                player.SendMessage(ColorToken.Red("Your " + sItemName + " has broken!"));
            }
            else
            {

                SetDurability(item, durability);
            }
        }

        private static string FormatDurability(float durability)
        {
            return durability.ToString("0.00");
        }

        public static void RunItemRepair(NWPlayer oPC, NWItem item, float amount, float maxReductionAmount)
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
            oPC.SendMessage(ColorToken.Green("You repaired your " + item.Name + ". (" + durMessage + ")"));
        }
        
        private static void OnHitCastSpell()
        {
            NWPlayer oTarget = OBJECT_SELF;
            if (!oTarget.IsValid) return;
            NWItem oSpellOrigin = (GetSpellCastItem());

            var decayItem = oSpellOrigin;

            if (oSpellOrigin.BaseItemType == BaseItem.Arrow ||
                oSpellOrigin.BaseItemType == BaseItem.Bolt ||
                oSpellOrigin.BaseItemType == BaseItem.Bullet)
            {
                decayItem = oTarget.RightHand;
            }

            if (oSpellOrigin.BaseItemType == BaseItem.Armor)
            {
                // Distribute durability hits across items in all clothing slots.
                // PCs could have any number of these filled. Pick one at random, 
                // defaulting to armor. 
                //
                // Because randomising from a huge possible number of combinations
                // isn't simple, just do a % check for each item in turn.  Items
                // are broadly in order of "most likely to get hit" as items near
                // the top have a slightly higher chance of being chosen.                
                if (oTarget.Chest.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Chest;
                }
                else if (oTarget.Cloak.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Cloak;
                }
                else if (oTarget.Head.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Head;
                }
                // Gloves only decay from this code if they are not being used as a weapon.
                else if (oTarget.Arms.IsValid && oTarget.RightHand.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Arms;
                }
                else if (oTarget.Boots.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Boots;
                }
                else if (oTarget.Belt.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Belt;
                }
                else if (oTarget.Neck.IsValid && d100() > 85)
                {
                    decayItem = oTarget.Neck;
                }
                // Rings are very small, so less likely. 
                else if (oTarget.LeftRing.IsValid && d100() > 95)
                {
                    decayItem = oTarget.LeftRing;
                }
                else if (oTarget.RightRing.IsValid && d100() > 95)
                {
                    decayItem = oTarget.RightRing;
                }
                else
                {
                    // Do nothing, leave it as default (chest). 
                }
            }

            RunItemDecay(oTarget, decayItem);
        }

    }
}
