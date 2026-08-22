namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>How one row of a behavior editor is presented.</summary>
    public enum BehaviorFieldKind
    {
        Text,

        /// <summary>A CExoLocString — the object's displayed name.</summary>
        LocalizedText,

        /// <summary>Free text over several lines — an exploration note's message, say.</summary>
        Paragraph,

        Integer,

        Float,

        Check,

        /// <summary>A resref naming a registered script handler.</summary>
        Script,

        /// <summary>A fixed set of named values (type, destination, faction, and so on).</summary>
        Choice,

        /// <summary>A tag that must resolve somewhere in the module, with the resolution shown.</summary>
        TagReference,

        /// <summary>Not editable: something true about the behavior that the editor can state.</summary>
        Statement,

        /// <summary>A set of choices stored as a contiguous local-variable sequence.</summary>
        MultiChoice,

        /// <summary>A choice whose selection writes more than one backing field.</summary>
        CompositeChoice,

        /// <summary>An ordered GFF list of structs, each carrying one sound ResRef.</summary>
        SoundList
    }
}
