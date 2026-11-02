using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A city's Edicts (Player City Revamp, "Edict rules"): enacting, repealing, billing, and asking whether one works.
    /// The rules live in <see cref="EdictRules"/> and <see cref="EdictChanges"/>; this class loads and saves the city
    /// records, charges the treasury and posts the Gazette. Enacting an Edict stores, bills and pauses it, and what it
    /// does in play is read through <see cref="IsActive(string, EdictType)"/> and
    /// <see cref="GetCitizenEdict(Player, EdictType, DateTime?)"/>.
    /// </summary>
    public static class Edict
    {
        #region Asking

        /// <summary>The city-wide conditions that hold for the city right now.</summary>
        public static CityWideCondition GetConditions(WorldProperty city)
        {
            return EdictChanges.GetConditions(city);
        }

        /// <summary>Whether the city has the Edict in effect: enacted, with no city-wide condition and no single-Edict pause.</summary>
        public static bool IsActive(string cityId, EdictType type)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return false;

            return IsActive(DB.Get<WorldProperty>(cityId), type);
        }

        public static bool IsActive(WorldProperty city, EdictType type, DateTime? nowUtc = null)
        {
            return EdictChanges.GetInEffect(city, type, nowUtc ?? DateTime.UtcNow) != null;
        }

        /// <summary>
        /// The Edict a character gets from its own city, or null. Only citizen Edicts qualify, and only for a citizen who
        /// owes no taxes and has been one for at least 7 days.
        /// </summary>
        public static CityEdict GetCitizenEdict(uint player, EdictType type)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return null;

            return GetCitizenEdict(DB.Get<Player>(GetObjectUUID(player)), type);
        }

        public static CityEdict GetCitizenEdict(Player dbPlayer, EdictType type, DateTime? nowUtc = null)
        {
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
                return null;

            var detail = EdictCatalog.Get(type);
            if (detail == null || !detail.BenefitsCitizens)
                return null;

            var city = DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId);
            if (!IsLiveCity(city))
                return null;

            var now = nowUtc ?? DateTime.UtcNow;
            if (!EdictRules.IsEligibleCitizen(
                    dbPlayer.CitizenPropertyId,
                    city.Id,
                    dbPlayer.IsDeleted,
                    dbPlayer.PropertyOwedTaxes,
                    dbPlayer.CitizenSinceUtc,
                    now))
            {
                return null;
            }

            return EdictChanges.GetInEffect(city, type, now);
        }

        /// <summary>Whether the city is a player station in Wild Space, where planet-based Edicts do not apply.</summary>
        public static bool IsWildSpaceStation(WorldProperty city)
        {
            return Station.GetAnchor(city)?.IsWildSpace ?? false;
        }

        #endregion

        #region Enacting and repealing

        /// <summary>
        /// What enacting would do right now, without changing anything: the reason it would be refused, or what it would
        /// charge and whether it would take effect. The Edicts tab uses it to word its confirmation.
        /// </summary>
        public static EdictEnactment CheckEnact(string cityId, EdictType type, string targetCityId, DateTime? nowUtc = null)
        {
            var city = DB.Get<WorldProperty>(cityId);
            if (!IsLiveCity(city))
                return EdictEnactment.Refused(EdictRefusal.CityUnavailable);

            var namesTarget = EdictCatalog.Get(type)?.NamesTargetCity ?? false;
            var target = namesTarget ? GetLiveCity(targetCityId) : null;
            return EdictChanges.PlanEnact(city, target, targetCityId, type, IsWildSpaceStation(city), nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>
        /// Enacts an Edict for a city. The enacting city pays the days left until its next upkeep check from its treasury
        /// at once (restricted funds first) and is refused if it cannot. An Alliance stays pending, and costs nothing,
        /// until the other city also has one naming this city; the second city to enact pays, and the waiting city's
        /// part-week goes on its upkeep bill. The caller has already checked the official's Manage Edicts permission.
        /// </summary>
        /// <param name="cityId">The enacting city.</param>
        /// <param name="type">The Edict.</param>
        /// <param name="targetCityId">The city an Alliance or Embargo names, otherwise ignored.</param>
        /// <param name="officialPlayerId">The official the Gazette names.</param>
        /// <param name="fromPlatform">True when the sitting mayor's election platform enacts it.</param>
        /// <param name="nowUtc">The time, for tests.</param>
        public static EdictRefusal Enact(
            string cityId,
            EdictType type,
            string targetCityId,
            string officialPlayerId,
            bool fromPlatform = false,
            DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var city = DB.Get<WorldProperty>(cityId);
            if (!IsLiveCity(city))
                return EdictRefusal.CityUnavailable;

            var namesTarget = EdictCatalog.Get(type)?.NamesTargetCity ?? false;
            var target = namesTarget ? GetLiveCity(targetCityId) : null;

            var enactment = EdictChanges.Enact(city, target, targetCityId, type, IsWildSpaceStation(city), fromPlatform, now);
            if (enactment.Refusal != EdictRefusal.None)
                return enactment.Refusal;

            // One save of the city record holds the new Edict and the charge, so a restart cannot split them.
            if (!CityTreasury.Spend(city, enactment.Charge).Succeeded)
                return EdictRefusal.InsufficientFunds;

            Log.Write(LogGroup.Property, $"Edict '{type}' enacted for city '{city.CustomName}' ({city.Id}) by '{officialPlayerId}' for {enactment.Charge} credits.");

            if (enactment.ActivatedPartner)
            {
                DB.Set(target);
                CityGazette.Post(target.Id, CityGazetteEntryType.Edict, EdictRules.BuildAllianceFormedText(city.CustomName));
            }

            CityGazette.Post(
                city.Id,
                CityGazetteEntryType.Edict,
                EdictRules.BuildEnactedText(type, target?.CustomName, enactment.TakesEffect),
                officialPlayerId,
                CityPowers.GetDelegateTitle(city, officialPlayerId));
            return EdictRefusal.None;
        }

        /// <summary>
        /// Repeals an Edict. It must be past its 7-day lock, and nothing is refunded. Repealing an Edict from the mayor's
        /// platform marks a broken promise. An Alliance ends for the other city too. The caller has already checked the
        /// official's Manage Edicts permission.
        /// </summary>
        public static EdictRefusal Repeal(string cityId, EdictType type, string officialPlayerId, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var city = DB.Get<WorldProperty>(cityId);
            if (!IsLiveCity(city))
                return EdictRefusal.CityUnavailable;

            var targetCityId = city.Edicts.TryGetValue(type, out var held) ? held.TargetCityId : string.Empty;
            var target = string.IsNullOrWhiteSpace(targetCityId) ? null : DB.Get<WorldProperty>(targetCityId);

            var repeal = EdictChanges.Repeal(city, target, type, now);
            if (repeal.Refusal != EdictRefusal.None)
                return repeal.Refusal;

            DB.Set(city);
            Log.Write(LogGroup.Property, $"Edict '{type}' repealed for city '{city.CustomName}' ({city.Id}) by '{officialPlayerId}'.");

            if (repeal.EndedPartnerAlliance)
            {
                DB.Set(target);
                CityGazette.Post(target.Id, CityGazetteEntryType.Edict, EdictRules.BuildAllianceEndedText(city.CustomName, "repealed it"));
            }

            CityGazette.Post(
                city.Id,
                CityGazetteEntryType.Edict,
                EdictRules.BuildRepealedText(type, target?.CustomName),
                officialPlayerId,
                CityPowers.GetDelegateTitle(city, officialPlayerId));
            return EdictRefusal.None;
        }

        /// <summary>
        /// Replaces every Edict the city holds with an outright election winner's platform, whatever the locks, marks them as
        /// from the platform, adds their first part-week to the upkeep bill and sets the mayor's record to the number
        /// promised (see <see cref="EdictChanges.EnactPlatform"/>). Changes the city in memory only: the election result
        /// saves it, and then calls <see cref="AnnouncePlatform"/>. The other cities an Alliance joins or ends are saved here.
        /// </summary>
        /// <param name="city">The city whose election result is posting, with the election's conditions already cleared.</param>
        /// <param name="platform">The winner's platform.</param>
        /// <param name="nowUtc">The time, for tests.</param>
        public static EdictPlatformEnactment EnactPlatform(WorldProperty city, ElectionPlatform platform, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var enactment = EdictChanges.EnactPlatform(city, platform.Edicts, GetLiveCity, IsWildSpaceStation(city), now);

            // The other cities are saved first. If the server stops before the city is, the result is counted again at the
            // next start, and that run finds their records already changed and ends in the same place.
            foreach (var other in enactment.ChangedCities)
            {
                DB.Set(other);
            }

            Log.Write(LogGroup.Property, $"Platform of the new mayor of city '{city.CustomName}' ({city.Id}) enacted {enactment.Enacted.Count} Edict(s) over {enactment.ReplacedCount} earlier one(s); {enactment.PartWeekAdded} credits added to the upkeep bill.");
            foreach (var (type, refusal) in enactment.Dropped)
            {
                Log.Write(LogGroup.Property, $"Platform Edict '{type}' of city '{city.CustomName}' ({city.Id}) was not enacted: {refusal}.");
            }

            return enactment;
        }

        /// <summary>Posts the Gazette entries for a platform that took effect, once the city has been saved.</summary>
        /// <param name="city">The city.</param>
        /// <param name="enactment">What <see cref="EnactPlatform"/> returned.</param>
        /// <param name="mayorPlayerId">The mayor the platform belongs to.</param>
        public static void AnnouncePlatform(WorldProperty city, EdictPlatformEnactment enactment, string mayorPlayerId)
        {
            foreach (var partner in enactment.FormedAlliances)
            {
                CityGazette.Post(partner.Id, CityGazetteEntryType.Edict, EdictRules.BuildAllianceFormedText(city.CustomName));
            }

            foreach (var partner in enactment.EndedAlliances)
            {
                CityGazette.Post(partner.Id, CityGazetteEntryType.Edict, EdictRules.BuildAllianceEndedText(city.CustomName, "replaced its Edicts after an election"));
            }

            if (enactment.Enacted.Count == 0 && enactment.ReplacedCount == 0)
                return;

            CityGazette.Post(
                city.Id,
                CityGazetteEntryType.Edict,
                EdictRules.BuildPlatformEnactedText(enactment.Enacted, enactment.ReplacedCount),
                mayorPlayerId,
                EdictRules.GetOfficialTitle(true));
        }

        /// <summary>
        /// Ends every other city's Alliance with a city that is being removed. Their slots free up, and the Gazette says why.
        /// </summary>
        public static void OnCityDeleted(WorldProperty deletedCity)
        {
            if (deletedCity == null)
                return;

            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return;

            var now = DateTime.UtcNow;
            foreach (var city in DB.Search(query.AddPaging(count, 0)).ToList().Where(x => x.Id != deletedCity.Id))
            {
                if (!EdictChanges.RemoveAllianceNaming(city, deletedCity.Id, now))
                    continue;

                DB.Set(city);
                Log.Write(LogGroup.Property, $"Alliance of city '{city.CustomName}' ({city.Id}) ended because city '{deletedCity.CustomName}' ({deletedCity.Id}) was removed.");
                CityGazette.Post(city.Id, CityGazetteEntryType.Edict, EdictRules.BuildAllianceEndedText(deletedCity.CustomName, "was removed"));
            }
        }

        private static bool IsLiveCity(WorldProperty city)
        {
            return city != null && city.PropertyType == PropertyType.City && !city.IsQueuedForDeletion;
        }

        /// <summary>The city with the id, or null when it does not exist or is being removed.</summary>
        public static WorldProperty GetLiveCity(string cityId)
        {
            if (string.IsNullOrWhiteSpace(cityId))
                return null;

            var city = DB.Get<WorldProperty>(cityId);
            return IsLiveCity(city) ? city : null;
        }

        #endregion

        #region Billing and pauses

        /// <summary>The Edict charges the weekly check adds to the bill, one line for each billed Edict.</summary>
        public static IReadOnlyList<EdictBillLine> GetWeeklyBill(WorldProperty city)
        {
            return EdictRules.GetBillLines(city.Edicts, GetConditions(city));
        }

        /// <summary>The Edicts the city is billed for right now. Take it before a change that can resume billing.</summary>
        public static HashSet<EdictType> GetBilledTypes(WorldProperty city)
        {
            return EdictChanges.GetBilledTypes(city);
        }

        /// <summary>
        /// Adds the part-week of each Edict that is billed now but was not before, because the last weekly check skipped
        /// it. Changes only the record in memory. Call it with the billed set taken before whatever cleared the pause
        /// (a bill paid, an election result), so the Edicts that resume pay for the days left in the week.
        /// </summary>
        public static int AddResumedBilling(WorldProperty city, HashSet<EdictType> billedBefore, DateTime nowUtc)
        {
            return EdictChanges.AddResumedBilling(city, billedBefore, nowUtc);
        }

        /// <summary>
        /// Pauses the Edicts the city's level cannot support, and lets paused ones resume once it can. Changes only the
        /// record in memory. The weekly check passes false, since it bills a resumed Edict itself.
        /// </summary>
        public static void ApplyCityLevelPauses(WorldProperty city, DateTime nowUtc, bool billResumes)
        {
            EdictChanges.ApplyCityLevelPauses(city, nowUtc, billResumes);
        }

        #endregion
    }
}
