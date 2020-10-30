using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Scripts.Placeable.QuestSystem.ItemCollector
{
    public class OnOpened : IScript
    {
        public void Main()
        {
            NWPlaceable container = NWScript.OBJECT_SELF;

            container.IsUseable = false;

            NWPlayer oPC = (NWScript.GetLastOpenedBy());
            var questID = container.GetLocalInt("QUEST_ID");
            var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(oPC.GlobalID, questID);

            oPC.FloatingText("Please place the items you would like to turn in for this quest into the container. If you want to cancel this process, move away from the container.");

            var text = "Required Items: \n\n";

            var itemProgress = DataService.PCQuestItemProgress.GetAllByPCQuestStatusID(status.ID);
            foreach (var item in itemProgress)
            {
                var tempItemModel = QuestService.GetTempItemInformation(item.Resref, item.Remaining);
                text += tempItemModel.Quantity + "x " + tempItemModel.Name + "\n";
            }

            oPC.SendMessage(text);
        }

        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }
    }
}
