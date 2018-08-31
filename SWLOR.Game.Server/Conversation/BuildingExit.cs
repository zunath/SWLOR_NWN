using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using SWLOR.Game.Server.ValueObject.Structure;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingExit: ConversationBase
    {
        private readonly IStructureService _structure;

        public BuildingExit(
            INWScript script, 
            IDialogService dialog,
            IStructureService structure) 
            : base(script, dialog)
        {
            _structure = structure;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select an option.",
                "Exit the building",
                "Peek outside",
                "Adjust Building Permissions"
            );

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlayer oPC = GetPC();
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            NWArea area = door.Area;
            Location location = door.Location;
            NWObject flag = _structure.GetTerritoryFlagOwnerOfLocation(location);
            int territoryFlagID = _structure.GetTerritoryFlagID(flag);
            int structureID = _structure.GetPlaceableStructureID(door);

            // Only territory owner or building owner may adjust permissions.
            BuildingOwners owners = _structure.GetBuildingOwners(territoryFlagID, structureID);
            if (oPC.GlobalID != owners.TerritoryOwner &&
                oPC.GlobalID != owners.BuildingOwner)
            {
                SetResponseVisible("MainPage", 3, false);
            }

            if (area.GetLocalInt("IS_BUILDING_PREVIEW") == 1)
            {
                SetResponseVisible("MainPage", 2, false);
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
                case 1: // Exit the building
                    DoExitBuilding();
                    break;
                case 2: // Peek outside
                    DoPeekOutside();
                    break;
                case 3: // Adjust building permissions
                    SwitchConversation("TerritoryFlag");
                    break;
            }
        }

        private void DoExitBuilding()
        {
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            NWArea oArea = door.Area;
            Location location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");

            GetPC().AssignCommand(() => _.ActionJumpToLocation(location));

            GetPC().DelayCommand(() =>
            {
                NWPlayer player = NWPlayer.Wrap(_.GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, oArea)) return;
                    player = NWPlayer.Wrap(_.GetNextPC());
                }

                _.DestroyArea(oArea.Object);
            }, 1.0f);
        }

        private void DoPeekOutside()
        {
            const float MaxDistance = 2.5f;
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            Location location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");

            int numberFound = 0;
            int nth = 1;
            NWCreature nearest = NWCreature.Wrap(_.GetNearestObjectToLocation(NWScript.OBJECT_TYPE_CREATURE, location, nth));
            while (nearest.IsValid)
            {
                if (_.GetDistanceBetweenLocations(location, nearest.Location) > MaxDistance) break;

                if (nearest.IsPlayer)
                {
                    numberFound++;
                }

                nth++;
                nearest = NWCreature.Wrap(_.GetNearestObjectToLocation(NWScript.OBJECT_TYPE_CREATURE, location, nth));
            }

            if (numberFound <= 0)
            {
                _.FloatingTextStringOnCreature("You don't see anyone outside.", GetPC().Object, NWScript.FALSE);
            }
            else if (numberFound == 1)
            {
                _.FloatingTextStringOnCreature("You see one person outside.", GetPC().Object, NWScript.FALSE);
            }
            else
            {
                _.FloatingTextStringOnCreature("You see " + numberFound + " people outside.", GetPC().Object, NWScript.FALSE);
            }

        }
        public override void EndDialog()
        {
        }
    }
}
