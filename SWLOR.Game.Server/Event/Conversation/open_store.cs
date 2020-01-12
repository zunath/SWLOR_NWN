using SWLOR.Game.Server;

using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class open_store
#pragma warning restore IDE1006 // Naming Styles
    {
        public void Main()
        {
            using (new Profiler(nameof(open_store)))
            {
                NWPlayer player = SWLOR.Game.Server.NWScript._.GetPCSpeaker();
                NWObject self = NWGameObject.OBJECT_SELF;
                string storeTag = self.GetLocalString("STORE_TAG");
                NWObject store;

                if (string.IsNullOrWhiteSpace(storeTag))
                {
                    store = SWLOR.Game.Server.NWScript._.GetNearestObject(ObjectType.Store, self);
                }
                else
                {
                    store = SWLOR.Game.Server.NWScript._.GetObjectByTag(storeTag);
                }

                if (!store.IsValid)
                {
                    SWLOR.Game.Server.NWScript._.SpeakString("ERROR: Unable to locate store.");
                }

                SWLOR.Game.Server.NWScript._.OpenStore(store, player);
            }
        }
    }
}
