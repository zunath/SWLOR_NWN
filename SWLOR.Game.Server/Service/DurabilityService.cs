using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class DurabilityService : IDurabilityService
    {
        private const float DefaultDurability = 3.0f;

        private readonly INWScript _;
        private readonly IColorTokenService _color;

        public DurabilityService(
            INWScript script,
            IColorTokenService color)
        {
            _ = script;
            _color = color;
        }
        
        private void InitializeDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.SetLocalInt("DURABILITY_OVERRIDE", TRUE);
            if (item.GetLocalInt("DURABILITY_INITIALIZE") <= 0 &&
                item.GetLocalFloat("DURABILITY_CURRENT") <= 0.0f)
            {
                float durability = GetMaxDurability(item) <= 0 ? DefaultDurability : GetMaxDurability(item);
                item.SetLocalFloat("DURABILITY_CURRENT", durability);
                if (item.GetLocalFloat("DURABILITY_MAX") <= 0.0f)
                {
                    float maxDurability = DefaultDurability;
                    foreach (var ip in item.ItemProperties)
                    {
                        if (_.GetItemPropertyType(ip) == (int) CustomItemPropertyType.MaxDurability)
                        {
                            maxDurability = _.GetItemPropertyCostTableValue(ip);
                            break;
                        }
                    }

                    item.SetLocalFloat("DURABILITY_MAX", maxDurability);
                }
            }
            item.SetLocalInt("DURABILITY_INITIALIZED", 1);
        }

        public float GetMaxDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            
            int maxDurability = item.GetItemPropertyValueAndRemove((int)CustomItemPropertyType.MaxDurability);
            if (maxDurability <= -1) return item.GetLocalFloat("DURABILITY_MAX") <= 0 ? DefaultDurability : item.GetLocalFloat("DURABILITY_MAX");
            SetMaxDurability(item, maxDurability);
            return maxDurability;
        }

        public void SetMaxDurability(NWItem item, float value)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            item.SetLocalInt("DURABILITY_OVERRIDE", TRUE);
            if (value <= 0) value = DefaultDurability;

            item.SetLocalFloat("DURABILITY_MAX", value);
            InitializeDurability(item);
        }

        public float GetDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            int durability = item.GetItemPropertyValueAndRemove((int)CustomItemPropertyType.Durability);

            if (durability <= -1)
            {
                InitializeDurability(item);
                return item.GetLocalFloat("DURABILITY_CURRENT");
            }

            SetDurability(item, durability);
            return durability;
        }

        public void SetDurability(NWItem item, float value)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (value < 0.0f) value = 0.0f;

            item.SetLocalInt("DURABILITY_OVERRIDE", TRUE);
            InitializeDurability(item);
            item.SetLocalFloat("DURABILITY_CURRENT", value);
        }
        
        public void OnModuleEquip()
        {
            NWPlayer oPC = (_.GetPCItemLastEquippedBy());
            NWItem oItem = (_.GetPCItemLastEquipped());
            float durability = GetDurability(oItem);

            if (durability <= 0 && durability != -1)
            {
                oPC.AssignCommand(() =>
                {
                    _.ClearAllActions();
                    _.ActionUnequipItem(oItem.Object);
                });
                
                oPC.FloatingText(_color.Red("That item is broken and must be repaired before you can use it."));
            }
        }

        public string OnModuleExamine(string existingDescription, NWObject examinedObject)
        {
            if (examinedObject.ObjectType != OBJECT_TYPE_ITEM) return existingDescription;

            NWItem examinedItem = (examinedObject.Object);
            if (examinedItem.GetLocalFloat("DURABILITY_MAX") <= 0f) return existingDescription;

            string description = _color.Orange("Durability: ");
            float durability = GetDurability(examinedItem);
            if (durability <= 0.0f) description += _color.Red(Convert.ToString(durability));
            else description += _color.White(FormatDurability(durability));

            description += _color.White(" / " + FormatDurability(GetMaxDurability(examinedItem)));

            return existingDescription + "\n\n" + description;
        }

        public void RunItemDecay(NWPlayer player, NWItem item)
        {
            RunItemDecay(player, item, 0.01f);
        }

        public void RunItemDecay(NWPlayer player, NWItem item, float reduceAmount)
        {
            if (reduceAmount <= 0) return;
            if (player.IsPlot ||
                item.IsPlot ||
                item.GetLocalInt("UNBREAKABLE") == 1 ||
                !item.IsValid ||
                item.BaseItemType == BASE_ITEM_CREATUREITEM ||  // Creature skin
                item.BaseItemType == BASE_ITEM_CBLUDGWEAPON ||  // Creature bludgeoning weapon
                item.BaseItemType == BASE_ITEM_CPIERCWEAPON ||  // Creature piercing weapon
                item.BaseItemType == BASE_ITEM_CSLASHWEAPON ||  // Creature slashing weapon
                item.BaseItemType == BASE_ITEM_CSLSHPRCWEAP)    // Creature slashing/piercing weapon
                return;
            
            float durability = GetDurability(item);
            string sItemName = item.Name;
            
            // Reduce by 0.001 each time it's run. Player only receives notifications when it drops a full point.
            // I.E: Dropping from 29.001 to 29.
            // Note that players only see two decimal places in-game on purpose.
            durability -= reduceAmount;
            bool displayMessage = durability % 1 == 0;

            if (displayMessage)
            {
                player.SendMessage(_color.Red("Your " + sItemName + " has been damaged. (" + FormatDurability(durability) + " / " + GetMaxDurability(item)));
            }

            if (durability <= 0.00f)
            {
                item.Destroy();
                player.SendMessage(_color.Red("Your " + sItemName + " has broken!"));
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

        public void RunItemRepair(NWPlayer oPC, NWItem item, float amount, float maxReductionAmount)
        {
            // Prevent repairing for less than 0.01
            if (amount < 0.01f) return;

            float maxDurability = GetMaxDurability(item) - maxReductionAmount;
            float durability = GetDurability(item) + amount;
            
            if (maxDurability < 0.01f)
                maxDurability = 0.01f;
            if (durability > maxDurability)
                durability = maxDurability;
            
            SetMaxDurability(item, maxDurability);
            SetDurability(item, durability);
            string durMessage = FormatDurability(durability) + " / " + FormatDurability(maxDurability);
            oPC.SendMessage(_color.Green("You repaired your " + item.Name + ". (" + durMessage + ")"));
        }
        
        public void OnHitCastSpell(NWPlayer oTarget)
        {
            NWItem oSpellOrigin = (_.GetSpellCastItem());

            NWItem decayItem = oSpellOrigin;

            if (oSpellOrigin.BaseItemType == BASE_ITEM_ARROW ||
                oSpellOrigin.BaseItemType == BASE_ITEM_BOLT ||
                oSpellOrigin.BaseItemType == BASE_ITEM_BULLET)
            {
                decayItem = oTarget.RightHand;
            }

            RunItemDecay(oTarget, decayItem);
        }

    }
}
