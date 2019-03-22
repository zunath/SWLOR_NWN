using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class HealingKit: IActionItem
    {

        
        private readonly ISkillService _skill;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IPlayerStatService _playerStat;

        public HealingKit(
            ISkillService skill,
            IRandomService random,
            IPerkService perk,
            IPlayerStatService playerStat)
        {
            
            _skill = skill;
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
            NWPlayer player = (user.Object);
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            target.RemoveEffect(EFFECT_TYPE_REGENERATE);
            int rank = _skill.GetPCSkillRank(player, SkillType.Medicine);
            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky);
            int perkDurationBonus = _perk.GetPCPerkLevel(player, PerkType.HealingKitExpert) * 6 + (luck * 2);
            float duration = 30.0f + (rank * 0.4f) + perkDurationBonus;
            int restoreAmount = 1 + item.GetLocalInt("HEALING_BONUS") + effectiveStats.Medicine + item.MedicineBonus;
            int delta = item.RecommendedLevel - rank;
            float effectivenessPercent = 1.0f;

            if (delta > 0)
            {
                effectivenessPercent = effectivenessPercent - (delta * 0.1f);
            }

            restoreAmount = (int)(restoreAmount * effectivenessPercent);

            int perkBlastBonus = _perk.GetPCPerkLevel(player, PerkType.ImmediateImprovement);
            if (perkBlastBonus > 0)
            {
                int blastHeal = restoreAmount * perkBlastBonus;
                if (_random.Random(100) + 1 <= luck / 2)
                {
                    blastHeal *= 2;
                }
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(blastHeal), target.Object);
            }

            float interval = 6.0f;
            BackgroundType background = (BackgroundType) player.Class1;

            if (background == BackgroundType.Medic)
                interval *= 0.5f;

            _.PlaySound("use_bacta");
            Effect regeneration = _.EffectRegenerate(restoreAmount, interval);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, regeneration, target.Object, duration);
            player.SendMessage("You successfully treat " + target.Name + "'s wounds. The healing kit will expire in " + duration + " seconds.");
            
            _.DelayCommand(duration + 0.5f, () => { player.SendMessage("The healing kit that you applied to " + target.Name + " has expired."); });

            if(target.IsPlayer){
                int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
                _skill.GiveSkillXP(player, SkillType.Medicine, xp);
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if ( _random.Random(100) + 1 <= _perk.GetPCPerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = _skill.GetPCSkillRank((NWPlayer)user, SkillType.Medicine);
            return 12.0f - (rank * 0.1f);
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
            BackgroundType background = (BackgroundType) user.Class1;

            if (background == BackgroundType.Medic)
            {
                consumeChance += 5;
            }


            return _random.Random(100) + 1 > consumeChance;
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
