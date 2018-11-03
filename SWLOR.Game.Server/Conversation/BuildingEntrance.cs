using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingEntrance : ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IPlayerService _player;
        private readonly IDataService _data;
        private readonly IAreaService _area;
        private readonly IBasePermissionService _perm;

        public BuildingEntrance(
            INWScript script,
            IDialogService dialog,
            IBaseService @base,
            IPlayerService player,
            IDataService data,
            IAreaService area,
            IBasePermissionService perm)
            : base(script, dialog)
        {
            _base = @base;
            _player = player;
            _data = data;
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
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            int structureID = door.GetLocalInt("PC_BASE_STRUCTURE_ID");
            bool canEnterBuilding = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanEnterBuilding);

            SetResponseVisible("MainPage", 1, canEnterBuilding);
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
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            int structureID = door.GetLocalInt("PC_BASE_STRUCTURE_ID");
            
            if (structureID <= 0)
            {
                _.FloatingTextStringOnCreature("ERROR: Door doesn't have a structure ID assigned. Notify an admin about this issue.", oPC.Object, NWScript.FALSE);
                return;
            }

            bool canEnterBuilding = _perm.HasStructurePermission(GetPC(), structureID, StructurePermission.CanEnterBuilding);

            if (!canEnterBuilding)
            {
                oPC.FloatingText("You don't have permission to enter that building.");
                return;
            }

            var structure = _data.PCBaseStructures.Single(x => x.PCBaseStructureID == structureID);
            NWArea instance = GetAreaInstance(structureID);

            if (instance == null)
            {
                string name = structure.CustomName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    PlayerCharacter owner = _player.GetPlayerEntity(structure.PCBase.PlayerID);
                    name = owner.CharacterName + "'s Building";
                }

                instance = _area.CreateAreaInstance(oPC, structure.InteriorStyle.Resref, name, "PLAYER_HOME_ENTRANCE");
                instance.SetLocalInt("PC_BASE_STRUCTURE_ID", structureID);
                instance.SetLocalInt("BUILDING_TYPE", (int)BuildingType.Interior);

                foreach (var child in structure.ChildStructures)
                {
                    _base.SpawnStructure(instance, child.PCBaseStructureID);
                }

            }
            
            _base.JumpPCToBuildingInterior(oPC, instance);
        }



        private NWArea GetAreaInstance(int buildingStructureID)
        {
            NWArea instance = null;
            foreach(var area in NWModule.Get().Areas)
            {
                if (area.GetLocalInt("PC_BASE_STRUCTURE_ID") == buildingStructureID)
                {
                    instance = area;
                    break;
                }
            }

            return instance;
        }



        private void DoKnockOnDoor()
        {
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            int structureID = door.GetLocalInt("PC_BASE_STRUCTURE_ID");
            NWArea instance = GetAreaInstance(structureID);

            _.FloatingTextStringOnCreature("You knock on the door.", GetPC().Object, NWScript.FALSE);

            if (instance != null)
            {
                NWPlayer player = (_.GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, instance))
                    {
                        _.FloatingTextStringOnCreature("Someone is knocking on the front door.", player.Object, NWScript.FALSE);
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
