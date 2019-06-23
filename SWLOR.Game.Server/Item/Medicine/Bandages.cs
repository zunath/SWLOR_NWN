using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class Bandages: IActionItem
    {
        public string CustomKey => "Medicine.Bandages";

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin treating " + target.Name + "'s wounds...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);

            CustomEffectService.RemovePCCustomEffect(target.Object, CustomEffectType.Bleeding);
            player.SendMessage("You finish bandaging " + target.Name + "'s wounds.");
            
            int rank = SkillService.GetPCSkillRank(player, SkillType.Medicine);
            
            int healAmount = 2 + effectiveStats.Medicine / 2;
            healAmount += item.MedicineBonus;
            if (rank >= item.RecommendedLevel && item.MedicineBonus > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(healAmount), target);
            }
            if(target.IsPlayer){
                int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, item.RecommendedLevel, rank);
                SkillService.GiveSkillXP(player, SkillType.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if (RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel(user.Object, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = SkillService.GetPCSkillRank(user.Object, SkillType.Medicine);
            float seconds = 6.0f - (rank * 0.2f);
            if (seconds < 1.0f) seconds = 1.0f;
            return seconds;
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
            return true;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsCreature || target.IsDM)
            {
                return "Only creatures may be targeted with this item.";
            }

            if (target.CurrentHP >= 40)
            {
                return "Bandages cannot be used on targets with more than 40 HP.";
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
