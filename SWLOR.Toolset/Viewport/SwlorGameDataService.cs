using Radoub.Formats.Itp;
using Radoub.Formats.Resolver;
using Radoub.Formats.Services;
using Radoub.Formats.Ssf;
using Radoub.Formats.TwoDA;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Viewport
{
    /// <summary>
    /// Minimal <see cref="IGameDataService"/> adapter that lets Radoub.UI's rendering stack
    /// (TextureService / ModelPreviewGLControl / MdlPartComposer) resolve resources through
    /// this app's layered <see cref="ResourceIndex"/> (SWLOR hak sources over the base-game
    /// KEY/BIF). Only the members the rendering path uses are fully implemented; soundset and
    /// palette members return empty because no renderer touches them, and configuration is
    /// fixed at construction (the index already encodes SWLOR's layer order).
    /// </summary>
    public sealed class SwlorGameDataService : IGameDataService
    {
        private readonly ResourceIndex _resourceIndex;
        private readonly TwoDaService? _twoDaService;
        private readonly TlkService? _tlkService;

        public SwlorGameDataService(
            ResourceIndex resourceIndex, TwoDaService? twoDaService = null, TlkService? tlkService = null)
        {
            _resourceIndex = resourceIndex;
            _twoDaService = twoDaService;
            _tlkService = tlkService;
        }

        public byte[]? FindResource(string resRef, ushort resourceType)
        {
            return _resourceIndex.TryLookup(new ResourceIdentity(resRef, resourceType), out var handle)
                ? handle.GetBytes()
                : null;
        }

        /// <summary>The index has no override/module layers, so "base only" reduces to the
        /// same lookup — SWLOR's haks deliberately override base content and previews should
        /// match the game.</summary>
        public byte[]? FindBaseResource(string resRef, ushort resourceType)
        {
            return FindResource(resRef, resourceType);
        }

        public ResourceResult? FindResourceWithSource(string resRef, ushort resourceType)
        {
            if (!_resourceIndex.TryLookup(new ResourceIdentity(resRef, resourceType), out var handle))
                return null;

            var source = handle.Provenance.Kind == ResourceLayerKind.BaseGame
                ? ResourceSource.Bif
                : ResourceSource.Hak;
            return new ResourceResult(
                handle.GetBytes(), source, handle.Provenance.SourcePath, resRef, resourceType);
        }

        public IEnumerable<GameResourceInfo> ListResources(ushort resourceType)
        {
            yield break;
        }

        public TwoDAFile? Get2DA(string name)
        {
            // Radoub's TwoDAFile is not what our TwoDaService caches; the rendering path
            // does not call this, and other consumers should use the app's own services.
            return null;
        }

        public string? Get2DAValue(string twoDAName, int rowIndex, string columnName)
        {
            if (_twoDaService == null || !_twoDaService.TryGetTable(twoDAName, out var table) || table == null)
                return null;

            return rowIndex >= 0 && rowIndex < table.RowCount ? table.GetString(rowIndex, columnName) : null;
        }

        public bool Has2DA(string name)
        {
            return _twoDaService != null && _twoDaService.TryGetTable(name, out _);
        }

        public void ClearCache()
        {
        }

        public string? GetString(uint strRef) => _tlkService?.GetString(strRef);

        public string? GetString(string? strRefStr)
        {
            return uint.TryParse(strRefStr, out var strRef) ? GetString(strRef) : null;
        }

        public bool HasCustomTlk => _tlkService != null;

        public void SetCustomTlk(string? path)
        {
        }

        public SsfFile? GetSoundset(int soundsetId) => null;

        public SsfFile? GetSoundsetByResRef(string resRef) => null;

        public string? GetSoundsetResRef(int soundsetId) => null;

        public IEnumerable<PaletteCategory> GetPaletteCategories(ushort resourceType)
        {
            yield break;
        }

        public string? GetPaletteCategoryName(ushort resourceType, byte categoryId) => null;

        public bool IsConfigured => true;

        public void ReloadConfiguration()
        {
        }

        public void ConfigureModuleHaks(string moduleDirectory)
        {
        }

        public void Dispose()
        {
        }
    }
}
