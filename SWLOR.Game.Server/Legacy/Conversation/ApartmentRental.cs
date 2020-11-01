using System;
using System.Linq;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using BuildingType = SWLOR.Game.Server.Legacy.Enumeration.BuildingType;
using PCBaseType = SWLOR.Game.Server.Legacy.Enumeration.PCBaseType;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class ApartmentRental : ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage();
            var leasePage = new DialogPage();
            var purchaseDetailsPage = new DialogPage(string.Empty,
                "Purchase",
                "Preview");
            var detailsPage = new DialogPage(string.Empty,
                "Extend Lease (+1 day)",
                "Extend Lease (+7 days)",
                "Cancel Lease");

            var cancelLeasePage = new DialogPage(
                "Cancelling your lease will remove your right to access this apartment. All furniture you've placed will be sent to the impound. You'll need to go talk to a planetary representative to recover your impounded furniture.\n\n" +
                "Any remaining time on your lease will be forfeit. You will not receive a refund.\n\n" +
                "Are you sure you want to cancel your lease?",
                "Confirm Cancel Lease");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("LeasePage", leasePage);
            dialog.AddPage("PurchaseDetailsPage", purchaseDetailsPage);
            dialog.AddPage("DetailsPage", detailsPage);
            dialog.AddPage("CancelLeasePage", cancelLeasePage);
            return dialog;
        }

        public override void Initialize()
        {
            NWPlaceable terminal = OBJECT_SELF;
            var data = BaseService.GetPlayerTempData(GetPC());
            data.ApartmentBuildingID = terminal.GetLocalInt("APARTMENT_BUILDING_ID");

            if (data.ApartmentBuildingID <= 0)
            {
                SpeakString("APARTMENT_BUILDING_ID is not set. Please inform an admin.");
                return;
            }

            LoadMainPage();
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "LeasePage":
                    LeaseResponses(responseID);
                    break;
                case "PurchaseDetailsPage":
                    PurchaseDetailsResponses(responseID);
                    break;
                case "DetailsPage":
                    DetailsResponses(responseID);
                    break;
                case "CancelLeasePage":
                    CancelLeaseResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var data = BaseService.GetPlayerTempData(player);

            switch (beforeMovePage)
            {
                case "PurchaseDetailsPage":
                    data.IsConfirming = false;
                    SetResponseText("PurchaseDetailsPage", 1, "Purchase");
                    break;
                case "DetailsPage":
                    data.IsConfirming = false;
                    SetResponseText("DetailsPage", 1, "Extend Lease (+1 day)");
                    SetResponseText("DetailsPage", 2, "Extend Lease (+7 days)");
                    LoadMainPage();
                    break;
                case "CancelLeasePage":
                    data.IsConfirming = false;
                    SetResponseText("CancelLeasePage", 1, "Confirm Cancel Lease");
                    break;
            }
        }

        private void LoadMainPage()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var bases = DataService
                .PCBase.GetAllByPlayerID(player.GlobalID)
                .Where(x => x.ApartmentBuildingID == data.ApartmentBuildingID)
                .OrderBy(o => o.DateInitialPurchase)
                .ToList();

            var header = ColorTokenService.Green("Apartment Rental Terminal") + "\n\n";
            header += "Apartments you are currently renting can be found in the list below. You may also rent an apartment here.";
            SetPageHeader("MainPage", header);

            ClearPageResponses("MainPage");
            
            // Issue#895: GAME: DMs purchasing apartments can crash the server
            if (player.IsDM || player.IsDMPossessed) { return; }
            
            AddResponseToPage("MainPage", ColorTokenService.Green("Lease New Apartment"));
            var count = 1;
            foreach (var apartment in bases)
            {
                var name = "Apartment #" + count;

                if (!string.IsNullOrWhiteSpace(apartment.CustomName))
                {
                    name = apartment.CustomName;
                }

                AddResponseToPage("MainPage", name, true, apartment.ID);

                count++;
            }
        }

        private void MainResponses(int responseID)
        {
            if (responseID == 1)
            {
                LoadLeasePage();
                ChangePage("LeasePage");
            }
            else
            {
                var data = BaseService.GetPlayerTempData(GetPC());
                var response = GetResponseByID("MainPage", responseID);
                data.PCBaseID = (Guid)response.CustomData;

                LoadDetailsPage();
                ChangePage("DetailsPage");
            }

        }

        private void LoadLeasePage()
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            var apartmentBuilding = DataService.ApartmentBuilding.GetByID(data.ApartmentBuildingID);
            var styles = DataService.BuildingStyle
                .GetAll()
                .Where(x => x.BuildingTypeID == (int)BuildingType.Apartment && 
                            x.IsActive).ToList();

            var header = ColorTokenService.Green(apartmentBuilding.Name) + "\n\n";

            header += "You may rent an apartment here. Select a layout style from the list below to learn more about pricing details.";
            SetPageHeader("LeasePage", header);
            ClearPageResponses("LeasePage");

            foreach (var style in styles)
            {
                AddResponseToPage("LeasePage", style.Name, true, style.ID);
            }
        }

        private void LeaseResponses(int responseID)
        {
            var response = GetResponseByID("LeasePage", responseID);
            var data = BaseService.GetPlayerTempData(GetPC());
            var styleID = (int)response.CustomData;
            data.BuildingStyleID = styleID;
            
            LoadPurchaseDetailsPage();
            ChangePage("PurchaseDetailsPage");
        }

        private void LoadPurchaseDetailsPage()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(GetPC());
            var style = DataService.BuildingStyle.GetByID(data.BuildingStyleID);
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var purchasePrice = style.PurchasePrice + (int) (style.PurchasePrice * (dbPlayer.LeaseRate * 0.01f));
            var dailyUpkeep = style.DailyUpkeep + (int) (style.DailyUpkeep * (dbPlayer.LeaseRate * 0.01f));

            var header = ColorTokenService.Green("Style: ") + style.Name + "\n\n";
            header += ColorTokenService.Green("Purchase Price: ") + purchasePrice + " credits\n";
            header += ColorTokenService.Green("Daily Upkeep: ") + dailyUpkeep + " credits\n\n";
            header += "Purchasing an apartment will grant you 7 days on your lease. Leases can be extended for up to 30 days (real world time) in advance.";

            SetPageHeader("PurchaseDetailsPage", header);

            if (player.Gold < purchasePrice)
            {
                SetResponseVisible("PurchaseDetailsPage", 1, false);
            }
        }

        private void PurchaseDetailsResponses(int responseID)
        {
            var data = BaseService.GetPlayerTempData(GetPC());

            switch (responseID)
            {
                case 1: // Purchase
                    if (data.IsConfirming)
                    {
                        data.IsConfirming = false;
                        SetResponseText("PurchaseDetailsPage", 1, "Purchase");
                        DoPurchase();
                    }
                    else
                    {
                        data.IsConfirming = true;
                        SetResponseText("PurchaseDetailsPage", 1, "CONFIRM PURCHASE APARTMENT");
                    }
                    break;
                case 2: // Preview
                    DoPreview();
                    break;
            }
        }

        private void DoPreview()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(GetPC());
            var style = DataService.BuildingStyle.GetByID(data.BuildingStyleID);
            var area = AreaService.CreateAreaInstance(player, style.Resref, "APARTMENT PREVIEW: " + style.Name, "PLAYER_HOME_ENTRANCE");
            SetLocalBool(area, "IS_BUILDING_PREVIEW", true);
            BaseService.JumpPCToBuildingInterior(player, area);
        }

        private void DoPurchase()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(GetPC());
            var style = DataService.BuildingStyle.GetByID(data.BuildingStyleID);
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var purchasePrice = style.PurchasePrice + (int)(style.PurchasePrice * (dbPlayer.LeaseRate * 0.01f));

            if (player.Gold < purchasePrice)
            {
                player.SendMessage("You don't have enough credits to purchase that apartment.");
                return;
            }

            var pcApartment = new PCBase
            {
                PlayerID = player.GlobalID,
                BuildingStyleID = style.ID,
                PCBaseTypeID = (int)PCBaseType.Apartment,
                ApartmentBuildingID = data.ApartmentBuildingID,
                CustomName = string.Empty,
                DateInitialPurchase = DateTime.UtcNow,
                DateRentDue = DateTime.UtcNow.AddDays(7),
                AreaResref = style.Resref,
                DateFuelEnds = DateTime.UtcNow,
                Sector = "AP",
            };
            DataService.SubmitDataChange(pcApartment, DatabaseActionType.Insert);
            
            var permission = new PCBasePermission
            {
                PCBaseID = pcApartment.ID,
                PlayerID = player.GlobalID
            };
            DataService.SubmitDataChange(permission, DatabaseActionType.Insert);
            
            
            // Grant all base permissions to owner.
            var allPermissions = Enum.GetValues(typeof(BasePermission)).Cast<BasePermission>().ToArray();
            BasePermissionService.GrantBasePermissions(player, pcApartment.ID, allPermissions);

            TakeGoldFromCreature(purchasePrice, player, true);
            
            LoadMainPage();
            ClearNavigationStack();
            ChangePage("MainPage", false);
        }


        private void LoadDetailsPage()
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var pcApartment = DataService.PCBase.GetByID(data.PCBaseID);
            var buildingStyle = DataService.BuildingStyle.GetByID(Convert.ToInt32(pcApartment.BuildingStyleID));
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var name = player.Name + "'s Apartment";
            var dailyUpkeep = buildingStyle.DailyUpkeep + (int)(buildingStyle.DailyUpkeep * (dbPlayer.LeaseRate * 0.01f));


            if (!string.IsNullOrWhiteSpace(pcApartment.CustomName))
            {
                name = pcApartment.CustomName;
            }

            var header = ColorTokenService.Green(name) + "\n\n";
            header += ColorTokenService.Green("Purchased: ") + pcApartment.DateInitialPurchase + "\n";
            header += ColorTokenService.Green("Rent Due: ") + pcApartment.DateRentDue + "\n";
            header += ColorTokenService.Green("Daily Upkeep: ") + dailyUpkeep + " credits\n\n";
            header += "Daily upkeep may be paid up to 30 days in advance.\n";

            SetPageHeader("DetailsPage", header);

            const int MaxAdvancePay = 30;
            var newRentDate = pcApartment.DateRentDue.AddDays(1);
            var ts = newRentDate - DateTime.UtcNow;
            var canPayRent = ts.TotalDays < MaxAdvancePay;
            SetResponseVisible("DetailsPage", 1, canPayRent);

            newRentDate = pcApartment.DateRentDue.AddDays(7);
            ts = newRentDate - DateTime.UtcNow;
            canPayRent = ts.TotalDays < MaxAdvancePay;

            SetResponseVisible("DetailsPage", 2, canPayRent);
        }

        private void DetailsResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Extend Lease (+1 day)
                    ExtendLease(1, responseID, "Extend Lease (+1 day)");
                    break;
                case 2: // Extend Lease (+7 days)
                    ExtendLease(7, responseID, "Extend Lease (+7 days)");
                    break;
                case 3: // Cancel lease
                    ChangePage("CancelLeasePage");
                    break;
            }
        }

        private void ExtendLease(int days, int responseID, string optionText)
        {
            var player = GetPC();
            var data = BaseService.GetPlayerTempData(player);
            var pcApartment = DataService.PCBase.GetByID(data.PCBaseID);
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var style = DataService.BuildingStyle.GetByID(Convert.ToInt32(pcApartment.BuildingStyleID));
            var dailyUpkeep = style.DailyUpkeep + (int)(style.DailyUpkeep * (dbPlayer.LeaseRate * 0.01f));

            if (data.ExtensionDays != days)
            {
                data.IsConfirming = false;
                SetResponseText("DetailsPage", 1, "Extend Lease (+1 day)");
                SetResponseText("DetailsPage", 2, "Extend Lease (+7 days)");
            }
            data.ExtensionDays = days;

            if (player.Gold < dailyUpkeep * days)
            {
                player.SendMessage("You don't have enough credits to extend your lease.");
                data.IsConfirming = false;
                SetResponseText("DetailsPage", responseID, optionText);
                return;
            }

            if (data.IsConfirming)
            {
                TakeGoldFromCreature(dailyUpkeep * days, GetPC(), true);
                data.IsConfirming = false;
                SetResponseText("DetailsPage", responseID, optionText);
                pcApartment.DateRentDue = pcApartment.DateRentDue.AddDays(days);
                DataService.SubmitDataChange(pcApartment, DatabaseActionType.Update);
                LoadDetailsPage();
            }
            else
            {
                data.IsConfirming = true;
                SetResponseText("DetailsPage", responseID, "CONFIRM " + optionText.ToUpper());
            }

        }

        private void CancelLeaseResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm cancel lease
                    CancelLease();
                    break;
            }

        }

        private void CancelLease()
        {
            var data = BaseService.GetPlayerTempData(GetPC());

            if (data.IsConfirming)
            {
                data.IsConfirming = false;

                var pcBase = DataService.PCBase.GetByID(data.PCBaseID);
                BaseService.ClearPCBaseByID(data.PCBaseID);
                MessageHub.Instance.Publish(new OnBaseLeaseCancelled(pcBase));

                GetPC().FloatingText("Your lease has been canceled. Any property left behind has been delivered to the planetary government. Speak with them to retrieve it.");
                LoadMainPage();
                SetResponseText("CancelLeasePage", 1, "Confirm Cancel Lease");
                ClearNavigationStack();
                ChangePage("MainPage", false);
            }
            else
            {
                data.IsConfirming = true;
                SetResponseText("CancelLeasePage", 1, "CONFIRM CANCEL LEASE");
            }
        }

        public override void EndDialog()
        {
            BaseService.ClearPlayerTempData(GetPC());
        }
    }
}
