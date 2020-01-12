using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.OnQuestState;
using static SWLOR.Game.Server.NWScript._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class on_qst10_state_8
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestCheckState.Check(10, 8) ? 1 : 0;
        }
    }
}
