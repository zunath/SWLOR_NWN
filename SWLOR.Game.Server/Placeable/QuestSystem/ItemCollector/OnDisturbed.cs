using System;
using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnDisturbed : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlaceable container = Object.OBJECT_SELF;
            NWObject owner = container.GetLocalObject("QUEST_OWNER");

            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            int disturbType = _.GetInventoryDisturbType();
            string crafterPlayerID = item.GetLocalString("CRAFTER_PLAYER_ID");
            Guid? crafterPlayerGUID = null;
            if (!string.IsNullOrWhiteSpace(crafterPlayerID))
                crafterPlayerGUID = new Guid(crafterPlayerID);

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                int questID = container.GetLocalInt("QUEST_ID");
                PCQuestStatus status = DataService.Single<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
                PCQuestItemProgress progress = DataService.SingleOrDefault<PCQuestItemProgress>(x => x.PCQuestStatusID == status.ID && x.Resref == item.Resref);
                DatabaseActionType action = DatabaseActionType.Update;

                if (progress == null)
                {
                    _.CopyItem(item, player, TRUE);
                    player.SendMessage(ColorTokenService.Red("That item is not required for this quest."));
                }
                else if (progress.MustBeCraftedByPlayer && crafterPlayerGUID != player.GlobalID)
                {
                    _.CopyItem(item, player, TRUE);
                    player.SendMessage(ColorTokenService.Red("You may only submit items which you have personally created for this quest."));
                }
                else
                {
                    progress.Remaining--;

                    if (progress.Remaining <= 0)
                    {
                        var progressCopy = progress;
                        progress = DataService.Single<PCQuestItemProgress>(x => x.ID == progressCopy.ID);
                        action = DatabaseActionType.Delete;
                    }
                    DataService.SubmitDataChange(progress, action);

                    // Recalc the remaining items needed.
                    int remainingCount = DataService.GetAll<PCQuestItemProgress>().Count(x => x.PCQuestStatusID == status.ID);
                    if (remainingCount <= 0)
                    {
                        QuestService.AdvanceQuestState(player, owner, questID);
                    }

                    player.SendMessage("You need " + progress.Remaining + "x " + item.Name + " for this quest.");
                }
                item.Destroy();

                var questItemProgresses = DataService.Where<PCQuestItemProgress>(x => x.PCQuestStatusID == status.ID);
                if (!questItemProgresses.Any())
                {
                    string conversation = _.GetLocalString(owner, "CONVERSATION");

                    // Either start a SWLOR conversation
                    if (!string.IsNullOrWhiteSpace(conversation))
                    {
                        DialogService.StartConversation(player, owner, conversation);
                    }
                    // Or a regular NWN conversation.
                    else
                    {
                        player.AssignCommand(() => { _.ActionStartConversation(owner, "", TRUE, FALSE); });
                    }
                }

            }

            return true;
        }
    }
}
