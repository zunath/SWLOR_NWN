using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class StructureStorage: ConversationBase
    {
        private readonly IColorTokenService _color;
        private readonly IStructureService _structure;

        public StructureStorage(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IStructureService structure) 
            : base(script, dialog)
        {
            _color = color;
            _structure = structure;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                _color.Green("Persistent Storage Menu") + "\n\nPlease select an option.",
                "Open Storage",
                "Change Container Name"
            );

            DialogPage changeNamePage = new DialogPage(
                _color.Green("Change Container Name") + "\n\nPlease type a name for the container into your chat bar and then press enter. After that's done click the 'Next' button on this conversation window.",
                "Next",
                "Back"
            );

            DialogPage confirmChangeName = new DialogPage(
                "<SET LATER>",
                "Confirm Name Change",
                "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ChangeNamePage", changeNamePage);
            dialog.AddPage("ConfirmChangeNamePage", confirmChangeName);
            return dialog;
        }

        public override void Initialize()
        {
            int structureID = _structure.GetPlaceableStructureID((NWPlaceable)GetDialogTarget());
            PCTerritoryFlagsStructure structure = _structure.GetPCStructureByID(structureID);

            if (!_structure.PlayerHasPermission(GetPC(), StructurePermission.CanAccessPersistentStorage, structure.PCTerritoryFlagID))
            {
                SetResponseVisible("MainPage", 1, false);
            }

            if (!_structure.PlayerHasPermission(GetPC(), StructurePermission.CanRenameStructures, structure.PCTerritoryFlagID))
            {
                SetResponseVisible("MainPage", 2, false);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponse(responseID);
                    break;
                case "ChangeNamePage":
                    HandleChangeNamePageResponse(responseID);
                    break;
                case "ConfirmChangeNamePage":
                    HandleConfirmChangeNamePageResponse(responseID);
                    break;
            }

        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Open Storage
                    OpenPersistentStorage();
                    EndConversation();
                    break;
                case 2: // Change Container Name
                    GetPC().SetLocalInt("LISTENING_FOR_NEW_CONTAINER_NAME", 1);
                    ChangePage("ChangeNamePage");
                    break;
            }
        }

        private void HandleChangeNamePageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Next
                    string name = GetPC().GetLocalString("NEW_CONTAINER_NAME");
                    if (name == "")
                    {
                        GetPC().FloatingText("Type in a new name to the chat bar and then press 'Next'.");
                        return;
                    }

                    string header = _color.Green("Change Container Name") + "\n\n";
                    header += _color.Green("New Container Name: ") + name + "\n\n";
                    header += "Are you sure you want to change your container to this name?";

                    SetPageHeader("ConfirmChangeNamePage", header);
                    ChangePage("ConfirmChangeNamePage");
                    break;
                case 2: // Back
                    ClearTempVariables();
                    ChangePage("MainPage");
                    break;
            }
        }

        private void HandleConfirmChangeNamePageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm Change Name
                    string name = GetPC().GetLocalString("NEW_CONTAINER_NAME");
                    _structure.SetStructureCustomName(GetPC(), (NWPlaceable)GetDialogTarget(), name);
                    EndConversation();
                    break;
                case 2: // Back
                    ChangePage("ChangeNamePage");
                    break;
            }
        }

        private void ClearTempVariables()
        {
            GetPC().DeleteLocalInt("LISTENING_FOR_NEW_CONTAINER_NAME");
            GetPC().DeleteLocalString("NEW_CONTAINER_NAME");
        }

        public override void EndDialog()
        {
            ClearTempVariables();
        }

        private void OpenPersistentStorage()
        {
            NWPlaceable chest = (NWPlaceable)GetDialogTarget();
            NWPlayer oPC = GetPC();

            if (NWPlaceable.Wrap(chest.GetLocalObject("STRUCTURE_TEMP_INVENTORY_OPENED")).IsValid)
            {
                oPC.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                return;
            }

            int structureID = _structure.GetPlaceableStructureID(chest);
            Location location = oPC.Location;
            NWPlaceable copy = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "str_storage_copy", location));
            copy.Name = chest.Name;
            copy.AssignCommand(() => _.SetFacingPoint(oPC.Position));

            copy.SetLocalObject("STRUCTURE_TEMP_INVENTORY_OPENED", copy.Object);
            copy.SetLocalObject("STRUCTURE_TEMP_PARENT", chest.Object);
            copy.SetLocalInt("STRUCTURE_TEMP_STRUCTURE_ID", structureID);

            oPC.AssignCommand(() => _.ActionInteractObject(copy.Object));
        }

    }
}
