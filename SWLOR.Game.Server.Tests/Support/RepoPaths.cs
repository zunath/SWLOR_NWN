using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Shared helper for locating the SWLOR_NWN repository root from a test's working directory.
/// Consolidates the many near-identical private FindRepositoryRoot() copies that used to live
/// in individual test files.
/// </summary>
internal static class RepoPaths
{
    /// <summary>
    /// Walks up from the NUnit test directory until it finds the directory containing
    /// SWLOR.Game.Server.sln, and returns that directory.
    /// </summary>
    public static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    /// <summary>
    /// Same as <see cref="FindRepositoryRoot"/>, but returns the full path as a string for
    /// call sites that previously used a string-returning FindRepositoryRoot() copy.
    /// </summary>
    public static string FindRepositoryRootPath()
    {
        return FindRepositoryRoot().FullName;
    }
}
