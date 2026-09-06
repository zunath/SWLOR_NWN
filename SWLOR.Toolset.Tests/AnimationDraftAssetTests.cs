using System.IO.Compression;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public class AnimationDraftAssetTests
{
    private static readonly string[] Names = ["ShieldBash", "ShieldWall", "CoveringStrike", "Invincible",
        "RiotBlade", "RendingStrike", "SavageCleave"];
    private static string Root
    {
        get
        {
            var path = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (path != null && !File.Exists(Path.Combine(path.FullName, "SWLOR.Game.Server.sln"))) path = path.Parent;
            return path?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
    private static string Folder => Path.Combine(Root, "design", "animations", "drafts", "vibroblade");

    [Test]
    public void TwoBeatOneShotsAreRejectedBeforePosing()
    {
        var motion = new Motion("TwoBeat", "Two beat", 0, "", "", "", false,
            [new Beat(0, "Start", new()), new Beat(1, "Finish", new())], "");
        Action bake = () => MotionAuthor.Bake(null!, [], null!, motion);
        bake.Should().Throw<InvalidDataException>().WithMessage("*at least three beats*");
    }

    [Test]
    public void TwoBeatClosedChannelsRemainSupported()
    {
        var recipe = JsonSerializer.Deserialize<Recipe>(File.ReadAllText(Path.Combine(Root,
            "design", "animations", "recipes", "vibroblade.json")), Recipe.Json)!;
        var channel = recipe.Motions.Single(m => m.Id == "ShieldWall");
        var motion = channel with { Beats = [channel.Beats[0], channel.Beats[0] with { Time = 1 }] };
        var rig = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Folder, "ShieldWall.swlanim")));
        var project = MotionAuthor.Bake(rig, rig.Sample(0), recipe, motion);
        project.Duration.Should().Be(1);
        project.Keys.Should().HaveCountGreaterThan(2);
        project.Sample(0).Should().Equal(project.Sample(1));
    }

    [TestCase("a_ba")]
    [TestCase("a_fa")]
    public void InstalledHumanoidOverlaysContainEveryClipAndBothPlaybackPhases(string modelName)
    {
        var path = Path.Combine(Root, "SWLOR_Haks", "sw_cr_creature", modelName + ".mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize the HAK submodule to verify installed native assets.");
        var target = new MdlReader().Parse(File.ReadAllBytes(path));
        target.SuperModel.Should().Be("an_" + modelName);
        var overlay = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(path)!, target.SuperModel + ".mdl")));
        var registry = JsonSerializer.Deserialize<AnimationRegistration[]>(File.ReadAllText(Path.Combine(Root, "design", "animations", "registry.json")))!;
        foreach (var name in Names)
        {
            var entry = registry.Single(r => r.Name == name);
            entry.Targets.Should().Contain("SWLOR_Haks/sw_cr_creature/" + modelName + ".mdl");
            overlay.Animations.Single(a => a.Name == entry.AnimationName).Length.Should().BeApproximately(entry.Duration, .0001f);
            overlay.Animations.Should().Contain(a => a.Name == entry.AnimationName + "_in");
            overlay.Animations.Should().Contain(a => a.Name == entry.AnimationName + "_out");
            var exit = overlay.Animations.Single(a => a.Name == entry.AnimationName + "_out");
            exit.Length.Should().BeApproximately(.2f, .0001f);
            var exitPose = MdlAnimationPose.Sample(exit, exit.Length, MdlAnimationPose.BindPose(overlay));
            var idle = MdlAnimationPose.Sample(MdlAnimationPose.FindIdle(target), 0, MdlAnimationPose.BindPose(target));
            foreach (var joint in AnimationProject.FromModel(target).Joints.Where(j => j.Parent >= 0))
            {
                var expected = idle.TryGetValue(joint.Name, out var value) ? value : joint.Rest;
                Vector3.Distance(exitPose[joint.Name].Position * target.Scale, expected.Position).Should().BeLessThan(.0001f);
                Math.Abs(Quaternion.Dot(exitPose[joint.Name].Orientation, expected.Orientation)).Should().BeGreaterThan(.9999f);
            }
            var installed = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Root, "design", "animations", name + ".swlanim")));
            var draft = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Folder, name + ".swlanim")));
            installed.Serialize().Should().Be(draft.Serialize());
        }
    }

    [TestCaseSource(nameof(Names))]
    public void DraftsRemainEditableAndPreserveNativeBoneLengths(string name)
    {
        var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Folder, name + ".swlanim")));
        project.Name.Should().Be(name);
        project.ModelName.Should().Be("a_ba");
        project.Keys.Count.Should().BeInRange(10, 80);
        project.Events.Should().BeEmpty("review drafts must not accidentally fire gameplay events");
        foreach (var key in project.Keys)
            for (var i = 0; i < project.Joints.Count; i++)
                if (project.Joints[i].Name != "rootdummy")
                    Vector3.Distance(key.Pose[i].Position, project.Joints[i].Rest.Position).Should().BeLessThan(.000001f,
                        "posing must not stretch a native bone; interpolation may round the last float bit");
        var first = AnimationRig.World(project.Joints, project.Sample(0));
        var last = AnimationRig.World(project.Joints, project.Sample(project.Duration));
        for (var i = 0; i < first.Length; i++)
            Vector3.Distance(first[i].Translation, last[i].Translation).Should().BeLessThan(.0001f, "the ready pose must close without a jump");
        var feet = new[] { "lfoot_g", "rfoot_g" }.Select(n => project.Joints.FindIndex(j => j.Name == n)).ToArray();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, "manifest.json")));
        var beats = manifest.RootElement.GetProperty("Animations").EnumerateArray().Single(a => a.GetProperty("Id").GetString() == name)
            .GetProperty("Beats").EnumerateArray().Select(b => b.GetProperty("Time").GetSingle()).ToArray();
        var planted = AnimationRig.World(project.Joints, project.Sample(beats[1]));
        for (var t = 0f; t < project.Duration; t += 1f / 120)
        {
            var world = AnimationRig.World(project.Joints, project.Sample(t));
            foreach (var i in feet)
            {
                world[i].Translation.Z.Should().BeGreaterThan(.125f);
                if (name != "CoveringStrike" && t >= beats[1] && t <= beats[^2])
                    Vector3.Distance(world[i].Translation, planted[i].Translation).Should().BeLessThan(.015f,
                        "planted feet must remain fixed between baked frames, even as the body turns");
            }
        }
    }

    [TestCaseSource(nameof(Names))]
    public void EveryMoveKeepsTheActualShieldUprightInItsAuthoredGuard(string name)
    {
        var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Folder, name + ".swlanim")));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, "manifest.json")));
        var entry = manifest.RootElement.GetProperty("Animations").EnumerateArray().Single(a => a.GetProperty("Id").GetString() == name);
        var time = entry.GetProperty("Beats")[1].GetProperty("Time").GetSingle();
        var hand = AnimationRig.World(project.Joints, project.Sample(time))[project.Joints.FindIndex(j => j.Name == "lhand_g")];
        // AShLw model +Y is its top, -X its facing. The old proxy incorrectly used +Z as top.
        Vector3.TransformNormal(Vector3.UnitY, hand).Z.Should().BeGreaterThan(.7f, "an equipped shield must not lie sideways over the arm");
        Vector3.TransformNormal(-Vector3.UnitX, hand).Y.Should().BeGreaterThan(.5f, "the shield face must point toward the attack");
    }

    [TestCaseSource(nameof(Names))]
    public void OneShotsFinishInTheNativeIdlePoseRatherThanHoldingTheirCombatStance(string name)
    {
        if (name == "ShieldWall") return; // Channel loop releases through its installed exit phase.
        var path = Path.Combine(Root, "SWLOR_Haks", "sw_cr_creature", "a_ba.mdl");
        if (!File.Exists(path)) Assert.Ignore("Initialize the HAK submodule for native idle verification.");
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        var idle = MdlAnimationPose.Sample(model.Animations.Single(a => a.Name == "pause1"), 0, MdlAnimationPose.BindPose(model));
        var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Folder, name + ".swlanim")));
        var finish = project.Sample(project.Duration);
        for (var i = 0; i < project.Joints.Count; i++)
        {
            var joint = project.Joints[i];
            var expected = idle.TryGetValue(joint.Name, out var value) ? value : joint.Rest;
            Vector3.Distance(finish[i].Position, expected.Position).Should().BeLessThan(.0001f);
            Math.Abs(Quaternion.Dot(finish[i].Orientation, expected.Orientation)).Should().BeGreaterThan(.9999f);
        }
    }

    [TestCaseSource(nameof(Names))]
    public void EveryDraftSurvivesNativeMdlExchange(string name)
    {
        var project = AnimationProject.Deserialize(File.ReadAllText(Path.Combine(Folder, name + ".swlanim")));
        var mdl = $"newmodel a_ba\nsetsupermodel a_ba NULL\n" + AnimationMdl.ExportGeometry(project) +
                  AnimationMdl.Export(project) + "donemodel a_ba\n";
        var model = new MdlReader().Parse(System.Text.Encoding.UTF8.GetBytes(mdl));
        var clip = model.Animations.Single(); clip.Name.Should().Be(name);
        for (var t = 0f; t < project.Duration; t += .037f)
        {
            var sampled = MdlAnimationPose.Sample(clip, t, MdlAnimationPose.BindPose(model));
            var actual = AnimationRig.World(project.Joints, project.Joints.Select(j => sampled[j.Name]).ToArray());
            var expected = AnimationRig.World(project.Joints, project.Sample(t));
            for (var i = 0; i < actual.Length; i++)
                Vector3.Distance(actual[i].Translation, expected[i].Translation).Should().BeLessThan(.001f);
        }
    }

    [Test]
    public void ManifestMatchesCurrentPerksFromTheFirstNineBibleReferencesAndSavedProjects()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, "manifest.json")));
        using var zip = ZipFile.OpenRead(Path.Combine(Root, "design", "bible", "SWLOR Design Bible - Combat Upgrade.xlsx"));
        XDocument Xml(string name) { using var stream = zip.GetEntry(name)!.Open(); return XDocument.Load(stream); }
        XNamespace s = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var sheet = Xml("xl/workbook.xml").Descendants(s + "sheet").Single(e => (string?)e.Attribute("name") == "Animations");
        var relationship = Xml("xl/_rels/workbook.xml.rels").Root!.Elements().Single(e =>
            (string?)e.Attribute("Id") == (string?)sheet.Attribute(r + "id"));
        var target = (string)relationship.Attribute("Target")!;
        var sheetPath = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        var rows = Xml(sheetPath).Descendants(s + "row").ToDictionary(e => (int)e.Attribute("r")!);
        var shared = zip.GetEntry("xl/sharedStrings.xml") == null ? [] :
            Xml("xl/sharedStrings.xml").Descendants(s + "si").Select(e => string.Concat(e.Descendants(s + "t").Select(t => t.Value))).ToArray();
        string Text(XElement cell) => (string?)cell.Attribute("t") switch
        {
            "s" => shared[int.Parse(cell.Element(s + "v")!.Value)],
            "inlineStr" => string.Concat(cell.Descendants(s + "t").Select(t => t.Value)),
            _ => cell.Element(s + "v")?.Value ?? ""
        };
        var entries = manifest.RootElement.GetProperty("Animations").EnumerateArray().ToArray();
        entries.Select(e => e.GetProperty("Id").GetString()).Should().Equal(Names);
        entries.Select(e => e.GetProperty("BibleRow").GetInt32()).Should().Equal(2, 3, 4, 5, 7, 8, 9);
        foreach (var entry in entries)
        {
            var row = entry.GetProperty("BibleRow").GetInt32();
            string Cell(string column) => Text(rows[row].Elements(s + "c").Single(c => (string?)c.Attribute("r") == column + row));
            Cell("C").Should().Be(entry.GetProperty("Name").GetString());
            Cell("E").Should().Be(entry.GetProperty("Reference").GetString());
            var text = File.ReadAllText(Path.Combine(Folder, entry.GetProperty("Project").GetString()!)).Replace("\r\n", "\n");
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant().Should().Be(entry.GetProperty("ProjectSha256").GetString());
        }
    }
}
