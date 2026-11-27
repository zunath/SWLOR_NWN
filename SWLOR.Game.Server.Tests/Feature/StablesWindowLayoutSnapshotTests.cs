using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Pins the full NUI JSON of the stables window (root and every partial view) against a snapshot taken before
/// <c>StablesDefinition.BuildWindow</c> was split into one method per section. Identical JSON means the client receives
/// the same window. Set SWLOR_UPDATE_LAYOUT_SNAPSHOTS=1 to rewrite the snapshot.
/// </summary>
public class StablesWindowLayoutSnapshotTests
{
    private static readonly string SnapshotPath = Path.Combine(
        TestContext.CurrentContext.TestDirectory, "..", "..", "..", "Feature", "Snapshots", "StablesWindow.json.txt");

    [Test]
    public void StablesWindow_FullJsonMatchesSnapshot()
    {
        using var nui = new NuiJsonScope();
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definitionType = typeof(IGuiWindowDefinition).Assembly.GetType("SWLOR.Game.Server.Feature.GuiDefinition.StablesDefinition")!;
        var definition = Activator.CreateInstance(definitionType)!;
        var constructed = ((IGuiWindowDefinition)definition).BuildWindow();
        constructed.LayoutFindings.Should().BeEmpty();

        // The validation-only build keeps the authored window and skips the player event registration, which needs the
        // service container. The widgets and the window serialize here exactly as Build does for the client.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var builder = definitionType.GetField("_builder", flags)!.GetValue(definition)!;
        var authoredWindow = builder.GetType().GetField("_activeWindow", flags)!.GetValue(builder)!;
        var partials = (IReadOnlyDictionary<string, IGuiWidget>)authoredWindow.GetType().GetProperty("PartialViews")!.GetValue(authoredWindow)!;

        var text = new StringBuilder();
        text.AppendLine("ROOT");
        text.AppendLine(nui.Parse((Json)authoredWindow.GetType().GetMethod("Build")!.Invoke(authoredWindow, null)!).ToJsonString());
        foreach (var key in partials.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            text.AppendLine("PARTIAL " + key);
            text.AppendLine(nui.Parse(partials[key].ToJson()).ToJsonString());
        }

        // Element ids are random GUIDs; number them in order of first appearance so the snapshot keeps which widgets share an id.
        var ids = new Dictionary<string, string>();
        var actual = Regex.Replace(text.ToString().Replace("\r\n", "\n"),
            "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
            match => ids.TryGetValue(match.Value, out var id) ? id : ids[match.Value] = $"id-{ids.Count + 1}");
        var path = Path.GetFullPath(SnapshotPath);
        if (Environment.GetEnvironmentVariable("SWLOR_UPDATE_LAYOUT_SNAPSHOTS") == "1")
            File.WriteAllText(path, actual, new UTF8Encoding(false));

        File.ReadAllText(path).Replace("\r\n", "\n").Should().Be(actual);
    }
}
