﻿using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class Knockdown: IPerkHandler
    {
        public PerkType PerkType => PerkType.Knockdown;

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            NWItem weapon = oPC.RightHand;
            return weapon.CustomItemType == CustomItemType.Baton;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You must be equipped with a baton weapon to use Knockdown.";
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
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            int damage;
            float length;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D4(1);
                    length = 6.0f;
                    break;
                case 2:
                    damage = RandomService.D4(2);
                    length = 6.0f;
                    break;
                case 3:
                    damage = RandomService.D6(2);
                    length = 6.0f;
                    break;
                case 4:
                    damage = RandomService.D6(2);
                    length = 9.0f;
                    break;
                case 5:
                    damage = RandomService.D6(3);
                    length = 9.0f;
                    break;
                case 6:
                    damage = RandomService.D8(3);
                    length = 9.0f;
                    break;
                default: return;
            }

            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target.Object, length);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_BLUDGEONING), target);
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
