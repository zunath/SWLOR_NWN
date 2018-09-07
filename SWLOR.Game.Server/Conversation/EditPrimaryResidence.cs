using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class EditPrimaryResidence: ConversationBase
    {
        private readonly IDataContext _db;
        private readonly IBaseService _base;
        private readonly IBasePermissionService _perm;
        private readonly IColorTokenService _color;

        public EditPrimaryResidence(
            INWScript script, 
            IDialogService dialog,
            IDataContext db,
            IBaseService @base,
            IBasePermissionService perm,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _db = db;
            _base = @base;
            _perm = perm;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                string.Empty,
                "Select as my primary residence",                                 
                "Revoke primary residence",
                "Back");

            DialogPage revokePage = new DialogPage("Are you sure you want to revoke the current resident's residency?",
                "Yes, revoke their residency",
                "Cancel");

            DialogPage setAsResidence = new DialogPage("Are you sure you want to claim this building as your primary residence?\n\nThe current resident's residency will be revoked.",
                "Yes, set as my primary residence",
                "Cancel");


            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("RevokePage", revokePage);
            dialog.AddPage("SetAsResidencePage", setAsResidence);
            return dialog;
        }

        public override void Initialize()
        {
            BuildMainPageHeader();
            BuildMainPageResponses();
        }

        private void BuildMainPageHeader()
        {
            var data = _base.GetPlayerTempData(GetPC());
            int structureID = data.StructureID;
            var structure = _db.PlayerCharacters.SingleOrDefault(x => x.PrimaryResidencePCBaseStructureID == structureID);
            string residentName = structure == null ? "[Unclaimed]" : structure.CharacterName;

            string header = "Selecting a primary residence grants your character benefits such as increased XP gain and bonuses. These are modified based on the structures you place inside your primary residence.\n\n" +
                            "You may only have one primary residence at a time. A building or starship may only have one primary resident at a time.\n\n" +
                            "Your primary residence may be revoked by the base owner or any player with permission to do so.\n\n";
            header += _color.Green("Primary Resident: ") + residentName + "\n";

            SetPageHeader("MainPage", header);
        }

        private void BuildMainPageResponses()
        {
            var player = GetPC();
            var data = _base.GetPlayerTempData(player);
            int structureID = data.StructureID;
            var dbPlayer = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
            var primaryResident = _db.PlayerCharacters.SingleOrDefault(x => x.PrimaryResidencePCBaseStructureID == structureID);

            bool isPrimaryResident = dbPlayer.PrimaryResidencePCBaseStructureID != null && dbPlayer.PrimaryResidencePCBaseStructureID == structureID;
            bool canEditPrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanEditPrimaryResidence);
            bool canRemovePrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanRemovePrimaryResidence);

            // If another person is a resident and this player does not have the "remove" permission, don't allow them to make primary residence.
            if (!isPrimaryResident && primaryResident != null && !canRemovePrimaryResidence)
            {
                canEditPrimaryResidence = false;
            }

            SetResponseVisible("MainPage", 1, canEditPrimaryResidence);
            SetResponseVisible("MainPage", 2, canRemovePrimaryResidence || isPrimaryResident);

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "RevokePage":
                    RevokeResponses(responseID);
                    break;
                case "SetAsResidencePage":
                    SetAsResidenceResponses(responseID);
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Select as my primary residence
                    ChangePage("SetAsResidencePage");
                    break;
                case 2: // Revoke Primary Residence
                    ChangePage("RevokePage");
                    break;
                case 3: // Back
                    SwitchConversation("BaseManagementTool");
                    break;

            }
        }

        private void SetAsResidenceResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    DoSetAsResidence();
                    break;
                case 2:
                    ChangePage("MainPage");
                    break;
            }
        }

        private void RevokeResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    DoRevoke();
                    break;
                case 2:
                    ChangePage("MainPage");
                    break;
            }
        }

        private void DoSetAsResidence()
        {
            var player = GetPC();
            var data = _base.GetPlayerTempData(player);
            int structureID = data.StructureID;
            var currentResident = _db.PlayerCharacters.SingleOrDefault(x => x.PrimaryResidencePCBaseStructureID == structureID);
            var newResident = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);

            bool isPrimaryResident = newResident.PrimaryResidencePCBaseStructureID != null && newResident.PrimaryResidencePCBaseStructureID == structureID;
            bool canEditPrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanEditPrimaryResidence);
            bool canRemovePrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanRemovePrimaryResidence);

            // If another person is a resident and this player does not have the "remove" permission, don't allow them to make primary residence.
            if (!isPrimaryResident && currentResident != null && !canRemovePrimaryResidence)
            {
                player.FloatingText("You do not have permission to revoke the current resident's residency.");
                return;
            }

            if (!canEditPrimaryResidence)
            {
                player.FloatingText("You do not have permission to select this as your primary residency.");
                return;
            }

            if (currentResident != null)
            {
                currentResident.PrimaryResidencePCBaseStructureID = null;
                NotifyPlayer(currentResident.PlayerID);
            }

            newResident.PrimaryResidencePCBaseStructureID = structureID;
            _db.SaveChanges();
            BuildMainPageHeader();
            BuildMainPageResponses();
            ChangePage("MainPage");
        }

        private void DoRevoke()
        {
            var data = _base.GetPlayerTempData(GetPC());
            int structureID = data.StructureID;
            var currentResident = _db.PlayerCharacters.SingleOrDefault(x => x.PrimaryResidencePCBaseStructureID == structureID);

            if (currentResident != null)
            {
                currentResident.PrimaryResidencePCBaseStructureID = null;
                _db.SaveChanges();

                NotifyPlayer(currentResident.PlayerID);
            }

            BuildMainPageHeader();
            BuildMainPageResponses();
            ChangePage("MainPage");
        }

        private void NotifyPlayer(string playerID)
        {
            foreach (var player in NWModule.Get().Players)
            {
                if (player.GlobalID == playerID)
                {
                    player.SendMessage("Your primary residency has been revoked.");
                    return;
                }
            }
        }

        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
