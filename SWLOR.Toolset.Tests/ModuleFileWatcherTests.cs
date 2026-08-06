using System.Reflection;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class ModuleFileWatcherTests
    {
        [TestCase(@"C:\module\utc\alask.utc.json", ResourceType.Utc, "alask")]
        [TestCase(@"C:\module\nss\on_enter.nss", ResourceType.Nss, "on_enter")]
        [TestCase(@"C:\module\nss\ON_EXIT.NSS", ResourceType.Nss, "ON_EXIT")]
        [TestCase(@"C:\repo\SWLOR.Game.Server\ConversationData\intro.conversation.json", ResourceType.Dlg, "intro")]
        public void SupportedResourcePathsResolveTheirTypeAndResRef(
            string path,
            ResourceType expectedType,
            string expectedResRef)
        {
            ModuleFileWatcher.TryResolveResource(path, out var type, out var resRef).Should().BeTrue();

            type.Should().Be(expectedType);
            resRef.Should().Be(expectedResRef);
        }

        [TestCase(@"C:\module\itp\doorpalcus.itp.json", "doorpalcus")]
        [TestCase(@"C:\module\itp\soundpalcus.ITP.JSON", "soundpalcus")]
        [TestCase(@"C:\module\itp\triggerpalcus.itp.json", "triggerpalcus")]
        [TestCase(@"C:\module\itp\waypointpalcus.itp.json", "waypointpalcus")]
        public void PalettePathsResolveWithoutPretendingItpIsAResourceType(
            string path,
            string expectedResRef)
        {
            ModuleFileWatcher.TryResolvePalette(path, out var paletteResRef).Should().BeTrue();

            paletteResRef.Should().Be(expectedResRef);
            ModuleFileWatcher.TryResolveResource(path, out _, out _).Should().BeFalse();
        }

        [TestCase(@"C:\module\config.json")]
        [TestCase(@"C:\module\packing\area.git.json.tmp")]
        [TestCase(@"C:\module\readme.txt")]
        public void NonResourcePathsAreIgnored(string path)
        {
            ModuleFileWatcher.TryResolveResource(path, out _, out _).Should().BeFalse();
        }

        [TestCase(@"C:\module\git\area.git.json")]
        [TestCase(@"C:\module\are\area.are.json")]
        [TestCase(@"C:\module\utd\door.utd.json")]
        [TestCase(@"C:\module\uti\door_key.uti.json")]
        [TestCase(@"C:\module\utw\waypoint.utw.json")]
        public void TransitionTagResourcesInvalidateTheTagIndex(string path)
        {
            ModuleFileWatcher.AffectsTagIndex(path).Should().BeTrue();
        }

        [TestCase(@"C:\module\utc\creature.utc.json")]
        [TestCase(@"C:\module\dlg\conversation.dlg.json")]
        public void UnrelatedResourcesDoNotInvalidateTheTagIndex(string path)
        {
            ModuleFileWatcher.AffectsTagIndex(path).Should().BeFalse();
        }

        [TestCase(@"C:\module\git\area.git.json", true)]
        [TestCase(@"C:\module\are\area.are.json", false)]
        [TestCase(@"C:\module\utd\door.utd.json", false)]
        [TestCase(@"C:\module\uti\door_key.uti.json", false)]
        [TestCase(@"C:\module\utw\waypoint.utw.json", false)]
        public void OnlyPairedGitResourcesInvalidateThePlacementIndex(string path, bool expected)
        {
            ModuleFileWatcher.AffectsPlacementIndex(path).Should().Be(expected);
        }

        [TestCase(@"C:\module\git\area.git.json")]
        [TestCase(@"C:\module\utc\creature.utc.json")]
        [TestCase(@"C:\module\dlg\conversation.dlg.json")]
        [TestCase(@"C:\module\are\area.are.json")]
        public void ScriptBearingResourcesInvalidateUsageIndex(string path)
        {
            ModuleFileWatcher.AffectsScriptUsages(path).Should().BeTrue();
        }

        [TestCase(@"C:\module\nss\script.nss")]
        [TestCase(@"C:\module\config.json")]
        public void ResourcesWithoutScriptSlotsDoNotInvalidateUsageIndex(string path)
        {
            ModuleFileWatcher.AffectsScriptUsages(path).Should().BeFalse();
        }

        [TestCase("temp0")]
        [TestCase("TEMP1")]
        [TestCase("temp12")]
        public void NwnToolsetTemporaryDirectoryNamesAreRecognized(string directoryName)
        {
            ModuleFileWatcher.IsNwnToolsetTemporaryDirectoryName(directoryName).Should().BeTrue();
        }

        [TestCase("temp")]
        [TestCase("temporary")]
        [TestCase("temp1-backup")]
        [TestCase("attempt0")]
        [TestCase("templates")]
        public void NormalDirectoryNamesAreNotMistakenForNwnToolsetTemporaryDirectories(
            string directoryName)
        {
            ModuleFileWatcher.IsNwnToolsetTemporaryDirectoryName(directoryName).Should().BeFalse();
        }

        [Test]
        [NonParallelizable]
        public void WatchIncludesTheSiblingConversationDataDirectory()
        {
            var repositoryRoot = Path.Combine(
                Path.GetTempPath(), $"swlor_conversation_watch_{Guid.NewGuid():N}");
            var moduleRoot = Path.Combine(repositoryRoot, "Module");
            var conversationRoot = Path.Combine(
                repositoryRoot, "SWLOR.Game.Server", "ConversationData");
            Directory.CreateDirectory(moduleRoot);
            Directory.CreateDirectory(conversationRoot);
            using var watcher = new ModuleFileWatcher(new OutputLogService());

            try
            {
                watcher.Watch(moduleRoot);

                var field = typeof(ModuleFileWatcher).GetField(
                    "_watchers", BindingFlags.Instance | BindingFlags.NonPublic);
                field.Should().NotBeNull();
                var watchers = field!.GetValue(watcher) as Dictionary<string, FileSystemWatcher>;

                watchers.Should().NotBeNull();
                watchers!.Keys.Should().Contain(
                    Path.TrimEndingDirectorySeparator(Path.GetFullPath(conversationRoot)),
                    "graph-native conversations live outside Module and need their own watcher");
            }
            finally
            {
                watcher.Stop();
                if (Directory.Exists(repositoryRoot))
                    Directory.Delete(repositoryRoot, recursive: true);
            }
        }

        [Test]
        public void WorkspaceCatalogRefreshInvalidatesScriptUsagesOnlyForScriptedResources()
        {
            var context = new WorkspaceContext(
                _ => throw new NotSupportedException(),
                new OutputLogService());
            var invalidations = 0;
            context.ScriptUsagesInvalidated += () => invalidations++;

            context.RefreshCatalogEntry(ResourceType.Utc, "creature");
            context.RefreshCatalogEntry(ResourceType.Nss, "script");

            invalidations.Should().Be(1);
        }

        [Test]
        public void WorkspaceTagInvalidationNotifiesLiveIndexConsumers()
        {
            var context = new WorkspaceContext(
                _ => throw new NotSupportedException(),
                new OutputLogService());
            var invalidations = 0;
            context.TagIndexInvalidated += () => invalidations++;

            context.InvalidateTagIndex();

            invalidations.Should().Be(1,
                "open behavior editors need a signal that their materialized object-tag choices are stale");
        }

        [Test]
        public void WorkspacePaletteInvalidationNotifiesChoiceConsumers()
        {
            var context = new WorkspaceContext(
                _ => throw new NotSupportedException(),
                new OutputLogService());
            string? invalidated = null;
            context.PaletteChoicesInvalidated += paletteResRef => invalidated = paletteResRef;

            context.InvalidatePaletteChoices("doorpalcus");

            invalidated.Should().Be("doorpalcus");
        }

        /// <summary>
        /// A populated resource directory moved or renamed into the module in one atomic operation
        /// produces only a single directory-created event at the root watcher. Attaching a recursive
        /// watcher to it (what the Created/Renamed handlers already did) observes only changes from
        /// that point on; the files already inside are invisible to the catalog until something
        /// enumerates them. This exercises the actual production path
        /// (<c>TryAddTopLevelDirectoryWatcher</c>) rather than waiting on real
        /// <see cref="FileSystemWatcher"/> event delivery, whose OS-level latency would make the test
        /// flaky without testing anything this fix does not already cover.
        /// </summary>
        /// <remarks>
        /// Asserts that the debounce timer ends up armed rather than waiting for
        /// <see cref="ModuleFileWatcher.RescanRequested"/> to actually fire: the headless test platform
        /// does not drive <see cref="DispatcherTimer"/> from wall-clock sleeps (confirmed separately -
        /// a bare <see cref="DispatcherTimer"/> started in an <see cref="AvaloniaTestAttribute"/> test
        /// and pumped with <see cref="Dispatcher.RunJobs"/> in a sleep loop never ticks here), so a real
        /// one-second wait would only make this test slow without proving anything the armed-timer
        /// check does not already prove.
        /// </remarks>
        [AvaloniaTest]
        [NonParallelizable]
        public void APopulatedDirectoryAttachedAfterWatchStartsArmsARescan()
        {
            var root = Path.Combine(Path.GetTempPath(), $"swlor_watch_{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var utc = Path.Combine(root, "utc");
            var log = new OutputLogService();
            using var watcher = new ModuleFileWatcher(log);

            try
            {
                // "utc" does not exist yet when watching starts - it arrives afterwards, already
                // holding a file, the way an atomic move/rename would deliver it.
                watcher.Watch(root);
                Directory.CreateDirectory(utc);
                File.WriteAllText(Path.Combine(utc, "guard.utc.json"), "{}");

                var method = typeof(ModuleFileWatcher).GetMethod(
                    "TryAddTopLevelDirectoryWatcher", BindingFlags.Instance | BindingFlags.NonPublic);
                method.Should().NotBeNull("the Created/Renamed handlers call this to attach the new directory's watcher");
                method!.Invoke(watcher, new object[] { utc });

                // ScheduleRescan defers its own timer setup through a Dispatcher.UIThread.Post; one pump
                // is enough to run it.
                Dispatcher.UIThread.RunJobs();

                var timerField = typeof(ModuleFileWatcher).GetField(
                    "_rescanDebounceTimer", BindingFlags.Instance | BindingFlags.NonPublic);
                timerField.Should().NotBeNull();
                var timer = timerField!.GetValue(watcher) as DispatcherTimer;

                timer.Should().NotBeNull(
                    "the new recursive watcher never enumerated the file already inside 'utc', so only a " +
                    "full rescan brings the catalog into line with it - and that rescan is armed here");
                timer!.IsEnabled.Should().BeTrue("the debounced rescan must be armed, not merely constructed");
            }
            finally
            {
                watcher.Stop();
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [AvaloniaTest]
        [NonParallelizable]
        public void RemovingTransientPackDirectoriesDoesNotArmARescan()
        {
            var root = Path.Combine(Path.GetTempPath(), $"swlor_watch_{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var log = new OutputLogService();
            using var watcher = new ModuleFileWatcher(log);

            try
            {
                watcher.Watch(root);

                var method = typeof(ModuleFileWatcher).GetMethod(
                    "HandleTopLevelDirectoryRemoved",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                method.Should().NotBeNull("the root Deleted/Renamed handlers use this path");

                foreach (var directoryName in new[]
                         {
                             "packing",
                             "palette-refresh",
                             "temp0",
                             ".swlor-toolset-item-rename-0123456789abcdef"
                         })
                {
                    method!.Invoke(
                        watcher,
                        new object[] { Path.Combine(root, directoryName) });
                }

                Dispatcher.UIThread.RunJobs();

                var timerField = typeof(ModuleFileWatcher).GetField(
                    "_rescanDebounceTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                timerField.Should().NotBeNull();
                timerField!.GetValue(watcher).Should().BeNull(
                    "routine pack/toolset cleanup must not masquerade as a watcher overflow");
            }
            finally
            {
                watcher.Stop();
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}
