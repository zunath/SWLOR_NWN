using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Toolset.Domain.Render;
using Nwn.Preview.Thumbnails;

namespace SWLOR.Toolset.Tests.Render;

[TestFixture]
public sealed class SwlorRenderMeshMetadataStoreTests
{
    [Test]
    public void TileGroupCompositionRetainsArmorClassificationOnClonedMeshes()
    {
        var source = new RenderMesh
        {
            NodeName = "robe",
            TextureName = "robe_texture",
            Positions = [0f, 0f, 0f],
            Normals = [],
            TexCoords = [],
            Indices = [],
            Transform = System.Numerics.Matrix4x4.Identity
        };
        SwlorRenderMeshMetadataStore.SetArmorPart(source, AppearanceArmor.Robe);
        var sourceModel = new RenderModel { Name = "source", Meshes = [source] };

        var composed = TileGroupPreview.Compose([sourceModel], columns: 1, rows: 1, copyMeshMetadata: SwlorRenderMeshMetadataStore.Copy);

        composed.Should().NotBeNull();
        var composedMesh = composed!.Meshes.Single();
        composedMesh.Should().NotBeSameAs(source);
        SwlorRenderMeshMetadataStore.GetArmorPart(composedMesh).Should().Be(AppearanceArmor.Robe);
    }
}
