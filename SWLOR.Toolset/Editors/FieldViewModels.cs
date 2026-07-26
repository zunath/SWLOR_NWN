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
        public bool IsReadOnly => Descriptor.IsReadOnly;

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

    /// <summary>2DA-backed dropdown; degrades to a numeric box when no options resolve.</summary>
    public partial class DropdownFieldViewModel : FieldViewModel
    {
        public IReadOnlyList<LookupOption> Options { get; }
        public bool HasOptions => Options.Count > 0;

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

        partial void OnRawValueChanged(long value)
        {
            if (Context.IsRefreshing || HasOptions)
                return;

            if (!Context.RunEdit($"Change {Label}",
                    () => SchemaFieldAccessor.SetInteger(Context.Document, Descriptor, value)))
                RefreshFromDocument();
        }
    }

    /// <summary>Script slots are resref text for now; a picker arrives with later packages.</summary>
    public partial class ScriptFieldViewModel : TextFieldViewModel
    {
        public ScriptFieldViewModel(FieldDescriptor descriptor, EditorFieldContext context)
            : base(descriptor, context)
        {
        }
    }
}
