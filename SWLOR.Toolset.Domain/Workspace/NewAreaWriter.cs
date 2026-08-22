using System.Security.Cryptography;
using System.Text.Json;
using SWLOR.NWN.Formats.Common;
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
            if (!NwnResRef.IsCanonical(resRef))
            {
                error =
                    $"ResRef must be 1-{NwnResRef.MaxLength} characters, " +
                    "lowercase letters/digits/underscore only.";
                return false;
            }

            if (width is < 1 or > MaxDimension || height is < 1 or > MaxDimension)
            {
                error = $"Width and height must each be between 1 and {MaxDimension}.";
                return false;
            }

            ModuleWriteLock moduleWriteLock;
            try
            {
                moduleWriteLock = ModuleWriteLock.Acquire(workspace.ModuleRoot);
            }
            catch (Exception ex)
            {
                error = $"Could not reserve the module while creating '{resRef}': {ex.Message}";
                return false;
            }
            using var heldModuleWriteLock = moduleWriteLock;

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

            ModuleIfoUpdateLock ifoUpdateLock;
            try
            {
                ifoUpdateLock = ModuleIfoUpdateLock.Acquire(workspace.ModuleRoot);
            }
            catch (Exception ex)
            {
                error = $"Could not reserve module.ifo.json while creating '{resRef}': {ex.Message}";
                return false;
            }
            using var heldIfoUpdateLock = ifoUpdateLock;

            try
            {
                if (File.Exists(markerPath))
                {
                    var registered = IfoDocument.Load(ifoPath).AreaResRefs
                        .Contains(resRef, StringComparer.OrdinalIgnoreCase);

                    if (!registered)
                    {
                        var pending = ReadPendingManifest(markerPath, resRef);
                        var destinations = PendingDestinations(
                            arePath, gitPath, gicPath, pending);
                        ValidatePendingDestinations(destinations);
                        foreach (var (path, _) in destinations)
                            if (File.Exists(path))
                                File.Delete(path);
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
            PendingAreaManifest? pendingManifest = null;
            try
            {
                var are = AreDocument.Load(templateAre);
                var templateGitBytes = File.ReadAllBytes(templateGit);
                var templateGicBytes = File.ReadAllBytes(templateGic);
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

                var areBytes = are.ToBytes();
                pendingManifest = new PendingAreaManifest
                {
                    ResRef = resRef,
                    Are = Fingerprint(areBytes),
                    Git = Fingerprint(templateGitBytes),
                    Gic = Fingerprint(templateGicBytes)
                };

                // Persist intent before the first destination write. If the process or machine stops
                // before module.ifo is committed, the next retry recognizes and removes only this
                // writer's partial triplet.
                WriteAtomic(
                    markerPath,
                    JsonSerializer.SerializeToUtf8Bytes(pendingManifest),
                    overwrite: true);
                markerCreated = true;

                // Write the area triplet first, then the module index: the marker makes an orphaned
                // partial triplet recoverable, while an index entry pointing at missing files would
                // break module load.
                WriteAtomic(arePath, areBytes, overwrite: false);
                createdPaths.Add(arePath);
                // Stage each companion beside its destination, then install it atomically without
                // overwrite. Ownership begins only after the move succeeds: if another editor wins
                // the destination race, rollback must not delete that editor's file.
                WriteAtomic(gitPath, templateGitBytes, overwrite: false);
                createdPaths.Add(gitPath);
                WriteAtomic(gicPath, templateGicBytes, overwrite: false);
                createdPaths.Add(gicPath);

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
                try
                {
                    if (pendingManifest != null)
                    {
                        var destinations = PendingDestinations(
                                arePath, gitPath, gicPath, pendingManifest)
                            .Where(item => createdPaths.Contains(
                                item.Path,
                                StringComparer.OrdinalIgnoreCase))
                            .ToList();
                        ValidatePendingDestinations(destinations);
                    }

                    for (var index = createdPaths.Count - 1; index >= 0; index--)
                        File.Delete(createdPaths[index]);
                }
                catch
                {
                    cleanupComplete = false;
                    // Preserve the original failure. A changed destination and the marker remain
                    // so a later recovery cannot mistake somebody else's file for writer-owned data.
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

        private static PendingAreaManifest ReadPendingManifest(
            string markerPath,
            string expectedResRef)
        {
            var manifest = JsonSerializer.Deserialize<PendingAreaManifest>(
                File.ReadAllBytes(markerPath));
            if (manifest == null ||
                !string.Equals(manifest.ResRef, expectedResRef, StringComparison.OrdinalIgnoreCase) ||
                !IsValidFingerprint(manifest.Are) ||
                !IsValidFingerprint(manifest.Git) ||
                !IsValidFingerprint(manifest.Gic))
            {
                throw new InvalidDataException(
                    $"The pending marker '{Path.GetFileName(markerPath)}' is incomplete.");
            }

            return manifest;
        }

        private static List<(string Path, AreaFileFingerprint Fingerprint)> PendingDestinations(
            string arePath,
            string gitPath,
            string gicPath,
            PendingAreaManifest manifest) =>
            new()
            {
                (arePath, manifest.Are),
                (gitPath, manifest.Git),
                (gicPath, manifest.Gic)
            };

        private static void ValidatePendingDestinations(
            IEnumerable<(string Path, AreaFileFingerprint Fingerprint)> destinations)
        {
            foreach (var (path, expected) in destinations)
            {
                if (!File.Exists(path))
                    continue;

                var actual = Fingerprint(File.ReadAllBytes(path));
                if (actual != expected)
                {
                    throw new IOException(
                        $"'{Path.GetFileName(path)}' changed after the interrupted area creation. " +
                        "Recovery was refused so the newer file is preserved.");
                }
            }
        }

        private static AreaFileFingerprint Fingerprint(byte[] content) =>
            new(content.LongLength, Convert.ToHexString(SHA256.HashData(content)));

        private static bool IsValidFingerprint(AreaFileFingerprint? fingerprint) =>
            fingerprint != null &&
            fingerprint.Length >= 0 &&
            fingerprint.Sha256.Length == 64 &&
            fingerprint.Sha256.All(character =>
                character is >= '0' and <= '9' or >= 'A' and <= 'F');

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

        private sealed class PendingAreaManifest
        {
            public string ResRef { get; set; } = string.Empty;
            public AreaFileFingerprint Are { get; set; } = new();
            public AreaFileFingerprint Git { get; set; } = new();
            public AreaFileFingerprint Gic { get; set; } = new();
        }

        private sealed record AreaFileFingerprint(long Length = -1, string Sha256 = "");
    }
}
