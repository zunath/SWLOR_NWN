namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>How one row of the trigger editor is presented.</summary>
    public enum TriggerFieldKind
    {
        Text,

        /// <summary>A CExoLocString — the trigger's displayed name.</summary>
        LocalizedText,

        /// <summary>Free text over several lines — an exploration note's message, say.</summary>
        Paragraph,

        Integer,

        Float,

        Check,

        /// <summary>A resref naming a registered script handler.</summary>
        Script,

        /// <summary>A fixed set of named values (trigger type, link target, faction).</summary>
        Choice,

        /// <summary>A tag that must resolve somewhere in the module, with the resolution shown.</summary>
        TagReference,

        /// <summary>Not editable: something true about this trigger that the editor can state.</summary>
        Statement
    }
}
