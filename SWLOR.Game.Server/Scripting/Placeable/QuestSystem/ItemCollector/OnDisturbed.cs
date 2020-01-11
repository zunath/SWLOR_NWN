using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Placeable.QuestSystem.ItemCollector
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
            NWPlaceable container = NWGameObject.OBJECT_SELF;
            NWObject owner = container.GetLocalObject("QUEST_OWNER");

            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            var disturbType = _.GetInventoryDisturbType();
            string crafterPlayerID = item.GetLocalString("CRAFTER_PLAYER_ID");
            Guid? crafterPlayerGUID = null;
            if (!string.IsNullOrWhiteSpace(crafterPlayerID))
                crafterPlayerGUID = new Guid(crafterPlayerID);

            if (disturbType == InventoryDisturbType.Added)
            {
                int questID = container.GetLocalInt("QUEST_ID");
                PCQuestStatus status = DataService.PCQuestStatus.GetByPlayerAndQuestID(player.GlobalID, questID);
                PCQuestItemProgress progress = status.Items.ContainsKey(item.Resref) ?
                    status.Items[item.Resref] :
                    null;

                if (progress == null)
                {
                    _.CopyItem(item, player, true);
                    player.SendMessage(ColorTokenService.Red("That item is not required for this quest."));
                }
                else if (progress.MustBeCraftedByPlayer && crafterPlayerGUID != player.GlobalID)
                {
                    _.CopyItem(item, player, true);
                    player.SendMessage(ColorTokenService.Red("You may only submit items which you have personally created for this quest."));
                }
                else
                {
                    progress.Remaining--;
                    DataService.SubmitDataChange(status, DatabaseActionType.Set);

                    // Recalc the remaining items needed.
                    int remainingCount = status.Items.Sum(x => x.Value.Remaining);
                    if (remainingCount <= 0)
                    {
                        var quest = QuestService.GetQuestByID(questID);
                        quest.Advance(player, owner);
                    }

                    player.SendMessage("You need " + progress.Remaining + "x " + item.Name + " for this quest.");
                }
                item.Destroy();

                if (status.Items.Sum(x => x.Value.Remaining) <= 0)
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
                        player.AssignCommand(() => { _.ActionStartConversation(owner, "", true, false); });
                    }
                }
            }
        }
    }
}
