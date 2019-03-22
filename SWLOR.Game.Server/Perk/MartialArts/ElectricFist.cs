using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class ElectricFist: IPerk
    {
        
        private readonly IRandomService _random;

        public ElectricFist(
            
            IRandomService random)
        {
            
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return !oPC.RightHand.IsValid && !oPC.LeftHand.IsValid;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a power glove in order to use that ability.";
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
            float duration;

            switch (perkLevel)
            {
                case 1:
                    damage = _random.D8(1);
                    duration = 3;
                    break;
                case 2:
                    damage = _random.D8(2);
                    duration = 3;
                    break;
                case 3:
                    damage = _random.D8(3);
                    duration = 3;
                    break;
                case 4:
                    damage = _random.D8(3);
                    duration = 6;
                    break;
                case 5:
                    damage = _random.D8(4);
                    duration = 6;
                    break;
                case 6:
                    damage = _random.D8(5);
                    duration = 6;
                    break;
                case 7:
                    damage = _random.D8(6);
                    duration = 6;
                    break;
                case 8:
                    damage = _random.D8(7);
                    duration = 6;
                    break;
                case 9:
                    damage = _random.D8(7);
                    duration = 9;
                    break;
                case 10:
                    damage = _random.D8(8);
                    duration = 9;
                    break;
                default: return;
            }
            
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectStunned(), target, duration);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_ELECTRICAL), target);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_SUNSTRIKE), target);
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
