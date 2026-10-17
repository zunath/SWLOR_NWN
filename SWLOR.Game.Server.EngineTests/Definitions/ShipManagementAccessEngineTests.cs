using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Runs the ship management lists against the live Redis search index, where an unpaged search stops at 50 rows.
    /// </summary>
    public static class ShipManagementAccessEngineTests
    {
        private const string ShipItemTag = "ShipDeedLightFreighter";
        private const string DockAreaResref = "engine_shipaccess";
        private const int UnpagedSearchLimit = 50;
        private static readonly int[] BoundaryCounts = { 0, 49, 50, 51, 120 };

        [EngineTest("Owned ship list loads every ship at and past the 50-row search limit", Category = "ShipManagement", TimeoutSeconds = 120f)]
        public static async Task OwnedShipListIsComplete(EngineTestContext ctx)
        {
            var details = new List<string>();
            foreach (var count in BoundaryCounts)
            {
                var created = new List<EntityBase>();
                try
                {
                    var playerId = NewId();
                    var owned = Enumerable.Range(0, count).Select(_ => AddShip(created, playerId)).ToList();
                    foreach (var ship in owned)
                        Grant(created, playerId, ship.PropertyId, isOwner: true);
                    var otherOwnerId = NewId();
                    Grant(created, otherOwnerId, AddShip(created, otherOwnerId).PropertyId, isOwner: true);

                    List<PlayerShip> listed = null;
                    var queries = CountQueries(() => listed = PlayerShipAccess.GetOwnedShips(playerId));
                    AssertSameShips(ctx, owned, listed, $"{count} owned");
                    ctx.AssertEqual(count > 0 ? 2L : 1L, queries.Searches, $"{count} owned: searches (count, then one page)");
                    ctx.AssertEqual((long)count, queries.Rows, $"{count} owned: rows read");

                    var unpaged = UnpagedOwnedShips(playerId).Count;
                    ctx.AssertEqual(Math.Min(count, UnpagedSearchLimit), unpaged, $"{count} owned: the unpaged search this replaces");
                    details.Add($"{count} owned: listed {listed.Count}, unpaged {unpaged}");
                }
                finally
                {
                    DeleteAll(created);
                }

                await ctx.WaitFrameAsync();
            }

            ctx.SetResultDetail(string.Join("; ", details));
        }

        [EngineTest("Shared ship list loads every permitted ship at and past the 50-row search limit", Category = "ShipManagement", TimeoutSeconds = 120f)]
        public static async Task SharedShipListIsComplete(EngineTestContext ctx)
        {
            var details = new List<string>();
            foreach (var count in BoundaryCounts)
            {
                var created = new List<EntityBase>();
                try
                {
                    var playerId = NewId();
                    var shared = new List<PlayerShip>();
                    for (var index = 0; index < count; index++)
                    {
                        var ownerId = NewId();
                        var ship = AddShip(created, ownerId);
                        Grant(created, ownerId, ship.PropertyId, isOwner: true);
                        Grant(created, playerId, ship.PropertyId, isOwner: false);
                        shared.Add(ship);
                    }

                    var withoutAccess = AddShipsWithoutAccess(created, playerId);
                    var permissionRows = count + withoutAccess.NonShipGrants;

                    List<PlayerShip> listed = null;
                    var queries = CountQueries(() => listed = PlayerShipAccess.GetSharedShips(playerId));
                    AssertSameShips(ctx, shared, listed, $"{count} shared");
                    ctx.Assert(listed.All(ship => !withoutAccess.ShipIds.Contains(ship.Id)), $"{count} shared: ships without a permission record stay excluded");
                    ctx.AssertEqual(count > 0 ? 4L : 3L, queries.Searches, $"{count} shared: searches (permissions count and page, ships count and page)");
                    ctx.AssertEqual((long)(permissionRows + count), queries.Rows, $"{count} shared: rows read");

                    var legacy = LegacySharedPage(playerId).Count;
                    ctx.AssertEqual(Math.Min(count, UnpagedSearchLimit), legacy, $"{count} shared: the unpaged ship search this replaces");
                    details.Add($"{count} shared: listed {listed.Count}, unpaged {legacy}");
                }
                finally
                {
                    DeleteAll(created);
                }

                await ctx.WaitFrameAsync();
            }

            var noRecords = new List<PlayerShip>();
            var none = CountQueries(() => noRecords = PlayerShipAccess.GetSharedShips(NewId()));
            ctx.AssertEqual(0, noRecords.Count, "a player without permission records sees no shared ships");
            ctx.AssertEqual(1L, none.Searches, "a player without permission records needs only the permission count");
            ctx.SetResultDetail(string.Join("; ", details));
        }

        [EngineTest("Owned ships no longer use up the shared ship list's rows", Category = "ShipManagement", TimeoutSeconds = 120f)]
        public static async Task OwnedShipsDoNotCrowdOutSharedShips(EngineTestContext ctx)
        {
            const int ownedCount = 10;
            var details = new List<string>();
            foreach (var sharedCount in new[] { 40, 41, 49, 50, 51, 110 })
            {
                var created = new List<EntityBase>();
                try
                {
                    var playerId = NewId();
                    var owned = Enumerable.Range(0, ownedCount).Select(_ => AddShip(created, playerId)).ToList();
                    foreach (var ship in owned)
                        Grant(created, playerId, ship.PropertyId, isOwner: true);

                    var shared = new List<PlayerShip>();
                    var duplicateGrants = 0;
                    for (var index = 0; index < sharedCount; index++)
                    {
                        var ownerId = NewId();
                        var ship = AddShip(created, ownerId);
                        Grant(created, ownerId, ship.PropertyId, isOwner: true);
                        Grant(created, playerId, ship.PropertyId, isOwner: false);
                        // Duplicate records for one ship must not list it twice.
                        if (index % 5 == 0)
                        {
                            Grant(created, playerId, ship.PropertyId, isOwner: false);
                            duplicateGrants++;
                        }
                        shared.Add(ship);
                    }

                    List<PlayerShip> listed = null;
                    var queries = CountQueries(() => listed = PlayerShipAccess.GetSharedShips(playerId));
                    AssertSameShips(ctx, shared, listed, $"{ownedCount} owned + {sharedCount} shared");
                    ctx.AssertEqual(4L, queries.Searches, $"{sharedCount} shared: searches");
                    ctx.AssertEqual((long)(ownedCount + sharedCount + duplicateGrants + ownedCount + sharedCount), queries.Rows,
                        $"{sharedCount} shared: rows read (every permission record, then every matching ship once)");
                    AssertSameShips(ctx, owned, PlayerShipAccess.GetOwnedShips(playerId), $"{ownedCount} owned beside {sharedCount} shared");

                    // The pre-fix list read one unpaged page and only then dropped owned ships.
                    var total = ownedCount + sharedCount;
                    var legacyPage = LegacySharedPage(playerId);
                    var ownedInPage = legacyPage.Count(ship => ship.OwnerPlayerId == playerId);
                    var legacyShared = legacyPage.Count - ownedInPage;
                    ctx.AssertEqual(Math.Min(total, UnpagedSearchLimit), legacyPage.Count, $"{sharedCount} shared: the unpaged page");
                    if (total > UnpagedSearchLimit && sharedCount < UnpagedSearchLimit)
                        ctx.Assert(ownedInPage > 0, $"{sharedCount} shared: owned ships take rows of the unpaged page");
                    if (sharedCount > UnpagedSearchLimit)
                        ctx.Assert(legacyShared < sharedCount, $"{sharedCount} shared: the unpaged page loses shared ships");
                    details.Add($"{ownedCount}+{sharedCount}: listed {listed.Count}, unpaged page {legacyPage.Count} with {ownedInPage} owned");
                }
                finally
                {
                    DeleteAll(created);
                }

                await ctx.WaitFrameAsync();
            }

            ctx.SetResultDetail(string.Join("; ", details));
        }

        [EngineTest("A shared ship the 50-row list omitted can be selected and boarded", Category = "ShipManagement", TimeoutSeconds = 60f)]
        public static Task OmittedSharedShipCanBeBoarded(EngineTestContext ctx)
        {
            return WithShipManagementAsync(ctx, async (actor, model, created, makeEnterable, boardPosition) =>
            {
                var owned = Enumerable.Range(0, 3).Select(index => AddDockedShip(created, actor.Id, $"Owned {index}")).ToList();
                foreach (var ship in owned)
                    Grant(created, actor.Id, ship.PropertyId, isOwner: true);

                var shared = new List<PlayerShip>();
                var grants = new Dictionary<string, WorldPropertyPermission>();
                for (var index = 0; index < 55; index++)
                {
                    var ownerId = NewId();
                    var ship = AddDockedShip(created, ownerId, $"Shared {index:00}");
                    Grant(created, ownerId, ship.PropertyId, isOwner: true);
                    grants[ship.Id] = Grant(created, actor.Id, ship.PropertyId, isOwner: false);
                    shared.Add(ship);
                }

                var outsiderId = NewId();
                var foreign = AddDockedShip(created, NewId(), "Foreign");
                Grant(created, outsiderId, foreign.PropertyId, isOwner: false);

                var legacyIds = LegacySharedPage(actor.Id).Select(ship => ship.Id).ToHashSet();
                var omitted = shared.Where(ship => !legacyIds.Contains(ship.Id)).ToList();
                ctx.Assert(omitted.Count >= 5, $"the unpaged list omits at least five of 55 shared ships, omitted {omitted.Count}");
                var target = omitted[0];
                var revoked = omitted[1];
                var duplicated = shared.First(ship => ship.Id != target.Id && ship.Id != revoked.Id);
                Grant(created, actor.Id, duplicated.PropertyId, isOwner: false);

                await OpenShipManagementAsync(ctx, actor);
                ctx.Assert(Gui.IsWindowOpen(actor.Creature, GuiWindowType.ShipManagement), "ship management opens for the player");
                AssertListed(ctx, model, owned, "My Ships");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickOtherShips());
                AssertListed(ctx, model, shared, "Other Ships");
                ctx.Assert(!ShipIds(model).Contains(foreign.Id), "a ship shared only with another player is not listed");

                makeEnterable(target);
                makeEnterable(revoked);

                // Access revoked after selection blocks boarding.
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, revoked));
                ctx.Assert(model.IsBoardShipEnabled, "the omitted ship is boardable while its permission record exists");
                DB.Delete<WorldPropertyPermission>(grants[revoked.Id].Id);
                created.Remove(grants[revoked.Id]);
                await AssertBoardingRefusedAsync(ctx, actor, model, "a revoked ship");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, duplicated));
                ctx.AssertEqual(DB.Get<WorldProperty>(duplicated.PropertyId).CustomName, model.ShipName, "a ship with duplicate permission records loads");
                ctx.Assert(model.IsBoardShipEnabled, "a ship with duplicate permission records is boardable");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, target));
                ctx.AssertEqual(DB.Get<WorldProperty>(target.PropertyId).CustomName, model.ShipName, "the omitted ship loads");
                ctx.Assert(model.IsBoardShipEnabled, "the omitted ship is boardable at its dock");
                await AssertBoardsAsync(ctx, actor, model, boardPosition, "the omitted ship");
                ctx.SetResultDetail($"55 shared ships listed; unpaged list omitted {omitted.Count}; boarded '{DB.Get<WorldProperty>(target.PropertyId).CustomName}'");
            });
        }

        [EngineTest("Boarding from ship management requires the Enter Property permission", Category = "ShipManagement", TimeoutSeconds = 60f)]
        public static Task BoardingRequiresEnterProperty(EngineTestContext ctx)
        {
            return WithShipManagementAsync(ctx, async (actor, model, created, makeEnterable, boardPosition) =>
            {
                var owned = AddDockedShip(created, actor.Id, "Owned");
                Grant(created, actor.Id, owned.PropertyId, isOwner: true);
                var refitOnly = AddSharedShip(created, actor.Id, "Refit only", PropertyPermissionType.RefitShip);
                var enterRemoved = AddSharedShip(created, actor.Id, "Enter removed", PropertyPermissionType.EnterProperty, PropertyPermissionType.RefitShip);
                var enterGranted = AddSharedShip(created, actor.Id, "Enter granted", PropertyPermissionType.EnterProperty);
                foreach (var ship in new[] { owned, refitOnly.Ship, enterRemoved.Ship, enterGranted.Ship })
                    makeEnterable(ship);

                await OpenShipManagementAsync(ctx, actor);
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, owned));
                ctx.Assert(model.IsBoardShipEnabled, "an owner can board their own ship");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickOtherShips());
                AssertListed(ctx, model, new List<PlayerShip> { refitOnly.Ship, enterRemoved.Ship, enterGranted.Ship }, "Other Ships");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, refitOnly.Ship));
                ctx.Assert(!model.IsBoardShipEnabled, "a grant without Enter Property cannot board");
                ctx.Assert(model.IsRefitEnabled, "a Refit grant can still refit the listed ship");
                await AssertBoardingRefusedAsync(ctx, actor, model, "a Refit-only grant");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, enterRemoved.Ship));
                ctx.Assert(model.IsBoardShipEnabled, "an Enter Property grant can board");
                enterRemoved.Grant.Permissions[PropertyPermissionType.EnterProperty] = false;
                DB.Set(enterRemoved.Grant);
                await AssertBoardingRefusedAsync(ctx, actor, model, "Enter Property removed after selection");
                ctx.Assert(model.IsRefitEnabled, "removing Enter Property keeps the Refit permission");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, enterGranted.Ship));
                ctx.Assert(model.IsBoardShipEnabled, "an Enter Property grant can board");
                await AssertBoardsAsync(ctx, actor, model, boardPosition, "an Enter Property grant");
            });
        }

        /// <summary>
        /// Registers ship management for a headless player, docks test ships in the arena, and lets a test make
        /// ships enterable with the arena as their interior. Every change is undone afterward.
        /// </summary>
        private static async Task WithShipManagementAsync(EngineTestContext ctx,
            Func<PlayerAbilityFixture, ShipManagementViewModel, List<EntityBase>, Action<PlayerShip>, Vector3, Task> test)
        {
            using var actor = await PlayerAbilityFixture.CreateAsync(ctx);
            var created = new List<EntityBase>();
            var layout = Space.GetShipDetailByItemTag(ShipItemTag).Layout;
            var entrances = (Dictionary<PropertyLayoutType, Vector4>)typeof(Property)
                .GetField("_entrancesByLayout", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var instances = (Dictionary<string, PropertyInstance>)typeof(Property)
                .GetField("_propertyInstances", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var originalEntrance = entrances[layout];
            var originalPropertyId = Property.GetPropertyId(ctx.Arena);
            var registeredInstances = new List<string>();

            ctx.Assert(!windows.ContainsKey(actor.Id), "fixture GUI registration must be isolated");
            var template = Gui.GetWindowTemplate(GuiWindowType.ShipManagement);
            var playerWindow = template.CreatePlayerWindowAction();
            playerWindow.ViewModel.Geometry = template.InitialGeometry;
            windows.Add(actor.Id, new Dictionary<GuiWindowType, GuiPlayerWindow> { [GuiWindowType.ShipManagement] = playerWindow });
            var boardPosition = GetPositionFromLocation(ctx.GetArenaLocation(4f));

            try
            {
                Area.GetAreas()[DockAreaResref] = ctx.Arena;
                entrances[layout] = new Vector4(boardPosition.X, boardPosition.Y, boardPosition.Z, 0f);
                await test(actor, (ShipManagementViewModel)playerWindow.ViewModel, created, ship =>
                {
                    Property.RegisterInstance(ship.PropertyId, ctx.Arena, layout);
                    registeredInstances.Add(ship.PropertyId);
                }, boardPosition);
            }
            finally
            {
                Gui.ClosePlayerWindow(actor.Creature, GuiWindowType.ShipManagement);
                windows.Remove(actor.Id);
                entrances[layout] = originalEntrance;
                foreach (var propertyId in registeredInstances)
                    instances.Remove(propertyId);
                SetLocalString(ctx.Arena, "PROPERTY_ID", originalPropertyId);
                Area.GetAreas().Remove(DockAreaResref);
                DeleteAll(created);
            }
        }

        private static Task OpenShipManagementAsync(EngineTestContext ctx, PlayerAbilityFixture actor)
        {
            return ctx.ExecuteInCreatureContextAsync(actor.Creature, () =>
                Gui.TogglePlayerWindow(actor.Creature, GuiWindowType.ShipManagement,
                    new ShipManagementPayload(PlanetType.Viscara, ctx.GetArenaLocation(), ctx.GetArenaLocation())));
        }

        private static async Task AssertBoardingRefusedAsync(EngineTestContext ctx, PlayerAbilityFixture actor, ShipManagementViewModel model, string label)
        {
            var start = GetPosition(actor.Creature);
            await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickBoardShip());
            await ctx.DelaySecondsAsync(0.5f);
            ctx.Assert(!model.IsBoardShipEnabled, $"{label}: the refused selection has Board turned off");
            ctx.Assert(Gui.IsWindowOpen(actor.Creature, GuiWindowType.ShipManagement), $"{label}: a refused boarding keeps the window open");
            ctx.Assert(Vector3.Distance(start, GetPosition(actor.Creature)) < 0.1f, $"{label}: a refused boarding leaves the player in place");
        }

        private static async Task AssertBoardsAsync(EngineTestContext ctx, PlayerAbilityFixture actor, ShipManagementViewModel model, Vector3 entrance, string label)
        {
            await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickBoardShip());
            await ctx.WaitUntilAsync(() => Vector3.Distance(entrance, GetPosition(actor.Creature)) < 1f, 5f,
                $"{label}: the player to reach the ship's entrance");
            ctx.Assert(!Gui.IsWindowOpen(actor.Creature, GuiWindowType.ShipManagement), $"{label}: boarding closes ship management");
        }

        private static string NewId() => Guid.NewGuid().ToString();

        private static PlayerShip AddShip(List<EntityBase> created, string ownerId)
        {
            var ship = new PlayerShip
            {
                OwnerPlayerId = ownerId,
                PropertyId = NewId(),
                Status = new ShipStatus
                {
                    ItemTag = ShipItemTag,
                    Shield = 10, MaxShield = 10,
                    Hull = 10, MaxHull = 10,
                    Capacitor = 10, MaxCapacitor = 10
                }
            };
            DB.Set(ship);
            created.Add(ship);
            return ship;
        }

        private static PlayerShip AddDockedShip(List<EntityBase> created, string ownerId, string name)
        {
            var ship = AddShip(created, ownerId);
            var property = new WorldProperty
            {
                Id = ship.PropertyId,
                OwnerPlayerId = ownerId,
                CustomName = name,
                PropertyType = PropertyType.Starship
            };
            property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { AreaResref = DockAreaResref };
            DB.Set(property);
            created.Add(property);
            return ship;
        }

        private static WorldPropertyPermission Grant(List<EntityBase> created, string playerId, string propertyId, bool isOwner)
        {
            var permission = new WorldPropertyPermission { PlayerId = playerId, PropertyId = propertyId };
            foreach (var type in Property.GetPermissionsByPropertyType(PropertyType.Starship))
            {
                permission.Permissions[type] = isOwner || type is PropertyPermissionType.EnterProperty or PropertyPermissionType.PilotShip;
                permission.GrantPermissions[type] = isOwner;
            }

            DB.Set(permission);
            created.Add(permission);
            return permission;
        }

        private static WorldPropertyPermission Grant(List<EntityBase> created, string playerId, string propertyId, params PropertyPermissionType[] granted)
        {
            var permission = new WorldPropertyPermission { PlayerId = playerId, PropertyId = propertyId };
            foreach (var type in Property.GetPermissionsByPropertyType(PropertyType.Starship))
            {
                permission.Permissions[type] = granted.Contains(type);
                permission.GrantPermissions[type] = false;
            }

            DB.Set(permission);
            created.Add(permission);
            return permission;
        }

        /// <summary>
        /// A docked ship owned by someone else that shares only the given permissions with the player.
        /// </summary>
        private static (PlayerShip Ship, WorldPropertyPermission Grant) AddSharedShip(List<EntityBase> created, string playerId, string name,
            params PropertyPermissionType[] granted)
        {
            var ownerId = NewId();
            var ship = AddDockedShip(created, ownerId, name);
            Grant(created, ownerId, ship.PropertyId, isOwner: true);
            return (ship, Grant(created, playerId, ship.PropertyId, granted));
        }

        /// <summary>
        /// Ships the player cannot access, alongside permission records that match no ship.
        /// </summary>
        private static (HashSet<string> ShipIds, int NonShipGrants) AddShipsWithoutAccess(List<EntityBase> created, string playerId)
        {
            var shipIds = new HashSet<string>();
            var outsiderId = NewId();
            for (var index = 0; index < 60; index++)
            {
                var ownerId = NewId();
                var ship = AddShip(created, ownerId);
                Grant(created, ownerId, ship.PropertyId, isOwner: true);
                Grant(created, outsiderId, ship.PropertyId, isOwner: false);
                shipIds.Add(ship.Id);
            }

            var revokedShip = AddShip(created, NewId());
            var revoked = Grant(created, playerId, revokedShip.PropertyId, isOwner: false);
            DB.Delete<WorldPropertyPermission>(revoked.Id);
            created.Remove(revoked);
            shipIds.Add(revokedShip.Id);

            const int nonShipGrants = 5;
            for (var index = 0; index < nonShipGrants; index++)
                Grant(created, playerId, NewId(), isOwner: true);

            return (shipIds, nonShipGrants);
        }

        // The queries ship management ran before this fix.
        private static List<PlayerShip> UnpagedOwnedShips(string playerId)
        {
            return DB.Search(new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), playerId, false)).ToList();
        }

        private static List<PlayerShip> LegacySharedPage(string playerId)
        {
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissionCount = (int)DB.SearchCount(permissionQuery);
            var propertyIds = DB.Search(permissionQuery.AddPaging(permissionCount, 0))
                .Select(permission => permission.PropertyId)
                .ToList();
            if (propertyIds.Count <= 0)
                return new List<PlayerShip>();

            return DB.Search(new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyIds)).ToList();
        }

        /// <summary>
        /// Counts search commands and rows read. Main-thread code runs uninterrupted, so the difference is exact.
        /// </summary>
        private static (long Searches, long Rows) CountQueries(Action action)
        {
            var counters = (FrameCounters)typeof(FrameProfiler)
                .GetField("_frame", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            FrameProfiler.BeginCapture();
            try
            {
                var searches = counters.Count(FrameMetric.DBSearch);
                var rows = counters.Count(FrameMetric.DBSearchDocument);
                action();
                return (counters.Count(FrameMetric.DBSearch) - searches, counters.Count(FrameMetric.DBSearchDocument) - rows);
            }
            finally
            {
                FrameProfiler.EndCapture();
            }
        }

        private static void AssertSameShips(EngineTestContext ctx, IEnumerable<PlayerShip> expected, List<PlayerShip> actual, string label)
        {
            var actualIds = actual.Select(ship => ship.Id).ToList();
            var expectedIds = expected.Select(ship => ship.Id).ToHashSet();
            ctx.AssertEqual(actualIds.Count, actualIds.Distinct().Count(), $"{label}: distinct ships listed");
            ctx.AssertEqual(0, expectedIds.Except(actualIds).Count(), $"{label}: missing ships");
            ctx.AssertEqual(0, actualIds.Except(expectedIds).Count(), $"{label}: unexpected ships");
        }

        private static void AssertListed(EngineTestContext ctx, ShipManagementViewModel model, List<PlayerShip> expected, string tab)
        {
            var listed = ShipIds(model);
            ctx.AssertEqual(expected.Count, listed.Count, $"{tab}: listed ships");
            ctx.AssertEqual(listed.Count, listed.Distinct().Count(), $"{tab}: distinct ships listed");
            ctx.Assert(expected.All(ship => listed.Contains(ship.Id)), $"{tab}: every ship is listed");
            ctx.AssertEqual(listed.Count, model.ShipNames.Count, $"{tab}: ship names");
        }

        private static List<string> ShipIds(ShipManagementViewModel model)
        {
            return (List<string>)typeof(ShipManagementViewModel)
                .GetProperty("_shipIds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(model)!;
        }

        /// <summary>
        /// Mirrors OnClickShip, which reads the clicked row from the NUI event a headless player cannot send.
        /// </summary>
        private static void SelectShip(ShipManagementViewModel model, PlayerShip ship)
        {
            var index = ShipIds(model).IndexOf(ship.Id);
            if (index < 0)
                throw new InvalidOperationException($"Ship {ship.Id} is not listed.");

            var selected = typeof(ShipManagementViewModel)
                .GetProperty("SelectedShipIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var previous = (int)selected.GetValue(model)!;
            if (previous > -1)
                model.ShipToggles[previous] = false;
            selected.SetValue(model, index);
            model.ShipToggles[index] = true;
            typeof(ShipManagementViewModel)
                .GetMethod("LoadShip", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(model, null);
        }

        private static void DeleteAll(List<EntityBase> created)
        {
            foreach (var entity in created)
            {
                switch (entity)
                {
                    case PlayerShip:
                        DB.Delete<PlayerShip>(entity.Id);
                        break;
                    case WorldPropertyPermission:
                        DB.Delete<WorldPropertyPermission>(entity.Id);
                        break;
                    case WorldProperty:
                        DB.Delete<WorldProperty>(entity.Id);
                        break;
                }
            }

            created.Clear();
        }
    }
}
