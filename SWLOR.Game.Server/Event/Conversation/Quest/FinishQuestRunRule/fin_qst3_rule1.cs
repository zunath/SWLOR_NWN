using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.FinishQuest;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class fin_qst3_rule1
#pragma warning restore IDE1006 // Naming Styles
    {
        public int Main()
        {
            return QuestComplete.Check(3, 1) ? 1 : 0;
        }
    }
}
