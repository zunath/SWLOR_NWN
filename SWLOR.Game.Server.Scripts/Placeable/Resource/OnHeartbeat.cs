using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripts.Placeable.Resource
{
    public class OnHeartbeat: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable objSelf = (NWGameObject.OBJECT_SELF);

            // No matter what, we want to remove the heartbeat script from this resource.
            _.SetEventScript(objSelf, _.EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT, string.Empty);

            bool hasSpawnedProp = objSelf.GetLocalInt("RESOURCE_PROP_SPAWNED") == 1;
            if (hasSpawnedProp) return;

            string propResref = objSelf.GetLocalString("RESOURCE_PROP");
            if (string.IsNullOrWhiteSpace(propResref)) return;

            Location location = objSelf.Location;
            NWPlaceable prop = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, propResref, location));
            objSelf.SetLocalObject("RESOURCE_PROP_OBJ", prop.Object);
            objSelf.SetLocalInt("RESOURCE_PROP_SPAWNED", 1);
        }
    }
}
