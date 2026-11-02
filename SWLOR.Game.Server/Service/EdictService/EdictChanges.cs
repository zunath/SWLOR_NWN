using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// Changes to the Edicts a city holds, applied to city records in memory: enacting, repealing, the part-weeks of
    /// Edicts that resume between weekly checks, and the pauses a city's level causes. The caller loads the records,
    /// saves the ones that changed, charges the treasury and posts the Gazette. No database or engine calls.
    /// </summary>
    public static class EdictChanges
    {
        public static int GetCityLevel(WorldProperty city)
        {
            return city.Upgrades.GetValueOrDefault(PropertyUpgradeType.CityLevel, 1);
        }

        /// <summary>The city-wide conditions that hold for the city right now.</summary>
        public static CityWideCondition GetConditions(WorldProperty city)
        {
            return EdictRules.GetConditions(
                city.Upkeep,
                city.Dates.ContainsKey(PropertyDateType.DisrepairDestruction),
                city.NoConfidenceSinceUtc,
                city.NeglectSinceUtc);
        }

        /// <summary>When the city's next weekly upkeep check is due. A city with no date counts as due now.</summary>
        public static DateTime GetNextCheckUtc(WorldProperty city, DateTime nowUtc)
        {
            return city.Dates.TryGetValue(PropertyDateType.Upkeep, out var next) ? next : nowUtc;
        }

        /// <summary>The Edicts the city is billed for right now. Take it before a change that can resume billing.</summary>
        public static HashSet<EdictType> GetBilledTypes(WorldProperty city)
        {
            return EdictRules.GetBilledTypes(city.Edicts, GetConditions(city));
        }

        /// <summary>The Edict if the city has it in effect, otherwise null.</summary>
        public static CityEdict GetInEffect(WorldProperty city, EdictType type, DateTime nowUtc)
        {
            if (city == null || !city.Edicts.TryGetValue(type, out var edict))
                return null;

            return EdictRules.IsInEffect(edict, GetConditions(city), nowUtc) ? edict : null;
        }

        /// <summary>
        /// Adds an Edict to the city, or refuses and changes nothing. An enactment that takes effect starts its 7-day lock.
        /// An Alliance whose partner has one waiting takes effect together with it: the partner's lock starts, and its
        /// part-week joins its upkeep bill, since that city did not choose the moment and cannot be refused. The charge
        /// is returned, not taken, so the caller spends it through the treasury and saves the records.
        /// </summary>
        /// <param name="city">The enacting city.</param>
        /// <param name="target">The city an Alliance or Embargo names, or null when it is missing or being removed.</param>
        /// <param name="targetCityId">The id of the city the mayor named, or empty.</param>
        /// <param name="type">The Edict.</param>
        /// <param name="isWildSpaceStation">Whether the enacting city is a Wild Space station.</param>
        /// <param name="fromPlatform">True when the sitting mayor's election platform enacts it.</param>
        /// <param name="nowUtc">The time.</param>
        public static EdictEnactment Enact(
            WorldProperty city,
            WorldProperty target,
            string targetCityId,
            EdictType type,
            bool isWildSpaceStation,
            bool fromPlatform,
            DateTime nowUtc)
        {
            var plan = PlanEnact(city, target, targetCityId, type, isWildSpaceStation, nowUtc);
            if (plan.Refusal != EdictRefusal.None)
                return plan;

            var namesTarget = EdictCatalog.Get(type).NamesTargetCity;
            city.Edicts[type] = new CityEdict
            {
                State = plan.TakesEffect ? EdictState.Active : EdictState.Pending,
                EnactedUtc = nowUtc,
                LockEndsUtc = plan.TakesEffect ? EdictRules.GetLockEndUtc(nowUtc) : null,
                TargetCityId = namesTarget ? target.Id : string.Empty,
                FromPlatform = fromPlatform
            };

            if (plan.ActivatedPartner)
                ActivateWaitingAlliance(target, FindWaitingAlliance(target, city.Id), nowUtc);

            return plan;
        }

        /// <summary>
        /// What <see cref="Enact"/> would do, without changing anything: why it would be refused, or what it would charge
        /// and whether it would take effect.
        /// </summary>
        public static EdictEnactment PlanEnact(
            WorldProperty city,
            WorldProperty target,
            string targetCityId,
            EdictType type,
            bool isWildSpaceStation,
            DateTime nowUtc)
        {
            var refusal = EdictRules.GetEnactRefusal(
                type,
                city.Id,
                targetCityId,
                target != null,
                GetCityLevel(city),
                isWildSpaceStation,
                GetConditions(city),
                city.Edicts);
            if (refusal != EdictRefusal.None)
                return EdictEnactment.Refused(refusal);

            var waitingAlliance = type == EdictType.Alliance ? FindWaitingAlliance(target, city.Id) : null;
            var charge = EdictRules.GetEnactCharge(type, waitingAlliance != null, nowUtc, GetNextCheckUtc(city, nowUtc));
            if (!CityTreasuryRules.CanSpend(city, charge))
                return EdictEnactment.Refused(EdictRefusal.InsufficientFunds);

            var takesEffect = type != EdictType.Alliance || waitingAlliance != null;
            return new EdictEnactment(EdictRefusal.None, takesEffect, charge, waitingAlliance != null);
        }

        /// <summary>
        /// Removes an Edict once its lock has ended, or refuses and changes nothing. Nothing is refunded. An Edict from
        /// the mayor's platform marks a broken promise. An Alliance ends for the city it named as well. A slot freed by the
        /// repeal lets an Edict the level had paused resume, billed from now.
        /// </summary>
        /// <param name="city">The repealing city.</param>
        /// <param name="target">The city the Edict named, or null.</param>
        /// <param name="type">The Edict.</param>
        /// <param name="nowUtc">The time.</param>
        public static EdictRepeal Repeal(WorldProperty city, WorldProperty target, EdictType type, DateTime nowUtc)
        {
            if (!city.Edicts.TryGetValue(type, out var edict))
                return EdictRepeal.Refused(EdictRefusal.NotHeld);
            if (!EdictRules.CanRepeal(edict, nowUtc))
                return EdictRepeal.Refused(EdictRefusal.StillLocked);

            city.Edicts.Remove(type);
            var brokePromise = edict.FromPlatform;
            if (brokePromise)
                city.MayorRecord.Broken++;

            ApplyCityLevelPauses(city, nowUtc, billResumes: true);

            var endedPartner = type == EdictType.Alliance &&
                               edict.State == EdictState.Active &&
                               target != null &&
                               RemoveAllianceNaming(target, city.Id, nowUtc);
            return new EdictRepeal(EdictRefusal.None, brokePromise, endedPartner);
        }

        /// <summary>
        /// Removes the city's Alliance if it names the other city, and lets what the freed slot allows resume. Returns
        /// true if one was removed.
        /// </summary>
        public static bool RemoveAllianceNaming(WorldProperty city, string otherCityId, DateTime nowUtc)
        {
            if (!city.Edicts.TryGetValue(EdictType.Alliance, out var alliance) || alliance.TargetCityId != otherCityId)
                return false;

            city.Edicts.Remove(EdictType.Alliance);
            ApplyCityLevelPauses(city, nowUtc, billResumes: true);
            return true;
        }

        /// <summary>The other city's pending Alliance, when it names the enacting city.</summary>
        public static CityEdict FindWaitingAlliance(WorldProperty target, string enactingCityId)
        {
            if (target == null ||
                !target.Edicts.TryGetValue(EdictType.Alliance, out var alliance) ||
                alliance.State != EdictState.Pending ||
                alliance.TargetCityId != enactingCityId)
            {
                return null;
            }

            return alliance;
        }

        private static void ActivateWaitingAlliance(WorldProperty waitingCity, CityEdict alliance, DateTime nowUtc)
        {
            var billedBefore = GetBilledTypes(waitingCity);
            alliance.State = EdictState.Active;
            alliance.LockEndsUtc = EdictRules.GetLockEndUtc(nowUtc);
            AddResumedBilling(waitingCity, billedBefore, nowUtc);
        }

        /// <summary>
        /// Adds the part-week of each Edict that is billed now but was not before, because the last weekly check skipped it.
        /// An Edict that resumes at a weekly check is billed there instead.
        /// </summary>
        /// <param name="city">The city.</param>
        /// <param name="billedBefore">What <see cref="GetBilledTypes"/> returned before the change.</param>
        /// <param name="nowUtc">The time.</param>
        /// <returns>The credits added to the upkeep bill.</returns>
        public static int AddResumedBilling(WorldProperty city, HashSet<EdictType> billedBefore, DateTime nowUtc)
        {
            var nextCheck = GetNextCheckUtc(city, nowUtc);
            var added = 0;
            foreach (var type in GetBilledTypes(city).Where(x => !billedBefore.Contains(x)))
            {
                added += EdictRules.GetProration(EdictRules.GetWeeklyCost(type), nowUtc, nextCheck);
            }

            city.Upkeep += added;
            return added;
        }

        /// <summary>
        /// Pauses the Edicts the city's level cannot support, and lets paused ones resume once it can: those whose
        /// minimum level is too high first, then the newest above the slot count.
        /// </summary>
        /// <param name="city">The city, with its level already set.</param>
        /// <param name="nowUtc">The time.</param>
        /// <param name="billResumes">True between weekly checks, so a resumed Edict adds its part-week; the weekly check
        /// bills a resumed Edict itself.</param>
        public static void ApplyCityLevelPauses(WorldProperty city, DateTime nowUtc, bool billResumes)
        {
            var billedBefore = GetBilledTypes(city);
            var paused = EdictRules.GetCityLevelPauses(city.Edicts, GetCityLevel(city));
            foreach (var (type, edict) in city.Edicts)
            {
                edict.IsPausedByCityLevel = paused.Contains(type);
            }

            if (billResumes)
                AddResumedBilling(city, billedBefore, nowUtc);
        }
    }
}
