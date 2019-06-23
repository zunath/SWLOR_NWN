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

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.Throwing)
                return "Must be equipped with a throwing weapon to use that ability.";

            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int level, int spellTier)
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
            CustomEffectService.ApplyCustomEffect(creature, target.Object, CustomEffectType.Bleeding, seconds, level, Convert.ToString(dotDamage));


        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            throw new NotImplementedException();
        }
    }
}
