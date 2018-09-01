using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Shields
{
    public class ExpulsionManeuver : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;

        public ExpulsionManeuver(INWScript script,
            IPerkService perk,
            IRandomService random,
            IColorTokenService color)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _color = color;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.ExpulsionManeuver);
            float length;
            int ab;
            int chance;

            switch (perkLevel)
            {
                case 1:
                    length = 12.0f;
                    ab = 1;
                    chance = 10;
                    break;
                case 2:
                    length = 12.0f;
                    ab = 1;
                    chance = 20;
                    break;
                case 3:
                    length = 12.0f;
                    ab = 2;
                    chance = 20;
                    break;
                case 4:
                    length = 12.0f;
                    ab = 2;
                    chance = 30;
                    break;
                case 5:
                    length = 12.0f;
                    ab = 3;
                    chance = 30;
                    break;
                default:
                    return;
            }

            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + oPC.EffectiveLuckBonus;
            chance += luck;

            if (_random.Random(100) + 1 <= chance)
            {
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectAttackIncrease(ab), oPC.Object, length);
                oPC.SendMessage(_color.Combat("You perform a defensive maneuver."));
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
