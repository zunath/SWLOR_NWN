using SWLOR.Game.Server.Event.Conversation.Quest.AdvanceQuest;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class next_state_5
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestAdvance.Check(5) ? 1 : 0;
        }
    }
}
