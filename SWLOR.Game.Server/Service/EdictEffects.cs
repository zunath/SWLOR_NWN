using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What a character's city does to the calculations that Edicts and City Affairs outcomes reach (Player City Revamp, "Edict hook
    /// points"). Each method looks up the facts for one hook point (who the citizen is, which city and planet count) and hands them to
    /// <see cref="EdictBonusRules"/> or <see cref="FacilityAccessRules"/>, which hold the math. A hook point calls one method here and
    /// does nothing else about Edicts.
    /// </summary>
    public static class EdictEffects
    {
        #region Who qualifies

        /// <summary>
        /// The character's own city when they get its citizen benefits: a citizen in good standing, with no taxes owed and at least 7 days
        /// of citizenship. Null for anyone else, and for a city that is being removed.
        /// </summary>
        public static WorldProperty GetCitizenCity(Player dbPlayer, DateTime? nowUtc = null)
        {
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
                return null;

            var city = DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId);
            if (city == null || city.PropertyType != PropertyType.City || city.IsQueuedForDeletion)
                return null;

            return EdictRules.IsEligibleCitizen(
                dbPlayer.CitizenPropertyId,
                city.Id,
                dbPlayer.IsDeleted,
                dbPlayer.PropertyOwedTaxes,
                dbPlayer.CitizenSinceUtc,
                nowUtc ?? DateTime.UtcNow)
                ? city
                : null;
        }

        /// <summary>
        /// Whether an area is on the planet the city's Starport serves, on the ground. Space areas have no planet, so they never match,
        /// and a Wild Space station has no planet-based effects.
        /// </summary>
        public static bool IsOnCityPlanet(WorldProperty city, uint area)
        {
            if (city == null || Edict.IsWildSpaceStation(city))
                return false;

            var areaPlanet = Planet.GetPlanetType(area);
            return areaPlanet != PlanetType.Invalid && Station.GetCityPlanet(city) == areaPlanet;
        }

        /// <summary>
        /// Whether the city has built Home Ground. The City Projects that build it arrive with the Homes, farming and projects work, so
        /// until then no city has it; that work replaces this one body.
        /// </summary>
        public static bool HasHomeGround(WorldProperty city)
        {
            return false;
        }

        #endregion

        #region Roleplay XP and death debt

        /// <summary>
        /// The RP XP Festival Season and a funded festival add to an award, for a citizen in good standing: 10% and 5% of the award's
        /// base, beside the Cantina's 5% per level.
        /// </summary>
        public static int GetRoleplayBonusXp(Player dbPlayer, int baseXp, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var city = GetCitizenCity(dbPlayer, now);
            if (city == null)
                return 0;

            return EdictBonusRules.GetFestivalXp(
                baseXp,
                EdictChanges.GetInEffect(city, EdictType.FestivalSeason, now) != null,
                SituationRules.IsModifierActive(city.Modifiers, CityModifierType.FestivalXpBonus, now));
        }

        /// <summary>Whether a situation has the character's city's Medical Center rationing, which halves its bonus for every citizen.</summary>
        public static bool IsMedicalCenterHalved(Player dbPlayer)
        {
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId))
                return false;

            return CityAffairs.IsModifierActive(DB.Get<WorldProperty>(dbPlayer.CitizenPropertyId), CityModifierType.MedicalCenterBonusHalved);
        }

        #endregion

        #region Guild tasks

        /// <summary>The share Guild Charter adds to a guild task's guild points and credits: 0.10, or 0 without it.</summary>
        public static float GetGuildCharterBonus(Player dbPlayer)
        {
            return EdictBonusRules.GetGuildCharterBonus(Edict.GetCitizenEdict(dbPlayer, EdictType.GuildCharter) != null);
        }

        #endregion

        #region Harvesting and combat

        /// <summary>
        /// The chances, in percent, that a harvest finishing now gains 1 more item: Prospectors' Charter's and Home Ground's. Both are
        /// 0 off the city's planet and for anyone who is not a citizen in good standing.
        /// </summary>
        public static (int ProspectorsCharter, int HomeGround) GetHarvestBonusChances(Player dbPlayer, uint area, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var city = GetCitizenCity(dbPlayer, now);
            if (city == null || !IsOnCityPlanet(city, area))
                return (0, 0);

            return (
                EdictChanges.GetInEffect(city, EdictType.ProspectorsCharter, now) != null ? EdictBonusRules.ProspectorsCharterChancePercent : 0,
                HasHomeGround(city) ? EdictBonusRules.HomeGroundChancePercent : 0);
        }

        /// <summary>
        /// The points Hunting Grounds adds to the skill XP bonus of XP from defeating enemies: 10 on the ground of the city's planet,
        /// for a citizen in good standing, otherwise 0.
        /// </summary>
        public static int GetHuntingGroundsPoints(Player dbPlayer, uint area, DateTime? nowUtc = null)
        {
            var now = nowUtc ?? DateTime.UtcNow;
            var city = GetCitizenCity(dbPlayer, now);
            if (city == null ||
                EdictChanges.GetInEffect(city, EdictType.HuntingGrounds, now) == null ||
                !IsOnCityPlanet(city, area))
            {
                return 0;
            }

            return EdictBonusRules.HuntingGroundsBonusPoints;
        }

        #endregion

        #region Starport repairs, crafting and breeding

        /// <summary>The share Drydock Subsidy adds to the repair discount of a citizen in good standing: 0.15, or 0 without it.</summary>
        public static double GetDrydockSubsidyDiscount(Player dbPlayer)
        {
            return Edict.GetCitizenEdict(dbPlayer, EdictType.DrydockSubsidy) != null ? EdictBonusRules.DrydockSubsidyDiscount : 0;
        }

        /// <summary>
        /// The points Artisans' Charter adds to the XP of a craft made at a station in <paramref name="craftCityId"/>: 10 when it is the
        /// character's own city and they are a citizen in good standing, otherwise 0.
        /// </summary>
        public static int GetArtisansCharterPoints(Player dbPlayer, string craftCityId, DateTime? nowUtc = null)
        {
            return IsCitizenEdictInCity(dbPlayer, EdictType.ArtisansCharter, craftCityId, nowUtc)
                ? EdictBonusRules.ArtisansCharterBonusPoints
                : 0;
        }

        /// <summary>
        /// Whether Breeders' Charter applies to an incubation stage the character starts in <paramref name="locationCityId"/>: it is their
        /// own city, they are a citizen in good standing, and the city has the Edict in effect.
        /// </summary>
        public static bool HasBreedersCharter(Player dbPlayer, string locationCityId, DateTime? nowUtc = null)
        {
            return IsCitizenEdictInCity(dbPlayer, EdictType.BreedersCharter, locationCityId, nowUtc);
        }

        private static bool IsCitizenEdictInCity(Player dbPlayer, EdictType type, string cityId, DateTime? nowUtc)
        {
            if (dbPlayer == null || string.IsNullOrWhiteSpace(cityId) || dbPlayer.CitizenPropertyId != cityId)
                return false;

            return Edict.GetCitizenEdict(dbPlayer, type, nowUtc) != null;
        }

        #endregion

        #region Facility access

        /// <summary>
        /// Whether the character may use the city's Bank and Starport, by the Plan's order: Embargo refuses, the city's own citizens with
        /// no taxes owed, allied citizens with no taxes owed at home, anyone under Open Gates, otherwise refused.
        /// </summary>
        public static bool CanUseCityFacility(uint player, WorldProperty city)
        {
            if (!GetIsPC(player) || GetIsDM(player) || city == null)
                return false;

            return GetFacilityAccess(DB.Get<Player>(GetObjectUUID(player)), city) == FacilityAccessResult.Allowed;
        }

        /// <summary>What the city's Bank and Starport do with the character. A missing city has no Edicts, so only its citizens are let in.</summary>
        public static FacilityAccessResult GetFacilityAccess(Player dbPlayer, WorldProperty city, string cityId = null, DateTime? nowUtc = null)
        {
            if (dbPlayer == null)
                return FacilityAccessResult.NotWelcome;

            var now = nowUtc ?? DateTime.UtcNow;
            var hostId = city?.Id ?? cityId;
            var homeId = dbPlayer.CitizenPropertyId;
            var hasHome = !dbPlayer.IsDeleted && !string.IsNullOrWhiteSpace(homeId) && homeId != Guid.Empty.ToString();
            var isCitizenHere = hasHome && !string.IsNullOrWhiteSpace(hostId) && homeId == hostId;

            return FacilityAccessRules.Decide(
                hasHome && NamesCity(city, EdictType.Embargo, homeId, now),
                isCitizenHere,
                hasHome && !isCitizenHere && NamesCity(city, EdictType.Alliance, homeId, now),
                dbPlayer.PropertyOwedTaxes,
                EdictChanges.GetInEffect(city, EdictType.OpenGates, now) != null);
        }

        /// <summary>
        /// Why the character may not use the city's facility, worded for them, or an empty string when they may.
        /// </summary>
        /// <param name="notWelcomeMessage">What a character who is not welcome is told, which names the facility.</param>
        public static string GetFacilityFailure(Player dbPlayer, string cityId, string notWelcomeMessage, DateTime? nowUtc = null)
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            var result = GetFacilityAccess(dbPlayer, city, cityId, nowUtc);
            return FacilityAccessRules.DescribeRefusal(
                result,
                dbPlayer?.PropertyOwedTaxes ?? 0,
                dbPlayer != null && dbPlayer.CitizenPropertyId == cityId,
                notWelcomeMessage);
        }

        /// <summary>
        /// Whether the city has an Embargo against the character's city. An embargoed city's citizens are refused at its Bank, Starport,
        /// Jump Beacon, and Shipyard.
        /// </summary>
        public static bool IsEmbargoedBy(WorldProperty city, Player dbPlayer, DateTime? nowUtc = null)
        {
            return dbPlayer != null && NamesCity(city, EdictType.Embargo, dbPlayer.CitizenPropertyId, nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>Whether the city has an Alliance with the character's city in effect, which makes the character an ally there.</summary>
        public static bool IsAllyOf(WorldProperty city, Player dbPlayer, DateTime? nowUtc = null)
        {
            return dbPlayer != null && NamesCity(city, EdictType.Alliance, dbPlayer.CitizenPropertyId, nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>Whether the city holds the Edict in effect and it names <paramref name="otherCityId"/>.</summary>
        private static bool NamesCity(WorldProperty city, EdictType type, string otherCityId, DateTime nowUtc)
        {
            if (city == null || string.IsNullOrWhiteSpace(otherCityId))
                return false;

            var edict = EdictChanges.GetInEffect(city, type, nowUtc);
            return edict != null && edict.TargetCityId == otherCityId;
        }

        #endregion
    }
}
