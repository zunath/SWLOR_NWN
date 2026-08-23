using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Resource deletion from each of Module Contents' Areas, Dialogs, and Scripts tabs.</summary>
    [TestFixture]
    public sealed class ModuleExplorerDeleteTests
    {
        private string _root = string.Empty;
        private string _module = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_explorer_delete_{Guid.NewGuid():N}");
            _module = Path.Combine(_root, "Module");
            foreach (var folder in new[] { "are", "dlg", "gic", "git", "ifo", "ncs", "nss", "utc" })
                Directory.CreateDirectory(Path.Combine(_module, folder));
            Directory.CreateDirectory(ModuleWorkspace.ResolveConversationDataRoot(_module));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public async Task DeleteScript_RemovesSourceCompiledOutputAndFolderMembership()
        {
            const string resRef = "delete_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            var compiled = Path.Combine(_module, "ncs", resRef + ".ncs");
            File.WriteAllText(source, "void main() {}");
            File.WriteAllBytes(compiled, new byte[] { 1, 2, 3 });

            var prompts = new RecordingPrompts(answer: true);
            var (explorer, categories) = CreateExplorer(ResourceType.Nss, prompts);
            var folder = categories.Section(ResourceType.Nss)!.AddFolder("Utility");
            folder.AddMember(resRef);
            categories.Section(ResourceType.Nss)!.IsSeeded = true;
            categories.SaveChanges().Saved.Should().BeTrue();
            explorer.Refresh();
            explorer.SelectedRow = explorer.Rows.Single(row => row.Folder == folder).Children
                .Single(row => row.ResRef == resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(source).Should().BeFalse();
            File.Exists(compiled).Should().BeFalse("a deleted source must not leave runnable orphan bytecode");
            folder.Members.Should().NotContain(resRef);
            prompts.Message.Should().Contain("delete_script.nss").And.Contain("delete_script.ncs");
            explorer.Rows.SelectMany(row => row.Children).Should().NotContain(row => row.ResRef == resRef);
        }

        [Test]
        public async Task DeleteDialog_RemovesGraphAndLegacyForms()
        {
            const string resRef = "delete_dialog";
            var graph = Path.Combine(ModuleWorkspace.ResolveConversationDataRoot(_module), resRef + ".conversation.json");
            var legacy = Path.Combine(_module, "dlg", resRef + ".dlg.json");
            File.WriteAllText(graph, "{}");
            File.WriteAllText(legacy, "{}");

            var prompts = new RecordingPrompts(answer: true);
            var (explorer, _) = CreateExplorer(ResourceType.Dlg, prompts);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(graph).Should().BeFalse();
            File.Exists(legacy).Should().BeFalse(
                "deleting only the graph would make the shadowed legacy dialog reappear in Module Contents");
            prompts.Message.Should().Contain("delete_dialog.conversation.json").And.Contain("delete_dialog.dlg.json");
        }

        [Test]
        public async Task DeleteArea_RemovesTripletAndModuleRegistration()
        {
            const string resRef = "delete_area";
            CopyAreaTemplate(resRef);
            var ifoPath = Path.Combine(_module, "ifo", "module.ifo.json");
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"), ifoPath);
            var ifo = IfoDocument.Load(ifoPath);
            AreaTemplateFactory.AddAreaToModule(ifo, resRef).Should().BeTrue();
            File.WriteAllBytes(ifoPath, ifo.ToBytes());

            var prompts = new RecordingPrompts(answer: true);
            var (explorer, _) = CreateExplorer(ResourceType.Area, prompts);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(Path.Combine(_module, "are", resRef + ".are.json")).Should().BeFalse();
            File.Exists(Path.Combine(_module, "git", resRef + ".git.json")).Should().BeFalse();
            File.Exists(Path.Combine(_module, "gic", resRef + ".gic.json")).Should().BeFalse();
            IfoDocument.Load(ifoPath).AreaResRefs.Should().NotContain(resRef);
            prompts.Message.Should().Contain("delete_area.are.json")
                .And.Contain("delete_area.git.json")
                .And.Contain("delete_area.gic.json")
                .And.Contain("module.ifo.json");
        }

        [Test]
        public async Task DecliningConfirmation_PreservesEveryScriptFile()
        {
            const string resRef = "keep_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            var compiled = Path.Combine(_module, "ncs", resRef + ".ncs");
            File.WriteAllText(source, "void main() {}");
            File.WriteAllBytes(compiled, new byte[] { 1 });

            var (explorer, _) = CreateExplorer(ResourceType.Nss, new RecordingPrompts(answer: false));
            explorer.SelectedRow = UnsortedResource(explorer, resRef);
            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(source).Should().BeTrue();
            File.Exists(compiled).Should().BeTrue();
        }

        [Test]
        public async Task ModuleLockStartingDuringConfirmation_RefusesDelete()
        {
            const string resRef = "locked_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            File.WriteAllText(source, "void main() {}");
            var mutationLock = new ModuleMutationLock();
            var prompts = new RecordingPrompts(answer: true, onConfirm: () => mutationLock.Set(true));
            var (explorer, _) = CreateExplorer(ResourceType.Nss, prompts, mutationLock);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(source).Should().BeTrue();
            explorer.StatusMessage.Should().Contain("packed, validated, or built");
        }

        [Test]
        public async Task FileChangedDuringConfirmation_PreservesTheNewGeneration()
        {
            const string resRef = "changed_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            File.WriteAllText(source, "void main() { // old\n}");
            var prompts = new RecordingPrompts(
                answer: true,
                onConfirm: () => File.WriteAllText(source, "void main() { // new\n}"));
            var (explorer, _) = CreateExplorer(ResourceType.Nss, prompts);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.ReadAllText(source).Should().Contain("// new");
            explorer.StatusMessage.Should().Contain("changed while the delete confirmation was open");
        }

        [Test]
        public void WorkspaceOpen_RollsBackDeleteInterruptedBetweenCompanionMoves()
        {
            var interrupted = SimulateInterruptedScriptDelete("interrupted_open");
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);

            workspace.Open(_module);

            File.ReadAllText(interrupted.Source).Should().Be("void main() { // original\n}");
            File.ReadAllBytes(interrupted.Compiled).Should().Equal(1, 2, 3);
            File.Exists(interrupted.Backup).Should().BeFalse();
            File.Exists(interrupted.Manifest).Should().BeFalse();
        }

        [Test]
        public void WorkspaceOpen_RestoresInterruptedAreaFilesAndIfoRegistration()
        {
            const string resRef = "interrupted_area";
            CopyAreaTemplate(resRef);
            var ifoPath = Path.GetFullPath(Path.Combine(_module, "ifo", "module.ifo.json"));
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"), ifoPath);
            var originalIfo = IfoDocument.Load(ifoPath);
            AreaTemplateFactory.AddAreaToModule(originalIfo, resRef).Should().BeTrue();
            var expectedIfo = originalIfo.ToBytes();
            File.WriteAllBytes(ifoPath, expectedIfo);

            var updatedIfoDocument = IfoDocument.Parse(expectedIfo);
            AreaTemplateFactory.RemoveAreaFromModule(updatedIfoDocument, resRef).Should().Be(1);
            var updatedIfo = updatedIfoDocument.ToBytes();
            var transactionId = Guid.NewGuid().ToString("N");
            var paths = new[]
            {
                Path.GetFullPath(Path.Combine(_module, "are", resRef + ".are.json")),
                Path.GetFullPath(Path.Combine(_module, "git", resRef + ".git.json")),
                Path.GetFullPath(Path.Combine(_module, "gic", resRef + ".gic.json"))
            };
            var entries = paths.Select(path => new
            {
                SourcePath = path,
                BackupPath = path + "." + transactionId + ".delete-backup",
                SourceSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
            }).ToArray();
            var manifest = Path.Combine(
                _module,
                "." + transactionId + ".resource-delete-transaction.json");
            File.WriteAllText(manifest, JsonSerializer.Serialize(new
            {
                Version = 1,
                TransactionId = transactionId,
                ModuleRoot = Path.GetFullPath(_module),
                Type = ResourceType.Area.ToString(),
                ResRef = resRef,
                Entries = entries,
                IfoPath = ifoPath,
                ExpectedIfoBase64 = Convert.ToBase64String(expectedIfo),
                UpdatedIfoSha256 = Convert.ToHexString(SHA256.HashData(updatedIfo))
            }));

            File.WriteAllBytes(ifoPath, updatedIfo);
            File.Move(entries[0].SourcePath, entries[0].BackupPath);
            new WorkspaceContext(root => new ModuleWorkspace(root), new OutputLogService()).Open(_module);

            paths.Should().OnlyContain(path => File.Exists(path));
            entries.Select(entry => entry.BackupPath).Should().OnlyContain(path => !File.Exists(path));
            File.Exists(manifest).Should().BeFalse();
            IfoDocument.Load(ifoPath).AreaResRefs.Should().Contain(resRef);
        }

        [Test]
        public async Task Pack_RollsBackInterruptedDeleteBeforeReadingModuleSources()
        {
            var interrupted = SimulateInterruptedScriptDelete("interrupted_pack");
            var log = new OutputLogService();

            var exitCode = await new PackService(log).PackAsync(_module);

            exitCode.Should().Be(-1, "the synthetic repository intentionally has no CLI project");
            File.Exists(interrupted.Source).Should().BeTrue();
            File.Exists(interrupted.Compiled).Should().BeTrue();
            File.Exists(interrupted.Backup).Should().BeFalse();
            File.Exists(interrupted.Manifest).Should().BeFalse();
        }

        [Test]
        public async Task WaitingForModuleLease_DoesNotBlockTheCallingThread()
        {
            const string resRef = "background_delete";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            File.WriteAllText(source, "void main() {}");
            var mutationLock = new ModuleMutationLock();
            var (explorer, _) = CreateExplorer(
                ResourceType.Nss,
                new RecordingPrompts(answer: true),
                mutationLock);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            using var acquired = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            Exception? holderFailure = null;
            var holder = new Thread(() =>
            {
                try
                {
                    using var lease = ModuleWriteLock.Acquire(_module);
                    acquired.Set();
                    release.Wait();
                }
                catch (Exception ex)
                {
                    holderFailure = ex;
                    acquired.Set();
                }
            });
            holder.Start();
            acquired.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            holderFailure.Should().BeNull();

            // A delayed release prevents a failed implementation from hanging the test for the
            // lock's full 30-second timeout. A UI-safe implementation returns from ExecuteAsync
            // immediately with an incomplete task while its worker waits for the lease.
            using var emergencyRelease = new CancellationTokenSource();
            var emergencyReleaseTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), emergencyRelease.Token);
                    release.Set();
                }
                catch (OperationCanceledException)
                {
                }
            });

            var stopwatch = Stopwatch.StartNew();
            var deleteTask = explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);
            stopwatch.Stop();

            try
            {
                stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
                deleteTask.IsCompleted.Should().BeFalse();
                explorer.IsDeletingResource.Should().BeTrue();
                mutationLock.IsResourceDeletionActive.Should().BeTrue(
                    "all editor-opening routes and application shutdown share this state");
                explorer.CanOpenSelectedType.Should().BeFalse(
                    "an editor opened while the delete waits could save the resource back afterward");
                explorer.OpenSelectedCommand.CanExecute(null).Should().BeFalse();
                explorer.CanCreateSelectedType.Should().BeFalse(
                    "creation must not race the prepared delete plan");
                explorer.CanCompileSelectedType.Should().BeFalse(
                    "compilation must not recreate a script output while its source is being deleted");
            }
            finally
            {
                release.Set();
                emergencyRelease.Cancel();
            }

            await deleteTask.WaitAsync(TimeSpan.FromSeconds(5));
            holder.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
            await emergencyReleaseTask;
            holderFailure.Should().BeNull();
            File.Exists(source).Should().BeFalse();
            explorer.IsDeletingResource.Should().BeFalse();
            mutationLock.IsResourceDeletionActive.Should().BeFalse();
        }

        [AvaloniaTest]
        public async Task WorkspaceOpenAsync_RecoveryLeaseWaitKeepsUiResponsive()
        {
            var uiThreadId = Environment.CurrentManagedThreadId;
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            int? openedThreadId = null;
            workspace.WorkspaceOpened += () => openedThreadId = Environment.CurrentManagedThreadId;

            using var acquired = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            Exception? holderFailure = null;
            var holder = new Thread(() =>
            {
                try
                {
                    using var lease = ModuleWriteLock.Acquire(_module);
                    acquired.Set();
                    release.Wait();
                }
                catch (Exception ex)
                {
                    holderFailure = ex;
                    acquired.Set();
                }
            });
            holder.Start();
            acquired.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            holderFailure.Should().BeNull();

            using var emergencyRelease = new CancellationTokenSource();
            var emergencyReleaseTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), emergencyRelease.Token);
                    release.Set();
                }
                catch (OperationCanceledException)
                {
                }
            });

            var openTask = workspace.OpenAsync(_module);
            try
            {
                openTask.IsCompleted.Should().BeFalse();
                var uiHeartbeat = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(() => uiHeartbeat.TrySetResult(true));
                await uiHeartbeat.Task.WaitAsync(TimeSpan.FromSeconds(1));
                openTask.IsCompleted.Should().BeFalse(
                    "recovery should still be waiting for the held module lease");
            }
            finally
            {
                release.Set();
                emergencyRelease.Cancel();
            }

            await openTask.WaitAsync(TimeSpan.FromSeconds(5));
            holder.Join(TimeSpan.FromSeconds(5)).Should().BeTrue();
            await emergencyReleaseTask;
            holderFailure.Should().BeNull();
            workspace.Workspace.Should().NotBeNull();
            openedThreadId.Should().Be(uiThreadId,
                "WorkspaceOpened subscribers own UI-thread-only editor state");
        }

        private (ModuleExplorerViewModel Explorer, CategoryService Categories) CreateExplorer(
            ResourceType type,
            IEditorPromptService prompts,
            ModuleMutationLock? mutationLock = null)
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            categories.Section(type)!.IsSeeded = true;
            categories.SaveChanges().Saved.Should().BeTrue();
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log,
                prompts: prompts,
                mutationLock: mutationLock)
            {
                SelectedType = type
            };
            explorer.Initialize();
            return (explorer, categories);
        }

        private static ExplorerNodeViewModel UnsortedResource(ModuleExplorerViewModel explorer, string resRef) =>
            explorer.Rows.Single(row => row.Name == "Unsorted").Children
                .Single(row => row.ResRef == resRef);

        private void CopyAreaTemplate(string targetResRef)
        {
            foreach (var (folder, extension) in new[]
                     {
                         ("are", "are.json"),
                         ("git", "git.json"),
                         ("gic", "gic.json")
                     })
            {
                File.Copy(
                    Path.Combine(CorpusLocator.ModuleDirectory, folder, "area_template." + extension),
                    Path.Combine(_module, folder, targetResRef + "." + extension));
            }
        }

        private InterruptedDelete SimulateInterruptedScriptDelete(string resRef)
        {
            var source = Path.GetFullPath(Path.Combine(_module, "nss", resRef + ".nss"));
            var compiled = Path.GetFullPath(Path.Combine(_module, "ncs", resRef + ".ncs"));
            File.WriteAllText(source, "void main() { // original\n}");
            File.WriteAllBytes(compiled, new byte[] { 1, 2, 3 });

            var transactionId = Guid.NewGuid().ToString("N");
            var backup = source + "." + transactionId + ".delete-backup";
            var manifest = Path.Combine(
                _module,
                "." + transactionId + ".resource-delete-transaction.json");
            var sourceSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source)));
            var compiledSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(compiled)));
            var compiledBackup = compiled + "." + transactionId + ".delete-backup";

            File.WriteAllText(manifest, JsonSerializer.Serialize(new
            {
                Version = 1,
                TransactionId = transactionId,
                ModuleRoot = Path.GetFullPath(_module),
                Type = ResourceType.Nss.ToString(),
                ResRef = resRef,
                Entries = new[]
                {
                    new { SourcePath = source, BackupPath = backup, SourceSha256 = sourceSha },
                    new
                    {
                        SourcePath = compiled,
                        BackupPath = compiledBackup,
                        SourceSha256 = compiledSha
                    }
                },
                IfoPath = (string?)null,
                ExpectedIfoBase64 = (string?)null,
                UpdatedIfoSha256 = (string?)null
            }));

            // Simulate a hard process exit after the first companion move. The normal catch block
            // never runs, so the durable manifest is the only way to restore the logical script.
            File.Move(source, backup);
            return new InterruptedDelete(source, compiled, backup, manifest);
        }

        private sealed record InterruptedDelete(
            string Source,
            string Compiled,
            string Backup,
            string Manifest);

        private sealed class RecordingPrompts(
            bool answer,
            Action? onConfirm = null) : IEditorPromptService
        {
            public string Message { get; private set; } = string.Empty;

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel)
            {
                Message = message;
                onConfirm?.Invoke();
                return Task.FromResult(answer);
            }

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) => Task.FromResult<string?>(null);
        }
    }
}
