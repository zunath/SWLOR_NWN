using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnHeartbeat: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable objSelf = (NWGameObject.OBJECT_SELF);
            bool hasSpawnedProp = objSelf.GetLocalInt("RESOURCE_PROP_SPAWNED") == 1;
            if (hasSpawnedProp) return false;

            string propResref = objSelf.GetLocalString("RESOURCE_PROP");
            if (string.IsNullOrWhiteSpace(propResref)) return false;

            Location location = objSelf.Location;
            NWPlaceable prop = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, propResref, location));
            objSelf.SetLocalObject("RESOURCE_PROP_OBJ", prop.Object);
            objSelf.SetLocalInt("RESOURCE_PROP_SPAWNED", 1);

            _.SetEventScript(objSelf, _.EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT, string.Empty);
            return true;
        }
    }
}
