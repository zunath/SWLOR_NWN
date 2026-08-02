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
