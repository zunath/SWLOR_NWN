


using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Area;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal static class area_on_exit
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            MessageHub.Instance.Publish(new OnAreaExit());
        }
    }
}
