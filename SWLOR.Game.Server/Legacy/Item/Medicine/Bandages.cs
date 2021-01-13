using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Item.Contracts;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Item.Medicine
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

            //CustomEffectService.RemovePCCustomEffect(target.Object, CustomEffectType.Bleeding);
            player.SendMessage("You finish bandaging " + target.Name + "'s wounds.");
            
            var rank = SkillService.GetPCSkillRank(player, SkillType.Medicine);
            
            var healAmount = 2 + effectiveStats.Medicine / 2;
            healAmount += item.MedicineBonus;
            if (rank >= item.RecommendedLevel && item.MedicineBonus > 0)
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(healAmount), target);
            }
            if(target.IsPlayer){
                var xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, item.RecommendedLevel, rank);
                SkillService.GiveSkillXP(player, SkillType.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if (SWLOR.Game.Server.Service.Random.Next(100) + 1 <= PerkService.GetCreaturePerkLevel(user.Object, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            var rank = SkillService.GetPCSkillRank(user.Object, SkillType.Medicine);
            var seconds = 6.0f - (rank * 0.2f);
            if (seconds < 1.0f) seconds = 1.0f;
            return seconds;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public Animation AnimationID()
        {
            return Animation.LoopingGetMid;
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

            var rank = SkillService.GetPCSkillRank(user.Object, SkillType.Medicine);
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
