using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.ForceUtility
{
    public class DarkSpread : IPerk
    {
        private readonly INWScript _;
        private readonly ICustomEffectService _customEffect;
        private readonly IPerkService _perk;

        public DarkSpread(INWScript script,
            ICustomEffectService customEffect,
            IPerkService perk)
        {
            _ = script;
            _customEffect = customEffect;
            _perk = perk;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return true;
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

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel)
        {
            NWCreature targetCreature = target.Object;

            int duration;
            int uses;
            float range;
            
            switch (perkLevel)
            {
                case 1:
                    duration = 30;
                    uses = 1;
                    range = 10f;
                    break;
                case 2:
                    duration = 30;
                    uses = 2;
                    range = 10f;
                    break;
                case 3:
                    duration = 60;
                    uses = 2;
                    range = 20f;
                    break;
                case 4:
                    duration = 60;
                    uses = 3;
                    range = 20f;
                    break;
                case 5:
                    duration = 60;
                    uses = 4;
                    range = 20f;
                    break;
                case 6:
                    duration = 60;
                    uses = 5;
                    range = 20f;
                    break;
                case 7: // Only available with background bonus
                    duration = 90;
                    uses = 5;
                    range = 20f;
                    break;
                default: return;
            }
            _customEffect.ApplyCustomEffect(player, targetCreature, CustomEffectType.DarkSpread, duration, perkLevel, uses + "," + range);
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
