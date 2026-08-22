using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Shared context the field view models edit through: runs a mutation inside a one-step
    /// transaction on the owning session and notifies the editor afterwards so dirty/undo
    /// state refreshes.
    /// </summary>
    public sealed class EditorFieldContext
    {
        private readonly JsonGffDocument _document;
        private readonly Func<string, Action, bool> _runEdit;

        public JsonGffDocument Document => _document;

        /// <summary>True while the editor is loading values into view models; suppresses writes.</summary>
        public bool IsRefreshing { get; set; }

        /// <summary>
        /// Resolves a TLK strref for a localized field that carries one but no language-0 override, or
        /// null when no TLK is loaded - in which case such a field reads blank, as it did before.
        /// </summary>
        public Func<uint, string?>? ResolveStrRef { get; }

        public EditorFieldContext(
            JsonGffDocument document,
            Func<string, Action, bool> runEdit,
            Func<uint, string?>? resolveStrRef = null)
        {
            _document = document;
            _runEdit = runEdit;
            ResolveStrRef = resolveStrRef;
        }

        public bool RunEdit(string description, Action mutation)
        {
            return !IsRefreshing && _runEdit(description, mutation);
        }
    }

    /// <summary>Base of all per-field view models; subclass per EditorKind for clean templates.</summary>
    public abstract partial class FieldViewModel : ObservableObject
    {
        protected readonly EditorFieldContext Context;
        public FieldDescriptor Descriptor { get; }

        public string Label => Descriptor.Label;
        public string? Description => Descriptor.Description;
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool IsReadOnly => Descriptor.IsReadOnly;
        public bool IsMultiline => Descriptor.IsMultiline;
        public bool IsSingleLine => !Descriptor.IsMultiline;

        protected FieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
        {
            Descriptor = descriptor;
            Context = context;
        }

        /// <summary>Reloads the view model's value from the document (after undo/redo).</summary>
        public abstract void RefreshFromDocument();
    }

    public partial class TextFieldViewModel : FieldViewModel
    {
        [ObservableProperty]
        private string _text = string.Empty;

        public TextFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
            : base(descriptor, context)
        {
            RefreshFromDocument();
        }

        public sealed override void RefreshFromDocument()
        {
            Context.IsRefreshing = true;
            Text = SchemaFieldAccessor.GetText(Context.Document, Descriptor, Context.ResolveStrRef);
            Context.IsRefreshing = false;
            OnTextCommitted();
        }

        /// <summary>
        /// Called after the text settles, whether from an edit or a document refresh. Subclasses
        /// override it to re-evaluate anything derived from the value — a script slot uses it to
        /// re-check whether the script it names still exists.
        /// </summary>
        protected virtual void OnTextCommitted()
        {
        }

        partial void OnTextChanged(string value)
        {
            if (Context.IsRefreshing)
            {
                OnTextCommitted();
                return;
            }

            if (!Context.RunEdit($"Change {Label}",
                    () => SchemaFieldAccessor.SetText(Context.Document, Descriptor, value)))
                RefreshFromDocument();
            else
                OnTextCommitted();
        }
    }

    public partial class IntegerFieldViewModel : FieldViewModel
    {
        [ObservableProperty]
        private long _value;

        public IntegerFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
            : base(descriptor, context)
        {
            RefreshFromDocument();
        }

        public sealed override void RefreshFromDocument()
        {
            Context.IsRefreshing = true;
            Value = SchemaFieldAccessor.GetInteger(Context.Document, Descriptor);
            Context.IsRefreshing = false;
        }

        partial void OnValueChanged(long value)
        {
            if (Context.IsRefreshing)
                return;

            if (!Context.RunEdit($"Change {Label}",
                    () => SchemaFieldAccessor.SetInteger(Context.Document, Descriptor, value)))
                RefreshFromDocument();
        }
    }

    public partial class FloatFieldViewModel : FieldViewModel
    {
        [ObservableProperty]
        private double _value;

        public FloatFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
            : base(descriptor, context)
        {
            RefreshFromDocument();
        }

        public sealed override void RefreshFromDocument()
        {
            Context.IsRefreshing = true;
            Value = SchemaFieldAccessor.GetFloat(Context.Document, Descriptor);
            Context.IsRefreshing = false;
        }

        partial void OnValueChanged(double value)
        {
            if (Context.IsRefreshing)
                return;

            if (!Context.RunEdit($"Change {Label}",
                    () => SchemaFieldAccessor.SetFloat(Context.Document, Descriptor, value)))
                RefreshFromDocument();
        }
    }

    public partial class CheckFieldViewModel : FieldViewModel
    {
        [ObservableProperty]
        private bool _isChecked;

        public CheckFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
            : base(descriptor, context)
        {
            RefreshFromDocument();
        }

        public sealed override void RefreshFromDocument()
        {
            Context.IsRefreshing = true;
            IsChecked = SchemaFieldAccessor.GetBool(Context.Document, Descriptor);
            Context.IsRefreshing = false;
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (Context.IsRefreshing)
                return;

            if (!Context.RunEdit($"Toggle {Label}",
                    () => SchemaFieldAccessor.SetBool(Context.Document, Descriptor, value)))
                RefreshFromDocument();
        }
    }

    /// <summary>LocString fields edit the language-0 text; the strref (if any) is displayed.</summary>
    public partial class LocStringFieldViewModel : FieldViewModel
    {
        [ObservableProperty]
        private string _text = string.Empty;

        public string? StrRefDisplay { get; private set; }

        public LocStringFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
            : base(descriptor, context)
        {
            RefreshFromDocument();
        }

        public sealed override void RefreshFromDocument()
        {
            Context.IsRefreshing = true;

            // Deliberately the override only, not the resolved strref. This box edits the language-0
            // text, so showing TLK text in it would invite a stray keystroke to promote a strref-backed
            // name into a literal override of the same words - a silent change to what the field means.
            // The TLK text belongs beside the strref instead, where it explains the blank rather than
            // pretending to be it.
            Text = SchemaFieldAccessor.GetText(Context.Document, Descriptor);

            var field = Context.Document.Root.GetOrNull(Descriptor.FieldName);
            if (field?.GetLocStringId() is { } id)
            {
                var resolved = Context.ResolveStrRef?.Invoke(id);
                StrRefDisplay = string.IsNullOrWhiteSpace(resolved)
                    ? $"strref {id}"
                    : $"strref {id} - “{resolved}”";
            }
            else
            {
                StrRefDisplay = null;
            }

            OnPropertyChanged(nameof(StrRefDisplay));
            Context.IsRefreshing = false;
        }

        partial void OnTextChanged(string value)
        {
            if (Context.IsRefreshing)
                return;

            if (!Context.RunEdit($"Change {Label}",
                    () => SchemaFieldAccessor.SetText(Context.Document, Descriptor, value)))
                RefreshFromDocument();
        }
    }

    /// <summary>
    /// 2DA-backed dropdown. When its lookup is unavailable, the stored numeric value remains
    /// visible but read-only so missing metadata cannot turn a constrained field into free input.
    /// </summary>
    public partial class DropdownFieldViewModel : FieldViewModel
    {
        public IReadOnlyList<LookupOption> Options { get; }
        public bool HasOptions => Options.Count > 0;
        public string LookupUnavailableMessage =>
            "2DA metadata unavailable. The stored value is shown read-only.";

        [ObservableProperty]
        private LookupOption? _selectedOption;

        [ObservableProperty]
        private long _rawValue;

        public DropdownFieldViewModel(
            FieldDescriptor descriptor, EditorFieldContext context, IReadOnlyList<LookupOption> options)
            : base(descriptor, context)
        {
            var unset = DropdownValueValidator.GetUnsetSentinel(descriptor.FieldType);
            Options = options.Count == 0 || descriptor.IsRequired || options.Any(option => option.Id == unset)
                ? options
                : new[] { new LookupOption(unset, "(None)") }.Concat(options).ToList();
            RefreshFromDocument();
        }

        public sealed override void RefreshFromDocument()
        {
            Context.IsRefreshing = true;
            RawValue = SchemaFieldAccessor.GetInteger(Context.Document, Descriptor);
            SelectedOption = Options.FirstOrDefault(option => option.Id == RawValue);
            Context.IsRefreshing = false;
        }

        partial void OnSelectedOptionChanged(LookupOption? value)
        {
            if (Context.IsRefreshing || value == null)
                return;

            if (!Context.RunEdit($"Change {Label}",
                    () => SchemaFieldAccessor.SetInteger(Context.Document, Descriptor, value.Id)))
                RefreshFromDocument();
        }

    }

    /// <summary>
    /// A script slot: resref text, plus the ability to browse, open and create the script it names.
    /// </summary>
    /// <remarks>
    /// The missing-script warning is the point. A slot pointing at a script that does not exist is a
    /// live and otherwise invisible class of bug — 2,250 module resources name a script by resref,
    /// nothing validates them, and the failure only shows up in-game as an event that silently does
    /// nothing.
    /// </remarks>
    public partial class ScriptFieldViewModel : TextFieldViewModel
    {
        private readonly IScriptSlotHost? _host;

        public ScriptFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context, IScriptSlotHost? host = null)
            : base(descriptor, context)
        {
            _host = host;
        }

        public bool CanBrowse => _host != null;

        /// <summary>True when this slot names a script that is not in the module.</summary>
        public bool IsMissing =>
            _host != null && !string.IsNullOrWhiteSpace(Text) && !_host.ScriptExists(Text);

        public string MissingMessage => $"'{Text}' does not exist in this module.";

        [RelayCommand]
        private async Task Browse()
        {
            if (_host == null)
                return;

            var chosen = await _host.PickScriptAsync(Text).ConfigureAwait(true);
            if (chosen != null)
                Text = chosen;
        }

        [RelayCommand]
        private void OpenScript()
        {
            if (!string.IsNullOrWhiteSpace(Text))
                _host?.OpenScript(Text);
        }

        protected override void OnTextCommitted() => RaiseMissing();

        private void RaiseMissing()
        {
            OnPropertyChanged(nameof(IsMissing));
            OnPropertyChanged(nameof(MissingMessage));
        }
    }

    /// <summary>What a script slot needs from the app to browse, open and validate.</summary>
    public interface IScriptSlotHost
    {
        bool ScriptExists(string resRef);

        void OpenScript(string resRef);

        /// <summary>Shows the picker, returning the chosen resref or null if cancelled.</summary>
        Task<string?> PickScriptAsync(string current);
    }

    /// <summary>
    /// A resref field backed by a list of what actually exists — the conversation picker on a
    /// creature, door or placeable.
    /// </summary>
    /// <remarks>
    /// Still a text box underneath, and deliberately: the field can legitimately name something the
    /// module does not contain (a conversation from a hak, or one about to be created), so the list
    /// is a convenience rather than a constraint. What it fixes is the ordinary case, where a
    /// builder had to remember a sixteen-character resref exactly and got no warning when they did
    /// not.
    /// </remarks>
    public partial class ResourcePickerFieldViewModel : TextFieldViewModel
    {
        public ResourcePickerFieldViewModel(
            FieldDescriptor descriptor,
            EditorFieldContext context,
            IReadOnlyList<string> choices)
            : base(descriptor, context)
        {
            Choices = choices;
        }

        /// <summary>Everything of the right kind in the module, for the drop-down.</summary>
        public IReadOnlyList<string> Choices { get; }

        /// <summary>True when the current value names nothing in the module.</summary>
        public bool IsUnknown =>
            !string.IsNullOrWhiteSpace(Text)
            && !Choices.Contains(Text, StringComparer.OrdinalIgnoreCase);

        public string UnknownHint => IsUnknown
            ? "Nothing in the module has this name. It may come from a hak, or it may be a typo."
            : string.Empty;

        protected override void OnTextCommitted()
        {
            OnPropertyChanged(nameof(IsUnknown));
            OnPropertyChanged(nameof(UnknownHint));
        }
    }
}
