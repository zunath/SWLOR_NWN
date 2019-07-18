using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN._;

namespace SWLOR.Game.Server.Conversation
{
    public class ManageLease: ConversationBase
    {

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                ColorTokenService.Green("Manage Territory Menu") + "\n\nPlease select a territory.");

            DialogPage baseDetailsPage = new DialogPage(string.Empty,
                "Extend Lease (+1 day)",
                "Extend Lease (+7 days)",
                "Cancel Lease");

            DialogPage cancelLeasePage = new DialogPage(
                "Cancelling your lease will remove your right to build on this land. All structures you've placed will be sent to the impound. You'll need to go talk to a planetary representative to recover your impounded structures.\n\n" +
                "Any remaining time on your lease will be forfeit. You will not receive a refund.\n\n" +
                "Are you sure you want to cancel your lease?",
                "Confirm Cancel Lease");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("BaseDetailsPage", baseDetailsPage);
            dialog.AddPage("CancelLeasePage", cancelLeasePage);
            return dialog;
        }

        public override void Initialize()
        {
            BuildMainPage();
        }

        private void BuildMainPage()
        {
            Guid playerID = GetPC().GlobalID;
            
            // Look for any bases for which the player has permissions to manage leases or cancel leases.
            // Owners are included in this since they automatically get all permissions for their own bases.
            // Apartments are excluded from this list as they are canceled from terminals outside individual apartment buildings.
            var bases = DataService.PCBase.GetAll()
                .Where(x =>
                {
                    var pcBasePermissions = DataService.PCBasePermission.GetAllPermissionsByPCBaseID(x.ID);
                    return x.Sector != "AP" &&
                           pcBasePermissions
                               .Any(p => p.PlayerID == playerID && (p.CanExtendLease || p.CanCancelLease));
                })
                .ToList();
            
            ClearPageResponses("MainPage");
            foreach (var @base in bases)
            {
                Area dbArea = DataService.Area.GetByResref(@base.AreaResref);
                string status = @base.PlayerID == playerID ? " [OWNER]" : " [GUEST]";

                AddResponseToPage("MainPage", dbArea.Name + " (" + @base.Sector + ")" + status, true, @base.ID);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "BaseDetailsPage":
                    BaseDetailsResponses(responseID);
                    break;
                case "CancelLeasePage":
                    CancelLeaseResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            switch (beforeMovePage)
            {
                case "BaseDetailsPage":
                    data.IsConfirming = false;
                    SetResponseText("BaseDetailsPage", 1, "Extend Lease (+1 day)");
                    SetResponseText("BaseDetailsPage", 2, "Extend Lease (+7 days)");
                    break;
                case "CancelLeasePage":
                    data.IsConfirming = false;
                    SetResponseText("CancelLeasePage", 1, "Confirm Cancel Lease");
                    break;
            }
            
        }

        private void MainResponses(int responseID)
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            DialogResponse response = GetResponseByID("MainPage", responseID);
            Guid pcBaseID = (Guid)response.CustomData;
            data.PCBaseID = pcBaseID;
            LoadBaseDetailsPage();
            ChangePage("BaseDetailsPage");
        }

        private void LoadBaseDetailsPage()
        {
            var data = BaseService.GetPlayerTempData(GetPC());
            PCBase pcBase = DataService.PCBase.GetByID(data.PCBaseID);
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            var owner = DataService.Player.GetByID(pcBase.PlayerID);
            bool canExtendLease = BasePermissionService.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanExtendLease);
            bool canCancelLease = BasePermissionService.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanCancelLease);

            int dailyUpkeep = dbArea.DailyUpkeep + (int)(dbArea.DailyUpkeep * (owner.LeaseRate * 0.01f));

            string header = ColorTokenService.Green("Location: ") + dbArea.Name + " (" + pcBase.Sector + ")\n\n";
            header += ColorTokenService.Green("Owned By: ") + owner.CharacterName + "\n";
            header += ColorTokenService.Green("Purchased: ") + pcBase.DateInitialPurchase + "\n";
            header += ColorTokenService.Green("Rent Due: ") + pcBase.DateRentDue + "\n";
            header += ColorTokenService.Green("Daily Upkeep: ") + dailyUpkeep + " credits\n\n";
            header += "Daily upkeep may be paid up to 30 days in advance.\n";

            // Starships have slightly different setups.  They only pay rent when in a public starport and
            // the cost is set by the starport.
            if (pcBase.PCBaseTypeID == (int)Enumeration.PCBaseType.Starship)
            {
                canCancelLease = false;

                if (SpaceService.IsLocationPublicStarport(pcBase.ShipLocation))
                {
                    var shipLocationGuid = new Guid(pcBase.ShipLocation);
                    SpaceStarport starport = DataService.SpaceStarport.GetByID(shipLocationGuid);
                    header = ColorTokenService.Green("Location: ") + starport.Name + " (" + starport.Planet + ")\n";
                    header += ColorTokenService.Green("Rent Due: ") + pcBase.DateRentDue + "\n";
                    header += ColorTokenService.Green("Daily Upkeep: ") + starport.Cost + " credits\n\n";
                }
                else
                {
                    header = "This ship has no lease currently.  You only need to pay when in a starport.";
                    canExtendLease = false;
                }
            }

            SetPageHeader("BaseDetailsPage", header);

            const int MaxAdvancePay = 30;
            DateTime newRentDate = pcBase.DateRentDue.AddDays(1);
            TimeSpan ts = newRentDate - DateTime.UtcNow;
            bool canPayRent = ts.TotalDays < MaxAdvancePay;
            SetResponseVisible("BaseDetailsPage", 1, canPayRent && canExtendLease);

            newRentDate = pcBase.DateRentDue.AddDays(7);
            ts = newRentDate - DateTime.UtcNow;
            canPayRent = ts.TotalDays < MaxAdvancePay;

            SetResponseVisible("BaseDetailsPage", 2, canPayRent && canExtendLease);
            SetResponseVisible("BaseDetailsPage", 3, canCancelLease);
        }

        private void BaseDetailsResponses(int responseID)
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
            var data = BaseService.GetPlayerTempData(GetPC());
            PCBase pcBase = DataService.PCBase.GetByID(data.PCBaseID);
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            bool canExtendLease = BasePermissionService.HasBasePermission(GetPC(), pcBase.ID, BasePermission.CanExtendLease);
            var owner = DataService.Player.GetByID(pcBase.PlayerID);

            if (!canExtendLease)
            {
                GetPC().FloatingText("You don't have permission to extend leases on this base.");
                return;
            }

            int dailyUpkeep = dbArea.DailyUpkeep + (int)(dbArea.DailyUpkeep * (owner.LeaseRate * 0.01f));

            // Starship override.
            if (pcBase.PCBaseTypeID == (int)Enumeration.PCBaseType.Starship)
            {
                Guid shipLocationGuid = new Guid(pcBase.ShipLocation);
                SpaceStarport starport = DataService.SpaceStarport.GetByID(shipLocationGuid);
                dailyUpkeep = starport.Cost + (int)(starport.Cost * (owner.LeaseRate * 0.01f));
            }

            if (GetPC().Gold < dailyUpkeep * days)
            {
                GetPC().SendMessage("You don't have enough credits to extend your lease.");
                data.IsConfirming = false;
                SetResponseText("BaseDetailsPage", responseID, optionText);
                return;
            }
            
            if (data.IsConfirming)
            {
                _.TakeGoldFromCreature(dailyUpkeep * days, GetPC(), TRUE);
                data.IsConfirming = false;
                SetResponseText("BaseDetailsPage", responseID, optionText);
                pcBase.DateRentDue = pcBase.DateRentDue.AddDays(days);
                DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                LoadBaseDetailsPage();
            }
            else
            {
                data.IsConfirming = true;
                SetResponseText("BaseDetailsPage", responseID, "CONFIRM " + optionText.ToUpper());
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
            bool canCancelLease = BasePermissionService.HasBasePermission(GetPC(), data.PCBaseID, BasePermission.CanCancelLease);

            if (!canCancelLease)
            {
                GetPC().FloatingText("You don't have permission to cancel this base's lease.");
                return;
            }

            if (data.IsConfirming)
            {
                data.IsConfirming = false;
                PCBase pcBase = DataService.PCBase.GetByID(data.PCBaseID);
                BaseService.ClearPCBaseByID(data.PCBaseID);
                MessageHub.Instance.Publish(new OnBaseLeaseCancelled(pcBase));
                GetPC().FloatingText("Your lease has been canceled. Any property left behind has been delivered to the planetary government. Speak with them to retrieve it.");
                
                BuildMainPage();
                SetResponseText("CancelLeasePage", 1, "Confirm Cancel Lease");
                EndConversation();
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
