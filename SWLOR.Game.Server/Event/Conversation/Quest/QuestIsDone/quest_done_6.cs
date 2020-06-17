using SWLOR.Game.Server.Event.Conversation.Quest.QuestIsDone;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class quest_done_6
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestIsDone.Check(6) ? 1 : 0;
        }
    }
}
