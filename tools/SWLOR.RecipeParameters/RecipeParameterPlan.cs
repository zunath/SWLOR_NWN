namespace SWLOR.RecipeParameters;

public enum RecipeParameterAction
{
    /// <summary>The recipe belongs to another minigame; its source is not touched.</summary>
    Leave,

    /// <summary>The recipe needs the call written or brought up to date.</summary>
    Write,

    /// <summary>The recipe no longer takes parameters; an existing call is deleted.</summary>
    Remove,
}

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
