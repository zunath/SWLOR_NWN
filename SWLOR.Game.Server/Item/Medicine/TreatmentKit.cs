using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class TreatmentKit: IActionItem
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly ICustomEffectService _customEffect;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;

        public TreatmentKit(INWScript script,
            ISkillService skill,
            ICustomEffectService customEffect,
            IRandomService random,
            IPerkService perk)
        {
            _ = script;
            _skill = skill;
            _customEffect = customEffect;
            _random = random;
            _perk = perk;
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
                if (_.GetIsEffectValid(effect) == NWScript.TRUE)
                {
                    int effectType = _.GetEffectType(effect);
                    if (effectType == NWScript.EFFECT_TYPE_POISON || effectType == NWScript.EFFECT_TYPE_DISEASE)
                    {
                        _.RemoveEffect(target.Object, effect);
                    }
                }
            }

            user.SendMessage("You successfully treat " + target.Name + "'s infection.");

            PCSkill skill = _skill.GetPCSkill((NWPlayer)user, SkillType.Medicine);
            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, item.RecommendedLevel, skill.Rank);
            _skill.GiveSkillXP((NWPlayer)user, SkillType.Medicine, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);

            if (_random.Random(100) + 1 <= _perk.GetPCPerkLevel(player, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            PCSkill skill = _skill.GetPCSkill(player, SkillType.Medicine);
            return 12.0f - (skill.Rank + player.EffectiveMedicineBonus / 2) * 0.1f;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public int AnimationID()
        {
            return NWScript.ANIMATION_LOOPING_GET_MID;
        }

        public float MaxDistance()
        {
            return 3.5f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int consumeChance = _perk.GetPCPerkLevel((NWPlayer)user, PerkType.FrugalMedic) * 10;
            return _random.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (_.GetIsPC(target.Object) == NWScript.FALSE || _.GetIsDM(target.Object) == NWScript.TRUE)
            {
                return "Only players may be targeted with this item.";
            }

            bool hasEffect = false;
            foreach (Effect effect in target.Effects)
            {
                if (_.GetIsEffectValid(effect) == NWScript.TRUE)
                {
                    int effectType = _.GetEffectType(effect);
                    if (effectType == NWScript.EFFECT_TYPE_POISON || effectType == NWScript.EFFECT_TYPE_DISEASE)
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

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
