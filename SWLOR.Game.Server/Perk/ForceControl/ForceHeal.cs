using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceHeal: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceHeal;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
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

        public void OnConcentrationTick(NWPlayer player, NWObject target, int perkLevel, int tick)
        {
            int amount = 0;

            switch (perkLevel)
            {
                case 1: amount = 2; break;
                case 2: amount = 3; break;
                case 3: amount = 5; break;
                case 4: amount = 7; break;
                case 5: amount = 10; break;
            }

            // If target is at max HP, we do nothing else.
            int difference = target.MaxHP - target.CurrentHP;
            if (difference <= 0) return;

            // If we would heal the target for more than their max, reduce the amount healed to that number.
            if (amount > difference)
                amount = difference;

            // Apply the heal
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectHeal(amount), target);

            // Give Control XP
            SkillService.GiveSkillXP(player, SkillType.ForceControl, amount);
        }
    }
}
