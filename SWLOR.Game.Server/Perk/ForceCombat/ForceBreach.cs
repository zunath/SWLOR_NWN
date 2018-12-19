using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.ForceCombat
{
    public class ForceBreach : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _playerStat;
        private readonly ICombatService _combat;

        public ForceBreach(
            INWScript script,
            IPerkService perk,
            IRandomService random,
            ISkillService skill,
            ICustomEffectService customEffect,
            IPlayerStatService playerStat,
            ICombatService combat)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _skill = skill;
            _customEffect = customEffect;
            _playerStat = playerStat;
            _combat = combat;
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
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int length;
            int dotAmount;
            
            int basePotency;
            const float Tier1Modifier = 2.2f;
            const float Tier2Modifier = 1.6f;
            const float Tier3Modifier = 1.0f;
            const float Tier4Modifier = 0;

            switch (level)
            {
                case 1:
                    basePotency = 15;
                    length = 0;
                    dotAmount = 0;
                    break;
                case 2:
                    basePotency = 20;
                    length = 6;
                    dotAmount = 4;
                    break;
                case 3:
                    basePotency = 25;
                    length = 6;
                    dotAmount = 6;
                    break;
                case 4:
                    basePotency = 40;
                    length = 12;
                    dotAmount = 6;
                    break;
                case 5:
                    basePotency = 50;
                    length = 12;
                    dotAmount = 6;
                    break;
                case 6:
                    basePotency = 60;
                    length = 12;
                    dotAmount = 6;
                    break;
                case 7:
                    basePotency = 70;
                    length = 12;
                    dotAmount = 6;
                    break;
                case 8:
                    basePotency = 80;
                    length = 12;
                    dotAmount = 8;
                    break;
                case 9:
                    basePotency = 90;
                    length = 12;
                    dotAmount = 8;
                    break;
                case 10:
                    basePotency = 100;
                    length = 12;
                    dotAmount = 10;
                    break;
                default: return;
            }

            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                player.SendMessage("Lucky force breach!");
            }

            var calc = _combat.CalculateForceDamage(
                player,
                target.Object,
                ForceAbilityType.Light,
                basePotency,
                Tier1Modifier,
                Tier2Modifier,
                Tier3Modifier,
                Tier4Modifier);

            player.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(calc.Damage);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, target);
            });
            
            if (length > 0.0f && dotAmount > 0)
            {
                _customEffect.ApplyCustomEffect(player, target.Object, CustomEffectType.ForceBreach, length, level, null);
            }

            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceCombat, target.Object);

            player.AssignCommand(() =>
            {
                Effect vfx = _.EffectVisualEffect(VFX_IMP_DOMINATE_S);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, target);
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
