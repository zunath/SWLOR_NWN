using System.Linq;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item.Cooking
{
    public class Drink : IActionItem
    {
        public string CustomKey => "Cooking.Drink";

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {

            NWPlayer player = user.Object;
            var ability = (AbilityType)item.GetLocalInt("MISC_TYPE");
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int amount = item.GetLocalInt("AMOUNT") + effectiveStats.Cooking;
            int recommendedLevel = item.RecommendedLevel;
            int duration = 3600;
            Effect effect = _.EffectAbilityIncrease(ability, amount);
            effect = _.TagEffect(effect, "DRINK_EFFECT");

            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);

            user.SendMessage("The effects of this drink will last for one hour.");

            _.DelayCommand(duration + 0.5f, () => { player.SendMessage("The effects of the meal you have eaten have expired."); });
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 3.0f;
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
            var existing = target.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "DRINK_EFFECT");

            if (existing != null && _.GetIsEffectValid(existing) == true)
            {
                return "Already under the effects of another drink.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
