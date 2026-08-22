namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Locates the repository's Module directory from the test execution context by walking
    /// up from the test assembly location.
    /// </summary>
    public static class CorpusLocator
    {
        public static readonly string[] GffFolders =
        {
            "are", "dlg", "fac", "gic", "git", "ifo", "itp", "jrl",
            "utc", "utd", "uti", "utm", "utp", "uts", "utt", "utw"
        };

        /// <summary>The repository root, found by walking up to the folder that contains Module/are.</summary>
        public static string RepositoryRoot =>
            Path.GetFullPath(Path.Combine(ModuleDirectory, ".."));

        public static string ModuleDirectory
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "Module");
                    if (Directory.Exists(Path.Combine(candidate, "are")))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository Module directory from the test context.");
            }
        }

        public static IEnumerable<string> EnumerateGffJsonFiles()
        {
            var moduleDirectory = ModuleDirectory;
            foreach (var folder in GffFolders)
            {
                var path = Path.Combine(moduleDirectory, folder);
                if (!Directory.Exists(path))
                    continue;

                foreach (var file in Directory.EnumerateFiles(path, "*.json"))
                    yield return file;
            }
        }
    }
}
