using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;
using static NWN.NWScript;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class credit_check
#pragma warning restore IDE1006 // Naming Styles
    {
        public static void Main()
        {
            App.RunEvent<HasCredits>();
        }
    }
}
