
using NWN;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class OpenStore : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = _.GetPCSpeaker();
            NWObject self = Object.OBJECT_SELF;
            string storeTag = self.GetLocalString("STORE_TAG");
            NWObject store;

            if (string.IsNullOrWhiteSpace(storeTag))
            {
                store = _.GetNearestObject(OBJECT_TYPE_STORE, self);
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

            return true;
        }
    }
}
