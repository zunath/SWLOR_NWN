using SWLOR.Game.Server.Event.Conversation.Quest.CanAcceptQuest;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class can_accept_1
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestCanAccept.Check(1) ? 1 : 0;
        }
    }
}
