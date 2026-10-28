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
        /// The type weights in an area of this tier. Gas sites need tier 3 gas clouds; rifts have no weight until Wild
        /// Space exists.
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
        /// cache site can be <paramref name="locked"/>.
        /// </summary>
        public static ScanSignature Create(string id, string areaResref, int tier, SignatureType type, float x, float y, float z, DateTime now,
            bool locked = false)
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
                SignalRequired = ScanningRules.SignalRequired(type, tier),
                Locked = locked && type == SignatureType.CacheSite,
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

        /// <summary>The creature tags of a combat site's squad.</summary>
        public static IReadOnlyList<string> CombatSquad(int tier) => ScanningTuning.CombatSquads[TierIndex(tier)];

        /// <summary>The creature tags of a derelict's defenses.</summary>
        public static IReadOnlyList<string> DerelictDefenses(int tier) => ScanningTuning.DerelictDefenses[TierIndex(tier)];

        /// <summary>The gas cloud blueprint of a gas site.</summary>
        public static string GasCloud(int tier) => ScanningTuning.GasClouds[TierIndex(tier)];

        /// <summary>The credits in a site's cache.</summary>
        public static int SiteCredits(SignatureType type, int tier) =>
            ScanningTuning.SiteCredits.TryGetValue(type, out var credits) ? credits[TierIndex(tier)] : 0;

        /// <summary>The credits in a locked cache, paid when it is sliced open.</summary>
        public static int LockedCacheCredits(int tier) => ScanningTuning.LockedCacheCredits[TierIndex(tier)];

        /// <summary>The loot table a site cache pulls its items from.</summary>
        public static string CacheLootTable(int tier) => $"SPACE_ELITE_T{TierIndex(tier)}";

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
