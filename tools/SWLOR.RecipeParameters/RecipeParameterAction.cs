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
