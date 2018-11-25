using NWN;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class PoisonTreatmentKit: IActionItem
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly ICustomEffectService _customEffect;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IPlayerStatService _playerStat;

        public PoisonTreatmentKit(INWScript script,
            ISkillService skill,
            ICustomEffectService customEffect,
            IRandomService random,
            IPerkService perk,
            IPlayerStatService playerStat)
        {
            _ = script;
            _skill = skill;
            _customEffect = customEffect;
            _random = random;
            _perk = perk;
            _playerStat = playerStat;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin treating " + target.Name + "'s wounds...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            _customEffect.RemovePCCustomEffect((NWPlayer)target, CustomEffectType.Poison);

            foreach (Effect effect in target.Effects)
            {
                if (_.GetIsEffectValid(effect) == TRUE)
                {
                    int effectType = _.GetEffectType(effect);
                    if (effectType == EFFECT_TYPE_POISON || effectType == EFFECT_TYPE_DISEASE)
                    {
                        _.RemoveEffect(target.Object, effect);
                    }
                }
            }

            user.SendMessage("You successfully treat " + target.Name + "'s infection.");

            int rank = _skill.GetPCSkillRank((NWPlayer)user, SkillType.Medicine);
            
            if(target.IsPlayer){
                int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
                _skill.GiveSkillXP((NWPlayer)user, SkillType.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);

            if (_random.Random(100) + 1 <= _perk.GetPCPerkLevel(player, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = _skill.GetPCSkillRank(player, SkillType.Medicine);
            return 12.0f - (rank + effectiveStats.Medicine / 2) * 0.1f;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public int AnimationID()
        {
            return ANIMATION_LOOPING_GET_MID;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 3.5f + _perk.GetPCPerkLevel(user.Object, PerkType.RangedHealing);
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int consumeChance = _perk.GetPCPerkLevel((NWPlayer)user, PerkType.FrugalMedic) * 10;
            return _random.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
             if (target.IsCreature && !target.IsDM)
            {
                return "Only creatures may be targeted with this item.";
            }

            bool hasEffect = false;
            foreach (Effect effect in target.Effects)
            {
                if (_.GetIsEffectValid(effect) == TRUE)
                {
                    int effectType = _.GetEffectType(effect);
                    if (effectType == EFFECT_TYPE_POISON || effectType == EFFECT_TYPE_DISEASE)
                    {
                        hasEffect = true;
                    }
                }
            }

            if (_customEffect.DoesPCHaveCustomEffect((NWPlayer)target, CustomEffectType.Poison))
            {
                hasEffect = true;
            }

            if (!hasEffect)
            {
                return "This player is not diseased or poisoned.";
            }

            int rank = _skill.GetPCSkillRank(user.Object, SkillType.Medicine);

            if (rank < item.RecommendedLevel)
            {
                return "Your skill level is too low to use this item.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
