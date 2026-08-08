namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>What a row in the Area Contents tree stands for.</summary>
    public enum AreaContentsNodeKind
    {
        /// <summary>A placed-instance list: Creatures, Placeables, Waypoints.</summary>
        Kind,

        /// <summary>Several placements that share a name, blueprint or tag.</summary>
        Group,

        /// <summary>One placement.</summary>
        Instance,

        /// <summary>
        /// The tail of a group too long to realise in full - "... 445 more". Not selectable and not
        /// deletable; it exists so opening the 648-copy rug cannot lock the panel building 648 rows
        /// nobody is going to read.
        /// </summary>
        Overflow
    }
}
