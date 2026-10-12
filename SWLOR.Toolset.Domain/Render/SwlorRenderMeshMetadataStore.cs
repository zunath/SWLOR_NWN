using System.Runtime.CompilerServices;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Nwn.Preview.Scene;

namespace SWLOR.Toolset.Domain.Render;

/// <summary>Attaches SWLOR-only appearance classification without adding it to neutral geometry.</summary>
public static class SwlorRenderMeshMetadataStore
{
    private static readonly ConditionalWeakTable<RenderMesh, SwlorRenderMeshMetadata> Metadata = new();

    public static AppearanceArmor GetArmorPart(RenderMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        return Metadata.TryGetValue(mesh, out var value)
            ? value.ArmorPart
            : AppearanceArmor.Invalid;
    }

    public static void SetArmorPart(RenderMesh mesh, AppearanceArmor armorPart)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        Metadata.GetValue(mesh, _ => new SwlorRenderMeshMetadata()).ArmorPart = armorPart;
    }

    public static void Copy(RenderMesh source, RenderMesh destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (Metadata.TryGetValue(source, out var value))
            SetArmorPart(destination, value.ArmorPart);
    }
}
