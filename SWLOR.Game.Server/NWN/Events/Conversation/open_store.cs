using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class open_store
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            App.RunEvent<OpenStore>(1);
        }
    }
}
