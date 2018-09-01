using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.DarkSide
{
    public class Chainspell: IPerk
    {
        private readonly ICustomEffectService _customEffect;
        private readonly IPerkService _perk;
        private readonly INWScript _;

        public Chainspell(ICustomEffectService customEffect,
            IPerkService perk,
            INWScript script)
        {
            _customEffect = customEffect;
            _perk = perk;
            _ = script;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.Chest.CustomItemType == CustomItemType.ForceArmor;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You must be equipped with force armor to activate that ability.";
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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Chainspell);
            int ticks = 10; // 60 seconds base

            if (perkLevel >= 2)
                ticks += 4; // +24 seconds (84 total)
            if (perkLevel >= 3)
                ticks += 4; // +24 seconds (108 total)

            _customEffect.ApplyCustomEffect(oPC, oPC, CustomEffectType.Chainspell, ticks, perkLevel);

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(NWScript.VFX_IMP_EVIL_HELP), oPC.Object, 6.1f);
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
