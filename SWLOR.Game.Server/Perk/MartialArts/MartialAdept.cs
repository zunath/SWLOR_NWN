using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class MartialAdept: IPerk
    {
        private readonly INWScript _;
        private readonly IRandomService _random;

        public MartialAdept(
            INWScript script,
            IRandomService random)
        {
            _ = script;
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

            switch (perkLevel)
            {
                case 1:
                    damage = _random.D4(1);
                    break;
                case 2:
                    damage = _random.D4(2);
                    break;
                case 3:
                    damage = _random.D6(3);
                    break;
                case 4:
                    damage = _random.D6(3);
                    break;
                case 5:
                    damage = _random.D6(4);
                    break;
                case 6:
                    damage = _random.D8(5);
                    break;
                case 7:
                    damage = _random.D8(6);
                    break;
                case 8:
                    damage = _random.D8(7);
                    break;
                case 9:
                    damage = _random.D12(7);
                    break;
                case 10:
                    damage = _random.D12(8);
                    break;
                default: return;
            }
            
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
