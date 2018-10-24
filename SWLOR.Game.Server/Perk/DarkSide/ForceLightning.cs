using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.DarkSide
{
    public class ForceLightning : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _playerStat;

        public ForceLightning(INWScript script,
            IPerkService perk,
            IRandomService random,
            ISkillService skill,
            ICustomEffectService customEffect,
            IPlayerStatService playerStat)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _skill = skill;
            _customEffect = customEffect;
            _playerStat = playerStat;
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
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int darkBonus = effectiveStats.DarkAbility;
            int amount;
            int length;
            int dotAmount;
            int min = 1;

            int wisdom = player.WisdomModifier;
            int intelligence = player.IntelligenceModifier;
            min += darkBonus / 3 + intelligence / 2 + wisdom / 3;

            switch (level)
            {
                case 1:
                    amount = _random.D6(2, min);
                    length = 0;
                    dotAmount = 0;
                    break;
                case 2:
                    amount = _random.D6(2, min);
                    length = 6;
                    dotAmount = 1;
                    break;
                case 3:
                    amount = _random.D12(2, min);
                    length = 6;
                    dotAmount = 1;
                    break;
                case 4:
                    amount = _random.D12(2, min);
                    length = 12;
                    dotAmount = 1;
                    break;
                case 5:
                    amount = _random.D12(2, min);
                    length = 6;
                    dotAmount = 2;
                    break;
                case 6:
                    amount = _random.D12(2, min);
                    length = 12;
                    dotAmount = 2;
                    break;
                case 7:
                    amount = _random.D12(3, min);
                    length = 12;
                    dotAmount = 2;
                    break;
                case 8:
                    amount = _random.D12(3, min);
                    length = 6;
                    dotAmount = 4;
                    break;
                case 9:
                    amount = _random.D12(4, min);
                    length = 6;
                    dotAmount = 4;
                    break;
                case 10:
                    amount = _random.D12(4, min);
                    length = 12;
                    dotAmount = 4;
                    break;
                case 11: // Only attainable with background bonus
                    amount = _random.D12(5, min);
                    length = 12;
                    dotAmount = 4;
                    break;
                default: return;
            }

            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                player.SendMessage("Lucky force lightning!");
            }

            player.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, target);
            });

            if (length > 0.0f && dotAmount > 0)
            {
                _customEffect.ApplyCustomEffect(player, target.Object, CustomEffectType.ForceShock, length, level, dotAmount.ToString());
            }

            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.DarkSideAbilities, target.Object);

            Effect vfx = _.EffectVisualEffect(VFX_IMP_LIGHTNING_S);
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
