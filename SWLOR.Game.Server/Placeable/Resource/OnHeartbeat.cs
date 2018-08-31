using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using Object = SWLOR.Game.Server.NWN.NWScript.Object;

namespace SWLOR.Game.Server.Placeable.Resource
{
    public class OnHeartbeat: IRegisteredEvent
    {
        private readonly INWScript _;

        public OnHeartbeat(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable objSelf = NWPlaceable.Wrap(Object.OBJECT_SELF);
            bool hasSpawnedProp = objSelf.GetLocalInt("RESOURCE_PROP_SPAWNED") == 1;
            if (hasSpawnedProp) return false;

            string propResref = objSelf.GetLocalString("RESOURCE_PROP");
            if (string.IsNullOrWhiteSpace(propResref)) return false;

            Location location = objSelf.Location;
            NWPlaceable prop = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, propResref, location));
            objSelf.SetLocalObject("RESOURCE_PROP_OBJ", prop.Object);
            objSelf.SetLocalInt("RESOURCE_PROP_SPAWNED", 1);
            return true;
        }
    }
}
