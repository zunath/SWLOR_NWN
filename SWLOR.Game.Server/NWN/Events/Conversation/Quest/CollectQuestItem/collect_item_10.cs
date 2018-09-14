using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation;
using static NWN.NWScript;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class collect_item_10
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return App.RunEvent<QuestCollectItem>(10) ? TRUE : FALSE;
        }
    }
}
