using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Skip gates for corpus tests that read optional SWLOR_Haks folders. CI checks out only a
    /// subset of the haks submodule (sw_2da, sw_ability, sw_item, sw_tlk, sw_ui, sw_weapon), so a
    /// fixture that reads any other hak folder must <see cref="Assert.Ignore(string)"/> when that
    /// folder is absent instead of failing on missing files.
    /// </summary>
    internal static class HaksCorpusGuard
    {
        /// <summary>
        /// The repository's SWLOR_Haks directory, found by walking up from the test assembly
        /// location, or null when no checkout is reachable at all.
        /// </summary>
        private static string? TryFindHaksDirectory()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "SWLOR_Haks");
                if (Directory.Exists(candidate))
                    return candidate;

                current = current.Parent;
            }

            return null;
        }

        /// <summary>Ignores the calling test when any of the named SWLOR_Haks folders is absent.</summary>
        internal static void RequireDirectories(params string[] folderNames)
        {
            var haks = TryFindHaksDirectory();
            if (haks == null)
                Assert.Ignore("SWLOR_Haks not available from the test context.");

            foreach (var folderName in folderNames)
            {
                if (!Directory.Exists(Path.Combine(haks!, folderName)))
                    Assert.Ignore($"SWLOR_Haks/{folderName} is not checked out.");
            }
        }

        /// <summary>
        /// Ignores the calling test when no sw_t_* tileset folders are checked out - the gate for
        /// tests that sweep or count the whole tileset corpus rather than one named tileset.
        /// </summary>
        internal static void RequireTilesetCorpus()
        {
            var haks = TryFindHaksDirectory();
            if (haks == null)
                Assert.Ignore("SWLOR_Haks not available from the test context.");

            if (!Directory.EnumerateDirectories(haks!, "sw_t_*").Any())
                Assert.Ignore("SWLOR_Haks sw_t_* tileset folders are not checked out.");
        }
    }
}
