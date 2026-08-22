using System.Security.Cryptography;
using System.Text;
using Avalonia.Media.Imaging;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Stores rendered palette previews on disk so a blueprint is only ever rendered once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rendering the whole SWLOR module is roughly 17,000 model parses, texture decodes and rasterizer
    /// passes. Doing that on every launch would be minutes of background CPU for a result that does not
    /// change; a PNG per blueprint under the user's local app data turns it into a one-off.
    /// </para>
    /// <para>
    /// A blueprint with no artwork at all gets an empty marker file instead of a PNG. Without it, the
    /// several thousand placeables whose appearance row is blank would be re-examined - blueprint parse
    /// included - every time a builder opened their category, only to conclude "nothing" again.
    /// </para>
    /// <para>
    /// Entries are invalidated by timestamp: an entry older than either the blueprint or the indexed
    /// game-data content it was rendered from is stale and re-rendered. The latter covers standard
    /// blueprints and transitive model/texture dependencies in HAK or base-game layers. The cache is
    /// keyed by module root as well, because two checkouts have different blueprints under the same
    /// resrefs.
    /// </para>
    /// </remarks>
    public sealed class ThumbnailDiskCache
    {
        /// <summary>
        /// Bumped whenever a change to the render pipeline would produce a different image for the same
        /// input. Superseded versions are not migrated - they are regenerable - but they are deleted by
        /// <see cref="PruneSupersededVersions"/> so the cache does not accumulate a folder per release.
        /// </summary>
        /// <remarks>
        /// <para>
        /// v3: base-game doors resolve a model now that the resolver reads GenericType as well as
        /// GenericType_New. Bumping is what un-sticks them: a blueprint that lives in the base game or a
        /// hak has no file for <see cref="FreshnessThreshold"/> to compare against, so a "no artwork"
        /// marker written by an older pipeline would otherwise be believed forever.
        /// </para>
        /// <para>
        /// v4: the thumbnail camera moved to the front of the model. Every cached image is of a back.
        /// </para>
        /// <para>
        /// v5: waypoints resolve their waypoint.2da model, so their "no artwork" markers are stale.
        /// </para>
        /// <para>
        /// v6-v10: thumbnail camera angle. v6-v8 had it below the model looking up; v9 put it
        /// overhead, v10-v11 eased it back off vertical.
        /// </para>
        /// <para>
        /// v12: standard-DDS textures had their red and blue channels exchanged, so anything with a
        /// real hue was cached the wrong colour - a brown pelt as a blue one. Every thumbnail drawn
        /// from a standard DDS is wrong on disk and has to be drawn again.
        /// </para>
        /// <para>
        /// v13: DDS rows were upside down relative to TGA, so any DDS texture whose top and bottom
        /// differ was sampled inverted. Every preview drawn from one has to be drawn again.
        /// </para>
        /// <para>
        /// v14: creatures stand in their idle pose instead of the bind pose their geometry is stored
        /// in, so every creature thumbnail is a different picture.
        /// </para>
        /// <para>
        /// v15: generic door appearances resolve through genericdoors.2da instead of treating their
        /// GenericType_New value as a doortypes.2da row.
        /// </para>
        /// <para>
        /// v16: creature thumbnails compose visible armor, helmets, cloaks, held weapons, equipment
        /// dyes, cloak texture mappings, and cloak visibility flags.
        /// </para>
        /// </remarks>
        private const string FormatVersion = "v16";

        private const string MissingArtworkExtension = ".none";

        private readonly string? _root;
        private readonly string? _versionsRoot;
        private readonly DateTime _contentVersionUtc;

        /// <param name="moduleRoot">
        /// The module the cached previews belong to. Null disables the cache entirely (previews still
        /// render, they just are not persisted).
        /// </param>
        public ThumbnailDiskCache(string? moduleRoot, DateTime contentVersionUtc = default)
        {
            _contentVersionUtc = contentVersionUtc;
            if (moduleRoot == null)
                return;

            _versionsRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SWLOR.Toolset",
                "previews");
            _root = Path.Combine(_versionsRoot, FormatVersion, KeyFor(moduleRoot));
        }

        /// <summary>Where this cache keeps its files, for the Output log. Null when disabled.</summary>
        public string? RootPath => _root;

        public bool IsEnabled => _root != null;

        /// <summary>What a lookup found for a blueprint.</summary>
        public enum Lookup
        {
            /// <summary>Nothing cached, or what was cached is older than the blueprint.</summary>
            Miss,

            /// <summary>A cached image is available.</summary>
            Image,

            /// <summary>Cached knowledge that this blueprint has no artwork; use the type symbol.</summary>
            NoArtwork
        }

        /// <summary>
        /// Reads a cached preview. Returns <see cref="Lookup.Miss"/> on any I/O or decode failure - a
        /// damaged cache file must cost a re-render, never an error.
        /// </summary>
        public Lookup TryLoad(
            ResourceType type,
            string resRef,
            string? blueprintPath,
            bool useIndexedBlueprint,
            out Bitmap? bitmap,
            IReadOnlyList<string>? dependencyPaths = null)
        {
            bitmap = null;
            if (_root == null)
                return Lookup.Miss;

            var imagePath = PathFor(type, resRef, useIndexedBlueprint, ".png");
            var markerPath = PathFor(type, resRef, useIndexedBlueprint, MissingArtworkExtension);

            try
            {
                var blueprintWrittenAt = FreshnessThreshold(
                    blueprintPath, _contentVersionUtc, dependencyPaths);

                if (File.Exists(markerPath))
                {
                    if (File.GetLastWriteTimeUtc(markerPath) >= blueprintWrittenAt)
                        return Lookup.NoArtwork;
                }
                else if (File.Exists(imagePath))
                {
                    if (File.GetLastWriteTimeUtc(imagePath) >= blueprintWrittenAt)
                    {
                        bitmap = new Bitmap(imagePath);
                        return Lookup.Image;
                    }
                }
            }
            catch (Exception)
            {
                // Unreadable or half-written cache entry: treat as a miss and let it be rewritten.
            }

            return Lookup.Miss;
        }

        /// <summary>True when a usable entry exists, without paying to decode the image.</summary>
        public bool Contains(
            ResourceType type,
            string resRef,
            string? blueprintPath,
            bool useIndexedBlueprint,
            IReadOnlyList<string>? dependencyPaths = null)
        {
            if (_root == null)
                return false;

            try
            {
                var threshold = FreshnessThreshold(
                    blueprintPath, _contentVersionUtc, dependencyPaths);
                foreach (var extension in new[] { ".png", MissingArtworkExtension })
                {
                    var path = PathFor(type, resRef, useIndexedBlueprint, extension);
                    if (File.Exists(path) && File.GetLastWriteTimeUtc(path) >= threshold)
                        return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        /// <summary>Writes a rendered preview. Failures are swallowed: a cache is an optimisation.</summary>
        public void Store(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint,
            Bitmap bitmap)
        {
            if (_root == null)
                return;

            var path = PathFor(type, resRef, useIndexedBlueprint, ".png");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                Delete(PathFor(type, resRef, useIndexedBlueprint, MissingArtworkExtension));

                // Written aside and moved so a crash mid-write cannot leave a truncated PNG behind. The
                // name is unique per write because the background cache build and a palette browsing
                // the same category can both land on one resref at the same moment.
                var temporary = $"{path}.{Environment.CurrentManagedThreadId}.tmp";
                using (var stream = File.Create(temporary))
                    bitmap.Save(stream);

                File.Move(temporary, path, overwrite: true);
            }
            catch (Exception)
            {
                // Out of disk, a locked file, a read-only profile: previews still work this session.
            }
        }

        /// <summary>Records that a blueprint has no artwork, so the next session does not look again.</summary>
        public void StoreNoArtwork(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint)
        {
            if (_root == null)
                return;

            var path = PathFor(type, resRef, useIndexedBlueprint, MissingArtworkExtension);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                Delete(PathFor(type, resRef, useIndexedBlueprint, ".png"));
                File.WriteAllBytes(path, Array.Empty<byte>());
            }
            catch (Exception)
            {
                // Same as Store: losing a marker only costs a repeated lookup.
            }
        }

        /// <summary>
        /// Removes both possible forms of one entry. Used when a saved blueprint changes so an
        /// in-flight or timestamp-equal result cannot survive as stale artwork.
        /// </summary>
        public void Remove(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint)
        {
            if (_root == null)
                return;

            try
            {
                Delete(PathFor(type, resRef, useIndexedBlueprint, ".png"));
                Delete(PathFor(type, resRef, useIndexedBlueprint, MissingArtworkExtension));
            }
            catch (Exception)
            {
                // A cache is an optimisation; a locked entry will fail freshness on a later save.
            }
        }

        /// <summary>
        /// Deletes cache folders written by an older render pipeline, for every module. Returns the number
        /// of folders removed. Those previews can never be served again - the version in their path no
        /// longer matches - so keeping them only costs disk.
        /// </summary>
        public int PruneSupersededVersions()
        {
            if (_versionsRoot == null || !Directory.Exists(_versionsRoot))
                return 0;

            var removed = 0;
            foreach (var directory in Directory.EnumerateDirectories(_versionsRoot))
            {
                if (string.Equals(Path.GetFileName(directory), FormatVersion, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    Directory.Delete(directory, recursive: true);
                    removed++;
                }
                catch (Exception)
                {
                    // A locked file just means the old folder waits for the next launch.
                }
            }

            return removed;
        }

        /// <summary>Deletes every cached preview for this module. Returns the number of files removed.</summary>
        public int Clear()
        {
            if (_root == null || !Directory.Exists(_root))
                return 0;

            var removed = 0;
            foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    removed++;
                }
                catch (Exception)
                {
                    // A preview still open for reading stays; the next build overwrites it.
                }
            }

            return removed;
        }

        /// <summary>
        /// The timestamp a cache entry must beat to count as current. The indexed-content version
        /// remains authoritative when a standard/HAK blueprint has no loose module path.
        /// </summary>
        private static DateTime FreshnessThreshold(
            string? blueprintPath,
            DateTime contentVersionUtc,
            IReadOnlyList<string>? dependencyPaths)
        {
            var threshold = blueprintPath == null || !File.Exists(blueprintPath)
                ? contentVersionUtc
                : Max(contentVersionUtc, File.GetLastWriteTimeUtc(blueprintPath));

            if (dependencyPaths == null)
                return threshold;

            foreach (var dependencyPath in dependencyPaths)
            {
                // The UTC still names this dependency. If it has disappeared, no cache entry can
                // prove it was rendered after the deletion, so force a miss.
                if (!File.Exists(dependencyPath))
                    return DateTime.MaxValue;

                threshold = Max(threshold, File.GetLastWriteTimeUtc(dependencyPath));
            }

            return threshold;
        }

        private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

        private static void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private string PathFor(
            ResourceType type,
            string resRef,
            bool useIndexedBlueprint,
            string extension) =>
            useIndexedBlueprint
                ? Path.Combine(_root!, "standard", type.Extension(), Sanitize(resRef) + extension)
                : Path.Combine(_root!, type.Extension(), Sanitize(resRef) + extension);

        /// <summary>
        /// Resrefs are lowercase alphanumerics and underscores by NWN's own rules, but a hand-edited
        /// module can hold anything - and a resref that escaped into a path separator would write
        /// outside the cache. Anything unexpected is replaced rather than trusted.
        /// </summary>
        private static string Sanitize(string resRef)
        {
            var builder = new StringBuilder(resRef.Length);
            foreach (var character in resRef)
            {
                builder.Append(char.IsAsciiLetterOrDigit(character) || character is '_' or '-'
                    ? char.ToLowerInvariant(character)
                    : '$');
            }

            return builder.Length == 0 ? "$" : builder.ToString();
        }

        /// <summary>A short, stable, path-safe key for a module root.</summary>
        private static string KeyFor(string moduleRoot)
        {
            var normalized = Path.GetFullPath(moduleRoot).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
        }
    }
}
