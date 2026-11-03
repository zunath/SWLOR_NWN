using SWLOR.Game.Server;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Builds <see cref="ApplicationSettings"/> for a test. The server reads its settings once from the process environment
/// and caches them (<c>ApplicationSettings.Get</c>); a service whose settings a test must control reads them through a
/// <c>protected virtual</c> method, and the test answers one of these.
/// </summary>
public static class TestSettings
{
    /// <summary>
    /// Settings as the server would read them with <c>SWLOR_ENVIRONMENT</c> set to <paramref name="environment"/> (null for
    /// unset, which resolves to a non-explicit Development) and <c>SWLOR_APP_LOG_DIRECTORY</c> set to <paramref name="logDirectory"/>.
    /// The process variables are restored before this returns.
    /// </summary>
    public static ApplicationSettings Create(string? environment = null, string? logDirectory = null)
    {
        var names = new[] { "SWLOR_ENVIRONMENT", "SWLOR_APP_LOG_DIRECTORY" };
        var previous = names.Select(Environment.GetEnvironmentVariable).ToArray();
        try
        {
            Environment.SetEnvironmentVariable("SWLOR_ENVIRONMENT", environment);
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", logDirectory);
            return (ApplicationSettings)Activator.CreateInstance(typeof(ApplicationSettings), nonPublic: true)!;
        }
        finally
        {
            for (var index = 0; index < names.Length; index++)
                Environment.SetEnvironmentVariable(names[index], previous[index]);
        }
    }
}
