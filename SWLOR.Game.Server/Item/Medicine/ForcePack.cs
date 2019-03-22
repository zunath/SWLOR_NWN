using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN._;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class ForcePack: IActionItem
    {

        
        private readonly IDataService _data;
        private readonly ISkillService _skill;
        private readonly IRandomService _random;
        private readonly IPerkService _perk;
        private readonly IPlayerStatService _playerStat;
        private readonly IAbilityService _ability;
        private readonly ICustomEffectService _customEffect;

        public ForcePack(
            
            IDataService data,
            ISkillService skill,
            IRandomService random,
            IPerkService perk,
            IPlayerStatService playerStat,
            IAbilityService ability,
            ICustomEffectService customEffect)
        {
            
            _data = data;
            _skill = skill;
            _random = random;
            _perk = perk;
            _playerStat = playerStat;
            _ability = ability;
            _customEffect = customEffect;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            user.SendMessage("You begin applying a force pack to " + target.Name + "...");
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
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

            int perkBlastBonus = _perk.GetPCPerkLevel(player, PerkType.ImmediateForcePack);
            if (perkBlastBonus > 0)
            {
                int blastHeal = restoreAmount * perkBlastBonus;
                if (_random.Random(100) + 1 <= luck / 2)
                {
                    blastHeal *= 2;
                }

                _ability.RestoreFP(target.Object, blastHeal);
            }

            float interval = 6.0f;
            BackgroundType background = (BackgroundType) player.Class1;

            if (background == BackgroundType.Medic)
                interval *= 0.5f;

            string data = (int)interval + ", " + restoreAmount;
            _customEffect.ApplyCustomEffect(user, target.Object, CustomEffectType.ForcePack, (int)duration, restoreAmount, data);

            player.SendMessage("You successfully apply a force pack to " + target.Name + ". The force pack will expire in " + duration + " seconds.");

            _.DelayCommand(duration + 0.5f, () => { player.SendMessage("The force pack that you applied to " + target.Name + " has expired."); });

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
            _skill.GiveSkillXP(player, SkillType.Medicine, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            if ( _random.Random(100) + 1 <= _perk.GetPCPerkLevel((NWPlayer)user, PerkType.SpeedyFirstAid) * 10)
            {
                return 0.1f;
            }

            int rank = _skill.GetPCSkillRank(user.Object, SkillType.Medicine);
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
            if (!target.IsPlayer)
            {
                return "Only players may be targeted with this item.";
            }

            var dbTarget = _data.Single<Player>(x => x.ID == target.GlobalID);
            if (dbTarget.CurrentFP >= dbTarget.MaxFP)
            {
                return "Your target's FP is at their maximum.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
