using SWLOR.Game.Server.Event.Conversation.Quest.CanAcceptQuest;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class can_accept_2
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestCanAccept.Check(2) ? 1 : 0;
        }
    }
}
