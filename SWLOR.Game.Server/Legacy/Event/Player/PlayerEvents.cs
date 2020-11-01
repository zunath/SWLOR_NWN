using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.Event.Player
{
    public static class PlayerEvents
    {
        public static void OnDamaged()
        {
            MessageHub.Instance.Publish(new OnPlayerDamaged());
        }

        public static void OnHeartbeat()
        {
            MessageHub.Instance.Publish(new OnPlayerHeartbeat());
        }
    }
}
