using SWLOR.Game.Server.Event.Area;
using SWLOR.Game.Server.Messaging;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal static class area_on_user
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            MessageHub.Instance.Publish(new OnAreaUserDefined());
        }
    }
}
