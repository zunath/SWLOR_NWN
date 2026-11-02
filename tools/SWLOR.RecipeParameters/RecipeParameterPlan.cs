namespace SWLOR.RecipeParameters;

/// <summary>
/// What the writer should do with one recipe's source. <see cref="Arguments"/> is the argument list of
/// the builder call, such as <c>center: 50, width: 20</c>.
/// </summary>
public sealed class RecipeParameterPlan
{
    public static readonly RecipeParameterPlan Leave = new(RecipeParameterAction.Leave, null, null);

    public RecipeParameterAction Action { get; }
    public string BuilderMethod { get; }
    public string Arguments { get; }

    private RecipeParameterPlan(RecipeParameterAction action, string builderMethod, string arguments)
    {
        Action = action;
        BuilderMethod = builderMethod;
        Arguments = arguments;
    }

    public static RecipeParameterPlan Write(string builderMethod, string arguments)
    {
        return new RecipeParameterPlan(RecipeParameterAction.Write, builderMethod, arguments);
    }

    public static RecipeParameterPlan Remove(string builderMethod)
    {
        return new RecipeParameterPlan(RecipeParameterAction.Remove, builderMethod, null);
    }
}
