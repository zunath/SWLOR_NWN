using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item.Medicine
{
    public class StimPack: IActionItem
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;

        public StimPack(INWScript script,
            IPerkService perk,
            ISkillService skill)
        {
            _ = script;
            _perk = perk;
            _skill = skill;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = user.Object;
            int ability = item.GetLocalInt("ABILITY_TYPE");
            int amount = item.GetLocalInt("AMOUNT") + item.MedicineBonus;
            int rank = player.IsPlayer ? _skill.GetPCSkillRank(player, SkillType.Medicine) : 0;
            int recommendedLevel = item.RecommendedLevel;
            int delta = recommendedLevel - rank;
            int penalty = delta / 2;
            float duration = 30.0f;
            int perkLevel = player.IsPlayer ? _perk.GetPCPerkLevel(player, PerkType.StimFiend) : 0;
            float percentIncrease = perkLevel * 0.25f;
            duration = duration + (duration * percentIncrease);

            if (penalty > 0)
                amount -= penalty;
            if (amount < 1) amount = 1;

            Effect effect = _.EffectAbilityIncrease(ability, amount);
            effect = _.TagEffect(effect, "STIM_PACK_EFFECT");

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, effect, target, duration);

            user.SendMessage("You inject " + target.Name + " with a stim pack.");

            if (!Equals(user, target))
            {
                NWCreature targetCreature = target.Object;
                targetCreature.SendMessage(user.Name + " injects you with a stim pack.");
            }

            int xp = (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(300, item.RecommendedLevel, rank);
            _skill.GiveSkillXP(player, SkillType.Medicine, xp);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 3.0f;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public int AnimationID()
        {
            return NWScript.ANIMATION_LOOPING_GET_MID;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 3.5f + _perk.GetPCPerkLevel(user.Object, PerkType.RangedHealing);
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return true;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            var existing = target.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "STIM_PACK_EFFECT");

            if (existing != null && _.GetIsEffectValid(existing) == NWScript.TRUE)
            {
                return "Your target is already under the effects of another stimulant.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
