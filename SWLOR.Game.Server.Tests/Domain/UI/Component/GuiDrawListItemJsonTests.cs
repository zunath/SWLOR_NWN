using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.UI.Component;

/// <summary>
/// The draw list items turn their static values and bindings into the NUI JSON the client draws. A static value is
/// written as the value; a bound one as <c>{"bind": "Property"}</c>, named from the view model property.
/// </summary>
[TestFixture]
public class GuiDrawListItemJsonTests
{
    private NuiJsonScope _nui = null!;

    [SetUp]
    public void SetUp() => _nui = new NuiJsonScope();

    [TearDown]
    public void TearDown() => _nui.Dispose();

    private static string Bind(string property) => $"{{\"bind\":\"{property}\"}}";

    [Test]
    public void Arc_WritesItsStaticValues()
    {
        var json = _nui.Parse(new GuiDrawListItemArc<GuiJsonModel>()
            .SetColor(1, 2, 3, 4).SetIsFilled(true).SetLineThickness(2.5f).SetCenter(10f, 20f).SetRadius(7f)
            .SetAMinimum(0.5f).SetAMaximum(1.5f).SetIsEnabled(false).ToJson());

        json["enabled"]!.ToJsonString().Should().Be("false");
        json["color"]!.ToJsonString().Should().Be("{\"r\":1,\"g\":2,\"b\":3,\"a\":4}");
        json["fill"]!.ToJsonString().Should().Be("true");
        json["line_thickness"]!.ToJsonString().Should().Be("2.5");
        json["c"]!.ToJsonString().Should().Be("{\"x\":10,\"y\":20}");
        json["radius"]!.ToJsonString().Should().Be("7");
        json["amin"]!.ToJsonString().Should().Be("0.5");
        json["amax"]!.ToJsonString().Should().Be("1.5");
    }

    [Test]
    public void Arc_TakesAColorAndCenterObject()
    {
        var json = _nui.Parse(new GuiDrawListItemArc<GuiJsonModel>()
            .SetColor(new GuiColor(9, 8, 7)).SetCenter(new GuiVector2(3f, 4f)).ToJson());

        json["color"]!.ToJsonString().Should().Be("{\"r\":9,\"g\":8,\"b\":7,\"a\":255}");
        json["c"]!.ToJsonString().Should().Be("{\"x\":3,\"y\":4}");
        json["enabled"]!.ToJsonString().Should().Be("true", "an item is enabled until told otherwise");
    }

    [Test]
    public void Arc_WritesBindingsInsteadOfValues()
    {
        var json = _nui.Parse(new GuiDrawListItemArc<GuiJsonModel>()
            .BindIsEnabled(m => m.Flag).BindColor(m => m.Text).BindIsFilled(m => m.Flag)
            .BindLineThickness(m => m.Amount).BindCenter(m => m.Text).BindRadius(m => m.Amount)
            .BindAMinimum(m => m.Amount).BindAMaximum(m => m.Count).ToJson());

        json["enabled"]!.ToJsonString().Should().Be(Bind("Flag"));
        json["color"]!.ToJsonString().Should().Be(Bind("Text"));
        json["fill"]!.ToJsonString().Should().Be(Bind("Flag"));
        json["line_thickness"]!.ToJsonString().Should().Be(Bind("Amount"));
        json["c"]!.ToJsonString().Should().Be(Bind("Text"));
        json["radius"]!.ToJsonString().Should().Be(Bind("Amount"));
        json["amin"]!.ToJsonString().Should().Be(Bind("Amount"));
        json["amax"]!.ToJsonString().Should().Be(Bind("Count"));
    }

    [Test]
    public void Circle_WritesItsStaticValuesAndBindings()
    {
        var staticJson = _nui.Parse(new GuiDrawListItemCircle<GuiJsonModel>()
            .SetColor(5, 6, 7).SetIsFilled(true).SetLineThickness(3f).SetBounds(1f, 2f, 3f, 4f).ToJson());
        staticJson["color"]!.ToJsonString().Should().Be("{\"r\":5,\"g\":6,\"b\":7,\"a\":255}");
        staticJson["fill"]!.ToJsonString().Should().Be("true");
        staticJson["line_thickness"]!.ToJsonString().Should().Be("3");
        staticJson["rect"]!.ToJsonString().Should().Be("{\"x\":1,\"y\":2,\"w\":3,\"h\":4}");

        var boundJson = _nui.Parse(new GuiDrawListItemCircle<GuiJsonModel>()
            .BindIsEnabled(m => m.Flag).BindColor(m => m.Text).BindIsFilled(m => m.Flag)
            .BindLineThickness(m => m.Amount).BindBounds(m => m.Text).ToJson());
        boundJson["enabled"]!.ToJsonString().Should().Be(Bind("Flag"));
        boundJson["color"]!.ToJsonString().Should().Be(Bind("Text"));
        boundJson["fill"]!.ToJsonString().Should().Be(Bind("Flag"));
        boundJson["line_thickness"]!.ToJsonString().Should().Be(Bind("Amount"));
        boundJson["rect"]!.ToJsonString().Should().Be(Bind("Text"));
    }

    [Test]
    public void Circle_TakesAColorAndRectangleObject()
    {
        var json = _nui.Parse(new GuiDrawListItemCircle<GuiJsonModel>()
            .SetColor(new GuiColor(1, 1, 1, 9)).SetBounds(new GuiRectangle(5f, 6f, 7f, 8f)).ToJson());

        json["color"]!.ToJsonString().Should().Be("{\"r\":1,\"g\":1,\"b\":1,\"a\":9}");
        json["rect"]!.ToJsonString().Should().Be("{\"x\":5,\"y\":6,\"w\":7,\"h\":8}");
    }

    [Test]
    public void Curve_WritesItsStaticValuesAndBindings()
    {
        var staticJson = _nui.Parse(new GuiDrawListItemCurve<GuiJsonModel>()
            .SetColor(1, 2, 3).SetLineThickness(4f).SetA(1f, 2f).SetB(new GuiVector2(3f, 4f))
            .SetCtrl0(5f, 6f).SetCtrl1(new GuiVector2(7f, 8f)).ToJson());
        staticJson["line_thickness"]!.ToJsonString().Should().Be("4");
        staticJson["fill"]!.ToJsonString().Should().Be("false", "a curve is never filled");
        staticJson["a"]!.ToJsonString().Should().Be("{\"x\":1,\"y\":2}");
        staticJson["b"]!.ToJsonString().Should().Be("{\"x\":3,\"y\":4}");
        staticJson["ctrl0"]!.ToJsonString().Should().Be("{\"x\":5,\"y\":6}");
        staticJson["ctrl1"]!.ToJsonString().Should().Be("{\"x\":7,\"y\":8}");

        var boundJson = _nui.Parse(new GuiDrawListItemCurve<GuiJsonModel>()
            .SetColor(new GuiColor(1, 2, 3)).BindColor(m => m.Text).BindLineThickness(m => m.Amount)
            .BindA(m => m.Text).BindB(m => m.Text).BindCtrl0(m => m.Text).BindCtrl1(m => m.Text).ToJson());
        boundJson["color"]!.ToJsonString().Should().Be(Bind("Text"));
        boundJson["line_thickness"]!.ToJsonString().Should().Be(Bind("Amount"));
        boundJson["a"]!.ToJsonString().Should().Be(Bind("Text"));
        boundJson["ctrl1"]!.ToJsonString().Should().Be(Bind("Text"));
    }

    [Test]
    public void Image_WritesItsStaticValuesAndTheTextureRegion()
    {
        var json = _nui.Parse(new GuiDrawListItemImage<GuiJsonModel>()
            .SetResref("ui_pic").SetPosition(1f, 2f, 3f, 4f).SetAspect(NuiAspect.Fill)
            .SetHorizontalAlign(NuiHorizontalAlign.Right).SetVerticalAlign(NuiVerticalAlign.Bottom)
            .SetDrawTextureRegion(10, 20, 30, 40).ToJson());

        json["image"]!.ToJsonString().Should().Be("\"ui_pic\"");
        json["rect"]!.ToJsonString().Should().Be("{\"x\":1,\"y\":2,\"w\":3,\"h\":4}");
        json["image_aspect"]!.ToJsonString().Should().Be("1");
        json["image_halign"]!.ToJsonString().Should().Be("2");
        json["image_valign"]!.ToJsonString().Should().Be("2");
        json["image_region"]!.ToJsonString().Should().Be("{\"x\":10,\"y\":20,\"w\":30,\"h\":40}");
    }

    [Test]
    public void Image_WritesNoRegionUnlessOneIsSet()
    {
        var json = _nui.Parse(new GuiDrawListItemImage<GuiJsonModel>()
            .SetResref("ui_pic").SetPosition(new GuiRectangle(0f, 0f, 1f, 1f)).ToJson());

        json.AsObject().ContainsKey("image_region").Should().BeFalse();
    }

    [Test]
    public void Image_WritesBindings()
    {
        var json = _nui.Parse(new GuiDrawListItemImage<GuiJsonModel>()
            .BindResref(m => m.Text).BindPosition(m => m.Text).BindAspect(m => m.Count)
            .BindHorizontalAlign(m => m.Count).BindVerticalAlign(m => m.Count)
            .BindDrawTextureRegion(m => m.Text).ToJson());

        json["image"]!.ToJsonString().Should().Be(Bind("Text"));
        json["rect"]!.ToJsonString().Should().Be(Bind("Text"));
        json["image_aspect"]!.ToJsonString().Should().Be(Bind("Count"));
        json["image_halign"]!.ToJsonString().Should().Be(Bind("Count"));
        json["image_valign"]!.ToJsonString().Should().Be(Bind("Count"));
        json["image_region"]!.ToJsonString().Should().Be(Bind("Text"));
    }

    [Test]
    public void PolyLine_WritesItsPointsAsAFlatFloatArray()
    {
        var json = _nui.Parse(new GuiDrawListItemPolyLine<GuiJsonModel>()
            .SetColor(1, 2, 3).SetColor(new GuiColor(4, 5, 6)).SetIsFilled(true).SetLineThickness(2f)
            .AddPoint(1f, 2f).AddPoint(new GuiVector2(3f, 4f)).ToJson());

        json["points"]!.ToJsonString().Should().Be("[1,2,3,4]");
        json["fill"]!.ToJsonString().Should().Be("true");
        json["color"]!.ToJsonString().Should().Be("{\"r\":4,\"g\":5,\"b\":6,\"a\":255}");
        json["line_thickness"]!.ToJsonString().Should().Be("2");
    }

    [Test]
    public void PolyLine_BoundPointsReplaceTheStaticOnes()
    {
        var json = _nui.Parse(new GuiDrawListItemPolyLine<GuiJsonModel>()
            .AddPoint(1f, 2f).BindPoints(m => m.Text).BindColor(m => m.Text).BindIsFilled(m => m.Flag)
            .BindLineThickness(m => m.Amount).ToJson());

        json["points"]!.ToJsonString().Should().Be(Bind("Text"));
        json["color"]!.ToJsonString().Should().Be(Bind("Text"));
        json["fill"]!.ToJsonString().Should().Be(Bind("Flag"));
        json["line_thickness"]!.ToJsonString().Should().Be(Bind("Amount"));
    }

    [Test]
    public void Text_WritesItsStaticValuesAndBindings()
    {
        var staticJson = _nui.Parse(new GuiDrawListItemText<GuiJsonModel>()
            .SetColor(1, 2, 3).SetColor(new GuiColor(7, 8, 9)).SetBounds(1f, 2f, 3f, 4f)
            .SetBounds(new GuiRectangle(5f, 6f, 7f, 8f)).SetText("Hello").ToJson());
        staticJson["text"]!.ToJsonString().Should().Be("\"Hello\"");
        staticJson["rect"]!.ToJsonString().Should().Be("{\"x\":5,\"y\":6,\"w\":7,\"h\":8}");
        staticJson["color"]!.ToJsonString().Should().Be("{\"r\":7,\"g\":8,\"b\":9,\"a\":255}");

        var boundJson = _nui.Parse(new GuiDrawListItemText<GuiJsonModel>()
            .BindColor(m => m.Text).BindBounds(m => m.Text).BindText(m => m.Text).ToJson());
        boundJson["text"]!.ToJsonString().Should().Be(Bind("Text"));
        boundJson["rect"]!.ToJsonString().Should().Be(Bind("Text"));
        boundJson["color"]!.ToJsonString().Should().Be(Bind("Text"));
    }
}
