using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class Knockdown: IPerkHandler
    {
        public PerkType PerkType => PerkType.Knockdown;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            NWItem weapon = oPC.RightHand;
            if (weapon.CustomItemType != CustomItemType.Baton)
                return "You must be equipped with a baton weapon to use that ability.";

            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellTier)
        {
            int damage;
            float length;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D4(1);
                    length = 6.0f;
                    break;
                case 2:
                    damage = RandomService.D4(2);
                    length = 6.0f;
                    break;
                case 3:
                    damage = RandomService.D6(2);
                    length = 6.0f;
                    break;
                case 4:
                    damage = RandomService.D6(2);
                    length = 9.0f;
                    break;
                case 5:
                    damage = RandomService.D6(3);
                    length = 9.0f;
                    break;
                case 6:
                    damage = RandomService.D8(3);
                    length = 9.0f;
                    break;
                default: return;
            }

            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target.Object, length);
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

        public void OnConcentrationTick(NWPlayer player, int perkLevel, int tick)
        {
            
        }
    }
}
