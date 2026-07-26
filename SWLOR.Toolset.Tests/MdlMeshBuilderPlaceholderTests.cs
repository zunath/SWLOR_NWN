using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using Radoub.Formats.Mdl;
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
    }
}
