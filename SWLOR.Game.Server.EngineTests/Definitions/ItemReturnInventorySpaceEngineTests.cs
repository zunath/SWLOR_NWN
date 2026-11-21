using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.EngineTests.Framework.FullInventoryFixture;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Windows and items that hand a stored item back to the player keep the record, credits or loaded item when the
    /// player's inventory has no room, and complete normally once there is room.
    /// </summary>
    public static class ItemReturnInventorySpaceEngineTests
    {
        [EngineTest("Unloading bait into a full inventory keeps the bait on the rod and every carried item", Category = "ItemClaimSpace")]
        public static async Task FishingBaitUnloadWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var rodDetail = new FishingRodItemDefinition().BuildItems()[Fishing.FishingRodTag];
            var rod = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                rod = CreateItemOnObject("bamboo_rod", player);
                ctx.AssertEqual(Fishing.FishingRodTag, GetTag(rod), "The player carries a fishing rod");
                SetLocalString(rod, Fishing.ActiveBaitVariable, SerializeNewItem(ctx, "little_worm"));
                SetLocalInt(rod, Fishing.RemainingBaitVariable, 7);
                var fillers = Fill(ctx, player);
                var carried = Snapshot(player);

                rodDetail.ApplyAction(player, rod, rod, GetLocation(player), 0);
                AssertCarriedIntact(ctx, player, carried);
                ctx.Assert(!string.IsNullOrEmpty(GetLocalString(rod, Fishing.ActiveBaitVariable)), "The rod keeps its bait");
                ctx.AssertEqual(7, GetLocalInt(rod, Fishing.RemainingBaitVariable), "The rod keeps its bait count");
                ctx.AssertEqual(0, CountCarried(player, "little_worm"), "No bait reaches the full inventory");
                Free(fillers);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var carried = Snapshot(player);
                rodDetail.ApplyAction(player, rod, rod, GetLocation(player), 0);
                AssertCarriedIntact(ctx, player, carried);
                ctx.AssertEqual(string.Empty, GetLocalString(rod, Fishing.ActiveBaitVariable), "Unloading empties the rod");
                ctx.AssertEqual(7, CarriedStackTotal(player, "little_worm"), "The remaining bait returns to the inventory");
            });
        }

        [EngineTest("Removing a market listing without inventory room keeps the listing and every carried item", Category = "ItemClaimSpace")]
        public static async Task MarketDelistWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var listing = new MarketItem
            {
                MarketId = PlayerMarket.GetMarketRegion(MarketRegionType.Global).MarketId, PlayerId = fixture.Id,
                SellerName = "Fixture", Name = "Claim space listing", Resref = LargeItemResref, Quantity = 1, Price = 100
            };
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    listing.Data = SerializeNewItem(ctx, LargeItemResref);
                    DB.Set(listing);
                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    var model = OpenWindow<MarketListingViewModel>(fixture, GuiWindowType.MarketListing, new MarketPayload(MarketRegionType.Global));

                    RemoveListing(ctx, model, listing.Id);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<MarketItem>(listing.Id) != null, "The market keeps the listing");
                    ctx.Assert(ListingIds(model).Contains(listing.Id), "The window still shows the listing");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    var model = (MarketListingViewModel)Gui.GetPlayerWindow(player, GuiWindowType.MarketListing).ViewModel;
                    RemoveListing(ctx, model, listing.Id);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<MarketItem>(listing.Id) == null, "The removed listing leaves the market");
                    ctx.AssertEqual(1, CountCarried(player, LargeItemResref), "The listed item returns to the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
                DB.Delete<MarketItem>(listing.Id);
            }
        }

        [EngineTest("Buying from the market without inventory room keeps the listing, the buyer's credits and every carried item", Category = "ItemClaimSpace")]
        public static async Task MarketBuyWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var seller = new Player(Guid.NewGuid().ToString());
            var listing = new MarketItem
            {
                MarketId = PlayerMarket.GetMarketRegion(MarketRegionType.Global).MarketId, PlayerId = seller.Id,
                SellerName = "Seller", SellerCDKey = "ENGINESL", Name = "Claim space purchase",
                Resref = LargeItemResref, Quantity = 1, Price = 100, IsListed = true
            };
            var gold = 0;
            try
            {
                DB.Set(seller);
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    listing.Data = SerializeNewItem(ctx, LargeItemResref);
                    DB.Set(listing);
                    GiveGoldToCreature(player, 500);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    gold = GetGold(player);
                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    var model = OpenWindow<MarketBuyViewModel>(fixture, GuiWindowType.MarketBuying, new MarketPayload(MarketRegionType.Global));

                    Buy(ctx, model, listing);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<MarketItem>(listing.Id) != null, "The market keeps the listing");
                    ctx.AssertEqual(0, DB.Get<Player>(seller.Id).MarketTill, "The seller is not paid");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(gold, GetGold(player), "The buyer keeps their credits");
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    var model = (MarketBuyViewModel)Gui.GetPlayerWindow(player, GuiWindowType.MarketBuying).ViewModel;
                    Buy(ctx, model, listing);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<MarketItem>(listing.Id) == null, "The bought listing leaves the market");
                    ctx.Assert(DB.Get<Player>(seller.Id).MarketTill > 0, "The seller is paid");
                    ctx.AssertEqual(1, CountCarried(player, LargeItemResref), "The bought item reaches the inventory");
                });
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(gold - listing.Price, GetGold(player), "The buyer pays once");
            }
            finally
            {
                CloseWindows(fixture);
                DB.Delete<MarketItem>(listing.Id);
                DB.Delete<Player>(seller.Id);
            }
        }

        [EngineTest("Completing or cancelling research without inventory room keeps the job and every carried item", Category = "ItemClaimSpace")]
        public static async Task ResearchWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var terminalId = "engine-claim-research/" + fixture.Id;
            var payload = new ResearchPayload(terminalId, OBJECT_INVALID, RecipeType.TwinElectroblade1);
            var now = DateTime.UtcNow;
            var completed = new ResearchJob
            {
                ParentPropertyId = terminalId, PlayerId = fixture.Id, DateStarted = now.AddHours(-2),
                DateCompleted = now.AddHours(-1), SerializedItem = string.Empty, Recipe = RecipeType.TwinElectroblade1
            };
            var running = new ResearchJob
            {
                ParentPropertyId = terminalId, PlayerId = fixture.Id, DateStarted = now,
                DateCompleted = now.AddHours(1), Level = 1, Recipe = RecipeType.TwinElectroblade1
            };
            try
            {
                DB.Set(completed);
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    Click(OpenWindow<object>(fixture, GuiWindowType.Research, payload), "ClickCompleteJob");
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<ResearchJob>(completed.Id) != null, "A completed job stays at the terminal");
                    ctx.AssertEqual(0, CountCarried(player, "blueprint"), "No blueprint reaches the full inventory");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    Click(Gui.GetPlayerWindow(player, GuiWindowType.Research).ViewModel, "ClickCompleteJob");
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<ResearchJob>(completed.Id) == null, "The collected job leaves the terminal");
                    ctx.AssertEqual(1, CountCarried(player, "blueprint"), "The blueprint reaches the inventory");
                });

                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    running.SerializedItem = SerializeNewItem(ctx, "blueprint");
                    DB.Set(running);
                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    var model = OpenWindow<object>(fixture, GuiWindowType.Research, payload);
                    Click(model, "ClickCancelJob");
                    Click(model, "OnModalConfirmClick");
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<ResearchJob>(running.Id) != null, "A cancelled job stays at the terminal");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    var model = Gui.GetPlayerWindow(player, GuiWindowType.Research).ViewModel;
                    Click(model, "ClickCancelJob");
                    Click(model, "OnModalConfirmClick");
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<ResearchJob>(running.Id) == null, "The cancelled job leaves the terminal");
                    ctx.AssertEqual(2, CarriedStackTotal(player, "blueprint"), "The researched blueprint returns to the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
                DB.Delete<ResearchJob>(completed.Id);
                DB.Delete<ResearchJob>(running.Id);
            }
        }

        [EngineTest("Removing a ship module or unregistering a ship without inventory room keeps the ship and every carried item", Category = "ItemClaimSpace")]
        public static async Task ShipWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var withModule = Ship(fixture, "with module");
            var plain = Ship(fixture, "plain");
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    withModule.Ship.SerializedItem = SerializeNewItem(ctx, "sd_striker");
                    plain.Ship.SerializedItem = SerializeNewItem(ctx, "sd_striker");
                    withModule.Ship.Status.HighPowerModules[1] = new ShipStatus.ShipStatusModule
                    {
                        ItemTag = "sm_laser_00", SerializedItem = SerializeNewItem(ctx, "sm_laser_00"), RecastTime = DateTime.MinValue
                    };
                    foreach (var (property, ship) in new[] { withModule, plain })
                    {
                        DB.Set(property);
                        DB.Set(ship);
                    }

                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    var model = OpenWindow<ShipManagementViewModel>(fixture, GuiWindowType.ShipManagement,
                        new ShipManagementPayload(default, GetLocation(player), GetLocation(player)));

                    SelectShip(ctx, model, withModule.Ship.Id);
                    Call(model, "ProcessHighPower", 1);
                    model.OnModalConfirmClick()();
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<PlayerShip>(withModule.Ship.Id).Status.HighPowerModules.ContainsKey(1), "The ship keeps its module");

                    SelectShip(ctx, model, plain.Ship.Id);
                    model.OnClickUnregisterShip()();
                    model.OnModalConfirmClick()();
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<PlayerShip>(plain.Ship.Id) != null, "The ship stays registered");
                    ctx.Assert(!DB.Get<WorldProperty>(plain.Property.Id).IsQueuedForDeletion, "The ship's property is kept");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    var model = (ShipManagementViewModel)Gui.GetPlayerWindow(player, GuiWindowType.ShipManagement).ViewModel;

                    SelectShip(ctx, model, withModule.Ship.Id);
                    Call(model, "ProcessHighPower", 1);
                    model.OnModalConfirmClick()();
                    ctx.Assert(!DB.Get<PlayerShip>(withModule.Ship.Id).Status.HighPowerModules.ContainsKey(1), "The module leaves the ship");
                    ctx.AssertEqual(1, CountCarried(player, "sm_laser_00"), "The module reaches the inventory");

                    SelectShip(ctx, model, plain.Ship.Id);
                    model.OnClickUnregisterShip()();
                    model.OnModalConfirmClick()();
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<PlayerShip>(plain.Ship.Id) == null, "The ship is unregistered");
                    ctx.AssertEqual(1, CountCarried(player, "sd_striker"), "The deed reaches the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
                foreach (var (property, ship) in new[] { withModule, plain })
                {
                    DB.Delete<PlayerShip>(ship.Id);
                    DB.Delete<WorldProperty>(property.Id);
                }
            }
        }

        [EngineTest("Taking an item from property storage without inventory room keeps it stored with every carried item", Category = "ItemClaimSpace")]
        public static async Task PropertyStorageWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var property = new WorldProperty
            {
                Id = "engine-claim-storage/" + fixture.Id, OwnerPlayerId = fixture.Id, CustomName = "Claim space storage",
                PropertyType = PropertyType.Apartment, ItemStorageCount = 10
            };
            var category = new WorldPropertyCategory { ParentPropertyId = property.Id, Name = "Claim space" };
            var permission = Permission(fixture.Id, category.Id, PropertyType.Category);
            const string storedId = "engine-claim-stored-item";
            var previousPropertyId = Property.GetPropertyId(ctx.Arena);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    category.Items[storedId] = new WorldPropertyItem
                    {
                        Name = "Twin Electroblade", Resref = LargeItemResref, Quantity = 1, Data = SerializeNewItem(ctx, LargeItemResref)
                    };
                    DB.Set(property);
                    DB.Set(category);
                    DB.Set(permission);
                    SetLocalString(ctx.Arena, "PROPERTY_ID", property.Id);

                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    var model = OpenWindow<PropertyItemStorageViewModel>(fixture, GuiWindowType.PropertyItemStorage, null);
                    SelectStoredItem(ctx, model, category.Id, storedId);
                    model.OnRetrieveItem()();
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<WorldPropertyCategory>(category.Id).Items.ContainsKey(storedId), "The storage keeps the item");
                    ctx.AssertEqual(Item.InventoryFullMessage, model.Instructions, "The window says the inventory is full");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    var model = (PropertyItemStorageViewModel)Gui.GetPlayerWindow(player, GuiWindowType.PropertyItemStorage).ViewModel;
                    SelectStoredItem(ctx, model, category.Id, storedId);
                    model.OnRetrieveItem()();
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(!DB.Get<WorldPropertyCategory>(category.Id).Items.ContainsKey(storedId), "The item leaves storage");
                    ctx.AssertEqual(1, CountCarried(player, LargeItemResref), "The item reaches the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
                SetLocalString(ctx.Arena, "PROPERTY_ID", previousPropertyId);
                DB.Delete<WorldPropertyPermission>(permission.Id);
                DB.Delete<WorldPropertyCategory>(category.Id);
                DB.Delete<WorldProperty>(property.Id);
            }
        }

        [EngineTest("Retrieving a structure without inventory room keeps it placed with every carried item", Category = "ItemClaimSpace")]
        public static async Task StructureRetrievalWithoutRoom(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var parent = new WorldProperty
            {
                Id = "engine-claim-structures/" + fixture.Id, OwnerPlayerId = fixture.Id, CustomName = "Claim space apartment",
                PropertyType = PropertyType.Apartment
            };
            var structure = new WorldProperty
            {
                ParentPropertyId = parent.Id, OwnerPlayerId = fixture.Id, CustomName = "Claim space obelisk",
                PropertyType = PropertyType.Structure, StructureType = StructureType.ObeliskLarge
            };
            parent.ChildPropertyIds[PropertyChildType.Structure] = new List<string> { structure.Id };
            var permission = Permission(fixture.Id, parent.Id, PropertyType.Apartment);
            var previousPropertyId = Property.GetPropertyId(ctx.Arena);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    structure.SerializedItem = SerializeNewItem(ctx, LargeItemResref);
                    DB.Set(parent);
                    DB.Set(structure);
                    DB.Set(permission);
                    SetLocalString(ctx.Arena, "PROPERTY_ID", parent.Id);

                    var fillers = Fill(ctx, player);
                    var carried = Snapshot(player);
                    var model = OpenWindow<ManageStructuresViewModel>(fixture, GuiWindowType.ManageStructures, null);
                    RetrieveStructure(ctx, model, structure.Id);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<WorldProperty>(structure.Id) != null, "The structure stays placed");
                    ctx.Assert(DB.Get<WorldProperty>(parent.Id).ChildPropertyIds[PropertyChildType.Structure].Contains(structure.Id),
                        "The property still lists the structure");
                    ctx.AssertEqual(Item.InventoryFullShortMessage, model.Instructions, "The window says the inventory is full");
                    Free(fillers);
                });
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    var carried = Snapshot(player);
                    var model = (ManageStructuresViewModel)Gui.GetPlayerWindow(player, GuiWindowType.ManageStructures).ViewModel;
                    RetrieveStructure(ctx, model, structure.Id);
                    AssertCarriedIntact(ctx, player, carried);
                    ctx.Assert(DB.Get<WorldProperty>(structure.Id) == null, "The structure is retrieved");
                    ctx.AssertEqual(1, CountCarried(player, LargeItemResref), "The structure's item reaches the inventory");
                });
            }
            finally
            {
                CloseWindows(fixture);
                SetLocalString(ctx.Arena, "PROPERTY_ID", previousPropertyId);
                DB.Delete<WorldPropertyPermission>(permission.Id);
                DB.Delete<WorldProperty>(structure.Id);
                DB.Delete<WorldProperty>(parent.Id);
            }
        }

        // List clicks report their row through NuiGetEventArrayIndex, which only exists inside a real NUI event, so the
        // tests call the row methods the click handlers forward to.
        private static List<string> ListingIds(MarketListingViewModel model) => ((List<string>)Get(model, "_itemIds")).ToList();

        private static void RemoveListing(EngineTestContext ctx, MarketListingViewModel model, string listingId)
        {
            var index = ListingIds(model).IndexOf(listingId);
            ctx.Assert(index >= 0, "The listing window shows the listing");
            Call(model, "RemoveListing", index);
            model.OnModalConfirmClick()();
        }

        private static void Buy(EngineTestContext ctx, MarketBuyViewModel model, MarketItem listing)
        {
            Call(model, "Search");
            var rows = ((IEnumerable)Get(model, "_rows")).Cast<object>().ToList();
            var index = rows.FindIndex(row => (string)row.GetType().GetProperty("ItemId").GetValue(row) == listing.Id);
            ctx.Assert(index >= 0, "The market shows the listing");
            Call(model, "Buy", index);
            model.OnModalConfirmClick()();
        }

        private static (WorldProperty Property, PlayerShip Ship) Ship(PlayerAbilityFixture fixture, string name)
        {
            var property = new WorldProperty
            {
                OwnerPlayerId = fixture.Id, CustomName = "Claim space " + name, PropertyType = PropertyType.Starship
            };
            var dock = GetLocation(fixture.Creature);
            property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
            {
                AreaResref = GetResRef(GetAreaFromLocation(dock)), X = GetPositionFromLocation(dock).X,
                Y = GetPositionFromLocation(dock).Y, Z = GetPositionFromLocation(dock).Z
            };
            var ship = new PlayerShip
            {
                OwnerPlayerId = fixture.Id, PropertyId = property.Id,
                Status = new ShipStatus
                {
                    ItemTag = "sd_striker", Hull = 50, MaxHull = 50, Shield = 50, MaxShield = 50,
                    Capacitor = 50, MaxCapacitor = 50
                }
            };
            return (property, ship);
        }

        private static void SelectShip(EngineTestContext ctx, ShipManagementViewModel model, string shipId)
        {
            var index = ((List<string>)Get(model, "_shipIds")).IndexOf(shipId);
            ctx.Assert(index >= 0, "The ship window lists the ship");
            Set(model, "SelectedShipIndex", index);
            Call(model, "LoadShip");
        }

        private static WorldPropertyPermission Permission(string playerId, string propertyId, PropertyType type) => new()
        {
            PlayerId = playerId, PropertyId = propertyId,
            Permissions = Property.GetPermissionsByPropertyType(type).ToDictionary(x => x, _ => true),
            GrantPermissions = Property.GetPermissionsByPropertyType(type).ToDictionary(x => x, _ => true)
        };

        private static void SelectStoredItem(EngineTestContext ctx, PropertyItemStorageViewModel model, string categoryId, string itemId)
        {
            var categoryIndex = ((List<string>)Get(model, "_categoryIds")).IndexOf(categoryId);
            ctx.Assert(categoryIndex >= 0, "The storage window lists the category");
            Set(model, "SelectedCategoryIndex", categoryIndex);
            Call(model, "LoadCategory");
            var itemIndex = ((List<string>)Get(model, "_itemIds")).IndexOf(itemId);
            ctx.Assert(itemIndex >= 0, "The storage window lists the item");
            Set(model, "SelectedItemIndex", itemIndex);
        }

        private static void RetrieveStructure(EngineTestContext ctx, ManageStructuresViewModel model, string structureId)
        {
            var index = ((List<string>)Get(model, "_structurePropertyIds")).IndexOf(structureId);
            ctx.Assert(index >= 0, "The structure window lists the structure");
            Set(model, "SelectedStructureIndex", index);
            model.OnRetrieveStructure()();
            model.OnModalConfirmClick()();
        }
    }
}
