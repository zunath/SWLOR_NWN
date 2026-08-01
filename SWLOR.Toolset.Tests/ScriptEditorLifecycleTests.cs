using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class ScriptEditorLifecycleTests
    {
        private string _directory = null!;
        private string _path = null!;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(
                Path.GetTempPath(), "swlor-script-editor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "test_script.nss");
            File.WriteAllText(_path, "void main() {}\n");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public async Task SaveWaitsForCompileOnSaveToFinish()
        {
            var editor = Editor();
            editor.OnTextChanged("void main() { int value = 1; }\n");
            var compileStarted = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var allowCompileToFinish = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            editor.CompileOnSave = async _ =>
            {
                compileStarted.SetResult();
                await allowCompileToFinish.Task;
                return true;
            };

            var save = editor.TrySaveAsync();
            await compileStarted.Task;

            save.IsCompleted.Should().BeFalse(
                "pack and subsequent builds must wait for the .ncs writer");

            allowCompileToFinish.SetResult();
            (await save).Should().BeTrue();
        }

        [Test]
        public async Task ExplicitCompilePathCanSuppressCompileOnSave()
        {
            var editor = Editor();
            editor.OnTextChanged("void main() { int value = 2; }\n");
            var compileCount = 0;
            editor.CompileOnSave = _ =>
            {
                compileCount++;
                return Task.FromResult(true);
            };

            (await editor.TrySaveAsync(compileOnSave: false)).Should().BeTrue();

            compileCount.Should().Be(0, "F7 performs the one explicit compile after saving");
            File.ReadAllText(_path).Should().Contain("void main() { int value = 2; }");
        }

        [Test]
        public async Task AFailedCompileOnSaveIsRetriedByTheNextSave()
        {
            var editor = Editor();
            editor.OnTextChanged("void main() { int value = 3; }\n");
            var compileSucceeds = false;
            var compileCount = 0;
            editor.CompileOnSave = _ =>
            {
                compileCount++;
                return Task.FromResult(compileSucceeds);
            };

            (await editor.TrySaveAsync()).Should().BeFalse("the .ncs was not updated");
            editor.HasPendingCompileFailure.Should().BeTrue(
                "application close must treat the stale .ncs as unsaved work");

            // The source is clean now, but the stale bytecode means the save is not done: the next
            // save must retry the compile instead of silently succeeding (which previously let a
            // second close attempt ship the stale .ncs).
            compileSucceeds = true;
            (await editor.TrySaveAsync()).Should().BeTrue();
            compileCount.Should().Be(2, "the clean-source save must retry the failed compile");
            editor.HasPendingCompileFailure.Should().BeFalse("a successful compile clears the pending state");

            (await editor.TrySaveAsync()).Should().BeTrue();
            compileCount.Should().Be(2, "a successful compile clears the pending retry");
        }

        [Test]
        public async Task ClosingCancelsThePendingDebouncedAnalysis()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(_ => throw new InvalidOperationException(), log);
            var language = new ScriptLanguageService(context, log);
            var editor = new ScriptEditorViewModel(
                _path, "test_script", log, new StubPrompts(), language);
            var callbacks = 0;
            editor.DiagnosticsChanged += _ => callbacks++;

            editor.OnTextChanged(editor.TextBinding);
            editor.OnClose().Should().BeTrue();
            await Task.Delay(400);

            callbacks.Should().Be(0);
        }

        [Test]
        public void ClosingDetachesTheEditorFromTheModuleMutationLock()
        {
            var editor = Editor();
            var mutationLock = new ModuleMutationLock();
            editor.MutationLock = mutationLock;

            editor.OnClose().Should().BeTrue();
            editor.MutationLock.Should().BeNull();

            var notifications = 0;
            editor.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ScriptEditorViewModel.CanCompile))
                    notifications++;
            };

            mutationLock.Set(true);

            notifications.Should().Be(0,
                "a closed editor must no longer be reachable or notified through the singleton lock");
        }

        [AvaloniaTest]
        public async Task DebouncedAnalysisRefreshesTheScriptOutline()
        {
            var log = new OutputLogService();
            var context = new WorkspaceContext(_ => throw new InvalidOperationException(), log);
            var language = new ScriptLanguageService(context, log);
            var editor = new ScriptEditorViewModel(
                _path, "test_script", log, new StubPrompts(), language);
            editor.AnalyzeNow();
            editor.OutlineFunctions.Should().Contain(function => function.Name == "main");

            var published = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            editor.DiagnosticsChanged += _ => published.TrySetResult();
            editor.OnTextChanged("void RenamedFunction() {}\n");

            await published.Task.WaitAsync(TimeSpan.FromSeconds(3));

            editor.OutlineFunctions.Should().ContainSingle()
                .Which.Name.Should().Be("RenamedFunction");
        }

        [Test]
        public void OutlineStartsCollapsedAndCanBeRestored()
        {
            var editor = Editor();

            editor.IsOutlineCollapsed.Should().BeTrue();

            editor.ToggleOutlineCommand.Execute(null);

            editor.IsOutlineCollapsed.Should().BeFalse();

            editor.ToggleOutlineCommand.Execute(null);

            editor.IsOutlineCollapsed.Should().BeTrue();
        }

        private ScriptEditorViewModel Editor() =>
            new(_path, "test_script", new OutputLogService(), new StubPrompts());

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
