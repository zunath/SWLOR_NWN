using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.AreaPropertiesHost
{
    /// <summary>Loads placement blueprints from the open module and its indexed palette resources.</summary>
    internal sealed class SwlorAreaInstanceBlueprintSource : IAreaInstanceBlueprintSource
    {
        private readonly ModuleWorkspace _workspace;

        public SwlorAreaInstanceBlueprintSource(ModuleWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public string ModuleIdentity => _workspace.ModuleRoot;

        public JsonGffDocument LoadBlueprint(ResourceType type, string resRef, bool useIndexedBlueprint) =>
            (useIndexedBlueprint
                ? _workspace.LoadIndexedBlueprint(type, resRef)
                : _workspace.LoadBlueprint(type, resRef)).Document;
    }
}
