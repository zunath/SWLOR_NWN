using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;
using SWLOR.Toolset.Services;
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
        private int _line = 1;
        private int _column = 1;

        private readonly ScriptLanguageService? _language;
        private readonly ScriptCompletionEngine? _completion;
        private readonly ScriptAnalyzer? _analyzer;

        public ScriptEditorViewModel(
            string filePath,
            string resRef,
            OutputLogService log,
            IEditorPromptService prompts,
            ScriptLanguageService? language = null)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            _language = language;
            _completion = language?.CreateCompletionEngine();
            _analyzer = language != null ? new ScriptAnalyzer(language.Engine) : null;
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
        public Func<string, Task>? CompileOnSave { get; set; }

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
            if (_analyzer == null)
                return;

            _analysisCts?.Cancel();
            var cts = new CancellationTokenSource();
            _analysisCts = cts;

            var source = _text;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, cts.Token).ConfigureAwait(false);
                    var analysis = _analyzer.Analyze(source);
                    if (cts.IsCancellationRequested)
                        return;

                    Diagnostics = analysis.Diagnostics;
                    DiagnosticsChanged?.Invoke(Diagnostics);
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
            if (_analyzer == null)
                return;

            var analysis = _analyzer.Analyze(_text);
            Diagnostics = analysis.Diagnostics;
            DiagnosticsChanged?.Invoke(Diagnostics);
            RefreshOutline(analysis.Outline);
        }

        // ----- navigation -----

        /// <summary>Functions declared in this file, for the outline strip.</summary>
        public ObservableCollection<ScriptFunctionDeclaration> OutlineFunctions { get; } = new();

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
        public async Task<bool> TrySaveAsync()
        {
            if (!IsDirty)
                return true;

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
                }

                SaveService.WriteAtomic(_session.FilePath, _session.ToBytes(_text));
                _session.MarkSaved(_text);
                AfterHistoryChange();
                _log.AppendLine($"Saved {_session.FilePath}.");

                // Compile-on-save, per the locked decision. Fire-and-forget so the save returns
                // immediately: NWN runs the .ncs, not the .nss, so a save that did not rebuild
                // bytecode would look effective and change nothing in game.
                if (CompileOnSave != null)
                    _ = CompileOnSave(_resRef);

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
            if (!_closeApproved && IsDirty)
            {
                if (!_closePromptOpen)
                {
                    _closePromptOpen = true;
                    _ = ConfirmCloseAsync();
                }

                return false;
            }

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
