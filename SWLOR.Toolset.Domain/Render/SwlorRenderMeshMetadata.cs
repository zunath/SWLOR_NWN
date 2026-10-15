using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Toolset.Domain.Render;

/// <summary>SWLOR appearance classification associated with one shared render mesh.</summary>
public sealed class SwlorRenderMeshMetadata
{
    public AppearanceArmor ArmorPart { get; set; } = AppearanceArmor.Invalid;
}
