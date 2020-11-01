using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PlayerHouseDialog : DialogBase
    {
        private class Model
        {
            public PlayerHouseType SelectedHouseType { get; set; }
            public bool IsConfirmingPurchase { get; set; }
            public bool IsConfirmingSell { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string PurchaseHouseLayoutDetailPageId = "PURCHASE_HOUSE_LAYOUT_DETAIL_PAGE";
        private const string SellHousePageId = "SELL_HOUSE_PAGE";


        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(PurchaseHouseLayoutDetailPageId, PurchaseHouseLayoutDetailPageInit)
                .AddPage(SellHousePageId, SellHousePageInit)
                .AddBackAction((previous, next) =>
                {
                    var model = GetDataModel<Model>();
                    model.IsConfirmingPurchase = false;
                    model.IsConfirmingSell = false;
                });

            return builder.Build();
        }

        /// <summary>
        /// Loads the main page header and response options.
        /// </summary>
        /// <param name="page">The dialog page to adjust.</param>
        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var hasHouse = DB.Exists<PlayerHouse>(playerId);

            // Player currently owns a house.
            // Provide options to adjust existing house.
            if (hasHouse)
            {
                var house = DB.Get<PlayerHouse>(playerId);
                var detail = Housing.GetHouseTypeDetail(house.HouseType);

                page.Header = ColorToken.Green("Layout Type: ") + detail.Name + "\n" +
                              ColorToken.Green("Furniture Limit: ") + house.Furnitures.Count + " / " + detail.FurnitureLimit + "\n\n" +
                              "What would you like to do?";

                page.AddResponse("Enter", () =>
                {
                    var instance = Housing.LoadPlayerHouse(playerId);
                    var entrancePosition = Housing.GetEntrancePosition(house.HouseType);
                    var location = Location(instance, entrancePosition, 0.0f);
                    Housing.StoreOriginalLocation(player);

                    AssignCommand(player, () => ActionJumpToLocation(location));
                });

                page.AddResponse("Sell Home", () =>
                {
                    ChangePage(SellHousePageId);
                });
            }
            // Player doesn't own a house.
            // Provide options to buy one.
            else
            {
                PurchaseHouseLayoutListPageInit(page);
            }

        }

        /// <summary>
        /// Handles the "Purchase List Page" header and responses.
        /// </summary>
        /// <param name="page">The page we're building.</param>
        private void PurchaseHouseLayoutListPageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var seedRank = dbPlayer.SeedProgress.Rank;
            var availableLayouts = Housing.GetActiveHouseTypes();
            var model = GetDataModel<Model>();

            page.Header = "You can purchase any of the following layouts. Please note that this list is restricted based on your current SeeD rank. Complete missions to raise your SeeD rank and unlock new layouts.\n\n" +
                ColorToken.Green("Your SeeD Rank: ") + seedRank;

            foreach (var layout in availableLayouts)
            {
                // Player's SeeD rank isn't high enough to purchase this layout.
                if (seedRank < layout.Value.RequiredSeedRank) continue;

                page.AddResponse(layout.Value.Name, () =>
                {
                    model.SelectedHouseType = layout.Key;
                    ChangePage(PurchaseHouseLayoutDetailPageId);
                });
            }

        }

        /// <summary>
        /// Handles the "Purchase Detail Page" header and responses.
        /// </summary>
        /// <param name="page">The page we're building.</param>
        private void PurchaseHouseLayoutDetailPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            var layoutDetail = Housing.GetHouseTypeDetail(model.SelectedHouseType);

            void PurchaseHome()
            {
                var playerId = GetObjectUUID(player);

                // Grant the owner all permissions.
                var permission = new PlayerHousePermission
                {
                    CanAdjustPermissions = true,
                    CanEnter = true,
                    CanPlaceFurniture = true
                };

                var playerHouse = new PlayerHouse
                {
                    HouseType = model.SelectedHouseType,
                    PlayerPermissions = new Dictionary<string, PlayerHousePermission>
                    {
                        { playerId, permission }
                    }
                };

                DB.Set(playerId, playerHouse);

                FloatingTextStringOnCreature("You've purchased a new home!", player, false);
                Log.Write(LogGroup.PlayerHousing, $"Player {GetName(player)} (ID = '{playerId}') bought property type {layoutDetail.Name} for {layoutDetail.Price} gil.");
            }

            page.Header = ColorToken.Green("Layout: ") + layoutDetail.Name + "\n" +
                ColorToken.Green("Required SeeD Rank: ") + layoutDetail.RequiredSeedRank + "\n" +
                ColorToken.Green("Price: ") + layoutDetail.Price + " gil\n" +
                ColorToken.Green("Furniture Limit: ") + layoutDetail.FurnitureLimit + " items";

            page.AddResponse("Preview", () =>
            {
                var originalArea = Cache.GetAreaByResref(layoutDetail.AreaInstanceResref);
                if (originalArea == OBJECT_INVALID)
                {
                    Log.Write(LogGroup.Error, $"{GetName(player)} attempted to preview a house layout with resref {layoutDetail.AreaInstanceResref} but the area could not be found. Ensure an area has been created in the module.");
                    return;
                }

                var copy = Housing.CreateInstance(originalArea);
                var position = Housing.GetEntrancePosition(model.SelectedHouseType);
                var location = Location(copy, position, 0.0f);
                SetName(copy, $"[PREVIEW] {GetName(copy)}");

                Housing.StoreOriginalLocation(player);
                AssignCommand(player, () => ActionJumpToLocation(location));
            });

            // Purchase/confirm purchase options only display if player has enough gold.
            if (GetGold(player) >= layoutDetail.Price)
            {
                // Is confirming purchase
                if (model.IsConfirmingPurchase)
                {
                    page.AddResponse($"CONFIRM PURCHASE ({layoutDetail.Price} gil)", () =>
                    {
                        model.IsConfirmingPurchase = false;

                        // Need to check gold again, in case they dropped money to the ground while the conversation was open.
                        if (GetGold(player) < layoutDetail.Price)
                        {
                            FloatingTextStringOnCreature("You do not have enough gil to purchase this home.", player, false);
                            return;
                        }

                        PurchaseHome();

                        EndConversation();
                    });
                }
                else
                {
                    page.AddResponse($"Purchase ({layoutDetail.Price} gil)", () =>
                    {
                        model.IsConfirmingPurchase = true;
                    });
                }
            }

        }

        /// <summary>
        /// Handles the "Sell House Page" header and responses.
        /// </summary>
        /// <param name="page">The page we're building.</param>
        private void SellHousePageInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbHouse = DB.Get<PlayerHouse>(playerId);
            var model = GetDataModel<Model>();

            var houseDetail = Housing.GetHouseTypeDetail(dbHouse.HouseType);
            var gil = houseDetail.Price / 2;
            page.Header = ColorToken.Red("WARNING: ") + "You are about to sell your property. All items contained inside will be permanently lost!\n\n" +
                          "It is highly recommended you pick up all items and furniture inside before selling the property.\n\n" +
                ColorToken.Green($"You will receive {gil} gil for the sale of this property. Are you sure you wish to sell it?");

            if (model.IsConfirmingSell)
            {
                page.AddResponse(ColorToken.Red($"CONFIRM SELL PROPERTY"), () =>
                {
                    DB.Delete<PlayerHouse>(playerId);

                    GiveGoldToCreature(player, gil);
                    FloatingTextStringOnCreature($"Property sold successfully for {gil} gil.", player, false);
                    EndConversation();

                    Log.Write(LogGroup.PlayerHousing, $"Player {GetName(player)} (ID = '{playerId}') sold their property for {gil} gil.");
                });
            }
            else
            {
                page.AddResponse(ColorToken.Red("Sell Property"), () =>
                {
                    model.IsConfirmingSell = true;
                });
            }

        }
    }
}
