using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class ShieldBoost: IPerkHandler
    {
        public PerkType PerkType => PerkType.ShieldBoost;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.Chest.CustomItemType != CustomItemType.HeavyArmor)
                return "Must be equipped with heavy armor to use that ability.";

            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int duration = 60;

            var vfx = _.EffectVisualEffect(VisualEffect.Dur_Blur);
            vfx = _.TagEffect(vfx, "SHIELD_BOOST_VFX");

            _.ApplyEffectToObject(DurationType.Temporary, vfx, target, duration);
            CustomEffectService.ApplyCustomEffect(creature, target.Object, CustomEffectType.ShieldBoost, duration, perkLevel, null);
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
