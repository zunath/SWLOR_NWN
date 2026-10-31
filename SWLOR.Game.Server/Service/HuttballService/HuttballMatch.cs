using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// The state and rules of one Huttball match. It knows nothing about the game world: the Huttball service
    /// maps squares onto the battle grid, moves creatures, and announces what each command returns.
    /// </summary>
    public class HuttballMatch
    {
        private const int MaxLogLines = 40;

        private readonly IHuttballDice _dice;
        private readonly List<HuttballPlayer> _players = new();
        private readonly List<string> _log = new();
        private readonly Dictionary<HuttballTeam, int> _score = new() { [HuttballTeam.Home] = 0, [HuttballTeam.Away] = 0 };
        private readonly Dictionary<HuttballTeam, int> _bribes = new()
        {
            [HuttballTeam.Home] = HuttballRules.DefaultBribes,
            [HuttballTeam.Away] = HuttballRules.DefaultBribes,
        };

        // Re-rolls and medics start at none; the DM hands them out.
        private readonly Dictionary<HuttballTeam, int> _rerolls = new() { [HuttballTeam.Home] = 0, [HuttballTeam.Away] = 0 };
        private readonly Dictionary<HuttballTeam, int> _medics = new() { [HuttballTeam.Home] = 0, [HuttballTeam.Away] = 0 };
        private readonly Dictionary<HuttballTeam, HuttballPlayer> _captains = new() { [HuttballTeam.Home] = null, [HuttballTeam.Away] = null };

        // Dice rolled so far, so a command can tell whether it rolled any.
        private int _rollCount;

        // The last command that rolled dice, which its team may still re-roll: the state before it and how to
        // run it again. Any other command that changes the match clears it.
        private (HuttballTeam Team, HuttballMatchSnapshot Before, Func<HuttballActionResult> Run)? _rerollable;

        public HuttballMatch(int width, int length, IHuttballDice dice)
        {
            if (width < 2 || length < 4)
                throw new ArgumentException("A pitch needs at least 2 squares across and 4 long.");

            Width = width;
            Length = length;
            _dice = dice;
            Phase = HuttballPhase.Setup;
            Half = 1;
            Turn = 1;
            ActiveTeam = GetFirstTeam(1);
        }

        public int Width { get; }
        public int Length { get; }
        public HuttballPhase Phase { get; private set; }
        public int Half { get; private set; }

        /// <summary>
        /// The turn of the current half, 1 through <see cref="HuttballRules.TurnsPerHalf"/>. Both teams play each turn.
        /// </summary>
        public int Turn { get; private set; }

        public HuttballTeam ActiveTeam { get; private set; }

        /// <summary>
        /// The player holding the ball, or null when it lies on the pitch or no drive is running.
        /// </summary>
        public HuttballPlayer BallCarrier { get; private set; }

        /// <summary>
        /// The square of a loose ball, or null when someone holds it or no drive is running.
        /// </summary>
        public HuttballSquare? LooseBall { get; private set; }

        public IReadOnlyList<HuttballPlayer> Players => _players;
        public IReadOnlyList<string> Log => _log;

        public int GetScore(HuttballTeam team) => _score[team];
        public int GetBribes(HuttballTeam team) => _bribes[team];

        public void SetBribes(HuttballTeam team, int bribes)
        {
            _bribes[team] = Math.Max(0, bribes);
        }

        public int GetRerolls(HuttballTeam team) => _rerolls[team];
        public int GetMedics(HuttballTeam team) => _medics[team];

        public void SetRerolls(HuttballTeam team, int rerolls)
        {
            _rerolls[team] = Math.Max(0, rerolls);
        }

        public void SetMedics(HuttballTeam team, int medics)
        {
            _medics[team] = Math.Max(0, medics);
        }

        /// <summary>
        /// Returns true when the team may re-roll its last action now: it was the last command, it rolled dice, and
        /// the team has a re-roll left.
        /// </summary>
        public bool CanReroll(HuttballTeam team)
        {
            return _rerollable?.Team == team && _rerolls[team] > 0;
        }

        /// <summary>
        /// Returns true when the team has a medic and a teammate injured, severely injured, or maimed.
        /// </summary>
        public bool CanUseMedic(HuttballTeam team)
        {
            return _medics[team] > 0 && _players.Any(p => p.Team == team && IsTreatable(p.Status));
        }

        private static bool IsTreatable(HuttballPlayerStatus status)
        {
            return status is HuttballPlayerStatus.Injured or HuttballPlayerStatus.SeverelyInjured or HuttballPlayerStatus.Maimed;
        }

        public static HuttballTeam GetOpponent(HuttballTeam team)
        {
            return team == HuttballTeam.Home ? HuttballTeam.Away : HuttballTeam.Home;
        }

        /// <summary>
        /// Home kicks off the first half, Away the second.
        /// </summary>
        public static HuttballTeam GetFirstTeam(int half)
        {
            return half == 1 ? HuttballTeam.Home : HuttballTeam.Away;
        }

        public HuttballPlayer GetPlayer(uint creature)
        {
            return _players.FirstOrDefault(p => p.Creature == creature);
        }

        /// <summary>
        /// Returns the player standing or lying on a square, if any.
        /// </summary>
        public HuttballPlayer GetPlayerAt(HuttballSquare square)
        {
            return _players.FirstOrDefault(p => p.IsOnPitch && p.Square == square);
        }

        public bool IsOnPitch(HuttballSquare square)
        {
            return square.X >= 0 && square.X < Width && square.Y >= 0 && square.Y < Length;
        }

        /// <summary>
        /// The row a team must carry the ball into: the far end from the end zone it defends.
        /// </summary>
        public int GetScoringRow(HuttballTeam team)
        {
            return team == HuttballTeam.Home ? Length - 1 : 0;
        }

        /// <summary>
        /// Home sets up in the southern half and Away in the northern half.
        /// </summary>
        public bool IsInOwnHalf(HuttballTeam team, HuttballSquare square)
        {
            return team == HuttballTeam.Home
                ? square.Y < Length / 2
                : square.Y >= Length - Length / 2;
        }

        /// <summary>
        /// The player who runs the team's drives and turns when no DM does, or null.
        /// </summary>
        public HuttballPlayer GetCaptain(HuttballTeam team) => _captains[team];

        public bool IsCaptain(uint creature)
        {
            var player = GetPlayer(creature);
            return player != null && _captains[player.Team] == player;
        }

        /// <summary>
        /// Adds a player to a team. The first player on a team who <paramref name="canCaptain"/> becomes its
        /// captain.
        /// </summary>
        public HuttballActionResult AddPlayer(uint creature, string name, HuttballTeam team, HuttballRole role, bool canCaptain = false)
        {
            if (GetPlayer(creature) != null)
                return HuttballActionResult.Fail($"{name} is already in the match.");

            var player = new HuttballPlayer(creature, name, team, role)
            {
                IsDone = true,
                CanCaptain = canCaptain,
            };
            _rerollable = null;
            _players.Add(player);

            var result = new HuttballActionResult();
            Say(result, $"{player.DisplayName} joins the {team} team.");
            if (canCaptain && _captains[team] == null)
            {
                _captains[team] = player;
                Say(result, $"{player.Name} captains the {team} team.");
            }

            return result;
        }

        /// <summary>
        /// Hands a team's captaincy to one of its players.
        /// </summary>
        public HuttballActionResult SetCaptain(uint creature)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");
            if (!player.CanCaptain)
                return HuttballActionResult.Fail($"{player.Name} can't captain a team. Only player characters can.");
            if (_captains[player.Team] == player)
                return HuttballActionResult.Fail($"{player.Name} already captains the {player.Team} team.");

            _rerollable = null;
            _captains[player.Team] = player;
            var result = new HuttballActionResult();
            Say(result, $"{player.Name} now captains the {player.Team} team.");
            return result;
        }

        public HuttballActionResult RemovePlayer(uint creature)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");

            var result = new HuttballActionResult();
            var square = player.Square;
            _rerollable = null;
            _players.Remove(player);
            Say(result, $"{player.DisplayName} leaves the match.");
            if (_captains[player.Team] == player)
            {
                // The captaincy passes to whoever joined the team next.
                var next = _players.FirstOrDefault(p => p.Team == player.Team && p.CanCaptain);
                _captains[player.Team] = next;
                if (next != null)
                    Say(result, $"{next.Name} now captains the {player.Team} team.");
            }

            if (BallCarrier == player && square != null)
            {
                BallCarrier = null;
                Say(result, "The ball comes loose!");
                Scatter(result, square.Value);
            }

            CheckTeamTurnOver(result);
            return result;
        }

        public HuttballActionResult SetRole(uint creature, HuttballRole role)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");
            if (Phase == HuttballPhase.Playing)
                return HuttballActionResult.Fail("Roles can only change between drives.");

            _rerollable = null;
            player.Role = role;
            var result = new HuttballActionResult();
            Say(result, $"{player.Name} now plays {role}.");
            return result;
        }

        /// <summary>
        /// Puts a player on a free square of their own half while the teams set up.
        /// </summary>
        public HuttballActionResult Place(uint creature, HuttballSquare square)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");
            if (Phase != HuttballPhase.Setup)
                return HuttballActionResult.Fail("Players can only take their places between drives.");
            if (player.Status != HuttballPlayerStatus.Standing)
                return HuttballActionResult.Fail($"{player.Name} is {DescribeStatus(player.Status)} and cannot take the field.");
            if (!IsOnPitch(square))
                return HuttballActionResult.Fail("That square is off the pitch.");
            if (!IsInOwnHalf(player.Team, square))
                return HuttballActionResult.Fail($"The {player.Team} team sets up in the {(player.Team == HuttballTeam.Home ? "southern" : "northern")} half.");

            var occupant = GetPlayerAt(square);
            if (occupant != null && occupant != player)
                return HuttballActionResult.Fail($"{occupant.Name} is already on that square.");

            _rerollable = null;
            player.Square = square;
            var result = new HuttballActionResult();
            result.Path.Add(square);
            return result;
        }

        /// <summary>
        /// Starts a drive with the ball on <paramref name="ballSquare"/>. A standing player on that square takes it.
        /// </summary>
        public HuttballActionResult StartDrive(HuttballSquare ballSquare)
        {
            if (Phase == HuttballPhase.Finished)
                return HuttballActionResult.Fail("The match is over.");
            if (Phase == HuttballPhase.Playing)
                return HuttballActionResult.Fail("A drive is already under way.");
            if (!IsOnPitch(ballSquare))
                return HuttballActionResult.Fail("The ball must start on the pitch.");

            var misplaced = _players
                .Where(p => p.IsOnPitch && !IsInOwnHalf(p.Team, p.Square!.Value))
                .Select(p => p.Name)
                .ToList();
            if (misplaced.Count > 0)
                return HuttballActionResult.Fail($"Not in their own half: {string.Join(", ", misplaced)}.");

            if (!_players.Any(p => p.Team == HuttballTeam.Home && p.IsOnPitch) ||
                !_players.Any(p => p.Team == HuttballTeam.Away && p.IsOnPitch))
                return HuttballActionResult.Fail("Both teams need at least one player on the pitch.");

            var result = new HuttballActionResult();
            _rerollable = null;
            Phase = HuttballPhase.Playing;
            Say(result, $"Kick-off! Half {Half}, turn {Turn}.");

            var holder = GetPlayerAt(ballSquare);
            if (holder != null && holder.Status == HuttballPlayerStatus.Standing)
            {
                BallCarrier = holder;
                LooseBall = null;
                Say(result, $"{holder.DisplayName} has the ball.");
            }
            else
            {
                BallCarrier = null;
                LooseBall = ballSquare;
                Say(result, $"The ball lands at {ballSquare}.");
            }

            StartTeamTurn(result);
            return result;
        }

        public HuttballActionResult Move(uint creature, HuttballSquare destination)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");
            if (Phase == HuttballPhase.Setup)
                return Place(creature, destination);

            return RunRerollable(player.Team, () => MoveInPlay(player, destination));
        }

        private HuttballActionResult MoveInPlay(HuttballPlayer player, HuttballSquare destination)
        {
            var error = GetActError(player);
            if (error != null)
                return HuttballActionResult.Fail(error);
            if (player.Status != HuttballPlayerStatus.Standing)
                return HuttballActionResult.Fail($"{player.Name} must stand up first.");
            if (player.MovementLeft <= 0)
                return HuttballActionResult.Fail($"{player.Name} has no movement left this turn.");
            if (!IsOnPitch(destination))
                return HuttballActionResult.Fail("That square is off the pitch.");
            if (destination == player.Square)
                return HuttballActionResult.Fail($"{player.Name} is already there.");

            var path = FindPath(player, destination);
            if (path == null)
                return HuttballActionResult.Fail($"{player.Name} cannot reach {destination} with {player.MovementLeft} movement left.");

            var result = new HuttballActionResult();
            foreach (var step in path)
            {
                var from = player.Square!.Value;
                if (!TryLeaveSquare(result, player, from))
                    break;

                player.Square = step;
                player.MovementLeft--;
                result.Path.Add(step);

                if (LooseBall == step)
                {
                    LooseBall = null;
                    BallCarrier = player;
                    Say(result, $"{player.DisplayName} scoops up the ball.");
                }

                if (CheckTouchdown(result, player))
                    return result;
            }

            if (player.Status == HuttballPlayerStatus.Standing && player.Square != null)
                Say(result, $"{player.DisplayName} moves to {player.Square.Value} ({player.MovementLeft} movement left).");

            FinishIfSpent(player);
            CheckTeamTurnOver(result);
            return result;
        }

        public HuttballActionResult StandUp(uint creature)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");

            var error = GetActError(player);
            if (error != null)
                return HuttballActionResult.Fail(error);
            if (player.Status != HuttballPlayerStatus.KnockedDown)
                return HuttballActionResult.Fail($"{player.Name} is not down.");

            var result = new HuttballActionResult();
            player.Status = HuttballPlayerStatus.Standing;
            _rerollable = null;
            player.MovementLeft = 0;
            Say(result, $"{player.DisplayName} gets back up. Standing uses this turn's movement.");
            FinishIfSpent(player);
            CheckTeamTurnOver(result);
            return result;
        }

        public HuttballActionResult Block(uint attackerCreature, uint defenderCreature)
        {
            var attacker = GetPlayer(attackerCreature);
            if (attacker == null)
                return HuttballActionResult.Fail("That creature is not in the match.");

            return RunRerollable(attacker.Team, () => BlockCore(attackerCreature, defenderCreature));
        }

        private HuttballActionResult BlockCore(uint attackerCreature, uint defenderCreature)
        {
            var attacker = GetPlayer(attackerCreature);
            var defender = GetPlayer(defenderCreature);
            var error = GetActionError(attacker, defender, mustBeStanding: true);
            if (error != null)
                return HuttballActionResult.Fail(error);

            var result = new HuttballActionResult();
            attacker.HasActed = true;

            var attackRoll = Roll(HuttballRules.D20);
            var defendRoll = Roll(HuttballRules.D20);
            var attackTotal = attackRoll + attacker.Stats.Strength;
            var defendTotal = defendRoll + defender.Stats.Strength;
            Say(result, $"{attacker.DisplayName} blocks {defender.DisplayName}: " +
                        $"{FormatRoll(attackRoll, attacker.Stats.Strength)} vs {FormatRoll(defendRoll, defender.Stats.Strength)}.");

            // Neither shoved: the two bounce off each other.
            result.Rebounded = Math.Abs(attackTotal - defendTotal) < HuttballRules.ShoveMargin;
            if (attackTotal == defendTotal)
            {
                Say(result, "Neither gives ground.");
            }
            else
            {
                var winner = attackTotal > defendTotal ? attacker : defender;
                var loser = winner == attacker ? defender : attacker;
                var margin = Math.Abs(attackTotal - defendTotal);
                Say(result, $"{winner.Name} wins by {margin}.");

                if (margin >= HuttballRules.ShoveMargin)
                    Shove(result, winner, loser);

                if (margin >= HuttballRules.ArmourBreakMargin)
                {
                    Say(result, $"Armour break on {loser.Name}!");
                    RollInjury(result, loser);
                }
                else if (loser.IsStandingOnPitch && BallCarrier == loser)
                {
                    CheckTouchdown(result, loser);
                }
            }

            FinishIfSpent(attacker);
            CheckTeamTurnOver(result);
            return result;
        }

        public HuttballActionResult DirtyMove(uint attackerCreature, uint victimCreature)
        {
            var attacker = GetPlayer(attackerCreature);
            if (attacker == null)
                return HuttballActionResult.Fail("That creature is not in the match.");

            return RunRerollable(attacker.Team, () => DirtyMoveCore(attackerCreature, victimCreature));
        }

        private HuttballActionResult DirtyMoveCore(uint attackerCreature, uint victimCreature)
        {
            var attacker = GetPlayer(attackerCreature);
            var victim = GetPlayer(victimCreature);
            var error = GetActionError(attacker, victim, mustBeStanding: false);
            if (error != null)
                return HuttballActionResult.Fail(error);
            if (victim.Status != HuttballPlayerStatus.KnockedDown)
                return HuttballActionResult.Fail($"{victim.Name} must be down for a dirty move.");

            var result = new HuttballActionResult();
            attacker.HasActed = true;
            Say(result, $"{attacker.DisplayName} puts the boot into {victim.DisplayName}!");
            RollInjury(result, victim);

            var refereeRoll = Roll(HuttballRules.D20);
            if (refereeRoll < HuttballRules.RefereeTarget)
            {
                if (_bribes[attacker.Team] > 0)
                {
                    _bribes[attacker.Team]--;
                    Say(result, $"Referee: {refereeRoll}. Spotted, but a bribe changes hands. " +
                                $"The {attacker.Team} team has {_bribes[attacker.Team]} bribe(s) left.");
                }
                else
                {
                    Say(result, $"Referee: {refereeRoll}. Spotted! {attacker.Name} is sent off until halftime.");
                    Bench(result, attacker, HuttballPlayerStatus.SentOff);
                }
            }
            else
            {
                Say(result, $"Referee: {refereeRoll}. Nobody saw a thing.");
            }

            FinishIfSpent(attacker);
            CheckTeamTurnOver(result);
            return result;
        }

        public HuttballActionResult Pass(uint throwerCreature, uint catcherCreature)
        {
            var thrower = GetPlayer(throwerCreature);
            if (thrower == null)
                return HuttballActionResult.Fail("That creature is not in the match.");

            return RunRerollable(thrower.Team, () => PassCore(throwerCreature, catcherCreature));
        }

        private HuttballActionResult PassCore(uint throwerCreature, uint catcherCreature)
        {
            var thrower = GetPlayer(throwerCreature);
            var catcher = GetPlayer(catcherCreature);
            if (thrower == null || catcher == null)
                return HuttballActionResult.Fail("Both players must be in the match.");

            var error = GetActError(thrower);
            if (error != null)
                return HuttballActionResult.Fail(error);
            if (thrower.HasActed)
                return HuttballActionResult.Fail($"{thrower.Name} has already acted this turn.");
            if (thrower.Status != HuttballPlayerStatus.Standing)
                return HuttballActionResult.Fail($"{thrower.Name} must be standing.");
            if (BallCarrier != thrower)
                return HuttballActionResult.Fail($"{thrower.Name} does not have the ball.");
            if (catcher == thrower)
                return HuttballActionResult.Fail("Pick a teammate to throw to.");
            if (catcher.Team != thrower.Team)
                return HuttballActionResult.Fail("Passes go to teammates.");
            if (!catcher.IsStandingOnPitch)
                return HuttballActionResult.Fail($"{catcher.Name} must be standing on the pitch.");

            var distance = thrower.Square!.Value.DistanceTo(catcher.Square!.Value);
            if (distance > HuttballRules.PassRange)
                return HuttballActionResult.Fail($"{catcher.Name} is {distance} squares away. Passes reach {HuttballRules.PassRange}.");

            var result = new HuttballActionResult();
            thrower.HasActed = true;
            BallCarrier = null;

            var throwRoll = Roll(HuttballRules.D20);
            var throwTotal = throwRoll + thrower.Stats.Agility;
            if (throwTotal < HuttballRules.PassTarget)
            {
                Say(result, $"{thrower.DisplayName} throws to {catcher.Name}: {FormatRoll(throwRoll, thrower.Stats.Agility)}, " +
                            $"needs {HuttballRules.PassTarget}. A wild throw!");
                Scatter(result, catcher.Square.Value);
                FinishIfSpent(thrower);
                CheckTeamTurnOver(result);
                return result;
            }

            Say(result, $"{thrower.DisplayName} throws to {catcher.Name}: {FormatRoll(throwRoll, thrower.Stats.Agility)}.");

            HuttballPlayer interceptor = null;
            var bestInterception = throwTotal;
            foreach (var square in HuttballRules.GetPassLine(thrower.Square.Value, catcher.Square.Value))
            {
                var opponent = GetPlayerAt(square);
                if (opponent == null || opponent.Team == thrower.Team || opponent.Status != HuttballPlayerStatus.Standing)
                    continue;

                var roll = Roll(HuttballRules.D20);
                var total = roll + opponent.Stats.Agility;
                Say(result, $"{opponent.DisplayName} leaps for it: {FormatRoll(roll, opponent.Stats.Agility)}.");
                if (total > bestInterception)
                {
                    bestInterception = total;
                    interceptor = opponent;
                }
            }

            if (interceptor != null)
            {
                BallCarrier = interceptor;
                Say(result, $"Intercepted by {interceptor.DisplayName}!");
                CheckTouchdown(result, interceptor);
            }
            else if (!TryCatch(result, catcher))
            {
                Scatter(result, catcher.Square.Value);
            }

            FinishIfSpent(thrower);
            CheckTeamTurnOver(result);
            return result;
        }

        /// <summary>
        /// Marks a player finished for this team turn.
        /// </summary>
        public HuttballActionResult FinishActivation(uint creature)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");
            if (Phase != HuttballPhase.Playing)
                return HuttballActionResult.Fail("No drive is under way.");
            if (player.Team != ActiveTeam)
                return HuttballActionResult.Fail($"It is the {ActiveTeam} team's turn.");
            if (player.IsDone)
                return HuttballActionResult.Fail($"{player.Name} is already done this turn.");

            var result = new HuttballActionResult();
            _rerollable = null;
            player.IsDone = true;
            Say(result, $"{player.DisplayName} is done.");
            CheckTeamTurnOver(result);
            return result;
        }

        /// <summary>
        /// Ends the active team's turn, whether or not every player has finished.
        /// </summary>
        public HuttballActionResult EndTeamTurn()
        {
            if (Phase != HuttballPhase.Playing)
                return HuttballActionResult.Fail("No drive is under way.");

            var result = new HuttballActionResult();
            _rerollable = null;
            Say(result, $"The {ActiveTeam} team's turn ends.");
            AdvanceTurn(result);
            return result;
        }

        /// <summary>
        /// Undoes the team's last action and rolls it again. The team keeps the new result: the action cannot be
        /// re-rolled a second time.
        /// </summary>
        public HuttballActionResult Reroll(HuttballTeam team)
        {
            if (_rerollable?.Team != team)
                return HuttballActionResult.Fail($"The {team} team has no roll to re-roll. Re-roll right after one of your own rolls.");
            if (_rerolls[team] <= 0)
                return HuttballActionResult.Fail($"The {team} team has no re-rolls left.");

            var (_, before, run) = _rerollable.Value;
            _rerollable = null;
            _rerolls[team]--;
            Restore(before);

            var announcement = $"The {team} team uses a re-roll ({_rerolls[team]} left)!";
            AddToLog(announcement);
            var result = run();
            _rerollable = null;
            result.Messages.Insert(0, announcement);
            return result;
        }

        /// <summary>
        /// Sends the team's medic to an injured teammate on the bench: maimed becomes a severe injury, a severe
        /// injury becomes an injury until halftime, and an injured player is patched up to rejoin at the next
        /// kick-off.
        /// </summary>
        public HuttballActionResult UseMedic(HuttballTeam team, uint creature)
        {
            var player = GetPlayer(creature);
            if (player == null)
                return HuttballActionResult.Fail("That creature is not in the match.");
            if (player.Team != team)
                return HuttballActionResult.Fail("Medics only treat their own team.");
            if (!IsTreatable(player.Status))
                return HuttballActionResult.Fail($"{player.Name} has no injury to treat.");
            if (_medics[team] <= 0)
                return HuttballActionResult.Fail($"The {team} team has no medics left.");

            _medics[team]--;
            _rerollable = null;
            var result = new HuttballActionResult();
            player.Status = player.Status switch
            {
                HuttballPlayerStatus.Maimed => HuttballPlayerStatus.SeverelyInjured,
                HuttballPlayerStatus.SeverelyInjured => HuttballPlayerStatus.Injured,
                _ => HuttballPlayerStatus.Standing,
            };
            var outcome = player.Status == HuttballPlayerStatus.Standing
                ? "patched up and ready for the next kick-off"
                : HuttballMatch.DescribeStatus(player.Status);
            Say(result, $"The {team} medic treats {player.DisplayName}: now {outcome}. {_medics[team]} medic(s) left.");
            return result;
        }

        /// <summary>
        /// Sends every player standing outside their own half to the nearest free square of it, for the next
        /// kick-off. Players already in their own half stay where they are.
        /// </summary>
        public HuttballActionResult ReturnToHalves()
        {
            if (Phase != HuttballPhase.Setup)
                return HuttballActionResult.Fail("Players return to their halves only between drives.");

            var result = new HuttballActionResult();
            var strays = _players
                .Where(p => p.IsOnPitch && !IsInOwnHalf(p.Team, p.Square!.Value))
                .OrderBy(p => p.Team == HuttballTeam.Home ? p.Square!.Value.Y : Length - p.Square!.Value.Y)
                .ToList();
            foreach (var player in strays)
            {
                var from = player.Square!.Value;
                var target = Enumerable.Range(0, Length)
                    .SelectMany(y => Enumerable.Range(0, Width).Select(x => new HuttballSquare(x, y)))
                    .Where(square => IsInOwnHalf(player.Team, square) && GetPlayerAt(square) == null)
                    .OrderBy(square => square.DistanceTo(from))
                    .ThenBy(square => Math.Abs(square.X - from.X))
                    .Cast<HuttballSquare?>()
                    .FirstOrDefault();
                player.Square = target;
            }

            if (strays.Count > 0)
                _rerollable = null;
            Say(result, "Both teams return to their own halves for the kick-off.");
            return result;
        }

        /// <summary>
        /// Returns true when the player may move now: between drives to take a place, or on their turn while
        /// standing with movement left.
        /// </summary>
        public bool CanMove(HuttballPlayer player)
        {
            if (Phase == HuttballPhase.Setup)
                return player.Status == HuttballPlayerStatus.Standing;

            return GetActError(player) == null &&
                   player.Status == HuttballPlayerStatus.Standing &&
                   player.MovementLeft > 0;
        }

        public bool CanStandUp(HuttballPlayer player)
        {
            return GetActError(player) == null && player.Status == HuttballPlayerStatus.KnockedDown;
        }

        public bool CanFinish(HuttballPlayer player)
        {
            return Phase == HuttballPhase.Playing && player.Team == ActiveTeam && !player.IsDone;
        }

        /// <summary>
        /// Returns true when the player has their action and a standing opponent next to them.
        /// </summary>
        public bool CanBlock(HuttballPlayer player)
        {
            return CanTakeAction(player) &&
                   GetAdjacentOpponents(player).Any(p => p.Status == HuttballPlayerStatus.Standing);
        }

        /// <summary>
        /// Returns true when the player has their action and a downed opponent next to them.
        /// </summary>
        public bool CanDirtyMove(HuttballPlayer player)
        {
            return CanTakeAction(player) &&
                   GetAdjacentOpponents(player).Any(p => p.Status == HuttballPlayerStatus.KnockedDown);
        }

        /// <summary>
        /// Returns true when the player has their action, holds the ball, and has a standing teammate in range.
        /// </summary>
        public bool CanPass(HuttballPlayer player)
        {
            return CanTakeAction(player) &&
                   BallCarrier == player &&
                   _players.Any(p => p != player &&
                                     p.Team == player.Team &&
                                     p.IsStandingOnPitch &&
                                     p.Square!.Value.DistanceTo(player.Square!.Value) <= HuttballRules.PassRange);
        }

        private bool CanTakeAction(HuttballPlayer player)
        {
            return GetActError(player) == null &&
                   !player.HasActed &&
                   player.Status == HuttballPlayerStatus.Standing;
        }

        private IEnumerable<HuttballPlayer> GetAdjacentOpponents(HuttballPlayer player)
        {
            return _players.Where(p => p.Team != player.Team &&
                                       p.IsOnPitch &&
                                       p.Square!.Value.IsAdjacentTo(player.Square!.Value));
        }

        /// <summary>
        /// Runs a command that rolls dice. When it succeeds and rolled at least one die during a drive, its team
        /// may re-roll it until the next command; any other successful command takes that chance away.
        /// </summary>
        private HuttballActionResult RunRerollable(HuttballTeam team, Func<HuttballActionResult> run)
        {
            var before = Capture();
            var rollsBefore = _rollCount;
            var result = run();
            if (!result.Succeeded)
                return result;

            // A touchdown or the end of a half resets the pitch, so there is nothing left to roll again.
            _rerollable = _rollCount > rollsBefore && Phase == HuttballPhase.Playing
                ? (team, before, run)
                : null;
            return result;
        }

        private HuttballMatchSnapshot Capture()
        {
            return new HuttballMatchSnapshot(
                _players.Select(p => new HuttballMatchSnapshot.PlayerState(
                    p.Creature, p.Status, p.Square, p.MovementLeft, p.HasActed, p.IsDone)).ToList(),
                Phase,
                Half,
                Turn,
                ActiveTeam,
                BallCarrier?.Creature,
                LooseBall,
                new Dictionary<HuttballTeam, int>(_score),
                new Dictionary<HuttballTeam, int>(_bribes));
        }

        private void Restore(HuttballMatchSnapshot snapshot)
        {
            foreach (var state in snapshot.Players)
            {
                var player = GetPlayer(state.Creature);
                if (player == null)
                    continue;

                player.Status = state.Status;
                player.Square = state.Square;
                player.MovementLeft = state.MovementLeft;
                player.HasActed = state.HasActed;
                player.IsDone = state.IsDone;
            }

            Phase = snapshot.Phase;
            Half = snapshot.Half;
            Turn = snapshot.Turn;
            ActiveTeam = snapshot.ActiveTeam;
            BallCarrier = snapshot.BallCarrier == null ? null : GetPlayer(snapshot.BallCarrier.Value);
            LooseBall = snapshot.LooseBall;
            foreach (var (team, score) in snapshot.Score)
                _score[team] = score;
            foreach (var (team, bribes) in snapshot.Bribes)
                _bribes[team] = bribes;
        }

        private int Roll(int sides)
        {
            _rollCount++;
            return _dice.Roll(sides);
        }

        /// <summary>
        /// Returns why a player cannot take part in the active turn, or null when they can.
        /// </summary>
        public string GetActError(HuttballPlayer player)
        {
            if (Phase == HuttballPhase.Finished)
                return "The match is over.";
            if (Phase != HuttballPhase.Playing)
                return "Wait for the DM to start the drive.";
            if (player.Team != ActiveTeam)
                return $"It is the {ActiveTeam} team's turn.";
            if (!player.IsOnPitch)
                return $"{player.Name} is {DescribeStatus(player.Status)}.";
            if (player.IsDone)
                return $"{player.Name} is done for this turn.";

            return null;
        }

        public static string DescribeStatus(HuttballPlayerStatus status)
        {
            return status switch
            {
                HuttballPlayerStatus.Standing => "off the pitch",
                HuttballPlayerStatus.KnockedDown => "knocked down",
                HuttballPlayerStatus.Injured => "injured until halftime",
                HuttballPlayerStatus.SeverelyInjured => "out for the match",
                HuttballPlayerStatus.SentOff => "sent off until halftime",
                HuttballPlayerStatus.Maimed => "maimed, out for the match",
                _ => status.ToString(),
            };
        }

        private string GetActionError(HuttballPlayer attacker, HuttballPlayer target, bool mustBeStanding)
        {
            if (attacker == null || target == null)
                return "Both players must be in the match.";

            var error = GetActError(attacker);
            if (error != null)
                return error;
            if (attacker.HasActed)
                return $"{attacker.Name} has already acted this turn.";
            if (attacker.Status != HuttballPlayerStatus.Standing)
                return $"{attacker.Name} must be standing.";
            if (target.Team == attacker.Team)
                return "Pick an opponent.";
            if (!target.IsOnPitch)
                return $"{target.Name} is not on the pitch.";
            if (mustBeStanding && target.Status != HuttballPlayerStatus.Standing)
                return $"{target.Name} is already down.";
            if (!attacker.Square!.Value.IsAdjacentTo(target.Square!.Value))
                return $"{target.Name} must be next to {attacker.Name}.";

            return null;
        }

        /// <summary>
        /// Finds the route to <paramref name="destination"/> within the player's movement that leaves the fewest
        /// squares next to standing opponents, then the shortest such route. Returns the squares after the start.
        /// </summary>
        public List<HuttballSquare> FindPath(HuttballPlayer player, HuttballSquare destination)
        {
            var start = player.Square!.Value;
            var maxSteps = player.MovementLeft;
            var bestRisk = new Dictionary<HuttballSquare, int> { [start] = 0 };
            var routes = new Dictionary<HuttballSquare, List<HuttballSquare>> { [start] = new() };
            var frontier = new List<HuttballSquare> { start };

            // Each pass extends every route by one square, so a square is first reached by its shortest route and
            // later passes only replace it with a route that leaves fewer threatened squares.
            for (var step = 0; step < maxSteps && frontier.Count > 0; step++)
            {
                var next = new List<HuttballSquare>();
                foreach (var square in frontier)
                {
                    // A safer but longer route may have replaced this square's route earlier in this pass. That
                    // route is extended next pass, when its length matches the step, so no route outgrows the limit.
                    if (routes[square].Count != step)
                        continue;

                    var risk = bestRisk[square] + (IsThreatened(player, square) ? 1 : 0);
                    foreach (var direction in HuttballRules.BounceDirections)
                    {
                        var neighbor = new HuttballSquare(square.X + direction.X, square.Y + direction.Y);
                        if (!IsOnPitch(neighbor) || neighbor == start)
                            continue;

                        var occupant = GetPlayerAt(neighbor);
                        if (occupant != null && occupant != player)
                            continue;

                        if (bestRisk.TryGetValue(neighbor, out var known) && known <= risk)
                            continue;

                        bestRisk[neighbor] = risk;
                        routes[neighbor] = new List<HuttballSquare>(routes[square]) { neighbor };
                        next.Add(neighbor);
                    }
                }

                frontier = next;
            }

            return routes.TryGetValue(destination, out var route) && route.Count > 0 ? route : null;
        }

        /// <summary>
        /// Returns true when a standing opponent is next to the square, so leaving it takes a dodge roll.
        /// </summary>
        private bool IsThreatened(HuttballPlayer player, HuttballSquare square)
        {
            return GetMarkers(player, square).Any();
        }

        private IEnumerable<HuttballPlayer> GetMarkers(HuttballPlayer player, HuttballSquare square)
        {
            return _players.Where(p => p.Team != player.Team &&
                                       p.IsStandingOnPitch &&
                                       p.Square!.Value.IsAdjacentTo(square));
        }

        /// <summary>
        /// Rolls to slip away from adjacent opponents. The opponent with the best bonus opposes: agility, or
        /// strength for a Blocker. Ties go to the mover. Returns false when the mover is knocked down.
        /// </summary>
        private bool TryLeaveSquare(HuttballActionResult result, HuttballPlayer player, HuttballSquare from)
        {
            var markers = GetMarkers(player, from).ToList();
            if (markers.Count == 0)
                return true;

            var marker = markers
                .OrderByDescending(GetMarkBonus)
                .First();
            var markBonus = GetMarkBonus(marker);
            var markStat = marker.Stats.OpposesMovementWithStrength ? "STR" : "AGI";

            var moveRoll = Roll(HuttballRules.D20);
            var markRoll = Roll(HuttballRules.D20);
            var moveTotal = moveRoll + player.Stats.Agility;
            var markTotal = markRoll + markBonus;
            if (moveTotal >= markTotal)
            {
                Say(result, $"{player.Name} slips past {marker.Name}: AGI {FormatRoll(moveRoll, player.Stats.Agility)} " +
                            $"vs {markStat} {FormatRoll(markRoll, markBonus)}.");
                return true;
            }

            Say(result, $"{marker.DisplayName} trips {player.Name}: AGI {FormatRoll(moveRoll, player.Stats.Agility)} " +
                        $"vs {markStat} {FormatRoll(markRoll, markBonus)}.");
            KnockDown(result, player);
            return false;
        }

        private static int GetMarkBonus(HuttballPlayer marker)
        {
            return marker.Stats.OpposesMovementWithStrength ? marker.Stats.Strength : marker.Stats.Agility;
        }

        private void Shove(HuttballActionResult result, HuttballPlayer winner, HuttballPlayer loser)
        {
            var from = loser.Square!.Value;
            var to = HuttballRules.GetPushSquare(winner.Square!.Value, from);
            if (!IsOnPitch(to) || GetPlayerAt(to) != null)
            {
                Say(result, $"{loser.Name} is pinned in place.");
                return;
            }

            loser.Square = to;
            Say(result, $"{loser.Name} is shoved back to {to}.");
            if (LooseBall == to && loser.Status == HuttballPlayerStatus.Standing)
            {
                LooseBall = null;
                BallCarrier = loser;
                Say(result, $"{loser.Name} ends up holding the ball.");
            }
        }

        private void RollInjury(HuttballActionResult result, HuttballPlayer victim)
        {
            var roll = Roll(HuttballRules.D20);
            var injury = HuttballRules.GetInjury(roll);
            Say(result, $"Injury roll for {victim.Name}: {roll}. {HuttballRules.GetInjuryText(injury)}.");

            switch (injury)
            {
                case HuttballInjury.KnockedDown:
                    KnockDown(result, victim);
                    break;
                case HuttballInjury.Injured:
                    Bench(result, victim, HuttballPlayerStatus.Injured);
                    break;
                case HuttballInjury.SevereInjury:
                    Bench(result, victim, HuttballPlayerStatus.SeverelyInjured);
                    break;
                default:
                    Bench(result, victim, HuttballPlayerStatus.Maimed);
                    break;
            }
        }

        private void KnockDown(HuttballActionResult result, HuttballPlayer player)
        {
            player.Status = HuttballPlayerStatus.KnockedDown;
            EndActivation(player);
            if (BallCarrier == player)
            {
                BallCarrier = null;
                Say(result, $"{player.Name} drops the ball!");
                Scatter(result, player.Square!.Value);
            }
        }

        private void Bench(HuttballActionResult result, HuttballPlayer player, HuttballPlayerStatus status)
        {
            var square = player.Square;
            player.Status = status;
            player.Square = null;
            EndActivation(player);
            if (BallCarrier == player && square != null)
            {
                BallCarrier = null;
                Say(result, $"{player.Name} drops the ball!");
                Scatter(result, square.Value);
            }
        }

        private void EndActivation(HuttballPlayer player)
        {
            player.MovementLeft = 0;
            if (player.Team == ActiveTeam)
                player.IsDone = true;
        }

        /// <summary>
        /// Bounces a loose ball from <paramref name="from"/> with a d8 until it settles on an empty square or a
        /// standing player catches it.
        /// </summary>
        private void Scatter(HuttballActionResult result, HuttballSquare from)
        {
            BallCarrier = null;
            LooseBall = null;
            var current = from;
            for (var bounce = 0; bounce < HuttballRules.MaxBounces; bounce++)
            {
                var direction = HuttballRules.BounceDirections[Roll(HuttballRules.BounceDirections.Count) - 1];
                var landing = ClampToPitch(new HuttballSquare(current.X + direction.X, current.Y + direction.Y));
                current = landing;

                var occupant = GetPlayerAt(landing);
                if (occupant == null)
                {
                    LooseBall = landing;
                    Say(result, $"The ball bounces to {landing}.");
                    return;
                }

                if (occupant.Status == HuttballPlayerStatus.Standing && TryCatch(result, occupant))
                    return;
            }

            var resting = FindNearestEmptySquare(current);
            LooseBall = resting;
            Say(result, $"The ball comes to rest at {resting}.");
        }

        /// <summary>
        /// Rolls for a player to catch the ball arriving on their square. The caller bounces a missed ball on.
        /// </summary>
        private bool TryCatch(HuttballActionResult result, HuttballPlayer catcher)
        {
            var roll = Roll(HuttballRules.D20);
            var total = roll + catcher.Stats.Agility;
            if (total >= HuttballRules.PassTarget)
            {
                BallCarrier = catcher;
                LooseBall = null;
                Say(result, $"{catcher.DisplayName} catches it: {FormatRoll(roll, catcher.Stats.Agility)}.");
                CheckTouchdown(result, catcher);
                return true;
            }

            Say(result, $"{catcher.DisplayName} fumbles it: {FormatRoll(roll, catcher.Stats.Agility)}, needs {HuttballRules.PassTarget}.");
            return false;
        }

        private HuttballSquare ClampToPitch(HuttballSquare square)
        {
            return new HuttballSquare(Math.Clamp(square.X, 0, Width - 1), Math.Clamp(square.Y, 0, Length - 1));
        }

        private HuttballSquare FindNearestEmptySquare(HuttballSquare from)
        {
            for (var radius = 0; radius < Math.Max(Width, Length); radius++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var square = new HuttballSquare(from.X + dx, from.Y + dy);
                        if (square.DistanceTo(from) == radius && IsOnPitch(square) && GetPlayerAt(square) == null)
                            return square;
                    }
                }
            }

            return from;
        }

        /// <summary>
        /// Scores when a standing ball carrier is in the end zone they attack, then sends the teams back to set up.
        /// </summary>
        private bool CheckTouchdown(HuttballActionResult result, HuttballPlayer player)
        {
            if (Phase != HuttballPhase.Playing ||
                BallCarrier != player ||
                !player.IsStandingOnPitch ||
                player.Square!.Value.Y != GetScoringRow(player.Team))
                return false;

            _score[player.Team]++;
            result.Scorer = player;
            Say(result, $"TOUCHDOWN! {player.DisplayName} scores for the {player.Team} team. " +
                        $"Home {_score[HuttballTeam.Home]} - Away {_score[HuttballTeam.Away]}.");

            ClearBall();
            foreach (var other in _players.Where(p => p.Status == HuttballPlayerStatus.KnockedDown))
                other.Status = HuttballPlayerStatus.Standing;

            var half = Half;
            Phase = HuttballPhase.Setup;
            AdvanceTurn(result);

            // The team scored on moves first next drive, even after an interception scores in its own turn.
            if (Phase == HuttballPhase.Setup && Half == half && ActiveTeam == player.Team)
                AdvanceTurn(result);

            if (Phase == HuttballPhase.Setup)
                Say(result, "Teams, take your places in your own half for the next kick-off.");
            return true;
        }

        private void ClearBall()
        {
            BallCarrier = null;
            LooseBall = null;
        }

        private static void FinishIfSpent(HuttballPlayer player)
        {
            if (player.HasActed && player.MovementLeft <= 0)
                player.IsDone = true;
        }

        private void CheckTeamTurnOver(HuttballActionResult result)
        {
            if (Phase != HuttballPhase.Playing)
                return;

            if (_players.Where(p => p.Team == ActiveTeam).All(p => p.IsDone))
            {
                Say(result, $"Every {ActiveTeam} player is done.");
                AdvanceTurn(result);
            }
        }

        /// <summary>
        /// Hands play to the other team. The turn counter moves on after the team that went second, and the half
        /// ends after the last turn. Called with the match in Setup after a touchdown, so the counter still moves.
        /// </summary>
        private void AdvanceTurn(HuttballActionResult result)
        {
            if (ActiveTeam != GetFirstTeam(Half))
                Turn++;

            ActiveTeam = GetOpponent(ActiveTeam);

            if (Turn > HuttballRules.TurnsPerHalf)
            {
                EndHalf(result);
                return;
            }

            if (Phase == HuttballPhase.Playing)
                StartTeamTurn(result);
        }

        private void StartTeamTurn(HuttballActionResult result)
        {
            foreach (var player in _players)
            {
                var active = player.Team == ActiveTeam && player.IsOnPitch;
                player.MovementLeft = active ? player.Stats.Movement : 0;
                player.HasActed = !active;
                player.IsDone = !active;
            }

            Say(result, $"Half {Half}, turn {Turn}: the {ActiveTeam} team's move.");

            if (_players.Where(p => p.Team == ActiveTeam).All(p => p.IsDone))
            {
                Say(result, $"The {ActiveTeam} team has nobody able to play.");
                AdvanceTurn(result);
            }
        }

        private void EndHalf(HuttballActionResult result)
        {
            ClearBall();
            foreach (var player in _players)
            {
                player.IsDone = true;
                player.HasActed = true;
                player.MovementLeft = 0;
            }

            if (Half >= HuttballRules.Halves)
            {
                Phase = HuttballPhase.Finished;
                var home = _score[HuttballTeam.Home];
                var away = _score[HuttballTeam.Away];
                var outcome = home == away ? "It's a draw!" : $"The {(home > away ? HuttballTeam.Home : HuttballTeam.Away)} team wins!";
                Say(result, $"Full time! Home {home} - Away {away}. {outcome}");
                return;
            }

            Half++;
            Turn = 1;
            ActiveTeam = GetFirstTeam(Half);
            Phase = HuttballPhase.Setup;
            foreach (var player in _players)
            {
                switch (player.Status)
                {
                    case HuttballPlayerStatus.KnockedDown:
                        player.Status = HuttballPlayerStatus.Standing;
                        break;
                    case HuttballPlayerStatus.Injured:
                    case HuttballPlayerStatus.SentOff:
                        player.Status = HuttballPlayerStatus.Standing;
                        player.Square = null;
                        break;
                }
            }

            Say(result, $"Halftime! Home {_score[HuttballTeam.Home]} - Away {_score[HuttballTeam.Away]}. " +
                        "Injured and sent-off players may return. Take your places for the second half.");
        }

        private static string FormatRoll(int roll, int bonus)
        {
            return bonus == 0 ? roll.ToString() : $"{roll}+{bonus}={roll + bonus}";
        }

        private void Say(HuttballActionResult result, string message)
        {
            result.Messages.Add(message);
            AddToLog(message);
        }

        private void AddToLog(string message)
        {
            _log.Add(message);
            if (_log.Count > MaxLogLines)
                _log.RemoveAt(0);
        }
    }
}
