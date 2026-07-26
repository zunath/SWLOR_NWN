using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Script;
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

        public ScriptEditorViewModel(
            string filePath,
            string resRef,
            OutputLogService log,
            IEditorPromptService prompts)
        {
            _log = log;
            _prompts = prompts;
            _resRef = resRef;
            Id = $"editor:{filePath}";
            _session = ScriptSession.Open(filePath);
            _text = _session.Document.Text;
            UpdateTitle();
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
