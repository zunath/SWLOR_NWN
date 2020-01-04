using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static NWN._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class HealingKit: IActionItem
    {
        public string CustomKey => "Medicine.HealingKit";

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin treating " + target.Name + "'s wounds...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            target.RemoveEffect(EffectType.Regenerate);
            int rank = SkillService.GetPCSkillRank(player, Skill.Medicine);
            int luck = PerkService.GetCreaturePerkLevel(player, PerkType.Lucky);
            int perkDurationBonus = PerkService.GetCreaturePerkLevel(player, PerkType.HealingKitExpert) * 6 + (luck * 2);
            float duration = 30.0f + (rank * 0.4f) + perkDurationBonus;
            int restoreAmount = 1 + item.GetLocalInt("HEALING_BONUS") + effectiveStats.Medicine + item.MedicineBonus;
            int delta = item.RecommendedLevel - rank;
            float effectivenessPercent = 1.0f;

            if (delta > 0)
            {
                effectivenessPercent = effectivenessPercent - (delta * 0.1f);
            }

            restoreAmount = (int)(restoreAmount * effectivenessPercent);

            int perkBlastBonus = PerkService.GetCreaturePerkLevel(player, PerkType.ImmediateImprovement);
            if (perkBlastBonus > 0)
            {
                int blastHeal = restoreAmount * perkBlastBonus;
                if (RandomService.Random(100) + 1 <= luck / 2)
                {
                    blastHeal *= 2;
                }
                _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(blastHeal), target.Object);
            }

            float interval = 6.0f;
            ClassType background = (ClassType) player.Class1;

            if (background == ClassType.Medic)
                interval *= 0.5f;

            _.PlaySound("use_bacta");
            Effect regeneration = _.EffectRegenerate(restoreAmount, interval);
            _.ApplyEffectToObject(DurationType.Temporary, regeneration, target.Object, duration);
            player.SendMessage("You successfully treat " + target.Name + "'s wounds. The healing kit will expire in " + duration + " seconds.");
            
            _.DelayCommand(duration + 0.5f, () => { player.SendMessage("The healing kit that you applied to " + target.Name + " has expired."); });

            if(target.IsPlayer){
                int xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
                SkillService.GiveSkillXP(player, Skill.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if ( RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = SkillService.GetPCSkillRank((NWPlayer)user, Skill.Medicine);
            return 12.0f - (rank * 0.1f);
        }

        public bool FaceTarget()
        {
            return true;
        }


        public Animation AnimationType()
        {
            return Animation.Get_Mid;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 3.5f + PerkService.GetCreaturePerkLevel(user.Object, PerkType.RangedHealing);
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int consumeChance = PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.FrugalMedic) * 10;
            ClassType background = (ClassType) user.Class1;

            if (background == ClassType.Medic)
            {
                consumeChance += 5;
            }


            return RandomService.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsCreature || target.IsDM)
            {
                return "Only creatures may be targeted with this item.";
            }

            if (target.CurrentHP >= target.MaxHP)
            {
                return "Your target is not hurt.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
