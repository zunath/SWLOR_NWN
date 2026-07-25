namespace SWLOR.Toolset.Domain.Categories
{
    /// <summary>
    /// One user-defined folder in a category tree: a display name, nested child folders, and the
    /// resrefs filed directly under it.
    /// </summary>
    /// <remarks>
    /// Membership is a plain list of resrefs rather than a parent pointer on each resource, which is
    /// what lets one resource sit in several folders at once (a crate in both "Cargo" and "Veles
    /// Dressing") without duplicating folders. Nothing here mirrors resource content - only names and
    /// resrefs - so this tree can never be a second, competing copy of a blueprint.
    /// </remarks>
    public sealed class CategoryFolder
    {
        private readonly List<CategoryFolder> _children = new();
        private readonly List<string> _members = new();

        public CategoryFolder(string name)
        {
            Name = Normalize(name);
        }

        public string Name { get; private set; }

        public IReadOnlyList<CategoryFolder> Children => _children;

        /// <summary>Resrefs filed directly in this folder, in insertion order, without duplicates.</summary>
        public IReadOnlyList<string> Members => _members;

        /// <summary>Resrefs in this folder and every folder beneath it, de-duplicated.</summary>
        public IEnumerable<string> MembersIncludingDescendants
        {
            get
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var resRef in DescendantsAndSelf().SelectMany(folder => folder._members))
                {
                    if (seen.Add(resRef))
                        yield return resRef;
                }
            }
        }

        public void Rename(string name) => Name = Normalize(name);

        public CategoryFolder AddChild(string name)
        {
            var child = new CategoryFolder(name);
            _children.Add(child);
            return child;
        }

        public void AddChild(CategoryFolder child, int index = -1)
        {
            ArgumentNullException.ThrowIfNull(child);
            if (index < 0 || index > _children.Count)
                _children.Add(child);
            else
                _children.Insert(index, child);
        }

        public bool RemoveChild(CategoryFolder child) => _children.Remove(child);

        /// <summary>Files a resref here. Returns false when it was already present.</summary>
        public bool AddMember(string resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return false;

            var trimmed = resRef.Trim();
            if (_members.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                return false;

            _members.Add(trimmed);
            return true;
        }

        public bool RemoveMember(string resRef)
        {
            var index = _members.FindIndex(existing => string.Equals(existing, resRef, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return false;

            _members.RemoveAt(index);
            return true;
        }

        /// <summary>This folder followed by every folder beneath it, depth first.</summary>
        public IEnumerable<CategoryFolder> DescendantsAndSelf()
        {
            yield return this;
            foreach (var descendant in _children.SelectMany(child => child.DescendantsAndSelf()))
                yield return descendant;
        }

        private static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A category needs a name.", nameof(name));

            return name.Trim();
        }
    }
}
