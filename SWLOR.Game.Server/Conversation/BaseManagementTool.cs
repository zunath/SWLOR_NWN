using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class BaseManagementTool: ConversationBase
    {
        private readonly IBaseService _base;
        private readonly IColorTokenService _color;
        private readonly IDataContext _db;
        private readonly IAreaService _area;

        public BaseManagementTool(
            INWScript script, 
            IDialogService dialog,
            IBaseService @base,
            IColorTokenService color,
            IDataContext db,
            IAreaService area) 
            : base(script, dialog)
        {
            _base = @base;
            _color = color;
            _db = db;
            _area = area;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage();
            DialogPage purchaseTerritoryPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PurchaseTerritoryPage", purchaseTerritoryPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }

        private void LoadMainPage()
        {
            ClearPageResponses("MainPage");
            var data = _base.GetPlayerTempData(GetPC());
            int cellX = (int)(_.GetPositionFromLocation(data.TargetLocation).m_X / 10.0f);
            int cellY = (int)(_.GetPositionFromLocation(data.TargetLocation).m_Y / 10.0f);

            Area dbArea = _db.Areas.Single(x => x.Resref == data.TargetArea.Resref);
            bool hasUnclaimed = false;
            string header = _color.Green("Base Management Menu\n\n");
            header += _color.Green("Area: ") + data.TargetArea.Name + " (" + cellX + ", " + cellY + ")\n\n";

            if (dbArea.NortheastOwnerPlayer != null)
            {
                header += _color.Green("Northeast Owner: ") + dbArea.NortheastOwnerPlayer.CharacterName + "\n";
            }
            else
            {
                header += _color.Green("Northeast Owner: ") + "Unclaimed\n";
                hasUnclaimed = true;
            }

            if (dbArea.NorthwestOwnerPlayer != null)
            {
                header += _color.Green("Northwest Owner: ") + dbArea.NorthwestOwnerPlayer.CharacterName + "\n";
            }
            else
            {
                header += _color.Green("Northwest Owner: ") + "Unclaimed\n";
                hasUnclaimed = true;
            }

            if (dbArea.SoutheastOwnerPlayer != null)
            {
                header += _color.Green("Southeast Owner: ") + dbArea.SoutheastOwnerPlayer.CharacterName + "\n";
            }
            else
            {
                header += _color.Green("Southeast Owner: ") + "Unclaimed\n";
                hasUnclaimed = true;
            }

            if (dbArea.SouthwestOwnerPlayer != null)
            {
                header += _color.Green("Southwest Owner: ") + dbArea.SouthwestOwnerPlayer.CharacterName + "\n";
            }
            else
            {
                header += _color.Green("Southwest Owner: ") + "Unclaimed\n";
                hasUnclaimed = true;
            }

            SetPageHeader("MainPage", header);

            string playerID = GetPC().GlobalID;
            bool showManage =
                playerID == dbArea.NortheastOwner ||
                playerID == dbArea.NorthwestOwner ||
                playerID == dbArea.SoutheastOwner ||
                playerID == dbArea.SouthwestOwner;

            AddResponseToPage("MainPage", "Manage My Territory", showManage);
            AddResponseToPage("MainPage", "Purchase Territory", hasUnclaimed);
        }
        
        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainResponses(responseID);
                    break;
                case "PurchaseTerritoryPage":
                    PurchaseTerritoryResponses(responseID);
                    break;
            }
        }

        private void MainResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Manage my territory
                    SwitchConversation("ManageTerritory");
                    break;
                case 2: // Purchase territory
                    SetPageHeader("PurchaseTerritoryPage", BuildPurchaseTerritoryHeader());
                    LoadPurchaseTerritoryResponses();
                    ChangePage("PurchaseTerritoryPage");
                    break;
            }
        }

        private string BuildPurchaseTerritoryHeader()
        {
            var data = _base.GetPlayerTempData(GetPC());
            Area dbArea = _db.Areas.Single(x => x.Resref == data.TargetArea.Resref);
            string header = _color.Green("Purchase Territory Menu\n\n");
            header += "Territory in this sector costs an initial price of " + dbArea.PurchasePrice + " credits.\n\n";
            header += "You will also be billed " + dbArea.WeeklyUpkeep + " credits per week (real world time). Your initial payment covers the cost of the first week.\n\n";
            header += "Purchasing territory gives you the ability to place a control tower, drill for raw materials, construct buildings, build starships, and much more.\n\n";
            header += "You will have a chance to review your purchase before confirming.";

            return header;
        }

        private void LoadPurchaseTerritoryResponses()
        {
            ClearPageResponses("PurchaseTerritoryPage");
            var data = _base.GetPlayerTempData(GetPC());
            Area dbArea = _db.Areas.Single(x => x.Resref == data.TargetArea.Resref);

            AddResponseToPage("PurchaseTerritoryPage", "Purchase Northeast Sector", string.IsNullOrWhiteSpace(dbArea.NortheastOwner));
            AddResponseToPage("PurchaseTerritoryPage", "Purchase Northwest Sector", string.IsNullOrWhiteSpace(dbArea.NorthwestOwner));
            AddResponseToPage("PurchaseTerritoryPage", "Purchase Southeast Sector", string.IsNullOrWhiteSpace(dbArea.SoutheastOwner));
            AddResponseToPage("PurchaseTerritoryPage", "Purchase Southwest Sector", string.IsNullOrWhiteSpace(dbArea.SouthwestOwner));
            AddResponseToPage("PurchaseTerritoryPage", "Back");
        }

        private void PurchaseTerritoryResponses(int responseID)
        {
            switch (responseID)
            {
                case 1: // Northeast sector
                    DoBuy(AreaSector.Northeast, responseID);
                    break;
                case 2: // Northwest sector
                    DoBuy(AreaSector.Northwest, responseID);
                    break;
                case 3: // Southeast sector
                    DoBuy(AreaSector.Southeast, responseID);
                    break;
                case 4: // Southwest sector
                    DoBuy(AreaSector.Southwest, responseID);
                    break;
                case 5: // Back
                    LoadMainPage();
                    ChangePage("MainPage");
                    break;
            }
        }

        private void DoBuy(string sector, int responseID)
        {
            var data = _base.GetPlayerTempData(GetPC());
            
            if (data.IsConfirmingPurchase && data.ConfirmingPurchaseResponseID == responseID)
            {
                _area.PurchaseArea(GetPC(), data.TargetArea, sector);
                data.IsConfirmingPurchase = false;
                RefreshPurchaseResponses();
                LoadMainPage();
                ChangePage("MainPage");
            }
            else if (data.IsConfirmingPurchase && data.ConfirmingPurchaseResponseID != responseID)
            {
                data.ConfirmingPurchaseResponseID = responseID;
                RefreshPurchaseResponses();
            }
            else
            {
                data.IsConfirmingPurchase = true;
                data.ConfirmingPurchaseResponseID = responseID;
                RefreshPurchaseResponses();
            }

        }

        private void RefreshPurchaseResponses()
        {
            var data = _base.GetPlayerTempData(GetPC());

            SetResponseText("PurchaseTerritoryPage", 1, 
                data.ConfirmingPurchaseResponseID == 1 ? 
                    "CONFIRM PURCHASE NORTHEAST SECTOR" : 
                    "Purchase Northeast Sector");
            SetResponseText("PurchaseTerritoryPage", 2,
                data.ConfirmingPurchaseResponseID == 2 ?
                    "CONFIRM PURCHASE NORTHWEST SECTOR" :
                    "Purchase Northwest Sector");
            SetResponseText("PurchaseTerritoryPage", 3,
                data.ConfirmingPurchaseResponseID == 3 ?
                    "CONFIRM PURCHASE SOUTHEAST SECTOR" :
                    "Purchase Southeast Sector");
            SetResponseText("PurchaseTerritoryPage", 4,
                data.ConfirmingPurchaseResponseID == 4 ?
                    "CONFIRM PURCHASE SOUTHWEST SECTOR" :
                    "Purchase Southwest Sector");
        }
        
        public override void EndDialog()
        {
            _base.ClearPlayerTempData(GetPC());
        }
    }
}
