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
        private const int PageSize = 25;
        private static readonly int[] OwnedCounts = { 0, 49, 50, 51, 120 };
        private static readonly int[] SharedCounts = { 0, 24, 25, 26, 49, 50, 51, 120 };

        [EngineTest("Owned ship list loads every ship at and past the 50-row search limit", Category = "ShipManagement", TimeoutSeconds = 120f)]
        public static async Task OwnedShipListIsComplete(EngineTestContext ctx)
        {
            var details = new List<string>();
            foreach (var count in OwnedCounts)
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

        [EngineTest("Shared ship pages cover every permitted ship at and past the 50-row search limit", Category = "ShipManagement", TimeoutSeconds = 180f)]
        public static async Task SharedShipPagesAreComplete(EngineTestContext ctx)
        {
            var details = new List<string>();
            foreach (var count in SharedCounts)
            {
                var created = new List<EntityBase>();
                try
                {
                    var playerId = NewId();
                    var shared = Enumerable.Range(0, count)
                        .Select(index => AddSharedShip(created, playerId, $"Shared {index:000}", PropertyPermissionType.EnterProperty).Ship)
                        .ToList();
                    var withoutAccess = AddShipsWithoutAccess(created, playerId);

                    List<string> propertyIds = null;
                    var idQueries = CountQueries(() => propertyIds = PlayerShipAccess.GetSharedPropertyIds(playerId));
                    ctx.AssertEqual(3L, idQueries.Searches, $"{count} shared: Id searches (permission count and page, owned count)");
                    ctx.AssertEqual((long)(count + withoutAccess.PermissionRows), idQueries.Rows, $"{count} shared: permission rows read once");

                    var pages = ReadSharedPages(propertyIds, out var totalCount, out var pageQueries);
                    AssertSharedPages(ctx, shared, pages, totalCount, $"{count} shared");
                    ctx.Assert(pages.SelectMany(page => page).All(entry => !withoutAccess.ShipIds.Contains(entry.Ship.Id)),
                        $"{count} shared: ships without access, queued for deletion or of another property type stay excluded");
                    AssertPageCosts(ctx, pages, pageQueries, $"{count} shared");

                    // The old query also matched the queued-for-deletion ship, which keeps its ship record in this fixture.
                    var legacy = LegacySharedPage(playerId).Count;
                    ctx.AssertEqual(Math.Min(count + 1, UnpagedSearchLimit), legacy, $"{count} shared: the unpaged ship search this replaces");
                    details.Add($"{count} shared: {pages.Count} pages ({string.Join("/", pages.Select(page => page.Count))}), unpaged {legacy}");
                }
                finally
                {
                    DeleteAll(created);
                }

                await ctx.WaitFrameAsync();
            }

            List<string> noRecords = null;
            var none = CountQueries(() => noRecords = PlayerShipAccess.GetSharedPropertyIds(NewId()));
            ctx.AssertEqual(0, noRecords.Count, "a player without permission records has no shared ships");
            ctx.AssertEqual(1L, none.Searches, "a player without permission records needs only the permission count");
            var emptyPage = CountQueries(() => PlayerShipAccess.GetSharedShipsPage(noRecords, 0, PageSize, out _));
            ctx.AssertEqual(0L, emptyPage.Searches, "an empty Id list pages without a search");
            ctx.SetResultDetail(string.Join("; ", details));
        }

        [EngineTest("Owned ships no longer use up the shared ship list's rows", Category = "ShipManagement", TimeoutSeconds = 180f)]
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
                    var owned = Enumerable.Range(0, ownedCount).Select(index => AddDockedShip(created, playerId, $"Owned {index:00}")).ToList();
                    foreach (var ship in owned)
                        Grant(created, playerId, ship.PropertyId, isOwner: true);

                    var shared = new List<PlayerShip>();
                    var duplicateGrants = 0;
                    for (var index = 0; index < sharedCount; index++)
                    {
                        var ship = AddSharedShip(created, playerId, $"Shared {index:000}", PropertyPermissionType.EnterProperty).Ship;
                        // Duplicate records for one ship must not list it twice.
                        if (index % 5 == 0)
                        {
                            Grant(created, playerId, ship.PropertyId, isOwner: false);
                            duplicateGrants++;
                        }
                        shared.Add(ship);
                    }

                    List<string> propertyIds = null;
                    var idQueries = CountQueries(() => propertyIds = PlayerShipAccess.GetSharedPropertyIds(playerId));
                    ctx.AssertEqual(4L, idQueries.Searches, $"{sharedCount} shared: Id searches (permissions and owned ships, count and page each)");
                    ctx.AssertEqual((long)(ownedCount + sharedCount + duplicateGrants + ownedCount), idQueries.Rows,
                        $"{sharedCount} shared: rows read (every permission record, then each owned ship)");
                    ctx.AssertEqual(sharedCount, propertyIds.Count, $"{sharedCount} shared: owned ships and duplicate records leave the Id list");

                    var pages = ReadSharedPages(propertyIds, out var totalCount, out _);
                    AssertSharedPages(ctx, shared, pages, totalCount, $"{ownedCount} owned + {sharedCount} shared");
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
                    details.Add($"{ownedCount}+{sharedCount}: {totalCount} shared in {pages.Count} pages, unpaged page {legacyPage.Count} with {ownedInPage} owned");
                }
                finally
                {
                    DeleteAll(created);
                }

                await ctx.WaitFrameAsync();
            }

            ctx.SetResultDetail(string.Join("; ", details));
        }

        [EngineTest("Shared ships with identical names page without repeats or gaps", Category = "ShipManagement", TimeoutSeconds = 120f)]
        public static async Task IdenticalShipNamesPageCleanly(EngineTestContext ctx)
        {
            var created = new List<EntityBase>();
            try
            {
                // New ships are all named "Starship" until renamed, so name ties are the common case.
                var playerId = NewId();
                var shared = Enumerable.Range(0, 60)
                    .Select(_ => AddSharedShip(created, playerId, "Starship", PropertyPermissionType.EnterProperty).Ship)
                    .ToList();

                var propertyIds = PlayerShipAccess.GetSharedPropertyIds(playerId);
                var pages = ReadSharedPages(propertyIds, out var totalCount, out _);
                AssertSharedPages(ctx, shared, pages, totalCount, "60 ships named Starship");

                // Repeated reads of the same page return the same ships in the same order.
                for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
                {
                    var again = PlayerShipAccess.GetSharedShipsPage(propertyIds, pageIndex, PageSize, out _);
                    ctx.Assert(again.Select(entry => entry.Ship.Id).SequenceEqual(pages[pageIndex].Select(entry => entry.Ship.Id)),
                        $"page {pageIndex + 1} reads back in the same order");
                }

                var pastEnd = PlayerShipAccess.GetSharedShipsPage(propertyIds, 99, PageSize, out _);
                ctx.Assert(pastEnd.Select(entry => entry.Ship.Id).SequenceEqual(pages[^1].Select(entry => entry.Ship.Id)),
                    "a page past the end returns the last page");
                ctx.SetResultDetail($"60 identical names: {string.Join("/", pages.Select(page => page.Count))}");
            }
            finally
            {
                DeleteAll(created);
            }

            await ctx.WaitFrameAsync();
        }

        [EngineTest("Other Ships pages 25 ships at a time and a page flip reads only that page", Category = "ShipManagement", TimeoutSeconds = 60f)]
        public static Task OtherShipsPagesTwentyFiveAtATime(EngineTestContext ctx)
        {
            return WithShipManagementAsync(ctx, async (actor, model, created, _, _) =>
            {
                var owned = Enumerable.Range(0, 3).Select(index => AddDockedShip(created, actor.Id, $"Owned {index}")).ToList();
                foreach (var ship in owned)
                    Grant(created, actor.Id, ship.PropertyId, isOwner: true);
                var shared = Enumerable.Range(0, 26)
                    .Select(index => AddSharedShip(created, actor.Id, $"Shared {index:00}", PropertyPermissionType.EnterProperty))
                    .ToList();
                // Extra permission records the page flips must not read again.
                for (var index = 0; index < 40; index++)
                    Grant(created, actor.Id, NewId(), isOwner: true);

                await OpenShipManagementAsync(ctx, actor);
                AssertListed(ctx, model, owned, "My Ships");
                ctx.AssertEqual(1, model.PageNumbers.Count, "My Ships: one page");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickOtherShips());
                ctx.AssertEqual(2, model.PageNumbers.Count, "Other Ships: 26 ships make two pages");
                ctx.AssertEqual(0, model.SelectedPageIndex, "Other Ships opens on the first page");
                AssertListed(ctx, model, shared.Take(PageSize).Select(entry => entry.Ship).ToList(), "Other Ships page 1");
                ctx.Assert(model.ShipNames.SequenceEqual(Enumerable.Range(0, PageSize).Select(index => $"Shared {index:00}")),
                    "page 1 lists the first 25 ships by name");

                (long Searches, long Rows) flip = default;
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => flip = CountQueries(model.OnNextPage()));
                AssertListed(ctx, model, new List<PlayerShip> { shared[25].Ship }, "Other Ships page 2");
                ctx.AssertEqual(1, model.SelectedPageIndex, "Next moves to page 2");
                ctx.AssertEqual(4L, flip.Searches, "a page flip runs the page's property and ship queries only");
                ctx.AssertEqual(2L, flip.Rows, "a page flip reads one property and one ship per listed row");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => flip = CountQueries(model.OnNextPage()));
                ctx.AssertEqual(0L, flip.Searches, "Next on the last page does nothing");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => flip = CountQueries(model.OnPreviousPage()));
                ctx.AssertEqual(0, model.SelectedPageIndex, "Previous returns to page 1");
                ctx.AssertEqual((long)(2 * PageSize), flip.Rows, "page 1 reads its 25 properties and ships");

                // The page selector is a watched combo: the client's change runs the property setter.
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => model.SelectedPageIndex = 1);
                AssertListed(ctx, model, new List<PlayerShip> { shared[25].Ship }, "Other Ships page 2 from the page selector");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, shared[25].Ship));
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickRemoveAccess());
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnModalConfirmClick());
                ctx.AssertEqual(1, model.PageNumbers.Count, "removing the only ship on the last page leaves one page");
                ctx.AssertEqual(0, model.SelectedPageIndex, "the emptied last page falls back to page 1");
                AssertListed(ctx, model, shared.Take(PageSize).Select(entry => entry.Ship).ToList(), "Other Ships after removal");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickMyShips());
                AssertListed(ctx, model, owned, "My Ships again");
                ctx.AssertEqual(1, model.PageNumbers.Count, "My Ships again: one page");
            });
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
                    var entry = AddSharedShip(created, actor.Id, $"Shared {index:00}", PropertyPermissionType.EnterProperty);
                    grants[entry.Ship.Id] = entry.Grant;
                    shared.Add(entry.Ship);
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
                var listed = await ReadAllListedPagesAsync(ctx, actor, model);
                ctx.AssertEqual(3, model.PageNumbers.Count, "55 shared ships make three pages");
                ctx.Assert(listed.Select(page => page.Count).SequenceEqual(new[] { 25, 25, 5 }), "pages hold 25, 25 and 5 ships");
                var listedIds = listed.SelectMany(page => page).ToList();
                ctx.AssertEqual(listedIds.Count, listedIds.Distinct().Count(), "no ship is listed on two pages");
                ctx.Assert(shared.All(ship => listedIds.Contains(ship.Id)), "every shared ship is on a page");
                ctx.Assert(!listedIds.Contains(foreign.Id), "a ship shared only with another player is not listed");

                makeEnterable(target);
                makeEnterable(revoked);

                // Access revoked after selection blocks boarding.
                await ShowAndSelectAsync(ctx, actor, model, revoked);
                ctx.Assert(model.IsBoardShipEnabled, "the omitted ship is boardable while its permission record exists");
                DB.Delete<WorldPropertyPermission>(grants[revoked.Id].Id);
                created.Remove(grants[revoked.Id]);
                await AssertBoardingRefusedAsync(ctx, actor, model, "a revoked ship");

                await ShowAndSelectAsync(ctx, actor, model, duplicated);
                ctx.AssertEqual(DB.Get<WorldProperty>(duplicated.PropertyId).CustomName, model.ShipName, "a ship with duplicate permission records loads");
                ctx.Assert(model.IsBoardShipEnabled, "a ship with duplicate permission records is boardable");

                await ShowAndSelectAsync(ctx, actor, model, target);
                ctx.AssertEqual(DB.Get<WorldProperty>(target.PropertyId).CustomName, model.ShipName, "the omitted ship loads");
                ctx.Assert(model.IsBoardShipEnabled, "the omitted ship is boardable at its dock");
                await AssertBoardsAsync(ctx, actor, model, boardPosition, "the omitted ship");
                ctx.SetResultDetail($"55 shared ships on 3 pages; unpaged list omitted {omitted.Count}; boarded '{DB.Get<WorldProperty>(target.PropertyId).CustomName}' from page {model.SelectedPageIndex + 1}");
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

        [EngineTest("Players can remove their own access to a shared ship", Category = "ShipManagement", TimeoutSeconds = 60f)]
        public static Task PlayerCanRemoveOwnShipAccess(EngineTestContext ctx)
        {
            return WithShipManagementAsync(ctx, async (actor, model, created, _, _) =>
            {
                var owned = AddDockedShip(created, actor.Id, "Owned");
                var ownedGrant = Grant(created, actor.Id, owned.PropertyId, isOwner: true);

                var leaving = AddSharedShip(created, actor.Id, "Leaving", PropertyPermissionType.EnterProperty);
                var duplicateGrant = Grant(created, actor.Id, leaving.Ship.PropertyId, PropertyPermissionType.RefitShip);
                var otherPlayerGrant = Grant(created, NewId(), leaving.Ship.PropertyId, PropertyPermissionType.EnterProperty);
                var category = AddCategory(created, leaving.Ship.PropertyId);
                var categoryGrant = Grant(created, actor.Id, category.Id, isOwner: true);
                var ownerCategoryGrant = Grant(created, leaving.Ship.OwnerPlayerId, category.Id, isOwner: true);
                var kept = AddSharedShip(created, actor.Id, "Kept", PropertyPermissionType.EnterProperty);

                await OpenShipManagementAsync(ctx, actor);
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, owned));
                ctx.Assert(!model.IsRemoveAccessEnabled, "My Ships offers no access removal");
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickRemoveAccess());
                ctx.Assert(!(model.ModalPromptText ?? string.Empty).Contains("Remove your access"), "an owned ship never prompts for access removal");
                ctx.Assert(DB.Exists<WorldPropertyPermission>(ownedGrant.Id), "an owner keeps their permissions");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickOtherShips());
                AssertListed(ctx, model, new List<PlayerShip> { leaving.Ship, kept.Ship }, "Other Ships before removal");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, kept.Ship));
                ctx.Assert(model.IsRemoveAccessEnabled, "a shared ship offers access removal");
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickRemoveAccess());
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnModalCancelClick());
                ctx.Assert(DB.Exists<WorldPropertyPermission>(kept.Grant.Id), "cancelling keeps the player's access");

                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, leaving.Ship));
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickRemoveAccess());
                ctx.Assert(model.ModalPromptText.Contains("Leaving"), "the prompt names the ship");
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnModalConfirmClick());

                ctx.Assert(!DB.Exists<WorldPropertyPermission>(leaving.Grant.Id), "the player's permission record is removed");
                ctx.Assert(!DB.Exists<WorldPropertyPermission>(duplicateGrant.Id), "a duplicate permission record is removed");
                ctx.Assert(!DB.Exists<WorldPropertyPermission>(categoryGrant.Id), "the player's storage category permission is removed");
                ctx.Assert(DB.Exists<WorldPropertyPermission>(leaving.OwnerGrant.Id), "the owner's ship permissions are kept");
                ctx.Assert(DB.Exists<WorldPropertyPermission>(ownerCategoryGrant.Id), "the owner's category permissions are kept");
                ctx.Assert(DB.Exists<WorldPropertyPermission>(otherPlayerGrant.Id), "other players' permissions are kept");
                ctx.Assert(DB.Exists<WorldPropertyPermission>(kept.Grant.Id), "access to other ships is kept");
                ctx.Assert(DB.Exists<PlayerShip>(leaving.Ship.Id) && DB.Exists<WorldProperty>(leaving.Ship.PropertyId), "the ship itself is untouched");

                AssertListed(ctx, model, new List<PlayerShip> { kept.Ship }, "Other Ships after removal");
                ctx.Assert(!model.IsRemoveAccessEnabled, "no ship is selected after removal");
                ctx.Assert(Gui.IsWindowOpen(actor.Creature, GuiWindowType.ShipManagement), "ship management stays open");
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, model.OnClickOtherShips());
                AssertListed(ctx, model, new List<PlayerShip> { kept.Ship }, "Other Ships reopened");
                var pages = ReadSharedPages(PlayerShipAccess.GetSharedPropertyIds(actor.Id), out var totalCount, out _);
                AssertSharedPages(ctx, new[] { kept.Ship }, pages, totalCount, "shared ships after removal");
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
            var entrances = PropertyCaches.EntrancesByLayout;
            var instances = PropertyCaches.Instances;
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

        /// <summary>
        /// Shows every page through the page selector and returns each page's listed ship Ids, ending on page 1.
        /// </summary>
        private static async Task<List<List<string>>> ReadAllListedPagesAsync(EngineTestContext ctx, PlayerAbilityFixture actor, ShipManagementViewModel model)
        {
            var pages = new List<List<string>>();
            var pageCount = model.PageNumbers.Count;
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var index = pageIndex;
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => model.SelectedPageIndex = index);
                ctx.AssertEqual(index, model.SelectedPageIndex, $"page {index + 1} is shown");
                ctx.AssertEqual(ShipIds(model).Count, model.ShipNames.Count, $"page {index + 1}: ship names");
                pages.Add(ShipIds(model).ToList());
            }

            await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => model.SelectedPageIndex = 0);
            return pages;
        }

        /// <summary>
        /// Moves to the page listing the ship, then selects it.
        /// </summary>
        private static async Task ShowAndSelectAsync(EngineTestContext ctx, PlayerAbilityFixture actor, ShipManagementViewModel model, PlayerShip ship)
        {
            for (var pageIndex = 0; pageIndex < model.PageNumbers.Count && !ShipIds(model).Contains(ship.Id); pageIndex++)
            {
                var index = pageIndex;
                await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => model.SelectedPageIndex = index);
            }

            await ctx.ExecuteInCreatureContextAsync(actor.Creature, () => SelectShip(model, ship));
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
        private static (PlayerShip Ship, WorldPropertyPermission Grant, WorldPropertyPermission OwnerGrant) AddSharedShip(
            List<EntityBase> created, string playerId, string name, params PropertyPermissionType[] granted)
        {
            var ownerId = NewId();
            var ship = AddDockedShip(created, ownerId, name);
            var ownerGrant = Grant(created, ownerId, ship.PropertyId, isOwner: true);
            return (ship, Grant(created, playerId, ship.PropertyId, granted), ownerGrant);
        }

        private static WorldPropertyCategory AddCategory(List<EntityBase> created, string propertyId)
        {
            var category = new WorldPropertyCategory { ParentPropertyId = propertyId, Name = "Cargo" };
            DB.Set(category);
            created.Add(category);
            return category;
        }

        /// <summary>
        /// Ships the shared list must not show, plus the player's permission records that match no listable starship.
        /// </summary>
        private static (HashSet<string> ShipIds, int PermissionRows) AddShipsWithoutAccess(List<EntityBase> created, string playerId)
        {
            var shipIds = new HashSet<string>();
            var outsiderId = NewId();
            for (var index = 0; index < 60; index++)
                shipIds.Add(AddSharedShip(created, outsiderId, $"Outsider {index:00}", PropertyPermissionType.EnterProperty).Ship.Id);

            var revoked = AddSharedShip(created, playerId, "Revoked", PropertyPermissionType.EnterProperty);
            DB.Delete<WorldPropertyPermission>(revoked.Grant.Id);
            created.Remove(revoked.Grant);
            shipIds.Add(revoked.Ship.Id);

            // An unregistered ship's property stays, queued for deletion, until the next boot.
            var queued = AddSharedShip(created, playerId, "Queued", PropertyPermissionType.EnterProperty);
            var queuedProperty = DB.Get<WorldProperty>(queued.Ship.PropertyId);
            queuedProperty.IsQueuedForDeletion = true;
            DB.Set(queuedProperty);
            shipIds.Add(queued.Ship.Id);

            var apartment = new WorldProperty { CustomName = "Apartment", PropertyType = PropertyType.Apartment, OwnerPlayerId = NewId() };
            DB.Set(apartment);
            created.Add(apartment);
            Grant(created, playerId, apartment.Id, isOwner: false);

            const int unmatchedGrants = 5;
            for (var index = 0; index < unmatchedGrants; index++)
                Grant(created, playerId, NewId(), isOwner: true);

            return (shipIds, unmatchedGrants + 2);
        }

        /// <summary>
        /// Reads every page the way the window does: one call per page against a property Id list loaded once.
        /// </summary>
        private static List<List<(PlayerShip Ship, WorldProperty Property)>> ReadSharedPages(
            List<string> propertyIds, out int totalCount, out List<(long Searches, long Rows)> pageQueries)
        {
            var pages = new List<List<(PlayerShip Ship, WorldProperty Property)>>();
            pageQueries = new List<(long Searches, long Rows)>();
            totalCount = 0;
            for (var pageIndex = 0; ; pageIndex++)
            {
                List<(PlayerShip Ship, WorldProperty Property)> page = null;
                var total = 0;
                var index = pageIndex;
                pageQueries.Add(CountQueries(() => page = PlayerShipAccess.GetSharedShipsPage(propertyIds, index, PageSize, out total)));
                totalCount = total;
                if (page.Count > 0)
                    pages.Add(page);
                if ((pageIndex + 1) * PageSize >= totalCount)
                    return pages;
            }
        }

        private static void AssertSharedPages(EngineTestContext ctx, IEnumerable<PlayerShip> expected,
            List<List<(PlayerShip Ship, WorldProperty Property)>> pages, int totalCount, string label)
        {
            var expectedIds = expected.Select(ship => ship.Id).ToHashSet();
            ctx.AssertEqual(expectedIds.Count, totalCount, $"{label}: total shared ships");
            ctx.AssertEqual((totalCount + PageSize - 1) / PageSize, pages.Count, $"{label}: pages");
            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var size = pageIndex < pages.Count - 1 ? PageSize : totalCount - PageSize * (pages.Count - 1);
                ctx.AssertEqual(size, pages[pageIndex].Count, $"{label}: page {pageIndex + 1} size");
            }

            var entries = pages.SelectMany(page => page).ToList();
            var ids = entries.Select(entry => entry.Ship.Id).ToList();
            ctx.AssertEqual(ids.Count, ids.Distinct().Count(), $"{label}: no ship on two pages");
            ctx.AssertEqual(0, expectedIds.Except(ids).Count(), $"{label}: missing ships");
            ctx.AssertEqual(0, ids.Except(expectedIds).Count(), $"{label}: unexpected ships");
            ctx.Assert(entries.All(entry => entry.Ship.PropertyId == entry.Property.Id), $"{label}: each ship carries its own property");
            ctx.Assert(entries.Zip(entries.Skip(1)).All(pair =>
                    string.Compare(pair.First.Property.CustomName, pair.Second.Property.CustomName, StringComparison.OrdinalIgnoreCase) <= 0),
                $"{label}: pages are sorted by ship name");
        }

        private static void AssertPageCosts(EngineTestContext ctx, List<List<(PlayerShip Ship, WorldProperty Property)>> pages,
            List<(long Searches, long Rows)> pageQueries, string label)
        {
            if (pages.Count == 0)
            {
                ctx.AssertEqual(1, pageQueries.Count, $"{label}: one empty page read");
                ctx.AssertEqual(1L, pageQueries[0].Searches, $"{label}: an empty list needs only the count");
                ctx.AssertEqual(0L, pageQueries[0].Rows, $"{label}: an empty list reads no rows");
                return;
            }

            ctx.AssertEqual(pages.Count, pageQueries.Count, $"{label}: one read per page");
            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                ctx.AssertEqual(4L, pageQueries[pageIndex].Searches, $"{label}: page {pageIndex + 1} searches (properties and ships, count and page each)");
                ctx.AssertEqual(2L * pages[pageIndex].Count, pageQueries[pageIndex].Rows, $"{label}: page {pageIndex + 1} reads one property and one ship per row");
            }
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
            // Records the code under test already deleted are skipped.
            foreach (var entity in created)
            {
                switch (entity)
                {
                    case PlayerShip when DB.Exists<PlayerShip>(entity.Id):
                        DB.Delete<PlayerShip>(entity.Id);
                        break;
                    case WorldPropertyPermission when DB.Exists<WorldPropertyPermission>(entity.Id):
                        DB.Delete<WorldPropertyPermission>(entity.Id);
                        break;
                    case WorldPropertyCategory when DB.Exists<WorldPropertyCategory>(entity.Id):
                        DB.Delete<WorldPropertyCategory>(entity.Id);
                        break;
                    case WorldProperty when DB.Exists<WorldProperty>(entity.Id):
                        DB.Delete<WorldProperty>(entity.Id);
                        break;
                }
            }

            created.Clear();
        }
    }
}
