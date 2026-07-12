using System;
using System.IO;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Locates repository-relative paths the same way SWLOR.ProcgenReview does: walk up from the
    /// running executable's directory until SWLOR.Game.Server.sln is found, so the app runs
    /// correctly regardless of machine or drive layout.
    /// </summary>
    internal static class RepoPaths
    {
        public static string Root { get; } = FindRepositoryRoot();

        public static string HaksDirectory => Path.Combine(Root, "SWLOR_Haks");

        public static string ProcgenReviewProjectPath => Path.Combine(Root, "SWLOR.ProcgenReview");

        public static string ReviewModuleOutputPath => Path.Combine(Root, "Module", "SWLOR Procgen Review.mod");

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the repository root (SWLOR.Game.Server.sln).");
        }
    }
}
