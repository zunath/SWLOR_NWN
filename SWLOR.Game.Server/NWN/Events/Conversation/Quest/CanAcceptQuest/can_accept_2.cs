using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;
using static NWN.NWScript;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class can_accept_2
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return App.RunEvent<QuestCanAccept>(2) ? TRUE : FALSE;
        }
    }
}
