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

            var firstPose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["forearm_g"] = new(new Vector3(1f, 1f, 0f), Quaternion.Identity, 1f)
            };
            var finalPose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["forearm_g"] = new(new Vector3(1f, 3f, 0f), Quaternion.Identity, 1f)
            };

            var rendered = MdlMeshBuilder.Build(
                new MdlModel { Name = "coat", GeometryRoot = root },
                [firstPose, finalPose]);

            var mesh = rendered.Meshes.Should().ContainSingle().Subject;
            mesh.Transform.Should().Be(Matrix4x4.Identity);
            mesh.PoseFrames.Should().Equal(Matrix4x4.Identity, Matrix4x4.Identity);
            mesh.PosePositions.Should().HaveCount(2);
            mesh.PosePositions[0].Take(3).Should().Equal(2f, 1f, 0f);
            mesh.PosePositions[1].Take(3).Should().Equal(2f, 3f, 0f);
            mesh.Positions.Take(3).Should().Equal(2f, 3f, 0f);
            mesh.Positions.Should().Equal(mesh.PosePositions[^1],
                "the static buffer remains the settled final frame");
        }

        [Test]
        public void NamedCreatureAnimationsRetainSkinnedVertexFrames()
        {
            var root = new MdlNode { Name = "root" };
            var bone = new MdlNode
            {
                Name = "forearm_g",
                Parent = root,
                Position = new Vector3(1f, 0f, 0f)
            };
            root.Children.Add(bone);
            root.Children.Add(TriangleSkin(root));

            var firstPose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["forearm_g"] = new(new Vector3(1f, 1f, 0f), Quaternion.Identity, 1f)
            };
            var finalPose = new Dictionary<string, PosedNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["forearm_g"] = new(new Vector3(1f, 3f, 0f), Quaternion.Identity, 1f)
            };
            var animation = new MdlAnimationPose.SampledAnimation(
                "walk",
                1f,
                [firstPose, finalPose]);

            var rendered = MdlMeshBuilder.BuildAnimatedPreview(
                new MdlModel { Name = "coat", GeometryRoot = root },
                [firstPose, finalPose],
                [animation]);

            rendered.DefaultAnimationName.Should().Be("walk");
            rendered.Animations.Should().ContainSingle(item => item.Name == "walk" && item.IsPlayable);
            var mesh = rendered.Meshes.Should().ContainSingle().Which;
            mesh.PosePositions.Should().ContainSingle()
                .Which.Take(3).Should().Equal(2f, 3f, 0f);
            var positions = mesh.AnimationPositions["walk"];
            positions.Should().HaveCount(2);
            positions[0].Take(3).Should().Equal(2f, 1f, 0f);
            positions[1].Take(3).Should().Equal(2f, 3f, 0f);
        }

        [Test]
        public void RobeSkinKeepsItsAuthoredAnimationHierarchy()
        {
            var composite = new MdlNode { Name = "composite" };
            var mannequinShoulder = new MdlNode
            {
                Name = "shoulder_g",
                Parent = composite,
                Position = new Vector3(10f, 0f, 0f)
            };
            composite.Children.Add(mannequinShoulder);
            var mannequinBone = new MdlNode
            {
                Name = "forearm_g",
                Parent = mannequinShoulder,
                Position = new Vector3(1f, 0f, 0f)
            };
            mannequinShoulder.Children.Add(mannequinBone);

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
            firstVertex.Should().Equal([2f, 2f, 0f],
                "an outer mannequin bone with the same name must not replace the robe's authored " +
                "bone hierarchy");
        }

        [Test]
        public void NormalLessGarmentGeneratesNormalsBeforeApplyingSurfaceClearance()
        {
            var root = new MdlNode { Name = "root" };
            var bone = new MdlNode
            {
                Name = "forearm_g",
                Parent = root,
                Position = new Vector3(1f, 0f, 0f)
            };
            root.Children.Add(bone);
            var skin = TriangleSkin(root, withNormals: false);
            root.Children.Add(skin);

            var rendered = MdlMeshBuilder.Build(
                new MdlModel { Name = "coat", GeometryRoot = root },
                skinSurfaceClearance: 0.004f);

            var mesh = rendered.Meshes.Should().ContainSingle().Subject;
            mesh.Normals.Should().Equal(
                1f, 0f, 0f,
                1f, 0f, 0f,
                1f, 0f, 0f);
            mesh.Positions.Should().Equal(
                2.004f, 0f, 0f,
                2.004f, 1f, 0f,
                2.004f, 0f, 1f);
        }

        [Test]
        public void GarmentClearanceIgnoresTheWeightOfConcealedBodyParts()
        {
            var root = new MdlNode { Name = "root" };
            var forearm = new MdlNode { Name = "forearm_g", Parent = root };
            var torso = new MdlNode { Name = "torso_g", Parent = root };
            root.Children.Add(forearm);
            root.Children.Add(torso);
            var skin = TriangleSkin(root, withNormals: false);
            skin.VertexInfluences =
            [
                [new MdlSkinInfluence("forearm_g", 0.5f), new MdlSkinInfluence("torso_g", 0.5f)],
                [new MdlSkinInfluence("forearm_g", 0.5f), new MdlSkinInfluence("torso_g", 0.5f)],
                [new MdlSkinInfluence("forearm_g", 0.5f), new MdlSkinInfluence("torso_g", 0.5f)]
            ];
            root.Children.Add(skin);

            var rendered = MdlMeshBuilder.Build(
                new MdlModel { Name = "coat", GeometryRoot = root },
                skinSurfaceClearance: 0.1f,
                skinSurfaceClearanceExcludedBones:
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "forearm_g" });

            rendered.Meshes.Should().ContainSingle().Which.Positions.Should().Equal(
                2.05f, 0f, 0f,
                2.05f, 1f, 0f,
                2.05f, 0f, 1f);
        }

        [Test]
        public void NormalLessProgrammaticGarmentPreservesAuthoredFaceWinding()
        {
            var root = new MdlNode { Name = "root" };
            var bone = new MdlNode { Name = "torso_g", Parent = root };
            root.Children.Add(bone);
            var skin = new MdlSkinmeshNode
            {
                Name = "mixed-winding-robe",
                Parent = root,
                Render = true,
                Bitmap = "robe",
                Vertices =
                [
                    new Vector3(2f, -1f, 0f),
                    new Vector3(2f, -1f, 1f),
                    new Vector3(2f, 1f, 0f),
                    new Vector3(-2f, -1f, 0f),
                    new Vector3(-2f, -1f, 1f),
                    new Vector3(-2f, 1f, 0f)
                ],
                Normals = Array.Empty<Vector3>(),
                TextureCoordinates =
                [
                    Vector2.Zero, Vector2.UnitX, Vector2.UnitY,
                    Vector2.Zero, Vector2.UnitX, Vector2.UnitY
                ],
                VertexInfluences =
                [
                    [new MdlSkinInfluence("torso_g", 1f)],
                    [new MdlSkinInfluence("torso_g", 1f)],
                    [new MdlSkinInfluence("torso_g", 1f)],
                    [new MdlSkinInfluence("torso_g", 1f)],
                    [new MdlSkinInfluence("torso_g", 1f)],
                    [new MdlSkinInfluence("torso_g", 1f)]
                ],
                Faces =
                [
                    new MdlFace { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 2 },
                    new MdlFace { VertexIndex0 = 3, VertexIndex1 = 4, VertexIndex2 = 5 }
                ]
            };
            root.Children.Add(skin);

            var rendered = MdlMeshBuilder.Build(
                new MdlModel { Name = "coat", GeometryRoot = root },
                skinSurfaceClearance: 0.1f);

            var mesh = rendered.Meshes.Should().ContainSingle().Subject;
            mesh.Normals.Should().Equal(
                -1f, 0f, 0f,
                -1f, 0f, 0f,
                -1f, 0f, 0f,
                -1f, 0f, 0f,
                -1f, 0f, 0f,
                -1f, 0f, 0f);
            mesh.Positions.Should().Equal(
                1.9f, -1f, 0f,
                1.9f, -1f, 1f,
                1.9f, 1f, 0f,
                -2.1f, -1f, 0f,
                -2.1f, -1f, 1f,
                -2.1f, 1f, 0f);
            mesh.Indices.Should().Equal(
                0, 1, 2,
                3, 4, 5);
        }

        private static MdlSkinmeshNode TriangleSkin(MdlNode parent, bool withNormals = true) =>
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
                Normals = withNormals
                    ? [Vector3.UnitX, Vector3.UnitX, Vector3.UnitX]
                    : Array.Empty<Vector3>(),
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
