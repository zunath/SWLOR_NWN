using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Throwing
{
    public class PreciseToss: IPerkHandler
    {
        public PerkType PerkType => PerkType.PreciseToss;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellFeatID)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.Throwing)
                return "Must be equipped with a throwing weapon to use that ability.";

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
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int level, int spellFeatID)
        {
            int damage;
            int seconds;
            int dotDamage;

            switch (level)
            {
                case 1:
                    damage = RandomService.D4(1);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 2:
                    damage = RandomService.D8(1);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 3:
                    damage = RandomService.D8(2);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 5:
                    damage = RandomService.D8(3);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 6:
                    damage = RandomService.D8(4);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 7:
                    damage = RandomService.D8(5);
                    seconds = 12;
                    dotDamage = 3;
                    break;
                case 8:
                    damage = RandomService.D8(5);
                    seconds = 18;
                    dotDamage = 3;
                    break;
                case 9:
                    damage = RandomService.D8(6);
                    seconds = 24;
                    dotDamage = 3;
                    break;
                default: return;
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_PIERCING), target);
            CustomEffectService.ApplyCustomEffect(player, target.Object, CustomEffectType.Bleeding, seconds, level, Convert.ToString(dotDamage));


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

        public void OnConcentrationTick(NWPlayer player, int perkLevel, int spellFeatID)
        {
            throw new NotImplementedException();
        }
    }
}
