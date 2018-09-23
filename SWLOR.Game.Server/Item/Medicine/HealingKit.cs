using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class HealingKit: IActionItem
    {

        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IPlayerStatService _playerStat;

        public HealingKit(INWScript script,
            ISkillService skill,
            IRandomService random,
            IPerkService perk,
            IPlayerStatService playerStat)
        {
            _ = script;
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

            target.RemoveEffect(NWScript.EFFECT_TYPE_REGENERATE);
            PCSkill skill = _skill.GetPCSkill(player, SkillType.Medicine);
            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky);
            int perkDurationBonus = _perk.GetPCPerkLevel(player, PerkType.HealingKitExpert) * 6 + (luck * 2);
            float duration = 30.0f + (skill.Rank * 0.4f) + perkDurationBonus;
            int restoreAmount = 1 + item.GetLocalInt("HEALING_BONUS") + _playerStat.EffectiveMedicineBonus(player);

            int perkBlastBonus = _perk.GetPCPerkLevel(player, PerkType.ImmediateImprovement);
            if (perkBlastBonus > 0)
            {
                int blastHeal = restoreAmount * perkBlastBonus;
                if (_random.Random(100) + 1 <= luck / 2)
                {
                    blastHeal *= 2;
                }
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(blastHeal), target.Object);
            }

            float interval = 6.0f;
            BackgroundType background = (BackgroundType) player.Class1;

            if (background == BackgroundType.Medic)
                interval *= 0.5f;

            Effect regeneration = _.EffectRegenerate(restoreAmount, interval);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, regeneration, target.Object, duration);
            player.SendMessage("You successfully treat " + target.Name + "'s wounds.");

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, skill.Rank);
            _skill.GiveSkillXP(player, SkillType.Medicine, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if ( _random.Random(100) + 1 <= _perk.GetPCPerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            PCSkill skill = _skill.GetPCSkill((NWPlayer)user, SkillType.Medicine);
            return 12.0f - (skill.Rank * 0.1f);
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
            BackgroundType background = (BackgroundType) user.Class1;

            if (background == BackgroundType.Medic)
            {
                consumeChance += 5;
            }


            return _random.Random(100) + 1 > consumeChance;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (_.GetIsPC(target.Object) == NWScript.FALSE || _.GetIsDM(target.Object) == NWScript.TRUE)
            {
                return "Only players may be targeted with this item.";
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
