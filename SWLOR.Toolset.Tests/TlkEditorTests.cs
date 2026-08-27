using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Logging;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using System.Text;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Tlk;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell;
using SWLOR.Toolset.Shell.Panels;
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
        var editRequests = 0;
        bool? wasBusyWhenEditRequested = null;
        editor.EntryEditRequested += () =>
        {
            editRequests++;
            wasBusyWhenEditRequested = editor.IsBusy;
        };

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

        await editor.AddRowCommand.ExecuteAsync(null);
        editor.SelectedId.Should().Be(3, "row 1 is referenced and row 2 is populated");
        editor.FilterText.Should().BeEmpty("blank navigation must reveal its result in the grid");
        editor.NavigationStatus.Should().Contain("ready for a new TLK entry");
        editor.RemoveRowCommand.CanExecute(null).Should().BeFalse();
        editRequests.Should().Be(1);
        wasBusyWhenEditRequested.Should().BeFalse("the text editor must be enabled before focus is requested");
    }

    [Test]
    public async Task TextEditingRemoveUndoRedoAndSaveFollowOneDocumentHistory()
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

        await editor.RemoveRowCommand.ExecuteAsync(null);
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
    public async Task RemoveRowLeavesLaterRowIdsUnchangedAndIsUndoable()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [2] = "two"
        });
        var editor = CreateEditor(backend);
        editor.SelectId(0);

        editor.RemoveRowCommand.CanExecute(null).Should().BeTrue();
        await editor.RemoveRowCommand.ExecuteAsync(null);

        backend.ContainsEntry(0).Should().BeFalse();
        backend.GetText(2).Should().Be("two");
        editor.SelectedId.Should().Be(0);
        editor.RemoveRowCommand.CanExecute(null).Should().BeFalse();

        editor.Undo();
        backend.GetText(0).Should().Be("zero");
        backend.GetText(2).Should().Be("two");
        editor.RemoveRowCommand.CanExecute(null).Should().BeTrue();
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
    public async Task GridPasteTreatsOneTrailingCarriageReturnAsAClipboardSeparator()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [5] = "five",
            [6] = "six"
        });
        var editor = CreateEditor(backend, new StubPrompts { ConfirmDestructive = true });
        editor.SelectId(5);

        (await editor.PasteRowsAsync("replacement\r")).Should().BeTrue();

        backend.GetText(5).Should().Be("replacement");
        backend.GetText(6).Should().Be("six", "the clipboard terminator is not another pasted row");
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
    public async Task SaveRevalidatesAppendedRowsAfterAnEarlierReferenceIsRemoved()
    {
        var backend = new MemoryBackend(
            new Dictionary<int, string>
            {
                [0] = "zero",
                [2] = "two"
            },
            referenced: new[] { 1 })
        {
            ReferencesAfterRefresh = Array.Empty<int>()
        };
        var editor = CreateEditor(backend);
        editor.SelectId(3);

        editor.SelectedText = "three";
        backend.GetText(3).Should().Be("three",
            "the cached reference initially makes the earlier blank unavailable");

        (await editor.TrySaveAsync()).Should().BeFalse();

        backend.Saved.Should().BeFalse();
        editor.NavigationStatus.Should().Contain("Blank row 1");
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

        await editor.AddRowCommand.ExecuteAsync(null);

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

        await editor.AddRowCommand.ExecuteAsync(null);

        editor.SelectedId.Should().Be(3, "row 1 became referenced after the editor opened");
        editor.SelectId(2);
        await editor.RemoveRowCommand.ExecuteAsync(null);

        backend.ContainsEntry(2).Should().BeTrue();
        prompts.DestructiveMessages.Should().ContainSingle(message => message.Contains("row 2"));
        backend.ReferenceRefreshCount.Should().Be(2);
    }

    [Test]
    public async Task ClearKeepsTheInvokedRowWhenNavigationChangesDuringReferenceRefresh()
    {
        using var refreshStarted = new ManualResetEventSlim();
        using var continueRefresh = new ManualResetEventSlim();
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [1] = "one"
        })
        {
            ReferenceRefreshStarted = refreshStarted,
            ContinueReferenceRefresh = continueRefresh
        };
        var editor = CreateEditor(backend);
        editor.SelectId(0);

        var clear = editor.RemoveRowCommand.ExecuteAsync(null);
        try
        {
            refreshStarted.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
            editor.SelectId(1);
        }
        finally
        {
            continueRefresh.Set();
        }
        await clear;

        backend.ContainsEntry(0).Should().BeFalse();
        backend.GetText(1).Should().Be("one");
        editor.SelectedId.Should().Be(1);
    }

    [Test]
    public void TypingCannotPopulateAKnownReferencedBlankRowWithoutConfirmation()
    {
        var backend = new MemoryBackend(
            new Dictionary<int, string> { [0] = "zero", [2] = "two" },
            referenced: new[] { 1 });
        var editor = CreateEditor(backend);
        editor.SelectId(1);

        editor.SelectedText = "one";

        backend.ContainsEntry(1).Should().BeFalse();
        editor.SelectedText.Should().BeEmpty();
        editor.NavigationStatus.Should().Contain("grid paste");
    }

    [Test]
    public async Task SaveConfirmsWhenAJustPopulatedBlankBecomesReferenced()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [2] = "two"
        });
        var prompts = new StubPrompts { ConfirmDestructive = true };
        var editor = CreateEditor(backend, prompts);
        editor.SelectId(1);
        editor.SelectedText = "one";
        backend.ReferencesAfterRefresh = new[] { 1 };

        (await editor.TrySaveAsync()).Should().BeTrue();

        prompts.DestructiveMessages.Should().ContainSingle(message =>
            message.Contains("newly populated", StringComparison.OrdinalIgnoreCase));
        backend.Saved.Should().BeTrue();
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
        editor.NavigationStatus.Should().Contain("Remove row");
        prompts.DestructiveMessages.Should().BeEmpty();

        await editor.RemoveRowCommand.ExecuteAsync(null);
        backend.ContainsEntry(4).Should().BeFalse();
        prompts.DestructiveMessages.Should().BeEmpty("the row was unreferenced when it was cleared");

        backend.ReferencesAfterRefresh = new[] { 4 };
        (await editor.TrySaveAsync()).Should().BeTrue();

        prompts.DestructiveMessages.Should().ContainSingle(message =>
            message.Contains("now referenced", StringComparison.OrdinalIgnoreCase));
        backend.Saved.Should().BeTrue();
    }

    [Test]
    public async Task SavingAfterUndoAcrossTheSavedPositionConfirmsTheNewClear()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "zero" });
        var prompts = new StubPrompts { ConfirmDestructive = true };
        var editor = CreateEditor(backend, prompts);
        editor.SelectId(1);
        editor.SelectedText = "one";

        (await editor.TrySaveAsync()).Should().BeTrue();
        editor.Undo();
        backend.ContainsEntry(1).Should().BeFalse();
        backend.ReferencesAfterRefresh = new[] { 1 };

        (await editor.TrySaveAsync()).Should().BeTrue();

        prompts.DestructiveMessages.Should().ContainSingle(message =>
            message.Contains("cleared", StringComparison.OrdinalIgnoreCase) && message.Contains("1"));
        backend.LastSavedEntries.Should().NotContainKey(1);
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
    public async Task ExternalReloadKeepsTheFilterAppliedWhenThePreviousRowNoLongerMatches()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "alpha old",
            [1] = "beta"
        });
        var prompts = new StubPrompts { ExternalChoice = ExternalChangeChoice.Reload };
        var editor = CreateEditor(backend, prompts);
        editor.FilterText = "alpha";
        editor.SelectedId.Should().Be(0);
        backend.ExternalChange = true;
        backend.ReloadEntries = new Dictionary<int, string>
        {
            [0] = "changed",
            [1] = "alpha new"
        };

        (await editor.TrySaveAsync()).Should().BeTrue();

        editor.FilterText.Should().Be("alpha");
        editor.Rows.Count.Should().Be(1);
        ((TlkEditorRowViewModel)editor.Rows[0]!).Id.Should().Be(1);
        editor.SelectedId.Should().Be(1);
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
    public async Task ClosingWaitsForANonSaveReferenceRefresh()
    {
        var backend = new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [2] = "two"
        })
        {
            ReferenceRefreshStarted = new ManualResetEventSlim(),
            ContinueReferenceRefresh = new ManualResetEventSlim()
        };
        var editor = CreateEditor(backend);
        var closeRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        editor.CloseRequested += _ => closeRequested.TrySetResult();

        var find = editor.AddRowCommand.ExecuteAsync(null);
        backend.ReferenceRefreshStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

        editor.OnClose().Should().BeFalse("the reference refresh is still active");
        closeRequested.Task.IsCompleted.Should().BeFalse();

        backend.ContinueReferenceRefresh.Set();
        await find;
        await closeRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task SaveWaitsForAnInProgressPasteBeforeCapturingTheDocument()
    {
        var backend = new MemoryBackend(new Dictionary<int, string> { [0] = "old" })
        {
            ReferenceRefreshStarted = new ManualResetEventSlim(),
            ContinueReferenceRefresh = new ManualResetEventSlim(),
            SaveStarted = new ManualResetEventSlim()
        };
        var editor = CreateEditor(backend, new StubPrompts { ConfirmDestructive = true });

        var paste = editor.PasteRowsAsync("new");
        backend.ReferenceRefreshStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        var save = editor.TrySaveAsync();

        backend.SaveStarted.IsSet.Should().BeFalse("save must wait behind the active paste");
        backend.ContinueReferenceRefresh.Set();

        (await paste).Should().BeTrue();
        (await save).Should().BeTrue();
        backend.LastSavedEntries.Should().ContainKey(0).WhoseValue.Should().Be("new");
        editor.IsDirty.Should().BeFalse();
    }

    [Test]
    public async Task NavigatingAnAlreadyOpenTlkEditorClearsAnyPendingRequest()
    {
        var log = new OutputLogService();
        var workspace = new WorkspaceContext(
            root => new SWLOR.Toolset.Domain.Workspace.ModuleWorkspace(root),
            log);
        var prompts = new StubPrompts();
        var factory = new ToolsetDockFactory(
            null!, null!, null!, null!, null!, null!, null!, null!, null!);
        var service = new EditorService(
            workspace,
            new LookupOptionProvider(workspace),
            log,
            factory,
            prompts);
        var openEditor = CreateEditor(new MemoryBackend(new Dictionary<int, string>
        {
            [0] = "zero",
            [2] = "two"
        }));
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(EditorService).GetField("_tlkEditor", flags)!.SetValue(service, openEditor);
        typeof(EditorService).GetField("_pendingTlkStrRef", flags)!
            .SetValue(service, TlkService.CustomTlkBase + 1);

        await service.OpenTlkEditorAsync(TlkService.CustomTlkBase + 2);

        openEditor.SelectedId.Should().Be(2);
        typeof(EditorService).GetField("_pendingTlkStrRef", flags)!.GetValue(service).Should().BeNull();
    }

    [Test]
    public void ReferenceRowsPutStructuredTwoDaUsagesBeforeRepositoryText()
    {
        var strRef = TlkService.CustomTlkBase;
        var backend = new MemoryBackend(
            new Dictionary<int, string> { [0] = "used" },
            referenced: new[] { 0 },
            usages:
            [
                new TlkEditorUsage(
                    "Module/sample.json", 12, "12", TlkReferenceIndex.RepositoryTextColumnName, strRef),
                new TlkEditorUsage("spells.2da", 4, "spell_label", "SpellDesc", strRef)
            ]);

        var editor = CreateEditor(backend);

        editor.Usages.Select(usage => usage.Source).Should().Equal("2DA", "Repository");
        editor.Usages.Select(usage => usage.File).Should().Equal("spells.2da", "Module/sample.json");
    }

    [Test]
    public async Task OpeningShowsALoadingDocumentBeforeReferenceIndexingFinishes()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-loading-tests", Guid.NewGuid().ToString("N"));
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        var jsonPath = Path.Combine(root, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(root, "sw_tlk.tlk");
        Directory.CreateDirectory(twoDaDirectory);
        File.WriteAllText(jsonPath, "{\"language\":0,\"entries\":[]}");
        var indexingStarted = new ManualResetEventSlim();
        var continueIndexing = new ManualResetEventSlim();
        var backend = new MemoryBackend(new Dictionary<int, string>());

        try
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(
                path => new SWLOR.Toolset.Domain.Workspace.ModuleWorkspace(path),
                log);
            var service = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                new ToolsetDockFactory(null!, null!, null!, null!, null!, null!, null!, null!, null!),
                new StubPrompts(),
                tlkEditorSource: new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory),
                tlkEditorBackendFactory: (_, _, cancellationToken) =>
                {
                    indexingStarted.Set();
                    continueIndexing.Wait(cancellationToken);
                    return backend;
                });

            var opening = service.OpenTlkEditorAsync();
            indexingStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(EditorService).GetField("_tlkEditorLoading", flags)!.GetValue(service)
                .Should().BeOfType<TlkEditorLoadingDocumentViewModel>();
            typeof(EditorService).GetField("_tlkEditor", flags)!.GetValue(service).Should().BeNull();

            continueIndexing.Set();
            await opening;

            typeof(EditorService).GetField("_tlkEditorLoading", flags)!.GetValue(service).Should().BeNull();
            typeof(EditorService).GetField("_tlkEditor", flags)!.GetValue(service)
                .Should().BeOfType<TlkEditorDocumentViewModel>();
        }
        finally
        {
            continueIndexing.Set();
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void APreviouslyCapturedTlkRowOpenerRechecksTheModuleLockWhenInvoked()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-opener-lock-tests", Guid.NewGuid().ToString("N"));
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        var jsonPath = Path.Combine(root, "sw_tlk.tlk.json");
        Directory.CreateDirectory(twoDaDirectory);
        File.WriteAllText(jsonPath, "{\"language\":0,\"entries\":[]}");

        try
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(
                path => new SWLOR.Toolset.Domain.Workspace.ModuleWorkspace(path),
                log);
            var mutationLock = new ModuleMutationLock();
            var backendCreations = 0;
            var service = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                new ToolsetDockFactory(null!, null!, null!, null!, null!, null!, null!, null!, null!),
                new StubPrompts(),
                mutationLock: mutationLock,
                tlkEditorSource: new TlkEditorSource(
                    jsonPath,
                    Path.Combine(root, "sw_tlk.tlk"),
                    twoDaDirectory),
                tlkEditorBackendFactory: (_, _, _) =>
                {
                    Interlocked.Increment(ref backendCreations);
                    return new MemoryBackend(new Dictionary<int, string>());
                });
            var opener = (Action<uint>)typeof(EditorService)
                .GetProperty("TlkRowOpener", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)!
                .GetValue(service)!;

            using (mutationLock.BeginResourceDeletion())
                opener(TlkService.CustomTlkBase);

            backendCreations.Should().Be(0);
            log.Lines.Should().Contain(line => line.Contains(
                "module resource deletion is in progress", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task OpeningTheEditorPublishesTheCurrentRepositoryGeneration()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-open-publish-tests", Guid.NewGuid().ToString("N"));
        var tlkDirectory = Path.Combine(root, "sw_tlk");
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        Directory.CreateDirectory(tlkDirectory);
        Directory.CreateDirectory(twoDaDirectory);
        var jsonPath = Path.Combine(tlkDirectory, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(tlkDirectory, "sw_tlk.tlk");
        var oldPath = Path.Combine(tlkDirectory, "old.tlk");
        WritePair(jsonPath, binaryPath, "new repository generation");
        File.WriteAllBytes(oldPath,
            TlkWriter.Write(0, new Dictionary<int, string> { [0] = "old published generation" }));

        try
        {
            var tlk = new TlkService(TlkJsonFile.Parse(
                "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"startup\"}]}"));
            tlk.PublishCustomTlk(TlkReader.Read(oldPath));
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(
                path => new SWLOR.Toolset.Domain.Workspace.ModuleWorkspace(path),
                log);
            var properties = new PropertiesViewModel(workspace, log, tlkService: tlk);
            var service = new EditorService(
                workspace,
                new LookupOptionProvider(workspace),
                log,
                new ToolsetDockFactory(null!, properties, null!, null!, null!, null!, null!, null!, null!),
                new StubPrompts(),
                tlkService: tlk,
                tlkEditorSource: new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory));

            await service.OpenTlkEditorAsync();

            tlk.GetCustomText(0).Should().Be("new repository generation");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task TlkPublicationRefreshesCatalogPropertiesAndStandardPaletteCaches()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-surface-refresh-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "are"));
        Directory.CreateDirectory(Path.Combine(root, "utc"));
        Directory.CreateDirectory(Path.Combine(root, "uti"));
        var itemPath = Path.Combine(root, "uti", "test_item.uti.json");
        File.WriteAllText(itemPath,
            $"{{\"__data_type\":\"UTI \",\"LocalizedName\":{{\"id\":{TlkService.CustomTlkBase},\"type\":\"cexolocstring\",\"value\":{{}}}}}}");
        var tlk = new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}"));
        tlk.PublishCustomTlk(TlkReader.Read(
            TlkWriter.Write(0, new Dictionary<int, string> { [0] = "Old Label" })));

        try
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(
                path => new SWLOR.Toolset.Domain.Workspace.ModuleWorkspace(path),
                log,
                tlk);
            workspace.Open(root);
            await workspace.Catalog!.BuildTask;
            workspace.Catalog.TryGetEntry(ResourceType.Uti, "test_item", out var entry).Should().BeTrue();
            entry.Name.Should().Be("Old Label");

            var properties = new PropertiesViewModel(workspace, log, tlkService: tlk);
            properties.ShowEntry(entry);
            properties.Rows.Single(row => row.Key == "LocalizedName").Value.Should().Be("Old Label");

            var categories = new CategoryService(workspace, log, tlk);
            categories.StandardSection(ResourceType.Uti);
            GetCachedStandardPaletteCount(categories).Should().Be(1);
            var categoryRefreshes = 0;
            categories.Changed += () => categoryRefreshes++;
            var catalogRefreshes = 0;
            workspace.CatalogLabelsChanged += () => catalogRefreshes++;

            tlk.PublishCustomTlk(TlkReader.Read(
                TlkWriter.Write(0, new Dictionary<int, string> { [0] = "New Label" })));
            workspace.RefreshTlkLabels();
            categories.RefreshTlkLabels();
            new ToolsetDockFactory(null!, properties, null!, null!, null!, null!, null!, null!, null!)
                .RefreshTlkLabels();

            workspace.Catalog.TryGetEntry(ResourceType.Uti, "test_item", out entry).Should().BeTrue();
            entry.Name.Should().Be("New Label");
            catalogRefreshes.Should().Be(1);
            properties.Rows.Single(row => row.Key == "LocalizedName").Value.Should().Be("New Label");
            GetCachedStandardPaletteCount(categories).Should().Be(0);
            categoryRefreshes.Should().Be(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static int GetCachedStandardPaletteCount(CategoryService categories)
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var cache = typeof(CategoryService).GetField("_standardPalettes", flags)!.GetValue(categories)!;
        return (int)cache.GetType().GetProperty("Count")!.GetValue(cache)!;
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

    [Test]
    public void ReopenedProductionBackendCanReplaceAnOlderPublishedRepositoryGeneration()
    {
        var root = Path.Combine(Path.GetTempPath(), "swlor-tlk-reopen-publish-tests", Guid.NewGuid().ToString("N"));
        var tlkDirectory = Path.Combine(root, "sw_tlk");
        var twoDaDirectory = Path.Combine(root, "sw_2da");
        Directory.CreateDirectory(tlkDirectory);
        Directory.CreateDirectory(twoDaDirectory);
        var jsonPath = Path.Combine(tlkDirectory, "sw_tlk.tlk.json");
        var binaryPath = Path.Combine(tlkDirectory, "sw_tlk.tlk");
        var service = new TlkService(TlkJsonFile.Parse(
            "{\"language\":0,\"entries\":[{\"id\":0,\"text\":\"startup\"}]}"));

        try
        {
            WritePair(jsonPath, binaryPath, "old repository generation");
            var first = new TlkEditorBackend(
                new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory),
                service);
            first.Publish();
            service.GetCustomText(0).Should().Be("old repository generation");

            WritePair(jsonPath, binaryPath, "new repository generation");
            var reopened = new TlkEditorBackend(
                new TlkEditorSource(jsonPath, binaryPath, twoDaDirectory),
                service);
            reopened.Publish();

            service.GetCustomText(0).Should().Be("new repository generation");
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
            Grid.GetColumn(view.FindControl<Button>("NextBlankButton")!).Should().Be(0);
            view.FindControl<Button>("AddRowButton")!.Content.Should().Be("Add row");
            Grid.GetColumn(view.FindControl<Button>("AddRowButton")!).Should().Be(1);
            view.FindControl<Button>("RemoveRowButton")!.Content.Should().Be("Remove row");
            Grid.GetColumn(view.FindControl<Button>("RemoveRowButton")!).Should().Be(2);
            view.FindControl<TextBox>("SelectedTextEditor").Should().NotBeNull();
            grid.ItemsSource.Should().BeSameAs(editor.Rows);
            grid.GetVisualDescendants().OfType<ListBoxItem>().Count().Should().BeLessThan(100,
                "only visible rows should be realized from the 200,000-row virtual range");
            editor.Rows.CreatedRowCount.Should().BeLessThan(100,
                "the item source itself must not be eagerly copied into a collection view");
            view.GetVisualDescendants().OfType<TextBox>().Should().NotBeEmpty();
            view.GetVisualDescendants().OfType<TextBlock>()
                .Select(text => text.Text)
                .Should().NotContain(text =>
                    text != null && text.Contains("Paste here preserves", StringComparison.Ordinal));
            view.FindControl<DataGrid>("ReferenceGrid")!.Columns
                .Select(column => column.Header?.ToString())
                .Should().ContainInOrder("Source", "File", "Row / line", "Label", "Column");

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
    public void TlkLoadingDocumentShowsProgressAndCancelsWhenClosed()
    {
        var loading = new TlkEditorLoadingDocumentViewModel("memory/sw_tlk.tlk.json");
        var view = new TlkEditorLoadingDocumentView { DataContext = loading };
        var window = new Window { Width = 900, Height = 600, Content = view };
        window.Show();
        window.UpdateLayout();

        view.GetVisualDescendants().OfType<TextBlock>()
            .Should().Contain(text => text.Text == "Loading TLK Editor");
        view.GetVisualDescendants().OfType<ProgressBar>()
            .Should().ContainSingle(progress => progress.IsIndeterminate);
        loading.CancellationRequested.Should().BeFalse();

        window.Close();
        loading.OnClose().Should().BeTrue();
        loading.CancellationRequested.Should().BeTrue();
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
            IReadOnlyList<string>? warnings = null,
            IReadOnlyList<TlkEditorUsage>? usages = null)
        {
            _entries = new Dictionary<int, string>(entries);
            _referenced = referenced?.ToHashSet() ?? new HashSet<int>();
            ReferenceWarnings = warnings ?? Array.Empty<string>();
            UsageRows = usages;
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
        public Dictionary<int, string> LastSavedEntries { get; private set; } = new();
        public int SaveCount { get; private set; }
        public int ReloadCount { get; private set; }
        public int ReferenceRefreshCount { get; private set; }
        public Exception? ReloadFailure { get; init; }
        public ManualResetEventSlim? SaveStarted { get; init; }
        public ManualResetEventSlim? ContinueSave { get; init; }
        public ManualResetEventSlim? ReferenceRefreshStarted { get; init; }
        public ManualResetEventSlim? ContinueReferenceRefresh { get; init; }
        public IReadOnlyList<TlkEditorUsage>? UsageRows { get; }

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
        public int UsageCountFor(int id) => UsagesOf(id).Count;
        public IReadOnlyList<TlkEditorUsage> UsagesOf(int id)
        {
            if (UsageRows != null)
            {
                var strRef = TlkService.CustomTlkBase + (uint)id;
                return UsageRows.Where(usage => usage.StrRef == strRef).ToArray();
            }

            return IsReferenced(id)
                ? new[]
                {
                    new TlkEditorUsage(
                        "test.2da", 7, "label", "STRREF", TlkService.CustomTlkBase + (uint)id)
                }
                : Array.Empty<TlkEditorUsage>();
        }
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
            ReferenceRefreshStarted?.Set();
            ContinueReferenceRefresh?.Wait(TimeSpan.FromSeconds(5));
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
            LastSavedEntries = new Dictionary<int, string>(_entries);
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
