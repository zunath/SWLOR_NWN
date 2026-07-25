namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// One resource type's slice of the sidecar: how it is grouped, its folder tree, and which folders
    /// the user has pinned.
    /// </summary>
    /// <remarks>
    /// The section is deliberately additive. It never lists what exists - the module does that - so a
    /// resref it names that has since been deleted is simply skipped, and a resref the module has that
    /// the section never mentions lands in <see cref="UnsortedFolderName"/>. Both halves of that are
    /// what make the file safe to delete, safe to merge, and incapable of corrupting content.
    /// </remarks>
    public sealed class CategorySection
    {
        /// <summary>The folder that catches everything no rule and no user placed. Always present, never stored.</summary>
        public const string UnsortedFolderName = "Unsorted";

        private readonly List<CategoryFolder> _folders = new();
        private readonly List<string> _pinned = new();

        public CategoryGrouping Grouping { get; set; } = CategoryGrouping.Automatic;

        public IReadOnlyList<CategoryFolder> Folders => _folders;

        /// <summary>Names of folders the user pinned to the top, in their chosen order.</summary>
        public IReadOnlyList<string> Pinned => _pinned;

        public CategoryFolder AddFolder(string name)
        {
            var folder = new CategoryFolder(name);
            _folders.Add(folder);
            return folder;
        }

        public void AddFolder(CategoryFolder folder, int index = -1)
        {
            ArgumentNullException.ThrowIfNull(folder);
            if (index < 0 || index > _folders.Count)
                _folders.Add(folder);
            else
                _folders.Insert(index, folder);
        }

        public bool RemoveFolder(CategoryFolder folder)
        {
            if (_folders.Remove(folder))
                return true;

            foreach (var candidate in AllFolders())
            {
                if (candidate.RemoveChild(folder))
                    return true;
            }

            return false;
        }

        public bool Pin(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return false;

            var trimmed = folderName.Trim();
            if (_pinned.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                return false;

            _pinned.Add(trimmed);
            return true;
        }

        public bool Unpin(string folderName)
        {
            var index = _pinned.FindIndex(existing => string.Equals(existing, folderName, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return false;

            _pinned.RemoveAt(index);
            return true;
        }

        /// <summary>Every folder in the tree, depth first.</summary>
        public IEnumerable<CategoryFolder> AllFolders() =>
            _folders.SelectMany(folder => folder.DescendantsAndSelf());

        /// <summary>
        /// Finds a folder by its path segments ("Interiors", "Consoles &amp; Terminals"), or null.
        /// Paths rather than ids because the sidecar stores names, and names are what a merge conflict
        /// leaves legible.
        /// </summary>
        public CategoryFolder? Find(params string[] path)
        {
            if (path.Length == 0)
                return null;

            var current = _folders.FirstOrDefault(folder => Matches(folder, path[0]));
            for (var i = 1; i < path.Length && current != null; i++)
                current = current.Children.FirstOrDefault(child => Matches(child, path[i]));

            return current;
        }

        /// <summary>The path segments that reach a folder, or an empty array when it isn't in this section.</summary>
        public IReadOnlyList<string> PathTo(CategoryFolder folder)
        {
            foreach (var root in _folders)
            {
                var path = new List<string>();
                if (TryBuildPath(root, folder, path))
                    return path;
            }

            return Array.Empty<string>();
        }

        /// <summary>The folders a resref is filed in - more than one is legal and intended.</summary>
        public IEnumerable<CategoryFolder> FoldersContaining(string resRef) =>
            AllFolders().Where(folder =>
                folder.Members.Contains(resRef, StringComparer.OrdinalIgnoreCase));

        /// <summary>Every resref filed anywhere in this section.</summary>
        public IReadOnlySet<string> AssignedResRefs()
        {
            var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in AllFolders())
                foreach (var member in folder.Members)
                    assigned.Add(member);

            return assigned;
        }

        /// <summary>
        /// The resrefs that exist in the module but are filed nowhere. Anything the section names that
        /// is absent from <paramref name="existingResRefs"/> is ignored rather than reported, so a
        /// deleted resource cannot leave a phantom row behind.
        /// </summary>
        public IReadOnlyList<string> UnsortedResRefs(IEnumerable<string> existingResRefs)
        {
            var assigned = AssignedResRefs();
            return existingResRefs
                .Where(resRef => !assigned.Contains(resRef))
                .ToList();
        }

        /// <summary>
        /// How many of <paramref name="existingResRefs"/> a folder holds, counting descendants. Counted
        /// against what exists rather than against the stored list, so the number on a folder is never
        /// larger than the number of rows it will actually show.
        /// </summary>
        public int CountIn(CategoryFolder folder, IReadOnlySet<string> existingResRefs)
        {
            ArgumentNullException.ThrowIfNull(folder);
            return folder.MembersIncludingDescendants.Count(existingResRefs.Contains);
        }

        private static bool Matches(CategoryFolder folder, string name) =>
            string.Equals(folder.Name, name, StringComparison.OrdinalIgnoreCase);

        private static bool TryBuildPath(CategoryFolder current, CategoryFolder target, List<string> path)
        {
            path.Add(current.Name);
            if (ReferenceEquals(current, target))
                return true;

            foreach (var child in current.Children)
            {
                if (TryBuildPath(child, target, path))
                    return true;
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }
    }
}
