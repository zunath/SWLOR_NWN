using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.ForceCombat
{
    public class ForcePush: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IPlayerStatService _playerStat;
        private readonly ISkillService _skill;

        public ForcePush(INWScript script,
            IPerkService perk,
            IRandomService random,
            IPlayerStatService playerStat,
            ISkillService skill)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _playerStat = playerStat;
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
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            int lightBonus = effectiveStats.ForceSupport;
            int min = 1;
            float length;
            int damage;
            int wisdom = player.WisdomModifier;
            int intelligence = player.IntelligenceModifier;
            min += lightBonus / 4 + wisdom / 3 + intelligence / 4;

            switch (level)
            {
                case 1:
                    damage = _random.D4(1, min);
                    length = 3;
                    break;
                case 2:
                    damage = _random.D4(1, min);
                    length = 6;
                    break;
                case 3:
                    damage = _random.D6(1, min);
                    length = 6;
                    break;
                case 4:
                    damage = _random.D8(1, min);
                    length = 6;
                    break;
                case 5:
                    damage = _random.D8(1, min);
                    length = 9;
                    break;
                case 6: // Only available with background perk
                    damage = _random.D12(1, min);
                    length = 9;
                    break;

                default: return;
            }
            
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                player.SendMessage("Lucky force push!");
            }

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectDamage(damage, NWScript.DAMAGE_TYPE_POSITIVE), target);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target, length);
            _skill.RegisterPCToNPCForSkill(player, target, SkillType.ForceCombat);
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
            return true;
        }
    }
}
