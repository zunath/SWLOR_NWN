﻿using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.Perk.General
{
    public class Dash: IPerkHandler
    {
        public PerkType PerkType => PerkType.Dash;

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return true;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
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
            float duration = 60.0f;
            int speed;

            switch (perkLevel)
            {
                case 1:
                    speed = 25;
                    break;
                case 2:
                    speed = 30;
                    break;
                case 3:
                    speed = 35;
                    break;
                case 4:
                    speed = 40;
                    break;
                case 5:
                    speed = 45;
                    break;
                case 6:
                    speed = 50;
                    break;
                case 7:
                    speed = 50;
                    duration = 120.0f;
                    break;
                default: return;
            }

            if (player.DexterityModifier > 0)
            {
                duration = duration + player.DexterityModifier * 5;
            }

            Effect movement = _.EffectMovementSpeedIncrease(speed);
            movement = _.TagEffect(movement, "DASH");

            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, movement, target, duration);
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
