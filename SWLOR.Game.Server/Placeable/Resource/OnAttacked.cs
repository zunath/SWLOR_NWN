using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnAttacked: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IRandomService _random;
        private readonly IColorTokenService _color;

        public OnAttacked(
            INWScript script,
            IRandomService random,
            IColorTokenService color)
        {
            _ = script;
            _random = random;
            _color = color;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable plc = NWPlaceable.Wrap(Object.OBJECT_SELF);
            NWPlayer oPC = NWPlayer.Wrap(_.GetLastAttacker());
            NWItem oWeapon = NWItem.Wrap(_.GetLastWeaponUsed(oPC.Object));
            int type = oWeapon.BaseItemType;
            int resourceCount = plc.GetLocalInt("RESOURCE_COUNT");

            if (resourceCount <= -1)
            {
                resourceCount = _random.Random(3) + 3;
                plc.SetLocalInt("RESOURCE_COUNT", resourceCount);
            }

            if (type == NWScript.BASE_ITEM_INVALID)
            {
                oWeapon = oPC.RightHand;
            }

            int activityID = plc.GetLocalInt("RESOURCE_ACTIVITY");

            string improperWeaponMessage = "";
            bool usingCorrectWeapon = true;
            if (activityID == 1) // 1 = Logging
            {
                usingCorrectWeapon = oWeapon.CustomItemType == CustomItemType.Blade || 
                                     oWeapon.CustomItemType == CustomItemType.TwinBlade || 
                                     oWeapon.CustomItemType == CustomItemType.HeavyBlade ||
                                     oWeapon.CustomItemType == CustomItemType.FinesseBlade ||
                                     oWeapon.CustomItemType == CustomItemType.Polearm;
                improperWeaponMessage = "You must be using a blade to harvest this object.";
            }
            else if (activityID == 2) // Mining
            {
                usingCorrectWeapon = oWeapon.CustomItemType == CustomItemType.Blade ||
                                     oWeapon.CustomItemType == CustomItemType.TwinBlade ||
                                     oWeapon.CustomItemType == CustomItemType.HeavyBlade ||
                                     oWeapon.CustomItemType == CustomItemType.FinesseBlade ||
                                     oWeapon.CustomItemType == CustomItemType.Polearm ||
                                     oWeapon.CustomItemType == CustomItemType.Blunt ||
                                     oWeapon.CustomItemType == CustomItemType.HeavyBlunt;
                improperWeaponMessage = "You must be using a blade or a blunt weapon to harvest this object.";
            }

            if (!usingCorrectWeapon)
            {
                plc.IsPlot = true;
                oPC.SendMessage(_color.Red(improperWeaponMessage));
                oPC.SetLocalInt("NOT_USING_CORRECT_WEAPON", 1);
                oPC.ClearAllActions();
                oPC.DelayCommand(() => plc.IsPlot = false, 1.0f);
            }

            return true;
        }
    }
}
