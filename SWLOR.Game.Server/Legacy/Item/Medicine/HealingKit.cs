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
            target.RemoveEffect(EffectTypeScript.Regenerate);
            var rank = SkillService.GetPCSkillRank(player, SkillType.Medicine);
            var luck = PerkService.GetCreaturePerkLevel(player, PerkType.Lucky);
            var perkDurationBonus = PerkService.GetCreaturePerkLevel(player, PerkType.HealingKitExpert) * 6 + (luck * 2);
            var duration = 30.0f + (rank * 0.4f) + perkDurationBonus + item.MedicineBonus;
            var restoreAmount = 1 + item.GetLocalInt("HEALING_BONUS") + effectiveStats.Medicine + (rank / 10);
            var delta = item.RecommendedLevel - rank;
            var effectivenessPercent = 1.0f;

            if (delta > 0)
            {
                effectivenessPercent = effectivenessPercent - (delta * 0.1f);
            }

            restoreAmount = (int)(restoreAmount * effectivenessPercent);

            var perkBlastBonus = PerkService.GetCreaturePerkLevel(player, PerkType.ImmediateImprovement);
            if (perkBlastBonus > 0)
            {
                var blastHeal = restoreAmount * perkBlastBonus;
                if (RandomService.Random(100) + 1 <= luck / 2)
                {
                    blastHeal *= 2;
                }
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(blastHeal), target.Object);
            }

            var interval = 6.0f;
            var background = (BackgroundType) player.Class1;

            if (background == BackgroundType.Medic)
                interval *= 0.5f;

            NWScript.PlaySound("use_bacta");
            var regeneration = NWScript.EffectRegenerate(restoreAmount, interval);
            NWScript.ApplyEffectToObject(DurationType.Temporary, regeneration, target.Object, duration);
            player.SendMessage("You successfully treat " + target.Name + "'s wounds. The healing kit will expire in " + duration + " seconds.");
            
            NWScript.DelayCommand(duration + 0.5f, () => { player.SendMessage("The healing kit that you applied to " + target.Name + " has expired."); });

            if(target.IsPlayer){
                var xp = (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
                SkillService.GiveSkillXP(player, SkillType.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if ( RandomService.Random(100) + 1 <= PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            var rank = SkillService.GetPCSkillRank((NWPlayer)user, SkillType.Medicine);
            return 12.0f - (rank * 0.1f);
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
            var consumeChance = PerkService.GetCreaturePerkLevel((NWPlayer)user, PerkType.FrugalMedic) * 10;
            var background = (BackgroundType) user.Class1;

            if (background == BackgroundType.Medic)
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
