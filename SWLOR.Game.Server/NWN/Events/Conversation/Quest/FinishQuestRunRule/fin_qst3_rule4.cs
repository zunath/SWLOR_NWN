using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class fin_qst3_rule4
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return App.RunEvent<QuestComplete>(3, 4) ? TRUE : FALSE;
        }
    }
}
