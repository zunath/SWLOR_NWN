using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class PoisonTreatmentKit: IActionItem
    {
        public string CustomKey => "Medicine.PoisonTreatmentKit";

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin treating " + target.Name + "'s wounds...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            CustomEffectService.RemovePCCustomEffect(target.Object, CustomEffectType.Poison);

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

            int rank = SkillService.GetPCSkillRank(user.Object, SkillType.Medicine);
            
            if(target.IsPlayer){
                int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
                SkillService.GiveSkillXP(user.Object, SkillType.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);

            if (RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel(player, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = SkillService.GetPCSkillRank(player, SkillType.Medicine);
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
            return 3.5f + PerkService.GetCreaturePerkLevel(user.Object, PerkType.RangedHealing);
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int consumeChance = PerkService.GetCreaturePerkLevel(user.Object, PerkType.FrugalMedic) * 10;
            return RandomService.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsCreature || target.IsDM)
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

            if (CustomEffectService.DoesPCHaveCustomEffect(target.Object, CustomEffectType.Poison))
            {
                hasEffect = true;
            }

            if (!hasEffect)
            {
                return "This player is not diseased or poisoned.";
            }

            int rank = SkillService.GetPCSkillRank(user.Object, SkillType.Medicine);

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
