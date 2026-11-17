using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.UI.Component;

/// <summary>The window definition turns its settings into the NUI window JSON, with bindings winning over static values.</summary>
[TestFixture]
public class GuiWindowJsonTests
{
    private NuiJsonScope _nui = null!;

    [SetUp]
    public void SetUp() => _nui = new NuiJsonScope();

    [TearDown]
    public void TearDown() => _nui.Dispose();

    private static string Bind(string property) => $"{{\"bind\":\"{property}\"}}";

    [Test]
    public void ANewWindowIsAResizableClosableBorderedWindowThatTakesInput()
    {
        var window = new GuiWindow<GuiJsonModel>();

        var json = _nui.Parse(window.Build());

        json["version"]!.ToJsonString().Should().Be("1");
        json["title"]!.ToJsonString().Should().Be("\"New Window\"");
        json["geometry"]!.ToJsonString().Should().Be(Bind("Geometry"), "the geometry is always bound");
        json["resizable"]!.ToJsonString().Should().Be("true");
        json["collapsed"]!.ToJsonString().Should().Be("false", "a window that cannot collapse sends false");
        json["closable"]!.ToJsonString().Should().Be("true");
        json["transparent"]!.ToJsonString().Should().Be("false");
        json["border"]!.ToJsonString().Should().Be("true");
        json["accepts_input"]!.ToJsonString().Should().Be("true");
        window.Geometry.Width.Should().Be(400f);
        window.KeepsOpenOnAreaChange.Should().BeFalse();
    }

    [Test]
    public void StaticSettingsAreWritten()
    {
        var window = new GuiWindow<GuiJsonModel>()
            .SetTitle("Bank").SetInitialGeometry(1f, 2f, 300f, 200f).SetIsResizable(false).SetIsCollapsible(true)
            .SetIsClosable(false).SetIsTransparent(true).SetShowBorder(false).SetAcceptsInput(false)
            .SetKeepsOpenOnAreaChange(true);

        var json = _nui.Parse(window.Build());

        json["title"]!.ToJsonString().Should().Be("\"Bank\"");
        json["resizable"]!.ToJsonString().Should().Be("false");
        json["collapsed"].Should().BeNull("a collapsible window sends null so the client may collapse it");
        json["closable"]!.ToJsonString().Should().Be("false");
        json["transparent"]!.ToJsonString().Should().Be("true");
        json["border"]!.ToJsonString().Should().Be("false");
        json["accepts_input"]!.ToJsonString().Should().Be("false");
        window.KeepsOpenOnAreaChange.Should().BeTrue();
    }

    [Test]
    public void BindingsReplaceTheStaticSettings()
    {
        var window = new GuiWindow<GuiJsonModel>()
            .BindTitle(m => m.Text).BindIsResizable(m => m.Flag).BindIsCollapsed(m => m.Flag)
            .BindIsClosable(m => m.Flag).BindIsTransparent(m => m.Flag).BindShowBorder(m => m.Flag)
            .BindAcceptsInput(m => m.Flag);

        var json = _nui.Parse(window.Build());

        json["title"]!.ToJsonString().Should().Be(Bind("Text"));
        foreach (var key in new[] { "resizable", "collapsed", "closable", "transparent", "border", "accepts_input" })
            json[key]!.ToJsonString().Should().Be(Bind("Flag"), key);
    }

    [Test]
    public void ATitleSetToNullWritesFalseSoTheWindowHasNoTitleBar()
    {
        var json = _nui.Parse(new GuiWindow<GuiJsonModel>().SetTitle(null!).Build());

        json["title"]!.ToJsonString().Should().Be("false");
    }

    [Test]
    public void InitialGeometryCanBeSetFromARectangle()
    {
        var window = new GuiWindow<GuiJsonModel>().SetInitialGeometry(new GuiRectangle(5f, 6f, 7f, 8f));

        window.Geometry.X.Should().Be(5f);
        window.Geometry.Height.Should().Be(8f);
    }

    [Test]
    public void RowsAndColumnsAreWrittenUnderTheRootRow()
    {
        var window = new GuiWindow<GuiJsonModel>();
        window.AddColumn(column => column.AddRow(row => row.AddSpacer()));
        window.AddRow(row => row.AddSpacer());

        var json = _nui.Parse(window.Build());

        window.Elements.Should().HaveCount(2);
        json["root"]!["type"]!.ToJsonString().Should().Be("\"row\"");
        json["root"]!["children"]!.AsArray().Should().HaveCount(2);
    }

    [Test]
    public void PartialViewsAreKeptByNameWithoutBorderOrScrollbars()
    {
        var window = new GuiWindow<GuiJsonModel>();

        window.DefinePartialView("sidebar", group => group.AddColumn(column => column.AddRow(row => row.AddSpacer())));

        window.PartialViews.Should().ContainKey("sidebar");
        var partial = _nui.Parse(window.PartialViews["sidebar"].ToJson());
        partial["border"]!.ToJsonString().Should().Be("false");
        partial["scrollbars"]!.ToJsonString().Should().Be("0");
    }

    [Test]
    public void OpenAndCloseEventsRecordTheirMethods()
    {
        var window = new GuiWindow<GuiJsonModel>();
        window.OpenedEventMethodInfo.Should().BeNull();

        window.BindOnOpened(m => m.OnClick(1)).BindOnClosed(m => m.OnClick(2));

        window.OpenedEventMethodInfo.Method.Name.Should().Be(nameof(GuiJsonModel.OnClick));
        window.ClosedEventMethodInfo.Method.Name.Should().Be(nameof(GuiJsonModel.OnClick));
    }
}
