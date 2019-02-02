﻿using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;

namespace SWLOR.Game.Server.Conversation
{
    public class EditPrimaryResidence: ConversationBase
    {
        private readonly IDataService _data;
        private readonly IBaseService _base;
        private readonly IBasePermissionService _perm;
        private readonly IColorTokenService _color;
        private readonly IDeathService _death;

        public EditPrimaryResidence(
            INWScript script, 
            IDialogService dialog,
            IDataService data,
            IBaseService @base,
            IBasePermissionService perm,
            IColorTokenService color,
            IDeathService death) 
            : base(script, dialog)
        {
            _data = data;
            _base = @base;
            _perm = perm;
            _color = color;
            _death = death;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                string.Empty,
                "Select as my primary residence",                                 
                "Revoke primary residence",
                "Set as respawn point.");

            DialogPage revokePage = new DialogPage("Are you sure you want to revoke the current resident's residency?",
                "Yes, revoke their residency");

            DialogPage setAsResidence = new DialogPage("Are you sure you want to claim this building as your primary residence?\n\nThe current resident's residency will be revoked.",
                "Yes, set as my primary residence");
            DialogPage setRespawnPoint = new DialogPage("Are you sure you want to set this residence as your respawn point? You will spawn here once you die.",
                "Yes, I would like to respawn here from now on.");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("RevokePage", revokePage);
            dialog.AddPage("SetAsResidencePage", setAsResidence);
            dialog.AddPage("SetRespawnPoint", setRespawnPoint);
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
            
            Player player;
            
            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship)
            {
                Guid structureID = data.StructureID;
                player = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseStructureID == structureID);
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                Guid pcBaseID = data.PCBaseID;
                player = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseID == pcBaseID);
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
            header += _color.Green("Primary Resident: ") + residentName + "\n";

            SetPageHeader("MainPage", header);
        }

        private void BuildMainPageResponses()
        {
            var player = GetPC();
            var data = _base.GetPlayerTempData(player);

            Player dbPlayer = _data.Single<Player>(x => x.ID == player.GlobalID);
            Player primaryResident;

            bool isPrimaryResident;
            bool canEditPrimaryResidence;
            bool canRemovePrimaryResidence;

            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship) 
            {
                Guid structureID = data.StructureID;
                primaryResident = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseStructureID == structureID);

                isPrimaryResident = dbPlayer.PrimaryResidencePCBaseStructureID != null && dbPlayer.PrimaryResidencePCBaseStructureID == structureID;
                canEditPrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanRemovePrimaryResidence);

            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                Guid pcBaseID = data.PCBaseID;
                primaryResident = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseID == pcBaseID);

                isPrimaryResident = dbPlayer.PrimaryResidencePCBaseID != null && dbPlayer.PrimaryResidencePCBaseID == pcBaseID;
                canEditPrimaryResidence = _perm.HasBasePermission(player, pcBaseID, BasePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = _perm.HasBasePermission(player, pcBaseID, BasePermission.CanRemovePrimaryResidence);
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
                case "SetRespawnPoint":
                    SetRespawnPointResponses(responseID);
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
                case 3: // Set as Respawn Point
                    ChangePage("SetRespawnPoint");
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
        private void SetRespawnPointResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    DoSetRespawnPoint();
                    break;
            }



        }
        private void DoSetAsResidence()
        {
            var player = GetPC();
            var data = _base.GetPlayerTempData(player);
            var newResident = _data.Single<Player>(x => x.ID == player.GlobalID);
            
            Player currentResident;
            bool isPrimaryResident;
            bool canEditPrimaryResidence;
            bool canRemovePrimaryResidence;

            if (data.BuildingType == BuildingType.Interior || data.BuildingType == BuildingType.Starship)
            {
                Guid structureID = data.StructureID;
                currentResident = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseStructureID == structureID);

                isPrimaryResident = newResident.PrimaryResidencePCBaseStructureID != null && newResident.PrimaryResidencePCBaseStructureID == structureID;
                canEditPrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = _perm.HasStructurePermission(player, structureID, StructurePermission.CanRemovePrimaryResidence);
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                Guid pcBaseID = data.PCBaseID;
                currentResident = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseID == pcBaseID);

                isPrimaryResident = newResident.PrimaryResidencePCBaseID != null && newResident.PrimaryResidencePCBaseID == pcBaseID;
                canEditPrimaryResidence = _perm.HasBasePermission(player, pcBaseID, BasePermission.CanEditPrimaryResidence);
                canRemovePrimaryResidence = _perm.HasBasePermission(player, pcBaseID, BasePermission.CanRemovePrimaryResidence);
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

            _data.SubmitDataChange(newResident, DatabaseActionType.Update);
            BuildMainPageHeader();
            BuildMainPageResponses();
            ClearNavigationStack();
            ChangePage("MainPage", false);
        }
        private void DoSetRespawnPoint()
        {
            var player = GetPC();
            var data = _base.GetPlayerTempData(player);
            var newResident = _data.Single<Player>(x => x.ID == player.GlobalID);
            _death.SetRespawnLocation(player);
            BuildMainPageHeader();
            BuildMainPageResponses();
            ClearNavigationStack();
            ChangePage("MainPage", false);
        }
        private void DoRevoke()
        {
            var data = _base.GetPlayerTempData(GetPC());
            Player currentResident;

            if (data.BuildingType == BuildingType.Interior)
            {
                var structureID = data.StructureID;
                currentResident = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseStructureID == structureID);
            }
            else if (data.BuildingType == BuildingType.Apartment)
            {
                var pcBaseID = data.PCBaseID;
                currentResident = _data.SingleOrDefault<Player>(x => x.PrimaryResidencePCBaseID == pcBaseID);
            }
            else
            {
                throw new Exception("EditPrimaryResidence -> DoRevoke: Can't handle building type " + data.BuildingType);
            }

            if (currentResident != null)
            {
                currentResident.PrimaryResidencePCBaseStructureID = null;
                currentResident.PrimaryResidencePCBaseID = null;
                _data.SubmitDataChange(currentResident, DatabaseActionType.Update);

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
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
