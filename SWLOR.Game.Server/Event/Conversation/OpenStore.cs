using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Event.Conversation
{
    internal static class OpenStore
    {
        public static void Main()
        {
            NWPlayer player = _.GetPCSpeaker();
            NWObject self = NWGameObject.OBJECT_SELF;
            string storeTag = self.GetLocalString("STORE_TAG");
            NWObject store;

            if (string.IsNullOrWhiteSpace(storeTag))
            {
                store = _.GetNearestObject(ObjectType.Store, self);
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
