using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.TwoHanded
{
    public class Knockdown: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public Knockdown(INWScript script,
            IPerkService perk,
            IRandomService random)
        {
            _ = script;
            _perk = perk;
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            NWItem weapon = oPC.RightHand;
            return weapon.CustomItemType == CustomItemType.HeavyBlunt;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You must be equipped with a heavy blunt weapon to use Knockdown.";
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

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Knockdown);
            int chance;
            float length;

            switch (perkLevel)
            {
                case 1:
                    chance = 10;
                    length = 3.0f;
                    break;
                case 2:
                    chance = 20;
                    length = 3.0f;
                    break;
                case 3:
                    chance = 20;
                    length = 6.0f;
                    break;
                case 4:
                    chance = 30;
                    length = 6.0f;
                    break;
                case 5:
                    chance = 40;
                    length = 6.0f;
                    break;
                case 6:
                    chance = 50;
                    length = 6.0f;
                    break;
                default: return;
            }

            if (_random.Random(100) + 1 <= chance)
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), oTarget.Object, length);
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
