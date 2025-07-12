using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public class DMActions
    {
        [NWNEventHandler(ScriptName.OnDMSpawnObjectAfter)]
        public static void OnDMSpawnObject()
        {
            var obj = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            var type = GetObjectType(obj);

            if (type == ObjectType.Creature)
            {
                for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                {
                    SetDroppableFlag(item, false);
                }
            }
        }
    }
}
