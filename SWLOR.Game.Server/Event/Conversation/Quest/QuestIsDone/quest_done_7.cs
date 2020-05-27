using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.QuestIsDone;
using static SWLOR.Game.Server.NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class quest_done_7
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestIsDone.Check(7) ? true : false;
        }
    }
}
