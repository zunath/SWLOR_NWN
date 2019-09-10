using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class ApartmentEntrance : ConversationBase
    {
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage("Please select which apartment you would like to enter from the list below. If you do not have an apartment but would like to rent one please use the nearby Apartment Terminal.");

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
            NWPlaceable door = NWGameObject.OBJECT_SELF;
            int apartmentBuildingID = door.GetLocalInt("APARTMENT_BUILDING_ID");

            if (apartmentBuildingID <= 0)
            {
                _.SpeakString("APARTMENT_BUILDING_ID is not set. Please inform an admin.");
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

            int count = 1;
            foreach (var apartment in apartments)
            {
                string name = "Apartment #" + count;

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
                string name = owner.CharacterName + "'s Apartment [" + owner.CharacterName + "]";

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
            Guid pcApartmentID = (Guid)response.CustomData;
            EnterApartment(pcApartmentID);
        }

        private void EnterApartment(Guid pcBaseID)
        {
            NWPlaceable door = NWGameObject.OBJECT_SELF;
            NWPlayer oPC = GetPC();

            int apartmentBuildingID = door.GetLocalInt("APARTMENT_BUILDING_ID");
            var permission = DataService.PCBasePermission.GetPlayerPrivatePermissionOrDefault(oPC.GlobalID, pcBaseID);

            if (permission == null || !permission.CanEnterBuildings)
            {
                oPC.FloatingText("You do not have permission to enter that apartment.");
                return;
            }

            // If we're swapping from one apartment to another (without going to an intermediary non-instance)
            // we'll run a check to see if we need to kill the current instance.
            var area = door.Area;
            _.DelayCommand(1.0f, () =>
            {
                NWPlayer player = (_.GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, area)) return;
                    player = (_.GetNextPC());
                }

                AreaService.DestroyAreaInstance(area);
            });


            // Get or create the new apartment instance.
            NWArea instance = BaseService.GetAreaInstance(pcBaseID, true);
            if (instance == null)
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
