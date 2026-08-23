using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Merchants;
using SWLOR.Toolset.Editors.Sources;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Generation changes around EditorService's module-wide background indexes.</summary>
    [TestFixture]
    public class EditorServiceIndexTests
    {
        private readonly List<string> _roots = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var root in _roots)
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void ReplacingAWorkspaceDuringTheItemSourceBuildStartsANewBuild()
        {
            var firstRoot = NewModuleRoot();
            var secondRoot = NewModuleRoot();
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            using var firstStarted = new ManualResetEventSlim();
            using var releaseFirst = new ManualResetEventSlim();
            var builtRoots = new List<string>();
            var gate = new object();

            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                itemSourcesBuilder: (module, _) =>
                {
                    int buildNumber;
                    lock (gate)
                    {
                        builtRoots.Add(module.ModuleRoot);
                        buildNumber = builtRoots.Count;
                    }

                    if (buildNumber == 1)
                    {
                        firstStarted.Set();
                        releaseFirst.Wait(TimeSpan.FromSeconds(5));
                    }

                    return ItemObtainabilityIndex.Build(module, gameSourceRoot: null);
                });

            workspace.Open(firstRoot);
            var firstCatalogBuild = workspace.Catalog!.BuildTask;
            var firstTagBuild = workspace.Workspace!.TagIndex.GetTransitionDestinationTagsAsync();
            firstStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            workspace.Open(secondRoot);
            var secondCatalogBuild = workspace.Catalog!.BuildTask;
            var secondTagBuild = workspace.Workspace!.TagIndex.GetTransitionDestinationTagsAsync();
            releaseFirst.Set();

            SpinWait.SpinUntil(
                    () =>
                    {
                        lock (gate)
                            return builtRoots.Count >= 2;
                    },
                    TimeSpan.FromSeconds(5))
                .Should().BeTrue("the obsolete in-flight task must retry after releasing its shared slot");

            editors.WarmItemSourcesAsync().GetAwaiter().GetResult();
            Task.WaitAll(firstCatalogBuild, firstTagBuild, secondCatalogBuild, secondTagBuild);

            lock (gate)
                builtRoots.Should().Equal(firstRoot, secondRoot);
        }

        [Test]
        public void FailedItemSourceBuildRetriesWithoutAnotherWorkspaceAction()
        {
            var moduleRoot = NewModuleRoot();
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var attempts = 0;

            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                itemSourcesBuilder: (module, _) =>
                {
                    if (Interlocked.Increment(ref attempts) == 1)
                        throw new IOException("transient scan failure");

                    return ItemObtainabilityIndex.Build(module, gameSourceRoot: null);
                });

            workspace.Open(moduleRoot);

            SpinWait.SpinUntil(
                    () => Volatile.Read(ref attempts) >= 2,
                    TimeSpan.FromSeconds(5))
                .Should().BeTrue("the failed background scan should queue its own retry");
            editors.WarmItemSourcesAsync().GetAwaiter().GetResult();

            attempts.Should().Be(2);
            log.Lines.Should().Contain(line => line.Contains("transient scan failure"));
        }

        [Test]
        public async Task PersistentItemSourceFailuresStopUntilContentIsInvalidated()
        {
            var moduleRoot = NewModuleRoot();
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var attempts = 0;

            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                itemSourcesBuilder: (_, _) =>
                {
                    Interlocked.Increment(ref attempts);
                    throw new IOException("persistent scan failure");
                });

            workspace.Open(moduleRoot);
            SpinWait.SpinUntil(
                    () => Volatile.Read(ref attempts) >= 2,
                    TimeSpan.FromSeconds(5))
                .Should().BeTrue();

            await Task.Delay(750);
            await editors.WarmItemSourcesAsync();
            attempts.Should().Be(2, "one automatic retry must not become a permanent scan loop");

            workspace.RefreshCatalogEntry(ResourceType.Uti, "changed_item");
            SpinWait.SpinUntil(
                    () => Volatile.Read(ref attempts) >= 4,
                    TimeSpan.FromSeconds(5))
                .Should().BeTrue("a content invalidation should permit a fresh attempt and its one retry");

            await Task.Delay(750);
            attempts.Should().Be(4);
        }

        [Test]
        public async Task ReplacingAWorkspaceDuringPlacementLookupRetriesAgainstTheReplacement()
        {
            var firstRoot = NewModuleRoot();
            var secondRoot = NewModuleRoot();
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var firstStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var expected = new ObjectPlacement(
                ResourceType.Utw,
                "arrival_wp",
                "replacement_area",
                0,
                "ARRIVAL",
                1f,
                2f,
                3f);
            var queriedRoots = new List<string>();
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                objectPlacementsFinder: async (module, _, _) =>
                {
                    queriedRoots.Add(module.ModuleRoot);
                    if (module.ModuleRoot == firstRoot)
                    {
                        firstStarted.SetResult();
                        await releaseFirst.Task;
                        return Array.Empty<ObjectPlacement>();
                    }

                    return new[] { expected };
                });
            workspace.Open(firstRoot);

            var method = typeof(EditorService).GetMethod(
                "FindObjectPlacementsAsync",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var lookup = (Task<IReadOnlyList<ObjectPlacement>>)method.Invoke(
                editors,
                new object[] { ResourceType.Utw, "arrival_wp" })!;
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            workspace.Open(secondRoot);
            releaseFirst.SetResult();

            var placements = await lookup.WaitAsync(TimeSpan.FromSeconds(5));

            placements.Should().ContainSingle().Which.Should().BeSameAs(expected);
            queriedRoots.Should().Equal(firstRoot, secondRoot);
            log.Lines.Should().Contain(line => line.Contains("Retrying placement scan"));
        }

        [Test]
        public async Task ReplacingAWorkspaceDuringAreaDocumentLoadingDoesNotOpenTheObsoleteArea()
        {
            const string areaResRef = "bank";
            var firstRoot = NewModuleRoot();
            var secondRoot = NewModuleRoot();
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.Copy(
                    Path.Combine(
                        CorpusLocator.ModuleDirectory,
                        extension,
                        $"{areaResRef}.{extension}.json"),
                    Path.Combine(firstRoot, extension, $"{areaResRef}.{extension}.json"));
            }

            var firstWorkspace = new ModuleWorkspace(firstRoot);
            var secondWorkspace = new ModuleWorkspace(secondRoot);
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(
                root => root == firstRoot ? firstWorkspace : secondWorkspace,
                log);
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                areaDocumentsLoader: (arePath, gitPath, gicPath) =>
                {
                    loadStarted.Set();
                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("The area-load race test was not released.");

                    return AreaEditorDocumentLoad.Load(arePath, gitPath, gicPath);
                });

            workspace.Open(firstRoot);
            var firstCatalog = workspace.Catalog!.BuildTask;
            await firstWorkspace.TagIndex.GetTransitionDestinationTagsAsync();
            editors.TryOpenEditor(ResourceType.Area, areaResRef);
            loadStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            workspace.Open(secondRoot);
            var secondCatalog = workspace.Catalog!.BuildTask;
            releaseLoad.Set();

            var openingAreas = (HashSet<string>)typeof(EditorService)
                .GetField("_openingAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            await WaitUntilAsync(() => !openingAreas.Contains(areaResRef));
            await Task.WhenAll(firstCatalog, secondCatalog);

            var openAreas = (Dictionary<string, AreaEditorViewModel>)typeof(EditorService)
                .GetField("_openAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            openAreas.Should().NotContainKey(areaResRef,
                "an area parsed for the previous module must not be published into the replacement workspace");
            log.Lines.Should().NotContain(line => line.Contains("Failed to open area editor"));
        }

        [Test]
        public async Task ReplacingAWorkspaceForTheSameModuleRetriesAreaDocumentLoading()
        {
            const string areaResRef = "bank";
            var moduleRoot = NewModuleRoot();
            var differentRoot = NewModuleRoot();
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.Copy(
                    Path.Combine(
                        CorpusLocator.ModuleDirectory,
                        extension,
                        $"{areaResRef}.{extension}.json"),
                    Path.Combine(moduleRoot, extension, $"{areaResRef}.{extension}.json"));
            }

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            using var firstLoadStarted = new ManualResetEventSlim();
            using var releaseFirstLoad = new ManualResetEventSlim();
            using var secondLoadStarted = new ManualResetEventSlim();
            using var releaseSecondLoad = new ManualResetEventSlim();
            var loadCount = 0;
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                areaDocumentsLoader: (arePath, gitPath, gicPath) =>
                {
                    var loadNumber = Interlocked.Increment(ref loadCount);
                    var started = loadNumber == 1 ? firstLoadStarted : secondLoadStarted;
                    var release = loadNumber == 1 ? releaseFirstLoad : releaseSecondLoad;
                    started.Set();
                    if (!release.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException($"Area-load attempt {loadNumber} was not released.");

                    return AreaEditorDocumentLoad.Load(arePath, gitPath, gicPath);
                });

            workspace.Open(moduleRoot);
            editors.TryOpenEditor(ResourceType.Area, areaResRef);
            firstLoadStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            workspace.Open(moduleRoot);
            releaseFirstLoad.Set();
            secondLoadStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue(
                "reopening the same module for a watcher rescan must preserve the pending area open");

            workspace.Open(differentRoot);
            releaseSecondLoad.Set();

            var openingAreas = (HashSet<string>)typeof(EditorService)
                .GetField("_openingAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            await WaitUntilAsync(() => !openingAreas.Contains(areaResRef));

            loadCount.Should().Be(2);
            log.Lines.Should().NotContain(line => line.Contains("Failed to open area editor"));
        }

        [Test]
        public async Task AreaLoadCompletingAfterDeletionStartsIsNotPublished()
        {
            const string areaResRef = "bank";
            var moduleRoot = NewModuleRoot();
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.Copy(
                    Path.Combine(
                        CorpusLocator.ModuleDirectory,
                        extension,
                        $"{areaResRef}.{extension}.json"),
                    Path.Combine(moduleRoot, extension, $"{areaResRef}.{extension}.json"));
            }

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var mutationLock = new ModuleMutationLock();
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                mutationLock: mutationLock,
                areaDocumentsLoader: (arePath, gitPath, gicPath) =>
                {
                    loadStarted.Set();
                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("The area-load deletion race test was not released.");

                    return AreaEditorDocumentLoad.Load(arePath, gitPath, gicPath);
                });

            workspace.Open(moduleRoot);
            await workspace.Workspace!.TagIndex.GetTransitionDestinationTagsAsync();
            editors.TryOpenEditor(ResourceType.Area, areaResRef);
            loadStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            using (mutationLock.BeginResourceDeletion())
            {
                releaseLoad.Set();
                var openingAreas = (HashSet<string>)typeof(EditorService)
                    .GetField("_openingAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(editors)!;
                await WaitUntilAsync(() => !openingAreas.Contains(areaResRef));

                var openAreas = (Dictionary<string, AreaEditorViewModel>)typeof(EditorService)
                    .GetField("_openAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(editors)!;
                openAreas.Should().NotContainKey(areaResRef,
                    "an area load that finishes during deletion must not publish stale documents");
            }

            log.Lines.Should().Contain(line =>
                line.Contains($"Could not open '{areaResRef}'") &&
                line.Contains("a module resource deletion is in progress"));
        }

        [Test]
        public async Task AreaLoadCancelledByCommittedDeletionCannotPublishAfterReservationEnds()
        {
            const string areaResRef = "bank";
            var moduleRoot = NewModuleRoot();
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                File.Copy(
                    Path.Combine(
                        CorpusLocator.ModuleDirectory,
                        extension,
                        $"{areaResRef}.{extension}.json"),
                    Path.Combine(moduleRoot, extension, $"{areaResRef}.{extension}.json"));
            }

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var mutationLock = new ModuleMutationLock();
            using var loadStarted = new ManualResetEventSlim();
            using var releaseLoad = new ManualResetEventSlim();
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                mutationLock: mutationLock,
                areaDocumentsLoader: (arePath, gitPath, gicPath) =>
                {
                    loadStarted.Set();
                    if (!releaseLoad.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("The cancelled area-load test was not released.");

                    return AreaEditorDocumentLoad.Load(arePath, gitPath, gicPath);
                });

            workspace.Open(moduleRoot);
            await workspace.Workspace!.TagIndex.GetTransitionDestinationTagsAsync();
            editors.TryOpenEditor(ResourceType.Area, areaResRef);
            loadStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            using (mutationLock.BeginResourceDeletion())
            {
                var closeForDeletion = typeof(EditorService).GetMethod(
                    "TryCloseResourceForDeletion",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                ((bool)closeForDeletion.Invoke(editors, [ResourceType.Area, areaResRef])!)
                    .Should().BeTrue();
            }

            // The delete command has now committed and released its reservation, but the old parse
            // is still running. Its tombstone must outlive that transient lock.
            releaseLoad.Set();
            var openingAreas = (HashSet<string>)typeof(EditorService)
                .GetField("_openingAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            await WaitUntilAsync(() => !openingAreas.Contains(areaResRef));

            var openAreas = (Dictionary<string, AreaEditorViewModel>)typeof(EditorService)
                .GetField("_openAreaEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            openAreas.Should().NotContainKey(areaResRef,
                "a load cancelled by a committed delete must not publish after the lock is released");
        }

        [Test]
        public void ResourceDeletionBlocksEveryEditorOpeningRoute()
        {
            const string resRef = "delete_in_progress";
            var moduleRoot = NewModuleRoot();
            Directory.CreateDirectory(Path.Combine(moduleRoot, "nss"));
            File.WriteAllText(
                Path.Combine(moduleRoot, "nss", resRef + ".nss"),
                "void main() {}");
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var mutationLock = new ModuleMutationLock();
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                mutationLock: mutationLock);
            workspace.Open(moduleRoot);

            using (mutationLock.BeginResourceDeletion())
            {
                editors.TryOpenEditor(ResourceType.Nss, resRef);
                typeof(EditorService)
                    .GetMethod("OpenScriptEditor", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(editors, new object[] { workspace.Workspace!, resRef });
                typeof(EditorService)
                    .GetMethod("GoToObjectPlacement", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(editors, new object[]
                    {
                        new ObjectPlacement(
                            ResourceType.Utw,
                            "arrival_wp",
                            "area_being_deleted",
                            0,
                            "ARRIVAL",
                            1f,
                            2f,
                            3f)
                    });
                editors.OpenModuleProperties();
            }

            var openScripts = (Dictionary<string, ScriptEditorViewModel>)typeof(EditorService)
                .GetField("_openScriptEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            openScripts.Should().BeEmpty();
            var pendingAreaReveals = (Dictionary<string, ObjectPlacement>)typeof(EditorService)
                .GetField("_pendingAreaReveals", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(editors)!;
            pendingAreaReveals.Should().BeEmpty(
                "object-source Go To must not queue an area reveal during deletion");
            log.Lines.Count(line => line.Contains("a module resource deletion is in progress"))
                .Should().Be(4,
                    "public, include-navigation, object-source, and Module Properties routes are all gated");
        }

        [Test]
        public async Task PlacementInvalidationReloadsAnOpenObjectSource()
        {
            var moduleRoot = NewModuleRoot();
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var first = new ObjectPlacement(
                ResourceType.Utw, "arrival_wp", "old_area", 0, "OLD", 1f, 2f, 3f);
            var replacement = new ObjectPlacement(
                ResourceType.Utw, "arrival_wp", "new_area", 1, "NEW", 4f, 5f, 6f);
            IReadOnlyList<ObjectPlacement> current = new[] { first };
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!,
                objectPlacementsFinder: (_, _, _) => Task.FromResult(current));
            workspace.Open(moduleRoot);
            var createSource = typeof(EditorService).GetMethod(
                "CreateObjectSource",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var source = (ObjectSourceSectionViewModel)createSource.Invoke(
                editors,
                new object[] { ResourceType.Utw, "arrival_wp" })!;
            await WaitUntilAsync(() =>
                !source.IsLoading &&
                source.Placements.Count == 1 &&
                ReferenceEquals(source.Placements[0].Placement, first));

            current = new[] { replacement };
            workspace.InvalidatePlacementIndex();
            await WaitUntilAsync(() =>
                !source.IsLoading &&
                source.Placements.Count == 1 &&
                ReferenceEquals(source.Placements[0].Placement, replacement));

            source.Placements.Should().ContainSingle()
                .Which.Placement.Should().BeSameAs(replacement);
        }

        [Test]
        public void PlacementInvalidationClearsAnOpenMerchantSourceSnapshot()
        {
            var moduleRoot = NewModuleRoot();
            var merchantFolder = Path.Combine(moduleRoot, "utm");
            Directory.CreateDirectory(merchantFolder);
            var merchantPath = Path.Combine(merchantFolder, "probe_store.utm.json");
            File.WriteAllBytes(
                merchantPath,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store"));
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var editors = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                factory: null!,
                prompts: null!);
            workspace.Open(moduleRoot);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var merchant = new MerchantDocumentViewModel(
                merchantPath,
                "probe_store",
                log,
                new StubPrompts());

            try
            {
                var openEditors = (Dictionary<string, MerchantDocumentViewModel>)typeof(EditorService)
                    .GetField("_openMerchantEditors", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(editors)!;
                openEditors[merchantPath] = merchant;
                merchant.Editor.ArePlacedInstancesLoaded = true;
                merchant.Editor.PlacedInstances.Add(new MerchantInstancePlacement(
                    "Old Area", "old_area", "OLD_STORE", 0, 0, 0));

                workspace.InvalidatePlacementIndex();
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();

                merchant.Editor.ArePlacedInstancesLoaded.Should().BeFalse();
                merchant.Editor.PlacedInstancesNeedRefresh.Should().BeTrue();
                merchant.Editor.PlacedInstances.Should().BeEmpty(
                    "stale merchant coordinates must not remain available to Go To");
            }
            finally
            {
                merchant.OnClose();
            }
        }

        [Test]
        public async Task ReplacingAWorkspaceCancelsItsObsoletePlacementWarmup()
        {
            var firstRoot = NewModuleRoot();
            var secondRoot = NewModuleRoot();
            File.WriteAllText(Path.Combine(firstRoot, "are", "broken.are.json"), "{}");
            File.WriteAllText(Path.Combine(firstRoot, "git", "broken.git.json"), "{");
            var firstWorkspace = new ModuleWorkspace(firstRoot);
            using var readFailed = new ManualResetEventSlim();
            using var releaseFailure = new ManualResetEventSlim();
            firstWorkspace.PlacementIndex.AreaReadFailed += (area, _) =>
            {
                if (area != "broken")
                    return;

                readFailed.Set();
                releaseFailure.Wait(TimeSpan.FromSeconds(5));
            };
            var log = new OutputLogService();
            var cancellationLogged = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            log.Lines.CollectionChanged += (_, args) =>
            {
                if (args.NewItems?.Cast<string>().Any(line => line.Contains(
                        "Placement index warm-up canceled because its snapshot was invalidated")) == true)
                {
                    cancellationLogged.TrySetResult();
                }
            };
            var workspace = new WorkspaceContext(
                root => root == firstRoot ? firstWorkspace : new ModuleWorkspace(root),
                log);

            workspace.Open(firstRoot);
            var firstCatalog = workspace.Catalog!.BuildTask;
            readFailed.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            workspace.Open(secondRoot);
            var secondCatalog = workspace.Catalog!.BuildTask;
            releaseFailure.Set();

            await cancellationLogged.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.WhenAll(firstCatalog, secondCatalog);
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var timeout = DateTime.UtcNow.AddSeconds(5);
            while (!condition() && DateTime.UtcNow < timeout)
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                await Task.Delay(10);
            }

            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            condition().Should().BeTrue();
        }

        private string NewModuleRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), $"swlor_editor_index_{Guid.NewGuid():N}");
            foreach (var folder in new[] { "are", "git", "gic", "utc" })
                Directory.CreateDirectory(Path.Combine(root, folder));
            _roots.Add(root);
            return root;
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline,
                string message,
                string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
