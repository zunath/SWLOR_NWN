using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceCombat
{
    public class ForcePush: IPerk
    {
        
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IPlayerStatService _playerStat;
        private readonly ISkillService _skill;
        private readonly ICombatService _combat;

        public ForcePush(
            
            IPerkService perk,
            IRandomService random,
            IPlayerStatService playerStat,
            ISkillService skill,
            ICombatService combat)
        {
            
            _perk = perk;
            _random = random;
            _playerStat = playerStat;
            _skill = skill;
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
            float length;
            int damage;
            
            switch (level)
            {
                case 1:
                    damage = _random.D4(1);
                    length = 3;
                    break;
                case 2:
                    damage = _random.D4(1);
                    length = 6;
                    break;
                case 3:
                    damage = _random.D6(1);
                    length = 6;
                    break;
                case 4:
                    damage = _random.D8(1);
                    length = 6;
                    break;
                case 5:
                    damage = _random.D8(1);
                    length = 9;
                    break;

                default: return;
            }
            _skill.RegisterPCToNPCForSkill(player, target, SkillType.ForceCombat);
            
            // Resistance affects length for this perk.
            ForceResistanceResult resistance = _combat.CalculateResistanceRating(player, target.Object, ForceAbilityType.Mind);
            length = length * resistance.Amount;

            if (length <= 0.0f || resistance.Type != ResistanceType.Zero)
            {
                player.SendMessage("Your Force Push effect was resisted.");
                return;
            }

            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                player.SendMessage("Lucky force push!");
            }

            _.PlaySound("v_imp_frcpush");
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_POSITIVE), target);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target, length);
            _combat.AddTemporaryForceDefense(target.Object, ForceAbilityType.Light);
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
