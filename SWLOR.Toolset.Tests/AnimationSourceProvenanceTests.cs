using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class AnimationSourceProvenanceTests
{
    private string _folder = null!;
    private string _model = null!;
    private string _input = null!;
    private string _output = null!;
    private string Manifest => Path.Combine(_output, "active-manifest.json");

    [SetUp]
    public void SetUp()
    {
        _folder = Path.Combine(Path.GetTempPath(), "swlor-source-provenance-" + Guid.NewGuid().ToString("N"));
        _model = Path.Combine(_folder, "a_ba.mdl");
        _input = Path.Combine(_folder, "input.json");
        _output = Path.Combine(_folder, "output");
        Directory.CreateDirectory(Path.Combine(_output, "force"));
        File.WriteAllText(_model, Model(.1f));
        File.WriteAllText(_input, JsonSerializer.Serialize(new[]
        {
            new ActiveMotion("Native", "sw_native", "Force", "Combat", "Native lift", SourceAnimation: "castout"),
            new ActiveMotion("Directed", "sw_direct", "Force", "Combat", "Authored lift")
        }));
        var choreography = new Choreography("Directed", "Authored lift", 1,
            [new(0, "Start", "pause1"), new(.5f, "Lift", "castout", .5f), new(1, "Finish", "pause1")]);
        File.WriteAllText(Path.Combine(_output, "force/choreographies.json"),
            JsonSerializer.Serialize(new[] { choreography }, BulkMotionAuthor.Json));
        BulkMotionAuthor.Generate(_model, _input, _output, false);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_folder, true);

    [TestCase("changed")]
    [TestCase("missing")]
    [TestCase("null")]
    public void PreservedProjectsLoseUnverifiedBaseProvenanceWithoutChangingTheirFiles(string sourceState)
    {
        var authored = ProjectBytes();
        if (sourceState == "changed")
            File.WriteAllText(_model, Model(.2f));
        else
        {
            var previous = JsonNode.Parse(File.ReadAllText(Manifest))!.AsObject();
            if (sourceState == "missing") previous.Remove("SourceModelSha256");
            else previous["SourceModelSha256"] = null;
            File.WriteAllText(Manifest, previous.ToJsonString());
        }

        // Refreshing the manifest must not silently rebake animator-owned projects,
        // or imply that an unchanged saved project was generated from this new model.
        for (var pass = 0; pass < 2; pass++)
        {
            BulkMotionAuthor.Generate(_model, _input, _output, false);
            using var manifest = JsonDocument.Parse(File.ReadAllText(Manifest));
            manifest.RootElement.GetProperty("SourceModelSha256").GetString().Should().Be(ModelHash());
            // A second refresh sees the new top-level hash; it must not resurrect
            // provenance that was invalidated on the first refresh.
            foreach (var report in manifest.RootElement.GetProperty("Animations").EnumerateArray())
            {
                var id = report.GetProperty("Id").GetString()!;
                report.GetProperty("SourceModel").GetString().Should().Be("Existing authored project");
                report.GetProperty("Profile").GetString().Should().Be("Preserved authored motion");
                foreach (var field in new[] { "SourceAnimation", "ChoreographyPath", "ChoreographySha256", "ChoreographySourceSha256" })
                    report.GetProperty(field).ValueKind.Should().Be(JsonValueKind.Null, field + " is no longer verified for " + id);
                report.GetProperty("ProjectSha256").GetString().Should().Be(
                    BulkMotionAuthor.ProjectHash(Encoding.UTF8.GetString(authored[id])));
                File.ReadAllBytes(Path.Combine(_output, "force", id + ".swlanim")).Should().Equal(authored[id]);
            }
        }
    }

    [Test]
    public void MatchingBaseHashRetainsNativeAndChoreographedProvenanceWithoutChangingProjects()
    {
        var authored = ProjectBytes();
        var originalManifest = File.ReadAllBytes(Manifest);

        BulkMotionAuthor.Generate(_model, _input, _output, false);

        File.ReadAllBytes(Manifest).Should().Equal(originalManifest);
        foreach (var (id, bytes) in authored)
            File.ReadAllBytes(Path.Combine(_output, "force", id + ".swlanim")).Should().Equal(bytes);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Manifest));
        var reports = manifest.RootElement.GetProperty("Animations").EnumerateArray().ToArray();
        reports[0].GetProperty("SourceAnimation").GetString().Should().Be("castout");
        reports[1].GetProperty("ChoreographySha256").GetString().Should().NotBeNullOrEmpty();
    }

    private Dictionary<string, byte[]> ProjectBytes() => new[] { "Native", "Directed" }
        .ToDictionary(id => id, id => File.ReadAllBytes(Path.Combine(_output, "force", id + ".swlanim")));

    private string ModelHash() => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_model))).ToLowerInvariant();

    private static string Model(float lift)
    {
        var rest = new PosedNode(Vector3.Zero, Quaternion.Identity, 1);
        var project = new AnimationProject
        {
            Name = "pause1", ModelName = "a_ba", AnimationRoot = "a_ba", Duration = 1,
            Joints = [new("a_ba", -1, rest), new("rootdummy", 0, rest),
                new("lfoot_g", 1, rest with { Position = new(-.1f, 0, 0) }),
                new("rfoot_g", 1, rest with { Position = new(.1f, 0, 0) })]
        };
        var neutral = project.Joints.Select(j => j.Rest).ToArray();
        project.SetKey(0, neutral);
        project.SetKey(1, neutral);
        var model = "newmodel a_ba\nsetsupermodel a_ba NULL\n" + AnimationMdl.ExportGeometry(project) + AnimationMdl.Export(project);
        var raised = neutral.ToArray();
        raised[1] = raised[1] with { Position = new(0, 0, lift) };
        project.SetKey(.5f, raised);
        return model + AnimationMdl.Export(project, "castout") + "donemodel a_ba\n";
    }
}
