using SWLOR.Toolset.Domain.Editors;

namespace SWLOR.Toolset.Editors
{
    /// <summary>
    /// Builds the field view model a <see cref="FieldDescriptor"/>'s <see cref="EditorKind"/> calls
    /// for. Shared by every schema-driven editor so a new kind is wired in exactly one place.
    /// </summary>
    public static class FieldViewModelFactory
    {
        /// <param name="scriptSlotHost">
        /// Lets a script slot browse, open and validate the script it names. Null leaves the slot as
        /// plain resref text, which is what it was before the picker existed.
        /// </param>
        /// <param name="resourceChoices">
        /// Resolves the module resources of one kind, keyed by extension, for a resref field that
        /// names another resource — a creature's conversation, say. Null leaves it free text.
        /// </param>
        public static FieldViewModel Create(
            FieldDescriptor descriptor,
            EditorFieldContext context,
            LookupOptionProvider lookups,
            IScriptSlotHost? scriptSlotHost = null,
            Func<string?, IReadOnlyList<string>>? resourceChoices = null)
        {
            return descriptor.Kind switch
            {
                EditorKind.Integer => new IntegerFieldViewModel(descriptor, context),
                EditorKind.Float => new FloatFieldViewModel(descriptor, context),
                EditorKind.Check => new CheckFieldViewModel(descriptor, context),
                EditorKind.LocString => new LocStringFieldViewModel(descriptor, context),
                EditorKind.TwoDaDropdown => new DropdownFieldViewModel(
                    descriptor, context, lookups.GetOptions(descriptor.LookupKey)),
                EditorKind.ScriptSlot => new ScriptFieldViewModel(descriptor, context, scriptSlotHost),
                EditorKind.ResourcePicker => new ResourcePickerFieldViewModel(
                    descriptor, context,
                    resourceChoices?.Invoke(descriptor.LookupKey) ?? Array.Empty<string>()),
                _ => new TextFieldViewModel(descriptor, context)
            };
        }
    }
}
