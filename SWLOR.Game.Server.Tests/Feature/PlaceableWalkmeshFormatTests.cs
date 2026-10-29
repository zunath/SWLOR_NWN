using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Reads placeable and door walkmeshes the way the server does. CNWPlaceableSurfaceMesh::LoadWalkMesh ends a line at
/// every CR or LF (CR LF counts once) and reads exactly the declared number of vertex and face lines. A stray line
/// break inside those blocks leaves vertex indices uninitialised, and the server crashes in
/// CNWArea::NoNonWalkPolysDetailed when a creature paths near the placeable.
/// </summary>
public class PlaceableWalkmeshFormatTests
{
    private static readonly Regex VertexHeader = new(@"^(verts|vertices)\s+(\d+)", RegexOptions.Compiled);
    private static readonly Regex FaceHeader = new(@"^faces\s+(\d+)", RegexOptions.Compiled);

    [Test]
    public void Walkmeshes_HaveEveryDeclaredVertexAndFaceOnItsOwnLine()
    {
        var haks = Path.Combine(FindRoot(), "SWLOR_Haks");
        var files = Directory.EnumerateFiles(haks, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".pwk", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".dwk", StringComparison.OrdinalIgnoreCase))
            .ToList();
        files.Should().NotBeEmpty("the haks submodule must be checked out with its walkmeshes");

        var problems = files
            .Select(path => (Path: Path.GetRelativePath(haks, path), Problem: FindProblem(File.ReadAllBytes(path))))
            .Where(result => result.Problem != null)
            .Select(result => $"{result.Path}: {result.Problem}")
            .ToList();

        problems.Should().BeEmpty();
    }

    [Test]
    public void FindProblem_RejectsDoubledCarriageReturns()
    {
        var mesh = "node trimesh x\r\r\n verts 1\r\r\n 0 0 0\r\r\n faces 1\r\r\n 0 0 0 1 0 0 0 1\r\r\nendnode\r\r\n";

        FindProblem(System.Text.Encoding.ASCII.GetBytes(mesh)).Should().NotBeNull();
        FindProblem(System.Text.Encoding.ASCII.GetBytes(mesh.Replace("\r\r\n", "\r\n"))).Should().BeNull();
    }

    [Test]
    public void FindProblem_RejectsNonNumericVertexRows()
    {
        var mesh = "node trimesh x\r\n verts 1\r\n faces 1\r\n 0 0 0 1 0 0 0 1\r\nendnode\r\n";

        FindProblem(System.Text.Encoding.ASCII.GetBytes(mesh)).Should().NotBeNull();
    }

    private static string? FindProblem(byte[] content)
    {
        var lines = SplitLines(content);
        int? vertexCount = null;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            var vertices = VertexHeader.Match(line);
            if (vertices.Success)
            {
                vertexCount = int.Parse(vertices.Groups[2].Value);
                for (var v = 1; v <= vertexCount; v++)
                {
                    var coordinates = i + v < lines.Count ? Fields(lines[i + v]) : [];
                    if (coordinates.Length < 3 || coordinates.Take(3).Any(field => !float.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                        return $"line {i + v + 1} should hold vertex {v - 1}";
                }

                i += vertexCount.Value;
                continue;
            }

            var faces = FaceHeader.Match(line);
            if (!faces.Success)
                continue;

            var faceCount = int.Parse(faces.Groups[1].Value);
            for (var f = 1; f <= faceCount; f++)
            {
                var fields = i + f < lines.Count ? Fields(lines[i + f]) : [];
                if (fields.Length < 8 || fields.Take(8).Any(field => !int.TryParse(field, out _)))
                    return $"line {i + f + 1} should hold face {f - 1}";
                if (vertexCount != null && fields.Take(3).Select(int.Parse).Any(index => index < 0 || index >= vertexCount))
                    return $"face {f - 1} on line {i + f + 1} uses a vertex outside 0-{vertexCount - 1}";
            }

            i += faceCount;
        }

        return null;
    }

    // Matches CNWPlaceableSurfaceMesh::LoadWalkMeshString: CR, LF and CR LF each end one line.
    private static List<string> SplitLines(byte[] content)
    {
        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] != '\r' && content[i] != '\n')
                continue;

            lines.Add(System.Text.Encoding.ASCII.GetString(content, start, i - start));
            if (content[i] == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                i++;
            start = i + 1;
        }

        if (start < content.Length)
            lines.Add(System.Text.Encoding.ASCII.GetString(content, start, content.Length - start));
        return lines;
    }

    private static string[] Fields(string line) => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
