namespace SWLOR.CraftSim.Data
{
    /// <summary>Finds the repository folders the simulator reads game data from.</summary>
    public static class RepositoryPaths
    {
        private const string SolutionFile = "SWLOR.Game.Server.sln";

        /// <summary>The repository root, found by walking up from <paramref name="start"/> (the program folder by default).</summary>
        public static string FindRoot(string start = null)
        {
            var directory = new DirectoryInfo(start ?? AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, SolutionFile)))
                directory = directory.Parent;

            return directory?.FullName ?? throw new DirectoryNotFoundException(
                $"Cannot find {SolutionFile} above {start ?? AppContext.BaseDirectory}. Run the simulator from the repository or pass --repo.");
        }

        public static string ModuleItems(string root) => Path.Combine(root, "Module", "uti");

        public static string ServerSource(string root) => Path.Combine(root, "SWLOR.Game.Server");

        public static string HakTwoDa(string root) => Path.Combine(root, "SWLOR_Haks", "sw_2da");
    }
}
