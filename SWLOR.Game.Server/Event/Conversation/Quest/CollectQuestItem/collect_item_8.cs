using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.CollectQuestItem;
using static NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    internal class collect_item_8
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestCollectItem.Check(8) ? TRUE : FALSE;
        }
    }
}
