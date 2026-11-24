using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using Nwn.Formats.NativeModels;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Rugs, carpets and floor mats lie on the floor, which is the placeable's z = 0. Any part of a covering's top
/// surface at or below z = 0 is drawn under the floor, so a strip of the rug vanishes on every flat floor
/// (the Ashmark Floor Mat's bear hide did this along one side).
/// </summary>
public class FloorCoveringPlaceableTests
{
    // Up-facing area allowed under the floor: covers a few chin triangles of the Trailmark wookiee head
    // (0.003 m2), far below the 0.067 m2 strip of hide the Ashmark bear rug lost.
    private const float HiddenAreaTolerance = 0.01f;

    private static IEnumerable<StructureType> FloorCoverings() => Enum.GetValues<StructureType>()
        .Where(type => type.ToString() is var name
            && (name.EndsWith("FloorMat") || name.Contains("Rug") || name.StartsWith("Carpet")));

    [Test]
    public void FloorCoverings_KeepTheirTopSurfaceAboveTheFloor()
    {
        var root = FindRoot();
        var placeables = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var models = Directory.EnumerateDirectories(Path.Combine(root, "SWLOR_Haks"), "sw_plc*")
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.mdl"))
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var checkedTypes = new List<StructureType>();
        var problems = new List<string>();
        foreach (var type in FloorCoverings())
        {
            var resref = type.GetAttribute<StructureType, StructureAttribute>().Resref;
            var blueprint = Path.Combine(root, "Module", "utp", resref + ".utp.json");
            if (!File.Exists(blueprint))
                continue; // A base-game blueprint showing a base-game model, which ships with the game.

            using var json = JsonDocument.Parse(File.ReadAllText(blueprint));
            var appearance = json.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
            var modelName = placeables.GetValue(appearance, "ModelName");
            models.Should().ContainKey(modelName, $"{type}'s model {modelName} ships in the haks");

            checkedTypes.Add(type);
            var hidden = HiddenTopArea(new MdlReader().Parse(File.ReadAllBytes(models[modelName])));
            if (hidden > HiddenAreaTolerance)
                problems.Add($"{type} ({modelName}): {hidden:0.000} m2 of its top surface is under the floor");
        }

        checkedTypes.Should().Contain(Enum.GetValues<StructureType>().Where(type => type.ToString().EndsWith("FloorMat")),
            "every furniture set's Floor Mat uses a module blueprint and a hak model");
        problems.Should().BeEmpty("a rug's top surface must sit above z = 0 or the floor hides part of it");
    }

    // Projected area of the up-facing rendered triangles that lies at or below z = 0.
    private static float HiddenTopArea(MdlModel model)
    {
        var total = 0f;
        foreach (var node in model.GetMeshNodes().OfType<MdlTrimeshNode>().Where(node => node.Render && !node.IsWalkmesh))
        {
            var vertices = node.Vertices.Select(vertex => ToModelSpace(node, vertex)).ToArray();
            foreach (var face in node.Faces)
            {
                var triangle = new[] { vertices[face.VertexIndex0], vertices[face.VertexIndex1], vertices[face.VertexIndex2] };
                if (SignedArea(triangle) > 0)
                    total += Math.Abs(SignedArea(BelowFloor(triangle)));
            }
        }

        return total;
    }

    // Clips a triangle to its part at or below z = 0.
    private static Vector3[] BelowFloor(Vector3[] triangle)
    {
        var result = new List<Vector3>();
        for (var index = 0; index < triangle.Length; index++)
        {
            var a = triangle[index];
            var b = triangle[(index + 1) % triangle.Length];
            if (a.Z <= 0) result.Add(a);
            if (a.Z <= 0 != b.Z <= 0) result.Add(Vector3.Lerp(a, b, a.Z / (a.Z - b.Z)));
        }

        return result.ToArray();
    }

    // Area projected onto the floor; positive when the polygon faces up.
    private static float SignedArea(Vector3[] polygon)
    {
        var sum = 0f;
        for (var index = 0; index < polygon.Length; index++)
        {
            var a = polygon[index];
            var b = polygon[(index + 1) % polygon.Length];
            sum += a.X * b.Y - b.X * a.Y;
        }

        return sum / 2;
    }

    private static Vector3 ToModelSpace(MdlNode node, Vector3 vertex)
    {
        for (var current = node; current != null; current = current.Parent)
            vertex = Vector3.Transform(vertex, current.Orientation) + current.Position;
        return vertex;
    }

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
