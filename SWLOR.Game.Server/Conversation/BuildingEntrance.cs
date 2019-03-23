using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingEntrance : ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IPlayerService _player;
        
        private readonly IAreaService _area;
        private readonly IBasePermissionService _perm;

        public BuildingEntrance(
            
            IDialogService dialog,
            IBaseService @base,
            IPlayerService player,
            
            IAreaService area,
            IBasePermissionService perm)
            : base(dialog)
        {
            _base = @base;
            _player = player;
            
            _area = area;
            _perm = perm;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
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
            bool canEnterBuilding = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanEnterBuilding);

            SetHeader();
            SetResponseVisible("MainPage", 1, canEnterBuilding);
        }

        private void SetHeader()
        {
            NWPlaceable door = GetDialogTarget().Object;
            var structureID = new Guid(door.GetLocalString("PC_BASE_STRUCTURE_ID"));
            PCBaseStructure structure = DataService.Get<PCBaseStructure>(structureID);
            PCBase pcBase = DataService.Get<PCBase>(structure.PCBaseID);
            Player owner = _player.GetPlayerEntity(pcBase.PlayerID);
            string buildingName = owner.CharacterName + "'s Building";
            if (!string.IsNullOrWhiteSpace(structure.CustomName))
            {
                buildingName = structure.CustomName;
            }

            string header = buildingName + "\n\nPlease select an option.";
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
            NWPlayer oPC = GetPC();
            NWPlaceable door = GetDialogTarget().Object;
            string pcBaseStructureID = door.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(pcBaseStructureID))
            {
                _.FloatingTextStringOnCreature("ERROR: Door doesn't have a structure ID assigned. Notify an admin about this issue.", oPC.Object, _.FALSE);
                return;
            }
            var structureID = new Guid(pcBaseStructureID);
            bool canEnterBuilding = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanEnterBuilding);

            if (!canEnterBuilding)
            {
                oPC.FloatingText("You don't have permission to enter that building.");
                return;
            }

            var structure = DataService.Single<PCBaseStructure>(x => x.ID == structureID);
            var pcBase = DataService.Get<PCBase>(structure.PCBaseID);
            var interiorStyle = DataService.Get<BuildingStyle>(structure.InteriorStyleID);

            bool starship = pcBase.PCBaseTypeID == 3;
            NWArea instance = _base.GetAreaInstance(structureID, false);

            if (instance == null)
            {
                instance = _base.CreateAreaInstance(oPC, structureID, false);
            }

            _base.JumpPCToBuildingInterior(oPC, instance);
        }

        private void DoKnockOnDoor()
        {
            NWPlaceable door = GetDialogTarget().Object;
            Guid structureID = new Guid(door.GetLocalString("PC_BASE_STRUCTURE_ID"));
            NWArea instance = _base.GetAreaInstance(structureID, false);

            _.FloatingTextStringOnCreature("You knock on the door.", GetPC().Object, _.FALSE);

            if (instance != null)
            {
                NWPlayer player = (_.GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, instance))
                    {
                        _.FloatingTextStringOnCreature("Someone is knocking on the front door.", player.Object, _.FALSE);
                    }

                    player = (_.GetNextPC());
                }
            }

        }

        public override void EndDialog()
        {
        }
    }
}
