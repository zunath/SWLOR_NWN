using Nwn.Authoring.Documents.Native;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Editors.AreaPropertiesHost
{
    /// <summary>Reads the module's custom palette (<c>itp/*palcus.itp.json</c>) for an instance section's Add flow.</summary>
    internal sealed class SwlorAreaInstancePaletteSource : IAreaInstancePaletteSource
    {
        private readonly ModuleWorkspace _workspace;

        public SwlorAreaInstancePaletteSource(ModuleWorkspace workspace)
        {
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public bool TryLocate(ResourceType type, out string source)
        {
            source = Path.Combine(_workspace.ModuleRoot, "itp", PaletteFileName(type));
            return File.Exists(source);
        }

        public IReadOnlyList<PaletteNode> Read(string source) => ItpDocument.Load(source).Nodes;

        private static string PaletteFileName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utc => "creaturepalcus.itp.json",
                ResourceType.Utp => "placeablepalcus.itp.json",
                ResourceType.Utd => "doorpalcus.itp.json",
                ResourceType.Utw => "waypointpalcus.itp.json",
                ResourceType.Utm => "storepalcus.itp.json",
                ResourceType.Uts => "soundpalcus.itp.json",
                ResourceType.Utt => "triggerpalcus.itp.json",
                ResourceType.Uti => "itempalcus.itp.json",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "No palette file mapping for this type.")
            };
        }
    }
}
