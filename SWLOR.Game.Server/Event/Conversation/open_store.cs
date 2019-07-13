using SWLOR.Game.Server;

using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class open_store
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            using (new Profiler(nameof(open_store)))
            {
                NWPlayer player = _.GetPCSpeaker();
                NWObject self = NWGameObject.OBJECT_SELF;
                string storeTag = self.GetLocalString("STORE_TAG");
                NWObject store;

                if (string.IsNullOrWhiteSpace(storeTag))
                {
                    store = _.GetNearestObject(_.OBJECT_TYPE_STORE, self);
                }
                else
                {
                    store = _.GetObjectByTag(storeTag);
                }

                if (!store.IsValid)
                {
                    _.SpeakString("ERROR: Unable to locate store.");
                }

                _.OpenStore(store, player);
            }
        }
    }
}
