
using NWN;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class OpenStore : IRegisteredEvent
    {
        private readonly INWScript _;

        public OpenStore(INWScript script)
        {
            _ = script;
        }

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
