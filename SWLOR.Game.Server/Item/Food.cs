using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Item
{
    public class Food: IActionItem
    {
        private readonly ICustomEffectService _customEffect;

        public Food(ICustomEffectService customEffect)
        {
            _customEffect = customEffect;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            string type = item.GetLocalString("BONUS_TYPE");
            int length = item.GetLocalInt("BONUS_LENGTH") * 60;
            int amount = item.GetLocalInt("BONUS_AMOUNT");

            string data = $"{type},{amount}";

            _customEffect.ApplyCustomEffect(user, target.Object, CustomEffectType.FoodEffect, length, item.RecommendedLevel, data);
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 1.5f;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return ANIMATION_FIREFORGET_SALUTE;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 1.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return true;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            string type = item.GetLocalString("BONUS_TYPE");
            int length = item.GetLocalInt("BONUS_LENGTH");
            int amount = item.GetLocalInt("BONUS_AMOUNT");

            if (string.IsNullOrWhiteSpace(type) || length <= 0 || amount <= 0)
            {
                return "ERROR: This food isn't set up properly. Please inform an admin. Resref: " + item.Resref;
            }

            bool hasFoodEffect = _customEffect.DoesPCHaveCustomEffectByCategory(user.Object, CustomEffectCategoryType.FoodEffect);

            if (hasFoodEffect)
            {
                return "You are not hungry right now.";
            }

            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
