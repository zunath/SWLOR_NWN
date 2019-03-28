﻿using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnOpened : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = Object.OBJECT_SELF;

            container.IsUseable = false;

            NWPlayer oPC = (_.GetLastOpenedBy());
            int questID = container.GetLocalInt("QUEST_ID");
            PCQuestStatus status = DataService.Single<PCQuestStatus>(x => x.PlayerID == oPC.GlobalID && x.QuestID == questID);

            oPC.FloatingText("Please place the items you would like to turn in for this quest into the container. If you want to cancel this process, move away from the container.");

            string text = "Required Items: \n\n";

            var itemProgress = DataService.Where<PCQuestItemProgress>(x => x.PCQuestStatusID == status.ID);
            foreach (PCQuestItemProgress item in itemProgress)
            {
                ItemVO tempItemModel = QuestService.GetTempItemInformation(item.Resref, item.Remaining);
                text += tempItemModel.Quantity + "x " + tempItemModel.Name + "\n";
            }

            oPC.SendMessage(text);
            return true;
        }
    }
}
