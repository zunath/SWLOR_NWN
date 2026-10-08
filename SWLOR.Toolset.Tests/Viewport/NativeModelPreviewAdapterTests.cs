using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Tests.Support;
using Nwn.Preview.Dds;

namespace SWLOR.Toolset.Tests.Viewport;

[TestFixture]
public sealed class NativeModelPreviewAdapterTests
{
    [Test]
    public void Load_CanonicalizesNwnDdsRowsAndInvalidatesChangedTextureBytes()
    {
        using var fixture = new NativePreviewWorkspaceFixture();
        var adapter = new NativeModelPreviewAdapter(fixture.Resources);
        var first = adapter.Load("preview_fixture");
        Assert.That(first.Textures["fixture_map"].GetPixel(0, 0), Is.EqualTo(new RgbaPixel(0, 0, 255, 255)));
        Assert.That(first.Textures["fixture_map"].GetPixel(0, 7), Is.EqualTo(new RgbaPixel(255, 0, 0, 255)));
        Assert.That(first.UnsupportedMaterials, Does.Contain("fixture_material"));
        NativePreviewWorkspaceFixture.WriteDds(Path.Combine(fixture.DirectoryPath, "fixture_map.dds"), reverseColors: true);
        var changed = adapter.Load("preview_fixture");
        Assert.That(changed.Textures["fixture_map"].GetPixel(0, 0), Is.EqualTo(new RgbaPixel(255, 0, 0, 255)));
        Assert.That(first.Textures["fixture_map"].GetPixel(0, 0), Is.EqualTo(new RgbaPixel(0, 0, 255, 255)));
        Assert.That(changed.Scene, Is.SameAs(first.Scene));
    }

    [Test]
    public void Load_ResolvesExplicitMaterialDiffuseBeforeBitmapAndReportsItsShaderLimit()
    {
        using var fixture = new NativePreviewWorkspaceFixture(directory =>
        {
            File.WriteAllText(Path.Combine(directory, "fixture_material.mtr"), "customshaderFS unsupported_shader\ntexture0 fixture_diffuse\n");
            NativePreviewWorkspaceFixture.WriteTga(Path.Combine(directory, "fixture_diffuse.tga"), 25, 70, 110);
        });
        var result = new NativeModelPreviewAdapter(fixture.Resources).Load("preview_fixture");
        Assert.That(result.Textures["fixture_material"].GetPixel(0, 0), Is.EqualTo(new RgbaPixel(25, 70, 110, 255)));
        Assert.That(result.Textures.ContainsKey("fixture_map"), Is.False);
        Assert.That(result.UnsupportedMaterials, Does.Contain("fixture_material"));
    }

    [Test]
    public void Load_PreservesSwlorTgaPreferenceAndNullMaterialSentinel()
    {
        using var fixture = new NativePreviewWorkspaceFixture(directory =>
        {
            var path = Path.Combine(directory, "preview_fixture.mdl");
            File.WriteAllText(path, File.ReadAllText(path).Replace("fixture_material", "NULL", StringComparison.Ordinal));
            NativePreviewWorkspaceFixture.WriteTga(Path.Combine(directory, "fixture_map.tga"), 15, 40, 65);
        });
        var result = new NativeModelPreviewAdapter(fixture.Resources).Load("preview_fixture");
        Assert.That(result.Textures["fixture_map"].GetPixel(0, 0), Is.EqualTo(new RgbaPixel(15, 40, 65, 255)));
        Assert.That(result.UnsupportedMaterials, Is.Empty);
        Assert.That(result.MissingTextures, Is.Empty);
    }

    [Test]
    public void Load_ComposesTheActualChestMaterialWithSwlorPalettePolicy()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_HAKS_ROOT") ?? SWLOR.Toolset.Tests.Support.ToolsetCorpusPaths.HaksRoot;
        Assert.That(root, Is.Not.Null.And.Not.Empty, "Select the read-only corpus with SWLOR_HAKS_ROOT.");
        var resources = new ResourceIndex(null, [new("chest", Path.Combine(root!, "sw_pt_chest")),
            new("materials", Path.Combine(root!, "sw_tint_mtr")), new("tint", Path.Combine(root!, "sw_tint0")),
            new("palettes", Path.Combine(root!, "sw_item"))]);
        resources.InitializationTask.GetAwaiter().GetResult();
        var result = new NativeModelPreviewAdapter(resources).Load("pfa0_chest001");
        var material = MaterialResolver.TryParseMaterial(resources, "pfh0_chest001");
        Assert.That(material, Is.Not.Null);
        var expected = TintMapTextureRenderer.Render(resources, "pfh0_chest001", material!, null, null);
        Assert.That(expected, Is.Not.Null);
        Assert.That(result.Textures["pfh0_chest001"].CopyRgbaBytes(), Is.EqualTo(expected!.Pixels));
        Assert.That(result.Textures["pfh0_chest001"].Width, Is.EqualTo(512));
        Assert.That(result.MissingTextures, Is.Empty);
        Assert.That(result.UnsupportedMaterials, Is.Empty);
    }

    [Test]
    public void Load_UsesSharedRigidReaderForNativeChestFixture()
    {
        var haksRoot = Environment.GetEnvironmentVariable("SWLOR_HAKS_ROOT") ?? SWLOR.Toolset.Tests.Support.ToolsetCorpusPaths.HaksRoot;
        if (string.IsNullOrWhiteSpace(haksRoot) || !Directory.Exists(Path.Combine(haksRoot, "sw_pt_chest")))
            Assert.Ignore("Set SWLOR_HAKS_ROOT to run the scoped native chest fixture check.");
        var resources = new ResourceIndex(null,
        [
            new ResourceIndex.HakLayer("sw_pt_chest", Path.Combine(haksRoot, "sw_pt_chest"))
        ]);
        resources.InitializationTask.GetAwaiter().GetResult();

        var result = new NativeModelPreviewAdapter(resources).Load("pfa0_chest001");

        Assert.That(result.Scene.ModelName, Is.EqualTo("pfa0_chest001"));
        Assert.That(result.Scene.Nodes.Any(node => node.Mesh is { Faces.Count: > 0 }), Is.True);
        Assert.That(result.Scene.Nodes.Select(node => node.Mesh?.BitmapName).Where(name => name is not null),
            Does.Contain("pfh0_chest001"));
        Assert.That(result.MissingTextures, Does.Contain("pfh0_chest001"));
    }
}
