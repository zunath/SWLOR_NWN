using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>One category node in the Module Explorer's category list (Areas, or one blueprint type), with its item count.</summary>
    public sealed record CategoryNode(ResourceType Type, string DisplayName, int Count)
    {
        public string Label => $"{DisplayName} ({Count})";
    }

    /// <summary>One leaf item in the Module Explorer's item list for the selected category.</summary>
    public sealed record ExplorerItem(string ResRef, string? Name, string? Tag)
    {
        /// <summary>
        /// The line a builder reads first. The name when there is one, and only then the resref -
        /// which the row still shows underneath in monospace, so nothing is hidden from anyone who
        /// needs to type it into a script.
        /// </summary>
        public string PrimaryText => string.IsNullOrWhiteSpace(Name) ? ResRef : Name;
    }
}
