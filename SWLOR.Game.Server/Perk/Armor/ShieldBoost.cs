using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class ShieldBoost: IPerk
    {
        
        private readonly IPerkService _perk;
        private readonly ICustomEffectService _customEffect;

        public ShieldBoost(
            
            IPerkService perk,
            ICustomEffectService customEffect)
        {
            
            _perk = perk;
            _customEffect = customEffect;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.Chest.CustomItemType == CustomItemType.HeavyArmor;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with heavy armor to use that ability.";
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
            int duration = 60;

            var vfx = _.EffectVisualEffect(VFX_DUR_BLUR);
            vfx = _.TagEffect(vfx, "SHIELD_BOOST_VFX");

            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, vfx, target, duration);
            _customEffect.ApplyCustomEffect(player, target.Object, CustomEffectType.ShieldBoost, duration, perkLevel, null);
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
