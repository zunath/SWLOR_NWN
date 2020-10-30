using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingExit : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
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
            var area = door.Area;
            var type = (BuildingType)area.GetLocalInt("BUILDING_TYPE");
            var isPreview = area.GetLocalBool("IS_BUILDING_PREVIEW") == true;
            var canPeek = type == BuildingType.Interior && !isPreview;
            var canChangeApartment = type == BuildingType.Apartment && !isPreview;

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
            var location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");

            var numberFound = 0;
            var nth = 1;
            NWCreature nearest = (NWScript.GetNearestObjectToLocation(location, ObjectType.Creature, nth));
            while (nearest.IsValid)
            {
                if (NWScript.GetDistanceBetweenLocations(location, nearest.Location) > MaxDistance) break;

                if (nearest.IsPlayer)
                {
                    numberFound++;
                }

                nth++;
                nearest = (NWScript.GetNearestObjectToLocation(location, ObjectType.Creature, nth));
            }

            if (numberFound <= 0)
            {
                NWScript.FloatingTextStringOnCreature("You don't see anyone outside.", GetPC().Object, false);
            }
            else if (numberFound == 1)
            {
                NWScript.FloatingTextStringOnCreature("You see one person outside.", GetPC().Object, false);
            }
            else
            {
                NWScript.FloatingTextStringOnCreature("You see " + numberFound + " people outside.", GetPC().Object, false);
            }

        }
        public override void EndDialog()
        {
        }
    }
}
