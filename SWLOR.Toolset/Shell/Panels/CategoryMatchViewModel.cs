using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// A category that matched the palette's search, shown with the path that reaches it.
    /// </summary>
    /// <remarks>
    /// The path is the point. Once a module has hundreds of categories the same leaf name appears in
    /// several places - "Consoles" under Interiors and under Ships - so a match with no path is
    /// ambiguous exactly when it matters most.
    /// </remarks>
    public sealed record CategoryMatchViewModel(CategoryFolder Folder, string ParentPath, int Count)
    {
        public string Name => Folder.Name;

        public bool HasParentPath => !string.IsNullOrEmpty(ParentPath);
    }
}
