using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public sealed class ComposedPartTexturesTests
{
    [Test]
    public void NullBitmapKeepsTheStampedBodyPartTexture()
    {
        var source = ModelWithMesh("Hand", "NULL");
        var composed = ModelWithMesh("Hand", "pmh0_handl001");
        var textures = new ComposedPartTextures();

        textures.Record("pmh0_handl001", source);
        textures.Restore(composed, _ => true);

        composed.GetMeshNodes().Single().Bitmap.Should().Be("pmh0_handl001",
            "NULL means the standard body-part texture convention, not a literal texture");
    }

    [Test]
    public void RealAuthoredTextureStillReplacesTheStampedPartTexture()
    {
        var source = ModelWithMesh("arm", "n_repsold01");
        var composed = ModelWithMesh("arm", "pmh0_bicepl249");
        var textures = new ComposedPartTextures();

        textures.Record("pmh0_bicepl249", source);
        textures.Restore(composed, name => name.Equals("n_repsold01", StringComparison.OrdinalIgnoreCase));

        composed.GetMeshNodes().Single().Bitmap.Should().Be("n_repsold01");
    }

    private static MdlModel ModelWithMesh(string meshName, string bitmap)
    {
        var root = new MdlNode { Name = "root" };
        root.Children.Add(new MdlTrimeshNode
        {
            Name = meshName,
            Bitmap = bitmap,
            Parent = root
        });
        return new MdlModel { Name = "part", GeometryRoot = root };
    }
}
