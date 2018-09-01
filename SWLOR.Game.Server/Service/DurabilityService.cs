using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class DurabilityService : IDurabilityService
    {
        private const float DefaultDurability = 5.0f;

        private readonly INWScript _;
        private readonly IColorTokenService _color;

        public DurabilityService(INWScript script,
            IColorTokenService color)
        {
            _ = script;
            _color = color;
        }
        
        public bool IsValidDurabilityType(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            int[] validTypes =
            {
                NWScript.BASE_ITEM_ARMOR,
                NWScript.BASE_ITEM_BASTARDSWORD,
                NWScript.BASE_ITEM_BATTLEAXE,
                NWScript.BASE_ITEM_BELT,
                NWScript.BASE_ITEM_BOOTS,
                NWScript.BASE_ITEM_BRACER,
                NWScript.BASE_ITEM_CLOAK,
                NWScript.BASE_ITEM_CLUB,
                NWScript.BASE_ITEM_DAGGER,
                NWScript.BASE_ITEM_DIREMACE,
                NWScript.BASE_ITEM_DOUBLEAXE,
                NWScript.BASE_ITEM_DWARVENWARAXE,
                NWScript.BASE_ITEM_GLOVES,
                NWScript.BASE_ITEM_GREATAXE,
                NWScript.BASE_ITEM_GREATSWORD,
                NWScript.BASE_ITEM_HALBERD,
                NWScript.BASE_ITEM_HANDAXE,
                NWScript.BASE_ITEM_HEAVYCROSSBOW,
                NWScript.BASE_ITEM_HEAVYFLAIL,
                NWScript.BASE_ITEM_HELMET,
                NWScript.BASE_ITEM_KAMA,
                NWScript.BASE_ITEM_KATANA,
                NWScript.BASE_ITEM_KUKRI,
                NWScript.BASE_ITEM_LARGESHIELD,
                NWScript.BASE_ITEM_LIGHTCROSSBOW,
                NWScript.BASE_ITEM_LIGHTFLAIL,
                NWScript.BASE_ITEM_LIGHTHAMMER,
                NWScript.BASE_ITEM_LIGHTMACE,
                NWScript.BASE_ITEM_LONGBOW,
                NWScript.BASE_ITEM_LONGSWORD,
                NWScript.BASE_ITEM_MORNINGSTAR,
                NWScript.BASE_ITEM_QUARTERSTAFF,
                NWScript.BASE_ITEM_RAPIER,
                NWScript.BASE_ITEM_SCIMITAR,
                NWScript.BASE_ITEM_SCYTHE,
                NWScript.BASE_ITEM_SHORTBOW,
                NWScript.BASE_ITEM_SHORTSPEAR,
                NWScript.BASE_ITEM_SHORTSWORD,
                NWScript.BASE_ITEM_SICKLE,
                NWScript.BASE_ITEM_SLING,
                NWScript.BASE_ITEM_SMALLSHIELD,
                NWScript.BASE_ITEM_TOWERSHIELD,
                NWScript.BASE_ITEM_TRIDENT,
                NWScript.BASE_ITEM_TWOBLADEDSWORD,
                NWScript.BASE_ITEM_WARHAMMER,
                NWScript.BASE_ITEM_WHIP
            };

            return validTypes.Contains(item.BaseItemType);
        }

        private void InitializeDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (!IsValidDurabilityType(item)) return;

            if (item.GetLocalInt("DURABILITY_INITIALIZE") <= 0 &&
                item.GetLocalFloat("DURABILITY_CURRENT") <= 0.0f)
            {
                float durability = GetMaxDurability(item) <= 0 ? DefaultDurability : GetMaxDurability(item);
                item.SetLocalFloat("DURABILITY_CURRENT", durability);
            }
            item.SetLocalInt("DURABILITY_INITIALIZED", 1);
        }

        public float GetMaxDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (!IsValidDurabilityType(item)) return -1.0f;
            return item.GetLocalInt("DURABILITY_MAX") <= 0 ? DefaultDurability : item.GetLocalInt("DURABILITY_MAX");
        }

        public void SetMaxDurability(NWItem item, float value)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (!IsValidDurabilityType(item)) return;
            if (value <= 0) value = DefaultDurability;

            item.SetLocalFloat("DURABILITY_MAX", value);
            InitializeDurability(item);
        }

        public float GetDurability(NWItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (!IsValidDurabilityType(item)) return -1.0f;
            InitializeDurability(item);
            return item.GetLocalFloat("DURABILITY_CURRENT");
        }

        public void SetDurability(NWItem item, float value)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (value < 0.0f) value = 0.0f;

            if (!IsValidDurabilityType(item)) return;
            InitializeDurability(item);
            item.SetLocalFloat("DURABILITY_CURRENT", value);
        }
        
        public void OnModuleEquip()
        {
            NWPlayer oPC = NWPlayer.Wrap(_.GetPCItemLastEquippedBy());
            NWItem oItem = NWItem.Wrap(_.GetPCItemLastEquipped());
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
            if (examinedObject.ObjectType != NWScript.OBJECT_TYPE_ITEM) return existingDescription;

            NWItem examinedItem = NWItem.Wrap(examinedObject.Object);
            if (!IsValidDurabilityType(examinedItem)) return existingDescription;

            string description = _color.Orange("Durability: ");
            float durability = GetDurability(examinedItem);
            if (durability <= 0.0f) description += _color.Red(Convert.ToString(durability));
            else description += _color.White(FormatDurability(durability));

            description += _color.White(" / " + GetMaxDurability(examinedItem));

            return existingDescription + "\n\n" + description;
        }

        public void RunItemDecay(NWPlayer oPC, NWItem oItem)
        {
            RunItemDecay(oPC, oItem, 0.001f);
        }

        public void RunItemDecay(NWPlayer oPC, NWItem oItem, float reduceAmount)
        {
            if (reduceAmount <= 0) return;
            // Item decay doesn't run for any items if Invincible is in effect
            // Or if the item is unbreakable (e.g profession items)
            // Or if the item is not part of the valid list of base item types
            if (oPC.IsPlot ||
                oItem.GetLocalInt("UNBREAKABLE") == 1 ||
                !IsValidDurabilityType(oItem))
                return;

            float durability = GetDurability(oItem);
            string sItemName = oItem.Name;

            // Reduce by 0.001 each time it's run. Player only receives notifications when it drops a full point.
            // I.E: Dropping from 29.001 to 29.
            // Note that players only see two decimal places in-game on purpose.
            durability -= reduceAmount;
            bool displayMessage = durability % 1 == 0;

            if (displayMessage)
            {
                oPC.SendMessage(_color.Red("Your " + sItemName + " has been damaged. (" + FormatDurability(durability) + " / " + GetMaxDurability(oItem)));
            }

            if (durability <= 0.00f)
            {
                NWItem oCopy = NWItem.Wrap(_.CopyItem(oItem.Object, oPC.Object, NWScript.TRUE));
                oItem.Destroy();
                SetDurability(oCopy, 0);
                oPC.SendMessage(_color.Red("Your " + sItemName + " has broken!"));
            }
            else
            {
                SetDurability(oItem, durability);
            }
        }

        private static string FormatDurability(float durability)
        {
            return durability.ToString("0.00");
        }

        public void RunItemRepair(NWPlayer oPC, NWItem oItem, float amount)
        {
            // Prevent repairing for less than 0.01
            if (amount < 0.01f) return;

            // Item has no durability - inform player they can't repair it
            if (GetDurability(oItem) == -1)
            {
                oPC.SendMessage(_color.Red("You cannot repair that item."));
                return;
            }

            SetDurability(oItem, GetDurability(oItem) + amount);
            string durMessage = FormatDurability(GetDurability(oItem)) + " / " + GetMaxDurability(oItem);
            oPC.SendMessage(_color.Green("You repaired your " + oItem.Name + ". (" + durMessage + ")"));
        }
        
        public void OnHitCastSpell(NWPlayer oTarget)
        {
            NWItem oSpellOrigin = NWItem.Wrap(_.GetSpellCastItem());
            // Durability system
            if (IsValidDurabilityType(oSpellOrigin))
            {
                RunItemDecay(oTarget, oSpellOrigin);
            }
        }

    }
}
