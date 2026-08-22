using SWLOR.NWN.Formats.Tlk;

namespace SWLOR.Toolset.Domain.GameData.Tlk
{
    /// <summary>
    /// Resolves strrefs across SWLOR's custom TLK (JSON) and, optionally, a base game dialog.tlk.
    /// Custom strrefs follow NWN's custom-TLK convention: strref >= <see cref="CustomTlkBase"/>
    /// (16777216 / 0x01000000) addresses sw_tlk.tlk.json entry id (strref - CustomTlkBase);
    /// anything below that addresses the base dialog.tlk. The base TLK is optional - no NWN
    /// install is assumed present - so lookups below the boundary return null when it is absent.
    /// </summary>
    public sealed class TlkService
    {
        public const uint CustomTlkBase = 16777216;

        private readonly Lazy<(TlkJsonFile Custom, TlkFile? Base)> _data;
        private TlkFile? _customBinaryOverride;

        /// <summary>Raised after the module's selected custom TLK has been atomically replaced.</summary>
        public event Action? CustomTlkReloaded;

        public TlkService(TlkJsonFile customTlk, TlkFile? baseTlk = null)
        {
            ArgumentNullException.ThrowIfNull(customTlk);
            _data = new Lazy<(TlkJsonFile, TlkFile?)>(
                () => (customTlk, baseTlk),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private TlkService(Func<(TlkJsonFile Custom, TlkFile? Base)> load)
        {
            ArgumentNullException.ThrowIfNull(load);
            _data = new Lazy<(TlkJsonFile, TlkFile?)>(
                load,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        /// <summary>
        /// Convenience factory: loads the custom TLK from its JSON path, and optionally the base
        /// dialog.tlk from a binary path (pass null when no NWN install is available).
        /// </summary>
        public static TlkService Load(string customTlkJsonPath, string? baseTlkPath = null)
        {
            var customTlk = TlkJsonFile.Load(customTlkJsonPath);
            var baseTlk = baseTlkPath is null ? null : TlkReader.Read(baseTlkPath);
            return new TlkService(customTlk, baseTlk);
        }

        /// <summary>
        /// Loads the required custom TLK and treats an unreadable optional base TLK as a degraded
        /// feature rather than a startup failure. The custom file is still allowed to throw because
        /// it is repository data the toolset itself requires.
        /// </summary>
        public static TlkService LoadWithOptionalBase(
            string customTlkJsonPath,
            string? baseTlkPath,
            out string? warning)
        {
            var data = LoadDataWithOptionalBase(customTlkJsonPath, baseTlkPath, out warning);
            return new TlkService(data.Custom, data.Base);
        }

        /// <summary>
        /// Creates the shared resolver immediately but defers parsing both TLK files until the first
        /// lookup. App startup uses this form so loading hundreds of thousands of strings never gates
        /// the interactive shell; the background catalog naturally triggers the first lookup.
        /// </summary>
        public static TlkService LoadDeferredWithOptionalBase(
            string customTlkJsonPath,
            string? baseTlkPath,
            Action<string>? reportWarning = null)
        {
            return new TlkService(() =>
            {
                var data = LoadDataWithOptionalBase(customTlkJsonPath, baseTlkPath, out var warning);
                if (warning != null)
                    reportWarning?.Invoke(warning);

                return data;
            });
        }

        private static (TlkJsonFile Custom, TlkFile? Base) LoadDataWithOptionalBase(
            string customTlkJsonPath,
            string? baseTlkPath,
            out string? warning)
        {
            var customTlk = TlkJsonFile.Load(customTlkJsonPath);
            warning = null;
            if (baseTlkPath == null)
                return (customTlk, null);

            try
            {
                return (customTlk, TlkReader.Read(baseTlkPath));
            }
            catch (Exception ex)
            {
                warning =
                    $"Could not load optional base-game dialog.tlk '{baseTlkPath}': {ex.Message}. " +
                    "Custom SWLOR text remains available.";
                return (customTlk, null);
            }
        }

        /// <summary>
        /// Resolves a strref to text. Strrefs >= <see cref="CustomTlkBase"/> resolve against the
        /// custom TLK (id = strref - CustomTlkBase); lower strrefs resolve against the base
        /// dialog.tlk, returning null if no base TLK was supplied.
        /// </summary>
        public string? GetString(uint strref)
        {
            if (strref >= CustomTlkBase)
                return GetCustomText((int)(strref - CustomTlkBase));

            return _data.Value.Base?.GetString(strref);
        }

        /// <summary>
        /// Convenience accessor for a custom TLK entry by its raw (non-offset) id.
        /// </summary>
        public string? GetCustomText(int id)
        {
            var binary = Volatile.Read(ref _customBinaryOverride);
            return binary != null
                ? binary.GetString((uint)id)
                : _data.Value.Custom.GetText(id);
        }

        /// <summary>
        /// Parses a packed custom TLK away from readers, then publishes it in one pointer swap.
        /// Passing null restores the repository JSON custom table used at startup.
        /// </summary>
        public void ReloadCustomTlk(string? binaryTlkPath)
        {
            var replacement = string.IsNullOrWhiteSpace(binaryTlkPath)
                ? null
                : TlkReader.Read(binaryTlkPath);
            Volatile.Write(ref _customBinaryOverride, replacement);
            CustomTlkReloaded?.Invoke();
        }
    }
}
