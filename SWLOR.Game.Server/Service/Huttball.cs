using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HuttballService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
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
        private const string CarrierEffectTag = "HUTTBALL_CARRIER";
        private const string KnockedDownEffectTag = "HUTTBALL_DOWN";

        // A block borrows the taunt for one swing: the engine plays the slash in its place, with no real attack.
        private const string SwingStandIn = "taunt";
        private const string SwingAnimation = "1hslashr";
        private const float SwingSeconds = 1.5f;

        private class MatchEntry
        {
            public HuttballMatch Match { get; init; }
            public HuttballPitch Pitch { get; init; }
            public uint Ball { get; set; } = OBJECT_INVALID;
            public HuttballSquare? BallSquare { get; set; }
            public List<uint> Markers { get; } = new();
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
                "Type /huttball to open the match panel."
            });
            RefreshWindows();
            return null;
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
            foreach (var marker in entry.Markers)
                DestroyObject(marker);
            _matches.Remove(area);
            Announce(area, new[] { "The Huttball match has ended." });
            RefreshWindows();
        }

        /// <summary>
        /// How to read the pitch markings, for announcements and the match panel.
        /// </summary>
        public const string PitchKey =
            "Blue rings and flags: the Home end zone (south). Red rings and flags: the Away end zone (north). " +
            "White rings: the halfway line. Home scores in the red end zone, Away in the blue.";

        /// <summary>
        /// Rings every square of both end zones in its team's color, rings the halfway line in white, and plants
        /// a team flag at each corner, so players can read the pitch at a glance.
        /// </summary>
        private static void MarkPitch(uint area, MatchEntry entry)
        {
            var pitch = entry.Pitch;
            var ringScale = pitch.SquareSize / 2f;
            var halfway = pitch.Length / 2;

            for (var x = 0; x < pitch.Width; x++)
            {
                AddMarker(area, entry, pitch.GetSquareCenter(new HuttballSquare(x, 0)), VisualEffect.Vfx_Dur_Aura_Blue, ringScale);
                AddMarker(area, entry, pitch.GetSquareCenter(new HuttballSquare(x, pitch.Length - 1)), VisualEffect.Vfx_Dur_Aura_Red, ringScale);

                // The halfway line runs along the edge between the two halves, half a square south of the first
                // northern row.
                var line = pitch.GetSquareCenter(new HuttballSquare(x, halfway));
                AddMarker(area, entry, line with { Y = line.Y - pitch.SquareSize / 2f }, VisualEffect.Vfx_Dur_Aura_White, ringScale);
            }

            var southWest = pitch.GetSquareCenter(new HuttballSquare(0, 0));
            var southEast = pitch.GetSquareCenter(new HuttballSquare(pitch.Width - 1, 0));
            var northWest = pitch.GetSquareCenter(new HuttballSquare(0, pitch.Length - 1));
            var northEast = pitch.GetSquareCenter(new HuttballSquare(pitch.Width - 1, pitch.Length - 1));
            AddMarker(area, entry, southWest, VisualEffect.Vfx_Dur_Flag_Blue, 1f);
            AddMarker(area, entry, southEast, VisualEffect.Vfx_Dur_Flag_Blue, 1f);
            AddMarker(area, entry, northWest, VisualEffect.Vfx_Dur_Flag_Red, 1f);
            AddMarker(area, entry, northEast, VisualEffect.Vfx_Dur_Flag_Red, 1f);
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
                Swing(striker, struck);
            SyncWorld(area, entry, result, mover, striker);
            RefreshWindows();
        }

        /// <summary>
        /// Turns the two players to face each other and plays a melee slash for the striker. The slash stands in
        /// for the taunt for one swing, so no attack is made and nobody turns hostile.
        /// </summary>
        private static void Swing(uint striker, uint struck)
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
            AssignCommand(GetModule(), () => DelayCommand(SwingSeconds, () =>
            {
                if (GetIsObjectValid(striker))
                    ReplaceObjectAnimation(striker, SwingStandIn);
            }));
            AssignCommand(struck, () => SetFacingPoint(strikerPosition));
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
    }
}
