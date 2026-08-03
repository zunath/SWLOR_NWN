using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
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

        private string NewModuleRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), $"swlor_editor_index_{Guid.NewGuid():N}");
            foreach (var folder in new[] { "are", "git", "utc" })
                Directory.CreateDirectory(Path.Combine(root, folder));
            _roots.Add(root);
            return root;
        }
    }
}
