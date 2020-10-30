using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.QuestSystem.ItemCollector
{
    public class OnDisturbed : IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable container = NWScript.OBJECT_SELF;
            NWObject owner = container.GetLocalObject("QUEST_OWNER");

            NWPlayer player = NWScript.GetLastDisturbed();
            NWItem item = NWScript.GetInventoryDisturbItem();
            var disturbType = NWScript.GetInventoryDisturbType();
            var crafterPlayerID = item.GetLocalString("CRAFTER_PLAYER_ID");
            Guid? crafterPlayerGUID = null;
            if (!string.IsNullOrWhiteSpace(crafterPlayerID))
                crafterPlayerGUID = new Guid(crafterPlayerID);

            if (disturbType == DisturbType.Added)
            {
                var questID = container.GetLocalInt("QUEST_ID");
                var status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
                var progress = DataService.PCQuestItemProgress.GetByPCQuestStatusIDAndResrefOrDefault(status.ID, item.Resref);
                var action = DatabaseActionType.Update;

                if (progress == null)
                {
                    NWScript.CopyItem(item, player, true);
                    player.SendMessage(ColorTokenService.Red("That item is not required for this quest."));
                }
                else if (progress.MustBeCraftedByPlayer && crafterPlayerGUID != player.GlobalID)
                {
                    NWScript.CopyItem(item, player, true);
                    player.SendMessage(ColorTokenService.Red("You may only submit items which you have personally created for this quest."));
                }
                else
                {
                    progress.Remaining--;

                    if (progress.Remaining <= 0)
                    {
                        var progressCopy = progress;
                        progress = DataService.PCQuestItemProgress.GetByID(progressCopy.ID);
                        action = DatabaseActionType.Delete;
                    }
                    DataService.SubmitDataChange(progress, action);

                    // Recalc the remaining items needed.
                    var remainingCount = DataService.PCQuestItemProgress.GetCountByPCQuestStatusID(status.ID);
                    if (remainingCount <= 0)
                    {
                        var quest = QuestService.GetQuestByID(questID);
                        quest.Advance(player, owner);
                    }

                    player.SendMessage("You need " + progress.Remaining + "x " + item.Name + " for this quest.");
                }
                item.Destroy();

                var questItemProgresses = DataService.PCQuestItemProgress.GetAllByPCQuestStatusID(status.ID);
                if (!questItemProgresses.Any())
                {
                    var conversation = NWScript.GetLocalString(owner, "CONVERSATION");

                    // Either start a SWLOR conversation
                    if (!string.IsNullOrWhiteSpace(conversation))
                    {
                        DialogService.StartConversation(player, owner, conversation);
                    }
                    // Or a regular NWN conversation.
                    else
                    {
                        player.AssignCommand(() => { NWScript.ActionStartConversation(owner, "", true, false); });
                    }
                }
            }
        }
    }
}
