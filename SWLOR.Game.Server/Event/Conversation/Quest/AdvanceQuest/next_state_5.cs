using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.AdvanceQuest;
using static SWLOR.Game.Server.NWScript._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class next_state_5
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestAdvance.Check(5) ? 1 : 0;
        }
    }
}
