namespace SWLOR.Game.Server.Tests;

public static class CombatSourceReader
{
    public static string Read(DirectoryInfo repositoryRoot)
    {
        return Read(repositoryRoot.FullName);
    }

    public static string Read(string repositoryRoot)
    {
        var serviceRoot = Path.Combine(repositoryRoot, "SWLOR.Game.Server", "Service", "CombatService");
        var files = Directory.EnumerateFiles(serviceRoot, "*.cs").OrderBy(x => x);

        return string.Join("\n", files.Select(file => File.ReadAllText(file)));
    }
}
