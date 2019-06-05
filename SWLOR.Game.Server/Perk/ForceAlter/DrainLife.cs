using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceAlter
{
    public class DrainLife: IPerkHandler
    {
        public PerkType PerkType => PerkType.DrainLife;
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            if (!oTarget.IsCreature)
                return "This ability can only be used on living creatures.";
            NWCreature targetCreature = oTarget.Object;
            if (targetCreature.RacialType == (int)CustomRaceType.Robot)
                return "This ability cannot be used on droids.";

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

        public void OnConcentrationTick(NWPlayer player, NWObject target, int spellTier, int tick)
        {
            int amount;

            switch (spellTier)
            {
                case 1:
                    amount = 5;
                    break;
                case 2:
                    amount = 6;
                    break;
                case 3:
                    amount = 7;
                    break;
                case 4:
                    amount = 8;
                    break;
                case 5:
                    amount = 10;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            var result = CombatService.CalculateAbilityResistance(player, target.Object, SkillType.ForceAlter, ForceBalanceType.Dark);

            // +/- percent change based on resistance
            float delta = 0.01f * result.Delta;
            amount = amount + (int)(amount * delta);

            player.AssignCommand(() =>
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectDamage(amount, _.DAMAGE_TYPE_NEGATIVE), target);
            });

            // Only apply a heal if caster is not at max HP. Otherwise they'll get unnecessary spam.
            if (player.CurrentHP < player.MaxHP)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectHeal(amount), player);
            }
            SkillService.RegisterPCToNPCForSkill(player, target, SkillType.ForceAlter);
        }
    }
}
