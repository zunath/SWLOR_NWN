using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingEntrance : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
                "Please select an option.",
                "Enter the building",
                "Knock on the door");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable door = GetDialogTarget().Object;
            var structureID = new Guid(door.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var canEnterBuilding = BasePermissionService.HasStructurePermission(GetPC(), structureID, StructurePermission.CanEnterBuilding);

            SetHeader();
            SetResponseVisible("MainPage", 1, canEnterBuilding);
        }

        private void SetHeader()
        {
            NWPlaceable door = GetDialogTarget().Object;
            var structureID = new Guid(door.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structure = DataService.PCBaseStructure.GetByID(structureID);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            var owner = PlayerService.GetPlayerEntity(pcBase.PlayerID);
            var buildingName = owner.CharacterName + "'s Building";
            if (!string.IsNullOrWhiteSpace(structure.CustomName))
            {
                buildingName = structure.CustomName;
            }

            var header = buildingName + "\n\nPlease select an option.";
            SetPageHeader("MainPage", header);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleMainPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Enter the building
                    DoEnterBuilding();
                    break;
                case 2: // Knock on the door
                    DoKnockOnDoor();
                    break;
            }
        }

        private void DoEnterBuilding()
        {
            var oPC = GetPC();
            NWPlaceable door = GetDialogTarget().Object;
            var pcBaseStructureID = door.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(pcBaseStructureID))
            {
                FloatingTextStringOnCreature("ERROR: Door doesn't have a structure ID assigned. Notify an admin about this issue.", oPC.Object, false);
                return;
            }
            var structureID = new Guid(pcBaseStructureID);
            var canEnterBuilding = BasePermissionService.HasStructurePermission(GetPC(), structureID, StructurePermission.CanEnterBuilding);

            if (!canEnterBuilding)
            {
                oPC.FloatingText("You don't have permission to enter that building.");
                return;
            }

            var instance = BaseService.GetAreaInstance(structureID, false);

            if (!GetIsObjectValid(instance))
            {
                instance = BaseService.CreateAreaInstance(oPC, structureID, false);
            }

            BaseService.JumpPCToBuildingInterior(oPC, instance);
        }

        private void DoKnockOnDoor()
        {
            NWPlaceable door = GetDialogTarget().Object;
            var structureID = new Guid(door.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var instance = BaseService.GetAreaInstance(structureID, false);

            FloatingTextStringOnCreature("You knock on the door.", GetPC().Object, false);

            if (GetIsObjectValid(instance))
            {
                NWPlayer player = (GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, instance))
                    {
                        FloatingTextStringOnCreature("Someone is knocking on the front door.", player.Object, false);
                    }

                    player = (GetNextPC());
                }
            }

        }

        public override void EndDialog()
        {
        }
    }
}
