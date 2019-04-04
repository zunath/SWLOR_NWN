using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class SneakAttack: IPerkHandler
    {
        public PerkType PerkType => PerkType.SneakAttack;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            NWItem weapon = oPC.RightHand;
            NWItem armor = oPC.Chest;

            if (weapon.CustomItemType != CustomItemType.FinesseVibroblade &&
                armor.CustomItemType != CustomItemType.LightArmor)
                return "You must be equipped with a finesse blade and light armor to use that ability";

            return string.Empty;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            int perkRank = PerkService.GetPCPerkLevel(oPC, PerkType.SneakAttack);
            float cooldown = baseCooldownTime;

            if (perkRank == 2)
            {
                cooldown -= 30f;
            }
            else if (perkRank > 2)
            {
                cooldown -= 60f;
            }

            return cooldown;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            float minimum = player.Facing - 20;
            float maximum = player.Facing + 20;

            if (target.Facing >= minimum &&
                target.Facing <= maximum)
            {
                // Mark the player as committing a sneak attack.
                // This is later picked up in the OnApplyDamage event.
                player.SetLocalInt("SNEAK_ATTACK_ACTIVE", 1);
            }
            else
            {
                player.SetLocalInt("SNEAK_ATTACK_ACTIVE", 2);
            }
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
        }

        public void OnRemoved(NWPlayer oPC)
        {
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
