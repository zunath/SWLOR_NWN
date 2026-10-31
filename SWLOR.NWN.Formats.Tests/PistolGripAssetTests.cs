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
            foreach (var mesh in meshes)
            {
                Assert.That(mesh.Vertices.All(Finite), Is.True, $"Part #{part}/{mesh.Name} positions");
                Assert.That(mesh.Normals.All(Finite), Is.True, $"Part #{part}/{mesh.Name} normals");
                Assert.That(mesh.TextureCoordinates.All(uv => float.IsFinite(uv.X) && float.IsFinite(uv.Y)),
                    Is.True, $"Part #{part}/{mesh.Name} UVs");
                foreach (var face in mesh.Faces)
                    Assert.That(new[] { face.VertexIndex0, face.VertexIndex1, face.VertexIndex2 },
                        Is.All.InRange(0, mesh.Vertices.Length - 1), $"Part #{part}/{mesh.Name} face");
            }
        }
    }

    [Test]
    public void ReportedPistolGripSeatsInTheHumanPalm()
    {
        // Measured in the native rhand socket frame, not the wrist's rhand_g frame.
        // Both normal Human sexes' bare and fingerless-glove palms lie around
        // Z=-0.02. Positive Z moves a hanging pistol up the arm toward the wrist.
        // Sample the reported Part #301's handle at several heights. This is a
        // regression screen for that model's measured grip, not a universal grip
        // detector for every possible pistol silhouette. Native client
        // views of both hands, body families and firing poses still qualify a fit.
        foreach (var part in new[] { 301 })
        {
            var (_, model) = Read(part);
            var centers = new List<float>();
            foreach (var y in new[] { -.02f, -.03f, -.04f })
            {
                var intersections = new List<float>();
                foreach (var mesh in model.GetMeshNodes().Where(mesh => mesh.Render))
                {
                    var transform = World(mesh);
                    foreach (var face in mesh.Faces)
                    {
                        var a = Vector3.Transform(mesh.Vertices[face.VertexIndex0], transform);
                        var b = Vector3.Transform(mesh.Vertices[face.VertexIndex1], transform);
                        var c = Vector3.Transform(mesh.Vertices[face.VertexIndex2], transform);
                        if (Intersect(a, b, c, y) is { } z)
                            intersections.Add(z);
                    }
                }
                var crossings = intersections.Select(z => MathF.Round(z, 5)).Distinct().Order().ToArray();
                if (crossings.Length >= 2)
                    centers.Add((crossings[0] + crossings[^1]) / 2);
            }
            Assert.That(centers, Is.Not.Empty, $"Part #{part}: no handle intersects the palm sampling planes.");
            centers.Sort();
            var center = centers.Count % 2 == 0
                ? (centers[centers.Count / 2 - 1] + centers[centers.Count / 2]) / 2
                : centers[centers.Count / 2];
            Assert.That(center, Is.InRange(-.035f, 0f),
                $"Part #{part}: grip center Z={center:0.0000} misses the palm; positive Z raises it toward the wrist.");
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

    private static float? Intersect(Vector3 a, Vector3 b, Vector3 c, float y)
    {
        var u = b - a;
        var v = c - a;
        var determinant = u.X * v.Y - u.Y * v.X;
        if (MathF.Abs(determinant) < 1e-12f) return null;
        var first = (-a.X * v.Y - (y - a.Y) * v.X) / determinant;
        var second = (u.X * (y - a.Y) + u.Y * a.X) / determinant;
        return first >= 0 && second >= 0 && first + second <= 1
            ? a.Z + first * u.Z + second * v.Z
            : null;
    }
}
