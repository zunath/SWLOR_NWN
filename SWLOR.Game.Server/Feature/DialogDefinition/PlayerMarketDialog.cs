using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PlayerMarketDialog: DialogBase
    {
        private class Model
        {
            public bool IsConfirmingExtendMaximum { get; set; }
            public bool IsConfirmingExtend7Days { get; set; }
            public bool IsConfirmingExtend1Day { get; set; }
            public bool IsConfirmingRemoveItem { get; set; }

            public string SelectedItemId { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ViewShopsPageId = "VIEW_SHOPS_PAGE";
        private const string EditMyShopPageId = "EDIT_MY_SHOPS_PAGE";
        private const string ChangeStoreNamePageId = "CHANGE_STORE_NAME_PAGE";
        private const string EditItemListPageId = "EDIT_ITEM_LIST_PAGE";
        private const string ExtendLeasePageId = "EXTEND_LEASE_PAGE";
        private const string ListingPageId = "LISTING_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainInit)
                .AddPage(ViewShopsPageId, ViewShopsInit)
                .AddPage(EditMyShopPageId, EditMyShopInit)
                .AddPage(ChangeStoreNamePageId, ChangeStoreNameInit)
                .AddPage(EditItemListPageId, EditItemListInit)
                .AddPage(ExtendLeasePageId, ExtendLeaseInit)
                .AddPage(ListingPageId, ListingInit)
                .AddBackAction((oldPage, newPage) =>
                {
                    ClearTemporaryVariables();
                })
                .AddEndAction(ClearTemporaryVariables);

            return builder.Build();
        }

        private void ClearTemporaryVariables()
        {
            var player = GetPC();
            var model = GetDataModel<Model>();

            model.IsConfirmingExtendMaximum = false;
            model.IsConfirmingExtend7Days = false;
            model.IsConfirmingExtend1Day = false;
            model.IsConfirmingRemoveItem = false;
            DeleteLocalString(player, "NEW_STORE_NAME");
            DeleteLocalBool(player, "IS_SETTING_STORE_NAME");
        }

        private void MainInit(DialogPage page)
        {
            page.Header = "Please select an option.";

            page.AddResponse("View Shops", () =>
            {
                ChangePage(ViewShopsPageId);
            });

            page.AddResponse("Edit My Shop", () =>
            {
                ChangePage(EditMyShopPageId);
            });

        }

        private void ViewShopsInit(DialogPage page)
        {
            page.Header = "Please select a shop.";

            var stores = PlayerMarket.GetAllActiveStores();

            foreach (var (key, name) in stores)
            {
                page.AddResponse(name, () =>
                {
                    PlayerMarket.OpenPlayerStore(GetPC(), key);
                    EndConversation();
                });
            }
        }

        private string GetLeaseStatus()
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId) ?? new PlayerStore();
            var leaseStatus = DateTime.UtcNow > dbPlayerStore.DateLeaseExpires
                ? ColorToken.Red("EXPIRED")
                : dbPlayerStore.DateLeaseExpires.ToString("MM/dd/yyyy hh:mm:ss");

            return leaseStatus;
        }

        private void EditMyShopInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId) ?? new PlayerStore
            {
                StoreName = $"{GetName(player)}'s Store"
            };

            page.Header = ColorToken.Green("Store Name: ") + dbPlayerStore.StoreName + "\n" +
                ColorToken.Green("Lease Expires: ") + GetLeaseStatus() + "\n" +
                ColorToken.Green("Tax Rate: ") + (int)(dbPlayerStore.TaxRate * 100) + "%\n" +
                ColorToken.Green("Status: ") + (dbPlayerStore.IsOpen ? "OPEN" : "CLOSED") + "\n\n" +
                "Please select an option.";

            // The Till will be filled only if the player sold an item.
            if (dbPlayerStore.Till > 0)
            {
                page.AddResponse(ColorToken.Green($"Retrieve {dbPlayerStore.Till} Credits"), () =>
                {
                    GiveGoldToCreature(player, dbPlayerStore.Till);
                    dbPlayerStore.Till = 0;
                    DB.Set(playerId, dbPlayerStore);
                    PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);
                });
            }

            page.AddResponse("Change Store Name", () =>
            {
                ChangePage(ChangeStoreNamePageId);
            });

            page.AddResponse("Edit Item List", () =>
            {
                if (PlayerMarket.IsStoreBeingAccessed(playerId))
                {
                    FloatingTextStringOnCreature("Your store is currently being accessed by another player. Close your store and wait until they finish to try this action again.", player, false);
                    return;
                }

                ChangePage(EditItemListPageId);
            });

            page.AddResponse(dbPlayerStore.IsOpen ? "Close Store" : "Open Store", () =>
            {
                dbPlayerStore.IsOpen = !dbPlayerStore.IsOpen;
                DB.Set(playerId, dbPlayerStore);
                PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);
            });

            page.AddResponse("Extend Lease", () =>
            {
                ChangePage(ExtendLeasePageId);
            });

            DB.Set(playerId, dbPlayerStore);
            PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);
        }

        [NWNEventHandler("on_nwnx_chat")]
        public static void ListenForStoreName()
        {
            var player = Chat.GetSender();
            if (!GetLocalBool(player, "IS_SETTING_STORE_NAME")) return;

            var message = Chat.GetMessage();

            if (message.Length > 30)
                message = message.Substring(0, 30);

            SetLocalString(player, "NEW_STORE_NAME", message);
            Chat.SkipMessage();

            FloatingTextStringOnCreature("Press 'Refresh' in the chat window to see the changes.", player, false);
        }

        private void ChangeStoreNameInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId) ?? new PlayerStore();
            var newStoreName = GetLocalString(player, "NEW_STORE_NAME");

            if (string.IsNullOrWhiteSpace(newStoreName))
                newStoreName = dbPlayerStore.StoreName;

            page.Header = ColorToken.Green("Store Name: ") + dbPlayerStore.StoreName + "\n" +
                ColorToken.Green("New Name: ") + newStoreName + "\n\n" +
                "Set your store's name by entering it in the chat bar. Press 'Refresh' to see changes.";

            page.AddResponse(ColorToken.Green("Refresh"), () => { });
            page.AddResponse("Set Name", () =>
            {
                dbPlayerStore.StoreName = newStoreName;
                DB.Set(playerId, dbPlayerStore);
                PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);
            });

            SetLocalBool(player, "IS_SETTING_STORE_NAME", true);
        }

        private void EditItemListInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId);
            var model = GetDataModel<Model>();
            var itemLimit = 20;

            page.Header = ColorToken.Green("Listing Limit: ") + dbPlayerStore.ItemsForSale.Count + " / " + itemLimit + "\n" +
                          "Please select the 'List Items' option to add an item to your store. Otherwise select any other option to edit that listing.";

            page.AddResponse($"{ColorToken.Green("List Items")}", () =>
            {
                if (dbPlayerStore.ItemsForSale.Count >= itemLimit)
                {
                    FloatingTextStringOnCreature("You have reached your listing limit.", player, false);
                    return;
                }

                var terminal = OBJECT_SELF;

                SetEventScript(terminal, EventScript.Placeable_OnOpen, "mkt_term_open");
                SetEventScript(terminal, EventScript.Placeable_OnClosed, "mkt_term_closed");
                SetEventScript(terminal, EventScript.Placeable_OnInventoryDisturbed, "mkt_term_dist");
                SetEventScript(terminal, EventScript.Placeable_OnUsed, string.Empty);
                
                AssignCommand(player, () => ActionInteractObject(terminal));
            });

            foreach (var (itemId, item) in dbPlayerStore.ItemsForSale)
            {
                var responseName = $"{item.StackSize}x {item.Name} [{item.Price} credits]";

                page.AddResponse(responseName, () =>
                {
                    // Someone bought the item already. Don't let them progress.
                    if (!dbPlayerStore.ItemsForSale.ContainsKey(itemId))
                    {
                        FloatingTextStringOnCreature("This item has already been sold. Please select another.", player, false);
                        return;
                    }

                    model.SelectedItemId = itemId;
                    ChangePage(ListingPageId);
                });
            }
        }

        private void ListingInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId);
            var model = GetDataModel<Model>();
            var item = dbPlayerStore.ItemsForSale[model.SelectedItemId];

            void AdjustPrice(int amount)
            {
                item.Price += amount;

                if (item.Price <= 0)
                    item.Price = 1;
                else if (item.Price > 999999)
                    item.Price = 999999;

                DB.Set(playerId, dbPlayerStore);
                PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);
            }

            page.Header = ColorToken.Green("Item: ") + item.StackSize + "x " + item.Name + "\n" +
                          ColorToken.Green("Price: ") + item.Price + "\n\n" +
                          "Please select an option.";

            if (model.IsConfirmingRemoveItem)
            {
                page.AddResponse(ColorToken.Red("CONFIRM REMOVE ITEM"), () =>
                {
                    var inWorldItem = Core.NWNX.Object.Deserialize(item.Data);
                    Core.NWNX.Object.AcquireItem(player, inWorldItem);
                    dbPlayerStore.ItemsForSale.Remove(model.SelectedItemId);

                    DB.Set(playerId, dbPlayerStore);
                    PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);

                    ChangePage(EditItemListPageId, false);
                    model.IsConfirmingRemoveItem = false;
                });
            }
            else
            {
                page.AddResponse(ColorToken.Red("Remove Item"), () =>
                {
                    model.IsConfirmingRemoveItem = true;
                });
            }

            page.AddResponse("Increase by 10,000 credits", () =>
            {
                AdjustPrice(10000);
            });
            page.AddResponse("Increase by 1,000 credits", () =>
            {
                AdjustPrice(1000);
            });
            page.AddResponse("Increase by 100 credits", () =>
            {
                AdjustPrice(100);
            });
            page.AddResponse("Increase by 10 credits", () =>
            {
                AdjustPrice(10);
            });
            page.AddResponse("Increase by 1 credit", () =>
            {
                AdjustPrice(1);
            });

            page.AddResponse("Decrease by 10,000 credits", () =>
            {
                AdjustPrice(-10000);
            });
            page.AddResponse("Decrease by 1,000 credits", () =>
            {
                AdjustPrice(-1000);
            });
            page.AddResponse("Decrease by 100 credits", () =>
            {
                AdjustPrice(-100);
            });
            page.AddResponse("Decrease by 10 credits", () =>
            {
                AdjustPrice(-10);
            });
            page.AddResponse("Decrease by 1 credit", () =>
            {
                AdjustPrice(-1);
            });
        }

        private void ExtendLeaseInit(DialogPage page)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId);
            var model = GetDataModel<Model>();
            const int PricePerDay = 200;

            void ExtendLease(int days)
            {
                model.IsConfirmingExtendMaximum = false;
                model.IsConfirmingExtend7Days = false;
                model.IsConfirmingExtend1Day = false;

                var now = DateTime.UtcNow;

                // If expired, we start from the current time.
                if (now > dbPlayerStore.DateLeaseExpires)
                {
                    dbPlayerStore.DateLeaseExpires = now;
                }

                // Make sure the lease doesn't go over 30 days from now.
                if (dbPlayerStore.DateLeaseExpires.AddDays(days) > now.AddDays(30))
                {
                    var daysOver = dbPlayerStore.DateLeaseExpires.AddDays(days) - now.AddDays(30);
                    days -= daysOver.Days;
                }

                // Unable to add any more days to the lease. Must be at the maximum.
                if (days <= 0)
                {
                    FloatingTextStringOnCreature("Your lease cannot be extended past 30 days at a time.", player, false);
                    return;
                }

                // Player needs more money!
                if (GetGold(player) < days * PricePerDay)
                {
                    FloatingTextStringOnCreature("You do not have enough money to extend this lease.", player, false);
                    return;
                }

                TakeGoldFromCreature(days * PricePerDay, player, true);

                dbPlayerStore.DateLeaseExpires = dbPlayerStore.DateLeaseExpires.AddDays(days);
                DB.Set(playerId, dbPlayerStore);
                PlayerMarket.UpdateCacheEntry(playerId, dbPlayerStore);
            }

            page.Header = ColorToken.Green("Lease Expires: ") + GetLeaseStatus() + "\n\n" +
                $"Your store lease can be extended up to 30 days at a rate of {PricePerDay} credits per day.";

            if (model.IsConfirmingExtendMaximum)
            {
                page.AddResponse("CONFIRM EXTEND TO MAXIMUM", () =>
                {
                    ExtendLease(30);
                });
            }
            else
            {
                page.AddResponse("Extend to maximum", () =>
                {
                    model.IsConfirmingExtendMaximum = true;
                    model.IsConfirmingExtend7Days = false;
                    model.IsConfirmingExtend1Day = false;
                });
            }

            if (model.IsConfirmingExtend7Days)
            {
                page.AddResponse($"CONFIRM EXTEND BY 7 DAYS ({PricePerDay * 7} CREDITS)", () =>
                {
                    ExtendLease(7);
                });
            }
            else
            {
                page.AddResponse($"Extend by 7 days ({PricePerDay * 7} credits)", () =>
                {
                    model.IsConfirmingExtendMaximum = false;
                    model.IsConfirmingExtend7Days = true;
                    model.IsConfirmingExtend1Day = false;
                });
            }

            if (model.IsConfirmingExtend1Day)
            {
                page.AddResponse($"CONFIRM EXTEND BY 1 DAY ({PricePerDay * 1} CREDITS)", () =>
                {
                    ExtendLease(1);
                });
            }
            else
            {
                page.AddResponse($"Extend by 1 day ({PricePerDay * 1} credits)", () =>
                {
                    model.IsConfirmingExtendMaximum = false;
                    model.IsConfirmingExtend7Days = false;
                    model.IsConfirmingExtend1Day = true;
                });
            }
        }
    }
}
