using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class ForcePush: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForcePush;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellFeatID)
        {
            int size = _.GetCreatureSize(oTarget);
            int maxSize = _.CREATURE_SIZE_INVALID;
            switch ((CustomFeatType) spellFeatID)
            {
                case CustomFeatType.ForcePush1:
                    maxSize = _.CREATURE_SIZE_SMALL;
                    break;
                case CustomFeatType.ForcePush2:
                    maxSize = _.CREATURE_SIZE_MEDIUM;
                    break;
                case CustomFeatType.ForcePush3:
                    maxSize = _.CREATURE_SIZE_LARGE;
                    break;
                case CustomFeatType.ForcePush4:
                    maxSize = _.CREATURE_SIZE_HUGE;
                    break;
            }

            if (size > maxSize)
                return "Your target is too large to force push.";

            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            switch ((CustomFeatType) spellFeatID)
            {
                case CustomFeatType.ForcePush1: return 4;
                case CustomFeatType.ForcePush2: return 6;
                case CustomFeatType.ForcePush3: return 8;
                case CustomFeatType.ForcePush4: return 10;
            }

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
            float duration = 0.0f;

            switch ((CustomFeatType) spellFeatID)
            {
                case CustomFeatType.ForcePush1:
                    duration = 6f;
                    break;
                case CustomFeatType.ForcePush2:
                    duration = 12f;
                    break;
                case CustomFeatType.ForcePush3:
                    duration = 18f;
                    break;
                case CustomFeatType.ForcePush4:
                    duration = 24f;
                    break;
            }

            var result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Universal);

            // Resisted - Only apply slow for six seconds
            if (result.IsResisted)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, _.EffectSlow(), target, 6.0f);
            }

            // Not resisted - Apply knockdown for the specified duration
            else
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target, duration);
            }

            SkillService.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceAlter, target.Object);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_COM_BLOOD_SPARK_SMALL), target);
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
