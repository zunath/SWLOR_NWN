using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class BuildingExit : ConversationBase
    {
        private readonly IBaseService _base;

        public BuildingExit(
            INWScript script,
            IDialogService dialog,
            IBaseService @base)
            : base(script, dialog)
        {
            _base = @base;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select an option.",
                "Exit the building",
                "Peek outside"
            );

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            NWArea area = door.Area;
            
            if (area.GetLocalInt("IS_BUILDING_PREVIEW") == 1)
            {
                SetResponseVisible("MainPage", 2, false);
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
            }
        }

        private void DoExitBuilding()
        {
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            _base.DoPlayerExitBuildingInstance(GetPC(), door);
        }

        private void DoPeekOutside()
        {
            const float MaxDistance = 2.5f;
            NWPlaceable door = (NWPlaceable)GetDialogTarget();
            Location location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");

            int numberFound = 0;
            int nth = 1;
            NWCreature nearest = (_.GetNearestObjectToLocation(NWScript.OBJECT_TYPE_CREATURE, location, nth));
            while (nearest.IsValid)
            {
                if (_.GetDistanceBetweenLocations(location, nearest.Location) > MaxDistance) break;

                if (nearest.IsPlayer)
                {
                    numberFound++;
                }

                nth++;
                nearest = (_.GetNearestObjectToLocation(NWScript.OBJECT_TYPE_CREATURE, location, nth));
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
