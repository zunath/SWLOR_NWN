using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.CustomEffect;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.General
{
    public class Rest : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly ICustomEffectService _customEffect;

        public Rest(INWScript script,
            IPerkService perk,
            ICustomEffectService customEffect)
        {
            _ = script;
            _perk = perk;
            _customEffect = customEffect;
        }


        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return RestEffect.CanRest(oPC);
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You cannot rest while you or a party member are in combat.";
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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Rest);

            switch (perkLevel)
            {
                case 1: return 300.0f;
                case 2: return 270.0f;
                case 3:
                case 4:
                    return 240.0f;
                case 5:
                    return 210.0f;
                case 6:
                case 7:
                    return 180.0f;
                default: return 300.0f;
            }
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel)
        {
            _customEffect.ApplyCustomEffect(player, player, CustomEffectType.Rest, -1, 0, null);
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
