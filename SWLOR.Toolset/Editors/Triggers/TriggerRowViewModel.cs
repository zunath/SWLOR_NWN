using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Triggers
{
    /// <summary>
    /// One row of the trigger editor: a label on the left and its value on the right, in the shape
    /// the placeable editor uses.
    /// </summary>
    /// <remarks>
    /// Deliberately one view model rather than a subclass per kind. Every row shares the same
    /// geometry and differs only in which control sits in the value cell, so the alternative was
    /// nine near-identical classes and nine near-identical DataTemplates; the kind flags below let a
    /// single template switch the control instead.
    /// </remarks>
    public sealed partial class TriggerRowViewModel : ObservableObject
    {
        private readonly TriggerValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, string?>? _resolveTag;
        private bool _loading;

        public TriggerFieldDefinition Definition { get; }

        public string Label => Definition.Label;

        public bool IsRequired => Definition.IsRequired;

        public string? Note => Definition.Note;

        public bool HasNote => !string.IsNullOrEmpty(Definition.Note);

        public bool IsPerPlacement => Definition.IsPerPlacement;

        /// <summary>Resolved at construction, so a game-data choice set and a fixed one read alike.</summary>
        public IReadOnlyList<TriggerChoice> Choices { get; }

        public bool IsText => Definition.Kind is TriggerFieldKind.Text or TriggerFieldKind.Script;
        public bool IsLocalizedText => Definition.Kind == TriggerFieldKind.LocalizedText;
        public bool IsParagraph => Definition.Kind == TriggerFieldKind.Paragraph;
        public bool IsNumber => Definition.Kind is TriggerFieldKind.Integer or TriggerFieldKind.Float;
        public bool IsCheck => Definition.Kind == TriggerFieldKind.Check;
        public bool IsChoice => Definition.Kind == TriggerFieldKind.Choice;
        public bool IsTagReference => Definition.Kind == TriggerFieldKind.TagReference;
        public bool IsStatement => Definition.Kind == TriggerFieldKind.Statement;

        /// <summary>Every kind that shows a single-line text box: text, scripts, names and tags.</summary>
        public bool IsTextEntry => IsText || IsTagReference || IsLocalizedText;

        [ObservableProperty]
        private string _text = string.Empty;

        [ObservableProperty]
        private decimal _number;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private TriggerChoice? _choice;

        /// <summary>Live feedback beside the value: where a tag resolved, or why it did not.</summary>
        [ObservableProperty]
        private string? _status;

        [ObservableProperty]
        private bool _isStatusGood;

        public TriggerRowViewModel(
            TriggerFieldDefinition definition,
            TriggerValueStore store,
            Func<string, Action, bool> runEdit,
            Func<string, string?>? resolveTag,
            IReadOnlyList<TriggerChoice>? choices = null)
        {
            Definition = definition;
            _store = store;
            _runEdit = runEdit;
            _resolveTag = resolveTag;
            Choices = choices ?? definition.Choices;
            Reload();
        }

        /// <summary>Re-reads this row's value from the document, after an undo or a behavior swap.</summary>
        public void Reload()
        {
            if (Definition.Kind == TriggerFieldKind.Statement)
                return;

            _loading = true;
            try
            {
                switch (Definition.Kind)
                {
                    case TriggerFieldKind.Check:
                        IsChecked = _store.GetInteger(Definition.Storage, Definition.Name) == 1;
                        break;
                    case TriggerFieldKind.Integer:
                        Number = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                        break;
                    case TriggerFieldKind.Float:
                        Number = (decimal)(_store.GetFloat(Definition.Storage, Definition.Name) ?? 0);
                        break;
                    case TriggerFieldKind.Choice:
                        var current = _store.GetInteger(Definition.Storage, Definition.Name) ?? 0;
                        Choice = Choices.FirstOrDefault(option => option.Value == current)
                                 ?? Choices.FirstOrDefault();
                        break;
                    case TriggerFieldKind.LocalizedText:
                        Text = _store.GetLocalizedText(Definition.Name);
                        break;
                    default:
                        Text = _store.GetString(Definition.Storage, Definition.Name);
                        break;
                }
            }
            finally
            {
                _loading = false;
            }

            UpdateStatus();
        }

        partial void OnTextChanged(string value)
        {
            if (_loading)
                return;

            var applied = Definition.Kind == TriggerFieldKind.LocalizedText
                ? _runEdit($"Change {Label}", () => _store.SetLocalizedText(Definition.Name, value))
                : _runEdit($"Change {Label}",
                    () => _store.SetString(Definition.Storage, Definition.Name, Definition.FieldType, value));

            if (!applied)
                Reload();
            else
                UpdateStatus();
        }

        partial void OnNumberChanged(decimal value)
        {
            if (_loading)
                return;

            var applied = Definition.Kind == TriggerFieldKind.Float
                ? _runEdit($"Change {Label}",
                    () => _store.SetFloat(Definition.Storage, Definition.Name, (double)value))
                : _runEdit($"Change {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, (long)value));

            if (!applied)
                Reload();
        }

        partial void OnIsCheckedChanged(bool value)
        {
            if (_loading)
                return;

            if (!_runEdit($"Toggle {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value ? 1 : 0)))
                Reload();
        }

        partial void OnChoiceChanged(TriggerChoice? value)
        {
            if (_loading || value == null)
                return;

            if (!_runEdit($"Change {Label}",
                    () => _store.SetInteger(Definition.Storage, Definition.Name, Definition.FieldType, value.Value)))
                Reload();
            else
                UpdateStatus();
        }

        /// <summary>
        /// A required row that is empty says so, and a tag row says which area its target lives in —
        /// the check that catches a doorway pointing at a tag no area defines.
        /// </summary>
        private void UpdateStatus()
        {
            if (Definition.Kind == TriggerFieldKind.TagReference && Text.Length > 0)
            {
                if (_resolveTag == null)
                {
                    Status = null;
                    return;
                }

                var area = _resolveTag(Text);
                IsStatusGood = area != null;
                Status = area != null ? $"✓ in {area}" : "✗ no area defines this tag";
                return;
            }

            if (IsRequired && IsTextEntry && Text.Length == 0)
            {
                IsStatusGood = false;
                Status = "required";
                return;
            }

            Status = null;
        }
    }
}
