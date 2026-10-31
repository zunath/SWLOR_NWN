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
        private const string CarrierEffectTag = "HUTTBALL_CARRIER";
        private const string KnockedDownEffectTag = "HUTTBALL_DOWN";

        private class MatchEntry
        {
            public HuttballMatch Match { get; init; }
            public HuttballPitch Pitch { get; init; }
            public uint Ball { get; set; } = OBJECT_INVALID;
            public HuttballSquare? BallSquare { get; set; }
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
            _matches[area] = new MatchEntry
            {
                Match = new HuttballMatch(pitch.Width, pitch.Length, new HuttballDice()),
                Pitch = pitch,
            };

            Announce(area, new[]
            {
                $"A Huttball match is set up on a {pitch.Width} x {pitch.Length} pitch. " +
                "The Home team defends the south end and scores in the north; Away is the reverse. Type /huttball to open the match panel."
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
            _matches.Remove(area);
            Announce(area, new[] { "The Huttball match has ended." });
            RefreshWindows();
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
        public static void Apply(uint area, HuttballActionResult result, uint actor, uint mover = OBJECT_INVALID)
        {
            if (!result.Succeeded)
            {
                DMPresence.SendMessage(actor, ColorToken.Red(result.Error));
                return;
            }

            if (!_matches.TryGetValue(area, out var entry))
                return;

            Announce(area, result.Messages);
            SyncWorld(area, entry, result, mover);
            RefreshWindows();
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
        private static void SyncWorld(uint area, MatchEntry entry, HuttballActionResult result, uint mover)
        {
            var match = entry.Match;
            var pitch = entry.Pitch;
            var benchSlots = new Dictionary<HuttballTeam, int> { [HuttballTeam.Home] = 0, [HuttballTeam.Away] = 0 };

            foreach (var player in match.Players)
            {
                var creature = player.Creature;
                if (!GetIsObjectValid(creature) || GetArea(creature) != area)
                    continue;

                if (player.Square != null)
                {
                    var target = pitch.GetSquareCenter(player.Square.Value);
                    // A mover tripped on the way is knocked down at once, which would cancel the walk.
                    if (creature == mover && result.Path.Count > 0 && player.Status == HuttballPlayerStatus.Standing)
                        WalkPath(area, pitch, creature, result.Path);
                    else if (System.Numerics.Vector3.Distance(GetPosition(creature), target) > pitch.SquareSize * 0.35f)
                        JumpTo(area, creature, target);
                }
                else if (player.Status != HuttballPlayerStatus.Standing)
                {
                    var bench = pitch.GetBenchPosition(player.Team, benchSlots[player.Team]++);
                    if (System.Numerics.Vector3.Distance(GetPosition(creature), bench) > pitch.SquareSize * 0.35f)
                        JumpTo(area, creature, bench);
                }

                SetTaggedEffect(creature, KnockedDownEffectTag, player.Status == HuttballPlayerStatus.KnockedDown,
                    () => SupernaturalEffect(EffectKnockdown()));
                SetTaggedEffect(creature, CarrierEffectTag, match.BallCarrier == player,
                    () => SupernaturalEffect(EffectVisualEffect(VisualEffect.Vfx_Dur_Glow_Yellow)));
            }

            SyncBall(area, entry);
        }

        private static void WalkPath(uint area, HuttballPitch pitch, uint creature, IReadOnlyList<HuttballSquare> path)
        {
            var steps = path.Select(square => Location(area, pitch.GetSquareCenter(square), GetFacing(creature))).ToList();
            AssignCommand(creature, () =>
            {
                ClearAllActions();
                foreach (var step in steps)
                    ActionMoveToLocation(step, true);

                // Walking stops short of a point; the jump leaves the creature exactly on its square.
                ActionJumpToLocation(steps[^1]);
            });
        }

        /// <summary>
        /// Places a creature at once. Knockdown blocks queued jumps, and shoved or tripped players are often down.
        /// </summary>
        private static void JumpTo(uint area, uint creature, Vector3 position)
        {
            var ground = GetGroundHeight(Location(area, position, 0f));
            AssignCommand(creature, () => ClearAllActions());
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
