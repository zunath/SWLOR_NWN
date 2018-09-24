using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Throwing
{
    public class PreciseToss: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ICustomEffectService _customEffect;

        public PreciseToss(
            INWScript script,
            IPerkService perk,
            IRandomService random,
            ICustomEffectService customEffect)
        {
            _perk = perk;
            _ = script;
            _random = random;
            _customEffect = customEffect;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.Throwing;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a throwing weapon to use that ability.";
        }

        public int FPCost(NWPlayer oPC, int baseFPCost)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer player, NWObject target, int level)
        {
            int damage;
            int seconds;
            int dotDamage;

            switch (level)
            {
                case 1:
                    damage = _random.D4(1);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 2:
                    damage = _random.D8(1);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 3:
                    damage = _random.D8(2);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 4:
                    damage = _random.D8(2);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 5:
                    damage = _random.D8(3);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 6:
                    damage = _random.D8(4);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 7:
                    damage = _random.D8(5);
                    seconds = 12;
                    dotDamage = 3;
                    break;
                case 8:
                    damage = _random.D8(5);
                    seconds = 18;
                    dotDamage = 3;
                    break;
                case 9:
                    damage = _random.D8(6);
                    seconds = 24;
                    dotDamage = 3;
                    break;
                default: return;
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_PIERCING), target);
            _customEffect.ApplyCustomEffect(player, target.Object, CustomEffectType.Bleeding, seconds, level, Convert.ToString(dotDamage));


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
