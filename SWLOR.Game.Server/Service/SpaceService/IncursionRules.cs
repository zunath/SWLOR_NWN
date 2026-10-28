using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The incursion rules (plan section 30), pure: the weekly slot, picking the region and invader, what each area spawns,
    /// blockades and the price spike, contribution and the payout. Numbers come from <see cref="IncursionTuning"/>.
    /// </summary>
    public static class IncursionRules
    {
        private static readonly Dictionary<string, string> _hullNames = new()
        {
            ["fighter"] = "Fighter",
            ["interceptor"] = "Interceptor",
            ["bomber"] = "Bomber",
            ["gunship"] = "Gunship",
            ["platform"] = "Blockade Platform",
        };

        // Schedule

        /// <summary>The first weekly slot strictly after <paramref name="after"/> (UTC).</summary>
        public static DateTime NextStart(DateTime after)
        {
            var day = DateTime.SpecifyKind(after.Date, DateTimeKind.Utc);
            for (var offset = 0; offset <= 7; offset++)
            {
                var slot = day.AddDays(offset).AddHours(IncursionTuning.StartHourUtc);
                if (slot.DayOfWeek == IncursionTuning.StartDay && slot > after)
                    return slot;
            }

            return day.AddDays(14).AddHours(IncursionTuning.StartHourUtc);
        }

        /// <summary>Whether a scheduled start has come.</summary>
        public static bool IsDue(DateTime nextStart, DateTime now) => nextStart != default && now >= nextStart;

        /// <summary>
        /// Whether a start was missed outright: the server was down for its whole window, so that week is skipped rather
        /// than started late.
        /// </summary>
        public static bool IsMissed(DateTime nextStart, DateTime now) => now >= nextStart.AddHours(IncursionTuning.DurationHours);

        /// <summary>A random region other than last week's.</summary>
        public static IncursionRegion PickRegion(IReadOnlyList<IncursionRegion> regions, string lastCenter, Func<int, int> next)
        {
            var choices = regions.Where(x => x.Center != lastCenter).ToList();
            if (choices.Count == 0)
                choices = regions.ToList();

            return choices.Count == 0 ? null : choices[next(choices.Count)];
        }

        public static IncursionInvader PickInvader(Func<int, int> next) =>
            IncursionTuning.Invaders[next(IncursionTuning.Invaders.Count)].Invader;

        public static IncursionRegion FindRegion(string center) =>
            IncursionTuning.Regions.FirstOrDefault(x => x.Center == center);

        public static IncursionInvaderDetail GetInvader(IncursionInvader invader) =>
            IncursionTuning.Invaders.FirstOrDefault(x => x.Invader == invader);

        /// <summary>The region area with this resref in the running incursion, or null.</summary>
        public static IncursionArea FindArea(ActiveIncursion incursion, string areaResref)
        {
            if (incursion == null || string.IsNullOrEmpty(areaResref))
                return null;

            return FindRegion(incursion.Center)?.Areas.FirstOrDefault(x => x.Resref == areaResref);
        }

        /// <summary>A new incursion of a region: every area's progress at zero and the blockaded docks found in it.</summary>
        public static ActiveIncursion Create(IncursionRegion region, IncursionInvader invader, DateTime now, IReadOnlyDictionary<string, int> docks)
        {
            return new ActiveIncursion
            {
                Center = region.Center,
                Invader = invader,
                StartedAt = now,
                EndsAt = now.AddHours(IncursionTuning.DurationHours),
                Areas = region.Areas.ToDictionary(x => x.Resref, _ => new IncursionAreaProgress()),
                Blockades = docks.ToDictionary(x => x.Key, x => x.Value),
            };
        }

        // Spawns

        /// <summary>The ships of one wave at a tier, as (creature tag, hull).</summary>
        public static List<(string Tag, string Hull)> WaveShips(int tier) =>
            IncursionTuning.WaveHulls[Math.Clamp(tier, 1, 6)].Select(h => ($"t{Math.Clamp(tier, 1, 6)}{h}", h)).ToList();

        /// <summary>A dock's blockade at a tier.</summary>
        public static List<(string Tag, string Hull)> BlockadeShips(int tier) =>
            IncursionTuning.BlockadeHulls.Select(h => ($"t{Math.Clamp(tier, 1, 6)}{h}", h)).ToList();

        /// <summary>The command group: the command ship first, then its escorts.</summary>
        public static List<(string Tag, string Hull)> CommandShips(int tier, IncursionInvader invader)
        {
            tier = Math.Clamp(tier, 1, 6);
            var ships = new List<(string Tag, string Hull)> { (CommandTag(tier, invader), "capital") };
            ships.AddRange(IncursionTuning.CommandEscorts.Select(h => ($"t{tier}{h}", h)));
            return ships;
        }

        /// <summary>The tier 6 command ship is the invader's capital; below it, the tier's capital flies that capital's hull.</summary>
        public static string CommandTag(int tier, IncursionInvader invader) =>
            tier >= 6 ? GetInvader(invader)?.CapitalTag ?? string.Empty : $"t{tier}capital";

        /// <summary>Whether a command ship takes the invader capital's appearance (tiers 1-5).</summary>
        public static bool UsesInvaderAppearance(int tier) => tier < 6;

        public static string ShipName(IncursionInvader invader, string hull)
        {
            var name = GetInvader(invader)?.Name ?? "Armada";
            return hull == "capital" ? $"{name} Command Ship" : $"{name} {(_hullNames.TryGetValue(hull, out var label) ? label : hull)}";
        }

        // Blockades, docking and trade

        public static string DockKey(string areaResref, string teleportWaypoint) => $"{areaResref}:{teleportWaypoint}";

        public static bool IsBlockadeHolding(ActiveIncursion incursion, string dockKey) =>
            incursion != null && incursion.Blockades.ContainsKey(dockKey) && !incursion.BrokenBlockades.Contains(dockKey);

        /// <summary>An exchange is closed while a blockade holds at a dock that serves it.</summary>
        public static bool IsExchangeClosed(ActiveIncursion incursion, TradeExchangeType exchange)
        {
            if (incursion == null || exchange == TradeExchangeType.Invalid)
                return false;

            return incursion.Blockades.Any(x => !incursion.BrokenBlockades.Contains(x.Key) &&
                                                TradeLookup.GetExchangeForPlanet((PlanetType)x.Value) == exchange);
        }

        /// <summary>The price multiplier of an exchange: the spike while its dock's orbit is in the running incursion's region.</summary>
        public static double GetPriceMultiplier(ActiveIncursion incursion, TradeExchangeType exchange)
        {
            var detail = TradeLookup.GetExchange(exchange);
            return detail != null && FindArea(incursion, detail.OrbitResref) != null ? IncursionTuning.PriceMultiplier : 1.0;
        }

        public static string DockRefusal(IncursionInvader invader) =>
            $"The {GetInvader(invader)?.Name ?? "armada"} blockades this dock. Docking services are offline until the blockade is broken.";

        public static string ExchangeRefusal(IncursionInvader invader) =>
            $"The exchange is closed: the {GetInvader(invader)?.Name ?? "armada"} blockades its dock.";

        // Progress

        public static bool IsAreaLiberated(IncursionAreaProgress progress) =>
            progress != null && progress.WavesCleared >= IncursionTuning.WavesPerArea && progress.CommandDestroyed;

        /// <summary>A region is cleared when every area is liberated and every blockade broken.</summary>
        public static bool IsCleared(ActiveIncursion incursion) =>
            incursion != null &&
            incursion.Areas.Values.All(IsAreaLiberated) &&
            incursion.Blockades.Keys.All(incursion.BrokenBlockades.Contains);

        // Contribution

        public static double KillPoints(string difficulty) =>
            IncursionTuning.KillEquivalents.TryGetValue(difficulty ?? string.Empty, out var points) ? points : IncursionTuning.KillEquivalents["Normal"];

        /// <summary>
        /// A destroyed ship's points split between the ships that damaged it, by damage dealt; the killing blow's ship adds
        /// <see cref="IncursionTuning.KillBonus"/> of them.
        /// </summary>
        public static Dictionary<T, double> SplitKill<T>(IReadOnlyDictionary<T, double> damage, double points, T killer)
        {
            var shares = new Dictionary<T, double>();
            var total = damage.Values.Where(x => x > 0).Sum();
            if (total <= 0)
                return shares;

            foreach (var (ship, dealt) in damage.Where(x => x.Value > 0))
                shares[ship] = points * dealt / total;

            if (killer is not null && shares.ContainsKey(killer))
                shares[killer] += points * IncursionTuning.KillBonus;

            return shares;
        }

        /// <summary>
        /// Credits a destroyed ship points to the contributions of the players behind the ships that damaged it.
        /// <paramref name="damage"/> and <paramref name="killer"/> are keyed by the persistent player id of each pilot, so
        /// a pilot who logged out before the kill is still paid (as a pending payout when the region clears).
        /// <paramref name="aboard"/> lists the players sharing a pilot ship now: the crew while the pilot is online, or
        /// just the pilot (with the last known name) when not.
        /// </summary>
        public static void CreditKill(ActiveIncursion incursion, int tier, IReadOnlyDictionary<string, double> damage, double points, string killer,
            Func<string, IReadOnlyList<(string Id, string Name)>> aboard)
        {
            foreach (var (pilot, share) in SplitKill(damage, points, killer))
            {
                var crew = aboard(pilot);
                if (crew == null || crew.Count == 0)
                    continue;

                foreach (var (id, name) in crew)
                {
                    if (!incursion.Contributions.TryGetValue(id, out var contribution))
                        incursion.Contributions[id] = contribution = new IncursionContribution();

                    if (!string.IsNullOrWhiteSpace(name))
                        contribution.Name = name;
                    AddPoints(contribution, tier, share / crew.Count);
                    contribution.Damage += (long)Math.Round(damage[pilot] / crew.Count);
                    if (pilot == killer)
                        contribution.Kills++;
                }
            }
        }

        /// <summary>
        /// Takes a pending payout out of the saved list only when it can be delivered right now, so a player who
        /// disconnects before delivery keeps it pending for the next login. Null when there is nothing to deliver.
        /// </summary>
        public static IncursionPayout ClaimPending(Dictionary<string, IncursionPayout> pending, string playerId, bool canDeliver) =>
            canDeliver && pending.Remove(playerId, out var payout) ? payout : null;

        /// <summary>Points for repairing an ally in an area of this tier.</summary>
        public static double RepairPoints(int restored, int tier)
        {
            if (restored <= 0)
                return 0;

            var pool = IncursionTuning.FighterPool[Math.Clamp(tier, 1, 6)];
            return pool <= 0 ? 0 : restored / (double)pool * IncursionTuning.RepairPerPool;
        }

        /// <summary>A ship's points shared evenly by the players aboard it (the pilot and the station crew).</summary>
        public static Dictionary<string, double> ShareAboard(double points, IReadOnlyCollection<string> players)
        {
            var distinct = players.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            return distinct.Count == 0 ? new Dictionary<string, double>() : distinct.ToDictionary(x => x, _ => points / distinct.Count);
        }

        /// <summary>Adds points earned at a tier to a player's contribution.</summary>
        public static void AddPoints(IncursionContribution contribution, int tier, double points)
        {
            if (points <= 0)
                return;

            contribution.Points.TryGetValue(tier, out var current);
            contribution.Points[tier] = current + points;
        }

        public static bool QualifiesForRegionReward(IncursionContribution contribution) =>
            contribution != null && contribution.TotalPoints() >= IncursionTuning.MinimumRegionPoints;

        // Payout

        /// <summary>
        /// What clearing the region pays one player: per tier, credits for the points paid and a pull per
        /// <see cref="IncursionTuning.PointsPerPull"/>; with enough points, the region reward and a chance at a Ship
        /// Schematic of the invader's maker. <paramref name="next"/> returns 0 to its argument minus 1.
        /// </summary>
        public static IncursionPayout BuildPayout(IncursionContribution contribution, IncursionRegion region, IncursionInvader invader, Func<int, int> next)
        {
            var payout = new IncursionPayout { Points = contribution.TotalPoints(), RegionName = region.Name };
            var credits = 0.0;
            foreach (var (tier, points) in contribution.Points.OrderBy(x => x.Key))
            {
                var t = Math.Clamp(tier, 1, 6);
                var paid = Math.Min(points, IncursionTuning.MaximumPaidPoints);
                credits += paid * IncursionTuning.CreditsPerPoint[t];
                var pulls = Math.Min(IncursionTuning.MaximumPulls, (int)Math.Floor(paid / IncursionTuning.PointsPerPull));
                AddPulls(payout, EliteTable(t), pulls);
            }

            if (QualifiesForRegionReward(contribution))
            {
                var tier = Math.Clamp(region.Tier, 1, 6);
                credits += IncursionTuning.RegionCredits[tier];
                AddPulls(payout, EliteTable(tier), IncursionTuning.RegionPulls);
                if (next(100) < IncursionTuning.SchematicPercent)
                {
                    var schematics = InvaderSchematics(invader, tier);
                    if (schematics.Count > 0)
                        payout.Items.Add(schematics[next(schematics.Count)]);
                }
            }

            payout.Credits = (int)Math.Round(credits, MidpointRounding.AwayFromZero);
            return payout;
        }

        public static string EliteTable(int tier) => $"SPACE_ELITE_T{Math.Clamp(tier, 1, 6)}";

        /// <summary>The Ship Schematics of the invader's maker up to a tier (Wild Space only ones left out).</summary>
        public static List<string> InvaderSchematics(IncursionInvader invader, int tier)
        {
            var maker = GetInvader(invader)?.Maker ?? ShipManufacturer.Invalid;
            var makers = RevampShipCatalog.Hulls.ToDictionary(x => x.Tag, x => x.Manufacturer);
            return RevampRewardCatalog.ShipSchematics
                .Where(x => !x.WildSpaceOnly && x.Tier <= Math.Min(tier, 5) && makers.TryGetValue(x.ShipTag, out var m) && m == maker)
                .Select(x => x.Resref)
                .ToList();
        }

        private static void AddPulls(IncursionPayout payout, string table, int pulls)
        {
            if (pulls <= 0)
                return;

            payout.Pulls.TryGetValue(table, out var current);
            payout.Pulls[table] = current + pulls;
        }

        /// <summary>Hours and minutes, for player messages.</summary>
        public static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            return remaining.TotalHours >= 1 ? $"{(int)remaining.TotalHours} h {remaining.Minutes} min" : $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes))} min";
        }
    }
}
