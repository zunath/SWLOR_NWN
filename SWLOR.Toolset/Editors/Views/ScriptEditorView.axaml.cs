using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;

namespace SWLOR.Toolset.Editors.Views
{
    public partial class ScriptEditorView : UserControl
    {
        private TextEditor? _editor;
        private ScriptEditorViewModel? _bound;
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

            _bound.TextReplaced += ReplaceText;
            _bound.CanUndoProbe = () => _editor.TextArea.Document.UndoStack.CanUndo;
            _bound.CanRedoProbe = () => _editor.TextArea.Document.UndoStack.CanRedo;
            _bound.UndoRequested = () => _editor.TextArea.Document.UndoStack.Undo();
            _bound.RedoRequested = () => _editor.TextArea.Document.UndoStack.Redo();
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
    }
}
