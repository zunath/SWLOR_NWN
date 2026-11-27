using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DuelService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TournamentService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    public static partial class Tournaments
    {
        private const float MonitorSeconds = 3.0f;

        /// <summary>How far outside the ring's edge a barred player is put.</summary>
        private const float EjectMarginMeters = 2.0f;

        private static IDisposable _monitor;

        // Restart

        /// <summary>
        /// Settles what a restart left behind. Every duel is gone, so a match in progress is replayed and a called match
        /// starts its five minutes again. A tournament whose settlement was under way finishes it, a tournament whose
        /// prize was never held is dropped, and a tournament whose city is gone returns its fees.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Recover()
        {
            var now = DateTime.UtcNow;
            foreach (var tournament in Search(new DBQuery<CityTournament>()))
            {
                try
                {
                    var city = DB.Get<WorldProperty>(tournament.CityId);
                    if (city == null)
                    {
                        Settle(tournament, TournamentOutcome.CityRemoved);
                        continue;
                    }

                    switch (tournament.State)
                    {
                        case TournamentState.Settling:
                            Settle(tournament, tournament.Outcome);
                            break;
                        case TournamentState.SignUp:
                            if (tournament.Entrants.Count == 0 && !city.Escrow.ContainsKey(tournament.Id))
                                DB.Delete<CityTournament>(tournament.Id);
                            break;
                        case TournamentState.Running:
                            if (TournamentRules.ReplayInProgress(tournament, now) > 0)
                                DB.Set(tournament);
                            EnsureMonitor();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Failed to recover tournament '{tournament.Id}': {ex}");
                }
            }
        }

        // The monitor

        private static void EnsureMonitor()
        {
            _monitor ??= Scheduler.ScheduleRepeating(Tick, TimeSpan.FromSeconds(MonitorSeconds));
        }

        /// <summary>
        /// Runs every few seconds while a tournament is running: starts a called match once both duelists stand in the
        /// ring, settles a match whose duelist did not show in time, and moves barred players out of the ring.
        /// </summary>
        private static void Tick()
        {
            var running = Search(new DBQuery<CityTournament>()
                .AddFieldSearch(nameof(CityTournament.State), (int)TournamentState.Running));
            if (running.Count == 0)
            {
                _monitor?.Dispose();
                _monitor = null;
                return;
            }

            foreach (var tournament in running)
            {
                try
                {
                    Run(tournament, DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Failed to run tournament '{tournament.Id}': {ex}");
                }
            }
        }

        private static void Run(CityTournament tournament, DateTime now)
        {
            var ring = Property.GetPlaceableByPropertyId(tournament.RingPropertyId);
            if (!GetIsObjectValid(ring))
                return;

            var labelsChanged = false;
            foreach (var entrant in tournament.Entrants)
            {
                labelsChanged |= RefreshLabel(tournament, entrant.PlayerId);
            }

            if (labelsChanged)
                DB.Set(tournament);

            EjectBarred(tournament, ring);

            var match = TournamentRules.GetCurrentMatch(tournament);
            if (match == null)
                return;

            var first = GetObjectByUUID(match.PlayerAId);
            var second = GetObjectByUUID(match.PlayerBId);

            if (match.State == TournamentMatchState.Active)
            {
                // A duel that is gone without a result leaves the match stuck, so it is replayed.
                if (Duels.GetDuel(first)?.TournamentId != tournament.Id && Duels.GetDuel(second)?.TournamentId != tournament.Id)
                {
                    TournamentRules.Call(match, now);
                    DB.Set(tournament);
                }

                return;
            }

            if (match.State != TournamentMatchState.Called)
                return;

            var firstPresent = IsPresent(ring, first);
            var secondPresent = IsPresent(ring, second);
            if (firstPresent && secondPresent)
            {
                if (Duels.GetDuel(first) == null && Duels.GetDuel(second) == null)
                {
                    TournamentRules.MarkActive(match);
                    DB.Set(tournament);
                    Duels.StartTournamentDuel(first, second, ring, tournament.Id);
                }

                return;
            }

            var decision = TournamentRules.EvaluateNoShow(match, firstPresent, secondPresent, now);
            if (decision == NoShowDecision.None)
                return;

            TournamentRules.ApplyNoShow(match, decision);
            TellNoShow(match, decision);
            FinishMatch(tournament, match);
        }

        /// <summary>Whether a duelist is online, alive and standing in the ring.</summary>
        private static bool IsPresent(uint ring, uint duelist)
        {
            return GetIsObjectValid(duelist) &&
                   PvpCombat.IsPlayer(duelist) &&
                   !GetIsDead(duelist) &&
                   Duels.IsInsideRing(ring, duelist);
        }

        /// <summary>Moves each barred player who stands in the ring to the nearest walkable spot just outside it.</summary>
        private static void EjectBarred(CityTournament tournament, uint ring)
        {
            foreach (var playerId in tournament.BarredPlayerIds)
            {
                var player = GetObjectByUUID(playerId);
                if (!PvpCombat.IsPlayer(player) || !Duels.IsInsideRing(ring, player))
                    continue;

                var center = GetPosition(ring);
                var position = GetPosition(player);
                var dx = position.X - center.X;
                var dy = position.Y - center.Y;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                var baseAngle = distance < 0.1f ? 0f : MathF.Atan2(dy, dx);
                var radius = DuelRules.RingRadiusMeters + EjectMarginMeters;

                // The outward direction first, then around the ring, until a spot is walkable.
                for (var step = 0; step < 8; step++)
                {
                    var angle = baseAngle + step * MathF.PI / 4f;
                    var spot = Vector3(center.X + MathF.Cos(angle) * radius, center.Y + MathF.Sin(angle) * radius, position.Z);
                    var location = Location(GetArea(ring), spot, GetFacing(player));
                    if (!IsWalkable(location))
                        continue;

                    AssignCommand(player, () =>
                    {
                        ClearAllActions();
                        ActionJumpToLocation(location);
                    });
                    SendMessageToPC(player, ColorToken.Red("You are barred from the Dueling Ring until the tournament ends."));
                    break;
                }
            }
        }

        private static bool IsWalkable(Location location)
        {
            var material = GetSurfaceMaterial(location);
            return material > 0 &&
                   int.TryParse(Get2DAString("surfacemat", "Walk", material), out var walkable) &&
                   walkable == 1;
        }

        // Duel results

        /// <summary>
        /// A tournament duel was voided by someone else joining the fight. The interrupter is barred from the ring until
        /// the tournament ends, and the match is replayed, or goes to an official on its third interruption.
        /// </summary>
        /// <param name="record">The voided duel.</param>
        /// <param name="interrupter">Whoever dealt the blow, or invalid when none can be named.</param>
        public static void OnDuelInterrupted(DuelRecord record, uint interrupter)
        {
            if (string.IsNullOrEmpty(record.TournamentId))
                return;

            var tournament = Get(record.TournamentId);
            var match = tournament == null ? null : FindActiveMatch(tournament, record);
            if (match == null)
                return;

            var interrupterId = PvpCombat.IsPlayer(interrupter) ? GetObjectUUID(interrupter) : string.Empty;
            if (TournamentRules.ShouldBar(match, interrupterId) && TournamentRules.Bar(tournament, interrupterId))
            {
                SendMessageToPC(interrupter, ColorToken.Red("You interrupted a tournament match. You are barred from the Dueling Ring until the tournament ends."));
                Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(interrupter)} interrupted a match of tournament '{tournament.Id}' and was barred from its ring.");
            }

            var state = TournamentRules.RecordInterruption(match, DateTime.UtcNow);
            DB.Set(tournament);
            TellInterruption(tournament, match, state);
        }

        /// <summary>A tournament duel ended. A win or forfeit settles the match; a draw replays it.</summary>
        public static void OnDuelFinished(DuelRecord record, DuelOutcome outcome)
        {
            var tournament = Get(record.TournamentId);
            var match = tournament == null ? null : FindActiveMatch(tournament, record);
            if (match == null)
                return;

            RefreshLabel(tournament, match.PlayerAId);
            RefreshLabel(tournament, match.PlayerBId);

            if (!TournamentRules.ApplyDuelOutcome(match, outcome, DateTime.UtcNow))
            {
                DB.Set(tournament);
                TellToDuelists(tournament, match, $"The match ended in a draw, so it will be replayed. Stand in the Dueling Ring within {TournamentRules.NoShowMinutes:0} minutes.");
                return;
            }

            FinishMatch(tournament, match);
        }

        private static TournamentMatch FindActiveMatch(CityTournament tournament, DuelRecord record)
        {
            return tournament.Matches.FirstOrDefault(x =>
                x.State == TournamentMatchState.Active &&
                x.PlayerAId == record.ChallengerPlayerId &&
                x.PlayerBId == record.TargetPlayerId);
        }

        /// <summary>
        /// Saves a finished match, posts its result, and moves the bracket on. The last match ends the tournament: a
        /// champion is paid the prize, and a final nobody won is called off.
        /// </summary>
        private static void FinishMatch(CityTournament tournament, TournamentMatch match, string officialPlayerId = null)
        {
            TournamentRules.Refresh(tournament.Matches);
            Announce(tournament, TournamentText.BuildMatchText(tournament, match), officialPlayerId);

            if (!TournamentRules.IsFinished(tournament))
            {
                DB.Set(tournament);
                return;
            }

            var champion = TournamentRules.GetChampionId(tournament);
            if (string.IsNullOrEmpty(champion))
            {
                Settle(tournament, TournamentOutcome.Cancelled, TournamentText.BuildNoChampionText());
                return;
            }

            tournament.ChampionPlayerId = champion;
            Settle(tournament, TournamentOutcome.Completed);
        }

        /// <summary>Keeps an entrant's label as strangers see them now. Returns true when it changed.</summary>
        private static bool RefreshLabel(CityTournament tournament, string playerId)
        {
            var entrant = TournamentRules.GetEntrant(tournament, playerId);
            var online = string.IsNullOrEmpty(playerId) ? OBJECT_INVALID : GetObjectByUUID(playerId);
            if (entrant == null || !PvpCombat.IsPlayer(online))
                return false;

            var label = Disguise.GetDisplayDescriptor(online);
            if (label == entrant.Label)
                return false;

            entrant.Label = label;
            return true;
        }

        // Messages

        private static void TellMatchCalled(CityTournament tournament, TournamentMatch match)
        {
            var round = TournamentRules.GetRoundName(match.Round, TournamentRules.GetRoundCount(tournament.Matches));
            foreach (var (playerId, opponentId) in new[] { (match.PlayerAId, match.PlayerBId), (match.PlayerBId, match.PlayerAId) })
            {
                var duelist = GetObjectByUUID(playerId);
                if (!GetIsObjectValid(duelist))
                    continue;

                SendMessageToPC(duelist, ColorToken.Green(
                    $"Your match in {round} of the tournament is called against {GetName(duelist, tournament, opponentId)}. " +
                    $"Stand in the Dueling Ring within {TournamentRules.NoShowMinutes:0} minutes or you forfeit."));
            }
        }

        private static void TellNoShow(TournamentMatch match, NoShowDecision decision)
        {
            foreach (var (playerId, missed) in new[]
                     {
                         (match.PlayerAId, decision is NoShowDecision.PlayerAForfeits or NoShowDecision.BothForfeit),
                         (match.PlayerBId, decision is NoShowDecision.PlayerBForfeits or NoShowDecision.BothForfeit)
                     })
            {
                var duelist = GetObjectByUUID(playerId);
                if (!GetIsObjectValid(duelist))
                    continue;

                SendMessageToPC(duelist, missed
                    ? ColorToken.Red("You did not reach the Dueling Ring in time, so you forfeited the match.")
                    : ColorToken.Green("Your opponent did not reach the Dueling Ring in time, so you advance."));
            }
        }

        private static void TellInterruption(CityTournament tournament, TournamentMatch match, TournamentMatchState state)
        {
            if (state == TournamentMatchState.AwaitingOfficial)
            {
                TellToDuelists(tournament, match,
                    $"Your match was interrupted {TournamentRules.InterruptionsBeforeOfficial} times. An official will decide the winner.");

                var official = GetObjectByUUID(tournament.OfficialPlayerId);
                if (GetIsObjectValid(official))
                    SendMessageToPC(official, ColorToken.Green("A tournament match was interrupted three times. Use the Dueling Ring to pick its winner."));
                return;
            }

            TellToDuelists(tournament, match,
                $"Your match was interrupted ({match.Interruptions} of {TournamentRules.InterruptionsBeforeOfficial}), so it will be replayed. " +
                $"Stand in the Dueling Ring within {TournamentRules.NoShowMinutes:0} minutes.");
        }

        private static void TellToDuelists(CityTournament tournament, TournamentMatch match, string message)
        {
            foreach (var playerId in new[] { match.PlayerAId, match.PlayerBId })
            {
                var duelist = GetObjectByUUID(playerId);
                if (GetIsObjectValid(duelist))
                    SendMessageToPC(duelist, ColorToken.Green(message));
            }
        }
    }
}
