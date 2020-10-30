using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;

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
            NWPlaceable objSelf = (NWScript.OBJECT_SELF);

            // No matter what, we want to remove the heartbeat script from this resource.
            NWScript.SetEventScript(objSelf, EventScript.Placeable_OnHeartbeat, string.Empty);

            var hasSpawnedProp = objSelf.GetLocalInt("RESOURCE_PROP_SPAWNED") == 1;
            if (hasSpawnedProp) return;

            var propResref = objSelf.GetLocalString("RESOURCE_PROP");
            if (string.IsNullOrWhiteSpace(propResref)) return;

            var location = objSelf.Location;
            NWPlaceable prop = (NWScript.CreateObject(ObjectType.Placeable, propResref, location));
            objSelf.SetLocalObject("RESOURCE_PROP_OBJ", prop.Object);
            objSelf.SetLocalInt("RESOURCE_PROP_SPAWNED", 1);
        }
    }
}
