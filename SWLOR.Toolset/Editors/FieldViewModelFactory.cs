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
        public static FieldViewModel Create(
            FieldDescriptor descriptor,
            EditorFieldContext context,
            LookupOptionProvider lookups,
            IScriptSlotHost? scriptSlotHost = null)
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
                _ => new TextFieldViewModel(descriptor, context)
            };
        }
    }
}
