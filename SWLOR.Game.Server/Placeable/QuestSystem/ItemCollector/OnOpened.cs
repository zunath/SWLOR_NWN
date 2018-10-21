using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnOpened : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IQuestService _quest;

        public OnOpened(
            INWScript script,
            IDataContext db,
            IQuestService quest)
        {
            _ = script;
            _db = db;
            _quest = quest;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable container = Object.OBJECT_SELF;

            container.IsUseable = false;

            NWPlayer oPC = (_.GetLastOpenedBy());
            int questID = container.GetLocalInt("QUEST_ID");
            PCQuestStatus status = _db.PCQuestStatus.Single(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            oPC.FloatingText("Please place the items you would like to turn in for this quest into the container. If you want to cancel this process, move away from the container.");

            string text = "Required Items: \n\n";

            foreach (PCQuestItemProgress item in status.PCQuestItemProgresses)
            {
                ItemVO tempItemModel = _quest.GetTempItemInformation(item.Resref, item.Remaining);
                text += tempItemModel.Quantity + "x " + tempItemModel.Name + "\n";
            }

            oPC.SendMessage(text);
            return true;
        }
    }
}
