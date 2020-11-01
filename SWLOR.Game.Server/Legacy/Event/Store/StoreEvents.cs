using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.Event.Store
{
    public static class StoreEvents
    {
        public static void OpenStore()
        {
            MessageHub.Instance.Publish(new OnOpenStore());
        }

        public static void CloseStore()
        {
            MessageHub.Instance.Publish(new OnCloseStore());
        }
    }
}
