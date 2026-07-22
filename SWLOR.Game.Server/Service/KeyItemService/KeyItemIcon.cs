using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Service.KeyItemService
{
    public static class KeyItemIcon
    {
        public const string Default = "iki_default";

        public const string PublishedFieldGuide = "iki_fn_guide";
        public const string LicensedResearchDatapad = "iki_fn_datapad";
        public const string HandwrittenDiscoveryJournal = "iki_fn_journal";
        public const string HolographicObservationLog = "iki_fn_holo";
        public const string SealedBossDossier = "iki_fn_dossier";
        public const string EncryptedRestrictedReport = "iki_fn_restrict";

        public const string OrbitMap = "iki_map_orbit";
        public const string WildernessMap = "iki_map_wild";
        public const string SettlementMap = "iki_map_settle";
        public const string FacilityMap = "iki_map_facility";
        public const string CavernMap = "iki_map_cavern";
        public const string RuinsMap = "iki_map_ruins";

        public static IReadOnlyCollection<string> FieldNoteIconResrefs { get; } = new[]
        {
            PublishedFieldGuide,
            LicensedResearchDatapad,
            HandwrittenDiscoveryJournal,
            HolographicObservationLog,
            SealedBossDossier,
            EncryptedRestrictedReport,
        };

        public static IReadOnlyCollection<string> MapIconResrefs { get; } = new[]
        {
            OrbitMap,
            WildernessMap,
            SettlementMap,
            FacilityMap,
            CavernMap,
            RuinsMap,
        };

        public static string GetIconResref(KeyItemType keyItem)
        {
            var detail = keyItem.GetAttribute<KeyItemType, KeyItemAttribute>();
            if (!detail.IsActive)
                return string.Empty;

            if (detail.Category == KeyItemCategoryType.Maps)
                return GetMapIconResref(keyItem);

            if (detail.Category != KeyItemCategoryType.FieldNotes)
                return $"iki_{(int)keyItem:D4}";

            if (!IncubationFieldNote.TryGetNoteForKeyItem(keyItem, out var note))
                throw new InvalidOperationException($"Field note key item '{keyItem}' is not registered.");

            var alternate = (int)keyItem % 2 == 0;
            return note.Acquisition switch
            {
                FieldNoteAcquisitionType.Store => alternate
                    ? PublishedFieldGuide
                    : LicensedResearchDatapad,
                FieldNoteAcquisitionType.DiscoveryOnly => alternate
                    ? HandwrittenDiscoveryJournal
                    : HolographicObservationLog,
                FieldNoteAcquisitionType.BossDrop => alternate
                    ? SealedBossDossier
                    : EncryptedRestrictedReport,
                _ => throw new InvalidOperationException(
                    $"Field note key item '{keyItem}' has invalid acquisition type '{note.Acquisition}'."),
            };
        }

        private static string GetMapIconResref(KeyItemType keyItem)
        {
            var name = keyItem.ToString();

            if (ContainsAny(name, "Orbit"))
                return OrbitMap;

            if (ContainsAny(name, "Ruin", "Crypt", "Temple"))
                return RuinsMap;

            if (ContainsAny(name, "Cave", "Cavern", "Grotto", "Sewer", "Tunnel"))
                return CavernMap;

            if (ContainsAny(name,
                    "Facility", "Base", "Maintenance", "Office", "Lab", "Warehouse",
                    "Hotel", "Casino", "Catwalk", "FightClub", "Cantina", "Medshed",
                    "Station", "Hub", "TiltedVisor", "CzerkaArms"))
                return FacilityMap;

            if (ContainsAny(name,
                    "Outpost", "Colony", "Anchorhead", "MosEsper", "Village", "Tribal",
                    "CorporateDistrict", "LandingPads", "Promenade", "ShippingDistrict", "Slums"))
                return SettlementMap;

            return WildernessMap;
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }
}
