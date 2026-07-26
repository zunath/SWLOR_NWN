using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Which area defines each waypoint and door tag in the module — what lets a transition's
    /// destination say "in moseis_cantina" rather than leaving the builder to guess.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built once, on first use, by reading every area's .git. That is a few hundred files, so it is
    /// deliberately lazy: nothing pays for the index until something asks a tag question.
    /// </para>
    /// <para>
    /// An instance's own Tag wins, and a blank one falls back to the blueprint's — which is how the
    /// engine resolves it, and the difference between 17 apparently-dangling transitions and the 16
    /// that really are.
    /// </para>
    /// </remarks>
    public sealed class ModuleTagIndex
    {
        private readonly ModuleWorkspace _workspace;
        private readonly object _syncRoot = new();
        private Dictionary<string, string>? _areasByTag;

        public ModuleTagIndex(ModuleWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        /// <summary>The area defining this waypoint or door tag, or null when nothing does.</summary>
        public string? FindAreaDefiningTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            return Index().GetValueOrDefault(tag);
        }

        /// <summary>
        /// Every tag the module defines, for the pickers that offer one rather than asking
        /// about a tag already stored - the placeable teleporter's destination, for instance.
        /// </summary>
        public IReadOnlyCollection<string> Tags => Index().Keys.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToList();

        /// <summary>Drops the cache so the next question re-reads the module.</summary>
        public void Invalidate()
        {
            lock (_syncRoot)
                _areasByTag = null;
        }

        private Dictionary<string, string> Index()
        {
            lock (_syncRoot)
            {
                if (_areasByTag != null)
                    return _areasByTag;

                var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var areaResRef in _workspace.EnumerateAreaResRefs())
                {
                    GitDocument git;
                    try
                    {
                        git = _workspace.LoadArea(areaResRef).Git;
                    }
                    catch (Exception)
                    {
                        // One unreadable area must not cost every other area its tags.
                        continue;
                    }

                    AddTags(index, areaResRef, git.Waypoints, ResourceType.Utw);
                    AddTags(index, areaResRef, git.Doors, ResourceType.Utd);
                }

                _areasByTag = index;
                return index;
            }
        }

        private void AddTags(
            Dictionary<string, string> index,
            string areaResRef,
            IReadOnlyList<Gff.JsonGffStruct> instances,
            ResourceType blueprintType)
        {
            foreach (var instance in instances)
            {
                var tag = instance.GetStringOrNull("Tag");
                if (string.IsNullOrEmpty(tag))
                    tag = BlueprintTag(blueprintType, instance.GetStringOrNull("TemplateResRef"));

                if (!string.IsNullOrEmpty(tag))
                    index.TryAdd(tag, areaResRef);
            }
        }

        private string? BlueprintTag(ResourceType type, string? resRef)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return null;

            try
            {
                var path = _workspace.GetResourcePath(type, resRef);
                return File.Exists(path)
                    ? Gff.JsonGffDocument.Load(path).Root.GetStringOrNull("Tag")
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
