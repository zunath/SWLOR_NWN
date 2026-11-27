using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.UI;

/// <summary>
/// The converter between view model property values and the JSON the NUI client holds: every supported type is written
/// and read back, and a type it does not know is refused rather than sent as garbage.
/// </summary>
[TestFixture]
public class GuiPropertyConverterTests
{
    private NuiJsonScope _nui = null!;
    private GuiPropertyConverter _converter = null!;

    [SetUp]
    public void SetUp()
    {
        _nui = new NuiJsonScope();
        _converter = new GuiPropertyConverter();
    }

    [TearDown]
    public void TearDown() => _nui.Dispose();

    private string Written(object value) => _nui.Parse(_converter.ToJson(value)).ToJsonString();

    [Test]
    public void Scalars_AreWrittenAsTheirJsonValue()
    {
        Written("hello").Should().Be("\"hello\"");
        Written(42).Should().Be("42");
        Written(1.5f).Should().Be("1.5");
        Written(true).Should().Be("true");
    }

    [Test]
    public void ARectangleAndAColor_AreWrittenAsNuiObjects()
    {
        Written(new GuiRectangle(1f, 2f, 3f, 4f)).Should().Be("{\"x\":1,\"y\":2,\"w\":3,\"h\":4}");
        Written(new GuiColor(10, 20, 30, 40)).Should().Be("{\"r\":10,\"g\":20,\"b\":30,\"a\":40}");
    }

    [Test]
    public void BindingLists_AreWrittenAsArraysInOrder()
    {
        Written(new GuiBindingList<string> { "a", "b" }).Should().Be("[\"a\",\"b\"]");
        Written(new GuiBindingList<int> { 3, 1, 2 }).Should().Be("[3,1,2]");
        Written(new GuiBindingList<float> { 0.5f, 2f }).Should().Be("[0.5,2]");
        Written(new GuiBindingList<bool> { true, false }).Should().Be("[true,false]");
        Written(new GuiBindingList<GuiRectangle> { new(1f, 2f, 3f, 4f) }).Should().Be("[{\"x\":1,\"y\":2,\"w\":3,\"h\":4}]");
        Written(new GuiBindingList<GuiColor> { new(1, 2, 3) }).Should().Be("[{\"r\":1,\"g\":2,\"b\":3,\"a\":255}]");
        Written(new GuiBindingList<GuiComboEntry> { new("One", 1) }).Should().Contain("\"One\"");
    }

    [Test]
    public void AnEmptyBindingList_IsAnEmptyArray()
    {
        Written(new GuiBindingList<string>()).Should().Be("[]");
    }

    [Test]
    public void ToJson_RefusesATypeItDoesNotKnow()
    {
        var act = () => _converter.ToJson(new object());

        act.Should().Throw<Exception>().WithMessage("*Converter is not defined for type*");
    }

    [Test]
    public void Scalars_AreReadBack()
    {
        _converter.ToObject(_nui.From("\"hi\""), typeof(string)).Should().Be("hi");
        _converter.ToObject(_nui.From("7"), typeof(int)).Should().Be(7);
        _converter.ToObject(_nui.From("2.5"), typeof(float)).Should().Be(2.5f);
        _converter.ToObject(_nui.From("1"), typeof(bool)).Should().Be(true);
        _converter.ToObject(_nui.From("0"), typeof(bool)).Should().Be(false);
        _converter.ToObject(_nui.From("true"), typeof(bool)).Should().Be(true);
    }

    [Test]
    public void ARectangleAndAColor_AreReadBack()
    {
        var rectangle = (GuiRectangle)_converter.ToObject(_nui.From("{\"x\":1.5,\"y\":2,\"w\":3,\"h\":4}"), typeof(GuiRectangle));
        rectangle.X.Should().Be(1.5f);
        rectangle.Y.Should().Be(2f);
        rectangle.Width.Should().Be(3f);
        rectangle.Height.Should().Be(4f);

        var color = (GuiColor)_converter.ToObject(_nui.From("{\"r\":9,\"g\":8,\"b\":7,\"a\":6}"), typeof(GuiColor));
        color.Should().Be(new GuiColor(9, 8, 7, 6));
    }

    [Test]
    public void BindingLists_AreReadBackInOrder()
    {
        ((GuiBindingList<string>)_converter.ToObject(_nui.From("[\"a\",\"b\"]"), typeof(GuiBindingList<string>)))
            .Should().Equal("a", "b");
        ((GuiBindingList<int>)_converter.ToObject(_nui.From("[3,1,2]"), typeof(GuiBindingList<int>)))
            .Should().Equal(3, 1, 2);
        ((GuiBindingList<float>)_converter.ToObject(_nui.From("[0.5,2.5]"), typeof(GuiBindingList<float>)))
            .Should().Equal(0.5f, 2.5f);
        ((GuiBindingList<bool>)_converter.ToObject(_nui.From("[1,0,1]"), typeof(GuiBindingList<bool>)))
            .Should().Equal(true, false, true);

        var rectangles = (GuiBindingList<GuiRectangle>)_converter.ToObject(
            _nui.From("[{\"x\":1,\"y\":2,\"w\":3,\"h\":4}]"), typeof(GuiBindingList<GuiRectangle>));
        rectangles.Should().ContainSingle().Which.Width.Should().Be(3f);

        var colors = (GuiBindingList<GuiColor>)_converter.ToObject(
            _nui.From("[{\"r\":1,\"g\":2,\"b\":3,\"a\":4}]"), typeof(GuiBindingList<GuiColor>));
        colors.Should().ContainSingle().Which.Should().Be(new GuiColor(1, 2, 3, 4));
    }

    [Test]
    public void AnEmptyArray_IsReadBackAsAnEmptyList()
    {
        ((GuiBindingList<int>)_converter.ToObject(_nui.From("[]"), typeof(GuiBindingList<int>))).Should().BeEmpty();
    }

    [Test]
    public void ToObject_RefusesATypeItDoesNotKnow()
    {
        var act = () => _converter.ToObject(_nui.From("1"), typeof(DateTime));

        act.Should().Throw<Exception>().WithMessage("*Converter is not defined for type*");
    }
}
