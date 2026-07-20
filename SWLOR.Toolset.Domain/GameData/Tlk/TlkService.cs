using Radoub.Formats.Tlk;

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

        private readonly TlkJsonFile _customTlk;
        private readonly TlkFile? _baseTlk;

        public TlkService(TlkJsonFile customTlk, TlkFile? baseTlk = null)
        {
            _customTlk = customTlk ?? throw new ArgumentNullException(nameof(customTlk));
            _baseTlk = baseTlk;
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
        /// Resolves a strref to text. Strrefs >= <see cref="CustomTlkBase"/> resolve against the
        /// custom TLK (id = strref - CustomTlkBase); lower strrefs resolve against the base
        /// dialog.tlk, returning null if no base TLK was supplied.
        /// </summary>
        public string? GetString(uint strref)
        {
            if (strref >= CustomTlkBase)
                return GetCustomText((int)(strref - CustomTlkBase));

            return _baseTlk?.GetString(strref);
        }

        /// <summary>
        /// Convenience accessor for a custom TLK entry by its raw (non-offset) id.
        /// </summary>
        public string? GetCustomText(int id) => _customTlk.GetText(id);
    }
}
