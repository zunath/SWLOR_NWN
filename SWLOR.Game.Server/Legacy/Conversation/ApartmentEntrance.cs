using System;
using System.Linq;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class ApartmentEntrance : ConversationBase
    {
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");

            var mainPage = new DialogPage("Please select which apartment you would like to enter from the list below. If you do not have an apartment but would like to rent one please use the nearby Apartment Terminal.");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            if (!GetPC().IsPlayer)
            {
                EndConversation();
                return;
            }

            LoadMainPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            MainPageResponses(responseID);
        }

        public override void Back(NWPlayer player, string previousPageName, string currentPageName)
        {
        }


        private void LoadMainPage()
        {
            NWPlaceable door = OBJECT_SELF;
            var apartmentBuildingID = door.GetLocalInt("APARTMENT_BUILDING_ID");

            if (apartmentBuildingID <= 0)
            {
                SpeakString("APARTMENT_BUILDING_ID is not set. Please inform an admin.");
                return;
            }

            ClearPageResponses("MainPage");

            var player = GetPC();

            // Get apartments owned by player.
            var apartments = DataService.PCBase.GetApartmentsOwnedByPlayer(player.GlobalID, apartmentBuildingID);
            // Get apartments owned by other players and the current player currently has access to.
            var permissions = DataService.PCBasePermission.GetAllByPlayerID(player.GlobalID);
            
            
            
            var permissionedApartments = DataService.PCBase
                .GetAll()
                .Where(x =>
                {
                    if (x.ApartmentBuildingID != apartmentBuildingID ||
                        x.DateRentDue <= DateTime.UtcNow ||
                        x.PlayerID == player.GlobalID) return false;
                    
                    var permission = permissions.SingleOrDefault(p => p.PCBaseID == x.ID);
                    return permission != null && permission.CanEnterBuildings;
                })
                .OrderBy(o => o.DateInitialPurchase)
                .ToList();

            var count = 1;
            foreach (var apartment in apartments)
            {
                var name = "Apartment #" + count;

                if (!string.IsNullOrWhiteSpace(apartment.CustomName))
                {
                    name = apartment.CustomName;
                }

                AddResponseToPage("MainPage", name, true, apartment.ID);

                count++;
            }

            foreach (var apartment in permissionedApartments)
            {
                var owner = DataService.Player.GetByID(apartment.PlayerID);
                var name = owner.CharacterName + "'s Apartment [" + owner.CharacterName + "]";

                if (!string.IsNullOrWhiteSpace(apartment.CustomName))
                {
                    name = apartment.CustomName + " [" + owner.CharacterName + "]";
                }

                AddResponseToPage("MainPage", name, true, apartment.ID);
            }

        }

        private void MainPageResponses(int responseID)
        {
            var response = GetResponseByID("MainPage", responseID);
            var pcApartmentID = (Guid)response.CustomData;
            EnterApartment(pcApartmentID);
        }

        private void EnterApartment(Guid pcBaseID)
        {
            NWPlaceable door = OBJECT_SELF;
            var oPC = GetPC();

            var apartmentBuildingID = door.GetLocalInt("APARTMENT_BUILDING_ID");
            var permission = DataService.PCBasePermission.GetPlayerPrivatePermissionOrDefault(oPC.GlobalID, pcBaseID);

            if (permission == null || !permission.CanEnterBuildings)
            {
                oPC.FloatingText("You do not have permission to enter that apartment.");
                return;
            }

            // If we're swapping from one apartment to another (without going to an intermediary non-instance)
            // we'll run a check to see if we need to kill the current instance.
            var area = door.Area;
            DelayCommand(1.0f, () =>
            {
                NWPlayer player = (GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, area)) return;
                    player = (GetNextPC());
                }

                AreaService.DestroyAreaInstance(area);
            });


            // Get or create the new apartment instance.
            var instance = BaseService.GetAreaInstance(pcBaseID, true);
            if (!GetIsObjectValid(instance))
            {
                instance = BaseService.CreateAreaInstance(oPC, pcBaseID, true);
            }

            // Port the player to the new instance.
            BaseService.JumpPCToBuildingInterior(oPC, instance, apartmentBuildingID);
        }

        public override void EndDialog()
        {
        }
    }
}
