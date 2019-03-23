using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Conversation
{
    public class ApartmentEntrance : ConversationBase
    {
        
        private readonly IAreaService _area;
        private readonly IBaseService _base;

        public ApartmentEntrance(
            
            IDialogService dialog,
            
            IAreaService area,
            IBaseService @base)
            : base(dialog)
        {
            
            _area = area;
            _base = @base;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage("Please select which apartment you would like to enter from the list below. If you do not have an apartment but would like to rent one please use the nearby Apartment Terminal.");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
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
            NWPlaceable door = Object.OBJECT_SELF;
            int apartmentBuildingID = door.GetLocalInt("APARTMENT_BUILDING_ID");

            if (apartmentBuildingID <= 0)
            {
                _.SpeakString("APARTMENT_BUILDING_ID is not set. Please inform an admin.");
                return;
            }

            ClearPageResponses("MainPage");

            var player = GetPC();

            // Get apartments owned by player.
            var apartments = DataService.GetAll<PCBase>().Where(x => x.PlayerID == player.GlobalID &&
                                                         x.ApartmentBuildingID == apartmentBuildingID &&
                                                         x.DateRentDue > DateTime.UtcNow)
                                             .OrderBy(o => o.DateInitialPurchase)
                                             .ToList();

            // Get apartments owned by other players and the current player currently has access to.
            var permissions = DataService.GetAll<PCBasePermission>().Where(x => x.PlayerID == player.GlobalID);
            var permissionedApartments = DataService.Where<PCBase>(x =>
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
                var owner = DataService.Get<Player>(apartment.PlayerID);
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
            NWPlayer oPC = GetPC();

            var apartment = DataService.Get<PCBase>(pcBaseID);
            var permission = DataService.SingleOrDefault<PCBasePermission>(x => x.PlayerID == oPC.GlobalID && 
                                                                          x.PCBaseID == pcBaseID &&
                                                                          !x.IsPublicPermission);

            if (permission == null || !permission.CanEnterBuildings)
            {
                oPC.FloatingText("You do not have permission to enter that apartment.");
                return;
            }

            NWArea instance = _base.GetAreaInstance(pcBaseID, true);

            if (instance == null)
            {
                instance = _base.CreateAreaInstance(oPC, pcBaseID, true);
            }

            _base.JumpPCToBuildingInterior(oPC, instance);
        }

        public override void EndDialog()
        {
        }
    }
}
