using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Chassis Workshop's pure rules (size steps, chassis rows, message text) and the shape of its window.
/// Nothing here runs the engine or the client's layout solver.
/// </summary>
public class AstromechWorkshopTests
{
    [TestCase(1.00f, 1, 1.01f)]
    [TestCase(1.00f, -1, 0.99f)]
    [TestCase(1.14f, 1, 1.15f)]
    [TestCase(1.15f, 1, 1.15f)]
    [TestCase(0.85f, -1, 0.85f)]
    [TestCase(0.86f, -1, 0.85f)]
    [TestCase(1.00f, 0, 1.00f)]
    public void StepScale_MovesOneHundredthAndStaysInRange(float current, int direction, float expected)
    {
        AstromechWorkshop.StepScale(current, direction).Should().BeApproximately(expected, 0.0001f);
    }

    [Test]
    public void StepScale_CrossesTheWholeRangeInWholeSteps()
    {
        var scale = AstromechChassisCatalog.MinimumScale;
        var steps = 0;
        while (scale < AstromechChassisCatalog.MaximumScale - 0.0001f && steps < 100)
        {
            scale = AstromechWorkshop.StepScale(scale, 1);
            steps++;
        }

        steps.Should().Be(30);
        scale.Should().BeApproximately(AstromechChassisCatalog.MaximumScale, 0.0001f);
    }

    [TestCase(0.5f, 0.85f)]
    [TestCase(2.0f, 1.15f)]
    [TestCase(1.004f, 1.00f)]
    [TestCase(1.006f, 1.01f)]
    [TestCase(0f, 1.00f)]
    [TestCase(-3f, 1.00f)]
    [TestCase(float.NaN, 1.00f)]
    public void RoundScale_SnapsToHundredthsAndClamps(float input, float expected)
    {
        AstromechWorkshop.RoundScale(input).Should().BeApproximately(expected, 0.0001f);
    }

    [TestCase(1.00f, "100%")]
    [TestCase(0.85f, "85%")]
    [TestCase(1.15f, "115%")]
    [TestCase(0.994f, "99%")]
    public void FormatScale_ShowsAWholePercent(float scale, string expected)
    {
        AstromechWorkshop.FormatScale(scale).Should().Be(expected);
    }

    [Test]
    public void GetChassisAt_MapsEveryListRowToItsChassis()
    {
        for (var index = 0; index < AstromechChassisCatalog.All.Count; index++)
            AstromechWorkshop.GetChassisAt(index).Should().BeSameAs(AstromechChassisCatalog.All[index]);
    }

    [TestCase(-1)]
    [TestCase(8)]
    [TestCase(100)]
    public void GetChassisAt_ReturnsNothingForARowThatIsNotAChassis(int index)
    {
        AstromechWorkshop.GetChassisAt(index).Should().BeNull();
    }

    [Test]
    public void TheMessagesUseAsciiPunctuationOnly()
    {
        foreach (var message in new[]
                 {
                     AstromechWorkshop.NotAstromechMessage,
                     AstromechWorkshop.InSpaceMessage,
                     AstromechWorkshop.HeadScaleMessage
                 })
            message.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void TheWorkshopWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new ChassisWorkshopDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.ChassisWorkshop);
        window.LayoutFindings.Should().BeEmpty();
        window.InitialGeometry.Height.Should().BeLessThanOrEqualTo(680f);
        window.InitialGeometry.Width.Should().BeLessThanOrEqualTo(1280f);
    }

    [Test]
    public void TheWorkshopWindowTypeIsUnique()
    {
        var duplicates = Enum.GetValues<GuiWindowType>()
            .Where(type => (int)type == (int)GuiWindowType.ChassisWorkshop)
            .ToList();

        duplicates.Should().ContainSingle();
    }

    [Test]
    public void TheWorkshopViewModelNamesItsPartialAndElement()
    {
        ChassisWorkshopViewModel.MainPartial.Should().NotBeNullOrWhiteSpace();
        ChassisWorkshopViewModel.ContentElement.Should().NotBeNullOrWhiteSpace();
    }
}
