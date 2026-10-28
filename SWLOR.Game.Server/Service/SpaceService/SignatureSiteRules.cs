using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Where signatures appear, which type each one is, what its site holds and when it ends (plan section 24). Pure
    /// rules; numbers come from <see cref="ScanningTuning"/>.
    /// </summary>
    public static class SignatureSiteRules
    {
        /// <summary>The scan area of an area resref, or null when signatures never appear there.</summary>
        public static ScanAreaDetail GetArea(string areaResref) =>
            ScanningTuning.Areas.FirstOrDefault(x => x.AreaResref == areaResref);

        /// <summary>
        /// The type weights in an area of this tier. Gas sites need tier 3 gas clouds. Rifts have no weight: the rift
        /// network (<see cref="RiftRules"/>) places them, so they never take a site signature's place.
        /// </summary>
        public static Dictionary<SignatureType, int> GetWeights(int tier)
        {
            return ScanningTuning.Weights.ToDictionary(
                x => x.Key,
                x => x.Key == SignatureType.GasSite && tier < ScanningTuning.GasMinimumTier ? 0 : x.Value);
        }

        /// <summary>Picks a signature type for an area of this tier. <paramref name="roll"/> returns 0 to max - 1.</summary>
        public static SignatureType PickType(int tier, Func<int, int> roll)
        {
            var weights = GetWeights(tier).Where(x => x.Value > 0).OrderBy(x => (int)x.Key).ToList();
            var total = weights.Sum(x => x.Value);
            if (total <= 0)
                return SignatureType.Invalid;

            var pick = roll(total);
            foreach (var (type, weight) in weights)
            {
                if (pick < weight)
                    return type;
                pick -= weight;
            }

            return weights[^1].Key;
        }

        /// <summary>
        /// A new hidden signature of an area, which fades after <see cref="ScanningTuning.HiddenLifetimeSeconds"/>. Only a
        /// cache site can be <paramref name="locked"/>, and only a derelict <paramref name="boardable"/> (and then
        /// <paramref name="contested"/>). A Wild Space signature is tier 5 content at tier 5.5 signal.
        /// </summary>
        public static ScanSignature Create(string id, string areaResref, int tier, SignatureType type, float x, float y, float z, DateTime now,
            bool locked = false, bool wildSpace = false, bool boardable = false, bool contested = false)
        {
            return new ScanSignature
            {
                Id = id,
                AreaResref = areaResref,
                Tier = Math.Clamp(tier, 1, 6),
                Type = type,
                X = x,
                Y = y,
                Z = z,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(ScanningTuning.HiddenLifetimeSeconds),
                SignalRequired = ScanningRules.SignalRequired(type, tier, wildSpace),
                Locked = locked && type == SignatureType.CacheSite,
                WildSpace = wildSpace,
                Boardable = boardable && type == SignatureType.Derelict,
                Contested = contested && boardable && type == SignatureType.Derelict,
            };
        }

        /// <summary>
        /// A faction agent's lead (plan section 29): a hidden signature of the mission's tier, found only by the probes of
        /// the mission holder's party. It is placed outside the area's signature count and never respawns.
        /// </summary>
        public static ScanSignature CreateAgentLead(string id, string areaResref, int tier, SignatureType type, float x, float y, float z,
            DateTime now, string missionId, string playerId)
        {
            return new ScanSignature
            {
                Id = id,
                AreaResref = areaResref,
                Tier = Math.Clamp(tier, 1, 6),
                Type = type,
                X = x,
                Y = y,
                Z = z,
                CreatedAt = now,
                ExpiresAt = now.AddSeconds(ScanningTuning.HiddenLifetimeSeconds),
                SignalRequired = ScanningRules.SignalRequired(type, tier, false),
                AgentMissionId = missionId,
                AgentPlayerId = playerId,
            };
        }

        /// <summary>Whether a party may gather signal on a signature: an agent lead only answers its holder's party.</summary>
        public static bool CanScan(ScanSignature signature, IReadOnlyCollection<string> partyIds)
        {
            return signature != null && (!signature.IsAgentLead || (partyIds?.Contains(signature.AgentPlayerId) ?? false));
        }

        /// <summary>
        /// One end of a rift: a hidden Rift signature that lasts until the rift collapses (the rift network removes it).
        /// A rift leads into or out of Wild Space, so its signal is tier 5.5's.
        /// </summary>
        public static ScanSignature CreateRiftEnd(string id, string areaResref, string riftId, float x, float y, float z, DateTime now,
            DateTime collapsesAt, string detail)
        {
            return new ScanSignature
            {
                Id = id,
                AreaResref = areaResref,
                Tier = WildSpaceTuning.ContentTier,
                Type = SignatureType.Rift,
                X = x,
                Y = y,
                Z = z,
                CreatedAt = now,
                ExpiresAt = collapsesAt,
                SignalRequired = ScanningRules.SignalRequired(SignatureType.Rift, WildSpaceTuning.ContentTier, true),
                WildSpace = true,
                RiftId = riftId,
                RiftDetail = detail,
            };
        }

        /// <summary>
        /// Whether a signature is over: a hidden one has faded, an open site has outlived
        /// <see cref="ScanningTuning.SiteLifetimeSeconds"/>, or its site was completed.
        /// </summary>
        public static bool IsOver(ScanSignature signature, DateTime now)
        {
            if (signature.Completed)
                return true;

            // A rift end lasts as long as its rift; the rift network ends it.
            if (signature.Type == SignatureType.Rift)
                return false;

            // A derelict being boarded or stripped stays open while the work lasts (plan section 28).
            if (signature.HeldUntil is { } held && now < held)
                return false;

            return signature.RevealedAt is { } revealed
                ? now >= revealed.AddSeconds(ScanningTuning.SiteLifetimeSeconds)
                : now >= signature.ExpiresAt;
        }

        /// <summary>
        /// Whether a revealed site's objectives are done, so its cache can spawn: every objective is gone (the squad
        /// or the defenses destroyed, the clouds harvested, the wreck stripped). A cache site is its own cache.
        /// </summary>
        public static bool ShouldSpawnCache(ScanSignature signature, int objectivesLeft) =>
            signature.IsRevealed && !signature.CacheSpawned && signature.Type != SignatureType.CacheSite && objectivesLeft == 0;

        private static int TierIndex(int tier) => Math.Clamp(tier, 1, 6);

        /// <summary>The creature tags of a combat site's squad: Rift Raiders in Wild Space.</summary>
        public static IReadOnlyList<string> CombatSquad(int tier, bool wildSpace = false) =>
            wildSpace ? ScanningTuning.WildSpaceCombatSquad : ScanningTuning.CombatSquads[TierIndex(tier)];

        /// <summary>The creature tags of a derelict's defenses.</summary>
        public static IReadOnlyList<string> DerelictDefenses(int tier) => ScanningTuning.DerelictDefenses[TierIndex(tier)];

        /// <summary>The gas cloud blueprint of a gas site.</summary>
        public static string GasCloud(int tier) => ScanningTuning.GasClouds[TierIndex(tier)];

        /// <summary>The credits in a site's cache.</summary>
        public static int SiteCredits(SignatureType type, int tier) =>
            ScanningTuning.SiteCredits.TryGetValue(type, out var credits) ? credits[TierIndex(tier)] : 0;

        /// <summary>The credits in a locked cache, paid when it is sliced open.</summary>
        public static int LockedCacheCredits(int tier) => ScanningTuning.LockedCacheCredits[TierIndex(tier)];

        /// <summary>The loot table a site cache pulls its items from: the Rift Raiders' elite table in Wild Space.</summary>
        public static string CacheLootTable(int tier, bool wildSpace = false) =>
            wildSpace ? ScanningTuning.WildSpaceCacheTable : $"SPACE_ELITE_T{TierIndex(tier)}";

        /// <summary>Where object <paramref name="index"/> of <paramref name="count"/> stands around a site, on a ring.</summary>
        public static (float X, float Y) RingOffset(int index, int count, float radius)
        {
            if (count <= 1)
                return (0f, 0f);

            var angle = 2.0 * Math.PI * index / count;
            return ((float)(Math.Cos(angle) * radius), (float)(Math.Sin(angle) * radius));
        }
    }
}
