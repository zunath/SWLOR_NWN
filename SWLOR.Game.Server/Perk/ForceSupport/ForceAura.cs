using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceSupport
{
    public class ForceAura: IPerk
    {
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            if (_.GetDistanceBetween(oPC, oTarget) > 15.0f)
                return false;

            return true;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Target out of range.";
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

        public void OnImpact(NWPlayer player, NWObject target, int level, int spellFeatID)
        {
            int ticks;
            var spread = CustomEffectService.GetForceSpreadDetails(player);

            switch (level)
            {
                default:
                    ticks = 300;
                    break;
                case 5:
                case 6:
                    ticks = 600;
                    break;
            }

            int itemPotency = CombatService.CalculateItemPotencyBonus(player.Object, ForceAbilityType.Light);
            int basePotency = (int) (player.Wisdom + player.Intelligence * 0.5f + player.Charisma * 0.25f);
            int finalPotency = itemPotency + basePotency;
            ticks += finalPotency * 10; // +10 seconds per potency


            // Force Spread isn't active. This is a single target cast.
            if (spread.Level <= 0)
            {
                CustomEffectService.ApplyCustomEffect(player, target.Object, CustomEffectType.ForceAura, ticks, level, null);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_AC_BONUS), target);
            }
            // Force Spread is active. Target nearby party members.
            else
            {
                spread.Uses--;
                var members = player.PartyMembers.Where(x => _.GetDistanceBetween(x, target) <= spread.Range ||
                                                             Equals(x, target));

                foreach (var member in members)
                {
                    CustomEffectService.ApplyCustomEffect(member, target.Object, CustomEffectType.ForceAura, ticks, level, null);
                    _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_AC_BONUS), member);
                }

                _.PlaySound("v_pro_frcaura");
                CustomEffectService.SetForceSpreadUses(player, spread.Uses);
                SkillService.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceUtility, null);
            }
            
            SkillService.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceSupport, null);
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
