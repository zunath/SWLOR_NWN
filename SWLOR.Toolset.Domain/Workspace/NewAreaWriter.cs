using System.Text.RegularExpressions;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Creates a new area on disk (the new-area wizard's action): clones the module's area
    /// template triplet under a new resref, reshapes the .are into a solid fill of the chosen
    /// tileset's own floor terrain, and registers the area in module.ifo. The .git/.gic halves are copied
    /// byte-for-byte - they are generic empty instance lists with nothing area-specific in them, so
    /// copying is both simpler and safer than re-serializing.
    ///
    /// Lives in Domain rather than the app layer because it is pure file/document work: that keeps
    /// it headlessly testable, and the app contributes only the wizard UI.
    /// </summary>
    public static class NewAreaWriter
    {
        /// <summary>The module resref whose are/git/gic triplet new areas are cloned from.</summary>
        public const string TemplateResRef = "area_template";

        /// <summary>The largest area NWN accepts on a side.</summary>
        public const int MaxDimension = 32;

        /// <summary>NWN resource names are at most 16 characters, lowercase, alphanumeric/underscore.</summary>
        private static readonly Regex ResRefPattern = new("^[a-z0-9_]{1,16}$", RegexOptions.Compiled);
        private const string PendingMarkerPrefix = ".swlor-toolset-new-area-";

        /// <summary>
        /// Resolves a tileset by resref. The app passes its TilesetCatalog's lookup; tests pass a
        /// stub. Keeping this a delegate leaves Domain free of the catalog's resource-index plumbing.
        /// </summary>
        public delegate bool TilesetResolver(string resRef, out TilesetDefinition tileset);

        /// <summary>
        /// Creates the area, returning false with a human-readable <paramref name="error"/> if
        /// anything is invalid or the write fails. Every check runs before the first write, so a
        /// rejected request leaves the module untouched.
        ///
        /// There is deliberately no terrain parameter: a terrain is not a property of an area (an
        /// area uses many terrains at once - that is what the paint tool is for), and every .set
        /// already declares what a blank area of that tileset is made of via [GENERAL] Floor/Default.
        /// Terrain selection belongs solely to the paint palette.
        /// </summary>
        public static bool TryCreate(
            ModuleWorkspace workspace,
            TilesetResolver? resolveTileset,
            string resRef, string displayName, string tilesetResRef,
            int width, int height,
            out string error)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            error = string.Empty;

            resRef = (resRef ?? string.Empty).Trim().ToLowerInvariant();
            if (!ResRefPattern.IsMatch(resRef))
            {
                error = "ResRef must be 1-16 characters, lowercase letters/digits/underscore only.";
                return false;
            }

            if (width is < 1 or > MaxDimension || height is < 1 or > MaxDimension)
            {
                error = $"Width and height must each be between 1 and {MaxDimension}.";
                return false;
            }

            var arePath = workspace.GetResourcePath(ResourceType.Area, resRef);
            var gitPath = GitPath(workspace, resRef);
            var gicPath = GicPath(workspace, resRef);
            var markerPath = PendingMarkerPath(workspace, resRef);

            // Ordinary duplicates are rejected before any expensive tileset/template work. A pending
            // marker is the one exception: it identifies writer-owned remnants that recovery below
            // may safely remove after the rest of the request is validated.
            if (!File.Exists(markerPath))
            {
                var existing = new[] { arePath, gitPath, gicPath }.FirstOrDefault(File.Exists);
                if (existing != null)
                {
                    error =
                        $"An area named '{resRef}' already exists ({Path.GetFileName(existing)} is present).";
                    return false;
                }
            }

            TilesetDefinition? tileset = null;
            if (resolveTileset != null && resolveTileset(tilesetResRef, out var resolved))
                tileset = resolved;

            if (tileset == null)
            {
                error = $"Tileset '{tilesetResRef}' could not be resolved.";
                return false;
            }

            // Fill with a solid, crosser-free tile of the tileset's own declared fill terrain
            // (DefaultFillTerrain resolves [GENERAL] Floor, then Default, then the first fillable
            // terrain) rather than an arbitrary legal tile.
            //
            // NOTE: "Floor" is the tileset's DEFAULT FILL, not necessarily walkable ground. Exterior
            // tilesets declare walkable ground (tms01 Floor=Grass, ztd01 Floor=Desert), but interior
            // ones declare solid rock (tib01 Floor=Wall, with Room being the walkable terrain). A new
            // interior area therefore starts as solid fill that the builder carves rooms out of by
            // painting - which is exactly how the Aurora toolset behaves, so this is faithful.
            var fillTerrain = TilePainter.DefaultFillTerrain(tileset);
            if (fillTerrain == null || TilePainter.FindSolidTile(tileset, fillTerrain) is not { } fill)
            {
                error = $"Tileset '{tilesetResRef}' has no solid tile for terrain '{fillTerrain ?? "(none)"}'.";
                return false;
            }

            var templateAre = workspace.GetResourcePath(ResourceType.Area, TemplateResRef);
            var templateGit = GitPath(workspace, TemplateResRef);
            var templateGic = GicPath(workspace, TemplateResRef);
            if (!File.Exists(templateAre) || !File.Exists(templateGit) || !File.Exists(templateGic))
            {
                error = $"The '{TemplateResRef}' template triplet is missing from the module.";
                return false;
            }

            var ifoPath = Path.Combine(workspace.ModuleRoot, "ifo", "module.ifo.json");
            if (!File.Exists(ifoPath))
            {
                error = "module.ifo.json was not found; cannot register the new area.";
                return false;
            }

            try
            {
                if (File.Exists(markerPath))
                {
                    var registered = IfoDocument.Load(ifoPath).AreaResRefs
                        .Contains(resRef, StringComparer.OrdinalIgnoreCase);

                    if (!registered)
                    {
                        // A marker means this writer owned the incomplete triplet. Removing only
                        // those exact destinations makes a power-loss retry safe.
                        foreach (var path in new[] { arePath, gitPath, gicPath })
                        {
                            if (File.Exists(path))
                                File.Delete(path);
                        }
                    }

                    File.Delete(markerPath);
                }
            }
            catch (Exception ex)
            {
                error = $"Could not recover interrupted area creation for '{resRef}': {ex.Message}";
                return false;
            }

            var existingDestination = new[] { arePath, gitPath, gicPath }.FirstOrDefault(File.Exists);
            if (existingDestination != null)
            {
                error = $"An area named '{resRef}' already exists ({Path.GetFileName(existingDestination)} is present).";
                return false;
            }

            byte[] ifoBaseline;
            try
            {
                ifoBaseline = File.ReadAllBytes(ifoPath);
            }
            catch (Exception ex)
            {
                error = $"Could not read module.ifo.json before creating '{resRef}': {ex.Message}";
                return false;
            }

            var createdPaths = new List<string>();
            var markerCreated = false;
            try
            {
                var are = AreDocument.Load(templateAre);
                // These are standalone documents loaded specifically for this write, but another
                // editor's DocumentSession may still have the ambient mutation guard enabled on
                // the UI context. Give each document its own short-lived session/transaction so
                // its mutations are explicit and cannot be captured by an unrelated editor.
                using (var areSession = new DocumentSession(arePath, are.Document))
                using (areSession.Begin($"Create area '{resRef}'"))
                {
                    AreaTemplateFactory.PopulateNewArea(
                        are, resRef, displayName, tilesetResRef, width, height, fill.TileId, fill.Orientation);
                }

                var ifo = IfoDocument.Parse(ifoBaseline);
                using (var ifoSession = new DocumentSession(ifoPath, ifo.Document))
                using (ifoSession.Begin($"Register area '{resRef}'"))
                    AreaTemplateFactory.AddAreaToModule(ifo, resRef);

                // Persist intent before the first destination write. If the process or machine stops
                // before module.ifo is committed, the next retry recognizes and removes only this
                // writer's partial triplet.
                WriteAtomic(markerPath, System.Text.Encoding.UTF8.GetBytes(resRef), overwrite: true);
                markerCreated = true;

                // Write the area triplet first, then the module index: the marker makes an orphaned
                // partial triplet recoverable, while an index entry pointing at missing files would
                // break module load.
                WriteAtomic(arePath, are.ToBytes(), overwrite: false);
                createdPaths.Add(arePath);
                // Destinations are recorded BEFORE each copy: File.Copy can create the file and
                // then fail partway (full disk, I/O error), and an untracked partial GIT/GIC would
                // survive cleanup, permanently blocking the resref as "already exists". A
                // destination that already exists is NOT recorded - the copy will refuse it, and
                // cleanup must never delete a file this writer did not create.
                if (!File.Exists(gitPath))
                    createdPaths.Add(gitPath);
                File.Copy(templateGit, gitPath, overwrite: false);
                if (!File.Exists(gicPath))
                    createdPaths.Add(gicPath);
                File.Copy(templateGic, gicPath, overwrite: false);

                var currentIfo = File.ReadAllBytes(ifoPath);
                if (!currentIfo.AsSpan().SequenceEqual(ifoBaseline))
                {
                    throw new IOException(
                        "module.ifo.json changed while the area was being created. Try again.");
                }

                WriteAtomic(ifoPath, ifo.ToBytes(), overwrite: true);
                try
                {
                    File.Delete(markerPath);
                }
                catch
                {
                    // The area and its registration are committed. A stale marker is harmless:
                    // the next attempt sees the IFO entry and removes the marker without touching
                    // the completed triplet.
                }
                return true;
            }
            catch (Exception ex)
            {
                var cleanupComplete = true;
                for (var index = createdPaths.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        File.Delete(createdPaths[index]);
                    }
                    catch
                    {
                        cleanupComplete = false;
                        // Preserve the original failure. The error below still identifies the
                        // create as failed, and a later retry's destination preflight will name
                        // any path that could not be rolled back.
                    }
                }

                if (markerCreated && cleanupComplete)
                {
                    try
                    {
                        File.Delete(markerPath);
                    }
                    catch
                    {
                        // Keeping the marker is safe: the next retry performs the same exact cleanup.
                    }
                }

                error = $"Failed to create area '{resRef}': {ex.Message}";
                return false;
            }
        }

        private static string GitPath(ModuleWorkspace workspace, string resRef) =>
            Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json");

        private static string GicPath(ModuleWorkspace workspace, string resRef) =>
            Path.Combine(workspace.ModuleRoot, "gic", resRef + ".gic.json");

        private static string PendingMarkerPath(ModuleWorkspace workspace, string resRef) =>
            Path.Combine(workspace.ModuleRoot, PendingMarkerPrefix + resRef + ".pending");

        private static void WriteAtomic(string path, byte[] bytes, bool overwrite)
        {
            var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, bytes);
                File.Move(temporaryPath, path, overwrite);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
