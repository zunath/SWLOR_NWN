using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Item.Contracts
{
    public interface IActionItem
    {
        string CustomKey { get; }
        CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation);
        void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData);
        float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData);
        bool FaceTarget();
        int AnimationID();
        float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation);
        bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData);
        string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation);
        bool AllowLocationTarget();
    }
}
