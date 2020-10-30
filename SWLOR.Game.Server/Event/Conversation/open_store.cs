using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class open_store
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            using (new Profiler(nameof(open_store)))
            {
                NWPlayer player = NWScript.GetPCSpeaker();
                NWObject self = NWScript.OBJECT_SELF;
                string storeTag = self.GetLocalString("STORE_TAG");
                NWObject store;

                if (string.IsNullOrWhiteSpace(storeTag))
                {
                    store = NWScript.GetNearestObject(ObjectType.Store, self);
                }
                else
                {
                    store = NWScript.GetObjectByTag(storeTag);
                }

                if (!store.IsValid)
                {
                    NWScript.SpeakString("ERROR: Unable to locate store.");
                }

                NWScript.OpenStore(store, player);
            }
        }
    }
}
