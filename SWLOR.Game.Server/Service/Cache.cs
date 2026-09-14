using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// This class is responsible for loading and retrieving NWN data which lives for the lifespan of the server.
    /// Nothing in here will be permanently stored, it's simply here to make queries quicker.
    /// If you need persistent storage, refer to the DB class.
    /// </summary>
    public static class Cache
    {
        private static Dictionary<string, string> ItemNamesByResref { get; set; } = new();
        private static Dictionary<string, string> ItemIconsByResref { get; set; } = new();
        private static Dictionary<string, bool> ItemSearchableByResref { get; set; } = new();
        private static Dictionary<int, int> PortraitIdsByInternalId { get; } = new();
        private static Dictionary<int, int> PortraitInternalIdsByPortraitId { get; } = new();
        private static Dictionary<int, string> PortraitResrefByInternalId { get; } = new();
        private static Dictionary<string, int> PortraitInternalIdsByPortraitResref { get; } = new();
        private static Dictionary<int, string> SoundSets { get; set; } = new();
        private static Dictionary<int, string> SoundSetPreviewSoundResrefs { get; set; } = new();
        private static readonly ReadOnlyDictionary<string, string> CustomSoundSetPreviewSoundResrefs = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["c_viper"] = "c_viper_bat1",
                ["c_slime"] = "c_slime_bat1",
                ["c_treant"] = "c_treant_bat1",
                ["c_hsecat"] = "c_hsecat_bat1",
                ["c_monodrn"] = "c_monodrone_bat1",
                ["c_horsexxx"] = "c_horsexxx_slct",
                ["c_parai"] = "c_parai_bat1",
                ["c_secundus"] = "c_secundus_bat1",
                ["c_primus"] = "c_primus_bat1",
                ["c_marut"] = "c_marut_bat1",
                ["c_codi_mane"] = "codi_c_mane_bat1",
                ["aliengen"] = "a_aliengen_bat",
                ["aqualish"] = "a_aqua_bat",
                ["assdroid"] = "c_drdasasin_bat1",
                ["bantha"] = "c_bantha_bat1",
                ["bith1"] = "n_bith_bat1",
                ["chandra"] = "a_chandra_bat",
                ["darkjedif"] = "n_darkjedif_bat1",
                ["darkjedim"] = "n_darkjedim_bat1",
                ["devaronian"] = "a_devar_bat",
                ["droid"] = "cs_droid",
                ["duro1"] = "n_duros_bat1",
                ["duro2"] = "a_duro_bat",
                ["gamorean"] = "c_gamorean_bat1",
                ["gand"] = "a_gand_bat",
                ["gran"] = "a_gran_bat",
                ["hssiss"] = "c_hssiss_atk1",
                ["hutt"] = "c_hutt_bat1",
                ["ithorian"] = "a_ithor_bat",
                ["kathhounda"] = "c_khounda_bat1",
                ["kathhoundb"] = "c_khoundb_bat1",
                ["malerodian"] = "n_rodian_bat1",
                ["mandaloriana"] = "p_mand_bat2",
                ["mandalorianb"] = "n_mndlorian_bat1",
                ["medicaldroid"] = "c_drdmse3_idle",
                ["nikto"] = "a_nikto_bat",
                ["repsoldier"] = "n_repsold_bat1",
                ["republicoff"] = "n_repoff_bat1",
                ["rodianf"] = "a_rodfem_bat",
                ["sithassassin"] = "a_sithass_bat",
                ["sithf"] = "n_sithcomf_bat1",
                ["sithmale"] = "n_sithcomm_bat1",
                ["sithsoldier"] = "n_sithsoldr_bat1",
                ["spaceship"] = "amb_trafnear_01",
                ["sullustan"] = "a_sullust_bat",
                ["tankdroid"] = "c_drdmse1_idle",
                ["toughrodian"] = "a_rodtough_bat",
                ["toughtwilek"] = "a_twitough_bat",
                ["trandoshan"] = "a_trando_bat",
                ["twilekfemale"] = "n_twilekf_bat1",
                ["twilekmale"] = "n_twilekm_bat1",
                ["twilekmaleb"] = "a_twimale_bat",
                ["weequay"] = "a_weequay_bat",
                ["wookie"] = "p_zaalbar_bat1",
                ["hk47"] = "p_hk47_bat1",
                ["c_catfacts"] = "c_catfacts_bat1",
                ["vs_ntuskenx"] = "vs_ntuskenx_bat1"
            });

        /// <summary>
        /// Handles caching data into server memory for quicker lookup later.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheAllItemData();
            CachePortraitsById();
            CacheSoundSets();

            Console.WriteLine($"Loaded {ItemNamesByResref.Count} item names by resref.");
            Console.WriteLine($"Loaded {PortraitIdsByInternalId.Count} portraits by Id.");
            Console.WriteLine($"Loaded {SoundSets.Count} soundsets.");
        }

        /// <summary>
        /// Builds the item name/icon/tradeability caches by spawning every item blueprint in the
        /// module once. Runs at every boot so the cache always matches the current module content;
        /// it is deliberately not persisted anywhere.
        /// </summary>
        private static void CacheAllItemData()
        {
            var resref = UtilPlugin.GetFirstResRef(ResRefType.Item);

            while (!string.IsNullOrWhiteSpace(resref))
            {
                CacheItemNameByResref(resref);
                resref = UtilPlugin.GetNextResRef();
            }
        }

        /// <summary>
        /// Stores the name, icon, and searchability of an individual item into the cache. An item is
        /// searchable when it follows the player market's listing rules (no containers, cursed, plot,
        /// or legacy items) and is not an NPC/creature/internal item per
        /// <see cref="Item.IsEconomyRestricted"/>.
        /// </summary>
        /// <param name="resref">The resref of the item we want to cache.</param>
        private static void CacheItemNameByResref(string resref)
        {
            var storageContainer = GetObjectByTag("TEMP_ITEM_STORAGE");
            var item = CreateItemOnObject(resref, storageContainer);
            ItemNamesByResref[resref] = GetName(item);
            ItemIconsByResref[resref] = Item.GetIconResref(item);
            ItemSearchableByResref[resref] = !GetHasInventory(item) &&
                                             !GetItemCursedFlag(item) &&
                                             !GetPlotFlag(item) &&
                                             !Item.IsLegacyItem(item) &&
                                             !Item.IsEconomyRestricted(item);
            DestroyObject(item);
        }

        /// <summary>
        /// Searches the cached item catalog for items whose name contains the given text. Matching is
        /// case-insensitive and runs against the in-memory name cache, so it is safe to call on demand.
        /// Results are limited to items eligible for player-facing economy surfaces: player-market
        /// tradeable, and not an NPC/creature/internal item (see <see cref="Item.IsEconomyRestricted"/>).
        /// </summary>
        /// <param name="search">The partial name to search for. Whitespace/empty returns no results.</param>
        /// <param name="maxResults">The maximum number of results to return.</param>
        /// <returns>Matching (resref, name) pairs ordered by name, capped at <paramref name="maxResults"/>.</returns>
        public static List<(string Resref, string Name)> SearchItemsByName(string search, int maxResults)
        {
            if (string.IsNullOrWhiteSpace(search))
                return new List<(string, string)>();

            var lowered = search.ToLower();

            return ItemNamesByResref
                .Where(x => !string.IsNullOrWhiteSpace(x.Value) && x.Value.ToLower().Contains(lowered))
                .OrderBy(x => x.Value)
                .Where(x => IsItemSearchableByResref(x.Key))
                .Take(maxResults)
                .Select(x => (x.Key, x.Value))
                .ToList();
        }

        /// <summary>
        /// Determines whether an item blueprint may appear in player-facing economy search surfaces:
        /// player-market tradeable, and not an NPC/creature/internal item. If the resref isn't cached
        /// (i.e. it wasn't part of the module's item palette at boot), the item is spawned once to evaluate it.
        /// </summary>
        /// <param name="resref">The item blueprint resref to check.</param>
        /// <returns>true if the item may be shown to players, false otherwise.</returns>
        public static bool IsItemSearchableByResref(string resref)
        {
            if (!ItemSearchableByResref.ContainsKey(resref))
            {
                CacheItemNameByResref(resref);
            }

            return ItemSearchableByResref[resref];
        }

        /// <summary>
        /// Retrieves the name of an item by its resref. If resref cannot be found, an empty string will be returned.
        /// </summary>
        /// <param name="resref">The resref to search for.</param>
        /// <returns>The name of an item, or an empty string if it cannot be found.</returns>
        public static string GetItemNameByResref(string resref)
        {
            // Item couldn't be found in the cache. Spawn it, get its details, put them in the cache, then destroy it.
            if (!ItemNamesByResref.ContainsKey(resref))
            {
                CacheItemNameByResref(resref);
            }

            return ItemNamesByResref[resref];
        }

        /// <summary>
        /// Retrieves the inventory icon resref of an item by its blueprint resref, using the same icon
        /// rules as the player market (<see cref="Item.GetIconResref"/>). If the resref isn't cached
        /// (i.e. it wasn't part of the module's item palette at boot), the item is spawned once to capture it.
        /// </summary>
        /// <param name="resref">The item blueprint resref to look up.</param>
        /// <returns>The icon resref for the item.</returns>
        public static string GetItemIconByResref(string resref)
        {
            if (!ItemIconsByResref.ContainsKey(resref))
            {
                CacheItemNameByResref(resref);
            }

            return ItemIconsByResref[resref];
        }

        /// <summary>
        /// Retrieves the number of portraits registered in the system.
        /// </summary>
        public static int PortraitCount => PortraitIdsByInternalId.Count;

        private static void CachePortraitsById()
        {
            const string Portraits2DA = "portraits";
            var twoDACount = Get2DARowCount(Portraits2DA);
            var internalId = 1;

            for (var row = 0; row < twoDACount; row++)
            {
                var baseResref = Get2DAString(Portraits2DA, "BaseResRef", row);
                var race = Get2DAString(Portraits2DA, "Race", row);

                if (!string.IsNullOrWhiteSpace(baseResref) &&
                    !string.IsNullOrWhiteSpace(race))
                {
                    PortraitIdsByInternalId[internalId] = row;
                    PortraitInternalIdsByPortraitId[row] = internalId;
                    PortraitResrefByInternalId[internalId] = "po_" + baseResref;
                    PortraitInternalIdsByPortraitResref["po_" + baseResref] = internalId;
                    internalId++;
                }
            }
        }

        private static void CacheSoundSets()
        {
            const string SoundSets2DA = "soundset";
            var soundSetCount = Get2DARowCount(SoundSets2DA);
            var soundSets = new Dictionary<int, string>();
            var previewSoundResrefs = new Dictionary<int, string>();

            for (var row = 0; row < soundSetCount; row++)
            {
                var strRef = Get2DAString(SoundSets2DA, "STRREF", row);
                var resref = Get2DAString(SoundSets2DA, "RESREF", row);

                if (!string.IsNullOrWhiteSpace(strRef) &&
                    !string.IsNullOrWhiteSpace(resref))
                {
                    soundSets.Add(row, GetStringByStrRef(Convert.ToInt32(strRef)));
                    previewSoundResrefs[row] = ResolveSoundSetPreviewSoundResref(resref);
                }
            }

            SoundSets = soundSets.OrderBy(o => o.Value).ToDictionary(x => x.Key, y => y.Value);
            SoundSetPreviewSoundResrefs = previewSoundResrefs;
        }

        private static string ResolveSoundSetPreviewSoundResref(string soundSetResref)
        {
            var trimmedResref = soundSetResref?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedResref) ||
                trimmedResref == "****")
                return string.Empty;

            if (CustomSoundSetPreviewSoundResrefs.TryGetValue(trimmedResref, out var customPreviewSoundResref))
                return customPreviewSoundResref;

            return trimmedResref.Length <= 11
                ? $"{trimmedResref}_bat1".ToLowerInvariant()
                : string.Empty;
        }

        /// <summary>
        /// Retrieves the portrait 2DA Id from the internal Id of the portrait.
        /// The value returned by this method can be used with NWScript.SetPortrait
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id to retrieve.</param>
        /// <returns>The 2DA Id of the portrait.</returns>
        public static int GetPortraitByInternalId(int portraitInternalId)
        {
            return PortraitIdsByInternalId[portraitInternalId];
        }

        /// <summary>
        /// Retrieves the internal Id of a portrait by its NWN 2DA Id.
        /// </summary>
        /// <param name="portraitId">The NWN portrait 2DA Id.</param>
        /// <returns>The internal Id of the portrait.</returns>
        public static int GetPortraitInternalId(int portraitId)
        {
            return PortraitInternalIdsByPortraitId[portraitId];
        }

        /// <summary>
        /// Retrieves the resref of the portrait by the internal portrait Id.
        /// The size of the portrait needs to be appended to the end of this result.
        /// </summary>
        /// <param name="portraitInternalId">The internal portrait Id</param>
        /// <returns>The resref of the portrait, excluding the size.</returns>
        public static string GetPortraitResrefByInternalId(int portraitInternalId)
        {
            return PortraitResrefByInternalId[portraitInternalId];
        }

        public static int GetPortraitInternalIdByResref(string resref)
        {
            if (!PortraitInternalIdsByPortraitResref.ContainsKey(resref))
                return -1;

            return PortraitInternalIdsByPortraitResref[resref];
        }

        public static Dictionary<int, string> GetSoundSets()
        {
            return SoundSets.ToDictionary(x => x.Key, y => y.Value);
        }

        public static string GetSoundSetPreviewSoundResref(int soundSetId)
        {
            return SoundSetPreviewSoundResrefs.TryGetValue(soundSetId, out var previewSoundResref)
                ? previewSoundResref
                : string.Empty;
        }
    }
}
