using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
        Animation AnimationID();
        float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation);
        bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData);
        string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation);
        bool AllowLocationTarget();
    }
}
