using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.DarkSide
{
    public class DrainLife: IPerk
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IPlayerStatService _stat;

        public DrainLife(
            INWScript script,
            IRandomService random,
            IPlayerStatService stat)
        {
            _ = script;
            _random = random;
            _stat = stat;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
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
            int darkBonus = _stat.EffectiveDarkAbilityBonus(player);
            int min = 1;
            int wisdom = player.WisdomModifier;
            int intelligence = player.IntelligenceModifier;
            min += darkBonus / 3 + intelligence / 2 + wisdom / 3;

            BackgroundType background = (BackgroundType)player.Class1;

            if (background == BackgroundType.Sorcerer ||
                background == BackgroundType.Corrupter)
            {
                perkLevel++;
            }

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
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage), target);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(heal), player);
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_BEAM_MIND), target, 1.0f);
            });

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
