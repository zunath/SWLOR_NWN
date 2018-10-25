using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.DarkSide
{
    public class ForceLeech : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IPlayerStatService _stat;
        private readonly ICustomEffectService _customEffect;
        private readonly ISkillService _skill;


        public ForceLeech(
            INWScript script,
            IPerkService perk,
            IRandomService random,
            IPlayerStatService stat,
            ICustomEffectService customEffect,
            ISkillService skill)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _stat = stat;
            _customEffect = customEffect;
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

        public void OnImpact(NWPlayer player, NWObject target, int level)
        {
            float recoveryPercent;
            var effectiveStats = _stat.GetPlayerItemEffectiveStats(player);
            int darkBonus = effectiveStats.DarkAbility;
            int length;
            int dotAmount;
            int wisdom = player.WisdomModifier;
            int intelligence = player.IntelligenceModifier;


            switch (level)
            {
                case 1:
                    length = 6;
                    dotAmount = 2;
                    recoveryPercent = 0.5f;
                    break;
                case 2:
                    length = 9;
                    dotAmount = 2;
                    recoveryPercent = 0.5f;
                    break;
                case 3:
                    length = 6;
                    dotAmount = 4;
                    recoveryPercent = 0.5f;
                    break;
                case 4:
                    length = 6;
                    dotAmount = 4;
                    recoveryPercent = 0.75f;
                    break;
                case 5:
                    length = 9;
                    dotAmount = 4;
                    recoveryPercent = 0.75f;
                    break;
                case 6:
                    length = 9;
                    dotAmount = 6;
                    recoveryPercent = 0.75f;
                    break;
                case 7:
                    length = 12;
                    dotAmount = 8;
                    recoveryPercent = 0.5f;
                    break;
                case 8:
                    length = 12;
                    dotAmount = 8;
                    recoveryPercent = 0.75f;
                    break;
                case 9:
                    length = 15;
                    dotAmount = 12;
                    recoveryPercent = 0.5f;
                    break;
                case 10:
                    length = 15;
                    dotAmount = 12;
                    recoveryPercent = 0.75f;
                    break;
                case 11: // Only available with background bonus
                    length = 18;
                    dotAmount = 12;
                    recoveryPercent = 1.0f;
                    break;
                default: return;
            }

            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.DarkSideAbilities);

            if (length > 0.0f && dotAmount > 0)
            {
                _customEffect.ApplyCustomEffect(player, target.Object, CustomEffectType.ForceLeech, length, level, dotAmount + "," + recoveryPercent);

            }

            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.DarkSideAbilities);

            Effect vfx = _.EffectVisualEffect(VFX_IMP_PULSE_NEGATIVE);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, target);
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

