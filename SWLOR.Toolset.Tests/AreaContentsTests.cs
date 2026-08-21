using System.Numerics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Shell.Views;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The Area Contents panel, over a working copy of a real area.
    /// </summary>
    /// <remarks>
    /// veles_exterior is the area the design was measured against and the reason the tree groups the
    /// way it does: 1,599 placeables, 179 blueprints, and one blueprint placed 648 times under 108
    /// different names. Anything that quietly re-keys the grouping onto the blueprint will fail
    /// <see cref="NameGrouping_KeepsDifferentlyNamedPlacementsOfOneBlueprintApart"/>.
    /// </remarks>
    public class AreaContentsTests
    {
        private const string AreaResRef = "veles_exterior";

        /// <summary>The blueprint veles_exterior reuses as a generic host for unrelated scenery.</summary>
        private const string ReusedBlueprint = "_mdrn_pl_carpt04";

        private string _moduleRoot = string.Empty;

        [SetUp]
        public void CreateWorkingModule()
        {
            _moduleRoot = Path.Combine(Path.GetTempPath(), "swlor-area-contents-" + Guid.NewGuid().ToString("N"));
            foreach (var folder in CorpusLocator.GffFolders)
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));

            foreach (var folder in new[] { "are", "git", "gic" })
            {
                var source = Path.Combine(CorpusLocator.ModuleDirectory, folder, $"{AreaResRef}.{folder}.json");
                if (!File.Exists(source))
                    continue;

                File.Copy(source, Path.Combine(_moduleRoot, folder, Path.GetFileName(source)));
            }
        }

        [TearDown]
        public void RemoveWorkingModule()
        {
            if (Directory.Exists(_moduleRoot))
                Directory.Delete(_moduleRoot, recursive: true);
        }

        private AreaEditorViewModel CreateEditor(
            IEditorPromptService? prompts = null,
            Func<ResourceType, string, string?>? editCopyBlueprint = null,
            ModuleMutationLock? mutationLock = null)
        {
            var log = new OutputLogService();
            return new AreaEditorViewModel(
                AreaResRef,
                new ModuleWorkspace(_moduleRoot),
                new LookupOptionProvider(new WorkspaceContext(_ => throw new NotSupportedException(), log)),
                gameCodeIndex: null,
                log,
                prompts: prompts ?? new StubPrompts(),
                editCopyBlueprint: editCopyBlueprint,
                mutationLock: mutationLock);
        }

        private static AreaContentsViewModel CreatePanel(
            AreaEditorViewModel editor, AreaContentsGrouping grouping = AreaContentsGrouping.Name)
        {
            var panel = new AreaContentsViewModel(new StubPrompts());
            panel.SetEditor(editor);
            panel.SelectedGrouping = panel.GroupingOptions.Single(option => option.Value == grouping);
            return panel;
        }

        private static AreaContentsNodeViewModel KindNode(AreaContentsViewModel panel, string title) =>
            panel.Rows.Single(row => row.Kind == AreaContentsNodeKind.Kind && row.Name == title);

        // ----- grouping -----

        [Test]
        public void Placeables_GroupedByName_AreFarFewerRowsThanInstances()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            var placeables = KindNode(panel, "Placeables");
            var instances = editor.SectionFor(ResourceType.Utp)!.Rows.Count;

            instances.Should().BeGreaterThan(1000, "this test is only meaningful on a busy area");
            placeables.Children.Count.Should().BeLessThan(
                instances / 4,
                "grouping exists so the branch is readable - a row per instance is what it replaces");
        }

        [Test]
        public void ViewportEditCopyRoutesTheSelectedBlueprintWithoutChangingTheInstance()
        {
            (ResourceType Type, string ResRef)? request = null;
            var mutationLock = new ModuleMutationLock();
            var editor = CreateEditor(
                editCopyBlueprint: (type, resRef) =>
                {
                    request = (type, resRef);
                    return "source_plc001";
                },
                mutationLock: mutationLock);
            var marker = new InstanceMarker
            {
                Kind = InstanceMarkerKind.Placeable,
                TemplateResRef = "source_plc",
                Tag = "placed_instance",
                Position = Vector3.Zero,
                Orientation = Vector2.UnitX
            };
            editor.SelectSceneInstance(marker);

            editor.EditCopySelectedBlueprintCommand.CanExecute(null).Should().BeTrue();
            editor.EditCopySelectedBlueprintCommand.Execute(null);

            request.Should().Be((ResourceType.Utp, "source_plc"));
            marker.TemplateResRef.Should().Be("source_plc",
                "Edit Copy creates a blueprint and must not retarget the placed instance");
            editor.SceneStatus.Should().Contain("source_plc001");

            mutationLock.Set(true);
            editor.EditCopySelectedBlueprintCommand.CanExecute(null).Should().BeFalse();
        }

        [Test]
        public void NameGrouping_KeepsDifferentlyNamedPlacementsOfOneBlueprintApart()
        {
            var editor = CreateEditor();
            var section = editor.SectionFor(ResourceType.Utp)!;

            var namesOfTheReusedBlueprint = section.Rows
                .Where(row => row.TemplateResRef == ReusedBlueprint)
                .Select(row => editor.ResolveInstanceName(ResourceType.Utp, row))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            namesOfTheReusedBlueprint.Count.Should().BeGreaterThan(
                1,
                "the whole point of grouping by name is that one blueprint is placed as several different objects");

            var panel = CreatePanel(editor, AreaContentsGrouping.Name);
            var placeables = KindNode(panel, "Placeables");

            // Every name the reused blueprint appears under must be its own row. Under blueprint
            // keying they would all be filed under the blueprint's own name instead - roads and
            // lightposts inside a row that says "Rug".
            foreach (var name in namesOfTheReusedBlueprint)
            {
                placeables.Children.Should().Contain(
                    child => child.Name == name,
                    "'{0}' is a distinct object in this area, not a copy of whatever {1} is called",
                    name, ReusedBlueprint);
            }
        }

        [Test]
        public void BlueprintGrouping_CollapsesTheReusedBlueprintIntoOneRow()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor, AreaContentsGrouping.Blueprint);

            var placeables = KindNode(panel, "Placeables");
            var reused = placeables.Children.Single(child => child.Name == ReusedBlueprint);

            reused.Indices.Count.Should().BeGreaterThan(
                100,
                "grouping by blueprint answers 'what would editing this blueprint touch', so every placement belongs to it");
        }

        [Test]
        public void SomethingPlacedOnce_IsALeafRatherThanAGroupOfOne()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            var singletons = panel.Rows
                .Where(row => row.Depth == 1 && row.Kind == AreaContentsNodeKind.Instance)
                .ToList();

            singletons.Should().NotBeEmpty("a real area always has objects placed exactly once");
            singletons.Should().OnlyContain(
                row => row.Indices.Count == 1 && row.Children.Count == 0,
                "a group of one is a click that buys nothing");
        }

        [Test]
        public void EmptyKinds_StillAppear()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            // veles_exterior has no doors, no triggers and no encounters. "None here" is an answer,
            // and a kind that vanished would read as a filter having hidden it.
            KindNode(panel, "Doors").Detail.Should().Be("0");
        }

        // ----- filtering -----

        [Test]
        public void Filter_MatchesNameResRefAndTag()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            panel.Filter = ReusedBlueprint;

            var matched = panel.Rows
                .Where(row => row.Kind == AreaContentsNodeKind.Kind)
                .Sum(row => CountUnder(row));

            matched.Should().BeGreaterThan(0, "the filter must reach the resref, not only the name");
            panel.StatusMessage.Should().Contain("of", "the status line reports matched of total");
        }

        [Test]
        public void Filter_ThatMatchesNothing_LeavesEveryKindEmpty()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            panel.Filter = "no-object-is-called-this-zzz";

            panel.Rows.Should().OnlyContain(
                row => row.Kind == AreaContentsNodeKind.Kind,
                "with nothing matching, only the kind headings remain");
        }

        // ----- what a row does -----

        [Test]
        public void Opening_AnInstance_SelectsItAndAsksForTheCamera()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            Vector3? focused = null;
            editor.CameraFocusRequested += position => focused = position;

            var creatures = KindNode(panel, "Creatures");
            var row = FirstInstanceUnder(creatures);
            var expected = editor.SectionFor(ResourceType.Utc)!.Rows[row.Indices[0]];

            panel.OpenCommand.Execute(row);

            focused.Should().NotBeNull("double-clicking a row is how the camera is sent to an object");
            focused!.Value.X.Should().BeApproximately(expected.X, 0.001f);
            focused.Value.Y.Should().BeApproximately(expected.Y, 0.001f);
            editor.SectionFor(ResourceType.Utc)!.SelectedRow.Should().BeSameAs(expected);
        }

        [Test]
        public void Selecting_AnInstance_SelectsItWithoutMovingTheCamera()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);

            var moved = false;
            editor.CameraFocusRequested += _ => moved = true;

            panel.SelectedRow = FirstInstanceUnder(KindNode(panel, "Creatures"));

            moved.Should().BeFalse(
                "a single click picks the object out; flying the camera on every arrow-key step would be unusable");
        }

        [Test]
        public void OpeningProperties_FromContents_RetainsTheSelectedKindAndPropertiesPage()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);
            var row = FirstInstanceUnder(KindNode(panel, "Creatures"));
            var section = editor.SectionFor(ResourceType.Utc)!;
            InstanceListSectionViewModel? requestedSection = null;
            editor.InstancePropertiesRequested += value => requestedSection = value;

            panel.OpenPropertiesCommand.Execute(row);

            editor.SelectedRootTabIndex.Should().Be(1);
            section.IsExpanded.Should().BeTrue();
            section.SelectedRow.Should().BeSameAs(section.Rows[row.Indices[0]]);
            requestedSection.Should().BeSameAs(
                section,
                "the view must bring the requested editor into view after switching pages");
        }

        [Test]
        public void OpeningProperties_FromAGroup_OpensItsFirstPlacement()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor, AreaContentsGrouping.Blueprint);
            var group = KindNode(panel, "Placeables").Children
                .First(child => child.Kind == AreaContentsNodeKind.Group);
            var section = editor.SectionFor(ResourceType.Utp)!;
            InstanceListSectionViewModel? requestedSection = null;
            editor.InstancePropertiesRequested += value => requestedSection = value;

            panel.OpenPropertiesCommand.Execute(group);

            section.SelectedRow.Should().BeSameAs(section.Rows[group.Indices[0]]);
            requestedSection.Should().BeSameAs(section);
        }

        [Test]
        public void RevealingPlacement_VerifiesIdentityBeforeTrustingAStaleIndex()
        {
            var editor = CreateEditor();
            var section = editor.SectionFor(ResourceType.Utp)!;
            var sameBlueprint = section.Rows
                .Where(row => row.TemplateResRef == ReusedBlueprint)
                .Take(2)
                .ToList();
            sameBlueprint.Should().HaveCount(2);
            var staleIndexRow = sameBlueprint[0];
            var expected = sameBlueprint[1];
            var placement = new ObjectPlacement(
                ResourceType.Utp,
                expected.TemplateResRef,
                AreaResRef,
                staleIndexRow.Index,
                expected.Tag,
                expected.X,
                expected.Y,
                expected.Z);

            editor.RevealPlacement(placement);

            section.SelectedRow.Should().BeSameAs(expected);
        }

        [Test]
        public void RevealingPlacement_BeforePanelsAttach_RetainsCameraAndContentsNavigation()
        {
            var editor = CreateEditor();
            var expected = editor.SectionFor(ResourceType.Utp)!.Rows[0];
            var placement = PlacementFor(expected);

            editor.RevealPlacement(placement);

            editor.TryTakePendingCameraFocus(out var cameraTarget).Should().BeTrue(
                "a cold area has no view to consume the Go To camera request yet");
            cameraTarget.Should().Be(new Vector3(expected.X, expected.Y, expected.Z));

            var panel = CreatePanel(editor);
            panel.SelectedRow.Should().NotBeNull();
            panel.SelectedRow!.Kind.Should().Be(AreaContentsNodeKind.Instance);
            panel.SelectedRow.Indices.Should().Equal(expected.Index);
            panel.Rows.Should().Contain(panel.SelectedRow,
                "the selected placement must be in the expanded visible tree");
            panel.TryTakePendingRowReveal(out var scrollTarget).Should().BeTrue();
            scrollTarget.Should().BeSameAs(panel.SelectedRow);
        }

        [AvaloniaTest]
        public void RevealingPlacement_ScrollsToAnInstancePastTheLargeGroupCap()
        {
            var editor = CreateEditor();
            var section = editor.SectionFor(ResourceType.Utp)!;
            var copies = section.Rows
                .Where(row => row.TemplateResRef == ReusedBlueprint)
                .ToList();
            copies.Should().HaveCountGreaterThan(200,
                "the regression needs a target that the normal group cap does not realise");
            var expected = copies[^1];
            var panel = CreatePanel(editor, AreaContentsGrouping.Blueprint);
            panel.Filter = "no-object-is-called-this-zzz";

            var view = new AreaContentsView { DataContext = panel };
            var window = new Window { Content = view, Width = 360, Height = 180 };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            try
            {
                editor.RevealPlacement(PlacementFor(expected));
                Dispatcher.UIThread.RunJobs();
                Dispatcher.UIThread.RunJobs();

                panel.Filter.Should().BeEmpty("Go To must remove a filter that hides its target");
                panel.SelectedRow.Should().NotBeNull();
                panel.SelectedRow!.Indices.Should().Equal(expected.Index);
                panel.Rows.Should().Contain(panel.SelectedRow,
                    "the exact capped placement must be realised and its ancestors expanded");

                var list = view.FindControl<ListBox>("RowsList")!;
                list.SelectedItem.Should().BeSameAs(panel.SelectedRow);
                var scroller = list.GetVisualDescendants().OfType<ScrollViewer>().Single();
                scroller.Offset.Y.Should().BeGreaterThan(0,
                    "the selected row is beyond the first screen and Go To must move the scrollbar to it");
            }
            finally
            {
                window.Close();
                Dispatcher.UIThread.RunJobs();
            }
        }

        [Test]
        public void RevealingPlacement_DoesNotSelectAnotherInstanceWhenTheSourceWasDeleted()
        {
            var editor = CreateEditor();
            var section = editor.SectionFor(ResourceType.Utp)!;
            var sameBlueprint = section.Rows
                .Where(row => row.TemplateResRef == ReusedBlueprint)
                .Take(2)
                .ToList();
            sameBlueprint.Should().HaveCount(2);
            var deleted = sameBlueprint[0];
            var placement = new ObjectPlacement(
                ResourceType.Utp,
                deleted.TemplateResRef,
                AreaResRef,
                deleted.Index,
                deleted.Tag,
                deleted.X,
                deleted.Y,
                deleted.Z);
            section.DeleteInstances(new[] { deleted.Index }).Should().BeTrue();
            section.SelectedRow = null;
            var cameraMoved = false;
            editor.CameraFocusRequested += _ => cameraMoved = true;

            editor.RevealPlacement(placement);

            section.SelectedRow.Should().BeNull();
            cameraMoved.Should().BeFalse(
                "a stale Source row must not silently navigate to another copy of the blueprint");
        }

        // ----- deleting -----

        [Test]
        public void Deleting_AGroup_RemovesEveryMemberAndNothingElse()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor, AreaContentsGrouping.Blueprint);
            var section = editor.SectionFor(ResourceType.Utp)!;

            var before = section.Rows.Count;
            var group = KindNode(panel, "Placeables").Children
                .First(child => child.Kind == AreaContentsNodeKind.Group);
            var doomed = group.Indices.Count;
            var survivor = section.Rows
                .First(row => !group.Indices.Contains(row.Index));
            var survivorResRef = survivor.TemplateResRef;
            var survivorX = survivor.X;

            editor.DeleteInstances(ResourceType.Utp, group.Indices).Should().BeTrue();

            section.Rows.Count.Should().Be(before - doomed);
            section.Rows.Should().Contain(
                row => row.TemplateResRef == survivorResRef && Math.Abs(row.X - survivorX) < 0.001f,
                "removing a group must not take its neighbours with it - which is what deleting " +
                "ascending indices does, because every index after the first has already shifted");
        }

        [Test]
        public void Deleting_AGroup_IsOneUndoEntry()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor, AreaContentsGrouping.Blueprint);
            var section = editor.SectionFor(ResourceType.Utp)!;

            var before = section.Rows.Count;
            var group = KindNode(panel, "Placeables").Children
                .First(child => child.Kind == AreaContentsNodeKind.Group && child.Indices.Count > 1);

            editor.DeleteInstances(ResourceType.Utp, group.Indices).Should().BeTrue();
            section.Rows.Count.Should().BeLessThan(before);

            editor.UndoInstancesCommand.Execute(null);

            section.Rows.Count.Should().Be(
                before,
                "one delete of a group must come back in one undo, not one undo per object");
        }

        [Test]
        public void Deleting_TheMapSelection_RemovesThatOneObject()
        {
            var editor = CreateEditor();
            var section = editor.SectionFor(ResourceType.Utc)!;
            var before = section.Rows.Count;

            editor.DeleteInstances(ResourceType.Utc, new[] { 0 }).Should().BeTrue();

            section.Rows.Count.Should().Be(before - 1);
        }

        [Test]
        public void DeleteSelected_WithNothingSelected_DoesNothing()
        {
            var editor = CreateEditor();

            editor.DeleteSelectedSceneInstance().Should().BeFalse(
                "Delete has to fall through when the map has no selection, or it swallows the key");
        }

        // ----- following the front tab -----

        [Test]
        public void PointingThePanelAtNothing_ClearsTheTree()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor);
            panel.Rows.Should().NotBeEmpty();

            panel.SetEditor(null);

            panel.Rows.Should().BeEmpty();
            panel.HasArea.Should().BeFalse();
            panel.AreaResRef.Should().BeEmpty();
        }

        [Test]
        public void AnEditAfterBinding_RebuildsTheTree()
        {
            var editor = CreateEditor();
            var panel = CreatePanel(editor, AreaContentsGrouping.Flat);

            var before = KindNode(panel, "Creatures").Detail;
            editor.DeleteInstances(ResourceType.Utc, new[] { 0 });

            KindNode(panel, "Creatures").Detail.Should().NotBe(
                before, "the panel listens for content changes rather than being refreshed by hand");
        }

        [Test]
        public async Task SavingAGitOnlyEditPublishesTheAreaCatalogChange()
        {
            var editor = CreateEditor();
            var section = editor.SectionFor(ResourceType.Utp)!;
            section.SelectedRow = section.Rows[0];
            var notifications = 0;
            var placementNotifications = 0;
            editor.CatalogEntryChanged += () => notifications++;
            editor.PlacementsChanged += () => placementNotifications++;

            section.DetailTag = "git_only_catalog_refresh";

            (await editor.TrySaveAsync()).Should().BeTrue();
            notifications.Should().Be(1,
                "placed-instance tags and script slots are indexed from GIT, not ARE");
            placementNotifications.Should().Be(1,
                "a saved GIT changes the module placement index");
            new GitDocument(JsonGffDocument.Load(
                    Path.Combine(_moduleRoot, "git", $"{AreaResRef}.git.json")))
                .Placeables[0].GetStringOrNull("Tag")
                .Should().Be("git_only_catalog_refresh");
        }

        [Test]
        public async Task SavingAnAreOnlyEditDoesNotPublishAPlacementChange()
        {
            var editor = CreateEditor();
            var comments = editor.AreaPropertyGroups
                .SelectMany(group => group.Fields)
                .OfType<TextFieldViewModel>()
                .Single(field => field.Descriptor.FieldName == "Comments");
            var catalogNotifications = 0;
            var placementNotifications = 0;
            editor.CatalogEntryChanged += () => catalogNotifications++;
            editor.PlacementsChanged += () => placementNotifications++;

            comments.Text = "ARE metadata only";

            (await editor.TrySaveAsync()).Should().BeTrue();
            catalogNotifications.Should().Be(1);
            placementNotifications.Should().Be(0,
                "ARE metadata is not part of the GIT placement index");
        }

        [Test]
        public async Task ReloadingAnExternalGitChangePublishesTheAreaCatalogChange()
        {
            var editor = CreateEditor(new ReloadPrompts());
            var section = editor.SectionFor(ResourceType.Utp)!;
            section.SelectedRow = section.Rows[0];
            var diskTag = section.Rows[0].Tag;
            section.DetailTag = "local_unsaved_tag";

            var gitPath = Path.Combine(_moduleRoot, "git", $"{AreaResRef}.git.json");
            File.SetLastWriteTimeUtc(
                gitPath,
                File.GetLastWriteTimeUtc(gitPath).AddSeconds(2));

            var notifications = 0;
            var placementNotifications = 0;
            editor.CatalogEntryChanged += () => notifications++;
            editor.PlacementsChanged += () => placementNotifications++;

            (await editor.TrySaveAsync()).Should().BeTrue();

            notifications.Should().Be(1);
            placementNotifications.Should().Be(1,
                "reloading the paired GIT changes the placement snapshot");
            editor.SectionFor(ResourceType.Utp)!.Rows[0].Tag.Should().Be(diskTag);
        }

        [Test]
        public async Task SavingGitRechecksTheCleanGicPartnerUnderTheCommitLease()
        {
            var gicPath = Path.Combine(_moduleRoot, "gic", $"{AreaResRef}.gic.json");
            var racingGeneration = File.ReadAllBytes(gicPath);
            racingGeneration = racingGeneration.Concat(Encoding.UTF8.GetBytes(" ")).ToArray();
            var prompts = new PairSaveRacePrompts(_moduleRoot, gicPath, racingGeneration);
            var editor = CreateEditor(prompts);
            var section = editor.SectionFor(ResourceType.Utp)!;
            section.SelectedRow = section.Rows[0];
            section.DetailTag = "pair_race_local_edit";

            var gitPath = Path.Combine(_moduleRoot, "git", $"{AreaResRef}.git.json");
            File.SetLastWriteTimeUtc(gitPath, File.GetLastWriteTimeUtc(gitPath).AddSeconds(2));

            (await editor.TrySaveAsync()).Should().BeFalse(
                "a clean GIC changed after planning must not be paired with the stale edited GIT");
            await prompts.WriterFinished.WaitAsync(TimeSpan.FromSeconds(5));
            File.ReadAllBytes(gicPath).Should().Equal(racingGeneration);
            editor.IsDirty.Should().BeTrue();
        }

        [Test]
        public async Task FailedEarlyAreReloadDiscardsAlreadyStagedCompanionWrites()
        {
            var editor = CreateEditor(new DeleteBeforeReloadPrompts());
            var comments = editor.AreaPropertyGroups
                .SelectMany(group => group.Fields)
                .OfType<TextFieldViewModel>()
                .Single(field => field.Descriptor.FieldName == "Comments");
            comments.Text = "local ARE edit";

            var section = editor.SectionFor(ResourceType.Utp)!;
            section.SelectedRow = section.Rows[0];
            section.DetailTag = "local GIT edit";

            var arePath = Path.Combine(_moduleRoot, "are", $"{AreaResRef}.are.json");
            File.SetLastWriteTimeUtc(
                arePath,
                File.GetLastWriteTimeUtc(arePath).AddSeconds(2));

            (await editor.TrySaveAsync()).Should().BeFalse();

            Directory.EnumerateFiles(_moduleRoot, "*.tmp", SearchOption.AllDirectories)
                .Should().BeEmpty("a failed reload must dispose every staged companion write");
        }

        private static int CountUnder(AreaContentsNodeViewModel kind) =>
            kind.Children.Sum(child => child.Kind == AreaContentsNodeKind.Group ? child.Indices.Count : 1);

        private static AreaContentsNodeViewModel FirstInstanceUnder(AreaContentsNodeViewModel kind)
        {
            foreach (var child in kind.Children)
            {
                if (child.Kind == AreaContentsNodeKind.Instance)
                    return child;

                var nested = child.Children.FirstOrDefault(g => g.Kind == AreaContentsNodeKind.Instance);
                if (nested != null)
                    return nested;
            }

            throw new InvalidOperationException($"No placement under '{kind.Name}'.");
        }

        private static ObjectPlacement PlacementFor(InstanceRow row) => new(
            ResourceType.Utp,
            row.TemplateResRef,
            AreaResRef,
            row.Index,
            row.Tag,
            row.X,
            row.Y,
            row.Z);

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(true);
        }

        private sealed class ReloadPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Reload);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(true);
        }

        private sealed class PairSaveRacePrompts(
            string moduleRoot,
            string gicPath,
            byte[] racingGeneration) : IEditorPromptService
        {
            private readonly TaskCompletionSource<bool> _writerStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private Task? _writer;

            public Task WriterFinished => _writer ?? Task.CompletedTask;

            public async Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path)
            {
                if (_writer == null)
                {
                    using (ExecutionContext.SuppressFlow())
                    {
                        _writer = Task.Run(async () =>
                        {
                            using var moduleWriteLock = ModuleWriteLock.Acquire(moduleRoot);
                            _writerStarted.TrySetResult(true);
                            await Task.Delay(500).ConfigureAwait(false);
                            File.WriteAllBytes(gicPath, racingGeneration);
                        });
                    }
                }

                await _writerStarted.Task.ConfigureAwait(false);
                return ExternalChangeChoice.Overwrite;
            }

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(true);
        }

        private sealed class DeleteBeforeReloadPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path)
            {
                File.Delete(path);
                return Task.FromResult(ExternalChangeChoice.Reload);
            }

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(true);
        }
    }

    /// <summary>
    /// <see cref="InstanceFieldMap.GetDisplayName"/> against the real corpus, one list shape at a
    /// time. The field a placement keeps its name in differs per list, and getting it wrong is
    /// silent: every object simply falls back to its blueprint's name.
    /// </summary>
    public class InstanceDisplayNameTests
    {
        private static JsonGffStruct FirstElement(string areaResRef, string listField)
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "git", $"{areaResRef}.git.json");
            var document = JsonGffDocument.Parse(File.ReadAllBytes(path));
            return document.Root.Get(listField).Elements!.First();
        }

        [Test]
        public void Creature_JoinsFirstAndLastName()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "utc", "osk.utc.json");
            var blueprint = JsonGffDocument.Parse(File.ReadAllBytes(path));

            InstanceFieldMap.GetDisplayName(ResourceType.Utc, blueprint.Root)
                .Should().Be("Osk Moh'roli", "a creature's name is split across two fields");
        }

        [Test]
        public void Placeable_ReadsLocName()
        {
            var element = FirstElement("veles_exterior", "Placeable List");
            var name = InstanceFieldMap.GetDisplayName(ResourceType.Utp, element);

            name.Should().NotBeNullOrWhiteSpace(
                "placeables in this module carry their own LocName, which is what the tree groups on");
        }

        [Test]
        public void Waypoint_ReadsLocalizedName()
        {
            var element = FirstElement("veles_exterior", "WaypointList");

            InstanceFieldMap.GetDisplayName(ResourceType.Utw, element)
                .Should().NotBeNullOrWhiteSpace("waypoints use LocalizedName, not LocName");
        }

        [Test]
        public void APlacementWithNoNameOfItsOwn_ReturnsNull()
        {
            var element = JsonGffField.CreateStruct(0).Struct!;

            InstanceFieldMap.GetDisplayName(ResourceType.Utp, element).Should().BeNull(
                "null is what tells the caller to fall back to the blueprint's name");
        }
    }
}
