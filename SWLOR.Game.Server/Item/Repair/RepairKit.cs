using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using NWN;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item.Repair
{
    // todo: implement
    public class RepairKit: IActionItem
    {
        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
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

        public float MaxDistance()
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
            return false;
        }
    }
}
