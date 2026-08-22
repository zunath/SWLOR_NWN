// SPDX-License-Identifier: MIT

using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Covers the robe-attachment parity regression: robe geometry is authored in absolute body
    /// space, so every robe part must graft directly onto the composite root, never onto a
    /// skeleton bone such as torso_g, regardless of how much of the body it covers.
    /// </summary>
    [TestFixture]
    public sealed class MdlPartComposerRobeTests
    {
        [Test]
        public void PartialRobeAttachesAtTheCompositeRootNotTorso()
        {
            var skeletonRoot = new MdlNode { Name = "root" };
            var torso = new MdlNode
            {
                Name = "torso_g",
                Position = new Vector3(5f, 0f, 0f),
                Parent = skeletonRoot
            };
            skeletonRoot.Children.Add(torso);
            var skeleton = new MdlModel { Name = "pfh0", GeometryRoot = skeletonRoot };

            // Spans Z 0.6-1.2 only: fails the full-body thresholds (minZ<0.5 && maxZ>1.35), so this
            // is a partial robe under RobeCoverage.IsFullBodyRobe.
            var robeRoot = new MdlNode { Name = "robe-root" };
            var robeMesh = Part("partial-robe", 0.6f, 1.2f);
            robeMesh.Parent = robeRoot;
            robeRoot.Children.Add(robeMesh);
            var robe = new MdlModel { Name = "partial_robe", GeometryRoot = robeRoot };

            var composer = new MdlPartComposer((resRef, _) =>
                resRef == "skeleton" ? skeleton :
                resRef == "partial_robe" ? robe :
                null);

            var composed = composer.Compose(
                "skeleton",
                new[] { ("robe", "partial_robe") },
                adjustSeams: false);

            composed.Should().NotBeNull();
            var partRoot = composed!.GetMeshNodes().Single().Parent!;
            partRoot.Parent.Should().BeSameAs(
                composed.GeometryRoot,
                "robe geometry is authored in absolute body space and must graft at the composite " +
                "root even when it only covers part of the body");
            partRoot.Parent.Should().NotBe(torso);
        }

        [Test]
        public void FullBodyRobeAttachesAtTheCompositeRoot()
        {
            var skeletonRoot = new MdlNode { Name = "root" };
            var torso = new MdlNode
            {
                Name = "torso_g",
                Position = new Vector3(5f, 0f, 0f),
                Parent = skeletonRoot
            };
            skeletonRoot.Children.Add(torso);
            var skeleton = new MdlModel { Name = "pfh0", GeometryRoot = skeletonRoot };

            // Spans Z 0.2-1.6: satisfies the full-body thresholds (minZ<0.5 && maxZ>1.35).
            var robeRoot = new MdlNode { Name = "robe-root" };
            var robeMesh = Part("full-robe", 0.2f, 1.6f);
            robeMesh.Parent = robeRoot;
            robeRoot.Children.Add(robeMesh);
            var robe = new MdlModel { Name = "full_robe", GeometryRoot = robeRoot };

            var composer = new MdlPartComposer((resRef, _) =>
                resRef == "skeleton" ? skeleton :
                resRef == "full_robe" ? robe :
                null);

            var composed = composer.Compose(
                "skeleton",
                new[] { ("robe", "full_robe") },
                adjustSeams: false);

            composed.Should().NotBeNull();
            var partRoot = composed!.GetMeshNodes().Single().Parent!;
            partRoot.Parent.Should().BeSameAs(
                composed.GeometryRoot,
                "behavior for a full-body robe is unchanged by this fix");
            partRoot.Parent.Should().NotBe(torso);
        }

        private static MdlTrimeshNode Part(string name, float minimumZ, float maximumZ) =>
            new()
            {
                Name = name,
                Render = true,
                Bitmap = "surface",
                Vertices =
                [
                    new Vector3(0f, 0f, minimumZ),
                    new Vector3(1f, 0f, maximumZ),
                    new Vector3(0f, 1f, minimumZ)
                ],
                Normals =
                [
                    Vector3.UnitZ,
                    Vector3.UnitZ,
                    Vector3.UnitZ
                ],
                TextureCoordinates =
                [
                    Vector2.Zero,
                    Vector2.UnitX,
                    Vector2.UnitY
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
