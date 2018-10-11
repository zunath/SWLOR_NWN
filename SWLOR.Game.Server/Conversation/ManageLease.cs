using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class ManageLease: ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IColorTokenService _color;
        private readonly IDataContext _db;
        private readonly IBasePermissionService _perm;

        public ManageLease(
            INWScript script, 
            IDialogService dialog,
            IBaseService @base,
            IColorTokenService color,
            IDataContext db,
            IBasePermissionService perm) 
            : base(script, dialog)
        {
            _base = @base;
            _color = color;
            _db = db;
            _perm = perm;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                _color.Green("Manage Territory Menu") + "\n\nPlease select a territory.");

            DialogPage baseDetailsPage = new DialogPage(string.Empty,
                "Extend Lease (+1 day)",
                "Extend Lease (+7 days)",
                "Cancel Lease",
                "Back");

            DialogPage cancelLeasePage = new DialogPage(
                "Cancelling your lease will remove your right to build on this land. All structures you've placed will be sent to the impound. You'll need to go talk to a planetary representative to recover your impounded structures.\n\n" +
                "Any remaining time on your lease will be forfeit. You will not receive a refund.\n\n" +
                "Are you sure you want to cancel your lease?",
                "Confirm Cancel Lease",
                "Back");

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
            string playerID = GetPC().GlobalID;
            
            // Look for any bases for which the player has permissions to manage leases or cancel leases.
            // Owners are included in this since they automatically get all permissions for their own bases.
            var bases = _db.PCBases
                .Where(x => x.PCBasePermissions
                    .Any(p => p.PlayerID == playerID && (p.CanExtendLease || p.CanCancelLease)));
            
            ClearPageResponses("MainPage");
            foreach (var @base in bases)
            {
                Area dbArea = _db.Areas.Single(x => x.Resref == @base.AreaResref);
                string status = @base.PlayerID == playerID ? " [OWNER]" : " [GUEST]";

                AddResponseToPage("MainPage", dbArea.Name + " (" + @base.Sector + ")" + status, true, @base.PCBaseID);
            }

            AddResponseToPage("MainPage", "Back", true, -1);
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

        private void MainResponses(int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());
            DialogResponse response = GetResponseByID("MainPage", responseID);
            int pcBaseID = response.CustomData[string.Empty];

            if (pcBaseID <= 0)
            {
                SwitchConversation("BaseManagementTool");
                return;
            }

            data.PCBaseID = pcBaseID;
            LoadBaseDetailsPage();
            ChangePage("BaseDetailsPage");
        }

        private void LoadBaseDetailsPage()
        {
            var data = _base.GetPlayerTempData(GetPC());
            PCBase pcBase = _db.PCBases.Single(x => x.PCBaseID == data.PCBaseID);
            Area dbArea = _db.Areas.Single(x => x.Resref == pcBase.AreaResref);
            bool canExtendLease = _perm.HasBasePermission(GetPC(), pcBase.PCBaseID, BasePermission.CanExtendLease);
            bool canCancelLease = _perm.HasBasePermission(GetPC(), pcBase.PCBaseID, BasePermission.CanCancelLease);

            string header = _color.Green("Location: ") + dbArea.Name + " (" + pcBase.Sector + ")\n\n";
            header += _color.Green("Owned By: ") + pcBase.PlayerCharacter.CharacterName + "\n";
            header += _color.Green("Purchased: ") + pcBase.DateInitialPurchase + "\n";
            header += _color.Green("Rent Due: ") + pcBase.DateRentDue + "\n";
            header += _color.Green("Daily Upkeep: ") + dbArea.DailyUpkeep + " credits\n\n";
            header += "Daily upkeep may be paid up to 30 days in advance.\n";
           

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
                case 4: // Back
                    var data = _base.GetPlayerTempData(GetPC());
                    data.IsConfirming = false;
                    SetResponseText("BaseDetailsPage", 1, "Extend Lease (+1 day)");
                    SetResponseText("BaseDetailsPage", 2, "Extend Lease (+7 days)");
                    ChangePage("MainPage");
                    break;
            }
        }

        private void ExtendLease(int days, int responseID, string optionText)
        {
            var data = _base.GetPlayerTempData(GetPC());
            PCBase pcBase = _db.PCBases.Single(x => x.PCBaseID == data.PCBaseID);
            Area dbArea = _db.Areas.Single(x => x.Resref == pcBase.AreaResref);
            bool canExtendLease = _perm.HasBasePermission(GetPC(), pcBase.PCBaseID, BasePermission.CanExtendLease);

            if (!canExtendLease)
            {
                GetPC().FloatingText("You don't have permission to extend leases on this base.");
                return;
            }

            if (GetPC().Gold < dbArea.DailyUpkeep * days)
            {
                GetPC().SendMessage("You don't have enough credits to extend your lease.");
                data.IsConfirming = false;
                SetResponseText("BaseDetailsPage", responseID, optionText);
                return;
            }
            
            if (data.IsConfirming)
            {
                _.TakeGoldFromCreature(dbArea.DailyUpkeep * days, GetPC(), TRUE);
                data.IsConfirming = false;
                SetResponseText("BaseDetailsPage", responseID, optionText);
                pcBase.DateRentDue = pcBase.DateRentDue.AddDays(days);
                _db.SaveChanges();
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
                case 2: // Back
                    var data = _base.GetPlayerTempData(GetPC());
                    data.IsConfirming = false;
                    SetResponseText("CancelLeasePage", 1, "Confirm Cancel Lease");
                    ChangePage("BaseDetailsPage");
                    break;
            }
        }

        private void CancelLease()
        {
            var data = _base.GetPlayerTempData(GetPC());
            bool canCancelLease = _perm.HasBasePermission(GetPC(), data.PCBaseID, BasePermission.CanCancelLease);

            if (!canCancelLease)
            {
                GetPC().FloatingText("You don't have permission to cancel this base's lease.");
                return;
            }

            if (data.IsConfirming)
            {
                data.IsConfirming = false;
                _base.ClearPCBaseByID(data.PCBaseID);
                GetPC().FloatingText("Your lease has been canceled. Any property left behind has been delivered to the planetary government. Speak with them to retrieve it.");
                
                BuildMainPage();
                SetResponseText("CancelLeasePage", 1, "Confirm Cancel Lease");
                ChangePage("MainPage");
            }
            else
            {
                data.IsConfirming = true;
                SetResponseText("CancelLeasePage", 1, "CONFIRM CANCEL LEASE");
            }
        }

        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
