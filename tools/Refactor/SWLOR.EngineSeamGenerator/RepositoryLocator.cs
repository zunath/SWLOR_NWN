namespace SWLOR.EngineSeamGenerator;

/// <summary>Finds the repository root by walking up to the folder that holds the solution file.</summary>
internal static class RepositoryLocator
{
    public static string? Find(string start)
    {
        var directory = new DirectoryInfo(start);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
