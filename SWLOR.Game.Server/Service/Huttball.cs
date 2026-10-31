using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HuttballService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Runs Huttball matches on a DM battle grid. Each square of the grid is a square of the pitch; players'
    /// own characters (and creatures a DM controls) are the pieces. The match rules live in
    /// <see cref="HuttballMatch"/>; this service turns their results into movement, effects, and messages.
    /// One match exists per area and lasts until a DM ends it or the server restarts.
    /// </summary>
    public static class Huttball
    {
        private const string BallResref = "zep_crysorb002";
        private const string BallTag = "HUTTBALL_BALL";
        private const string MarkerResref = "plc_invisobj";
        private const string MarkerTag = "HUTTBALL_MARKER";

        // A plain human in the Commoner faction with no AI scripts, so test players never wander or fight.
        private const string TestPlayerResref = "colonisthuman1";
        public const string TestPlayerTag = "HUTTBALL_TEST_PLAYER";
        private const string CarrierEffectTag = "HUTTBALL_CARRIER";
        private const string KnockedDownEffectTag = "HUTTBALL_DOWN";

        // The module's Teleport Object: an invisible placeable whose name shows on hover, used as a scoreboard.
        private const string ScoreboardResref = "tele_obj";
        private const string ScoreboardTag = "HUTTBALL_SCOREBOARD";

        // A block borrows the taunt for one swing: the engine plays the slash in its place, with no real attack.
        // A tied block then plays the hit reaction the same way on both players at the moment of contact.
        private const string SwingStandIn = "taunt";
        private const string SwingAnimation = "1hslashr";
        private const string FlinchAnimation = "damages";
        private const float SwingSeconds = 1.5f;
        private const float ContactSeconds = 0.5f;

        // After a touchdown, players get this long to celebrate before they are sent back to their halves.
        private const int CelebrationSeconds = 60;

        private class MatchEntry
        {
            public HuttballMatch Match { get; init; }
            public HuttballPitch Pitch { get; init; }
            public uint Ball { get; set; } = OBJECT_INVALID;
            public HuttballSquare? BallSquare { get; set; }
            public List<uint> Markers { get; } = new();
            public List<uint> TestPlayers { get; } = new();
            public Dictionary<HuttballTeam, List<uint>> Scoreboards { get; } = new()
            {
                [HuttballTeam.Home] = new List<uint>(),
                [HuttballTeam.Away] = new List<uint>(),
            };

            /// <summary>
            /// Bumped by each touchdown, so a celebration countdown left over from an earlier one does nothing.
            /// </summary>
            public int CelebrationId { get; set; }

            /// <summary>
            /// The half, turn, and team last flashed as starting their turn.
            /// </summary>
            public (int Half, int Turn, HuttballTeam Team)? LastTurn { get; set; }
        }

        private static readonly Dictionary<uint, MatchEntry> _matches = new();

        public static HuttballMatch GetMatch(uint area)
        {
            return _matches.TryGetValue(area, out var entry) ? entry.Match : null;
        }

        public static HuttballPitch? GetPitch(uint area)
        {
            return _matches.TryGetValue(area, out var entry) ? entry.Pitch : null;
        }

        /// <summary>
        /// Starts a match on the area's battle grid. Returns an error, or null on success.
        /// </summary>
        public static string CreateMatch(uint area)
        {
            if (!GetIsObjectValid(area))
                return "There is no area here.";
            if (_matches.ContainsKey(area))
                return "This area already has a Huttball match.";
            if (!BattleGrid.IsPlaced(area))
                return "Place a battle grid first. Its squares become the pitch.";

            var pitch = HuttballPitch.FromGrid(BattleGrid.GetCenter(area), BattleGrid.Clamp(BattleGrid.GetSettings(area)));
            OpenMatch(area, pitch);
            return null;
        }

        /// <summary>
        /// Starts a match with no DM: lays the pitch, without a battle grid, centred where the player stands, and
        /// puts the player on the Home team as its captain. Returns an error, or null on success.
        /// </summary>
        public static string CreatePlayerMatch(uint creature, HuttballRole role)
        {
            var area = GetArea(creature);
            if (!GetIsObjectValid(area))
                return "There is no area here.";
            if (_matches.ContainsKey(area))
                return "This area already has a Huttball match.";
            if (GetIsObjectValid(FindMatchArea(creature)))
                return "You are already playing in another match.";

            var pitch = HuttballPitch.At(GetPosition(creature), BattleGrid.DefaultSettings.SquareSize);
            if (!pitch.FitsWithin(GetAreaSize(Dimension.Width, area) * 10f, GetAreaSize(Dimension.Height, area) * 10f))
                return "There isn't room for a pitch here. Move toward the middle of a large open area.";

            var entry = OpenMatch(area, pitch);
            var result = entry.Match.AddPlayer(creature, GetName(creature), HuttballTeam.Home, role, CanCaptain(creature));
            Apply(area, result, creature);
            return null;
        }

        private static MatchEntry OpenMatch(uint area, HuttballPitch pitch)
        {
            var entry = new MatchEntry
            {
                Match = new HuttballMatch(pitch.Width, pitch.Length, new HuttballDice()),
                Pitch = pitch,
            };
            _matches[area] = entry;
            MarkPitch(area, entry);

            Announce(area, new[]
            {
                $"A Huttball match is set up on a {pitch.Width} x {pitch.Length} pitch. {PitchKey} " +
                "Type /huttball to open the match panel and join a team."
            });
            RefreshWindows();
            return entry;
        }

        /// <summary>
        /// Ends the area's match and clears its effects and ball.
        /// </summary>
        public static void EndMatch(uint area)
        {
            if (!_matches.TryGetValue(area, out var entry))
                return;

            foreach (var player in entry.Match.Players)
                ClearEffects(player.Creature);

            DestroyBall(entry);
            foreach (var marker in entry.Markers.Concat(entry.Scoreboards.Values.SelectMany(boards => boards)))
                DestroyObject(marker);
            foreach (var testPlayer in entry.TestPlayers.Where(GetIsObjectValid))
                DestroyObject(testPlayer);
            _matches.Remove(area);
            Announce(area, new[] { "The Huttball match has ended." });
            RefreshWindows();
        }

        /// <summary>
        /// How to read the pitch markings, for announcements and the match panel.
        /// </summary>
        public const string PitchKey =
            "Blue rings and flags: the Home end zone (south). Red rings and flags: the Away end zone (north). " +
            "White rings and gold flags: the halfway line. Home scores in the red end zone, Away in the blue.";

        /// <summary>
        /// Rings every square of both end zones in its team's color and the halfway line in white, and plants flags
        /// a square outside the pitch: team flags off each corner and gold flags off each end of the halfway line,
        /// so players can read the pitch and its edges at a glance.
        /// </summary>
        private static void MarkPitch(uint area, MatchEntry entry)
        {
            var pitch = entry.Pitch;
            var ringScale = pitch.SquareSize / 2f;
            var halfway = pitch.Length / 2f;

            for (var x = 0; x < pitch.Width; x++)
            {
                AddMarker(area, entry, pitch.GetSquareCenter(new HuttballSquare(x, 0)), VisualEffect.Vfx_Dur_Aura_Blue, ringScale);
                AddMarker(area, entry, pitch.GetSquareCenter(new HuttballSquare(x, pitch.Length - 1)), VisualEffect.Vfx_Dur_Aura_Red, ringScale);
                AddMarker(area, entry, pitch.GetPoint(x + 0.5f, halfway), VisualEffect.Vfx_Dur_Aura_White, ringScale);
            }

            var homeFlags = new[] { pitch.GetPoint(-1f, -1f), pitch.GetPoint(pitch.Width + 1f, -1f) };
            var awayFlags = new[] { pitch.GetPoint(-1f, pitch.Length + 1f), pitch.GetPoint(pitch.Width + 1f, pitch.Length + 1f) };
            foreach (var flag in homeFlags)
            {
                AddMarker(area, entry, flag, VisualEffect.Vfx_Dur_Flag_Blue, 1f);
                AddScoreboard(area, entry, HuttballTeam.Home, flag);
            }

            foreach (var flag in awayFlags)
            {
                AddMarker(area, entry, flag, VisualEffect.Vfx_Dur_Flag_Red, 1f);
                AddScoreboard(area, entry, HuttballTeam.Away, flag);
            }

            AddMarker(area, entry, pitch.GetPoint(-1f, halfway), VisualEffect.Vfx_Dur_Flag_Gold_Fixed, 1f);
            AddMarker(area, entry, pitch.GetPoint(pitch.Width + 1f, halfway), VisualEffect.Vfx_Dur_Flag_Gold_Fixed, 1f);
            UpdateScoreboards(entry);
        }

        /// <summary>
        /// Puts a team scoreboard at one of its flags: a Teleport Object whose hover name shows the team's score
        /// and resources. Its teleport script is cleared, so using it does nothing.
        /// </summary>
        private static void AddScoreboard(uint area, MatchEntry entry, HuttballTeam team, Vector3 position)
        {
            var board = CreateObject(ObjectType.Placeable, ScoreboardResref, Location(area, position, 0f), false, ScoreboardTag);
            if (!GetIsObjectValid(board))
                return;

            SetEventScript(board, EventScript.Placeable_OnUsed, string.Empty);
            SetPlotFlag(board, true);
            entry.Scoreboards[team].Add(board);
        }

        /// <summary>
        /// Renames each team's scoreboards to show its score, re-rolls, medics, and bribes.
        /// </summary>
        private static void UpdateScoreboards(MatchEntry entry)
        {
            var match = entry.Match;
            foreach (var (team, boards) in entry.Scoreboards)
            {
                var color = team == HuttballTeam.Home ? "Blue" : "Red";
                var name = $"{team} ({color}): {match.GetScore(team)} pts | Re-rolls {match.GetRerolls(team)} | " +
                           $"Medics {match.GetMedics(team)} | Bribes {match.GetBribes(team)}";
                foreach (var board in boards.Where(GetIsObjectValid))
                    SetName(board, name);
            }
        }

        /// <summary>
        /// Shows a change to a team's re-rolls, medics, or bribes on its scoreboards and every match panel.
        /// </summary>
        public static void ResourcesChanged(uint area)
        {
            if (_matches.TryGetValue(area, out var entry))
                UpdateScoreboards(entry);
            RefreshWindows();
        }

        /// <summary>
        /// Fills both teams for testing: one of each role per side, named like "Test Catcher Home", lined up a
        /// couple of squares back from the halfway line. Only allowed while the teams set up.
        /// </summary>
        public static void SpawnTestTeams(uint area, uint actor)
        {
            if (!_matches.TryGetValue(area, out var entry))
                return;

            var match = entry.Match;
            if (match.Phase != HuttballPhase.Setup)
            {
                DMPresence.SendMessage(actor, ColorToken.Red("Spawn test teams between drives."));
                return;
            }

            var result = new HuttballActionResult();
            var roles = new[] { HuttballRole.Bruiser, HuttballRole.Catcher, HuttballRole.Thrower, HuttballRole.Blocker };
            foreach (var team in new[] { HuttballTeam.Home, HuttballTeam.Away })
            {
                for (var index = 0; index < roles.Length; index++)
                {
                    var square = FindFormationSquare(match, team, index);
                    if (square == null)
                    {
                        result.Messages.Add($"No free square left in the {team} half for a test player.");
                        break;
                    }

                    var facing = team == HuttballTeam.Home ? 90f : 270f;
                    var creature = CreateObject(ObjectType.Creature, TestPlayerResref,
                        Location(area, entry.Pitch.GetSquareCenter(square.Value), facing), false, TestPlayerTag);
                    if (!GetIsObjectValid(creature))
                        continue;

                    var name = $"Test {roles[index]} {team}";
                    SetName(creature, name);
                    SetPlotFlag(creature, true);
                    entry.TestPlayers.Add(creature);
                    result.Messages.AddRange(match.AddPlayer(creature, name, team, roles[index]).Messages);
                    match.Place(creature, square.Value);
                }
            }

            Apply(area, result, actor);
        }

        /// <summary>
        /// The square for the <paramref name="index"/>th test player of a team: every other square across the
        /// middle of a row two squares back from the halfway line, or the nearest free square of the team's half.
        /// </summary>
        private static HuttballSquare? FindFormationSquare(HuttballMatch match, HuttballTeam team, int index)
        {
            var row = team == HuttballTeam.Home ? match.Length / 2 - 2 : match.Length / 2 + 1;
            var preferred = new HuttballSquare(match.Width / 2 - 3 + index * 2, row);
            if (match.IsOnPitch(preferred) && match.GetPlayerAt(preferred) == null)
                return preferred;

            return Enumerable.Range(0, match.Length)
                .SelectMany(y => Enumerable.Range(0, match.Width).Select(x => new HuttballSquare(x, y)))
                .Where(square => match.IsInOwnHalf(team, square) && match.GetPlayerAt(square) == null)
                .OrderBy(square => square.DistanceTo(preferred))
                .Cast<HuttballSquare?>()
                .FirstOrDefault();
        }

        private static void AddMarker(uint area, MatchEntry entry, Vector3 position, VisualEffect visual, float scale)
        {
            var marker = CreateObject(ObjectType.Placeable, MarkerResref, Location(area, position, 0f), false, MarkerTag);
            if (!GetIsObjectValid(marker))
                return;

            SetName(marker, "Huttball Pitch");
            SetPlotFlag(marker, true);
            SetUseableFlag(marker, false);
            ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(visual, false, scale), marker);
            entry.Markers.Add(marker);
        }

        /// <summary>
        /// Returns true for staff, who run matches as they may run the battle grid: DMs, and staff on their own
        /// characters. Staff are matched by CD key, which resolves from the creature they control.
        /// </summary>
        public static bool IsStaff(uint actor)
        {
            var controlled = DMPresence.GetControlledCreature(actor);
            if (string.IsNullOrEmpty(GetPCPublicCDKey(controlled)))
                return false;

            var level = Authorization.GetAuthorizationLevel(controlled);
            return level.HasFlag(AuthorizationLevel.DM) || level.HasFlag(AuthorizationLevel.Admin);
        }

        /// <summary>
        /// True for a player's own character, which may captain its team. NPCs, including those a DM possesses,
        /// never do.
        /// </summary>
        public static bool CanCaptain(uint creature)
        {
            return GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
        }

        /// <summary>
        /// Runs a match command for whoever clicked a panel button, in the area of the creature they control, then
        /// announces and plays out the result. See <see cref="Apply"/> for the optional creatures.
        /// </summary>
        public static void Command(
            uint actor,
            System.Func<HuttballMatch, HuttballActionResult> command,
            uint mover = OBJECT_INVALID,
            uint striker = OBJECT_INVALID,
            uint struck = OBJECT_INVALID)
        {
            var area = GetArea(DMPresence.GetControlledCreature(actor));
            var match = GetMatch(area);
            if (match == null)
            {
                DMPresence.SendMessage(actor, ColorToken.Red("There is no Huttball match in this area."));
                return;
            }

            Apply(area, command(match), actor, mover, striker, struck);
        }

        /// <summary>
        /// Asks whoever clicked a panel button for a square of the pitch: the ground, or anything standing on it.
        /// </summary>
        public static void PickSquare(uint actor, string message, System.Action<HuttballSquare> onPicked)
        {
            var controlled = DMPresence.GetControlledCreature(actor);
            Targeting.EnterTargetingMode(controlled, ObjectType.All, message, target =>
            {
                // A ground pick arrives as the area object, the way the battle grid window reads it.
                var area = GetArea(controlled);
                if (!_matches.TryGetValue(area, out var entry))
                    return;

                Vector3 position;
                if (target == area)
                    position = GetTargetingModeSelectedPosition();
                else if (GetIsObjectValid(target) && GetArea(target) == area)
                    position = GetPosition(target);
                else
                {
                    DMPresence.SendMessage(actor, ColorToken.Red("Pick a square of the pitch."));
                    return;
                }

                onPicked(entry.Pitch.GetSquare(position));
            });
        }

        /// <summary>
        /// Asks whoever clicked a panel button to click a creature.
        /// </summary>
        public static void PickCreature(uint actor, string message, System.Action<uint> onPicked)
        {
            Targeting.EnterTargetingMode(DMPresence.GetControlledCreature(actor), ObjectType.Creature, message, onPicked);
        }

        /// <summary>
        /// Returns the area whose match includes the creature, or OBJECT_INVALID.
        /// </summary>
        public static uint FindMatchArea(uint creature)
        {
            foreach (var (area, entry) in _matches)
            {
                if (entry.Match.GetPlayer(creature) != null)
                    return area;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Announces a command's play-by-play, moves the pieces to match, and refreshes every match window. A
        /// failed command only tells the person who issued it why.
        /// </summary>
        /// <param name="area">The match's area.</param>
        /// <param name="result">What the command returned.</param>
        /// <param name="actor">Whoever issued the command, to tell about a failure.</param>
        /// <param name="mover">The creature that walked <see cref="HuttballActionResult.Path"/>, if any.</param>
        /// <param name="striker">The creature that blocked or kicked, which swings at <paramref name="struck"/>.</param>
        /// <param name="struck">The creature the striker hit.</param>
        public static void Apply(
            uint area,
            HuttballActionResult result,
            uint actor,
            uint mover = OBJECT_INVALID,
            uint striker = OBJECT_INVALID,
            uint struck = OBJECT_INVALID)
        {
            if (!result.Succeeded)
            {
                DMPresence.SendMessage(actor, ColorToken.Red(result.Error));
                return;
            }

            if (!_matches.TryGetValue(area, out var entry))
                return;

            Announce(area, result.Messages);
            if (GetIsObjectValid(striker) && GetIsObjectValid(struck))
                Swing(striker, struck, result.Rebounded);
            SyncWorld(area, entry, result, mover, striker);
            UpdateScoreboards(entry);
            if (result.Scorer != null)
                Celebrate(area, entry, result.Scorer);
            RefreshWindows();
        }

        /// <summary>
        /// Turns the two players to face each other and plays a melee slash for the striker. The slash stands in
        /// for the taunt for one swing, so no attack is made and nobody turns hostile. On a rebound, both players
        /// flinch at the moment of contact as if struck.
        /// </summary>
        private static void Swing(uint striker, uint struck, bool rebound)
        {
            var strikerPosition = GetPosition(striker);
            var struckPosition = GetPosition(struck);
            ReplaceObjectAnimation(striker, SwingStandIn, SwingAnimation);
            AssignCommand(striker, () =>
            {
                ClearAllActions();
                SetFacingPoint(struckPosition);
                PlayAnimation(Animation.FireForgetTaunt);
            });
            AssignCommand(struck, () => SetFacingPoint(strikerPosition));

            if (rebound)
            {
                Delay(ContactSeconds, () =>
                {
                    foreach (var creature in new[] { striker, struck }.Where(GetIsObjectValid))
                    {
                        ReplaceObjectAnimation(creature, SwingStandIn, FlinchAnimation);
                        AssignCommand(creature, () => PlayAnimation(Animation.FireForgetTaunt));
                    }
                });
            }

            Delay(SwingSeconds, () =>
            {
                foreach (var creature in new[] { striker, struck }.Where(GetIsObjectValid))
                    ReplaceObjectAnimation(creature, SwingStandIn);
            });
        }

        private static void Delay(float seconds, System.Action action)
        {
            AssignCommand(GetModule(), () => DelayCommand(seconds, action));
        }

        /// <summary>
        /// Celebrates a touchdown: a burst over the scorer, a cheer from the scoring team, and a one-minute
        /// countdown for emotes before both teams are sent back to their halves for the kick-off.
        /// </summary>
        private static void Celebrate(uint area, MatchEntry entry, HuttballPlayer scorer)
        {
            var celebrationId = ++entry.CelebrationId;
            if (GetIsObjectValid(scorer.Creature))
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion), scorer.Creature);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst), scorer.Creature);
                FloatingTextStringOnCreature(ColorToken.Yellow($"TOUCHDOWN! {scorer.Name}!"), scorer.Creature, false);
            }

            var cheers = new[] { Animation.FireForgetVictory1, Animation.FireForgetVictory2, Animation.FireForgetVictory3 };
            var index = 0;
            foreach (var teammate in entry.Match.Players.Where(p => p.Team == scorer.Team && p.IsStandingOnPitch))
            {
                var cheer = cheers[index++ % cheers.Length];
                if (GetIsObjectValid(teammate.Creature))
                    AssignCommand(teammate.Creature, () => PlayAnimation(cheer));
            }

            if (entry.Match.Phase != HuttballPhase.Setup)
                return;

            Announce(area, new[] { $"Celebrate! Both teams return to their halves in {CelebrationSeconds} seconds." });
            foreach (var remaining in new[] { 30, 10, 5 })
            {
                Delay(CelebrationSeconds - remaining, () =>
                {
                    if (IsCelebrating(area, entry, celebrationId))
                        Announce(area, new[] { $"Back to your halves in {remaining} seconds." });
                });
            }

            Delay(CelebrationSeconds, () =>
            {
                if (IsCelebrating(area, entry, celebrationId))
                    Apply(area, entry.Match.ReturnToHalves(), OBJECT_INVALID);
            });
        }

        /// <summary>
        /// Returns true while a touchdown's countdown should still run: the match is the same one, no later
        /// touchdown has started its own countdown, and the next drive has not begun.
        /// </summary>
        private static bool IsCelebrating(uint area, MatchEntry entry, int celebrationId)
        {
            return _matches.TryGetValue(area, out var current) &&
                   current == entry &&
                   entry.CelebrationId == celebrationId &&
                   entry.Match.Phase == HuttballPhase.Setup;
        }

        private static void Announce(uint area, IEnumerable<string> messages)
        {
            var lines = messages.ToList();
            if (lines.Count == 0)
                return;

            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                var controlled = DMPresence.GetControlledCreature(pc);
                if (GetArea(controlled) != area)
                    continue;

                foreach (var line in lines)
                    SendMessageToPC(controlled, ColorToken.SkillCheck("Huttball: ") + line);
            }
        }

        public static void RefreshWindows()
        {
            var refresh = new HuttballRefreshEvent();
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (DMPresence.IsControlledByDM(pc))
                {
                    foreach (var target in DMPresence.GetShownOn(pc, GuiWindowType.Huttball))
                        Gui.PublishRefreshEvent(pc, refresh, target);
                }
                else
                {
                    Gui.PublishRefreshEvent(pc, refresh);
                }
            }
        }

        /// <summary>
        /// Puts every piece where the match says it is: the mover walks its path, anyone else off their square is
        /// jumped onto it, benched players go to the sideline, and the ball follows the carrier or lies on its square.
        /// </summary>
        private static void SyncWorld(uint area, MatchEntry entry, HuttballActionResult result, uint mover, uint striker)
        {
            var match = entry.Match;
            var pitch = entry.Pitch;
            var benchSlots = new Dictionary<HuttballTeam, int> { [HuttballTeam.Home] = 0, [HuttballTeam.Away] = 0 };

            foreach (var player in match.Players)
            {
                var creature = player.Creature;
                if (!GetIsObjectValid(creature) || GetArea(creature) != area)
                    continue;

                var isDown = player.Status == HuttballPlayerStatus.KnockedDown;
                var wasDown = HasEffectByTag(creature, KnockedDownEffectTag);
                var moved = false;
                if (player.Square != null)
                {
                    var target = pitch.GetSquareCenter(player.Square.Value);
                    // A mover tripped on the way goes down at once rather than walking.
                    if (creature == mover && result.Path.Count > 0 && !isDown)
                        WalkPath(area, pitch, creature, result.Path);
                    else if (System.Numerics.Vector3.Distance(GetPosition(creature), target) > pitch.SquareSize * 0.35f)
                    {
                        JumpTo(area, creature, target);
                        moved = true;
                    }
                }
                else if (player.Status != HuttballPlayerStatus.Standing)
                {
                    var bench = pitch.GetBenchPosition(player.Team, benchSlots[player.Team]++);
                    if (System.Numerics.Vector3.Distance(GetPosition(creature), bench) > pitch.SquareSize * 0.35f)
                    {
                        JumpTo(area, creature, bench);
                        moved = true;
                    }
                }

                SetTaggedEffect(creature, KnockedDownEffectTag, isDown,
                    () => SupernaturalEffect(EffectCutsceneImmobilize()));
                if (isDown && (!wasDown || moved))
                    LieDown(creature, tripped: creature == mover, delay: creature == striker ? SwingSeconds : 0f);
                else if (!isDown && wasDown)
                    AssignCommand(creature, () => ClearAllActions());

                SetTaggedEffect(creature, CarrierEffectTag, match.BallCarrier == player,
                    () => SupernaturalEffect(EffectVisualEffect(VisualEffect.Vfx_Dur_Glow_Yellow)));
            }

            SyncBall(area, entry);
            AnnounceTurnStart(area, entry);
        }

        /// <summary>
        /// Flashes every on-pitch player of a team when its turn begins, so they know it is their move.
        /// </summary>
        private static void AnnounceTurnStart(uint area, MatchEntry entry)
        {
            var match = entry.Match;
            if (match.Phase != HuttballPhase.Playing)
            {
                entry.LastTurn = null;
                return;
            }

            var turn = (match.Half, match.Turn, match.ActiveTeam);
            if (entry.LastTurn == turn)
                return;

            entry.LastTurn = turn;
            foreach (var player in match.Players.Where(p => p.Team == match.ActiveTeam && p.IsOnPitch))
            {
                if (GetIsObjectValid(player.Creature) && GetArea(player.Creature) == area)
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pdk_Final_Stand), player.Creature);
            }
        }

        /// <summary>
        /// Lays a knocked-down player out like the /deadfront and /deadback emotes: face down when tripped while
        /// moving, on their back when knocked over. The pose holds until they stand up.
        /// </summary>
        private static void LieDown(uint creature, bool tripped, float delay)
        {
            var pose = tripped ? Animation.LoopingDeadFront : Animation.LoopingDeadBack;
            AssignCommand(creature, () =>
            {
                ClearAllActions();
                // A striker knocked down by their own block falls after the swing finishes.
                if (delay > 0f)
                    ActionWait(delay);
                ActionPlayAnimation(pose, 1f, 86400f);
            });
        }

        private static void WalkPath(uint area, HuttballPitch pitch, uint creature, IReadOnlyList<HuttballSquare> path)
        {
            var points = path.Select(pitch.GetSquareCenter).ToList();
            var from = points.Count > 1 ? points[^2] : GetPosition(creature);
            var facing = GetFacingAlong(from, points[^1], GetFacing(creature));
            var steps = points.Select(point => Location(area, point, facing)).ToList();
            AssignCommand(creature, () =>
            {
                ClearAllActions();
                foreach (var step in steps)
                    ActionMoveToLocation(step, true);

                // Walking stops short of a point; the jump leaves the creature exactly on its square, still facing
                // the way it last ran.
                ActionJumpToLocation(steps[^1]);
            });
        }

        /// <summary>
        /// Returns the facing, in degrees counterclockwise from east, of travel from one point to another, or
        /// <paramref name="fallback"/> when they are the same point.
        /// </summary>
        public static float GetFacingAlong(Vector3 from, Vector3 to, float fallback)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            if (System.MathF.Abs(dx) < 0.001f && System.MathF.Abs(dy) < 0.001f)
                return fallback;

            var degrees = System.MathF.Atan2(dy, dx) * 180f / System.MathF.PI;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        /// <summary>
        /// Places a creature at once. Queued jumps would wait behind a knocked-down player's pose.
        /// </summary>
        private static void JumpTo(uint area, uint creature, Vector3 position)
        {
            var ground = GetGroundHeight(Location(area, position, 0f));
            ObjectPlugin.SetPosition(creature, position with { Z = ground });
        }

        private static void SetTaggedEffect(uint creature, string tag, bool shouldHave, System.Func<Effect> create)
        {
            var has = HasEffectByTag(creature, tag);
            if (shouldHave && !has)
                ApplyEffectToObject(DurationType.Permanent, TagEffect(create(), tag), creature);
            else if (!shouldHave && has)
                RemoveEffectByTag(creature, tag);
        }

        public static void ClearEffects(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            // Clearing actions ends a knocked-down player's pose.
            if (HasEffectByTag(creature, KnockedDownEffectTag))
                AssignCommand(creature, () => ClearAllActions());
            RemoveEffectByTag(creature, KnockedDownEffectTag, CarrierEffectTag);
        }

        private static void SyncBall(uint area, MatchEntry entry)
        {
            var square = entry.Match.LooseBall;
            if (square == entry.BallSquare && (square == null || GetIsObjectValid(entry.Ball)))
                return;

            DestroyBall(entry);
            if (square == null)
                return;

            var ball = CreateObject(ObjectType.Placeable, BallResref,
                Location(area, entry.Pitch.GetSquareCenter(square.Value), 0f), false, BallTag);
            if (GetIsObjectValid(ball))
            {
                SetName(ball, "Huttball");
                SetPlotFlag(ball, true);
                SetUseableFlag(ball, false);
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Glow_Yellow), ball);
            }

            entry.Ball = ball;
            entry.BallSquare = square;
        }

        private static void DestroyBall(MatchEntry entry)
        {
            if (GetIsObjectValid(entry.Ball))
                DestroyObject(entry.Ball);

            entry.Ball = OBJECT_INVALID;
            entry.BallSquare = null;
        }

        /// <summary>
        /// Takes a player who logs out out of their match, dropping the ball if they held it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void RemoveExitingPlayer()
        {
            var player = GetExitingObject();
            var area = FindMatchArea(player);
            if (!GetIsObjectValid(area))
                return;

            ClearEffects(player);
            Apply(area, _matches[area].Match.RemovePlayer(player), player);
        }

        /// <summary>
        /// Clears Huttball effects left on a player whose match is gone, such as after a server restart, when the
        /// exit cleanup never ran.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ClearStaleEffects()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (!GetIsObjectValid(FindMatchArea(player)))
                ClearEffects(player);
        }
    }
}
