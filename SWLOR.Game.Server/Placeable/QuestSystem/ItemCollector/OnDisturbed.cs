using System;
using System.Linq;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.QuestSystem.ItemCollector
{
    public class OnDisturbed : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IQuestService _quest;
        private readonly IColorTokenService _color;
        private readonly IDialogService _dialog;

        public OnDisturbed(
            INWScript script,
            IDataContext db,
            IQuestService quest,
            IColorTokenService color,
            IDialogService dialog)
        {
            _ = script;
            _db = db;
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

            if (disturbType == INVENTORY_DISTURB_TYPE_ADDED)
            {
                int questID = container.GetLocalInt("QUEST_ID");
                PCQuestStatus status = _db.PCQuestStatus.Single(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
                PCQuestItemProgress progress = status.PCQuestItemProgresses.SingleOrDefault(x => x.Resref == item.Resref);

                if (progress == null)
                {
                    _.CopyItem(item, player, TRUE);
                    player.SendMessage(_color.Red("That item is not required for this quest."));
                }
                else
                {
                    progress.Remaining--;

                    if (progress.Remaining <= 0)
                    {
                        progress = _db.PCQuestItemProgresses.Single(x => x.PCQuestItemProgressID == progress.PCQuestItemProgressID);
                        _db.PCQuestItemProgresses.Remove(progress);
                    }

                    _db.SaveChanges();

                    // Recalc the remaining items needed.
                    int remainingCount = _db.PCQuestItemProgresses.Count(x => x.PCQuestStatusID == status.PCQuestStatusID);
                    if (remainingCount <= 0)
                    {
                        _quest.AdvanceQuestState(player, owner, questID);
                    }

                    player.SendMessage("You need " + progress.Remaining + " " + item.Name + " for this quest.");
                }
                item.Destroy();


                if (status.PCQuestItemProgresses.Count <= 0)
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
