using SWLOR.Game.Server.CharacterizationTests.Definitions;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Records what a call returned, or that it threw, as one text cell. A snapshot of a function that rejects some
/// inputs then locks the rejection as well as the results. Messages are recorded only for a plain
/// <see cref="Exception"/> (the game throws those with its own text); framework exceptions are recorded by type and,
/// for argument exceptions, parameter name, because the framework text can change between runtime versions.
/// </summary>
internal static class Outcome
{
    public static string Of<T>(Func<T> function)
    {
        try
        {
            return DefinitionCell.Text(function());
        }
        catch (Exception exception)
        {
            return Describe(exception);
        }
    }

    /// <summary>The value itself (so a number stays a number in the snapshot), or the text of the exception it threw.</summary>
    public static object? Value<T>(Func<T> function)
    {
        try
        {
            return function();
        }
        catch (Exception exception)
        {
            return Describe(exception);
        }
    }

    public static string Describe(Exception exception)
    {
        if (exception is System.Reflection.TargetInvocationException { InnerException: { } inner })
            return Describe(inner);

        return exception switch
        {
            ArgumentException argument => $"throws {argument.GetType().Name}({argument.ParamName})",
            _ when exception.GetType() == typeof(Exception) => $"throws Exception: {exception.Message}",
            _ => $"throws {exception.GetType().Name}"
        };
    }
}
