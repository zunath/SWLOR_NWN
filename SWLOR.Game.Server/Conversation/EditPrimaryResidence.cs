using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;

namespace SWLOR.Game.Server.Conversation
{
    public class EditPrimaryResidence: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                string.Empty,
                "Select as my primary residence",                                 
                "Revoke primary residence");

            DialogPage revokePage = new DialogPage("Are you sure you want to revoke the current resident's residency?",
                "Yes, revoke their residency");

            DialogPage setAsResidence = new DialogPage("Are you sure you want to claim this building as your primary residence?\n\nThe current resident's residency will be revoked.",
                "Yes, set as my primary residence");
            
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
            var data = BaseService.GetPlayerTempData(GetPC());
            
            Player player;
            
            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship)
            {
                Guid structureID = data.StructureID;
                player = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(structureID);
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                Guid pcBaseID = data.PCBaseID;
                player = DataService.Player.GetByPrimaryResidencePCBaseIDOrDefault(pcBaseID);
            }
            else
            {
                SetPageHeader("MainPage", "Unable to select residency for this building type. Notify an admin there's a problem.");
                return;
            }

            string residentName = player == null ? "[Unclaimed]" : player.CharacterName;

            string header = "Selecting a primary residence grants your character benefits such as increased XP gain and bonuses. These are modified based on the structures you place inside your primary residence.\n\n" +
                            "You may only have one primary residence at a time. A building or starship may only have one primary resident at a time.\n\n" +
                            "Your primary residence may be revoked by the base owner or any player with permission to do so.\n\n";
            header += ColorTokenService.Green("Primary Resident: ") + residentName + "\n";

            SetPageHeader("MainPage", header);
        }

        private void BuildMainPageResponses()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);

            Player dbPlayer = DataService.Player.GetByID(player.GlobalID);
            Player primaryResident;

            bool isPrimaryResident;
            bool canEditPrimaryResidence;
            bool canRemovePrimaryResidence;

            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship) 
            {
                Guid structureID = data.StructureID;
                primaryResident = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(structureID);

                isPrimaryResident = dbPlayer.PrimaryResidencePCBaseStructureID != null && dbPlayer.PrimaryResidencePCBaseStructureID == structureID;
                canEditPrimaryResidence = BasePermissionService.HasStructurePermission(player, structureID, StructurePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = BasePermissionService.HasStructurePermission(player, structureID, StructurePermission.CanRemovePrimaryResidence);

            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                Guid pcBaseID = data.PCBaseID;
                primaryResident = DataService.Player.GetByPrimaryResidencePCBaseIDOrDefault(pcBaseID);

                isPrimaryResident = dbPlayer.PrimaryResidencePCBaseID != null && dbPlayer.PrimaryResidencePCBaseID == pcBaseID;
                canEditPrimaryResidence = BasePermissionService.HasBasePermission(player, pcBaseID, BasePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = BasePermissionService.HasBasePermission(player, pcBaseID, BasePermission.CanRemovePrimaryResidence);
            }
            else
            {
                throw new Exception("Invalid building type on EditPrimaryResidence conversation. Type = " + data.BuildingType);
            }
            
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

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
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
            }
        }

        private void SetAsResidenceResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    DoSetAsResidence();
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
            }
        }
        private void DoSetAsResidence()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var newResident = DataService.Player.GetByID(player.GlobalID);
            
            Player currentResident;
            bool isPrimaryResident;
            bool canEditPrimaryResidence;
            bool canRemovePrimaryResidence;

            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship)
            {
                Guid structureID = data.StructureID;
                currentResident = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(structureID);

                isPrimaryResident = newResident.PrimaryResidencePCBaseStructureID != null && newResident.PrimaryResidencePCBaseStructureID == structureID;
                canEditPrimaryResidence = BasePermissionService.HasStructurePermission(player, structureID, StructurePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = BasePermissionService.HasStructurePermission(player, structureID, StructurePermission.CanRemovePrimaryResidence);
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                Guid pcBaseID = data.PCBaseID;
                currentResident = DataService.Player.GetByPrimaryResidencePCBaseIDOrDefault(pcBaseID);

                isPrimaryResident = newResident.PrimaryResidencePCBaseID != null && newResident.PrimaryResidencePCBaseID == pcBaseID;
                canEditPrimaryResidence = BasePermissionService.HasBasePermission(player, pcBaseID, BasePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = BasePermissionService.HasBasePermission(player, pcBaseID, BasePermission.CanRemovePrimaryResidence);
            }
            else
            {
                throw new Exception("EditPrimaryResidence -> DoSetAsResidence: Can't handle building type ID " + data.BuildingType);
            }

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
                currentResident.PrimaryResidencePCBaseID = null;
                currentResident.PrimaryResidencePCBaseStructureID = null;
                NotifyPlayer(currentResident.ID);
            }

            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship)
            {
                newResident.PrimaryResidencePCBaseStructureID = data.StructureID;
                newResident.PrimaryResidencePCBaseID = null;
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                newResident.PrimaryResidencePCBaseStructureID = null;
                newResident.PrimaryResidencePCBaseID = data.PCBaseID;
            }

            DataService.SubmitDataChange(newResident, DatabaseActionType.Update);
            BuildMainPageHeader();
            BuildMainPageResponses();
            ClearNavigationStack();
            ChangePage("MainPage", false);
        }

        private void DoRevoke()
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            Player currentResident;

            if (data.BuildingType == BuildingType.Interior)
            {
                var structureID = data.StructureID;
                currentResident = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(structureID);
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                var pcBaseID = data.PCBaseID;
                currentResident = DataService.Player.GetByPrimaryResidencePCBaseIDOrDefault(pcBaseID);
            }
            else
            {
                throw new Exception("EditPrimaryResidence -> DoRevoke: Can't handle building type " + data.BuildingType);
            }

            if (currentResident != null)
            {
                currentResident.PrimaryResidencePCBaseStructureID = null;
                currentResident.PrimaryResidencePCBaseID = null;
                DataService.SubmitDataChange(currentResident, DatabaseActionType.Update);

                NotifyPlayer(currentResident.ID);
            }

            BuildMainPageHeader();
            BuildMainPageResponses();
            ClearNavigationStack();
            ChangePage("MainPage", false);
        }

        private void NotifyPlayer(Guid playerID)
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
            BaseService.ClearPlayerTempData(GetPC());
        }
    }
}
