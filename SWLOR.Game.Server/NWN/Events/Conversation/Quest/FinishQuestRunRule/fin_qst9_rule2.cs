using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class fin_qst9_rule2
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return App.RunEvent<QuestComplete>(9, 2) ? TRUE : FALSE;
        }
    }
}
