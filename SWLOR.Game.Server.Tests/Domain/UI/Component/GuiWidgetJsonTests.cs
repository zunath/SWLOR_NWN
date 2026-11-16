using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.UI.Component;

/// <summary>
/// The options every NUI widget shares (size, margin, tooltips, color, draw lists, events) and the sliders and image
/// that build on them. A widget is checked by the JSON it writes for the client.
/// </summary>
[TestFixture]
public class GuiWidgetJsonTests
{
    private NuiJsonScope _nui = null!;

    [SetUp]
    public void SetUp() => _nui = new NuiJsonScope();

    [TearDown]
    public void TearDown() => _nui.Dispose();

    private static string Bind(string property) => $"{{\"bind\":\"{property}\"}}";

    [Test]
    public void ADefaultWidgetIsEnabledAndVisibleWithNoOtherOptions()
    {
        var json = _nui.Parse(new GuiSliderInt<GuiJsonModel>().ToJson());

        json["enabled"]!.ToJsonString().Should().Be("true");
        json["visible"]!.ToJsonString().Should().Be("true");
        foreach (var key in new[] { "id", "width", "height", "aspect", "margin", "padding", "tooltip", "disabled_tooltip", "encouraged", "foreground_color" })
            json.AsObject().ContainsKey(key).Should().BeFalse($"{key} was never set");
    }

    [Test]
    public void StaticOptionsAreWrittenOntoTheElement()
    {
        var json = _nui.Parse(new GuiSliderInt<GuiJsonModel>()
            .SetId("slider").SetWidth(120f).SetHeight(30f).SetAspectRatio(2f).SetMargin(4f).SetPadding(3f)
            .SetIsEnabled(false).SetIsVisible(false).SetTooltip("Tip").SetDisabledTooltip("Off")
            .SetIsEncouraged(true).SetColor(1, 2, 3).ToJson());

        json["id"]!.ToJsonString().Should().Be("\"slider\"");
        json["width"]!.ToJsonString().Should().Be("120");
        json["height"]!.ToJsonString().Should().Be("30");
        json["aspect"]!.ToJsonString().Should().Be("2");
        json["margin"]!.ToJsonString().Should().Be("4");
        json["padding"]!.ToJsonString().Should().Be("3");
        json["enabled"]!.ToJsonString().Should().Be("false");
        json["visible"]!.ToJsonString().Should().Be("false");
        json["tooltip"]!.ToJsonString().Should().Be("\"Tip\"");
        json["disabled_tooltip"]!.ToJsonString().Should().Be("\"Off\"");
        json["encouraged"]!.ToJsonString().Should().Be("true");
        json["foreground_color"]!.ToJsonString().Should().Be("{\"r\":1,\"g\":2,\"b\":3,\"a\":255}");
    }

    [Test]
    public void ABoundOptionWinsOverItsStaticValue()
    {
        var json = _nui.Parse(new GuiSliderInt<GuiJsonModel>()
            .SetTooltip("static").BindTooltip(m => m.Text)
            .SetDisabledTooltip("static").BindDisabledTooltip(m => m.Text)
            .BindIsEnabled(m => m.Flag).BindIsVisible(m => m.Flag).BindIsEncouraged(m => m.Flag)
            .SetColor(new GuiColor(1, 1, 1)).BindColor(m => m.Text).ToJson());

        json["tooltip"]!.ToJsonString().Should().Be(Bind("Text"));
        json["disabled_tooltip"]!.ToJsonString().Should().Be(Bind("Text"));
        json["enabled"]!.ToJsonString().Should().Be(Bind("Flag"));
        json["visible"]!.ToJsonString().Should().Be(Bind("Flag"));
        json["encouraged"]!.ToJsonString().Should().Be(Bind("Flag"));
        json["foreground_color"]!.ToJsonString().Should().Be(Bind("Text"));
    }

    [Test]
    public void TheVisibilityOverrideReplacesTheVisibleValue()
    {
        var widget = new GuiSliderInt<GuiJsonModel>();
        widget.VisibilityOverrideBindName = "Hidden";

        _nui.Parse(widget.ToJson())["visible"]!.ToJsonString().Should().Be(Bind("Hidden"));
    }

    [Test]
    public void MouseEventsGiveTheWidgetAnIdAndRecordTheirMethod()
    {
        var widget = new GuiSliderInt<GuiJsonModel>();

        widget.BindOnMouseDown(m => m.OnClick(1)).BindOnMouseUp(m => m.OnClick(2));

        widget.Id.Should().NotBeNullOrWhiteSpace("the client only sends events for elements with an id");
        widget.Events.Keys.Should().BeEquivalentTo("mousedown", "mouseup");
        widget.Events["mousedown"].Method.Name.Should().Be(nameof(GuiJsonModel.OnClick));
        widget.Events["mouseup"].Method.Name.Should().Be(nameof(GuiJsonModel.OnClick));
    }

    [Test]
    public void ADrawListIsAttachedToTheElement()
    {
        var widget = new GuiSliderInt<GuiJsonModel>();
        widget.AddDrawList(list => list.AddText(text => text.SetText("Hi").SetBounds(0f, 0f, 10f, 10f)));

        var json = _nui.Parse(widget.ToJson());

        json["draw_list"]!.AsArray().Should().HaveCount(1);
        json["draw_list"]![0]!["text"]!.ToJsonString().Should().Be("\"Hi\"");
    }

    [Test]
    public void SliderInt_WritesItsValuesAndBindings()
    {
        var staticJson = _nui.Parse(new GuiSliderInt<GuiJsonModel>()
            .SetValue(5).SetMinimum(1).SetMaximum(10).SetStepSize(2).ToJson());
        staticJson["type"]!.ToJsonString().Should().Be("\"slider\"");
        staticJson["value"]!.ToJsonString().Should().Be("5");
        staticJson["min"]!.ToJsonString().Should().Be("1");
        staticJson["max"]!.ToJsonString().Should().Be("10");
        staticJson["step"]!.ToJsonString().Should().Be("2");

        var boundJson = _nui.Parse(new GuiSliderInt<GuiJsonModel>()
            .BindValue(m => m.Count).BindMinimum(m => m.Count).BindMaximum(m => m.Count).BindStepSize(m => m.Count).ToJson());
        foreach (var key in new[] { "value", "min", "max", "step" })
            boundJson[key]!.ToJsonString().Should().Be(Bind("Count"));
    }

    [Test]
    public void SliderFloat_WritesItsValuesAndBindings()
    {
        var staticJson = _nui.Parse(new GuiSliderFloat<GuiJsonModel>()
            .SetValue(0.5f).SetMinimum(0f).SetMaximum(2.5f).SetStepSize(0.25f).ToJson());
        staticJson["type"]!.ToJsonString().Should().Be("\"sliderf\"");
        staticJson["value"]!.ToJsonString().Should().Be("0.5");
        staticJson["min"]!.ToJsonString().Should().Be("0");
        staticJson["max"]!.ToJsonString().Should().Be("2.5");
        staticJson["step"]!.ToJsonString().Should().Be("0.25");

        var boundJson = _nui.Parse(new GuiSliderFloat<GuiJsonModel>()
            .BindValue(m => m.Amount).BindMinimum(m => m.Amount).BindMaximum(m => m.Amount).BindStepSize(m => m.Amount).ToJson());
        foreach (var key in new[] { "value", "min", "max", "step" })
            boundJson[key]!.ToJsonString().Should().Be(Bind("Amount"));
    }

    [Test]
    public void Image_WritesItsStaticValuesAndRegion()
    {
        var json = _nui.Parse(new GuiImage<GuiJsonModel>()
            .SetResref("ui_pic").SetAspect(NuiAspect.Stretch).SetHorizontalAlign(NuiHorizontalAlign.Left)
            .SetVerticalAlign(NuiVerticalAlign.Top).SetRegion(new GuiRectangle(1f, 2f, 3f, 4f)).ToJson());

        json["type"]!.ToJsonString().Should().Be("\"image\"");
        json["value"]!.ToJsonString().Should().Be("\"ui_pic\"");
        json["image_aspect"]!.ToJsonString().Should().Be("5");
        json["image_halign"]!.ToJsonString().Should().Be("1");
        json["image_valign"]!.ToJsonString().Should().Be("1");
        json["image_region"]!.ToJsonString().Should().Be("{\"x\":1,\"y\":2,\"w\":3,\"h\":4}");
    }

    [Test]
    public void Image_WritesBindingsAndNoRegionByDefault()
    {
        var plain = _nui.Parse(new GuiImage<GuiJsonModel>().SetResref("ui_pic").ToJson());
        plain.AsObject().ContainsKey("image_region").Should().BeFalse();

        var json = _nui.Parse(new GuiImage<GuiJsonModel>()
            .BindResref(m => m.Text).BindAspect(m => m.Count).BindHorizontalAlign(m => m.Count)
            .BindVerticalAlign(m => m.Count).BindRegion(m => m.Text).ToJson());
        json["value"]!.ToJsonString().Should().Be(Bind("Text"));
        json["image_aspect"]!.ToJsonString().Should().Be(Bind("Count"));
        json["image_halign"]!.ToJsonString().Should().Be(Bind("Count"));
        json["image_valign"]!.ToJsonString().Should().Be(Bind("Count"));
        json["image_region"]!.ToJsonString().Should().Be(Bind("Text"));
    }
}
