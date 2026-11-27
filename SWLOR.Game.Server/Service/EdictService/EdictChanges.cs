using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
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

        /// <summary>
        /// Whether a situation outcome on the city pauses the Edict: the Edict's own situation pause, or the city modifier
        /// that stands for one (Open Gates suspended). The Edict is still billed while it is paused.
        /// </summary>
        public static bool IsPausedBySituation(WorldProperty city, EdictType type, CityEdict edict, DateTime nowUtc)
        {
            return EdictRules.IsPausedBySituation(edict, nowUtc) ||
                   (type == EdictType.OpenGates &&
                    SituationRules.IsModifierActive(city.Modifiers, CityModifierType.OpenGatesSuspended, nowUtc));
        }

        /// <summary>The Edict if the city has it in effect, otherwise null.</summary>
        public static CityEdict GetInEffect(WorldProperty city, EdictType type, DateTime nowUtc)
        {
            if (city == null || !city.Edicts.TryGetValue(type, out var edict))
                return null;

            return EdictRules.IsInEffect(edict, GetConditions(city), nowUtc) && !IsPausedBySituation(city, type, edict, nowUtc)
                ? edict
                : null;
        }

        /// <summary>Every Edict the city has in effect, in type order.</summary>
        public static List<EdictType> GetInEffectTypes(WorldProperty city, DateTime nowUtc)
        {
            return city.Edicts.Keys
                .Where(type => GetInEffect(city, type, nowUtc) != null)
                .OrderBy(type => (int)type)
                .ToList();
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
        /// <param name="isOpenPort">Whether the enacting city's Starport exchange is an open port.</param>
        public static EdictEnactment Enact(
            WorldProperty city,
            WorldProperty target,
            string targetCityId,
            EdictType type,
            bool isWildSpaceStation,
            bool fromPlatform,
            DateTime nowUtc,
            bool isOpenPort = false)
        {
            var plan = PlanEnact(city, target, targetCityId, type, isWildSpaceStation, nowUtc, isOpenPort);
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
            DateTime nowUtc,
            bool isOpenPort = false)
        {
            var refusal = EdictRules.GetEnactRefusal(
                type,
                city.Id,
                targetCityId,
                target != null,
                GetCityLevel(city),
                isWildSpaceStation,
                GetConditions(city),
                city.Edicts,
                isOpenPort);
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
        /// Replaces every Edict the city holds with an outright winner's platform, whatever the locks (Player City Revamp,
        /// "Edict rules" item 3). Each promised Edict is enacted from the platform with a fresh 7-day lock, then the ones
        /// the city's level cannot hold pause (a paused Edict is not a broken promise), and the first part-week of each
        /// Edict that works now is added to the upkeep bill. The mayor's record starts again at the Edicts enacted.
        /// City-wide conditions are read from the city, so call it after the election has cleared the ones it ends: any
        /// that remain keep the new Edicts paused, and they pay their part-week when the pause lifts. An Alliance that
        /// replaces one with the same city keeps the pair going; any other old Alliance ends for the city it named.
        /// Nothing is charged to the treasury.
        /// </summary>
        /// <param name="city">The city. Its Edicts and mayor's record change; the caller saves it.</param>
        /// <param name="platform">The Edicts the winner promised.</param>
        /// <param name="findCity">A live city by id, or null when it is missing or being removed.</param>
        /// <param name="isWildSpaceStation">Whether the city is a Wild Space station.</param>
        /// <param name="nowUtc">The time the results post.</param>
        /// <param name="isOpenPort">Whether the city's Starport exchange is an open port, where a promised Shadowport is dropped.</param>
        public static EdictPlatformEnactment EnactPlatform(
            WorldProperty city,
            IEnumerable<ElectionPlatformEdict> platform,
            Func<string, WorldProperty> findCity,
            bool isWildSpaceStation,
            DateTime nowUtc,
            bool isOpenPort = false)
        {
            var previous = city.Edicts.ToList();

            // Promised Edicts are checked for what can go wrong since registration (a named city gone, a station moved to
            // Wild Space). The city's level is not: a platform is enacted in full and the level pauses what it cannot hold.
            var accepted = new List<(ElectionPlatformEdict Promise, WorldProperty Target)>();
            var dropped = new List<(EdictType Type, EdictRefusal Refusal)>();
            var held = new Dictionary<EdictType, CityEdict>();
            foreach (var promise in ElectionPlatformRules.Normalize(platform))
            {
                var namesTarget = EdictCatalog.Get(promise.Type)?.NamesTargetCity ?? false;
                var target = namesTarget && !string.IsNullOrWhiteSpace(promise.TargetCityId) ? findCity(promise.TargetCityId) : null;
                var refusal = EdictRules.GetEnactRefusal(
                    promise.Type,
                    city.Id,
                    promise.TargetCityId,
                    target != null,
                    CityLevelRules.MaxLevel,
                    isWildSpaceStation,
                    CityWideCondition.None,
                    held,
                    isOpenPort);
                if (refusal != EdictRefusal.None)
                {
                    dropped.Add((promise.Type, refusal));
                    continue;
                }

                accepted.Add((promise, target));
                held[promise.Type] = new CityEdict { TargetCityId = promise.TargetCityId };
            }

            // An Alliance that is replaced ends for the other city, unless the platform enacts it again with the same city.
            var keptPartnerIds = accepted
                .Where(x => x.Promise.Type == EdictType.Alliance)
                .Select(x => x.Target.Id)
                .ToHashSet();
            var ended = new List<WorldProperty>();
            foreach (var (type, edict) in previous)
            {
                if (type != EdictType.Alliance ||
                    edict.State != EdictState.Active ||
                    string.IsNullOrWhiteSpace(edict.TargetCityId) ||
                    keptPartnerIds.Contains(edict.TargetCityId))
                {
                    continue;
                }

                var partner = findCity(edict.TargetCityId);
                if (partner != null && RemoveAllianceNaming(partner, city.Id, nowUtc))
                    ended.Add(partner);
            }

            city.Edicts.Clear();
            var formed = new List<WorldProperty>();
            foreach (var (promise, target) in accepted)
            {
                var edict = new CityEdict
                {
                    State = EdictState.Active,
                    EnactedUtc = nowUtc,
                    LockEndsUtc = EdictRules.GetLockEndUtc(nowUtc),
                    TargetCityId = target?.Id ?? string.Empty,
                    FromPlatform = true
                };

                if (promise.Type == EdictType.Alliance)
                {
                    var partnerAlliance = target.Edicts.GetValueOrDefault(EdictType.Alliance);
                    if (partnerAlliance == null || partnerAlliance.TargetCityId != city.Id)
                    {
                        // The other mayor has not enacted it yet: it takes a slot and costs nothing until they do.
                        edict.State = EdictState.Pending;
                        edict.LockEndsUtc = null;
                    }
                    else if (partnerAlliance.State == EdictState.Pending)
                    {
                        ActivateWaitingAlliance(target, partnerAlliance, nowUtc);
                        formed.Add(target);
                    }
                }

                city.Edicts[promise.Type] = edict;
            }

            ApplyCityLevelPauses(city, nowUtc, billResumes: false);

            // Every Edict is new, so each one that works now pays its first part-week. Under a remaining city-wide pause
            // none works yet, and each pays when the pause lifts.
            var partWeek = AddResumedBilling(city, new HashSet<EdictType>(), nowUtc);

            city.MayorRecord = new MayorRecord { Promised = accepted.Count };

            return new EdictPlatformEnactment(
                accepted.Select(x => (x.Promise.Type, x.Target?.CustomName ?? string.Empty)).ToList(),
                dropped,
                previous.Count,
                formed,
                ended,
                partWeek);
        }

        /// <summary>
        /// Clears the platform mark on every Edict the city holds. Used when a different player takes the seat without a
        /// platform: the Edicts stay, but they were not promised by the new mayor, so repealing one breaks no promise.
        /// </summary>
        public static void ClearPlatformOrigin(WorldProperty city)
        {
            foreach (var edict in city.Edicts.Values)
            {
                edict.FromPlatform = false;
            }
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
