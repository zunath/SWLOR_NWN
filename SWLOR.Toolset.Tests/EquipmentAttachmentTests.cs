using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class EquipmentAttachmentTests
{
    [TestCase("shield", "lforearm", "lforearm_g")]
    [TestCase("weaponl", "lhand", "lhand_g")]
    [TestCase("weaponr", "rhand", "rhand_g")]
    public void EquipmentFollowsTheNativeSocketIncludingItsOffset(string part, string socket, string body)
    {
        var root = new MdlNode { Name = "root" };
        var pivot = new MdlNode { Name = body, Parent = root, Position = new(1, 2, 3) };
        root.Children.Add(pivot);
        var hook = new MdlNode { Name = socket, Parent = pivot, Position = new(.1f, .2f, .3f),
            Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2) };
        pivot.Children.Add(hook);
        var model = new MdlModel { Name = "rig", GeometryRoot = root };
        var equipment = new MdlModel { Name = "item", GeometryRoot = new MdlTrimeshNode
        {
            Name = "item", Render = true, Bitmap = "surface",
            Vertices = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            Faces = [new MdlFace { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 2 }]
        }};
        var composer = new MdlPartComposer((name, _) => name == "rig" ? model : equipment);
        var composed = composer.Compose("rig", [(part, "item")], adjustSeams: false)!;
        var mesh = MdlMeshBuilder.Build(composed).Meshes.Single();
        Vector3.Distance(Vector3.Transform(Vector3.UnitX, mesh.Transform), new(1.1f, 3.2f, 3.3f))
            .Should().BeLessThan(.00001f, "the socket has a nonzero offset and rotation relative to the body part");

        // Missing native sockets must not silently substitute the mesh pivot again.
        pivot.Children.Clear();
        composer.Clear();
        composer.Compose("rig", [(part, "item")], adjustSeams: false).Should().BeNull();
    }
}
