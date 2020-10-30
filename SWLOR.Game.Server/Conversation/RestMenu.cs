using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class RestMenu : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
                BuildMainPageHeader(player),
                ColorTokenService.Green("Open Overflow Inventory"),
                "View Skills",
                "View Perks",
                "View Blueprints",
                "View Key Items",
                "Modify Item Appearance",
                "Character Management",
                "Open Trash Can (Destroy Items)",
                "HoloCom");

            dialog.AddPage("MainPage", mainPage);

            return dialog;
        }

        public override void Initialize()
        {
            var playerID = GetPC().GlobalID;
            long overflowCount = DataService.PCOverflowItem.GetAllByPlayerID(playerID).Count();

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
                            NWObject storage = (NWScript.CreateObject(ObjectType.Placeable, "overflow_storage", player.Location));
                            player.AssignCommand(() => NWScript.ActionInteractObject(storage.Object));
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
                        // Modify Item Appearance
                        case 6:
                            SwitchConversation("ModifyItemAppearance");
                            break;
                        // Character Management
                        case 7:
                            SwitchConversation("CharacterManagement");
                            break;
                        // Open Trash Can (Destroy Items)
                        case 8:
                            EndConversation();
                            NWPlaceable trashCan = (NWScript.CreateObject(ObjectType.Placeable, "reo_trash_can", player.Location));

                            player.AssignCommand(() => NWScript.ActionInteractObject(trashCan.Object));
                            NWScript.DelayCommand(0.2f, () => trashCan.IsUseable = false);
                            break;
                        // HoloCom
                        case 9:
                            SwitchConversation("HoloCom");
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
            var playerEntity = DataService.Player.GetByID(player.GlobalID);
            var association = DataService.Association.GetByID(playerEntity.AssociationID);

            // Get all player skills and then sum them up by the rank.
            var totalSkillCount = DataService.PCSkill
                .GetAllByPlayerID(player.GlobalID)
                .Where(x => DataService.Skill.GetByID(x.SkillID).ContributesToSkillCap)
                .Sum(s => s.Rank);

            var header = ColorTokenService.Green("Name: ") + player.Name + "\n";
            header += ColorTokenService.Green("Association: ") + association.Name + "\n\n";
            header += ColorTokenService.Green("Skill Points: ") + totalSkillCount + " / " + SkillService.SkillCap + "\n";
            header += ColorTokenService.Green("Unallocated SP: ") + playerEntity.UnallocatedSP + "\n";
            header += ColorTokenService.Green("Roleplay XP: ") + playerEntity.RoleplayXP + "\n";
            header += ColorTokenService.Green("FP: ")  + (playerEntity.MaxFP > 0 ? MenuService.BuildBar(playerEntity.CurrentFP, playerEntity.MaxFP, 100, ColorTokenService.TokenStart(32, 223, 219)) : "N/A") + "\n";

            return header;
        }


    }
}
