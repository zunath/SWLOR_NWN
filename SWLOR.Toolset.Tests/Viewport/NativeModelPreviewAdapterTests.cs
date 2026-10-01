using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Viewport;

namespace SWLOR.Toolset.Tests.Viewport;

[TestFixture]
public sealed class NativeModelPreviewAdapterTests
{
    [Test]
    public void Load_UsesSharedRigidReaderForNativeChestFixture()
    {
        var haksRoot = Environment.GetEnvironmentVariable("SWLOR_HAKS_ROOT");
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
