using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The Rift Run rules (plan section 31), pure: the keys, who comes along, each chamber's waves, the run's steps and
    /// timer, and the payout. Numbers come from <see cref="RiftRunTuning"/>.
    /// </summary>
    public static class RiftRunRules
    {
        /// <summary>Seconds after the pocket opens before ships still arriving count as gone.</summary>
        public const int ArrivalGraceSeconds = 15;

        /// <summary>
        /// Whether a pilot is out of the run: logged out, no longer flying, or in an area other than the pocket. A pilot
        /// between areas is still loading into the pocket and stays in the run however long the load takes.
        /// </summary>
        public static bool HasLeftPocket(bool pilotValid, bool inSpace, bool betweenAreas, bool inPocket) =>
            !pilotValid || !inSpace || (!betweenAreas && !inPocket);

        /// <summary>
        /// A pilot logged out (their creature is gone): their ship waits in the pocket for them to rejoin while the run is
        /// open (W11k). Does nothing for a pilot the run does not know.
        /// </summary>
        public static void MarkAway(RiftRun run, uint pilot)
        {
            if (!run.ShipIds.Remove(pilot, out var shipId))
                return;

            run.Origins.Remove(pilot, out var origin);
            run.AwayShips[shipId] = origin;
        }

        /// <summary>
        /// Whether a ship whose pilot logged out may rejoin its run: the run is still open (not cleared and returning, not
        /// closing). A closed run keeps the shipped behaviour: the ship goes back to its dock when the pocket closes.
        /// </summary>
        public static bool CanRejoin(RiftRun run, string shipId) =>
            run != null && !string.IsNullOrEmpty(shipId) && run.AwayShips.ContainsKey(shipId) &&
            run.Phase is not (RiftRunPhase.Returning or RiftRunPhase.Closing);

        /// <summary>
        /// The pilot is back aboard their ship in the pocket: they count as one of its ships again, so they fight on and
        /// keep their payout share, and the run returns them where they opened it.
        /// </summary>
        public static bool Rejoin(RiftRun run, uint pilot, string shipId)
        {
            if (!CanRejoin(run, shipId))
                return false;

            run.AwayShips.Remove(shipId, out var origin);
            if (!run.Pilots.Contains(pilot))
                run.Pilots.Add(pilot);
            run.ShipIds[pilot] = shipId;
            if (origin != null)
                run.Origins[pilot] = origin;
            return true;
        }

        public static RiftKeyDetail FindKey(string resref) =>
            RiftRunTuning.Keys.FirstOrDefault(x => x.Resref == resref);

        /// <summary>The key a source of this tier gives: tiers 1-5 their own; tier 6 and Wild Space the tier 5.5 key.</summary>
        public static RiftKeyDetail KeyForTier(int tier, bool wildSpace = false)
        {
            if (wildSpace || tier >= 6)
                return RiftRunTuning.Keys.Single(x => x.TierTenths == 55);

            return RiftRunTuning.Keys.Single(x => x.TierTenths == Math.Clamp(tier, 1, 5) * 10);
        }

        /// <summary>The next tier's key (tier 5 leads to 5.5), or null after 5.5.</summary>
        /// <summary>Whether an area resref is one of the pocket templates (every key tier's and the tier 5.5 one).</summary>
        public static bool IsPocketTemplate(string resref) =>
            !string.IsNullOrEmpty(resref) && RiftRunTuning.Keys.Any(x => x.Template == resref);

        /// <summary>Where a pocket's chamber stands when its template has no waypoint for it.</summary>
        public static (float X, float Y, float Z) RoomPoint(string template, int room)
        {
            var points = RiftRunTuning.TemplateRoomPoints.TryGetValue(template ?? string.Empty, out var own)
                ? own
                : RiftRunTuning.RoomPoints;
            return points[Math.Clamp(room, 0, points.Count - 1)];
        }

        public static RiftKeyDetail NextKey(RiftKeyDetail key) =>
            key == null ? null : RiftRunTuning.Keys.Where(x => x.TierTenths > key.TierTenths).OrderBy(x => x.TierTenths).FirstOrDefault();

        public static RiftRunRefusal GetStartRefusal(RiftKeyDetail key, bool piloting, bool cloaked, bool inCombat, bool inRun, int openRuns)
        {
            if (key == null)
                return RiftRunRefusal.UnknownKey;
            if (!piloting)
                return RiftRunRefusal.NotPiloting;
            if (inRun)
                return RiftRunRefusal.AlreadyInRun;
            if (cloaked)
                return RiftRunRefusal.Cloaked;
            if (inCombat)
                return RiftRunRefusal.InCombat;
            if (openRuns >= RiftRunTuning.MaxRuns)
                return RiftRunRefusal.TooManyRuns;

            return RiftRunRefusal.None;
        }

        public static string Message(RiftRunRefusal refusal) => refusal switch
        {
            RiftRunRefusal.NotPiloting => "A Rift Key only works aboard a ship you pilot, out in space.",
            RiftRunRefusal.InCombat => "The rift will not open while you are in combat.",
            RiftRunRefusal.AlreadyInRun => "You are already inside a rift pocket.",
            RiftRunRefusal.TooManyRuns => "The rift will not hold another pocket open right now. Try again in a few minutes.",
            RiftRunRefusal.UnknownKey => "This key has a configuration problem. Please inform a DM.",
            RiftRunRefusal.Cloaked => "Decloak before you open a rift.",
            _ => string.Empty,
        };

        /// <summary>
        /// Who enters with the key user: the nearest party ships within <see cref="RiftRunTuning.JoinMeters"/>, up to
        /// <see cref="RiftRunTuning.MaxShips"/> ships in all, the user first.
        /// </summary>
        public static List<T> PickParty<T>(T user, IEnumerable<(T Ship, float Distance)> partyShips)
        {
            var party = new List<T> { user };
            party.AddRange(partyShips
                .Where(x => !EqualityComparer<T>.Default.Equals(x.Ship, user) && x.Distance <= RiftRunTuning.JoinMeters)
                .OrderBy(x => x.Distance)
                .Take(RiftRunTuning.MaxShips - 1)
                .Select(x => x.Ship));
            return party;
        }

        /// <summary>
        /// The waves of a chamber for a party of this many ships: each extra ship adds one ship to every wave. The Rift
        /// Raiders come one at a time.
        /// </summary>
        public static List<List<string>> RoomWaves(RiftKeyDetail key, int room, int ships)
        {
            var detail = RiftRunTuning.Rooms[Math.Clamp(room, 0, RiftRunTuning.Rooms.Count - 1)];
            var waves = key.CreaturePrefix == "rr"
                ? RiftRunTuning.RiftRaiderRooms[Math.Clamp(room, 0, RiftRunTuning.RiftRaiderRooms.Count - 1)].ToList()
                : detail.Waves.ToList();
            var extras = detail.Extras.Take(Math.Clamp(ships, 1, RiftRunTuning.MaxShips) - 1).ToList();
            return waves.Select(w => w.Concat(extras).ToList()).ToList();
        }

        /// <summary>The creature tags of one wave.</summary>
        public static List<string> WaveTags(RiftKeyDetail key, int room, int wave, int ships)
        {
            var waves = RoomWaves(key, room, ships);
            return wave < 0 || wave >= waves.Count ? new List<string>() : waves[wave].Select(h => key.CreaturePrefix + h).ToList();
        }

        public static int SecondsLeft(DateTime startedAt, DateTime now) =>
            Math.Max(0, RiftRunTuning.TimerSeconds - (int)Math.Floor((now - startedAt).TotalSeconds));

        /// <summary>
        /// One tick of a run: moves it on and says what the engine must do. Waves come one at a time; a cleared chamber
        /// pulls the party into the next; clearing the last finishes the run; the timer running out collapses the pocket.
        /// </summary>
        public static RiftRunAction Step(RiftRun run, int enemiesAlive, int shipsLeft, DateTime now)
        {
            switch (run.Phase)
            {
                case RiftRunPhase.Closing:
                    return RiftRunAction.None;
                case RiftRunPhase.Returning:
                    if (now < run.NextAt)
                        return RiftRunAction.None;
                    run.Phase = RiftRunPhase.Closing;
                    return RiftRunAction.ReturnHome;
            }

            if (shipsLeft <= 0 && (now - run.StartedAt).TotalSeconds >= ArrivalGraceSeconds)
            {
                run.Phase = RiftRunPhase.Closing;
                run.Failed = true;
                return RiftRunAction.Abandon;
            }

            var secondsLeft = SecondsLeft(run.StartedAt, now);
            if (run.Phase == RiftRunPhase.Fighting && enemiesAlive <= 0 && secondsLeft > 0)
            {
                var waves = RoomWaves(run.Key, run.Room, run.Ships).Count;
                if (run.Wave + 1 < waves)
                {
                    run.Wave++;
                    run.Phase = RiftRunPhase.Waiting;
                    run.NextAt = now.AddSeconds(RiftRunTuning.WaveDelaySeconds);
                    return RiftRunAction.None;
                }

                if (run.Room + 1 < RiftRunTuning.Rooms.Count)
                {
                    run.Phase = RiftRunPhase.Advancing;
                    run.NextAt = now.AddSeconds(RiftRunTuning.RoomDelaySeconds);
                    return RiftRunAction.RoomCleared;
                }

                run.Phase = RiftRunPhase.Returning;
                run.FinishedSecondsLeft = secondsLeft;
                run.NextAt = now.AddSeconds(RiftRunTuning.ReturnDelaySeconds);
                return RiftRunAction.Complete;
            }

            if (secondsLeft <= 0)
            {
                run.Phase = RiftRunPhase.Closing;
                run.Failed = true;
                return RiftRunAction.Collapse;
            }

            switch (run.Phase)
            {
                case RiftRunPhase.Waiting when now >= run.NextAt:
                    run.Phase = RiftRunPhase.Fighting;
                    return RiftRunAction.SpawnWave;
                case RiftRunPhase.Advancing when now >= run.NextAt:
                    run.Room++;
                    run.Wave = 0;
                    run.Phase = RiftRunPhase.Waiting;
                    run.NextAt = now.AddSeconds(RiftRunTuning.WaveDelaySeconds);
                    return RiftRunAction.PullToNextRoom;
                default:
                    return RiftRunAction.None;
            }
        }

        /// <summary>The timer warnings due now that the run has not given yet; marks them given.</summary>
        public static List<int> DueWarnings(RiftRun run, int secondsLeft)
        {
            var due = RiftRunTuning.WarningSeconds.Where(x => secondsLeft <= x && !run.Warned.Contains(x)).ToList();
            foreach (var warning in due)
                run.Warned.Add(warning);

            // Only the most urgent one is worth saying when several come due at once.
            return due.Count == 0 ? due : new List<int> { due.Min() };
        }

        /// <summary>The payout multiplier for the fraction of the timer left.</summary>
        public static double Multiplier(int secondsLeft) =>
            RiftRunTuning.MinimumMultiplier + RiftRunTuning.TimeSlope * Math.Clamp(secondsLeft / (double)RiftRunTuning.TimerSeconds, 0, 1);

        /// <summary>A finished run's credits for one ship.</summary>
        public static int Credits(RiftKeyDetail key, int secondsLeft) =>
            (int)Math.Round(key.BaseCredits * Multiplier(secondsLeft), MidpointRounding.AwayFromZero);

        /// <summary>A finished run's item pulls for one ship: one more with a third of the timer left.</summary>
        public static int Pulls(int secondsLeft) =>
            RiftRunTuning.RunPulls + (secondsLeft >= RiftRunTuning.BonusPullFraction * RiftRunTuning.TimerSeconds ? 1 : 0);

        /// <summary>A ship's credits shared evenly by the players aboard; the remainder goes to the first (the pilot).</summary>
        public static List<(string PlayerId, int Credits)> ShareCredits(int credits, IReadOnlyList<string> players)
        {
            var distinct = players.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
            if (distinct.Count == 0)
                return new List<(string PlayerId, int Credits)>();

            var each = credits / distinct.Count;
            var shares = distinct.Select(x => (x, each)).ToList();
            shares[0] = (shares[0].x, each + credits - each * distinct.Count);
            return shares;
        }

        /// <summary>Where the ship at <paramref name="index"/> arrives in a chamber, around its arrival point.</summary>
        public static (float X, float Y) ArrivalOffset(int index) =>
            index == 0 ? (0f, 0f) : ((float)(Math.Cos(index * 2.1) * 6), (float)(Math.Sin(index * 2.1) * 6));

        /// <summary>Where wave ship <paramref name="index"/> of <paramref name="count"/> spawns, relative to the chamber's arrival point.</summary>
        public static (float X, float Y) EnemyOffset(int index, int count)
        {
            var angle = 2.0 * Math.PI * index / Math.Max(1, count);
            return (RiftRunTuning.EnemyOffsetMeters + (float)(Math.Cos(angle) * RiftRunTuning.EnemyRingMeters),
                (float)(Math.Sin(angle) * RiftRunTuning.EnemyRingMeters));
        }

        public static string FormatTimer(int secondsLeft) => $"{secondsLeft / 60}:{secondsLeft % 60:00}";
    }
}
