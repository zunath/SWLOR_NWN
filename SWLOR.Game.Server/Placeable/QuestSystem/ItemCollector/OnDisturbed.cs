using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IQuestService _quest;
        private readonly IColorTokenService _color;
        private readonly IDialogService _dialog;

        public OnDisturbed(
            INWScript script,
            IDataService data,
            IQuestService quest,
            IColorTokenService color,
            IDialogService dialog)
        {
            _ = script;
            _data = data;
            _quest = quest;
            _color = color;
            _dialog = dialog;
        }


        public bool Run(params object[] args)
        {
            NWPlaceable container = Object.OBJECT_SELF;
            NWObject owner = container.GetLocalObject("QUEST_OWNER");

            NWPlayer player = _.GetLastDisturbed();
            NWItem item = _.GetInventoryDisturbItem();
            int disturbType = _.GetInventoryDisturbType();
            string crafterPlayerID = item.GetLocalString("CRAFTER_PLAYER_ID");

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                int questID = container.GetLocalInt("QUEST_ID");
                PCQuestStatus status = _data.Single<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
                PCQuestItemProgress progress = _data.SingleOrDefault<PCQuestItemProgress>(x => x.PCQuestStatusID == status.PCQuestStatusID && x.Resref == item.Resref);
                DatabaseActionType action = DatabaseActionType.Update;

                if (progress == null)
                {
                    _.CopyItem(item, player, TRUE);
                    player.SendMessage(_color.Red("That item is not required for this quest."));
                }
                else if (progress.MustBeCraftedByPlayer && crafterPlayerID != player.GlobalID)
                {
                    _.CopyItem(item, player, TRUE);
                    player.SendMessage(_color.Red("You may only submit items which you have personally created for this quest."));
                }
                else
                {
                    progress.Remaining--;

                    if (progress.Remaining <= 0)
                    {
                        progress = _data.Single<PCQuestItemProgress>(x => x.PCQuestItemProgressID == progress.PCQuestItemProgressID);
                        action = DatabaseActionType.Delete;
                    }
                    _data.SubmitDataChange(progress, action);

                    // Recalc the remaining items needed.
                    int remainingCount = _data.GetAll<PCQuestItemProgress>().Count(x => x.PCQuestStatusID == status.PCQuestStatusID);
                    if (remainingCount <= 0)
                    {
                        _quest.AdvanceQuestState(player, owner, questID);
                    }

                    player.SendMessage("You need " + progress.Remaining + " " + item.Name + " for this quest.");
                }
                item.Destroy();

                var questItemProgresses = _data.Where<PCQuestItemProgress>(x => x.PCQuestStatusID == status.PCQuestStatusID);
                if ( !questItemProgresses.Any())
                {
                    string conversation = _.GetLocalString(owner, "CONVERSATION");
                    if (!string.IsNullOrWhiteSpace(conversation))
                    {
                        _dialog.StartConversation(player, owner, conversation);
                    }
                    else
                    {
                        player.AssignCommand(() =>
                        {
                            _.ActionStartConversation(owner, "", TRUE, FALSE);
                        });
                    }
                }

            }

            return true;
        }
    }
}
