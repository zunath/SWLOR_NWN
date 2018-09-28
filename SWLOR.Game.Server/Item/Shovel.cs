using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item
{
    public class Shovel: IActionItem
    {
        private readonly IDialogService _dialog;

        public Shovel(IDialogService dialog)
        {
            _dialog = dialog;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWArea area = user.Area;
            bool farmingDisabled = area.GetLocalInt("FARMING_DISABLED") == 1;

            if (farmingDisabled)
            {
                user.SendMessage("You cannot dig a hole in this area.");
                return;
            }

            user.SetLocalObject("SHOVEL_ITEM", item.Object);
            user.SetLocalLocation("SHOVEL_TARGET_LOCATION", targetLocation);
            user.SetLocalObject("SHOVEL_TARGET_OBJECT", target.Object);
            user.ClearAllActions();
            _dialog.StartConversation(user, user, "Shovel");
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 0;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return 0;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 0;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
