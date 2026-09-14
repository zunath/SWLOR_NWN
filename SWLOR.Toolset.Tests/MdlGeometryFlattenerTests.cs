using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="MdlGeometryFlattener"/> — the WP4.3 fix for segmented-creature
    /// preview misalignment. Several SWLOR hak body-part MDLs (e.g. sw_pt_lthigh's
    /// pfh0_legl001) author their vertices offset from the part origin and correct them with a
    /// mesh-node Position inside the part file. Flattening bakes those node transforms into the
    /// vertex data before composition so attached parts remain aligned with their skeleton bones.
    /// </summary>
    public class MdlGeometryFlattenerTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                        return current.FullName;
                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        [Test]
        public void Flatten_SwlorLeftThighPart_BringsVerticesToPartOrigin()
        {
            // pfh0_legl001.mdl: raw mesh vertices span Z ≈ [-0.01..0.53] (pointing UP) and are
            // corrected by the mesh node's Position <0.026, 0.013, -0.459>. After flattening,
            // the geometry must hang DOWN from the origin like its right-leg counterpart
            // (Z ≈ [-0.47..0.07]) and the node transform must be identity.
            var path = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_pt_lthigh", "pfh0_legl001.mdl");
            var model = new MdlReader().Parse(File.ReadAllBytes(path));

            MdlGeometryFlattener.FlattenNodeTransforms(model);

            var mesh = model.GetMeshNodes().First(m => m.Name == "pfh0_legl001g");
            var minZ = mesh.Vertices.Min(v => v.Z);
            var maxZ = mesh.Vertices.Max(v => v.Z);

            minZ.Should().BeLessThan(-0.4f, "the thigh must hang down from the hip origin after flattening");
            maxZ.Should().BeLessThan(0.15f, "no thigh geometry should extend far above the hip origin");
            mesh.Position.Should().Be(Vector3.Zero);
            mesh.Orientation.Should().Be(Quaternion.Identity);
            mesh.Scale.Should().Be(1f);
        }

        [Test]
        public void Flatten_SwlorLeftShinPart_BringsVerticesToPartOrigin()
        {
            // pfh0_shinl001.mdl: 'Shin' mesh vertices sit at X ≈ +0.47 / Z ≈ +0.38, corrected by
            // node Position <-0.458, -1.033, -0.562>.
            var path = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_pt_lshin", "pfh0_shinl001.mdl");
            var model = new MdlReader().Parse(File.ReadAllBytes(path));

            MdlGeometryFlattener.FlattenNodeTransforms(model);

            var mesh = model.GetMeshNodes().First(m => m.Name == "Shin");
            mesh.Vertices.Min(v => v.Z).Should().BeLessThan(-0.35f);
            mesh.Vertices.Max(v => v.X).Should().BeLessThan(0.15f, "the shin must no longer sit half a meter to the side");
        }

        [Test]
        public void Flatten_BaseStylePartAuthoredAtOrigin_IsUnchanged()
        {
            // pfh0_legr001.mdl's mesh node transform is identity — flattening must not move it.
            var path = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_pt_rthigh", "pfh0_legr001.mdl");
            var model = new MdlReader().Parse(File.ReadAllBytes(path));
            var before = model.GetMeshNodes().First(m => m.Name == "pfh0_legr001g").Vertices.ToArray();

            MdlGeometryFlattener.FlattenNodeTransforms(model);

            var after = model.GetMeshNodes().First(m => m.Name == "pfh0_legr001g").Vertices;
            after.Should().Equal(before);
        }

        [Test]
        public void Flatten_NestedDummyAndRotatedMesh_ComposesFullChainIntoVertices()
        {
            // Synthetic: root → dummy(+1Z) → mesh(+1X, rotated 90° about Z, one vertex at local
            // (1,0,0) with normal (1,0,0)). The composed scale × rotation × translation transform,
            // accumulated child-to-root, must land
            // the vertex at (1,1,1) and rotate the normal to (0,1,0).
            var mesh = new MdlTrimeshNode
            {
                Name = "m",
                Position = new Vector3(1, 0, 0),
                Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2),
                Scale = 1f,
                Vertices = new[] { new Vector3(1, 0, 0) },
                Normals = new[] { new Vector3(1, 0, 0) },
            };
            var dummy = new MdlNode { Name = "d", Position = new Vector3(0, 0, 1), Orientation = Quaternion.Identity, Scale = 1f };
            var root = new MdlNode { Name = "root", Position = Vector3.Zero, Orientation = Quaternion.Identity, Scale = 1f };
            root.Children.Add(dummy); dummy.Parent = root;
            dummy.Children.Add(mesh); mesh.Parent = dummy;
            var model = new MdlModel { Name = "synthetic", GeometryRoot = root };

            MdlGeometryFlattener.FlattenNodeTransforms(model);

            mesh.Vertices[0].X.Should().BeApproximately(1f, 1e-5f);
            mesh.Vertices[0].Y.Should().BeApproximately(1f, 1e-5f);
            mesh.Vertices[0].Z.Should().BeApproximately(1f, 1e-5f);
            mesh.Normals[0].X.Should().BeApproximately(0f, 1e-5f);
            mesh.Normals[0].Y.Should().BeApproximately(1f, 1e-5f);
            mesh.Normals[0].Z.Should().BeApproximately(0f, 1e-5f);
            dummy.Position.Should().Be(Vector3.Zero, "dummy transforms are baked and zeroed too");
        }
    }
}
