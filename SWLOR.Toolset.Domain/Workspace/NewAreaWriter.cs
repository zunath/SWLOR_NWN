using System.Text.RegularExpressions;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Creates a new area on disk (the WP7.3 new-area wizard's action): clones the module's area
    /// template triplet under a new resref, reshapes the .are into a solid fill of the chosen
    /// tileset/terrain, and registers the area in module.ifo. The .git/.gic halves are copied
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

        /// <summary>
        /// Resolves a tileset by resref. The app passes its TilesetCatalog's lookup; tests pass a
        /// stub. Keeping this a delegate leaves Domain free of the catalog's resource-index plumbing.
        /// </summary>
        public delegate bool TilesetResolver(string resRef, out TilesetDefinition tileset);

        /// <summary>
        /// Creates the area, returning false with a human-readable <paramref name="error"/> if
        /// anything is invalid or the write fails. Every check runs before the first write, so a
        /// rejected request leaves the module untouched.
        /// </summary>
        public static bool TryCreate(
            ModuleWorkspace workspace,
            TilesetResolver? resolveTileset,
            string resRef, string displayName, string tilesetResRef,
            int width, int height, string? terrain,
            out string error)
        {
            ArgumentNullException.ThrowIfNull(workspace);
            error = string.Empty;

            resRef = (resRef ?? string.Empty).Trim().ToLowerInvariant();
            if (!ResRefPattern.IsMatch(resRef))
            {
                error = "Resref must be 1-16 characters, lowercase letters/digits/underscore only.";
                return false;
            }

            if (width is < 1 or > MaxDimension || height is < 1 or > MaxDimension)
            {
                error = $"Width and height must each be between 1 and {MaxDimension}.";
                return false;
            }

            var arePath = workspace.GetResourcePath(ResourceType.Area, resRef);
            if (File.Exists(arePath))
            {
                error = $"An area named '{resRef}' already exists.";
                return false;
            }

            TilesetDefinition? tileset = null;
            if (resolveTileset != null && resolveTileset(tilesetResRef, out var resolved))
                tileset = resolved;

            if (tileset == null)
            {
                error = $"Tileset '{tilesetResRef}' could not be resolved.";
                return false;
            }

            // Fill with a solid, crosser-free tile of the chosen terrain so the new area is a plain
            // walkable floor rather than an arbitrary tile that might be a wall or a pit.
            var fillTerrain = string.IsNullOrWhiteSpace(terrain) ? TilePainter.DefaultFillTerrain(tileset) : terrain;
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
                var are = AreDocument.Load(templateAre);
                AreaTemplateFactory.PopulateNewArea(
                    are, resRef, displayName, tilesetResRef, width, height, fill.TileId, fill.Orientation);

                var ifo = IfoDocument.Load(ifoPath);
                AreaTemplateFactory.AddAreaToModule(ifo, resRef);

                // Write the area triplet first, then the module index: an orphaned area file is
                // harmless and the create is re-runnable, whereas an index entry pointing at a
                // missing area would break module load.
                WriteAtomic(arePath, are.ToBytes());
                File.Copy(templateGit, GitPath(workspace, resRef), overwrite: false);
                File.Copy(templateGic, GicPath(workspace, resRef), overwrite: false);
                WriteAtomic(ifoPath, ifo.ToBytes());
                return true;
            }
            catch (Exception ex)
            {
                error = $"Failed to create area '{resRef}': {ex.Message}";
                return false;
            }
        }

        private static string GitPath(ModuleWorkspace workspace, string resRef) =>
            Path.Combine(workspace.ModuleRoot, "git", resRef + ".git.json");

        private static string GicPath(ModuleWorkspace workspace, string resRef) =>
            Path.Combine(workspace.ModuleRoot, "gic", resRef + ".gic.json");

        private static void WriteAtomic(string path, byte[] bytes)
        {
            var temporaryPath = path + ".tmp";
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, path, overwrite: true);
        }
    }
}
