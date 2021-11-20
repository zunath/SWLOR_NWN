using SWLOR.Game.Server.Event.Conversation.Quest.FinishQuest;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class fin_qst2_rule7
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestComplete.Check(2, 7) ? 1 : 0;
        }
    }
}
