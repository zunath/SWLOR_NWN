using System.Security.Cryptography;
using System.Text;
using Avalonia.Media.Imaging;
using Nwn.Preview.Cache;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>Persists rendered previews using shared atomic, bounded storage.</summary>
    public sealed class ThumbnailDiskCache
    {
        private const string FormatVersion = "v18";
        private const long MaximumModuleCacheBytes = 1024L * 1024 * 1024;
        private readonly string? _root;
        private readonly string? _versionsRoot;
        private readonly DateTime _contentVersionUtc;
        private readonly PersistentPreviewCache? _cache;

        public ThumbnailDiskCache(string? moduleRoot, DateTime contentVersionUtc = default)
        {
            _contentVersionUtc = contentVersionUtc;
            if (moduleRoot == null)
                return;
            _versionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SWLOR.Toolset", "previews");
            _root = Path.Combine(_versionsRoot, FormatVersion, KeyFor(moduleRoot));
            _cache = new PersistentPreviewCache(_root, FormatVersion, MaximumModuleCacheBytes,
                Path.Combine(_versionsRoot, FormatVersion));
        }

        public string? RootPath => _root;
        public bool IsEnabled => _cache is not null;

        public enum Lookup { Miss, Image, NoArtwork }

        public Lookup TryLoad(ResourceType type, string resRef, string? blueprintPath,
            bool useIndexedBlueprint, out Bitmap? bitmap, IReadOnlyList<string>? dependencyPaths = null)
        {
            bitmap = null;
            if (_cache is null)
                return Lookup.Miss;
            try
            {
                var key = Key(type, resRef, useIndexedBlueprint);
                var threshold = FreshnessThreshold(blueprintPath, dependencyPaths);
                if (_cache.LastWrittenUtc(key) is not { } written || written < threshold ||
                    !_cache.TryRead(key, FormatVersion, out var payload, out var hasImage))
                    return Lookup.Miss;
                if (!hasImage)
                    return Lookup.NoArtwork;
                using var image = new MemoryStream(payload!, writable: false);
                bitmap = new Bitmap(image);
                return Lookup.Image;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                               ArgumentException or InvalidDataException)
            {
                return Lookup.Miss;
            }
        }

        public bool Contains(ResourceType type, string resRef, string? blueprintPath,
            bool useIndexedBlueprint, IReadOnlyList<string>? dependencyPaths = null)
        {
            if (_cache is null)
                return false;
            var key = Key(type, resRef, useIndexedBlueprint);
            return _cache.Contains(key, FormatVersion) &&
                   _cache.LastWrittenUtc(key) is { } written &&
                   written >= FreshnessThreshold(blueprintPath, dependencyPaths);
        }

        public void Store(ResourceType type, string resRef, bool useIndexedBlueprint, Bitmap bitmap)
        {
            ArgumentNullException.ThrowIfNull(bitmap);
            if (_cache is null)
                return;
            try
            {
                using var stream = new MemoryStream();
                bitmap.Save(stream);
                _cache.Write(Key(type, resRef, useIndexedBlueprint), FormatVersion,
                    stream.GetBuffer().AsSpan(0, checked((int)stream.Length)), hasImage: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A cache write failure only costs a later re-render.
            }
        }

        public void StoreNoArtwork(ResourceType type, string resRef, bool useIndexedBlueprint)
        {
            if (_cache is null)
                return;
            try
            {
                _cache.Write(Key(type, resRef, useIndexedBlueprint), FormatVersion, [], hasImage: false);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Same as Store: a failed write only costs a later re-render, and throwing here would
                // fault the render before its waiting palette tiles are released.
            }
        }

        public void Remove(ResourceType type, string resRef, bool useIndexedBlueprint) =>
            _cache?.Remove(Key(type, resRef, useIndexedBlueprint));

        public int PruneSupersededVersions()
        {
            if (_versionsRoot is null || !Directory.Exists(_versionsRoot))
                return 0;
            var removed = 0;
            foreach (var directory in Directory.EnumerateDirectories(_versionsRoot))
            {
                if (string.Equals(Path.GetFileName(directory), FormatVersion, StringComparison.OrdinalIgnoreCase))
                    continue;
                try { Directory.Delete(directory, recursive: true); removed++; }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return removed;
        }

        public int Clear() => _cache?.Clear() ?? 0;

        private DateTime FreshnessThreshold(string? blueprintPath, IReadOnlyList<string>? dependencyPaths)
        {
            var threshold = blueprintPath is null || !File.Exists(blueprintPath)
                ? _contentVersionUtc
                : Max(_contentVersionUtc, File.GetLastWriteTimeUtc(blueprintPath));
            if (dependencyPaths is null)
                return threshold;
            foreach (var dependencyPath in dependencyPaths)
            {
                if (!File.Exists(dependencyPath))
                    return DateTime.MaxValue;
                threshold = Max(threshold, File.GetLastWriteTimeUtc(dependencyPath));
            }
            return threshold;
        }

        private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

        private static string Key(ResourceType type, string resRef, bool useIndexedBlueprint) =>
            (useIndexedBlueprint ? "standard:" : "module:") + type.Extension() + ":" + Sanitize(resRef);

        private static string Sanitize(string resRef)
        {
            var builder = new StringBuilder(resRef.Length);
            foreach (var character in resRef)
                builder.Append(char.IsAsciiLetterOrDigit(character) || character is '_' or '-'
                    ? char.ToLowerInvariant(character) : '$');
            return builder.Length == 0 ? "$" : builder.ToString();
        }

        private static string KeyFor(string moduleRoot)
        {
            var normalized = Path.GetFullPath(moduleRoot).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant();
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)).AsSpan(0, 6));
        }
    }
}

