using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.QuestSystem.ItemCollector
{
    public class OnOpened : IScript
    {
        public void Main()
        {
            NWPlaceable container = NWGameObject.OBJECT_SELF;

            container.IsUseable = false;

            NWPlayer oPC = (_.GetLastOpenedBy());
            int questID = container.GetLocalInt("QUEST_ID");
            PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestID(oPC.GlobalID, questID);

            oPC.FloatingText("Please place the items you would like to turn in for this quest into the container. If you want to cancel this process, move away from the container.");

            string text = "Required Items: \n\n";

            foreach (var item in status.Items)
            {
                ItemVO tempItemModel = QuestService.GetTempItemInformation(item.Key, item.Value.Remaining);
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
