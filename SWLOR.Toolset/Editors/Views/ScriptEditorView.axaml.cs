using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Editors.Script;

namespace SWLOR.Toolset.Editors.Views
{
    public partial class ScriptEditorView : UserControl
    {
        private readonly NwScriptColorizer _colorizer = new();
        private TextEditor? _editor;
        private ScriptEditorViewModel? _bound;
        private CompletionWindow? _completionWindow;
        private bool _suppressTextChanged;

        public ScriptEditorView()
        {
            InitializeComponent();
            _editor = this.FindControl<TextEditor>("Editor");
            DataContextChanged += OnDataContextChanged;

            if (_editor != null)
            {
                _editor.TextChanged += OnEditorTextChanged;
                _editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
                _editor.TextArea.TextView.LineTransformers.Add(_colorizer);
                _editor.TextArea.TextEntered += OnTextEntered;
                _editor.TextArea.TextEntering += OnTextEntering;
                _editor.AddHandler(KeyDownEvent, OnEditorKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_bound != null)
                _bound.TextReplaced -= ReplaceText;

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
            // Ctrl+Space forces the list open even mid-word, which is the one way to get at it
            // without typing a character first.
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ShowCompletion();
                e.Handled = true;
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

        private CompletionWindow CreateCompletionWindow()
        {
            var window = new CompletionWindow(_editor!.TextArea) { MaxHeight = 320, Width = 420 };
            window.Closed += (_, _) => _completionWindow = null;
            return window;
        }
    }
}
