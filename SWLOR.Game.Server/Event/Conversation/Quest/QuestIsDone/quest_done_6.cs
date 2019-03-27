using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.QuestIsDone;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class quest_done_6
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestIsDone.Check(6) ? TRUE : FALSE;
        }
    }
}
