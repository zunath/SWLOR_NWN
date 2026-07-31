namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Finds every file that still names an item resref, so a rename-on-save can refuse to
    /// delete a blueprint other content points at (a loot table's .AddItem literal, the item
    /// store page, a placed instance) - deleting it would silently break each of them.
    /// </summary>
    /// <remarks>
    /// A raw quoted-literal sweep rather than a semantic index: resrefs only ever appear inside
    /// string quotes in both the module's JSON and the game's C#, while generator inputs are scanned
    /// as delimited resref tokens. A rename is a rare, explicit action where a few seconds of IO is
    /// a fair price for not missing a reference shape no index anticipated. Module folders that
    /// cannot carry item resrefs (area terrain, comments) are skipped; everything else - instances
    /// (git), inventories (utc/utp), stores (utm), dialogs (dlg), scripts (nss) - is swept.
    /// Generated palettes (itp) are deliberately excluded because packing rebuilds them from the
    /// blueprint folders and their stale descriptor must not block the rename that causes that
    /// rebuild.
    /// </remarks>
    public static class ItemReferenceScanner
    {
        private static readonly string[] ModuleFolders = { "git", "utc", "utp", "utm", "dlg", "nss" };

        /// <summary>
        /// Relative display paths of every file referencing <paramref name="resRef"/> as a quoted
        /// string, excluding the blueprint's own file. Module hits come first, then game C# and
        /// generator-input hits.
        /// </summary>
        /// <param name="gameSourceRoot">
        /// The SWLOR.Game.Server project directory, or null/missing to sweep the module only.
        /// </param>
        /// <param name="selfFilePath">The blueprint being renamed - always contains its own resref.</param>
        /// <param name="generatorInputRoot">
        /// The SWLOR.CLI/InputFiles directory, or null/missing when no source checkout is available.
        /// </param>
        public static IReadOnlyList<string> FindReferences(
            string moduleRoot,
            string? gameSourceRoot,
            string resRef,
            string? selfFilePath,
            string? generatorInputRoot = null)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                return Array.Empty<string>();

            var quoted = $"\"{resRef}\"";
            var hits = new List<string>();

            foreach (var folder in ModuleFolders)
                SweepDirectory(Path.Combine(moduleRoot, folder), moduleRoot, quoted, selfFilePath, hits);

            if (!string.IsNullOrWhiteSpace(gameSourceRoot) && Directory.Exists(gameSourceRoot))
                SweepGameSource(gameSourceRoot, quoted, hits);

            if (!string.IsNullOrWhiteSpace(generatorInputRoot) &&
                Directory.Exists(generatorInputRoot))
            {
                SweepGeneratorInputs(generatorInputRoot, resRef, hits);
            }

            return hits;
        }

        private static void SweepDirectory(
            string directory, string moduleRoot, string quoted, string? selfFilePath, List<string> hits)
        {
            if (!Directory.Exists(directory))
                return;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                // Fail closed: a folder this scan cannot even enumerate may hold a reference, and
                // "could not check" must block the rename the same way "found one" does.
                hits.Add("Module/" + Path.GetFileName(directory) + " (unscannable — treated as a reference)");
                return;
            }

            foreach (var file in files)
            {
                if (selfFilePath != null &&
                    string.Equals(Path.GetFullPath(file), Path.GetFullPath(selfFilePath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var display = "Module/" + Path.GetRelativePath(moduleRoot, file).Replace('\\', '/');
                switch (FileContains(file, quoted))
                {
                    case true:
                        hits.Add(display);
                        break;
                    case null:
                        hits.Add(display + " (unreadable — treated as a reference)");
                        break;
                }
            }
        }

        private static void SweepGameSource(string gameSourceRoot, string quoted, List<string> hits)
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(gameSourceRoot, "*.cs", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                // Same fail-closed rule as the module sweep: an unenumerable source tree blocks
                // the rename instead of silently vouching for it.
                hits.Add("SWLOR.Game.Server (unscannable — treated as a reference)");
                return;
            }

            foreach (var file in files)
            {
                // Same nested-worktree guard as ItemObtainabilityIndex: a stray checkout inside
                // the scanned tree must not report phantom references.
                var relative = Path.GetRelativePath(gameSourceRoot, file).Replace('\\', '/');
                if (relative.Contains(".claude/worktrees/"))
                    continue;

                switch (FileContains(file, quoted))
                {
                    case true:
                        hits.Add("SWLOR.Game.Server/" + relative);
                        break;
                    case null:
                        hits.Add("SWLOR.Game.Server/" + relative + " (unreadable — treated as a reference)");
                        break;
                }
            }
        }

        private static void SweepGeneratorInputs(
            string generatorInputRoot,
            string resRef,
            List<string> hits)
        {
            IReadOnlyList<string> files;
            try
            {
                files = Directory
                    .EnumerateFiles(generatorInputRoot, "*", SearchOption.AllDirectories)
                    .ToList();
            }
            catch (Exception)
            {
                hits.Add("SWLOR.CLI/InputFiles (unscannable — treated as a reference)");
                return;
            }

            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(generatorInputRoot, file).Replace('\\', '/');
                switch (FileContainsResRefToken(file, resRef))
                {
                    case true:
                        hits.Add("SWLOR.CLI/InputFiles/" + relative);
                        break;
                    case null:
                        hits.Add(
                            "SWLOR.CLI/InputFiles/" + relative +
                            " (unreadable — treated as a reference)");
                        break;
                }
            }
        }

        /// <summary>
        /// Null when the file cannot be read. This scan exists to stop a rename from deleting a
        /// blueprint something still points at, so an unreadable candidate must fail closed - it
        /// counts as a reference and blocks the rename - rather than silently passing as "no match".
        /// </summary>
        private static bool? FileContains(string file, string quoted)
        {
            try
            {
                return File.ReadAllText(file).Contains(quoted, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool? FileContainsResRefToken(string file, string resRef)
        {
            try
            {
                var text = File.ReadAllText(file);
                var searchFrom = 0;
                while (searchFrom < text.Length)
                {
                    var index = text.IndexOf(
                        resRef,
                        searchFrom,
                        StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                        return false;

                    var beforeIsResRef = index > 0 && IsResRefCharacter(text[index - 1]);
                    var after = index + resRef.Length;
                    var afterIsResRef = after < text.Length && IsResRefCharacter(text[after]);
                    if (!beforeIsResRef && !afterIsResRef)
                        return true;

                    searchFrom = index + resRef.Length;
                }

                return false;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsResRefCharacter(char value) =>
            char.IsAsciiLetterOrDigit(value) || value == '_';
    }
}
