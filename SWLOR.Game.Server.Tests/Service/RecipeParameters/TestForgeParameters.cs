using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public sealed class TestForgeParameters : RecipeMinigameParameters
{
    public int Center { get; }
    public int Width { get; }
    public double Cooling { get; }
    public TestForgeStyle Style { get; }

    public TestForgeParameters(int center, int width, double cooling, TestForgeStyle style)
    {
        Center = center;
        Width = width;
        Cooling = cooling;
        Style = style;
    }

    public override IReadOnlyList<RecipeParameterValue> Values => new[]
    {
        new RecipeParameterValue("center", Center),
        new RecipeParameterValue("width", Width),
        new RecipeParameterValue("cooling", Cooling),
        new RecipeParameterValue("style", (int)Style),
    };
}
