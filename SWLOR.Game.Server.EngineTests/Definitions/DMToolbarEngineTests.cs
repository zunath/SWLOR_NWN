using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Drives the NUI DM toolbar windows through real server NUI state on headless player fixtures.
    /// Each test also exports window layouts and bind values so an offline client can render them.
    /// </summary>
    public static class DMToolbarEngineTests
    {
        private const string SnapshotFileName = "dm-toolbar-native-states.json";
        private static readonly List<object> Snapshots = new();

        private static readonly GuiWindowType[] DMWindowTypes =
        {
            GuiWindowType.DMToolbar,
            GuiWindowType.DMChooser,
            GuiWindowType.DMCreator,
            GuiWindowType.DMPlayerSearch,
            GuiWindowType.DMPlayerExamine,
            GuiWindowType.DMAdminTools,
            GuiWindowType.CreatureManager,
            GuiWindowType.DMTools,
            GuiWindowType.AreaNotes,
            GuiWindowType.MusicPicker,
            GuiWindowType.DMBattleGrid
        };

        /// <summary>
        /// A headless fixture cannot be made a DM, so this covers everything short of the native
        /// visibility change: placement, the Appear label tracking the native manifest flag (including
        /// the refresh raised by the native radial menu), the non-DM guard, and the window buttons.
        /// </summary>
        [EngineTest("DM toolbar sits in the corner, tracks Appear state and opens the DM windows", Category = "DMToolbar", TimeoutSeconds = 60f)]
        public static async Task ToolbarTracksAppearStateAndOpensWindows(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var stats = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(player.Creature).m_pStats;
            var originalManifested = stats.m_bDMManifested;
            using var windows = RegisterWindows(ctx, player);
            try
            {
                stats.m_bDMManifested = 0;
                await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
                    Gui.TogglePlayerWindow(player.Creature, GuiWindowType.DMToolbar));
                await ctx.WaitFrameAsync();
                var toolbar = (DMToolbarViewModel)windows.ViewModel(GuiWindowType.DMToolbar);
                ctx.AssertEqual(DMToolbarViewModel.ToolbarX, toolbar.Geometry.X, "toolbar pinned to the left edge");
                ctx.AssertEqual(DMToolbarViewModel.ToolbarY, toolbar.Geometry.Y, "toolbar pinned to the top edge");
                ctx.AssertEqual("Appear", toolbar.AppearText, "label while hidden from players");
                Capture(player.Creature, GuiWindowType.DMToolbar, "toolbar-hidden");

                // The native radial menu changes the flag directly and raises the refresh event.
                stats.m_bDMManifested = 1;
                await InContext(ctx, player.Creature, () =>
                    Gui.PublishRefreshEvent(player.Creature, new DMPresenceRefreshEvent()));
                ctx.AssertEqual("Disappear", toolbar.AppearText, "label while visible to players");
                Capture(player.Creature, GuiWindowType.DMToolbar, "toolbar-visible");

                // Area entry and possession changes re-show the toolbar; one already in place stays open.
                await InContext(ctx, player.Creature, () => DMPresence.ShowWindow(player.Creature, GuiWindowType.DMToolbar));
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMToolbar), "showing an open toolbar keeps it open");

                // Appear and Disappear only ever act on DM avatars.
                await InContext(ctx, player.Creature, toolbar.OnClickToggleAppear());
                ctx.Assert(DMPresence.GetIsManifested(player.Creature), "a non-DM's manifest state is left alone");

                await InContext(ctx, player.Creature, toolbar.OnClickChooser());
                await InContext(ctx, player.Creature, toolbar.OnClickCreator());
                await InContext(ctx, player.Creature, toolbar.OnClickPlayers());
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMChooser), "Chooser opens from the toolbar");
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMCreator), "Creator opens from the toolbar");
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMPlayerSearch), "Player Search opens from the toolbar");
                ctx.Assert(windows.ViewModel(GuiWindowType.DMChooser).Geometry.Y >= DMToolbarViewModel.ToolbarHeight,
                    "the Chooser opens below the toolbar");

                await InContext(ctx, player.Creature, toolbar.OnClickChooser());
                ctx.Assert(!Gui.IsWindowOpen(player.Creature, GuiWindowType.DMChooser), "a second click closes the Chooser");

                // The team's existing DM windows open from the toolbar too.
                await InContext(ctx, player.Creature, toolbar.OnClickCreatureManager());
                await InContext(ctx, player.Creature, toolbar.OnClickPlaceableTools());
                await InContext(ctx, player.Creature, toolbar.OnClickAreaNotes());
                await InContext(ctx, player.Creature, toolbar.OnClickMusic());
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.CreatureManager), "Creature Manager opens from the toolbar");
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMTools), "Placeable Tools open from the toolbar");
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.AreaNotes), "Area Notes open from the toolbar");
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.MusicPicker), "Music opens from the toolbar");

                // The fixture has no CD key, so it is not an admin: the Admin button is hidden and does nothing.
                ctx.Assert(!toolbar.IsAdmin, "non-admins do not see the Admin button");
                ctx.AssertEqual(DMToolbarViewModel.ToolbarWidth, toolbar.Geometry.Width, "the toolbar ends before the hidden Admin button");
                await InContext(ctx, player.Creature, toolbar.OnClickAdmin());
                ctx.Assert(!Gui.IsWindowOpen(player.Creature, GuiWindowType.DMAdminTools), "non-admins cannot open the admin tools");

                // The admin layout, exported for the client check.
                await InContext(ctx, player.Creature, () => toolbar.ShowAdminButton(true));
                ctx.AssertEqual(DMToolbarViewModel.AdminToolbarWidth, toolbar.Geometry.Width, "the toolbar grows to fit the Admin button");
                Capture(player.Creature, GuiWindowType.DMToolbar, "toolbar-admin");
                await InContext(ctx, player.Creature, () => Gui.TogglePlayerWindow(player.Creature, GuiWindowType.DMAdminTools));
                Capture(player.Creature, GuiWindowType.DMAdminTools, "admin-tools");

                // /resetwindows closes other windows but keeps the toolbar, which stands in for the hidden native taskbar.
                var resetWindows = new CharacterChatCommand().BuildChatCommands()["resetwindows"];
                await InContext(ctx, player.Creature, () =>
                    resetWindows.DoAction(player.Creature, OBJECT_INVALID, GetLocation(player.Creature)));
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMToolbar), "/resetwindows keeps the DM toolbar open");
                ctx.Assert(!Gui.IsWindowOpen(player.Creature, GuiWindowType.CreatureManager), "/resetwindows still closes other windows");
            }
            finally
            {
                stats.m_bDMManifested = originalManifested;
                WriteSnapshots();
            }
        }

        /// <summary>
        /// Opens the Battle Grid through /battlegrid and Placeable Tools on a non-DM fixture (as an admin on their own
        /// character would) and drives the window: the placed flag and section count must match the anchors that
        /// really exist, a click past a limit must keep the drawn anchors, and a grid off the map is not reported.
        /// </summary>
        [EngineTest("Battle grid opens from /battlegrid and Placeable Tools and draws only what it reports", Category = "BattleGrid", TimeoutSeconds = 60f)]
        public static async Task BattleGridDrawsAndRemovesSections(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            using var windows = RegisterWindows(ctx, player);
            var area = GetArea(player.Creature);
            try
            {
                await InContext(ctx, player.Creature, () => BattleGrid.Remove(area));
                await InContext(ctx, player.Creature, () => BattleGrid.SetSettings(area, BattleGrid.DefaultSettings));

                var command = new DMChatCommand().BuildChatCommands()["battlegrid"];
                ctx.Assert(command.ValidateArguments == null, "/battlegrid adds no check beyond the DM and Admin permission");
                await InContext(ctx, player.Creature, () =>
                    command.DoAction(player.Creature, OBJECT_INVALID, GetLocation(player.Creature)));
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMBattleGrid), "/battlegrid opens the Battle Grid window");
                await InContext(ctx, player.Creature, () =>
                    command.DoAction(player.Creature, OBJECT_INVALID, GetLocation(player.Creature)));
                ctx.Assert(!Gui.IsWindowOpen(player.Creature, GuiWindowType.DMBattleGrid), "/battlegrid again closes it");

                await InContext(ctx, player.Creature, () => Gui.TogglePlayerWindow(player.Creature, GuiWindowType.DMTools));
                var tools = (DMToolsViewModel)windows.ViewModel(GuiWindowType.DMTools);
                await InContext(ctx, player.Creature, tools.OnOpenBattleGrid());
                ctx.Assert(Gui.IsWindowOpen(player.Creature, GuiWindowType.DMBattleGrid), "Placeable Tools opens the Battle Grid window");
                var grid = (DMBattleGridViewModel)windows.ViewModel(GuiWindowType.DMBattleGrid);
                ctx.Assert(!grid.IsPlaced, "no grid before one is placed");

                await InContext(ctx, player.Creature, grid.OnPlaceHere());
                ctx.AssertEqual(4, GridAnchors(area).Count, "the default grid draws 2 x 2 sections");
                ctx.Assert(grid.IsPlaced && BattleGrid.GetDrawnSections(area) == 4, "the window and service report the drawn grid");
                ctx.AssertEqual("Grid shown: 40 x 40 squares.", grid.StatusText, "status names the full grid");

                await InContext(ctx, player.Creature, grid.OnWidthUp());
                ctx.AssertEqual(6, GridAnchors(area).Count, "one wider redraws 3 x 2 sections");

                await InContext(ctx, player.Creature, grid.OnWidthDown());
                await InContext(ctx, player.Creature, grid.OnWidthDown());
                ctx.AssertEqual(2, GridAnchors(area).Count, "the narrowest grid is 1 x 2 sections");
                var atLimit = GridAnchors(area);
                await InContext(ctx, player.Creature, grid.OnWidthDown());
                ctx.Assert(atLimit.SequenceEqual(GridAnchors(area)), "a click past the limit keeps the drawn anchors");

                var before = BattleGrid.GetCenter(area);
                await InContext(ctx, player.Creature, grid.OnMoveEast());
                ctx.AssertEqual(before.X + BattleGrid.DefaultSettings.SquareSize, BattleGrid.GetCenter(area).X, "Move East shifts one square");
                ctx.AssertEqual(2, GridAnchors(area).Count, "moving redraws the same sections");

                // Place at Target centers on an object in this area, and ignores picks with no position here.
                var centerBeforePicks = BattleGrid.GetCenter(area);
                var anchorsBeforePicks = GridAnchors(area);
                var item = OBJECT_INVALID;
                await InContext(ctx, player.Creature, () => item = CreateItemOnObject("bag_sm", player.Creature));
                ctx.Assert(GetIsObjectValid(item), "the fixture holds an inventory item to pick");
                await InContext(ctx, player.Creature, grid.OnPlaceAtTarget());
                await InContext(ctx, player.Creature, () => PendingTargetAction(player.Creature)(item));
                ctx.AssertEqual(centerBeforePicks, BattleGrid.GetCenter(area), "picking an inventory item leaves the grid where it was");
                ctx.Assert(anchorsBeforePicks.SequenceEqual(GridAnchors(area)), "picking an inventory item does not redraw the grid");

                var marker = CreateObject(ObjectType.Placeable, "plc_invisobj", ctx.GetArenaLocation(6.3f, 2.1f));
                ctx.Track(marker);
                await InContext(ctx, player.Creature, grid.OnPlaceAtTarget());
                await InContext(ctx, player.Creature, () => PendingTargetAction(player.Creature)(marker));
                var expectedCenter = BattleGrid.SnapCenter(GetPosition(marker), BattleGrid.DefaultSettings.SquareSize);
                ctx.AssertEqual(expectedCenter, BattleGrid.GetCenter(area), "picking an object in the area centers the grid on it");

                // Far outside the map: whatever the engine creates, the reported grid matches it.
                var drawnOffMap = 0;
                await InContext(ctx, player.Creature, () => drawnOffMap = BattleGrid.Place(area, new System.Numerics.Vector3(-5000f, -5000f, 0f)));
                ctx.AssertEqual(drawnOffMap, GridAnchors(area).Count, "the drawn count matches the anchors after an off-map placement");
                ctx.AssertEqual(drawnOffMap > 0, BattleGrid.IsPlaced(area), "the area only counts as having a grid when one was drawn");
                ctx.Log($"Sections drawn at (-5000, -5000): {drawnOffMap} of 2");

                await InContext(ctx, player.Creature, grid.OnPlaceHere());
                await InContext(ctx, player.Creature, grid.OnRemove());
                ctx.AssertEqual(0, GridAnchors(area).Count, "Remove destroys every anchor");
                ctx.Assert(!grid.IsPlaced && !BattleGrid.IsPlaced(area), "Remove clears the placed state");
            }
            finally
            {
                await InContext(ctx, player.Creature, () => BattleGrid.Remove(area));
            }
        }

        // A headless fixture cannot click, so run the handler Place at Target registered for its next pick.
        private static Action<uint> PendingTargetAction(uint creature)
        {
            var actions = (System.Collections.IDictionary)typeof(Targeting)
                .GetField("_playerTargetingActions", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var detail = actions[creature] ?? throw new InvalidOperationException("No targeting action is pending.");
            return (Action<uint>)detail.GetType().GetProperty("SelectionAction")!.GetValue(detail)!;
        }

        private static List<uint> GridAnchors(uint area)
        {
            var anchors = new List<uint>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) == BattleGrid.AnchorTag)
                    anchors.Add(obj);
            }

            return anchors;
        }

        [EngineTest("DM chooser searches, jumps to, summons and destroys objects", Category = "DMToolbar", TimeoutSeconds = 90f)]
        public static async Task ChooserFindsAndActsOnObjects(EngineTestContext ctx)
        {
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx);
            var probe = ctx.SpawnCreature("nw_bandit001", 8f, 0f);
            SetName(probe, "Chooser Probe Rancor");
            SetTag(probe, "dm_chooser_probe");
            using var windows = RegisterWindows(ctx, dm);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                    Gui.TogglePlayerWindow(dm.Creature, GuiWindowType.DMChooser));
                await ctx.WaitFrameAsync();
                var model = (DMChooserViewModel)windows.ViewModel(GuiWindowType.DMChooser);

                // Areas list from the whole module and include the test arena.
                var arenaResref = GetResRef(ctx.Arena);
                await InContext(ctx, dm.Creature, () => model.SearchText = arenaResref);
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.Assert(GetChooserResults(model).Any(result => result.Object == ctx.Arena), $"area search for '{arenaResref}' lists the arena");
                ctx.Assert(ShowsPartial(model, GuiWindowType.DMChooser, DMChooserViewModel.MainContentElement, DMChooserViewModel.AreaContentPartial),
                    "areas use the list without the object columns");
                var chooserLayouts = Gui.GetWindowTemplate(GuiWindowType.DMChooser).PartialViewLayouts;
                foreach (var objectColumn in new[] { nameof(DMChooserViewModel.ResultAreas), nameof(DMChooserViewModel.ResultIcons) })
                {
                    ctx.Assert(!chooserLayouts[DMChooserViewModel.AreaContentPartial].Contains(objectColumn) &&
                               chooserLayouts[DMChooserViewModel.MainContentPartial].Contains(objectColumn),
                        $"only the object list has the {objectColumn} column, so area names get its width");
                }

                var arenaRow = GetChooserResults(model).FindIndex(result => result.Object == ctx.Arena);
                ctx.AssertEqual(string.Empty, model.ResultInfo[arenaRow], "an area with no players leaves the Players column empty");
                ctx.AssertEqual(DMObjectDetails.NoSelectionIcon, model.DetailIcon, "icon with nothing selected");
                ctx.AssertEqual(ctx.Arena, GetAreaFromLocation(DMObjectSearch.GetAreaJumpLocation(ctx.Arena)), "area jump location");
                Capture(dm.Creature, GuiWindowType.DMChooser, "chooser-areas");

                await InContext(ctx, dm.Creature, () => model.SelectedCategory = (int)DMObjectCategory.Creatures);
                ctx.Assert(ShowsPartial(model, GuiWindowType.DMChooser, DMChooserViewModel.MainContentElement, DMChooserViewModel.MainContentPartial),
                    "objects use the list with summary and area columns");
                await InContext(ctx, dm.Creature, () => model.SearchText = "probe RANCOR");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(1, model.ResultNames.Count, "creature search result count");
                ctx.AssertEqual("Chooser Probe Rancor", model.ResultNames[0], "creature search result");
                ctx.Assert(model.ResultInfo[0].StartsWith("Lv "), $"creature row shows level and HP (was '{model.ResultInfo[0]}')");

                await SelectRow(ctx, dm.Creature, model, 0);
                ctx.Assert(model.CanJumpTo && model.CanBringHere && model.CanDestroy, "creature actions enabled");
                ctx.Assert(model.SelectedDetails.Contains("Tag: dm_chooser_probe"), "details include the tag");
                ctx.Assert(model.SelectedDetails.Contains("HP "), "details include hit points");
                ctx.AssertEqual("Chooser Probe Rancor", model.DetailTitle, "details title");
                Capture(dm.Creature, GuiWindowType.DMChooser, "chooser-creature-selected");

                // The Chooser stays open across area changes, so a current-area list reruns when the DM changes areas.
                var lateProbe = ctx.SpawnCreature("nw_bandit001", 10f, 2f);
                SetName(lateProbe, "Chooser Probe Rancor Late");
                await InContext(ctx, dm.Creature, () =>
                    Gui.PublishRefreshEvent(dm.Creature, new DMAreaChangedRefreshEvent()));
                ctx.AssertEqual(2, model.ResultNames.Count, "an area change reruns a current-area search");
                DestroyObject(lateProbe);

                // Player characters are left to the player search.
                SetName(dm.Creature, "Chooser Excluded Player");
                await InContext(ctx, dm.Creature, () => model.SearchText = "Chooser Excluded Player");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(0, model.ResultNames.Count, "player characters are excluded from the creature list");

                // A search of every area reads a slice of objects per server tick instead of stalling one.
                var scan = DMObjectSearch.StartSearch(dm.Creature, DMObjectCategory.Placeables, DMObjectSearchScope.AllAreas, "zzqq nomatch");
                var slices = 0;
                var slowestSlice = 0L;
                var sliceWatch = new System.Diagnostics.Stopwatch();
                bool isScanComplete;
                do
                {
                    sliceWatch.Restart();
                    isScanComplete = scan.Continue(DMObjectSearch.ObjectsPerSlice);
                    slowestSlice = Math.Max(slowestSlice, sliceWatch.ElapsedMilliseconds);
                    slices++;
                } while (!isScanComplete);
                ctx.Log($"All-areas placeable search with no matches: {slices} slices, slowest {slowestSlice} ms.");
                ctx.Assert(slices > 1, "the module's placeables span more than one search slice");

                var placedProbe = CreateObject(ObjectType.Placeable, "plc_chair", ctx.GetArenaLocation(4f, 4f));
                SetTag(placedProbe, "dm_chooser_all_areas_probe");
                await InContext(ctx, dm.Creature, () => model.SelectedCategory = (int)DMObjectCategory.Placeables);
                await InContext(ctx, dm.Creature, () => model.SearchText = "dm_chooser_all_areas_probe");
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () => model.SelectedScope = (int)DMObjectSearchScope.AllAreas);
                ctx.AssertEqual("Searching every area...", model.ResultSummary, "an all-areas search reports progress");
                await ctx.WaitUntilAsync(() => model.ResultSummary != "Searching every area...", 10f, "the all-areas search to finish");
                ctx.AssertEqual(1, model.ResultNames.Count, "the all-areas search finds the placed probe");
                DestroyObject(placedProbe);
                await InContext(ctx, dm.Creature, () => model.SelectedScope = (int)DMObjectSearchScope.CurrentArea);
                await InContext(ctx, dm.Creature, () => model.SelectedCategory = (int)DMObjectCategory.Creatures);

                await InContext(ctx, dm.Creature, () => model.SearchText = "dm_chooser_probe");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                await SelectRow(ctx, dm.Creature, model, 0);
                await InContext(ctx, dm.Creature, model.OnClickJumpTo());
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, probe) < 2.5f, 5f, "the DM to jump to the creature");

                AssignCommand(dm.Creature, () => JumpToLocation(ctx.GetArenaLocation(-6f, 0f)));
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, probe) > 10f, 5f, "the DM to move away");
                await InContext(ctx, dm.Creature, model.OnClickBringHere());
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, probe) < 3f, 5f, "the creature to be brought to the DM");

                await InContext(ctx, dm.Creature, model.OnClickDestroy());
                await InContext(ctx, dm.Creature, model.OnModalConfirmClick());
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(probe), 5f, "the creature to be destroyed");
                await ctx.WaitUntilAsync(() => model.ResultNames.Count == 0, 5f, "the chooser to refresh after destroying");
            }
            finally
            {
                WriteSnapshots();
            }
        }

        [EngineTest("DM creator lists palette categories, shows details and creates objects", Category = "DMToolbar", TimeoutSeconds = 180f)]
        public static async Task CreatorListsAndCreatesBlueprints(EngineTestContext ctx)
        {
            // Custom palettes are read while the module loads, so no DM's click waits on hundreds of template reads.
            var loadedPalettes = (System.Collections.IDictionary)typeof(DMBlueprintCatalog)
                .GetField("_palettes", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            ctx.Assert(Enum.GetValues<DMBlueprintCategory>().All(category => loadedPalettes.Contains((category, DMBlueprintSource.Custom))),
                "every custom palette is loaded before a DM opens the Creator");

            using var dm = await PlayerAbilityFixture.CreateAsync(ctx);
            using var windows = RegisterWindows(ctx, dm);
            await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                Gui.TogglePlayerWindow(dm.Creature, GuiWindowType.DMCreator));
            await ctx.WaitFrameAsync();
            var model = (DMCreatorViewModel)windows.ViewModel(GuiWindowType.DMCreator);

            // The custom creature palette keeps the toolset's categories.
            var palette = DMBlueprintCatalog.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Custom);
            ctx.Log($"Custom creatures: {palette.Blueprints.Count} blueprints in {palette.CategoryPaths.Count} categories.");
            ctx.Assert(palette.CategoryPaths.Any(path => path.StartsWith("World Creatures" + DMPaletteReader.PathSeparator)),
                "custom creature palette has World Creatures sub-categories");
            var humans = palette.CategoryPaths.FirstOrDefault(path => path.EndsWith(DMPaletteReader.PathSeparator + "Humans"));
            ctx.Assert(humans != null, "custom creature palette has a Humans category");
            ctx.AssertEqual(palette.CategoryPaths.Count + 1, model.CategoryOptions.Count, "category choices");

            var humansOption = model.CategoryOptions.First(option => option.Label == humans);
            await InContext(ctx, dm.Creature, () => model.SelectedCategoryIndex = humansOption.Value);
            ctx.Assert(model.ResultNames.Count > 0, "the Humans category lists creatures");
            ctx.Assert(GetResults(model).All(blueprint => blueprint.IsInCategory(humans)), "every result is in the Humans category");
            Capture(dm.Creature, GuiWindowType.DMCreator, "creator-creature-category");

            await InContext(ctx, dm.Creature, () => model.SelectedCategoryIndex = 0);
            await InContext(ctx, dm.Creature, () => model.SearchText = "civilian");
            await InContext(ctx, dm.Creature, model.OnClickSearch());
            var row = IndexOfResref(model, "civilian");
            ctx.Assert(row >= 0, "searching 'civilian' lists the civilian blueprint");
            ctx.AssertEqual("Civilian", model.ResultNames[row], "custom creature name");
            await SelectRow(ctx, dm.Creature, model, row);
            ctx.Assert(model.HasSelection && !model.CanCreateInInventory, "creature create actions");
            ctx.Assert(model.DetailBody.Contains("Level ") && model.DetailBody.Contains("Faction:"),
                $"creature details show level and faction (was '{model.DetailBody}')");
            ctx.Assert(model.DetailBody.Contains("Resref: civilian"), "creature details keep the resref");
            Capture(dm.Creature, GuiWindowType.DMCreator, "creator-creature-selected");

            var before = CountNear(dm.Creature, ObjectType.Creature, "civilian");
            await InContext(ctx, dm.Creature, model.OnClickCreateHere());
            await ctx.WaitUntilAsync(() => CountNear(dm.Creature, ObjectType.Creature, "civilian") == before + 1, 5f, "a civilian to be created at the DM");
            var created = FindNear(dm.Creature, ObjectType.Creature, "civilian");
            ctx.Track(created);
            ctx.AssertEqual("x2_def_ondeath", GetEventScript(created, EventScript.Creature_OnDeath), "DM spawn scripts applied");

            // Spawn preparation gives an unarmed plot creature claws; they must be undroppable like its other items.
            await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureRight, created)) &&
                                           GetIsObjectValid(GetItemInSlot(InventorySlot.CreatureLeft, created)), 5f, "the civilian to equip its claws");
            ctx.Assert(!GetDroppableFlag(GetItemInSlot(InventorySlot.CreatureRight, created)) &&
                       !GetDroppableFlag(GetItemInSlot(InventorySlot.CreatureLeft, created)),
                "claws added by spawn preparation are undroppable");

            // A creature with a stat skin shows the skin's stats.
            var skinned = palette.Blueprints.FirstOrDefault(blueprint =>
                blueprint.CategoryPath.StartsWith("World Creatures") &&
                DMObjectDetails.DescribeBlueprint(blueprint).Body.Contains("Attack "));
            ctx.Assert(skinned != null, "a World Creatures blueprint shows stat skin combat stats");
            ctx.Log($"{skinned.Resref}: {DMObjectDetails.DescribeBlueprint(skinned).Body.Replace("\n", " | ")}");
            var skinnedOption = model.CategoryOptions.First(option => option.Label == skinned.CategoryPath);
            await InContext(ctx, dm.Creature, () => model.SearchText = skinned.Resref);
            await InContext(ctx, dm.Creature, () => model.SelectedCategoryIndex = skinnedOption.Value);
            row = IndexOfResref(model, skinned.Resref);
            ctx.Assert(row >= 0, "the skinned creature is listed in its category");
            ctx.Assert(model.ResultInfo[row].StartsWith("Lv "), $"creature rows show level (was '{model.ResultInfo[row]}')");
            await SelectRow(ctx, dm.Creature, model, row);
            Capture(dm.Creature, GuiWindowType.DMCreator, "creator-world-creature-selected");

            await InContext(ctx, dm.Creature, () => model.SelectedSource = (int)DMBlueprintSource.Standard);
            await InContext(ctx, dm.Creature, () => model.SearchText = "nw_bandit001");
            await InContext(ctx, dm.Creature, model.OnClickSearch());
            row = IndexOfResref(model, "nw_bandit001");
            ctx.Assert(row >= 0, "the standard palette lists nw_bandit001");
            ctx.Assert(model.ResultNames[row] != "nw_bandit001", $"base game creature name resolves from the TLK (was '{model.ResultNames[row]}')");

            await InContext(ctx, dm.Creature, () => model.SelectedType = (int)DMBlueprintCategory.Placeables);
            await InContext(ctx, dm.Creature, () => model.SearchText = "plc_chair");
            await InContext(ctx, dm.Creature, model.OnClickSearch());
            row = IndexOfResref(model, "plc_chair");
            ctx.Assert(row >= 0, "the standard placeable palette lists plc_chair");
            await SelectRow(ctx, dm.Creature, model, row);
            ctx.Assert(model.DetailBody.Contains("Appearance:"), $"placeable details show the appearance (was '{model.DetailBody}')");
            await InContext(ctx, dm.Creature, model.OnClickCreateHere());
            await ctx.WaitUntilAsync(() => GetIsObjectValid(FindNear(dm.Creature, ObjectType.Placeable, "plc_chair")), 5f, "a chair to be created");
            var chair = FindNear(dm.Creature, ObjectType.Placeable, "plc_chair");
            ctx.Track(chair);
            ctx.AssertEqual(1, GetLocalInt(chair, "DM_SPAWNED_PLACEABLE"), "chair recorded as DM-spawned");
            ctx.AssertEqual(GetObjectUUID(dm.Creature), GetLocalString(chair, "DM_SPAWNED_BY"), "chair records its DM");

            await InContext(ctx, dm.Creature, () => model.SelectedSource = (int)DMBlueprintSource.Custom);
            await InContext(ctx, dm.Creature, () => model.SelectedType = (int)DMBlueprintCategory.Items);
            await InContext(ctx, dm.Creature, () => model.SearchText = "militia helmet");
            await InContext(ctx, dm.Creature, model.OnClickSearch());
            row = IndexOfResref(model, "001");
            ctx.Assert(row >= 0, "item search 'militia helmet' lists blueprint 001");
            ctx.AssertEqual("Tatooine Militia Field Helmet", model.ResultNames[row], "custom item name from the palette");
            ctx.Assert(!string.IsNullOrEmpty(model.ResultIcons[row]), "the item row shows its inventory icon");
            await SelectRow(ctx, dm.Creature, model, row);
            ctx.Assert(model.CanCreateInInventory, "items can be created in the DM's inventory");
            ctx.Assert(model.DetailBody.Contains("Weight ") && model.DetailBody.Contains("Value "),
                $"item details show weight and value (was '{model.DetailBody}')");
            ctx.Assert(!string.IsNullOrEmpty(model.DetailIcon), "the item details show its icon");
            Capture(dm.Creature, GuiWindowType.DMCreator, "creator-item-selected");
            await InContext(ctx, dm.Creature, model.OnClickCreateInInventory());
            await ctx.WaitUntilAsync(() => GetIsObjectValid(FindInventoryItem(dm.Creature, "001")), 5f, "the helmet to be created in the DM's inventory");
            DestroyObject(FindInventoryItem(dm.Creature, "001"));

            // Stores and container items take a created item into their inventory, like creatures and container
            // placeables. CreateItemOnObject only works inside a script context, as the Creator's targeting event is.
            var helmet = DMBlueprintCatalog.GetPalette(DMBlueprintCategory.Items, DMBlueprintSource.Custom)
                .Blueprints.First(blueprint => blueprint.Resref == "001");
            var bag = CreateObject(ObjectType.Item, "bag_sm", GetLocation(dm.Creature));
            ctx.Track(bag);
            var store = CreateObject(ObjectType.Store, "nw_storemagic001", GetLocation(dm.Creature));
            ctx.Track(store);
            ctx.Assert(new[] { bag, store, dm.Creature }.All(DMBlueprintCatalog.CanReceiveItems),
                "creatures, stores and container items receive created items");
            await InContext(ctx, dm.Creature, () =>
            {
                DMBlueprintCatalog.CreateInInventory(dm.Creature, helmet, bag);
                DMBlueprintCatalog.CreateInInventory(dm.Creature, helmet, store);
            });
            ctx.Assert(GetIsObjectValid(FindInventoryItem(bag, "001")), "an item is created in a container item's inventory");
            ctx.Assert(GetIsObjectValid(FindInventoryItem(store, "001")), "an item is created in a store's inventory");

            // Triggers are listed and described like the other blueprint types.
            await InContext(ctx, dm.Creature, () => model.SelectedType = (int)DMBlueprintCategory.Triggers);
            await InContext(ctx, dm.Creature, () => model.SearchText = "anti_spawn");
            await InContext(ctx, dm.Creature, model.OnClickSearch());
            row = IndexOfResref(model, "anti_spawn_trigg");
            ctx.Assert(row >= 0, "the custom trigger palette lists anti_spawn_trigg");
            await SelectRow(ctx, dm.Creature, model, row);
            ctx.Assert(model.DetailBody.Contains("Created as a 4 m square"), $"trigger details describe the shape (was '{model.DetailBody}')");
            Capture(dm.Creature, GuiWindowType.DMCreator, "creator-trigger-selected");
            WriteSnapshots();
        }

        [EngineTest("DM creator creates triggers and encounters as squares players can enter", Category = "DMToolbar", TimeoutSeconds = 90f)]
        public static async Task CreatorCreatesTriggersAndEncounters(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            SetPlotFlag(player.Creature, true);
            var area = GetArea(player.Creature);
            var existingCreatures = new HashSet<uint>();
            for (var obj = GetFirstObjectInArea(area, ObjectType.Creature); GetIsObjectValid(obj); obj = GetNextObjectInArea(area, ObjectType.Creature))
            {
                existingCreatures.Add(obj);
            }

            try
            {
                var trigger = DMBlueprintCatalog.GetPalette(DMBlueprintCategory.Triggers, DMBlueprintSource.Custom)
                    .Blueprints.Single(blueprint => blueprint.Resref == "anti_spawn_trigg");
                ctx.Assert(DMObjectDetails.DescribeBlueprint(trigger).Body.Contains("Created as a 4 m square"),
                    "trigger details say what shape is created");

                var triggerLocation = ctx.GetArenaLocation(6f, 0f);
                var createdTrigger = DMBlueprintCatalog.CreateAtLocation(player.Creature, trigger, triggerLocation);
                ctx.Track(createdTrigger);
                ctx.AssertEqual(ObjectType.Trigger, GetObjectType(createdTrigger), "a trigger is created");
                var centre = GetPositionFromLocation(triggerLocation);
                var corners = ReadGeometry(ObjectPlugin.GetTriggerGeometry(createdTrigger));
                ctx.Log($"Trigger corners around ({centre.X:0.##}, {centre.Y:0.##}): {string.Join(" ", corners.Select(corner => $"({corner.X:0.##}, {corner.Y:0.##})"))}");
                ctx.Assert(corners.Count == 4 &&
                           Math.Abs(corners.Min(corner => corner.X) - (centre.X - 2f)) < 0.01f &&
                           Math.Abs(corners.Max(corner => corner.X) - (centre.X + 2f)) < 0.01f &&
                           Math.Abs(corners.Min(corner => corner.Y) - (centre.Y - 2f)) < 0.01f &&
                           Math.Abs(corners.Max(corner => corner.Y) - (centre.Y + 2f)) < 0.01f,
                    "the trigger is a 4 m square around the location");

                AssignCommand(player.Creature, () => JumpToLocation(triggerLocation));
                await ctx.WaitUntilAsync(() => IsInside(createdTrigger, player.Creature), 5f, "the player to register inside the trigger");

                // A weak encounter, so it spawns for a level 1 player.
                var encounter = DMBlueprintCatalog.GetPalette(DMBlueprintCategory.Encounters, DMBlueprintSource.Standard)
                    .Blueprints.First(blueprint => blueprint.CategoryPath.EndsWith("Very Easy"));
                ctx.Log($"Encounter {encounter.Resref}: {DMObjectDetails.DescribeBlueprint(encounter).Body.Replace("\n", " | ")}");
                var encounterLocation = ctx.GetArenaLocation(-6f, 0f);
                var createdEncounter = DMBlueprintCatalog.CreateAtLocation(player.Creature, encounter, encounterLocation);
                ctx.Track(createdEncounter);
                ctx.AssertEqual(ObjectType.Encounter, GetObjectType(createdEncounter), "an encounter is created");

                AssignCommand(player.Creature, () => JumpToLocation(encounterLocation));
                // GetEncounterSpawnsCurrent stays 0 here, so look for the spawned creature inside the square instead.
                await ctx.WaitUntilAsync(() => HasSpawnInside(createdEncounter, player.Creature, existingCreatures), 20f,
                    "the encounter to spawn a creature inside its square when the player enters it");

            }
            finally
            {
                for (var obj = GetFirstObjectInArea(area, ObjectType.Creature); GetIsObjectValid(obj); obj = GetNextObjectInArea(area, ObjectType.Creature))
                {
                    if (!existingCreatures.Contains(obj) && !GetIsPC(obj))
                        DestroyObject(obj);
                }
            }
        }

        // NWNX reports trigger corners as "{x, y, z}{x, y, z}...".
        private static List<(float X, float Y)> ReadGeometry(string geometry)
        {
            return geometry
                .Split('}', StringSplitOptions.RemoveEmptyEntries)
                .Select(vertex => vertex.TrimStart('{').Split(','))
                .Where(parts => parts.Length >= 2)
                .Select(parts => (
                    float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)))
                .ToList();
        }

        private static bool HasSpawnInside(uint encounter, uint player, HashSet<uint> existingCreatures)
        {
            for (var obj = GetFirstInPersistentObject(encounter, ObjectType.Creature);
                 GetIsObjectValid(obj);
                 obj = GetNextInPersistentObject(encounter, ObjectType.Creature))
            {
                if (obj != player && !existingCreatures.Contains(obj))
                    return true;
            }

            return false;
        }

        private static bool IsInside(uint persistentObject, uint creature)
        {
            for (var obj = GetFirstInPersistentObject(persistentObject, ObjectType.Creature);
                 GetIsObjectValid(obj);
                 obj = GetNextInPersistentObject(persistentObject, ObjectType.Creature))
            {
                if (obj == creature)
                    return true;
            }

            return false;
        }

        private static List<DMObjectSearchResult> GetChooserResults(DMChooserViewModel model)
        {
            return (List<DMObjectSearchResult>)typeof(DMChooserViewModel)
                .GetField("_pageResults", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(model)!;
        }

        // Whether the window's group element currently holds the named partial view.
        private static bool ShowsPartial(IGuiViewModel model, GuiWindowType type, string elementId, string partialName)
        {
            var groupLayouts = (Dictionary<string, string>)model.GetType().BaseType!
                .GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(model)!;
            return groupLayouts.TryGetValue(elementId, out var layout) &&
                   layout == Gui.GetWindowTemplate(type).PartialViewLayouts[partialName];
        }

        private static List<DMBlueprint> GetResults(DMCreatorViewModel model)
        {
            return (List<DMBlueprint>)typeof(DMCreatorViewModel)
                .GetField("_results", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(model)!;
        }

        private static int IndexOfResref(DMCreatorViewModel model, string resref)
        {
            var page = (List<DMBlueprint>)typeof(DMCreatorViewModel)
                .GetField("_pageResults", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(model)!;
            return page.FindIndex(blueprint => string.Equals(blueprint.Resref, resref, StringComparison.OrdinalIgnoreCase));
        }

        [EngineTest("DM player search message reaches its player after a stale chat portrait click", Category = "DMToolbar", TimeoutSeconds = 90f)]
        public static async Task PlayerSearchMessageIgnoresStalePortraitClick(EngineTestContext ctx)
        {
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx);
            var first = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            PlayerAbilityFixture second = null;
            using var windows = RegisterWindows(ctx, dm);
            try
            {
                var sharedSlot = ChatPortraitTellEngineTests.GetSlot(first.Creature);
                await ChatPortraitTellEngineTests.SendClientTellAsync(ctx, first.Creature, ChatPortraitTellEngineTests.GetSlot(dm.Creature), "first line");
                first.Dispose();
                second = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
                second.Update(record => record.Name = "Portrait Probe Holder");
                SetName(second.Creature, "Portrait Probe Holder");
                ctx.AssertEqual(sharedSlot, ChatPortraitTellEngineTests.GetSlot(second.Creature), "the second player takes the freed slot");

                // The DM clicks the departed speaker's portrait and leaves the refused tell unsent.
                await ChatPortraitTellEngineTests.SendClientPortraitClickAsync(ctx, dm.Creature, second.Creature);

                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                    Gui.TogglePlayerWindow(dm.Creature, GuiWindowType.DMPlayerSearch));
                await ctx.WaitFrameAsync();
                var model = (DMPlayerSearchViewModel)windows.ViewModel(GuiWindowType.DMPlayerSearch);
                await InContext(ctx, dm.Creature, () => model.SearchText = "Portrait Probe Holder");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(1, model.ResultNames.Count, "the new slot holder is found");
                await SelectRow(ctx, dm.Creature, model, 0);
                await InContext(ctx, dm.Creature, model.OnClickSendMessage());
                await InContext(ctx, dm.Creature, () =>
                {
                    model.ModalInputText = "staff message";
                    model.OnInputModalConfirmClick()();
                });

                ctx.AssertEqual(1L, ChatPortraitTells.CountLinesReceived(second.Creature), "the Player Search message reaches the selected player");
                // The unsent portrait tell is still refused afterwards.
                ctx.Assert(ChatPortraitTells.RefuseStalePortraitTell(dm.Creature, second.Creature, false), "the stale portrait tell");
            }
            finally
            {
                first.Dispose();
                second?.Dispose();
            }
        }

        [EngineTest("DM player search finds online and offline characters and moves players", Category = "DMToolbar", TimeoutSeconds = 90f)]
        public static async Task PlayerSearchFindsOnlineAndOfflineCharacters(EngineTestContext ctx)
        {
            using var dm = await PlayerAbilityFixture.CreateAsync(ctx);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
            target.Update(record =>
            {
                record.Name = "Vexa Zyndari";
                record.UnknownDisplayName = "A lanky Twi'lek";
            });
            SetName(target.Creature, "Vexa Zyndari");
            using var second = await PlayerAbilityFixture.CreateAsync(ctx, 9f);
            second.Update(record => record.Name = "Morn Kesh");
            SetName(second.Creature, "Morn Kesh");

            var offlineId = Guid.NewGuid().ToString();
            var savedAreaResref = Area.GetAreas().Keys.First();
            DB.Set(new Player(offlineId)
            {
                Name = "Qorin Zyndari",
                UnknownDisplayName = "A weathered Zabrak",
                CDKey = "QZTEST01",
                AccountName = "qorinaccount",
                LocationAreaResref = savedAreaResref,
                LocationX = 10f,
                LocationY = 10f,
                LastLoginDate = new DateTime(2026, 10, 1, 18, 30, 0, DateTimeKind.Utc)
            });

            // An offline character whose active disguise shows a different descriptor.
            var disguisedId = Guid.NewGuid().ToString();
            var disguise = new PlayerDisguise { PlayerId = disguisedId, Descriptor = "A hooded smuggler" };
            DB.Set(disguise);
            DB.Set(new Player(disguisedId)
            {
                Name = "Talen Orsk",
                UnknownDisplayName = "A tall Human",
                ActiveDisguiseId = disguise.Id
            });

            // More offline matches than the search shows.
            var bulkIds = Enumerable.Range(0, DMPlayerDirectory.MaxResults + 1)
                .Select(_ => Guid.NewGuid().ToString())
                .ToList();
            foreach (var bulkId in bulkIds)
            {
                DB.Set(new Player(bulkId) { Name = "Bulkprobe Character" });
            }

            // Shares the common name word, but its account name is unique.
            var bulkTargetId = Guid.NewGuid().ToString();
            DB.Set(new Player(bulkTargetId) { Name = "Bulkprobe Target", AccountName = "uniqbulkaccount" });

            using var windows = RegisterWindows(ctx, dm);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(dm.Creature, () =>
                    Gui.TogglePlayerWindow(dm.Creature, GuiWindowType.DMPlayerSearch));
                await ctx.WaitFrameAsync();
                var model = (DMPlayerSearchViewModel)windows.ViewModel(GuiWindowType.DMPlayerSearch);
                ctx.Assert(model.ResultNames.Contains("Vexa Zyndari"), "online characters are listed when the window opens");

                await InContext(ctx, dm.Creature, () => PlayerList.SetFlag(target.Creature, PlayerListFlagType.GeneralRP, true));
                await InContext(ctx, dm.Creature, () => model.SearchText = "lanky");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(1, model.ResultNames.Count, "descriptor search result count");
                ctx.AssertEqual("Vexa Zyndari", model.ResultNames[0], "online descriptor search");
                ctx.AssertEqual("Online", model.ResultStatuses[0], "online status");
                await SelectRow(ctx, dm.Creature, model, 0);
                ctx.Assert(model.SelectedDetails.Contains($"Player ID: {target.Id}"), "details include the player ID");
                ctx.Assert(model.SelectedDetails.Contains("Status: Online\n"), "details show an active player as Online");
                ctx.Assert(model.SelectedDetails.Contains("Party: None"), "details show a solo player's party");
                ctx.Assert(model.SelectedDetails.Contains("Flags: General RP"), "details list the player's flags");
                ctx.Assert(model.IsSelectedOnline, "online actions enabled");
                Capture(dm.Creature, GuiWindowType.DMPlayerSearch, "players-online-selected");

                await InContext(ctx, dm.Creature, model.OnClickJumpTo());
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, target.Creature) < 2.5f, 5f, "the DM to jump to the player");
                AssignCommand(dm.Creature, () => JumpToLocation(ctx.GetArenaLocation(-6f, 0f)));
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, target.Creature) > 10f, 5f, "the DM to move away");
                await InContext(ctx, dm.Creature, model.OnClickJumpHere());
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, target.Creature) < 3f, 5f, "the player to be brought to the DM");

                // Selections gathered from separate searches bring the whole group at once.
                ctx.AssertEqual("Bring Selected Here (1)", model.BringSelectedText, "one online character is selected");
                await InContext(ctx, dm.Creature, () => model.SearchText = "Morn Kesh");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(1, model.ResultNames.Count, "second character search result count");
                ctx.Assert(!model.ResultSelections[0], "the second character starts unselected");
                ctx.Assert(model.IsSelectedOnline, "the first selection survives a new search");
                await SelectRow(ctx, dm.Creature, model, 0);
                ctx.AssertEqual("Bring Selected Here (2)", model.BringSelectedText, "selections persist across searches");
                ctx.Assert(model.ResultSelections[0] && model.SelectionSummary == "2 characters selected.", "both characters are selected");
                ctx.Assert(!model.CanJumpTo && !model.IsSelectedOnline, "single-character actions wait for one selection");
                ctx.AssertEqual("Select only one character to use this.", model.SingleActionReason, "the disabled actions say why");
                ctx.Assert(model.SelectedDetails.StartsWith("2 characters selected:") && model.SelectedDetails.Contains("Morn Kesh"),
                    "the details list the selected characters");
                Capture(dm.Creature, GuiWindowType.DMPlayerSearch, "players-multi-selected");

                await SelectRow(ctx, dm.Creature, model, 0);
                ctx.Assert(!model.ResultSelections[0] && model.IsSelectedOnline, "selecting a selected row again deselects it");
                await SelectRow(ctx, dm.Creature, model, 0);
                ctx.AssertEqual("Bring Selected Here (2)", model.BringSelectedText, "the row is selected again");

                AssignCommand(target.Creature, () =>
                {
                    ClearAllActions();
                    JumpToLocation(ctx.GetArenaLocation(-8f, 0f));
                });
                AssignCommand(second.Creature, () =>
                {
                    ClearAllActions();
                    JumpToLocation(ctx.GetArenaLocation(-6f, 0f));
                });
                AssignCommand(dm.Creature, () =>
                {
                    ClearAllActions();
                    JumpToLocation(ctx.GetArenaLocation(6f, 0f));
                });
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, target.Creature) > 10f, 10f, "the DM to move away from the first player");
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, second.Creature) > 10f, 10f, "the DM to move away from the second player");
                await InContext(ctx, dm.Creature, model.OnClickBringSelected());
                await ctx.WaitUntilAsync(() => GetDistanceBetween(dm.Creature, target.Creature) < 3f &&
                                               GetDistanceBetween(dm.Creature, second.Creature) < 3f, 5f, "both selected players to be brought to the DM");
                ctx.Assert(model.HasSelection, "selections stay after bringing the group, so it can be moved again");

                await InContext(ctx, dm.Creature, model.OnClickClearSelection());
                ctx.Assert(!model.HasSelection && !model.ResultSelections[0], "Clear Selection deselects everyone");

                await SelectRow(ctx, dm.Creature, model, 0);
                await InContext(ctx, dm.Creature, () => Gui.ClosePlayerWindow(dm.Creature, GuiWindowType.DMPlayerSearch));
                await InContext(ctx, dm.Creature, () => Gui.TogglePlayerWindow(dm.Creature, GuiWindowType.DMPlayerSearch));
                model = (DMPlayerSearchViewModel)windows.ViewModel(GuiWindowType.DMPlayerSearch);
                ctx.Assert(!model.HasSelection && model.ResultSelections.All(isSelected => !isSelected),
                    "reopening Player Search starts with nothing selected");

                await InContext(ctx, dm.Creature, () => model.SearchText = "lanky");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                await SelectRow(ctx, dm.Creature, model, 0);
                await InContext(ctx, dm.Creature, model.OnClickExamine());
                ctx.Assert(Gui.IsWindowOpen(dm.Creature, GuiWindowType.DMPlayerExamine), "Examine opens the DM examine window");
                Gui.ClosePlayerWindow(dm.Creature, GuiWindowType.DMPlayerExamine);

                await InContext(ctx, dm.Creature, () => model.SelectedScope = (int)DMPlayerSearchScope.Online);
                await InContext(ctx, dm.Creature, () => model.SearchText = "Qorin");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(0, model.ResultNames.Count, "offline characters are not in the online scope");

                await InContext(ctx, dm.Creature, () => model.SelectedScope = (int)DMPlayerSearchScope.Offline);
                await InContext(ctx, dm.Creature, () => model.SearchText = string.Empty);
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.Assert(model.ResultSummary.StartsWith("Enter a name"), "offline scope asks for search text");

                // Terms may come from different fields and in any order, as with online characters.
                foreach (var search in new[] { "Qorin", "qorinacc", "QZTEST01", "weathered zab", offlineId, offlineId.ToUpperInvariant(), "qorinacc Qorin", "Zyndari QZTEST01" })
                {
                    await InContext(ctx, dm.Creature, () => model.SearchText = search);
                    await InContext(ctx, dm.Creature, model.OnClickSearch());
                    ctx.Assert(model.ResultNames.Contains("Qorin Zyndari"), $"offline search '{search}' finds the character");
                }

                await InContext(ctx, dm.Creature, () => model.SearchText = "Qorin nomatchxyz");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.Assert(!model.ResultNames.Contains("Qorin Zyndari"), "every term must match an offline character");

                await InContext(ctx, dm.Creature, () => model.SearchText = "hooded smug");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.Assert(model.ResultNames.Contains("Talen Orsk"), "offline search finds an active disguise's descriptor");

                ctx.Assert(DMPlayerDirectory.Search("\\@ Qorin", DMPlayerSearchScope.Offline, out _).Any(result => result.Name == "Qorin Zyndari"),
                    "a word the index cannot query does not stop the other words from finding offline characters");

                var bulk = DMPlayerDirectory.Search("Bulkprobe", DMPlayerSearchScope.Offline, out var bulkTruncated);
                ctx.AssertEqual(DMPlayerDirectory.MaxResults, bulk.Count, "offline search returns up to the result limit");
                ctx.Assert(bulkTruncated, "offline search reports matches beyond the result limit");

                ctx.AssertEqual(0, DMPlayerDirectory.Search(", . *", DMPlayerSearchScope.All, out _).Count,
                    "a search of only punctuation matches nobody, online or offline");
                ctx.AssertEqual(0, DMPlayerDirectory.Search("\\@", DMPlayerSearchScope.All, out _).Count,
                    "a search whose only word cannot be queried matches nobody, online or offline");

                var crossField = DMPlayerDirectory.Search("Bulkprobe uniqbulk", DMPlayerSearchScope.Offline, out var crossFieldTruncated);
                ctx.Assert(crossField.Any(result => result.Name == "Bulkprobe Target") && !crossFieldTruncated,
                    "a common word plus a rare word in another field finds the character");

                // Online and offline characters both match the start of a word, as the index does.
                await InContext(ctx, dm.Creature, () => model.SelectedScope = (int)DMPlayerSearchScope.All);
                await InContext(ctx, dm.Creature, () => model.SearchText = "ynd");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.AssertEqual(0, model.ResultNames.Count, "the middle of a word matches neither online nor offline characters");
                await InContext(ctx, dm.Creature, () => model.SearchText = "zynd");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                ctx.Assert(model.ResultNames.Contains("Vexa Zyndari") && model.ResultNames.Contains("Qorin Zyndari"),
                    "the start of a word matches online and offline characters");

                await InContext(ctx, dm.Creature, () => model.SearchText = "Zyndari");
                await InContext(ctx, dm.Creature, model.OnClickSearch());
                var offlineRow = model.ResultNames.IndexOf("Qorin Zyndari");
                ctx.Assert(offlineRow >= 0 && model.ResultNames.Contains("Vexa Zyndari"), "the combined scope lists online and offline matches");
                ctx.Assert(model.ResultNames.IndexOf("Vexa Zyndari") < offlineRow, "online characters are listed first");
                await InContext(ctx, dm.Creature, model.OnClickClearSelection());
                await SelectRow(ctx, dm.Creature, model, offlineRow);
                ctx.Assert(!model.IsSelectedOnline && model.CanJumpTo, "offline characters only allow Jump To");
                ctx.Assert(!model.CanBringSelected, "an offline selection cannot be brought");
                ctx.AssertEqual("The character is offline.", model.SingleActionReason, "online-only actions say why");
                ctx.Assert(model.SelectedDetails.Contains("Account: qorinaccount") && model.SelectedDetails.Contains("CD Key: QZTEST01"),
                    "offline details show the recorded account and CD key");
                ctx.Assert(model.SelectedDetails.Contains("Last Login: 2026-10-01 18:30 UTC"), "offline details show the last login");
                ctx.Assert(!model.SelectedDetails.Contains("Party:") && !model.SelectedDetails.Contains("Flags:"),
                    "offline details leave out the live party and flags");
                ctx.Assert(DMPlayerDirectory.TryGetLastLocation(offlineId, out var lastLocation) &&
                           GetAreaFromLocation(lastLocation) == Area.GetAreaByResref(savedAreaResref),
                    "offline Jump To resolves the saved area");
                Capture(dm.Creature, GuiWindowType.DMPlayerSearch, "players-offline-selected");
            }
            finally
            {
                DB.Delete<Player>(offlineId);
                DB.Delete<Player>(disguisedId);
                DB.Delete<PlayerDisguise>(disguise.Id);
                foreach (var bulkId in bulkIds)
                {
                    DB.Delete<Player>(bulkId);
                }

                DB.Delete<Player>(bulkTargetId);

                WriteSnapshots();
            }
        }

        private static async Task InContext(EngineTestContext ctx, uint creature, Action action)
        {
            await ctx.ExecuteInCreatureContextAsync(creature, action);
            await ctx.WaitFrameAsync();
        }

        // A list click reports its row through NuiGetEventArrayIndex, which only exists inside a
        // real NUI event, so select the row through the view model's own selection method.
        private static Task SelectRow(EngineTestContext ctx, uint creature, object model, int row)
        {
            var method = model.GetType().GetMethod("SelectResult", BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new InvalidOperationException($"{model.GetType().Name} has no SelectResult method.");
            return InContext(ctx, creature, () => method.Invoke(model, new object[] { row }));
        }

        private static uint FindInventoryItem(uint creature, string resref)
        {
            for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            {
                if (GetResRef(item) == resref)
                    return item;
            }

            return OBJECT_INVALID;
        }

        private static int CountNear(uint dm, ObjectType type, string resref)
        {
            var count = 0;
            for (var obj = GetFirstObjectInArea(GetArea(dm), type); GetIsObjectValid(obj); obj = GetNextObjectInArea(GetArea(dm), type))
            {
                if (obj != dm && GetResRef(obj) == resref && GetDistanceBetween(dm, obj) < 3f)
                    count++;
            }

            return count;
        }

        private static uint FindNear(uint dm, ObjectType type, string resref)
        {
            for (var obj = GetFirstObjectInArea(GetArea(dm), type); GetIsObjectValid(obj); obj = GetNextObjectInArea(GetArea(dm), type))
            {
                if (obj != dm && GetResRef(obj) == resref && GetDistanceBetween(dm, obj) < 3f)
                    return obj;
            }

            return OBJECT_INVALID;
        }

        // Headless fixtures do not pass through OnModuleEnter, so register this player's windows
        // directly. Native watch events then route like a connected player's UI.
        private static WindowRegistration RegisterWindows(EngineTestContext ctx, PlayerAbilityFixture player)
        {
            var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
                .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            ctx.Assert(!windows.ContainsKey(player.Id), "fixture GUI registration must be isolated");
            var registered = DMWindowTypes.ToDictionary(type => type, type =>
            {
                // Gui.CreatePlayerWindows gives every window its starting geometry; do the same here.
                var template = Gui.GetWindowTemplate(type);
                var playerWindow = template.CreatePlayerWindowAction();
                var initial = template.InitialGeometry;
                playerWindow.ViewModel.Geometry = new GuiRectangle(initial.X, initial.Y, initial.Width, initial.Height);
                return playerWindow;
            });
            windows.Add(player.Id, registered);
            return new WindowRegistration(player.Creature, player.Id, windows, registered);
        }

        private sealed class WindowRegistration : IDisposable
        {
            private readonly uint _creature;
            private readonly string _playerId;
            private readonly Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> _allWindows;
            private readonly Dictionary<GuiWindowType, GuiPlayerWindow> _windows;

            public WindowRegistration(uint creature, string playerId,
                Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>> allWindows,
                Dictionary<GuiWindowType, GuiPlayerWindow> windows)
            {
                _creature = creature;
                _playerId = playerId;
                _allWindows = allWindows;
                _windows = windows;
            }

            public IGuiViewModel ViewModel(GuiWindowType type) => _windows[type].ViewModel;

            public void Dispose()
            {
                foreach (var type in DMWindowTypes)
                    Gui.ClosePlayerWindow(_creature, type);
                _allWindows.Remove(_playerId);
            }
        }

        // Records exactly what the server sent: the window, the composed root layout and every bind value.
        private static void Capture(uint player, GuiWindowType type, string name)
        {
            var window = Gui.GetPlayerWindow(player, type);
            var token = window.WindowToken;
            var template = Gui.GetWindowTemplate(type);
            var baseType = window.ViewModel.GetType().BaseType!;
            var rootPartial = (string)baseType.GetField("_rootPartial", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(window.ViewModel);
            var groupLayouts = (Dictionary<string, string>)baseType.GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(window.ViewModel)!;
            var root = GuiLayoutComposer.Compose(template.PartialViewLayouts[rootPartial], groupLayouts);

            var binds = new Dictionary<string, JsonElement>();
            foreach (var watched in new[] { false, true })
            {
                for (var index = 0; ; index++)
                {
                    var bind = NuiGetNthBind(player, token, watched, index);
                    if (string.IsNullOrEmpty(bind))
                        break;
                    using var json = JsonDocument.Parse(JsonDump(NuiGetBind(player, token, bind)));
                    binds[bind] = json.RootElement.Clone();
                }
            }

            using var windowJson = JsonDocument.Parse(JsonDump(template.Window));
            using var rootJson = JsonDocument.Parse(root);
            Snapshots.Add(new
            {
                name,
                windowId = template.WindowId,
                window = windowJson.RootElement.Clone(),
                root = rootJson.RootElement.Clone(),
                binds
            });
        }

        private static void WriteSnapshots()
        {
            var directory = ApplicationSettings.Get().EngineTestResultsDirectory;
            if (string.IsNullOrWhiteSpace(directory))
                directory = "./engine_tests/";
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, SnapshotFileName),
                JsonSerializer.Serialize(Snapshots, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
