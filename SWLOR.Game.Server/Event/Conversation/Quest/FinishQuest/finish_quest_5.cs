using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.FinishQuest;
using static SWLOR.Game.Server.NWScript._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class finish_quest_5
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestComplete.Check(5, 0) ? 1 : 0;
        }
    }
}
