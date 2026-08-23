using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Every surface that writes into the module folder has to stand down while a pack, a validation
    /// run, or Build All is walking it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The failure this guards is quiet and expensive: the packer copies each resource folder in turn,
    /// so a write that lands mid-copy is captured half-finished — or, for a new area, captured as a
    /// <c>module.ifo</c> entry with no ARE/GIT/GIC behind it. Nothing throws; the .mod is simply wrong.
    /// </para>
    /// <para>
    /// Each panel used to answer "is the module busy" for itself, and three of them answered "no"
    /// unconditionally. These tests are written against the shared lock so a new consumer is a
    /// constructor argument rather than a rediscovery of the same bug.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class ModuleMutationLockTests
    {
        private static (WorkspaceContext Workspace, OutputLogService Log) Context()
        {
            var log = new OutputLogService();
            return (new WorkspaceContext(root => new ModuleWorkspace(root), log), log);
        }

        [Test]
        public void TheLockStartsOpen()
        {
            new ModuleMutationLock().IsLocked.Should().BeFalse();
        }

        /// <summary>
        /// A predicate answers "is it locked now"; a control also needs telling when the answer
        /// changed, or it stays greyed for the rest of the session after the first pack.
        /// </summary>
        [Test]
        public void FlippingTheLockAnnouncesIt()
        {
            var mutationLock = new ModuleMutationLock();
            var announcements = 0;
            mutationLock.Changed += () => announcements++;

            mutationLock.Set(true);
            mutationLock.Set(true);
            mutationLock.Set(false);

            announcements.Should().Be(2, "repeating the current state is not a change");
        }

        [Test]
        public void ResourceDeletionPublishesANestableModuleOperation()
        {
            var mutationLock = new ModuleMutationLock();
            var announcements = 0;
            mutationLock.Changed += () => announcements++;

            using (mutationLock.BeginResourceDeletion())
            {
                mutationLock.IsLocked.Should().BeTrue();
                mutationLock.IsResourceDeletionActive.Should().BeTrue();
                using (mutationLock.BeginResourceDeletion())
                    announcements.Should().Be(1, "nested owners do not change the published state");
            }

            mutationLock.IsLocked.Should().BeFalse();
            mutationLock.IsResourceDeletionActive.Should().BeFalse();
            announcements.Should().Be(2);
        }

        [Test]
        public void ValidationStandsDownWhileTheModuleIsLocked()
        {
            var (workspace, log) = Context();
            var mutationLock = new ModuleMutationLock();
            var validation = new ValidationViewModel(
                workspace,
                log,
                () => null!,
                mutationLock: mutationLock);

            validation.CanRun().Should().BeTrue();

            mutationLock.Set(true);

            validation.CanRun().Should().BeFalse(
                "validation saves every dirty editor before it scans, which replaces the files the packer is copying");
            validation.RunCommand.CanExecute(null).Should().BeFalse();
        }

        [Test]
        public void ValidationComesBackWhenTheLockClears()
        {
            var (workspace, log) = Context();
            var mutationLock = new ModuleMutationLock();
            var validation = new ValidationViewModel(workspace, log, () => null!, mutationLock: mutationLock);

            mutationLock.Set(true);
            mutationLock.Set(false);

            validation.RunCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public void ModuleContentsStopsOfferingNewResourcesWhileTheModuleIsLocked()
        {
            var (workspace, log) = Context();
            var mutationLock = new ModuleMutationLock();
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log,
                mutationLock: mutationLock);

            explorer.CanCreateSelectedType.Should().BeTrue();

            mutationLock.Set(true);

            explorer.CanCreateSelectedType.Should().BeFalse(
                "creating an area writes its ARE/GIT/GIC and then module.ifo, and a pack between those two writes captures one without the other");
            explorer.NewItemCommand.CanExecute(null).Should().BeFalse();

            mutationLock.Set(false);

            explorer.NewItemCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public void ModuleContentsStopsOfferingScriptCompilationWhileTheModuleIsLocked()
        {
            var (workspace, log) = Context();
            var mutationLock = new ModuleMutationLock();
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log,
                mutationLock: mutationLock)
            {
                SelectedType = ResourceType.Nss
            };

            explorer.CanCompileSelectedType.Should().BeTrue();
            explorer.CompileSelectedCommand.CanExecute(null).Should().BeTrue();

            mutationLock.Set(true);

            explorer.CanCompileSelectedType.Should().BeFalse();
            explorer.CompileSelectedCommand.CanExecute(null).Should().BeFalse();

            mutationLock.Set(false);

            explorer.CompileSelectedCommand.CanExecute(null).Should().BeTrue();
        }

        /// <summary>
        /// The tab's own Compile button, not the Build menu. It saves the .nss and then writes the
        /// .ncs, so during a pack it replaces what is being copied and during Build All it points a
        /// second compiler process at the same output.
        /// </summary>
        [Test]
        public void APerScriptCompileStandsDownWhileTheModuleIsLocked()
        {
            var path = Path.Combine(Path.GetTempPath(), $"swlor_lock_{Guid.NewGuid():N}.nss");
            File.WriteAllText(path, "void main() { }");

            try
            {
                var mutationLock = new ModuleMutationLock();
                var editor = new ScriptEditorViewModel(
                    path, "lock_test", new OutputLogService(), new StubPrompts())
                {
                    CompileRequested = _ => Task.FromResult(true),
                    MutationLock = mutationLock
                };

                editor.CanCompile.Should().BeTrue();

                mutationLock.Set(true);

                editor.CanCompile.Should().BeFalse();
                editor.CompileCommand.CanExecute(null).Should().BeFalse();

                mutationLock.Set(false);

                editor.CompileCommand.CanExecute(null).Should().BeTrue();
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void ThePaletteStandsDownWhileTheModuleIsLocked()
        {
            var (workspace, log) = Context();
            var mutationLock = new ModuleMutationLock();
            var palette = new PaletteViewModel(
                workspace,
                new CategoryService(workspace, log),
                log,
                mutationLock: mutationLock);

            var announced = 0;
            palette.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PaletteViewModel.CanWrite))
                    announced++;
            };

            mutationLock.Set(true);

            palette.CanWrite.Should().BeFalse();
            announced.Should().BeGreaterThan(0, "the palette's write controls have to be told, not asked");
        }

        /// <summary>
        /// A pack, validation, or Build All can start in the gap between the confirmation dialog
        /// resuming and the deletion actually running - the dialog is the one await in the whole
        /// command with nothing stopping the click that started it. Unlike blueprint creation, which
        /// always goes through SaveService.WriteNewAtomic, blueprint deletion used to go straight to
        /// File.Delete with no recheck at all, so it could remove the file out from under a module
        /// walk that started while the "Delete?" dialog was still on screen.
        /// </summary>
        [Test]
        public async Task DeletingABlueprintRefusesWhenTheModuleLocksWhileTheConfirmationIsOpen()
        {
            var moduleRoot = Path.Combine(Path.GetTempPath(), $"swlor_palette_delete_{Guid.NewGuid():N}");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

            var blueprintPath = Path.Combine(moduleRoot, "utp", "testplc.utp.json");
            File.WriteAllText(blueprintPath, "{}");

            try
            {
                var log = new OutputLogService();
                var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
                workspace.Open(moduleRoot);
                var mutationLock = new ModuleMutationLock();

                // Stands in for a pack, validation, or Build All starting while the builder is still
                // looking at the confirmation dialog - the resume from that await is where the recheck
                // has to happen.
                var prompts = new LockDuringConfirmationPrompts(mutationLock);

                var palette = new PaletteViewModel(
                    workspace,
                    new CategoryService(workspace, log),
                    log,
                    prompts: prompts,
                    mutationLock: mutationLock)
                {
                    SelectedType = ResourceType.Utp
                };

                var tile = new PaletteTileViewModel("testplc", "Test Placeable", categoryPath: null);

                await palette.DeleteTileCommand.ExecuteAsync(tile);

                File.Exists(blueprintPath).Should().BeTrue(
                    "the module locked before the deletion ran, so the file must survive");
                palette.StatusMessage.Should().Contain("not deleted");
            }
            finally
            {
                Directory.Delete(moduleRoot, recursive: true);
            }
        }

        [Test]
        public async Task DeletingABlueprintPreservesAFileChangedWhileConfirmationIsOpen()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor_palette_delete_{Guid.NewGuid():N}");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

            var blueprintPath = Path.Combine(moduleRoot, "utp", "testplc.utp.json");
            File.WriteAllText(blueprintPath, "{\"generation\":\"original\"}");

            try
            {
                var log = new OutputLogService();
                var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
                workspace.Open(moduleRoot);
                var mutationLock = new ModuleMutationLock();
                const string externalGeneration = "{\"generation\":\"external\"}";
                var prompts = new ReplaceDuringConfirmationPrompts(
                    blueprintPath,
                    externalGeneration);
                var previousAmbient = ModuleMutationLock.ModuleWrites;
                ModuleMutationLock.ModuleWrites = mutationLock;

                var palette = new PaletteViewModel(
                    workspace,
                    new CategoryService(workspace, log),
                    log,
                    prompts: prompts,
                    mutationLock: mutationLock)
                {
                    SelectedType = ResourceType.Utp
                };
                var tile = new PaletteTileViewModel(
                    "testplc",
                    "Test Placeable",
                    categoryPath: null);

                try
                {
                    await palette.DeleteTileCommand.ExecuteAsync(tile);
                }
                finally
                {
                    ModuleMutationLock.ModuleWrites = previousAmbient;
                }

                File.ReadAllText(blueprintPath).Should().Be(
                    externalGeneration,
                    "the confirmation applies only to the generation the builder reviewed");
                palette.StatusMessage.Should().Contain("changed while the delete confirmation was open");
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        /// <summary>
        /// Baseline for the tests above: with no module-wide operation in the way, deletion still
        /// works even though the generated custom palette still describes the established blueprint.
        /// Packing rebuilds that palette from the surviving blueprint files.
        /// </summary>
        [Test]
        public async Task DeletingABlueprintStillWorksWhenTheGeneratedPaletteContainsItsDescriptor()
        {
            var moduleRoot = Path.Combine(Path.GetTempPath(), $"swlor_palette_delete_{Guid.NewGuid():N}");
            foreach (var folder in new[] { "are", "utc", "utp", "itp" })
                Directory.CreateDirectory(Path.Combine(moduleRoot, folder));

            var blueprintPath = Path.Combine(moduleRoot, "utp", "testplc.utp.json");
            File.WriteAllText(blueprintPath, "{}");
            File.WriteAllBytes(
                Path.Combine(moduleRoot, "itp", "placeablepalcus.itp.json"),
                SyntheticPalette.Flat(("Test Placeable", "testplc")).ToBytes());

            try
            {
                var log = new OutputLogService();
                var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
                workspace.Open(moduleRoot);
                var mutationLock = new ModuleMutationLock();
                var prompts = new AlwaysConfirmPrompts();

                // The delete path also consults the ambient ModuleWrites guard; own it here so a
                // lock left behind by another fixture cannot leak into this baseline.
                var previousAmbient = ModuleMutationLock.ModuleWrites;
                ModuleMutationLock.ModuleWrites = mutationLock;

                var palette = new PaletteViewModel(
                    workspace,
                    new CategoryService(workspace, log),
                    log,
                    prompts: prompts,
                    mutationLock: mutationLock)
                {
                    SelectedType = ResourceType.Utp
                };

                var tile = new PaletteTileViewModel("testplc", "Test Placeable", categoryPath: null);

                try
                {
                    await palette.DeleteTileCommand.ExecuteAsync(tile);
                }
                finally
                {
                    ModuleMutationLock.ModuleWrites = previousAmbient;
                }

                File.Exists(blueprintPath).Should().BeFalse(
                    "the generated palette is refreshed during packing and must not block deletion");
                palette.StatusMessage.Should().Contain("Deleted");
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        private sealed class AlwaysConfirmPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(true);
        }

        private sealed class LockDuringConfirmationPrompts(ModuleMutationLock mutationLock) : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel)
            {
                // The builder clicked Delete before anything else started, so the confirmation
                // resumes true - but a module-wide operation began while it was on screen.
                mutationLock.Set(true);
                return Task.FromResult(true);
            }
        }

        private sealed class ReplaceDuringConfirmationPrompts(
            string path,
            string replacement) : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline,
                string message,
                string confirmLabel)
            {
                File.WriteAllText(path, replacement);
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// The backstop, and the reason it is process-wide. Eight editor tabs each own a Save button
        /// that goes straight to their own TrySaveAsync; greying the shell's menu never reached any
        /// of them, so every one was a way to replace an ARE/GIT/GIC triplet while the packer walked
        /// past it - leaving the built module with two generations of the same area. Checking at the
        /// write means a ninth editor cannot forget.
        /// </summary>
        [Test]
        public void NoModuleWriteLandsWhileTheModuleIsLocked()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"swlor_write_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "guard.utc.json");
            File.WriteAllText(path, "{\"original\":true}");

            var mutationLock = new ModuleMutationLock();
            var previous = ModuleMutationLock.ModuleWrites;
            ModuleMutationLock.ModuleWrites = mutationLock;

            try
            {
                mutationLock.Set(true);

                var write = () => SaveService.WriteAtomic(
                    path, System.Text.Encoding.UTF8.GetBytes("{\"new\":true}"));
                write.Should().Throw<ModuleLockedException>();

                var create = () => SaveService.WriteNewAtomic(
                    Path.Combine(directory, "fresh.utc.json"),
                    System.Text.Encoding.UTF8.GetBytes("{}"));
                create.Should().Throw<ModuleLockedException>();

                File.ReadAllText(path).Should().Be("{\"original\":true}");
                Directory.GetFiles(directory).Should().ContainSingle(
                    "a refused write leaves no staging debris either");

                mutationLock.Set(false);

                write.Should().NotThrow();
                File.ReadAllText(path).Should().Be("{\"new\":true}");
            }
            finally
            {
                ModuleMutationLock.ModuleWrites = previous;
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void TheOwningModuleOperationCanPerformItsPrerequisiteSave()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"swlor_owner_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "guard.utc.json");
            File.WriteAllText(path, "{\"original\":true}");

            var mutationLock = new ModuleMutationLock();
            var previous = ModuleMutationLock.ModuleWrites;
            ModuleMutationLock.ModuleWrites = mutationLock;

            try
            {
                mutationLock.Set(true);

                using (ModuleMutationLock.AllowModuleWrites())
                {
                    SaveService.WriteAtomic(
                        path, System.Text.Encoding.UTF8.GetBytes("{\"saved\":true}"));
                }

                File.ReadAllText(path).Should().Be("{\"saved\":true}");
                var unrelatedWrite = () => SaveService.WriteAtomic(
                    path, System.Text.Encoding.UTF8.GetBytes("{\"raced\":true}"));
                unrelatedWrite.Should().Throw<ModuleLockedException>(
                    "only the operation that reserved the lock may perform its prerequisite saves");
            }
            finally
            {
                ModuleMutationLock.ModuleWrites = previous;
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public async Task AnIndependentWriterWaitsForTheCrossProcessLease()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor_cross_process_lock_{Guid.NewGuid():N}");
            var resourceDirectory = Path.Combine(moduleRoot, "utc");
            Directory.CreateDirectory(resourceDirectory);
            var path = Path.Combine(resourceDirectory, "guard.utc.json");
            File.WriteAllText(path, "{\"original\":true}");

            try
            {
                var heldLock = ModuleWriteLock.Acquire(moduleRoot);
                try
                {
                    Task attemptedWrite;
                    using (ExecutionContext.SuppressFlow())
                    {
                        attemptedWrite = Task.Run(() => SaveService.WriteAtomic(
                            path,
                            System.Text.Encoding.UTF8.GetBytes("{\"raced\":true}")));
                    }

                    await Task.Delay(100);
                    attemptedWrite.IsCompleted.Should().BeFalse(
                        "the independent writer must wait until the module-wide operation finishes");
                    File.ReadAllText(path).Should().Be("{\"original\":true}");

                    heldLock.Dispose();
                    await attemptedWrite;
                }
                finally
                {
                    heldLock.Dispose();
                }

                File.ReadAllText(path).Should().Be("{\"raced\":true}");
            }
            finally
            {
                if (Directory.Exists(moduleRoot))
                    Directory.Delete(moduleRoot, recursive: true);
            }
        }

        [Test]
        public async Task ConversationSaveWaitsForTheSourceLeaseUsedByPacking()
        {
            var conversationRoot = Path.Combine(
                Path.GetTempPath(), $"swlor_conversation_lock_{Guid.NewGuid():N}");
            Directory.CreateDirectory(conversationRoot);
            var path = Path.Combine(conversationRoot, "guard.conversation.json");
            File.WriteAllText(path, "{\"original\":true}");

            try
            {
                var heldLock = ModuleWriteLock.Acquire(conversationRoot);
                try
                {
                    Task attemptedSave;
                    using (ExecutionContext.SuppressFlow())
                    {
                        attemptedSave = Task.Run(() => SaveService.WriteAtomic(
                            path,
                            System.Text.Encoding.UTF8.GetBytes("{\"raced\":true}")));
                    }

                    await Task.Delay(100);
                    attemptedSave.IsCompleted.Should().BeFalse(
                        "packing holds the conversation source lease from build through deployment");
                    File.ReadAllText(path).Should().Be("{\"original\":true}");

                    heldLock.Dispose();
                    await attemptedSave;
                }
                finally
                {
                    heldLock.Dispose();
                }

                File.ReadAllText(path).Should().Be("{\"raced\":true}");
            }
            finally
            {
                if (Directory.Exists(conversationRoot))
                    Directory.Delete(conversationRoot, recursive: true);
            }
        }

        [Test]
        public async Task ALeaseHeldAcrossAwaitDoesNotFlowBackIntoSiblingWork()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                $"swlor_ambient_cross_process_lock_{Guid.NewGuid():N}");
            Directory.CreateDirectory(moduleRoot);

            try
            {
                // Prime the caller's ambient state. The old implementation retained this empty
                // dictionary after disposal, then a later async holder mutated the shared object.
                using (ModuleWriteLock.Acquire(moduleRoot))
                {
                }

                var acquired = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                var release = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                var holder = HoldLeaseAcrossAwait(moduleRoot, acquired, release.Task);
                Task? sibling = null;
                try
                {
                    await acquired.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    var siblingAttempting = new TaskCompletionSource(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    sibling = Task.Run(() =>
                    {
                        siblingAttempting.SetResult();
                        using var lease = ModuleWriteLock.Acquire(
                            moduleRoot,
                            TimeSpan.FromSeconds(5));
                    });

                    await siblingAttempting.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    var completed = await Task.WhenAny(sibling, Task.Delay(100));
                    completed.Should().NotBe(sibling,
                        "a sibling operation must contend for the OS lease rather than inherit it as nested work");
                }
                finally
                {
                    release.TrySetResult();
                    await holder;
                    if (sibling != null)
                        await sibling;
                }
            }
            finally
            {
                Directory.Delete(moduleRoot, recursive: true);
            }
        }

        private static async Task HoldLeaseAcrossAwait(
            string moduleRoot,
            TaskCompletionSource acquired,
            Task release)
        {
            using var lease = ModuleWriteLock.Acquire(moduleRoot);
            acquired.SetResult();
            await release;
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(false);
        }
    }
}
