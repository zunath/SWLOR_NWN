using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class AbsorptionField: IPerk
    {
        private readonly INWScript _;
        private readonly ICustomEffectService _customEffect;
        private readonly IPerkService _perk;

        public AbsorptionField(
            INWScript script,
            ICustomEffectService customEffect,
            IPerkService perk)
        {
            _ = script;
            _customEffect = customEffect;
            _perk = perk;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            NWItem armor = oPC.Chest;
            return armor.CustomItemType == CustomItemType.ForceArmor;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You must be equipped with force armor to use that combat ability.";
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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.AbsorptionField);
            _customEffect.ApplyCustomEffect(oPC, oPC, CustomEffectType.AbsorptionField, 20, perkLevel, null);

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_GLOBE_USE), oTarget);
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
