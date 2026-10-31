using System.Numerics;
using System.Text.RegularExpressions;
using Nwn.Formats.NativeModels;
using NUnit.Framework;

namespace SWLOR.NWN.Formats.Tests;

public sealed class PistolGripAssetTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static IEnumerable<int> CatalogParts()
    {
        var path = Path.Combine(Root, "SWLOR.Game.Server/Feature/AppearanceDefinition/ItemAppearance/PistolAppearanceDefinition.cs");
        var text = Regex.Replace(File.ReadAllText(path), @"//[^\n]*", "");
        var middle = Regex.Match(text, @"MiddleParts.*?\{(.*?)\};", RegexOptions.Singleline);
        Assert.That(middle.Success, Is.True, "The selectable pistol catalog must be readable.");
        return Regex.Matches(middle.Groups[1].Value, @"\b\d+\b").Select(match => int.Parse(match.Value));
    }

    [Test]
    public void EverySelectablePistolHasCompiledFiniteGeometry()
    {
        foreach (var part in CatalogParts())
        {
            var (data, model) = Read(part);
            Assert.That(BitConverter.ToUInt32(data, 0), Is.Zero, $"Part #{part} must ship compiled.");
            var meshes = model.GetMeshNodes().Where(mesh => mesh.Render).ToArray();
            Assert.That(meshes, Is.Not.Empty, $"Part #{part} must contain visible geometry.");
            Assert.That(meshes.Any(mesh => mesh.Vertices.Length > 0 && mesh.Faces.Length > 0), Is.True,
                $"Part #{part} must contain rendered vertices and faces.");
            var worldVertices = new List<Vector3>();
            foreach (var mesh in meshes)
            {
                Assert.That(mesh.Vertices.All(Finite), Is.True, $"Part #{part}/{mesh.Name} positions");
                var transform = World(mesh);
                var transformed = mesh.Vertices.Select(vertex => Vector3.Transform(vertex, transform)).ToArray();
                Assert.That(transformed.All(Finite), Is.True, $"Part #{part}/{mesh.Name} transformed positions");
                worldVertices.AddRange(transformed);
                Assert.That(mesh.Normals.All(Finite), Is.True, $"Part #{part}/{mesh.Name} normals");
                Assert.That(mesh.TextureCoordinates.All(uv => float.IsFinite(uv.X) && float.IsFinite(uv.Y)),
                    Is.True, $"Part #{part}/{mesh.Name} UVs");
                foreach (var face in mesh.Faces)
                    Assert.That(new[] { face.VertexIndex0, face.VertexIndex1, face.VertexIndex2 },
                        Is.All.InRange(0, mesh.Vertices.Length - 1), $"Part #{part}/{mesh.Name} face");
            }
            Assert.That(worldVertices.Any(vertex => vertex != worldVertices[0]), Is.True,
                $"Part #{part} transforms must preserve visible geometry.");
        }
    }

    private static (byte[] Data, MdlModel Model) Read(int part)
    {
        var slot = part % 100 * 10 + part / 100;
        var path = Path.Combine(Root, $"SWLOR_Haks/sw_weapon/wbwsh_m_{slot:000}.mdl");
        if (!Directory.Exists(Path.GetDirectoryName(path)))
            Assert.Ignore("Initialize the SWLOR_Haks sw_weapon folder to audit pistol assets.");
        var data = File.ReadAllBytes(path);
        return (data, new MdlReader().Parse(data));
    }

    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private static Matrix4x4 World(MdlNode node)
    {
        var local = Matrix4x4.CreateScale(node.Scale) * Matrix4x4.CreateFromQuaternion(node.Orientation) *
                    Matrix4x4.CreateTranslation(node.Position);
        return node.Parent == null ? local : local * World(node.Parent);
    }
}
