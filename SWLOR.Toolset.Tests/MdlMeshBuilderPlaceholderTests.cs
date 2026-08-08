using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// <see cref="MdlMeshBuilder"/> drops BioWare's placeholder nodes.
    /// </summary>
    /// <remarks>
    /// Every base-game door model carries a render=1 node called <c>sam</c> with no texture and real
    /// geometry, so it drew as a large blank panel over the door in every preview. It is a placeholder,
    /// not artwork, and nothing else in the builder filters it.
    /// </remarks>
    [TestFixture]
    public class MdlMeshBuilderPlaceholderTests
    {
        private static MdlTrimeshNode Trimesh(string name, string? bitmap = "some_texture")
        {
            var node = new MdlTrimeshNode
            {
                Name = name,
                Render = true,
                Bitmap = bitmap!,
                Vertices = new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0)
                },
                Faces = new[] { new MdlFace { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 2 } }
            };

            return node;
        }

        private static MdlModel ModelOf(params MdlNode[] nodes)
        {
            var root = new MdlNode { Name = "root" };
            foreach (var node in nodes)
                root.Children.Add(node);

            return new MdlModel { Name = "probe", GeometryRoot = root };
        }

        [TestCase("sam")]
        [TestCase("SAM")]
        [TestCase("rootdummy")]
        public void APlaceholderNode_IsNotBuilt(string name)
        {
            var built = MdlMeshBuilder.Build(ModelOf(Trimesh(name, bitmap: null)));

            built.Meshes.Should().BeEmpty(because: $"'{name}' is a placeholder, not artwork");
        }

        [Test]
        public void RealGeometry_IsStillBuilt()
        {
            var built = MdlMeshBuilder.Build(ModelOf(Trimesh("door_panel"), Trimesh("sam", bitmap: null)));

            built.Meshes.Should().ContainSingle().Which.NodeName.Should().Be("door_panel");
        }

        /// <summary>
        /// Untextured is not the same as placeholder. Measured over 4,000 models, the untextured meshes
        /// that are not placeholders include real artwork, so only the known names are dropped.
        /// </summary>
        [Test]
        public void AnUntexturedMeshWithARealName_IsKept()
        {
            var built = MdlMeshBuilder.Build(ModelOf(Trimesh("Gargoyle_Lwingtip", bitmap: null)));

            built.Meshes.Should().ContainSingle().Which.NodeName.Should().Be("Gargoyle_Lwingtip");
        }

        /// <summary>
        /// A collision node is the walkable surface, not artwork, and must never be drawn.
        /// </summary>
        /// <remarks>
        /// It cannot be filtered by <see cref="MdlTrimeshNode.Render"/>: ASCII MDL writes no
        /// <c>render</c> line for one, so it arrives at the default of true, and it carries no bitmap.
        /// dath_desert drew 19 of them as flat grey slabs lying over the sand - ztd01 has 440
        /// <c>node aabb</c> declarations in total.
        /// </remarks>
        [Test]
        public void AWalkmeshNode_IsNotBuilt()
        {
            var walkmesh = Trimesh("walkmesh", bitmap: null);
            walkmesh.IsWalkmesh = true;

            var built = MdlMeshBuilder.Build(ModelOf(walkmesh, Trimesh("ground")));

            built.Meshes.Should().ContainSingle(because: "only the artwork is drawn")
                .Which.NodeName.Should().Be("ground");
        }

        [Test]
        public void DoorTransitionBuild_KeepsHiddenSelectionGeometryOnlyForTheEditor()
        {
            var hiddenPlane = Trimesh("transition_plane", bitmap: null);
            hiddenPlane.Render = false;

            var ordinary = MdlMeshBuilder.Build(ModelOf(hiddenPlane));
            var transition = MdlMeshBuilder.BuildDoorTransition(ModelOf(hiddenPlane));

            ordinary.Meshes.Should().BeEmpty("render 0 geometry stays invisible in game artwork");
            transition.Meshes.Should().ContainSingle()
                .Which.NodeName.Should().Be("transition_plane");
            transition.IsDoorTransitionGeometry.Should().BeTrue();
        }

        [Test]
        public void DoorTransitionBuild_StillExcludesCollisionAndPlaceholderMeshes()
        {
            var hiddenPlane = Trimesh("transition_plane", bitmap: null);
            hiddenPlane.Render = false;
            var collision = Trimesh("collision", bitmap: null);
            collision.IsWalkmesh = true;
            var placeholder = Trimesh("sam", bitmap: null);

            var transition = MdlMeshBuilder.BuildDoorTransition(
                ModelOf(hiddenPlane, collision, placeholder));

            transition.Meshes.Should().ContainSingle()
                .Which.NodeName.Should().Be("transition_plane");
        }

        [Test]
        public void AMeshWhoseFacesAllReferenceMissingVertices_IsNotBuilt()
        {
            var malformed = Trimesh("malformed");
            malformed.Faces =
            [
                new MdlFace { VertexIndex0 = 0, VertexIndex1 = 1, VertexIndex2 = 99 }
            ];

            MdlMeshBuilder.IsRenderableMesh(malformed).Should().BeFalse();
            MdlMeshBuilder.Build(ModelOf(malformed)).Meshes.Should().BeEmpty();
        }

        [Test]
        public void AnUnsignedMinusOneFaceIndex_NeverReachesARenderBuffer()
        {
            var malformed = Trimesh("malformed");
            malformed.Faces =
            [
                new MdlFace
                {
                    // MDL face indices are ushort. A raw -1 sentinel is represented as 0xFFFF,
                    // so the existing upper-bound validation is also its non-negative guard.
                    VertexIndex0 = unchecked((ushort)-1),
                    VertexIndex1 = 1,
                    VertexIndex2 = 2
                }
            ];

            MdlMeshBuilder.IsRenderableMesh(malformed).Should().BeFalse();
            MdlMeshBuilder.Build(ModelOf(malformed)).Meshes.Should().BeEmpty();

            malformed.Render = false;
            MdlMeshBuilder.BuildDoorTransition(ModelOf(malformed)).Meshes.Should().BeEmpty();
        }

        /// <summary>
        /// The Aurora "no texture" literal must resolve the same way whether it came from an ASCII
        /// MDL (already lowercased/blanked by <see cref="AsciiMdlReader"/>) or a binary MDL (which
        /// stores the raw fixed string as-is), and in any casing. Otherwise a binary mesh authored
        /// with <c>bitmap NULL</c> attempts a texture lookup literally named "NULL" instead of
        /// taking the deliberate no-texture path.
        /// </summary>
        [TestCase("NULL")]
        [TestCase("null")]
        [TestCase("Null")]
        public void ANullLiteralBitmap_ProducesAnEmptyTextureName(string bitmap)
        {
            var built = MdlMeshBuilder.Build(ModelOf(Trimesh("door_panel", bitmap: bitmap)));

            built.Meshes.Should().ContainSingle().Which.TextureName.Should().BeEmpty();
        }
    }
}
