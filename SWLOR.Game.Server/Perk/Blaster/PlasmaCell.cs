using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class PlasmaCell: IPerk
    {
        private readonly INWScript _;
        private readonly ICustomEffectService _customEffect;

        public PlasmaCell(
            INWScript script,
            ICustomEffectService customEffect)
        {
            _ = script;
            _customEffect = customEffect;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.BlasterPistol;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a blaster pistol to use that ability.";
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
            _customEffect.ApplyCustomEffect(player, target.Object, CustomEffectType.PlasmaCell, 30, perkLevel, null);
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
