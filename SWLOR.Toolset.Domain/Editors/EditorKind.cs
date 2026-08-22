namespace SWLOR.Toolset.Domain.Editors
{
    /// <summary>
    /// The kind of control a field descriptor binds to in a blueprint editor. The app layer
    /// maps each kind to an Avalonia control template; the Domain layer only carries intent.
    /// </summary>
    public enum EditorKind
    {
        /// <summary>Single-line text (cexostring).</summary>
        Text,

        /// <summary>Resref text with the 16-character/lowercase constraint surfaced.</summary>
        ResRef,

        /// <summary>Integer numeric field (byte/word/short/dword/int).</summary>
        Integer,

        /// <summary>Float numeric field.</summary>
        Float,

        /// <summary>Boolean rendered as a checkbox over a byte 0/1 field.</summary>
        Check,

        /// <summary>Localized string (cexolocstring language-0 text + strref display).</summary>
        LocString,

        /// <summary>Integer id resolved against a 2DA-backed lookup (dropdown).</summary>
        TwoDaDropdown,

        /// <summary>Script slot: resref text (script picker in a later package).</summary>
        ScriptSlot,

        /// <summary>The local-variable table grid.</summary>
        VarTableGrid,

        /// <summary>
        /// A resref naming another module resource, offered as a list of what exists. Still
        /// free-text underneath: the value may legitimately name hak or base-game content.
        /// <see cref="FieldDescriptor.LookupKey"/> carries the resource extension ("dlg").
        /// </summary>
        ResourcePicker
    }
}
