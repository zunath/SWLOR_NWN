using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class RestMenu : ConversationBase
    {
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;
        private readonly ISkillService _skill;
        private readonly IMenuService _menu;

        public RestMenu(INWScript script,
            IDialogService dialog,
            IColorTokenService color,
            IDataContext db,
            ISkillService skill,
            IMenuService menu)
            : base(script, dialog)
        {
            _color = color;
            _db = db;
            _skill = skill;
            _menu = menu;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                BuildMainPageHeader(player),
                _color.Green("Open Overflow Inventory"),
                "View Skills",
                "View Perks",
                "View Blueprints",
                "View Key Items",
                "Modify Clothes",
                "Character Management",
                "Open Trash Can (Destroy Items)");

            dialog.AddPage("MainPage", mainPage);

            return dialog;
        }

        public override void Initialize()
        {
            string playerID = GetPC().GlobalID;
            long overflowCount = _db.PCOverflowItems.Where(x => x.PlayerID == playerID).LongCount();

            if (overflowCount <= 0)
            {
                SetResponseVisible("MainPage", 1, false);
            }

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    switch (responseID)
                    {
                        // Open Overflow Inventory
                        case 1:
                            NWObject storage = (_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "overflow_storage", player.Location));
                            player.AssignCommand(() => _.ActionInteractObject(storage.Object));
                            break;
                        // View Skills
                        case 2:
                            SwitchConversation("ViewSkills");
                            break;
                        // View Perks
                        case 3:
                            SwitchConversation("ViewPerks");
                            break;
                        // View Blueprints
                        case 4:
                            SwitchConversation("ViewBlueprints");
                            break;
                        // Key Item Categories Page
                        case 5:
                            SwitchConversation("KeyItems");
                            break;
                        // Modify Clothes
                        case 6:
                            player.AssignCommand(() => _.ActionStartConversation(player.Object, "x0_skill_ctrap", 1, 0));
                            break;
                        // Character Management
                        case 7:
                            SwitchConversation("CharacterManagement");
                            break;
                        // Open Trash Can (Destroy Items)
                        case 8:
                            EndConversation();
                            NWPlaceable trashCan = (_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "reo_trash_can", player.Location));

                            player.AssignCommand(() => _.ActionInteractObject(trashCan.Object));
                            _.DelayCommand(0.2f, () => trashCan.IsUseable = false);
                            break;
                    }
                    break;

            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }

        private string BuildMainPageHeader(NWPlayer player)
        {
            PlayerCharacter playerEntity = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            int totalSkillCount = _db.PCSkills.Where(x => x.PlayerID == player.GlobalID && x.Skill.ContributesToSkillCap).Sum(s => s.Rank);

            string header = _color.Green("Name: ") + player.Name + "\n";
            header += _color.Green("Association: ") + playerEntity.Association.Name + "\n\n";
            header += _color.Green("Skill Points: ") + totalSkillCount + " / " + _skill.SkillCap + "\n";
            header += _color.Green("Unallocated SP: ") + playerEntity.UnallocatedSP + "\n";
            header += _color.Green("FP: ")  + (playerEntity.MaxFP > 0 ? _menu.BuildBar(playerEntity.CurrentFP, playerEntity.MaxFP, 100, _color.TokenStart(32, 223, 219)) : "N/A") + "\n";

            return header;
        }


    }
}
