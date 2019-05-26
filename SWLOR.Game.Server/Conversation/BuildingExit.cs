using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingExit : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select an option.",
                "Exit the building",
                "Peek outside",
                "Enter Another Apartment"
            );

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable door = GetDialogTarget().Object;
            NWArea area = door.Area;
            BuildingType type = (BuildingType)area.GetLocalInt("BUILDING_TYPE");
            bool isPreview = area.GetLocalInt("IS_BUILDING_PREVIEW") == TRUE;
            bool canPeek = type == BuildingType.Interior && !isPreview;
            bool canChangeApartment = type == BuildingType.Apartment && !isPreview;

            SetResponseVisible("MainPage", 2, canPeek);
            SetResponseVisible("MainPage", 3, canChangeApartment);
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
                case 1: // Exit the building
                    DoExitBuilding();
                    break;
                case 2: // Peek outside
                    DoPeekOutside();
                    break;
                case 3: // Enter Another Apartment
                    SwitchConversation("ApartmentEntrance");
                    break;
            }
        }

        private void DoExitBuilding()
        {
            NWPlaceable door = GetDialogTarget().Object;
            BaseService.DoPlayerExitBuildingInstance(GetPC(), door);
        }

        private void DoPeekOutside()
        {
            const float MaxDistance = 2.5f;
            NWPlaceable door = GetDialogTarget().Object;
            Location location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");

            int numberFound = 0;
            int nth = 1;
            NWCreature nearest = (_.GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, location, nth));
            while (nearest.IsValid)
            {
                if (_.GetDistanceBetweenLocations(location, nearest.Location) > MaxDistance) break;

                if (nearest.IsPlayer)
                {
                    numberFound++;
                }

                nth++;
                nearest = (_.GetNearestObjectToLocation(OBJECT_TYPE_CREATURE, location, nth));
            }

            if (numberFound <= 0)
            {
                _.FloatingTextStringOnCreature("You don't see anyone outside.", GetPC().Object, FALSE);
            }
            else if (numberFound == 1)
            {
                _.FloatingTextStringOnCreature("You see one person outside.", GetPC().Object, FALSE);
            }
            else
            {
                _.FloatingTextStringOnCreature("You see " + numberFound + " people outside.", GetPC().Object, FALSE);
            }

        }
        public override void EndDialog()
        {
        }
    }
}
