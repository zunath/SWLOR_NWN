using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using SWLOR.Game.Server.ValueObject.Structure;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingEntrance: ConversationBase
    {
        private readonly IStructureService _structure;
        private readonly IPlayerService _player;

        public BuildingEntrance(
            INWScript script, 
            IDialogService dialog, 
            IStructureService structure,
            IPlayerService player) 
            : base(script, dialog)
        {
            _structure = structure;
            _player = player;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select an option.",
                "Enter the building",
                "Knock on the door",
                "Adjust Building Permissions");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlayer oPC = GetPC();
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            Location location = door.Location;
            NWObject flag = _structure.GetTerritoryFlagOwnerOfLocation(location);
            int territoryFlagID = _structure.GetTerritoryFlagID(flag);
            int structureID = _structure.GetPlaceableStructureID(door);
            int buildingFlagID = _structure.GetPCTerritoryFlagByBuildingStructureID(structureID).PCTerritoryFlagID;

            // Only players with permission can enter the building
            if (!_structure.PlayerHasPermission(oPC, StructurePermission.CanEnterBuildings, territoryFlagID) &&
                !_structure.PlayerHasPermission(oPC, StructurePermission.CanEnterBuildings, buildingFlagID))
            {
                SetResponseVisible("MainPage", 1, false);
            }

            // Only territory owner or building owner may adjust permissions.
            BuildingOwners owners = _structure.GetBuildingOwners(territoryFlagID, structureID);
            if (oPC.GlobalID != owners.TerritoryOwner &&
                oPC.GlobalID != owners.BuildingOwner)
            {
                SetResponseVisible("MainPage", 3, false);
            }
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
                case 3: // Adjust Building Permissions
                    SwitchConversation("TerritoryFlag");
                    break;
            }
        }

        private void DoEnterBuilding()
        {
            NWPlayer oPC = GetPC();
            NWPlaceable door = (NWPlaceable)GetDialogTarget();

            int structureID = _structure.GetPlaceableStructureID(door);

            if (structureID <= 0)
            {
                _.FloatingTextStringOnCreature("ERROR: Door doesn't have a structure ID assigned. Notify an admin about this issue.", oPC.Object, NWScript.FALSE);
                return;
            }

            PCTerritoryFlagsStructure structure = _structure.GetPCStructureByID(structureID);
            
            if (structure.BuildingInterior == null)
            {
                _.FloatingTextStringOnCreature("ERROR: Could not locate interior object for structure. Notify an admin about this issue.", oPC.Object, NWScript.FALSE);
                return;
            }

            NWArea instance = GetAreaInstance(structureID);

            if (instance == null)
            {
                PCTerritoryFlag flag = _structure.GetPCTerritoryFlagByBuildingStructureID(structureID);

                string name = structure.CustomName;
                if (name == string.Empty)
                {
                    PlayerCharacter owner = _player.GetPlayerEntity(structure.PCTerritoryFlag.PlayerID);

                    if (flag.ShowOwnerName)
                    {
                        name = owner.CharacterName + "'s Building";
                    }
                    else
                    {
                        name = "Building";
                    }
                }

                instance = NWArea.Wrap(_.CreateArea(structure.BuildingInterior.AreaResref, "", name));
                instance.SetLocalInt("BUILDING_STRUCTURE_ID", structureID);
                instance.SetLocalInt("TERRITORY_FLAG_ID", flag.PCTerritoryFlagID);

                // Load structures & construction sites
                foreach (Data.Entities.ConstructionSite entity in flag.ConstructionSites)
                {
                    if (entity.IsActive)
                    {
                        _structure.CreateConstructionSiteFromEntity(entity);
                    }
                }
                foreach (PCTerritoryFlagsStructure entity in flag.PCTerritoryFlagsStructures)
                {
                    if (entity.IsActive)
                    {
                        _structure.CreateStructureFromEntity(entity);
                    }
                }

            }

            instance.SetLocalInt("BUILDING_DISABLED", 0);
            _structure.JumpPCToBuildingInterior(oPC, instance);
        }


        private NWArea GetAreaInstance(int buildingStructureID)
        {
            NWArea instance = null;
            NWArea area = NWArea.Wrap(_.GetFirstArea());
            while (area.IsValid)
            {
                if (area.GetLocalInt("BUILDING_STRUCTURE_ID") == buildingStructureID)
                {
                    instance = area;
                    break;
                }

                area = NWArea.Wrap(_.GetNextArea());
            }

            return instance;
        }



        private void DoKnockOnDoor()
        {
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            int structureID = _structure.GetPlaceableStructureID(door);
            NWArea instance = GetAreaInstance(structureID);

            _.FloatingTextStringOnCreature("You knock on the door.", GetPC().Object, NWScript.FALSE);

            if (instance != null)
            {
                NWPlayer player = NWPlayer.Wrap(_.GetFirstPC());
                while(player.IsValid)
                {
                    if (Equals(player.Area, instance))
                    {
                        _.FloatingTextStringOnCreature("Someone is knocking on the front door.", player.Object, NWScript.FALSE);
                    }

                    player = NWPlayer.Wrap(_.GetNextPC());
                }
            }

        }

        public override void EndDialog()
        {
        }
    }
}
