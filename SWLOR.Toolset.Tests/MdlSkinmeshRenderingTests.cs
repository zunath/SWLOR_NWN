using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Skinned clothing follows a posed skeleton instead of remaining in its bind pose.</summary>
    public sealed class MdlSkinmeshRenderingTests
    {
        [Test]
        public void NamedBoneWeightsDeformVerticesIntoTheRestingPose()
        {
            var root = new MdlNode { Name = "root" };
            var bone = new MdlNode
            {
                Name = "forearm_g",
                Parent = root,
                Position = new Vector3(1f, 0f, 0f)
            };
            root.Children.Add(bone);

            // The skin node's authored offset first puts the local vertex at X=2 in model space.
            // Relative to the bone at X=1 it is one metre away. Moving that bone up by three metres
            // must therefore put the rendered vertex at (2, 3, 0).
            var skin = new MdlSkinmeshNode
            {
                Name = "sleeve",
                Parent = root,
                Position = new Vector3(-10f, 0f, 0f),
                Render = true,
                Bitmap = "cloth",
                Vertices =
                [
                    new Vector3(12f, 0f, 0f),
                    new Vector3(12f, 1f, 0f),
                    new Vector3(12f, 0f, 1f)
                ],
                Normals = [Vector3.UnitX, Vector3.UnitX, Vector3.UnitX],
                TextureCoordinates = [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
                VertexInfluences =
                [
                    [new MdlSkinInfluence("forearm_g", 1f)],
                    [new MdlSkinInfluence("forearm_g", 1f)],
                    [new MdlSkinInfluence("forearm_g", 1f)]
                ],
                Faces =
                [
                    new MdlFace
                    {
                        VertexIndex0 = 0,
                        VertexIndex1 = 1,
                        VertexIndex2 = 2
                    }
                ]
            };
            root.Children.Add(skin);

            var pose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["forearm_g"] = new(new Vector3(1f, 3f, 0f), Quaternion.Identity, 1f)
            };

            var rendered = MdlMeshBuilder.Build(
                new MdlModel { Name = "coat", GeometryRoot = root },
                [pose]);

            var mesh = rendered.Meshes.Should().ContainSingle().Subject;
            mesh.Transform.Should().Be(Matrix4x4.Identity);
            mesh.PoseFrames.Should().Equal(Matrix4x4.Identity);
            mesh.Positions.Take(3).Should().Equal(2f, 3f, 0f);
        }

        [Test]
        public void NearestBindSkeletonWinsWhenAComposedModelRepeatsBoneNames()
        {
            var composite = new MdlNode { Name = "composite" };
            var mannequinBone = new MdlNode
            {
                Name = "forearm_g",
                Parent = composite,
                Position = new Vector3(100f, 0f, 0f)
            };
            composite.Children.Add(mannequinBone);

            var robeRoot = new MdlNode { Name = "robe", Parent = composite };
            composite.Children.Add(robeRoot);
            var robeBone = new MdlNode
            {
                Name = "forearm_g",
                Parent = robeRoot,
                Position = new Vector3(1f, 0f, 0f)
            };
            robeRoot.Children.Add(robeBone);
            var skin = TriangleSkin(robeRoot);
            robeRoot.Children.Add(skin);

            var pose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["forearm_g"] = new(new Vector3(1f, 2f, 0f), Quaternion.Identity, 1f)
            };

            var rendered = MdlMeshBuilder.Build(
                new MdlModel { Name = "body", GeometryRoot = composite },
                [pose]);

            var firstVertex = rendered.Meshes.Should().ContainSingle()
                .Which.Positions.Take(3).ToArray();
            firstVertex.Should().Equal(2f, 2f, 0f);
            firstVertex[0].Should().BeLessThan(10f,
                "the robe's bind skeleton must be used, not the identically named mannequin sibling at X=100");
        }

        private static MdlSkinmeshNode TriangleSkin(MdlNode parent) =>
            new()
            {
                Name = "robe-skin",
                Parent = parent,
                Render = true,
                Bitmap = "robe",
                Vertices =
                [
                    new Vector3(2f, 0f, 0f),
                    new Vector3(2f, 1f, 0f),
                    new Vector3(2f, 0f, 1f)
                ],
                Normals = [Vector3.UnitX, Vector3.UnitX, Vector3.UnitX],
                TextureCoordinates = [Vector2.Zero, Vector2.UnitX, Vector2.UnitY],
                VertexInfluences =
                [
                    [new MdlSkinInfluence("forearm_g", 1f)],
                    [new MdlSkinInfluence("forearm_g", 1f)],
                    [new MdlSkinInfluence("forearm_g", 1f)]
                ],
                Faces =
                [
                    new MdlFace
                    {
                        VertexIndex0 = 0,
                        VertexIndex1 = 1,
                        VertexIndex2 = 2
                    }
                ]
            };
    }
}
