using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Editors.Script;

namespace SWLOR.Toolset.Editors
{
    public partial class ScriptEditorView : UserControl
    {
        private readonly NwScriptColorizer _colorizer = new();
        private readonly DiagnosticSquiggleRenderer _squiggles = new();
        private TextEditor? _editor;
        private ScriptEditorViewModel? _bound;
        private Action<IReadOnlyList<Domain.Script.Syntax.ScriptAnalysisDiagnostic>>? _diagnosticsHandler;
        private CompletionWindow? _completionWindow;
        private bool _suppressTextChanged;
        private Border? _searchPanel;
        private TextBox? _findTextBox;
        private TextBox? _replaceTextBox;
        private TextBlock? _replaceLabel;
        private Button? _replaceButton;
        private Button? _replaceAllButton;
        private CheckBox? _matchCaseBox;

        public ScriptEditorView()
        {
            InitializeComponent();
            _editor = this.FindControl<TextEditor>("Editor");
            _searchPanel = this.FindControl<Border>("SearchPanel");
            _findTextBox = this.FindControl<TextBox>("FindTextBox");
            _replaceTextBox = this.FindControl<TextBox>("ReplaceTextBox");
            _replaceLabel = this.FindControl<TextBlock>("ReplaceLabel");
            _replaceButton = this.FindControl<Button>("ReplaceButton");
            _replaceAllButton = this.FindControl<Button>("ReplaceAllButton");
            _matchCaseBox = this.FindControl<CheckBox>("MatchCaseBox");
            DataContextChanged += OnDataContextChanged;

            if (_editor != null)
            {
                _editor.TextChanged += OnEditorTextChanged;
                _editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
                _editor.TextArea.TextView.LineTransformers.Add(_colorizer);
                _editor.TextArea.TextView.BackgroundRenderers.Add(_squiggles);
                _editor.TextArea.TextEntered += OnTextEntered;
                _editor.TextArea.TextEntering += OnTextEntering;
                _editor.AddHandler(KeyDownEvent, OnEditorKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
                _editor.TextArea.AddHandler(KeyDownEvent, OnEditorKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_bound != null)
            {
                _bound.TextReplaced -= ReplaceText;
                if (_diagnosticsHandler != null)
                    _bound.DiagnosticsChanged -= _diagnosticsHandler;
                _diagnosticsHandler = null;

                // Fully unhook, symmetric with AreaEditorView.AttachViewModel: these delegates close
                // over this view's _editor, so leaving them set on the outgoing view model would let
                // a docking host that reuses this view act on the wrong document if that view model
                // is ever queried again.
                _bound.CanUndoProbe = null;
                _bound.CanRedoProbe = null;
                _bound.UndoRequested = null;
                _bound.RedoRequested = null;
                _bound.InsertAtCursorRequested = null;
                _bound.GoToOffsetRequested = null;
                _bound.GoToLineRequested = null;
                _bound.ReplaceAllRequested = null;
            }

            _bound = DataContext as ScriptEditorViewModel;
            if (_bound == null || _editor == null)
                return;

            _suppressTextChanged = true;
            _editor.Text = _bound.TextBinding;
            _suppressTextChanged = false;

            // The buffer starts clean: seeding the document above is not a user edit, and leaving it
            // on the stack would let Ctrl+Z wipe the file to empty on a freshly opened tab.
            _editor.TextArea.Document.UndoStack.ClearAll();

            _colorizer.IsEngineFunction = _bound.IsEngineFunction;
            _colorizer.IsEngineConstant = _bound.IsEngineConstant;

            _bound.TextReplaced += ReplaceText;
            _bound.CanUndoProbe = () => _editor.TextArea.Document.UndoStack.CanUndo;
            _bound.CanRedoProbe = () => _editor.TextArea.Document.UndoStack.CanRedo;
            _bound.UndoRequested = () => _editor.TextArea.Document.UndoStack.Undo();
            _bound.RedoRequested = () => _editor.TextArea.Document.UndoStack.Redo();
            _bound.InsertAtCursorRequested = InsertAtCursor;
            _bound.GoToOffsetRequested = GoToOffset;
            _bound.GoToLineRequested = GoToLine;
            _bound.ReplaceAllRequested = ReplaceAllAsOneEdit;

            // The handler carries the view model it was subscribed for: unsubscribing cannot recall
            // a dispatcher callback that is already queued, so the callback itself has to notice its
            // source lost the binding race and drop the stale diagnostics.
            var source = _bound;
            _diagnosticsHandler = diagnostics => OnDiagnosticsChanged(source, diagnostics);
            _bound.DiagnosticsChanged += _diagnosticsHandler;
            _bound.AnalyzeNow();
            RefreshFolding();
        }

        private void ReplaceText(string text)
        {
            if (_editor == null)
                return;

            _suppressTextChanged = true;
            _editor.Text = text;
            _suppressTextChanged = false;
            _editor.TextArea.Document.UndoStack.ClearAll();
        }

        private void InsertAtCursor(string text)
        {
            if (_editor == null)
                return;

            _editor.Document.Insert(_editor.TextArea.Caret.Offset, text);
            _editor.Focus();
        }

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (_suppressTextChanged || _bound == null || _editor == null)
                return;

            _bound.OnTextChanged(_editor.Text);
        }

        private void OnCaretPositionChanged(object? sender, EventArgs e)
        {
            if (_bound == null || _editor == null)
                return;

            _bound.OnCaretMoved(_editor.TextArea.Caret.Line, _editor.TextArea.Caret.Column);
        }

        private void OnEditorKeyDown(object? sender, KeyEventArgs e)
        {
            if (_editor == null)
                return;

            if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                OpenSearchPanel(showReplace: false);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.H && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                OpenSearchPanel(showReplace: true);
                e.Handled = true;
                return;
            }

            if (_bound == null)
                return;

            var caret = _editor.TextArea.Caret.Offset;

            // Ctrl+Space forces the list open even mid-word, which is the one way to get at it
            // without typing a character first.
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ShowCompletion();
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                // Shift+F12 must be tested before the bare F12 case, which would otherwise match first.
                case Key.F12 when e.KeyModifiers.HasFlag(KeyModifiers.Shift):
                    _bound.FindReferences(caret);
                    e.Handled = true;
                    break;

                case Key.F12:
                    _bound.GoToDefinition(caret);
                    e.Handled = true;
                    break;

                case Key.F2:
                    _ = _bound.RenameAsync(caret);
                    e.Handled = true;
                    break;

                // Ctrl+B compiles the script in front of you. Handled here as well as by the window
                // binding so it fires reliably with the buffer focused; marking it handled stops the
                // window binding running it a second time.
                case Key.B when e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
                                _bound.CompileCommand.CanExecute(null):
                    _bound.CompileCommand.Execute(null);
                    e.Handled = true;
                    break;

                // Ctrl+/ toggles comments, the one editing command that is tedious enough by hand
                // to be worth a binding.
                case Key.OemQuestion when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                    ToggleComment();
                    e.Handled = true;
                    break;
            }
        }

        internal bool IsSearchPanelInstalledForTests => _searchPanel != null;

        internal bool IsSearchPanelOpenForTests => _searchPanel?.IsVisible == true;

        internal void OpenSearchPanelForTests(bool showReplace = false) => OpenSearchPanel(showReplace);

        private void OpenSearchPanel(bool showReplace)
        {
            if (_searchPanel == null || _findTextBox == null || _editor == null)
                return;

            SetReplaceVisible(showReplace);

            var selected = _editor.TextArea.Selection;
            if (!selected.IsEmpty)
            {
                var segment = selected.SurroundingSegment;
                var text = _editor.Document.GetText(segment);
                if (!text.Contains('\n') && !text.Contains('\r'))
                    _findTextBox.Text = text;
            }

            _searchPanel.IsVisible = true;
            _findTextBox.Focus();
            _findTextBox.SelectAll();
        }

        private void CloseSearchPanel()
        {
            if (_searchPanel == null)
                return;

            _searchPanel.IsVisible = false;
            _editor?.Focus();
        }

        private void SetReplaceVisible(bool isVisible)
        {
            if (_replaceLabel != null)
                _replaceLabel.IsVisible = isVisible;
            if (_replaceTextBox != null)
                _replaceTextBox.IsVisible = isVisible;
            if (_replaceButton != null)
                _replaceButton.IsVisible = isVisible;
            if (_replaceAllButton != null)
                _replaceAllButton.IsVisible = isVisible;
        }

        private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    FindNext(previous: e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    e.Handled = true;
                    break;
                case Key.F3:
                    FindNext(previous: e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CloseSearchPanel();
                    e.Handled = true;
                    break;
            }
        }

        private void OnFindNextClick(object? sender, RoutedEventArgs e) => FindNext(previous: false);

        private void OnFindPreviousClick(object? sender, RoutedEventArgs e) => FindNext(previous: true);

        private void OnCloseSearchClick(object? sender, RoutedEventArgs e) => CloseSearchPanel();

        private void OnReplaceClick(object? sender, RoutedEventArgs e)
        {
            if (_editor?.Document == null)
                return;

            var pattern = _findTextBox?.Text;
            if (string.IsNullOrEmpty(pattern))
                return;

            var replacement = _replaceTextBox?.Text ?? string.Empty;
            var selection = _editor.TextArea.Selection;
            if (!selection.IsEmpty)
            {
                var segment = selection.SurroundingSegment;
                var selected = _editor.Document.GetText(segment);
                if (string.Equals(selected, pattern, Comparison()))
                {
                    _editor.Document.Replace(segment.Offset, segment.Length, replacement);
                    _editor.TextArea.Caret.Offset = segment.Offset + replacement.Length;
                }
            }

            FindNext(previous: false);
        }

        private void OnReplaceAllClick(object? sender, RoutedEventArgs e)
        {
            if (_editor?.Document == null)
                return;

            var pattern = _findTextBox?.Text;
            if (string.IsNullOrEmpty(pattern))
                return;

            var replacement = _replaceTextBox?.Text ?? string.Empty;
            var comparison = Comparison();
            var source = _editor.Text;
            var offset = 0;

            using (_editor.Document.RunUpdate())
            {
                while (offset <= source.Length)
                {
                    var found = source.IndexOf(pattern, offset, comparison);
                    if (found < 0)
                        break;

                    _editor.Document.Replace(found, pattern.Length, replacement);
                    source = _editor.Text;
                    offset = found + replacement.Length;
                    if (replacement.Length == 0)
                        offset = found;
                }
            }

            FindNext(previous: false);
        }

        private bool FindNext(bool previous)
        {
            if (_editor?.Document == null)
                return false;

            var pattern = _findTextBox?.Text;
            if (string.IsNullOrEmpty(pattern))
                return false;

            var source = _editor.Text;
            if (source.Length == 0)
                return false;

            var comparison = Comparison();
            var caret = Math.Clamp(_editor.TextArea.Caret.Offset, 0, source.Length);
            int found;

            if (previous)
            {
                var start = Math.Max(0, caret - 1);
                found = source.LastIndexOf(pattern, start, comparison);
                if (found < 0)
                    found = source.LastIndexOf(pattern, source.Length - 1, comparison);
            }
            else
            {
                var start = Math.Min(source.Length, caret + CurrentSelectionLength(pattern));
                found = source.IndexOf(pattern, start, comparison);
                if (found < 0)
                    found = source.IndexOf(pattern, 0, comparison);
            }

            if (found < 0)
                return false;

            _editor.Select(found, pattern.Length);
            _editor.TextArea.Caret.BringCaretToView();
            _editor.Focus();
            return true;
        }

        private int CurrentSelectionLength(string pattern)
        {
            if (_editor?.Document == null)
                return 0;

            var selection = _editor.TextArea.Selection;
            if (selection.IsEmpty)
                return 0;

            var segment = selection.SurroundingSegment;
            var selected = _editor.Document.GetText(segment);
            return string.Equals(selected, pattern, Comparison()) ? segment.Length : 0;
        }

        private StringComparison Comparison() =>
            _matchCaseBox?.IsChecked == true ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Comments the selected lines, or uncomments them when every one is already commented.
        /// Applied as a single undo step so one Ctrl+Z takes the whole block back.
        /// </summary>
        private void ToggleComment()
        {
            if (_editor?.Document == null)
                return;

            var document = _editor.Document;
            var selection = _editor.TextArea.Selection;
            var startLine = selection.IsEmpty
                ? _editor.TextArea.Caret.Line
                : document.GetLineByOffset(selection.SurroundingSegment.Offset).LineNumber;
            var endLine = selection.IsEmpty
                ? startLine
                : document.GetLineByOffset(selection.SurroundingSegment.EndOffset).LineNumber;

            var lines = Enumerable.Range(startLine, endLine - startLine + 1)
                .Select(document.GetLineByNumber)
                .ToList();

            var allCommented = lines
                .Select(l => document.GetText(l).TrimStart())
                .Where(t => t.Length > 0)
                .All(t => t.StartsWith("//", StringComparison.Ordinal));

            using (document.RunUpdate())
            {
                foreach (var line in lines)
                {
                    var text = document.GetText(line);
                    var trimmed = text.TrimStart();
                    if (trimmed.Length == 0)
                        continue;

                    var indent = text.Length - trimmed.Length;

                    if (allCommented)
                    {
                        var after = trimmed.StartsWith("// ", StringComparison.Ordinal) ? 3 : 2;
                        document.Remove(line.Offset + indent, after);
                    }
                    else
                    {
                        document.Insert(line.Offset + indent, "// ");
                    }
                }
            }
        }

        private void OnTextEntered(object? sender, TextInputEventArgs e)
        {
            if (_editor == null || _bound == null)
                return;

            var text = e.Text;
            if (string.IsNullOrEmpty(text))
                return;

            // Identifier characters continue or begin a word; '"' opens an #include path; '(' and ','
            // move to the next argument, where the constant family for that parameter is what matters.
            if (char.IsLetter(text[0]) || text[0] == '_' || text[0] == '"' || text[0] == '(' || text[0] == ',')
                ShowCompletion();
        }

        private void OnTextEntering(object? sender, TextInputEventArgs e)
        {
            if (_completionWindow == null || string.IsNullOrEmpty(e.Text))
                return;

            // A non-identifier character commits the highlighted item rather than typing through it.
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
                _completionWindow.CompletionList.RequestInsertion(e);
        }

        private void ShowCompletion()
        {
            if (_editor == null || _bound == null)
                return;

            var source = _editor.Text;
            var caret = _editor.TextArea.Caret.Offset;

            var (items, replaceFrom) = _bound.GetCompletions(source, caret);
            if (items.Count == 0)
            {
                _completionWindow?.Close();
                return;
            }

            _completionWindow ??= CreateCompletionWindow();

            var list = _completionWindow.CompletionList.CompletionData;
            list.Clear();

            // Priority descends so AvaloniaEdit keeps Domain's ordering, which is where the
            // "constant family first, then locals, then engine symbols" intent lives.
            var priority = items.Count;
            foreach (var item in items.Take(300))
                list.Add(new ScriptCompletionData(item, replaceFrom, priority--));

            _completionWindow.Show();
        }

        /// <summary>
        /// Rebuilds brace folds. Driven off the lexer's token stream rather than raw text so a brace
        /// inside a string literal or a comment cannot open a phantom fold — legacy scripts contain
        /// plenty of both.
        /// </summary>
        private void RefreshFolding()
        {
            if (_editor?.Document == null)
                return;

            _foldingManager ??= AvaloniaEdit.Folding.FoldingManager.Install(_editor.TextArea);

            var text = _editor.Text;
            var stack = new Stack<int>();
            var foldings = new List<AvaloniaEdit.Folding.NewFolding>();

            foreach (var token in Domain.Script.Syntax.ScriptLexer.Tokenize(text))
            {
                if (token.Kind == Domain.Script.Syntax.ScriptTokenKind.BlockComment && token.Length > 60)
                {
                    foldings.Add(new AvaloniaEdit.Folding.NewFolding(token.Start, token.End) { Name = "/* ... */" });
                    continue;
                }

                if (token.Kind != Domain.Script.Syntax.ScriptTokenKind.Operator)
                    continue;

                var c = text[token.Start];
                if (c == '{')
                    stack.Push(token.Start);
                else if (c == '}' && stack.Count > 0)
                {
                    var start = stack.Pop();
                    if (token.End - start > 40)
                        foldings.Add(new AvaloniaEdit.Folding.NewFolding(start, token.End));
                }
            }

            foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            _foldingManager.UpdateFoldings(foldings, -1);
        }

        private AvaloniaEdit.Folding.FoldingManager? _foldingManager;

        private void OnDiagnosticsChanged(
            ScriptEditorViewModel source,
            IReadOnlyList<Domain.Script.Syntax.ScriptAnalysisDiagnostic> diagnostics)
        {
            // Analysis runs on a background thread; the renderer touches visual state.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!ReferenceEquals(source, _bound))
                    return;

                _squiggles.SetDiagnostics(diagnostics);
                _editor?.TextArea.TextView.InvalidateLayer(AvaloniaEdit.Rendering.KnownLayer.Selection);
                RefreshFolding();
            });
        }

        private void GoToOffset(int offset)
        {
            if (_editor?.Document == null || offset < 0)
                return;

            _editor.TextArea.Caret.Offset = Math.Clamp(offset, 0, _editor.Document.TextLength);
            _editor.TextArea.Caret.BringCaretToView();
            _editor.Focus();
        }

        /// <summary>
        /// Replaces the whole buffer as one undoable edit, so a rename is a single Ctrl+Z rather than
        /// one per occurrence. The caret is restored because Replace collapses it to the start.
        /// </summary>
        private void ReplaceAllAsOneEdit(string text)
        {
            if (_editor?.Document == null)
                return;

            var caret = _editor.TextArea.Caret.Offset;
            _editor.Document.Replace(0, _editor.Document.TextLength, text);
            _editor.TextArea.Caret.Offset = Math.Clamp(caret, 0, _editor.Document.TextLength);
        }

        /// <summary>Moves the caret to a 1-based line, for click-to-navigate from Problems.</summary>
        public void GoToLine(int line)
        {
            if (_editor == null || _editor.Document == null)
                return;

            var clamped = Math.Clamp(line, 1, _editor.Document.LineCount);
            var offset = _editor.Document.GetLineByNumber(clamped).Offset;
            _editor.TextArea.Caret.Offset = offset;
            _editor.TextArea.Caret.BringCaretToView();
            _editor.Focus();
        }

        private CompletionWindow CreateCompletionWindow()
        {
            var window = new CompletionWindow(_editor!.TextArea) { MaxHeight = 320, Width = 420 };
            window.Closed += (_, _) => _completionWindow = null;
            return window;
        }
    }
}
