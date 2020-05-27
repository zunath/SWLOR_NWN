using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Quest.CollectQuestItem;
using static SWLOR.Game.Server.NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class collect_item_1
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return QuestCollectItem.Check(1) ? true : false;
        }
    }
}
