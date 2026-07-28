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

        /// <summary>
        /// True when this folder's <see cref="Name"/> is an unresolved "Category N" placeholder
        /// <see cref="Categories.ItpCategoryImporter"/> wrote because no TLK was available to look up
        /// the real name at import time.
        /// </summary>
        /// <remarks>
        /// Set only by the importer at the point it invents the placeholder text - never inferred
        /// elsewhere from the shape of <see cref="Name"/>. A builder can deliberately name a folder
        /// "Category 7", and that name is textually indistinguishable from a real placeholder; only
        /// this explicit marker tells <c>CategoryService.RepairPlaceholderNames</c> which folders it
        /// is safe to rename. Cleared by <see cref="Rename"/>, since any rename - whether that repair
        /// or a builder's own - resolves the name and the folder is no longer a pending placeholder.
        /// </remarks>
        public bool IsUnresolvedPlaceholder { get; set; }

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

        public void Rename(string name)
        {
            Name = Normalize(name);
            IsUnresolvedPlaceholder = false;
        }

        /// <summary>
        /// True when <paramref name="name"/> is free among this folder's children, ignoring
        /// <paramref name="except"/> (the folder being renamed, which may keep its own name).
        /// </summary>
        /// <remarks>
        /// Sibling names have to be unique because a path key is built from names: two children called
        /// the same thing share a key, so pinning or locating the second resolved the first, and
        /// toggling either pin could unpin the other.
        /// </remarks>
        public bool IsNameAvailable(string name, CategoryFolder? except = null)
        {
            var candidate = name?.Trim();
            if (string.IsNullOrEmpty(candidate))
                return false;

            foreach (var child in _children)
            {
                if (!ReferenceEquals(child, except) &&
                    string.Equals(child.Name, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public CategoryFolder AddChild(string name)
        {
            var child = new CategoryFolder(name);
            if (!IsNameAvailable(child.Name))
                throw new ArgumentException($"This category already has a '{child.Name}' folder.", nameof(name));

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

        /// <summary>The character <c>PathKey</c> joins segments with, and so the one a name may not contain.</summary>
        public const char PathSeparator = '/';

        /// <summary>What <see cref="Sanitize"/> puts in place of a <see cref="PathSeparator"/>.</summary>
        public const char PathSeparatorReplacement = '-';

        /// <summary>
        /// A name from outside this build turned into one the constructor will accept, or null when
        /// nothing usable is left. Names that are already legal come back trimmed and otherwise untouched.
        /// </summary>
        /// <remarks>
        /// The constructor is deliberately strict: a name with a separator in it cannot be addressed, so
        /// code that invents one has a bug and should hear about it. Data does not have that luxury. A
        /// sidecar written before the rule existed, and a base-game palette category such as "Skin/Hide",
        /// both carry the separator, and throwing at them means the module cannot be opened at all - so
        /// every path that reads a name from a file goes through here first. Repairing rather than dropping
        /// keeps the folder and everything filed in it; the name is the only thing that changes.
        /// </remarks>
        public static string? Sanitize(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var repaired = name.Replace(PathSeparator, PathSeparatorReplacement).Trim();
            return string.IsNullOrWhiteSpace(repaired) ? null : repaired;
        }

        /// <summary>
        /// Why a name a builder typed would be refused, or null when it is fine.
        /// </summary>
        /// <remarks>
        /// <see cref="Sanitize"/> is for a name that arrives from a file or a palette, where nobody is
        /// around to be told and quietly repairing it is the only thing that keeps the module openable.
        /// A name someone just typed is the opposite case: they are right there, so say what is wrong
        /// and let them retype it. Neither of those wants the constructor's throw - a command handler
        /// is not a place an exception has anywhere useful to go, and silently hyphenating what a
        /// builder typed would leave them looking at a name they did not ask for.
        /// </remarks>
        public static string? NameProblem(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "A category needs a name.";

            return name.IndexOf(PathSeparator) >= 0
                ? $"A category name cannot contain '{PathSeparator}' - it separates folders in a category path."
                : null;
        }

        private static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A category needs a name.", nameof(name));

            var trimmed = name.Trim();

            // Paths are joined with '/' and split on it again, unescaped. A folder called
            // "Weapons/Melee" produced a key that FindByPathKey read as two nested folders, so pinning
            // it stored a path that resolved to nothing - or to another branch entirely - and the pin
            // vanished on the next refresh.
            if (trimmed.IndexOf(PathSeparator) >= 0)
            {
                throw new ArgumentException(
                    $"A category name cannot contain '{PathSeparator}' - it separates folders in a category path.",
                    nameof(name));
            }

            return trimmed;
        }
    }
}
