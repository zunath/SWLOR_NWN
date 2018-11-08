using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Conversation
{
    public class ApartmentEntrance : ConversationBase
    {
        private readonly IDataService _data;
        private readonly IAreaService _area;
        private readonly IBaseService _base;

        public ApartmentEntrance(
            INWScript script,
            IDialogService dialog,
            IDataService data,
            IAreaService area,
            IBaseService @base)
            : base(script, dialog)
        {
            _data = data;
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
            var apartments = _data.GetAll<PCBase>().Where(x => x.PlayerID == player.GlobalID &&
                                                         x.ApartmentBuildingID == apartmentBuildingID &&
                                                         x.DateRentDue > DateTime.UtcNow)
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

            var apartment = _data.Get<PCBase>(pcBaseID);
            var structures = _data.Where<PCBaseStructure>(x => x.PCBaseID == apartment.ID);
            var buildingStyle = _data.Get<BuildingStyle>(apartment.BuildingStyleID);
            NWArea instance = GetAreaInstance(pcBaseID);

            if (instance == null)
            {
                string name = oPC.Name + "'s Apartment";
                if (!string.IsNullOrWhiteSpace(apartment.CustomName))
                {
                    name = apartment.CustomName;
                }

                instance = _area.CreateAreaInstance(oPC, buildingStyle.Resref, name, "PLAYER_HOME_ENTRANCE");
                instance.SetLocalString("PC_BASE_ID", pcBaseID.ToString());
                instance.SetLocalInt("BUILDING_TYPE", (int)BuildingType.Apartment);

                foreach (var furniture in structures)
                {
                    _base.SpawnStructure(instance, furniture.ID);
                }
            }

            _base.JumpPCToBuildingInterior(oPC, instance);
        }



        private NWArea GetAreaInstance(Guid pcApartmentID)
        {
            NWArea instance = null;
            foreach (var area in NWModule.Get().Areas)
            {
                string pcBaseID = area.GetLocalString("PC_BASE_ID");
                if (string.IsNullOrWhiteSpace(pcBaseID)) continue;
                
                Guid pcBaseGuid = new Guid(pcBaseID);
                if (pcBaseGuid == pcApartmentID)
                {
                    instance = area;
                    break;
                }
            }

            return instance;
        }

        public override void EndDialog()
        {
        }
    }
}
