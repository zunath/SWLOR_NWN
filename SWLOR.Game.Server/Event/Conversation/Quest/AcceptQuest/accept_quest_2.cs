using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.AcceptQuest;
using static SWLOR.Game.Server.NWScript._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class accept_quest_2
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestAccept.Check(2) ? 1 : 0;
        }
    }
}
