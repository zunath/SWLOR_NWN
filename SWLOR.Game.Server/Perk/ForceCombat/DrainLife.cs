using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.ForceCombat
{
    public class DrainLife: IPerk
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IPlayerStatService _stat;
        private readonly ISkillService _skill;

        public DrainLife(
            INWScript script,
            IRandomService random,
            IPlayerStatService stat,
            ISkillService skill)
        {
            _ = script;
            _random = random;
            _stat = stat;
            _skill = skill;
        }

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
            int damage;
            float recoveryPercent;
            var effectiveStats = _stat.GetPlayerItemEffectiveStats(player);
            int darkBonus = effectiveStats.ForceCombat;
            int min = 1;
            int wisdom = player.WisdomModifier;
            int intelligence = player.IntelligenceModifier;
            min += darkBonus / 3 + intelligence / 2 + wisdom / 3;
            
            switch (perkLevel)
            {
                case 1:
                    damage = _random.D6(3, min);
                    recoveryPercent = 0.2f;
                    break;
                case 2:
                    damage = _random.D6(5, min);
                    recoveryPercent = 0.2f;
                    break;
                case 3:
                    damage = _random.D6(5, min);
                    recoveryPercent = 0.4f;
                    break;
                case 4:
                    damage = _random.D6(6, min);
                    recoveryPercent = 0.4f;
                    break;
                case 5:
                    damage = _random.D6(6, min);
                    recoveryPercent = 0.5f;
                    break;
                case 6: // Only available with background bonus
                    damage = _random.D6(7, min);
                    recoveryPercent = 0.5f;
                    break;
                default: return;
            }
            
            _.AssignCommand(player, () =>
            {
                int heal = (int)(damage * recoveryPercent);
                if (heal > target.CurrentHP) heal = target.CurrentHP;

                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectDamage(damage), target);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(heal), player);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(NWScript.VFX_BEAM_MIND), target, 1.0f);
            });
            
            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceCombat, target.Object);
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
