using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Logging;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using System.Text;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Tlk;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests;

public class TlkEditorTests
{
    [Test]
    public async Task NavigationFilteringAndBlankSearchUseRawIdsAndCustomStrRefs()
    {
        var backend = new MemoryBackend(
            new Dictionary<int, string> { [0] = "Alpha", [2] = "Beta", [190000] = "Alpha Two" },
            referenced: new[] { 1 });
        var editor = CreateEditor(backend);

        editor.Rows.Count.Should().Be(190001);
        editor.GoToValue = (TlkService.CustomTlkBase + 2).ToString();
        editor.GoToRowCommand.Execute(null);
        editor.SelectedId.Should().Be(2);
        editor.SelectedStrRef.Should().Be(TlkService.CustomTlkBase + 2);
        var betaRow = (TlkEditorRowViewModel)editor.Rows[2]!;

        editor.FilterText = "alpha";
        editor.Rows.Count.Should().Be(2);
        editor.Rows.IndexOf(betaRow).Should().Be(-1,
            "IList.IndexOf must return -1 when a previously visible row is filtered out");
        ((TlkEditorRowViewModel)editor.Rows[0]!).Id.Should().Be(0);
        ((TlkEditorRowViewModel)editor.Rows[1]!).Id.Should().Be(190000);

        editor.ClearFilterCommand.Execute(null);
        editor.FilterText.Should().BeEmpty();
        editor.Rows.Count.Should().Be(190001);

        editor.FilterText = "alpha";
        editor.SelectId(190000, clearFilter: false);
        editor.SelectedId.Should().Be(190000);

        editor.SelectedText = "No longer matches";
        editor.SelectedId.Should().Be(190000,
            "editing must not redirect subsequent input when a filtered row stops matching");
        editor.Rows.Count.Should().Be(2,
            "the filtered snapshot is refreshed explicitly rather than on every keystroke");
        editor.Rows.Count.Should().Be(2);

        await editor.FindFirstBlankCommand.ExecuteAsync(null);
        editor.SelectedId.Should().Be(3, "row 1 is referenced and row 2 is populated");
        editor.FilterText.Should().BeEmpty("blank navigation must reveal its result in the grid");
    }

    [Test]
    public async Task TextEditingClearUndoRedoAndSaveFollowOneDocumentHistory()
    {
        var backend = new MemoryBackend(
            new Dictionary<int, string> { [4] = "Original" },
            referenced: new[] { 4 });
        var prompts = new StubPrompts { ConfirmDestructive = true };
        var afterSaveCount = 0;
        var editor = CreateEditor(backend, prompts, () => afterSaveCount++);
        editor.SelectId(4);

        editor.SelectedText = "Changed\nparagraph";
        editor.IsDirty.Should().BeTrue();
        backend.GetText(4).Should().Be("Changed\nparagraph");
        editor.Undo();
        backend.GetText(4).Should().Be("Original");
        editor.Redo();
        backend.GetText(4).Should().Be("Changed\nparagraph");

        await editor.ClearRowCommand.ExecuteAsync(null);
        backend.ContainsEntry(4).Should().BeFalse();
        prompts.DestructiveMessages.Should().ContainSingle(message => message.Contains("REFER", StringComparison.OrdinalIgnoreCase));
        editor.Undo();
        backend.GetText(4).Should().Be("Changed\nparagraph");

        (await editor.TrySaveAsync()).Should().BeTrue();
        backend.Saved.Should().BeTrue();
        backend.Published.Should().BeTrue();
        afterSaveCount.Should().Be(1);
        editor.IsDirty.Should().BeFalse();
    }

    [Test]
    public async Task GridClipboardRoundTripsMultilineRowsAndExternalTextFillsConsecutiveRows()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [10] = "one\ncontinued",
            [11] = "two"
        }, referenced: Enumerable.Range(0, 10));
        var prompts = new StubPrompts { ConfirmDestructive = true };
        var editor = CreateEditor(backend, prompts);

        var copied = editor.CopyRows(new[] { 10, 11, 12 });
        copied.Split(Environment.NewLine).Should().HaveCount(3,
            "one physical clipboard line represents each selected TLK row");
        copied.Should().NotEndWith(Environment.NewLine,
            "a trailing selected blank row must not be mistaken for a clipboard terminator");

        editor.SelectId(12);
        (await editor.PasteRowsAsync(copied)).Should().BeTrue();
        backend.GetText(12).Should().Be("one\ncontinued");
        backend.GetText(13).Should().Be("two");
        backend.ContainsEntry(14).Should().BeFalse();
        editor.Rows.Count.Should().Be(15,
            "the full pasted range remains navigable even when its final row is blank");

        editor.SelectId(13);
        (await editor.PasteRowsAsync("replacement\r\nnext\r\n")).Should().BeTrue();
        backend.GetText(13).Should().Be("replacement");
        backend.GetText(14).Should().Be("next");
        prompts.DestructiveMessages.Should().Contain(message => message.Contains("13: replacement"));

        editor.Undo();
        backend.GetText(13).Should().Be("two");
        backend.ContainsEntry(14).Should().BeFalse();
    }

    [Test]
    public async Task TypingAndPastingCannotSkipAnEarlierBlankRow()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [2] = "two"
        });
        var editor = CreateEditor(backend);

        editor.SelectId(3);
        editor.SelectedText = "append";

        backend.ContainsEntry(3).Should().BeFalse();
        editor.SelectedText.Should().BeEmpty();
        editor.NavigationStatus.Should().Contain("Blank row 1");
        (await editor.PasteRowsAsync("append")).Should().BeFalse();

        editor.SelectId(1);
        editor.SelectedText = "one";
        editor.SelectId(3);
        editor.SelectedText = "three";

        backend.GetText(1).Should().Be("one");
        backend.GetText(3).Should().Be("three");
    }

    [Test]
    public async Task PasteCanAppendWhenItFillsEveryEarlierBlankRow()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [3] = "old three"
        });
        var editor = CreateEditor(backend, new StubPrompts { ConfirmDestructive = true });
        editor.SelectId(1);

        (await editor.PasteRowsAsync("one\ntwo\nnew three\nfour")).Should().BeTrue();

        backend.GetText(1).Should().Be("one");
        backend.GetText(2).Should().Be("two");
        backend.GetText(3).Should().Be("new three");
        backend.GetText(4).Should().Be("four");
    }

    [Test]
    public async Task IncompleteReferenceCoverageFailsClosedForBlankAllocation()
    {
        var backend = new MemoryBackend(
            new Dictionary<int, string> { [0] = "used" },
            warnings: new[] { "broken.2da" });
        var editor = CreateEditor(backend);

        await editor.FindFirstBlankCommand.ExecuteAsync(null);

        editor.SelectedId.Should().Be(0);
        editor.NavigationStatus.Should().Contain("unavailable");

        editor.SelectId(1);
        editor.SelectedText = "new row";

        backend.ContainsEntry(1).Should().BeFalse();
        editor.NavigationStatus.Should().Contain("unavailable");
    }

    [Test]
    public async Task BlankAndClearOperationsRefreshReferencesAddedAfterOpen()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [2] = "two"
        })
        {
            ReferencesAfterRefresh = new[] { 1, 2 }
        };
        var prompts = new StubPrompts { ConfirmDestructive = false };
        var editor = CreateEditor(backend, prompts);

        await editor.FindFirstBlankCommand.ExecuteAsync(null);

        editor.SelectedId.Should().Be(3, "row 1 became referenced after the editor opened");
        editor.SelectId(2);
        await editor.ClearRowCommand.ExecuteAsync(null);

        backend.ContainsEntry(2).Should().BeTrue();
        prompts.DestructiveMessages.Should().ContainSingle(message => message.Contains("row 2"));
        backend.ReferenceRefreshCount.Should().Be(2);
    }

    [Test]
    public async Task EmptyTextRequiresClearAndSaveRechecksRowsReferencedAfterClearing()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [4] = "four" });
        var prompts = new StubPrompts { ConfirmDestructive = true };
        var editor = CreateEditor(backend, prompts);
        editor.SelectId(4);

        editor.SelectedText = string.Empty;

        backend.ContainsEntry(4).Should().BeTrue();
        editor.NavigationStatus.Should().Contain("Clear row");
        prompts.DestructiveMessages.Should().BeEmpty();

        await editor.ClearRowCommand.ExecuteAsync(null);
        backend.ContainsEntry(4).Should().BeFalse();
        prompts.DestructiveMessages.Should().BeEmpty("the row was unreferenced when it was cleared");

        backend.ReferencesAfterRefresh = new[] { 4 };
        (await editor.TrySaveAsync()).Should().BeTrue();

        prompts.DestructiveMessages.Should().ContainSingle(message =>
            message.Contains("now referenced", StringComparison.OrdinalIgnoreCase));
        backend.Saved.Should().BeTrue();
    }

    [Test]
    public void LocStringShortcutOnlyOpensCustomTlkRows()
    {
        uint? opened = null;
        var descriptor = new FieldDescriptor
        {
            Label = "Name",
            FieldName = "Name",
            Kind = EditorKind.LocString,
            FieldType = GffFieldType.CExoLocString
        };
        var customStrRef = TlkService.CustomTlkBase + 42;
        var customDocument = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
            $"{{\"__data_type\":\"UTI \",\"Name\":{{\"id\":{customStrRef},\"type\":\"cexolocstring\",\"value\":{{}}}}}}"));
        var custom = new LocStringFieldViewModel(
            descriptor,
            new EditorFieldContext(customDocument, (_, mutation) => { mutation(); return true; },
                openTlkRow: strRef => opened = strRef));

        custom.CanOpenTlkRow.Should().BeTrue();
        custom.OpenTlkRowCommand.Execute(null);
        opened.Should().Be(customStrRef);

        var baseDocument = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
            "{\"__data_type\":\"UTI \",\"Name\":{\"id\":42,\"type\":\"cexolocstring\",\"value\":{}}}"));
        var baseField = new LocStringFieldViewModel(
            descriptor,
            new EditorFieldContext(baseDocument, (_, mutation) => { mutation(); return true; },
                openTlkRow: _ => { }));
        baseField.CanOpenTlkRow.Should().BeFalse();
        baseField.OpenTlkRowCommand.CanExecute(null).Should().BeFalse();

        var unavailableCustomField = new LocStringFieldViewModel(
            descriptor,
            new EditorFieldContext(customDocument, (_, mutation) => { mutation(); return true; }));
        unavailableCustomField.CanOpenTlkRow.Should().BeFalse(
            "the shortcut must remain disabled when the TLK editor source is unavailable");
    }

    [Test]
    public async Task ReloadChoiceDiscardsHistoryWhenTheSourceChangedExternally()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "old" });
        var prompts = new StubPrompts { ExternalChoice = ExternalChangeChoice.Reload };
        var editor = CreateEditor(backend, prompts);
        editor.SelectedText = "unsaved";
        backend.ExternalChange = true;
        backend.ReloadEntries = new Dictionary<int, string> { [0] = "external" };
        backend.ReloadWarnings = new[] { "newly-unscannable.2da" };

        (await editor.TrySaveAsync()).Should().BeTrue();

        editor.SelectedText.Should().Be("external");
        editor.IsDirty.Should().BeFalse();
        backend.Saved.Should().BeFalse();
        backend.ReloadCount.Should().Be(1, "the worker reload must also be the generation applied to the view");
        editor.ReferenceStatus.Should().Contain("1 file");
    }

    [Test]
    public async Task FailedExternalReloadKeepsTheUnsavedEditorGeneration()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "old" })
        {
            ExternalChange = true,
            ReloadEntries = new Dictionary<int, string> { [0] = "external" },
            ReloadFailure = new InvalidDataException("torn pair")
        };
        var prompts = new StubPrompts { ExternalChoice = ExternalChangeChoice.Reload };
        var editor = CreateEditor(backend, prompts);
        editor.SelectedText = "unsaved";

        (await editor.TrySaveAsync()).Should().BeFalse();

        backend.ReloadCount.Should().Be(1);
        editor.SelectedText.Should().Be("unsaved");
        editor.IsDirty.Should().BeTrue();
        backend.Published.Should().BeFalse();
    }

    [Test]
    public async Task CleanSaveStillRegeneratesAndPublishesTheTlkPair()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "unchanged" });
        var editor = CreateEditor(backend);

        (await editor.TrySaveAsync()).Should().BeTrue();

        backend.SaveCount.Should().Be(1);
        backend.Published.Should().BeTrue();
        editor.IsDirty.Should().BeFalse();
    }

    [Test]
    public async Task ConcurrentSaveRequestsShareOneBackendTransaction()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "old" })
        {
            SaveStarted = new ManualResetEventSlim(),
            ContinueSave = new ManualResetEventSlim()
        };
        var editor = CreateEditor(backend);
        editor.SelectedText = "new";

        var first = editor.TrySaveAsync();
        backend.SaveStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        var second = editor.TrySaveAsync();
        backend.ContinueSave.Set();

        (await Task.WhenAll(first, second)).Should().OnlyContain(result => result);
        backend.SaveCount.Should().Be(1);
        editor.IsDirty.Should().BeFalse();
    }

    [Test]
    public async Task ApplicationCloseApprovalCannotBypassAnActiveTlkSave()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "old" })
        {
            SaveStarted = new ManualResetEventSlim(),
            ContinueSave = new ManualResetEventSlim()
        };
        var editor = CreateEditor(backend);
        editor.SelectedText = "new";
        var closeRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editor.CloseRequested += _ => closeRequested.TrySetResult();

        var save = editor.TrySaveAsync();
        backend.SaveStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        typeof(TlkEditorDocumentViewModel)
            .GetMethod("ApproveApplicationClose", System.Reflection.BindingFlags.Instance |
                                                   System.Reflection.BindingFlags.NonPublic)!
            .Invoke(editor, null);

        editor.OnClose().Should().BeFalse("the paired transaction is still active");
        closeRequested.Task.IsCompleted.Should().BeFalse();

        backend.ContinueSave.Set();
        (await save).Should().BeTrue();
        await closeRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public void ProductionBackendWritesAndVerifiesTheRepositoryJsonBinaryPair()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-editor-tests", Guid.NewGuid().ToString("N"));
        var tlkDirectory = Path.Combine(root, "sw_tlk");
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        Directory.CreateDirectory(tlkDirectory);
        Directory.CreateDirectory(twoDaDirectory);
        var jsonPath = Path.Combine(tlkDirectory, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(tlkDirectory, "sw_tlk.tlk");
        File.WriteAllText(jsonPath,
            "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"zero\"}]}");
        File.WriteAllText(Path.Combine(twoDaDirectory, "test.2da"),
            "2DA V2.0\n\nLABEL STRREF\n0 test 16777216\n");

        try
        {
            var backend = new TlkEditorBackend(new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory));
            backend.SetText(2, "two\nlines");
            backend.Save();

            TlkDocument.Load(jsonPath).GetText(2).Should().Be("two\nlines");
            var binary = TlkReader.Read(binaryPath);
            binary.Entries.Should().HaveCount(3);
            binary.GetString(2).Should().Be("two\nlines");
            backend.HasExternalChange().Should().BeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ProductionBackendRefreshesReferencesChangedAfterOpen()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-reference-refresh-tests", Guid.NewGuid().ToString("N"));
        var tlkDirectory = Path.Combine(root, "sw_tlk");
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        Directory.CreateDirectory(tlkDirectory);
        Directory.CreateDirectory(twoDaDirectory);
        var jsonPath = Path.Combine(tlkDirectory, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(tlkDirectory, "sw_tlk.tlk");
        var twoDaPath = Path.Combine(twoDaDirectory, "test.2da");
        File.WriteAllText(jsonPath,
            "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"zero\"}]}");
        File.WriteAllText(twoDaPath,
            "2DA V2.0\n\nLABEL STRREF\n0 test ****\n");

        try
        {
            var backend = new TlkEditorBackend(new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory));
            backend.IsReferenced(1).Should().BeFalse();
            File.WriteAllText(twoDaPath,
                $"2DA V2.0\n\nLABEL STRREF\n0 test {TlkService.CustomTlkBase + 1}\n");

            backend.RefreshReferences();

            backend.IsReferenced(1).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ProductionReloadRejectsAnExternallyTornJsonBinaryPair()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-reload-tests", Guid.NewGuid().ToString("N"));
        var tlkDirectory = Path.Combine(root, "sw_tlk");
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        Directory.CreateDirectory(tlkDirectory);
        Directory.CreateDirectory(twoDaDirectory);
        var jsonPath = Path.Combine(tlkDirectory, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(tlkDirectory, "sw_tlk.tlk");
        File.WriteAllText(jsonPath,
            "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"old\"}]}");
        File.WriteAllBytes(binaryPath, TlkWriter.Write(0, new Dictionary<int, string> { [0] = "old" }));

        try
        {
            var backend = new TlkEditorBackend(new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory));
            File.WriteAllText(jsonPath,
                "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"external\"}]}");

            var reload = () => backend.Reload();

            reload.Should().Throw<InvalidDataException>();
            backend.GetText(0).Should().Be("old", "a rejected external generation must not replace editor state");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ProductionPublishUsesTheExactVerifiedReloadGeneration()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-publish-tests", Guid.NewGuid().ToString("N"));
        var tlkDirectory = Path.Combine(root, "sw_tlk");
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        Directory.CreateDirectory(tlkDirectory);
        Directory.CreateDirectory(twoDaDirectory);
        var jsonPath = Path.Combine(tlkDirectory, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(tlkDirectory, "sw_tlk.tlk");
        WritePair(jsonPath, binaryPath, "old");
        var service = new TlkService(TlkJsonFile.Parse(
            "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"fallback\"}]}"));

        try
        {
            var backend = new TlkEditorBackend(
                new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory),
                service);
            WritePair(jsonPath, binaryPath, "accepted A");
            backend.Reload();

            WritePair(jsonPath, binaryPath, "later B");
            backend.Publish();

            backend.GetText(0).Should().Be("accepted A");
            service.GetCustomText(0).Should().Be("accepted A",
                "publication must use the verified snapshot rather than rereading the changed path");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WritePair(string jsonPath, string binaryPath, string text)
    {
        File.WriteAllText(jsonPath,
            $"{{\"language\":0,\"entries\":[{{\"id\":0,\"text\":\"{text}\"}}]}}");
        File.WriteAllBytes(binaryPath, TlkWriter.Write(0, new Dictionary<int, string> { [0] = text }));
    }

    [AvaloniaTest]
    public void EditorViewRendersAVirtualizedGridWithoutBindingErrors()
    {
        var previousSink = Logger.Sink;
        var sink = new CountingSink();
        Logger.Sink = sink;
        try
        {
            var backend = new MemoryBackend(new Dictionary<int, string> { [199999] = "last" });
            var editor = CreateEditor(backend);
            var view = new TlkEditorDocumentView { DataContext = editor };
            var window = new Window { Width = 1280, Height = 820, Content = view };
            window.Show();
            window.UpdateLayout();

            var grid = view.FindControl<ListBox>("RowGrid")!;
            view.FindControl<Button>("ClearFilterButton")!.Content.Should().Be("×");
            grid.ItemsSource.Should().BeSameAs(editor.Rows);
            grid.GetVisualDescendants().OfType<ListBoxItem>().Count().Should().BeLessThan(100,
                "only visible rows should be realized from the 200,000-row virtual range");
            editor.Rows.CreatedRowCount.Should().BeLessThan(100,
                "the item source itself must not be eagerly copied into a collection view");
            view.GetVisualDescendants().OfType<TextBox>().Should().NotBeEmpty();

            window.Close();
            editor.OnClose().Should().BeTrue();
        }
        finally
        {
            Logger.Sink = previousSink;
        }

        sink.Errors.Should().BeEmpty();
    }

    [AvaloniaTest]
    public void ToolsMenuContainsTheTlkEditorEntryPoint()
    {
        var settingsPath = Path.Combine(Path.GetTempPath(), $"swlor-toolset-tlk-{Guid.NewGuid():N}.json");
        var window = new MainWindow(ToolsetSettings.Load(settingsPath));

        window.FindControl<MenuItem>("TlkEditorMenuItem")!.Header.Should().Be("_TLK Editor...");
    }

    private static TlkEditorDocumentViewModel CreateEditor(
        MemoryBackend backend,
        StubPrompts? prompts = null,
        Action? afterSave = null) =>
        new(backend, new OutputLogService(), prompts ?? new StubPrompts(), afterSave);

    private sealed class MemoryBackend : ITlkEditorBackend
    {
        private Dictionary<int, string> _entries;
        private readonly HashSet<int> _referenced;

        public MemoryBackend(
            Dictionary<int, string> entries,
            IEnumerable<int>? referenced = null,
            IReadOnlyList<string>? warnings = null)
        {
            _entries = new Dictionary<int, string>(entries);
            _referenced = referenced?.ToHashSet() ?? new HashSet<int>();
            ReferenceWarnings = warnings ?? Array.Empty<string>();
        }

        public string JsonPath => "memory/sw_tlk.tlk.json";
        public string BinaryPath => "memory/sw_tlk.tlk";
        public int Language => 0;
        public int Count => _entries.Count;
        public int MaxEntryId => _entries.Count == 0 ? -1 : _entries.Keys.Max();
        public IReadOnlyList<TlkEditorEntry> Entries => _entries.OrderBy(pair => pair.Key)
            .Select(pair => new TlkEditorEntry(pair.Key, pair.Value)).ToArray();
        public IReadOnlyList<string> ReferenceWarnings { get; private set; }
        public bool ExternalChange { get; set; }
        public Dictionary<int, string>? ReloadEntries { get; set; }
        public IReadOnlyList<string>? ReloadWarnings { get; set; }
        public IReadOnlyCollection<int>? ReferencesAfterRefresh { get; set; }
        public Exception? ReferenceRefreshFailure { get; set; }
        public bool Saved { get; private set; }
        public bool Published { get; private set; }
        public int SaveCount { get; private set; }
        public int ReloadCount { get; private set; }
        public int ReferenceRefreshCount { get; private set; }
        public Exception? ReloadFailure { get; init; }
        public ManualResetEventSlim? SaveStarted { get; init; }
        public ManualResetEventSlim? ContinueSave { get; init; }

        public bool ContainsEntry(int id) => _entries.ContainsKey(id);
        public string? GetText(int id) => _entries.GetValueOrDefault(id);
        public void SetText(int id, string text)
        {
            if (text.Length == 0)
                _entries.Remove(id);
            else
                _entries[id] = text;
        }
        public bool Clear(int id) => _entries.Remove(id);
        public bool IsReferenced(int id) => _referenced.Contains(id);
        public int UsageCountFor(int id) => IsReferenced(id) ? 1 : 0;
        public IReadOnlyList<TlkEditorUsage> UsagesOf(int id) => IsReferenced(id)
            ? new[] { new TlkEditorUsage("test.2da", 7, "label", "STRREF", TlkService.CustomTlkBase + (uint)id) }
            : Array.Empty<TlkEditorUsage>();
        public int FindFirstAvailableBlank() => FindNextAvailableBlank(-1);
        public int FindNextAvailableBlank(int currentId)
        {
            for (var id = currentId + 1; id <= Math.Max(MaxEntryId + 1, currentId + 1); id++)
            {
                if (!ContainsEntry(id) && !IsReferenced(id))
                    return id;
            }
            throw new InvalidOperationException();
        }
        public void RefreshReferences()
        {
            ReferenceRefreshCount++;
            if (ReferenceRefreshFailure != null)
                throw ReferenceRefreshFailure;
            if (ReferencesAfterRefresh == null)
                return;
            _referenced.Clear();
            _referenced.UnionWith(ReferencesAfterRefresh);
        }
        public bool HasExternalChange() => ExternalChange;
        public void Reload()
        {
            ReloadCount++;
            if (ReloadFailure != null)
                throw ReloadFailure;
            if (ReloadEntries != null)
                _entries = new Dictionary<int, string>(ReloadEntries);
            if (ReloadWarnings != null)
                ReferenceWarnings = ReloadWarnings;
            ExternalChange = false;
        }
        public void Save(bool overwriteExternalChanges = false)
        {
            SaveCount++;
            SaveStarted?.Set();
            ContinueSave?.Wait(TimeSpan.FromSeconds(5));
            Saved = true;
            ExternalChange = false;
        }
        public void Publish() => Published = true;
    }

    private sealed class StubPrompts : IEditorPromptService
    {
        public ExternalChangeChoice ExternalChoice { get; set; } = ExternalChangeChoice.Cancel;
        public bool ConfirmDestructive { get; set; }
        public List<string> DestructiveMessages { get; } = new();

        public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
            Task.FromResult(ExternalChoice);
        public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
            Task.FromResult(UnsavedChangesChoice.Cancel);
        public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel)
        {
            DestructiveMessages.Add(headline + "\n" + message);
            return Task.FromResult(ConfirmDestructive);
        }
        public Task<string?> PromptForTextAsync(
            string headline, string message, string initialValue, string confirmLabel) =>
            Task.FromResult<string?>(null);
    }

    private sealed class CountingSink : ILogSink
    {
        public List<string> Errors { get; } = new();
        public bool IsEnabled(LogEventLevel level, string area) =>
            level >= LogEventLevel.Warning && area == LogArea.Binding;
        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            if (IsEnabled(level, area))
                Errors.Add(messageTemplate);
        }
        public void Log(
            LogEventLevel level,
            string area,
            object? source,
            string messageTemplate,
            params object?[] values)
        {
            if (IsEnabled(level, area))
                Errors.Add(messageTemplate + " | " + string.Join(", ", values));
        }
    }
}
