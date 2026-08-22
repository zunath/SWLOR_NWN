using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// The NWScript source editor, docked as a document tab. Unlike the blueprint and area editors
    /// it holds no GFF session: a script is plain text, so the buffer lives in AvaloniaEdit's
    /// TextDocument and this view model owns only the file binding, dirty state and save path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Undo/redo forward to the text buffer's own stack rather than to a DocumentTransaction. That
    /// is the one place the script editor deliberately does not reuse the existing editing
    /// infrastructure - see ScriptSession for why.
    /// </para>
    /// <para>
    /// The view sets <see cref="TextBinding"/> once when it attaches and then pushes buffer changes
    /// in through <see cref="OnTextChanged"/>. Keeping the text on the view model as a plain field
    /// rather than a two-way bound property avoids the feedback loop a bound TextDocument creates,
    /// where re-setting the text on every keystroke resets the caret.
    /// </para>
    /// </remarks>
    public partial class ScriptEditorViewModel : Document, IEditorDocument, IDocumentStatusSource
    {
        private readonly ScriptSession _session;
        private readonly OutputLogService _log;
        private readonly IEditorPromptService _prompts;
        private readonly string _resRef;
        private string _text;
        private bool _closeApproved;
        private bool _closePromptOpen;

        /// <summary>
        /// True when the last compile-on-save failed after the source was already written. The
        /// source is clean at that point, so this is what makes the next save (or close) retry the
        /// compile instead of skipping straight past a stale .ncs.
        /// </summary>
        private bool _compileOnSaveFailed;

        /// <summary>
        /// True while the last compile-on-save failed after the source was written: the tab is
        /// clean, yet the canonical .ncs is stale. Application close treats this as unsaved work
        /// so a second close attempt cannot exit silently with stale bytecode.
        /// </summary>
        public bool HasPendingCompileFailure => _compileOnSaveFailed;
        private bool _isClosed;
        private int _line = 1;
        private int _column = 1;
        private readonly object _analysisGate = new();

        private readonly ScriptLanguageService? _language;
        private readonly ScriptCompletionEngine? _completion;
        private readonly ScriptAnalyzer? _analyzer;

        public ScriptEditorViewModel(
            string filePath,
            string resRef,
            OutputLogService log,
            IEditorPromptService prompts,
            ScriptLanguageService? language = null,
            ScriptSearchViewModel? workspaceSearch = null)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            _language = language;
            _completion = language?.CreateCompletionEngine();
            _analyzer = language != null ? new ScriptAnalyzer(language.Engine, language.ReadScriptSource) : null;
            WorkspaceSearch = workspaceSearch;
            Id = $"editor:{filePath}";
            _session = ScriptSession.Open(filePath);
            _text = _session.Document.Text;
            UpdateTitle();
        }

        /// <summary>Tells the colorizer which identifiers are engine functions. Null when unavailable.</summary>
        public Func<string, bool>? IsEngineFunction => _language != null ? _language.IsEngineFunction : null;

        /// <summary>Tells the colorizer which identifiers are engine constants.</summary>
        public Func<string, bool>? IsEngineConstant => _language != null ? _language.IsEngineConstant : null;

        /// <summary>Inserts text at the caret. Set by the view; used by the Script Reference panel.</summary>
        public Action<string>? InsertAtCursorRequested { get; set; }

        /// <summary>
        /// Rebuilds this script's .ncs after a successful save. Set by EditorService; null when no
        /// compiler is vendored, in which case saving still works and simply changes nothing in game.
        /// </summary>
        public Func<string, Task<bool>>? CompileOnSave { get; set; }

        // ----- compiling, from the tab itself -----

        /// <summary>
        /// Compiles this script. Set by EditorService; null when no compiler is vendored.
        /// </summary>
        /// <remarks>
        /// Lives on the document rather than on the shell's Build menu. Compiling is an act on the
        /// file in front of you — a module-wide menu is the wrong home for it, and it left the action
        /// available (greyed) when nothing compilable was open.
        /// </remarks>
        public Func<string, Task<bool>>? CompileRequested { get; set; }

        /// <summary>Opens the Problems panel. Set by EditorService.</summary>
        public Action? ShowProblemsRequested { get; set; }

        /// <summary>
        /// Raised while a pack, validation, or Build All is running. Compiling saves the .nss and then
        /// writes its .ncs, so during a pack it replaces files the packer is copying, and during a
        /// Build All it points a second compiler process at the same output.
        /// </summary>
        public ModuleMutationLock? MutationLock
        {
            get => _mutationLock;
            set
            {
                if (ReferenceEquals(_mutationLock, value))
                    return;

                if (_mutationLock != null)
                    _mutationLock.Changed -= OnMutationLockChanged;

                _mutationLock = value;

                if (_mutationLock != null)
                    _mutationLock.Changed += OnMutationLockChanged;

                OnMutationLockChanged();
            }
        }

        private ModuleMutationLock? _mutationLock;

        private void OnMutationLockChanged()
        {
            OnPropertyChanged(nameof(CanCompile));
            CompileCommand.NotifyCanExecuteChanged();
        }

        public bool CanCompile =>
            CompileRequested != null && !IsCompiling && _mutationLock?.IsLocked != true;

        [ObservableProperty]
        private bool _isCompiling;

        /// <summary>
        /// The last compile's outcome, shown on the tab's own status strip so a failure is visible
        /// without hunting for a panel.
        /// </summary>
        [ObservableProperty]
        private string _compileStatus = string.Empty;

        [ObservableProperty]
        private bool _lastCompileFailed;

        private Func<Task<(int Compiled, int Failed)>>? _compileStatusAction;

        public bool HasCompileStatus => CompileStatus.Length > 0;

        partial void OnCompileStatusChanged(string value) => OnPropertyChanged(nameof(HasCompileStatus));

        partial void OnIsCompilingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanCompile));
            CompileCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanCompile))]
        private async Task Compile()
        {
            if (CompileRequested == null)
                return;

            // Rechecked at execution: Ctrl+B goes straight here, and a pack can start between the
            // keystroke and this line.
            if (_mutationLock?.IsLocked == true)
            {
                CompileStatus = "Compile is unavailable while the module is being packed or built.";
                LastCompileFailed = true;
                return;
            }

            using var moduleWriteLock =
                ModuleWriteLock.AcquireForResourcePath(_session.FilePath);
            // Compiling reads the file from disk, so unsaved work would silently not be built.
            if (!await TrySaveAsync(compileOnSave: false).ConfigureAwait(true))
            {
                CompileStatus = "Compile cancelled: could not save.";
                LastCompileFailed = true;
                return;
            }

            IsCompiling = true;
            _compileStatusAction = null;
            CompileStatus = "Compiling...";
            LastCompileFailed = false;

            try
            {
                var ok = await CompileRequested(_resRef).ConfigureAwait(true);
                LastCompileFailed = !ok;
                CompileStatus = ok
                    ? $"Compiled {_resRef}.ncs at {DateTime.Now:HH:mm:ss}"
                    : $"{_resRef} failed to compile — see Problems";

                if (!ok)
                    ShowProblemsRequested?.Invoke();
            }
            finally
            {
                IsCompiling = false;
            }
        }

        /// <summary>Brings the Problems panel forward; the status strip is clickable.</summary>
        /// <summary>
        /// Says how many dependent scripts a saved include already rebuilt.
        /// </summary>
        /// <remarks>
        /// A statement, not an offer: the work is done by the time this is called, and the previous
        /// clickable "N dependent script(s) need rebuilding" ran the whole pass again.
        /// </remarks>
        public void ReportDependentRebuild(int count)
        {
            if (count <= 0)
                return;

            _compileStatusAction = null;
            LastCompileFailed = false;
            CompileStatus = $"Rebuilt {count} dependent script(s).";
        }

        public void OfferDependentRebuild(
            IReadOnlyList<string> dependents,
            Func<Task<(int Compiled, int Failed)>> rebuild)
        {
            if (dependents.Count == 0)
                return;

            _compileStatusAction = rebuild;
            LastCompileFailed = false;
            CompileStatus = $"{dependents.Count} dependent script(s) need rebuilding - click to build";
        }

        [RelayCommand]
        private async Task ShowProblems()
        {
            if (_compileStatusAction == null)
            {
                ShowProblemsRequested?.Invoke();
                return;
            }

            var rebuild = _compileStatusAction;
            _compileStatusAction = null;
            IsCompiling = true;
            CompileStatus = "Building dependent scripts...";
            LastCompileFailed = false;

            try
            {
                var (compiled, failed) = await rebuild().ConfigureAwait(true);
                LastCompileFailed = failed > 0;
                CompileStatus = failed == 0
                    ? $"Built {compiled} dependent script(s) at {DateTime.Now:HH:mm:ss}"
                    : $"Built {compiled} dependent script(s); {failed} failed - see Problems";

                if (failed > 0)
                    ShowProblemsRequested?.Invoke();
            }
            finally
            {
                IsCompiling = false;
            }
        }

        /// <summary>
        /// The ranked completion list for a caret position, plus the offset the partial word starts
        /// at so the insertion replaces what was typed. Ranking happens in Domain.
        /// </summary>
        public (IReadOnlyList<CompletionItem> Items, int ReplaceFrom) GetCompletions(string source, int caret)
        {
            if (_completion == null)
                return (Array.Empty<CompletionItem>(), caret);

            var context = ScriptCompletionEngine.DescribeContext(source, caret);
            return (_completion.GetCompletions(source, caret), context.PrefixStart);
        }

        /// <summary>The initial buffer contents. Read once by the view when it attaches.</summary>
        public string TextBinding => _text;

        public string FilePath => _session.FilePath;

        public bool IsDirty => _session.IsDirty(_text);

        /// <summary>Cursor position and file shape, shown in the shell's status bar.</summary>
        public string StatusDetail =>
            $"Ln {_line}, Col {_column} · {(_session.Document.EolStyle == ScriptEolStyle.Crlf ? "CRLF" : "LF")}";

        /// <summary>Raised when the buffer must be replaced wholesale, i.e. after an external reload.</summary>
        public event Action<string>? TextReplaced;

        /// <summary>Raised when the tab closes so the editor registry can forget this instance.</summary>
        public event Action<ScriptEditorViewModel>? Closed;

        /// <summary>Raised after an async close prompt approves closing this tab.</summary>
        public event Action<ScriptEditorViewModel>? CloseRequested;

        /// <summary>Wired to the text buffer's undo stack by the view once it attaches.</summary>
        public Func<bool>? CanUndoProbe { get; set; }

        /// <summary>Wired to the text buffer's undo stack by the view once it attaches.</summary>
        public Func<bool>? CanRedoProbe { get; set; }

        /// <summary>Invokes undo on the text buffer. Set by the view.</summary>
        public Action? UndoRequested { get; set; }

        /// <summary>Invokes redo on the text buffer. Set by the view.</summary>
        public Action? RedoRequested { get; set; }

        public bool CanUndo => CanUndoProbe?.Invoke() ?? false;

        public bool CanRedo => CanRedoProbe?.Invoke() ?? false;

        public void Undo()
        {
            UndoRequested?.Invoke();
            AfterHistoryChange();
        }

        public void Redo()
        {
            RedoRequested?.Invoke();
            AfterHistoryChange();
        }

        /// <summary>Called by the view on every buffer change.</summary>
        public void OnTextChanged(string text)
        {
            _text = text;
            AfterHistoryChange();
            QueueAnalysis();
        }

        /// <summary>The current advisory findings. Replaced on each idle pass.</summary>
        public IReadOnlyList<ScriptAnalysisDiagnostic> Diagnostics { get; private set; } =
            Array.Empty<ScriptAnalysisDiagnostic>();

        /// <summary>Raised after an idle re-analysis, so the view can redraw squiggles.</summary>
        public event Action<IReadOnlyList<ScriptAnalysisDiagnostic>>? DiagnosticsChanged;

        /// <summary>The script's resref, which the Problems panel groups findings by.</summary>
        public string ResRef => _resRef;

        private CancellationTokenSource? _analysisCts;

        /// <summary>
        /// Re-analyses after a short idle. Debounced rather than run per keystroke: the pass is cheap
        /// but not free, and squiggles that appear mid-word while the author is still typing the name
        /// are worse than squiggles that appear a moment later.
        /// </summary>
        private void QueueAnalysis()
        {
            if (_analyzer == null || _isClosed)
                return;

            CancellationTokenSource cts;
            lock (_analysisGate)
            {
                if (_isClosed)
                    return;

                _analysisCts?.Cancel();
                _analysisCts?.Dispose();
                cts = new CancellationTokenSource();
                _analysisCts = cts;
            }

            var source = _text;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, cts.Token).ConfigureAwait(false);
                    var analysis = _analyzer.Analyze(source);

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        lock (_analysisGate)
                        {
                            if (_isClosed || cts.IsCancellationRequested || !ReferenceEquals(_analysisCts, cts))
                                return;

                            Diagnostics = analysis.Diagnostics;
                            RefreshOutline(analysis.Outline);
                            DiagnosticsChanged?.Invoke(Diagnostics);
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    // Superseded by a newer keystroke; nothing to report.
                }
            }, cts.Token);
        }

        /// <summary>Runs analysis immediately, e.g. right after the tab opens.</summary>
        public void AnalyzeNow()
        {
            if (_analyzer == null || _isClosed)
                return;

            var analysis = _analyzer.Analyze(_text);
            Diagnostics = analysis.Diagnostics;
            DiagnosticsChanged?.Invoke(Diagnostics);
            RefreshOutline(analysis.Outline);
        }

        // ----- navigation -----

        /// <summary>Functions declared in this file, for the outline strip.</summary>
        public ObservableCollection<ScriptFunctionDeclaration> OutlineFunctions { get; } = new();

        /// <summary>Cross-file search scoped to this script editor; absent in isolated view-model tests.</summary>
        public ScriptSearchViewModel? WorkspaceSearch { get; }

        public bool HasWorkspaceSearch => WorkspaceSearch != null;

        [ObservableProperty]
        private bool _isOutlineCollapsed = true;

        [ObservableProperty]
        private bool _isWorkspaceSearchOpen;

        public bool IsOutlineVisible => !IsOutlineCollapsed && !IsWorkspaceSearchOpen;

        partial void OnIsOutlineCollapsedChanged(bool value) =>
            OnPropertyChanged(nameof(IsOutlineVisible));

        partial void OnIsWorkspaceSearchOpenChanged(bool value) =>
            OnPropertyChanged(nameof(IsOutlineVisible));

        [RelayCommand(CanExecute = nameof(HasWorkspaceSearch))]
        private void ToggleWorkspaceSearch() => IsWorkspaceSearchOpen = !IsWorkspaceSearchOpen;

        [RelayCommand]
        private void ToggleOutline() => IsOutlineCollapsed = !IsOutlineCollapsed;

        private void RefreshOutline(ScriptOutline outline)
        {
            OutlineFunctions.Clear();
            foreach (var fn in outline.Functions.Where(f => f.IsDefinition))
                OutlineFunctions.Add(fn);
        }

        /// <summary>Moves the caret to an offset. Set by the view.</summary>
        public Action<int>? GoToOffsetRequested { get; set; }

        /// <summary>Moves the caret to a 1-based line. Set by the view.</summary>
        public Action<int>? GoToLineRequested { get; set; }

        /// <summary>Replaces the whole buffer as one undoable edit. Set by the view.</summary>
        public Action<string>? ReplaceAllRequested { get; set; }

        [RelayCommand]
        private void GoToOutlineEntry(ScriptFunctionDeclaration? entry)
        {
            if (entry != null)
                GoToOffsetRequested?.Invoke(entry.Offset);
        }

        /// <summary>
        /// Go-to-definition (F12). Resolves in this file first, then its direct includes, then the
        /// engine header — where there is nothing to open, so it reports the signature instead.
        /// </summary>
        public void GoToDefinition(int caretOffset)
        {
            if (_language == null)
                return;

            var definition = ScriptNavigation.FindDefinition(
                _text, caretOffset, _language.Engine, _language.ReadScriptSource);

            if (definition == null)
            {
                _log.AppendLine("No definition found for the symbol under the caret.");
                return;
            }

            if (definition.IsEngineSymbol)
            {
                var fn = _language.Engine.FindFunction(definition.Name);
                _log.AppendLine(fn != null
                    ? $"{definition.Name} is an engine function: {fn.Signature}"
                    : $"{definition.Name} is an engine constant.");
                return;
            }

            if (definition.ResRef == null)
            {
                GoToOffsetRequested?.Invoke(definition.Offset);
                return;
            }

            OpenIncludeRequested?.Invoke(definition.ResRef, definition.Offset);
        }

        /// <summary>Asks the shell to open another script and place the caret. Set by EditorService.</summary>
        public Action<string, int>? OpenIncludeRequested { get; set; }

        /// <summary>
        /// Lists every blueprint, area and instance whose script slots name this script.
        /// </summary>
        /// <remarks>
        /// The question Aurora could not answer. 2,250 module resources name a script by resref, and
        /// nothing else in the pipeline can tell you which — so editing a legacy script has always
        /// been guesswork about what it is attached to.
        /// </remarks>
        [RelayCommand]
        private async Task ShowUsages()
        {
            if (FindUsages == null)
            {
                _log.AppendLine("Script usage index is unavailable.");
                return;
            }

            _log.AppendLine($"Finding what uses {_resRef}...");
            var usages = await FindUsages(_resRef).ConfigureAwait(true);

            if (usages.Count == 0)
            {
                _log.AppendLine($"Nothing references {_resRef}. It may be called from C#, or be dead.");
                UsageSummary = "not referenced";
                return;
            }

            _log.AppendLine($"{usages.Count} resource(s) reference {_resRef}:");
            foreach (var group in usages.GroupBy(u => u.ResourceType).OrderBy(g => g.Key.ToString()))
            {
                _log.AppendLine($"  {group.Key.DisplayName()} ({group.Count()}):");
                foreach (var usage in group.OrderBy(u => u.ResRef, StringComparer.OrdinalIgnoreCase))
                    _log.AppendLine($"    {usage.ResRef} · {usage.FieldName}");
            }

            UsageSummary = $"used by {usages.Count}";
        }

        /// <summary>Resolves what references this script. Set by EditorService; null disables the command.</summary>
        public Func<string, Task<IReadOnlyList<ScriptUsage>>>? FindUsages { get; set; }

        /// <summary>Shown beside the outline once usages have been looked up.</summary>
        public string UsageSummary
        {
            get => _usageSummary;
            private set
            {
                _usageSummary = value;
                OnPropertyChanged(nameof(UsageSummary));
                OnPropertyChanged(nameof(HasUsageSummary));
            }
        }

        private string _usageSummary = string.Empty;

        public bool HasUsageSummary => UsageSummary.Length > 0;

        /// <summary>Lists every occurrence of the identifier under the caret in the Output panel.</summary>
        public void FindReferences(int caretOffset)
        {
            var name = ScriptNavigation.IdentifierAt(_text, caretOffset);
            if (name == null)
                return;

            var references = ScriptNavigation.FindReferences(_text, name);
            _log.AppendLine($"{references.Count} reference(s) to '{name}' in {_resRef}.nss:");
            foreach (var reference in references)
                _log.AppendLine($"  {_resRef}.nss({reference.Line})");
        }

        /// <summary>
        /// Renames the identifier under the caret throughout this file. Comments and string literals
        /// are untouched, which is what makes this safe on legacy scripts that use the same word as a
        /// local variable and as a string key.
        /// </summary>
        public async Task RenameAsync(int caretOffset)
        {
            var name = ScriptNavigation.IdentifierAt(_text, caretOffset);
            if (name == null)
            {
                _log.AppendLine("Place the caret on an identifier to rename it.");
                return;
            }

            var replacement = await _prompts.PromptForTextAsync(
                "Rename symbol", $"New name for '{name}'.", name, "Rename").ConfigureAwait(true);

            if (string.IsNullOrWhiteSpace(replacement) || replacement == name)
                return;

            if (!ScriptNavigation.IsValidIdentifier(replacement))
            {
                _log.AppendLine($"'{replacement}' is not a valid NWScript identifier.");
                return;
            }

            var renamed = ScriptNavigation.Rename(_text, name, replacement);
            var count = ScriptNavigation.FindReferences(_text, name).Count;

            ReplaceAllRequested?.Invoke(renamed);
            _log.AppendLine($"Renamed {count} occurrence(s) of '{name}' to '{replacement}'.");
        }

        /// <summary>Called by the view when the caret moves.</summary>
        public void OnCaretMoved(int line, int column)
        {
            _line = line;
            _column = column;
            OnPropertyChanged(nameof(StatusDetail));
        }

        [RelayCommand]
        private async Task Save()
        {
            await TrySaveAsync().ConfigureAwait(true);
        }

        /// <summary>Saves this editor, returning false when the user cancels or the write fails.</summary>
        public Task<bool> TrySaveAsync() => TrySaveAsync(compileOnSave: true);

        /// <summary>
        /// Saves this editor, optionally suppressing compile-on-save when an explicit compile is about
        /// to follow. A requested compile is awaited so pack/build cannot race the bytecode writer.
        /// </summary>
        public async Task<bool> TrySaveAsync(bool compileOnSave)
        {
            if (!IsDirty)
            {
                // The source is already on disk, but a previously failed compile-on-save means the
                // canonical .ncs is still stale; retry the compile rather than reporting success.
                if (compileOnSave && _compileOnSaveFailed && CompileOnSave != null)
                {
                    if (!await CompileOnSave(_resRef).ConfigureAwait(true))
                    {
                        _log.AppendLine(
                            $"Compiled output for {_session.FilePath} is still not updated.");
                        return false;
                    }

                    _compileOnSaveFailed = false;
                }

                return true;
            }

            try
            {
                if (_session.HasExternalChange())
                {
                    var choice = await _prompts.ConfirmExternalChangeAsync(_session.FilePath).ConfigureAwait(true);
                    if (choice == ExternalChangeChoice.Cancel)
                        return false;

                    if (choice == ExternalChangeChoice.Reload)
                    {
                        var reloaded = _session.ReloadFromDisk();
                        _text = reloaded.Text;
                        TextReplaced?.Invoke(_text);
                        AfterHistoryChange();
                        _log.AppendLine($"Reloaded externally changed file {_session.FilePath}.");
                        return true;
                    }

                    // Overwrite accepts the generation currently on disk. The locked check below
                    // still catches a second writer arriving while the source bytes are prepared.
                    _session.RecordCurrentFileState();
                }

                var saveBytes = _session.ToBytes(_text);
                if (!SaveService.TryWriteAtomicIfUnchanged(_session, saveBytes))
                {
                    _log.AppendLine(
                        $"Cannot save {_session.FilePath}: the file changed on disk while the save " +
                        "was being prepared. Nothing was written - reload or save again.");
                    return false;
                }
                _session.MarkSaved(_text, saveBytes);
                AfterHistoryChange();
                _log.AppendLine($"Saved {_session.FilePath}.");

                // NWN runs the .ncs, not the .nss. The save is not complete for build/pack purposes
                // until the bytecode writer has finished.
                if (compileOnSave && CompileOnSave != null)
                {
                    if (!await CompileOnSave(_resRef).ConfigureAwait(true))
                    {
                        _compileOnSaveFailed = true;
                        _log.AppendLine(
                            $"Saved {_session.FilePath}, but its compiled output was not updated.");
                        return false;
                    }

                    _compileOnSaveFailed = false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.AppendLine($"Save failed for {_session.FilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>Suppresses a second tab-level prompt after the window-level discard decision.</summary>
        internal void ApproveApplicationClose() => _closeApproved = true;

        public override bool OnClose()
        {
            if (!_closeApproved && (IsDirty || _compileOnSaveFailed))
            {
                if (!_closePromptOpen)
                {
                    _closePromptOpen = true;
                    _ = ConfirmCloseAsync();
                }

                return false;
            }

            lock (_analysisGate)
            {
                _isClosed = true;
                _analysisCts?.Cancel();
                _analysisCts?.Dispose();
                _analysisCts = null;
                DiagnosticsChanged = null;
            }

            MutationLock = null;
            Closed?.Invoke(this);
            return base.OnClose();
        }

        private async Task ConfirmCloseAsync()
        {
            try
            {
                var choice = await _prompts.ConfirmCloseAsync(Title ?? _resRef).ConfigureAwait(true);
                var approved = choice == UnsavedChangesChoice.Discard ||
                    choice == UnsavedChangesChoice.Save && await TrySaveAsync().ConfigureAwait(true);
                if (!approved)
                    return;

                _closeApproved = true;
                CloseRequested?.Invoke(this);
            }
            finally
            {
                _closePromptOpen = false;
            }
        }

        private void AfterHistoryChange()
        {
            UpdateTitle();
            OnPropertyChanged(nameof(IsDirty));
            // The shell's Edit menu mirrors this tab's history, so it needs the change too.
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(StatusDetail));
        }

        private void UpdateTitle()
        {
            Title = IsDirty ? $"{_resRef} *" : _resRef;
        }
    }
}
