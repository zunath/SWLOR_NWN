using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Service;

public class GuiHelperNamedBindingTests
{
    [Test]
    public void ADeclaredPropertyStillNamesItsBinding()
    {
        GuiHelper<AppearanceEditorViewModel>.GetPropertyName(model => model.IsAppearanceSelected)
            .Should().Be("IsAppearanceSelected");
    }

    [Test]
    public void TheBindingAccessorNamesABindingWithoutADeclaredProperty()
    {
        typeof(AppearanceEditorViewModel).GetProperty("ChestCloth1Region").Should().BeNull();
        GuiHelper<AppearanceEditorViewModel>.GetPropertyName(model => model.GetBindingValue<GuiRectangle>("ChestCloth1Region"))
            .Should().Be("ChestCloth1Region");
    }

    [Test]
    public void TheBindingAccessorResolvesTheNameADeclaredPropertyWouldHave()
    {
        GuiHelper<AppearanceEditorViewModel>.GetPropertyName(model => model.GetBindingValue<bool>("IsAppearanceSelected"))
            .Should().Be(GuiHelper<AppearanceEditorViewModel>.GetPropertyName(model => model.IsAppearanceSelected));
    }
}
