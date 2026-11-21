using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DuelService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Duels at a Dueling Ring on live players: an accepted duel takes the subdual path for both duelists, a third
    /// player's lethal blow voids it but still only subdues, and falls and forfeits end it.
    /// </summary>
    public static class DuelingRingEngineTests
    {
        private const string Category = "DuelingRing";
        private const string RingBlueprint = "swlor_0049";
        private const int LethalDamage = 5000;

        [EngineTest("A third player's lethal hit on a duelist voids the duel but only subdues, and the other duelist stays protected", Category = Category, TimeoutSeconds = 60f)]
        public static async Task ThirdPartyLethalHitOnlySubdues(EngineTestContext ctx)
        {
            using var challenger = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            using var stranger = await PlayerAbilityFixture.CreateAsync(ctx, 8f);
            var ring = SpawnRing(ctx);
            await ctx.WaitFrameAsync();

            var duel = await StartAcceptedDuelAsync(ctx, challenger.Creature, target.Creature, ring);
            AssertSettingsOff(ctx, challenger, target);

            await HitAsync(ctx, stranger.Creature, target.Creature, LethalDamage);
            await WaitForSubdualAsync(ctx, target.Creature, "the duelist a third player hit");
            ctx.AssertEqual(DuelState.Interrupted, duel.State, "a third player's blow voids the duel");
            var secondsLeft = (duel.ProtectedUntilUtc - DateTime.UtcNow).TotalSeconds;
            ctx.Assert(secondsLeft is > 3 and <= DuelRules.ProtectionSeconds,
                $"both duelists are protected for about {DuelRules.ProtectionSeconds} seconds, {secondsLeft:F1} s remain");

            await HitAsync(ctx, stranger.Creature, challenger.Creature, LethalDamage);
            await WaitForSubdualAsync(ctx, challenger.Creature, "the other duelist, protected by the same window");
        }

        [EngineTest("A third player's scratch voids the duel, and after 6 seconds a lethal hit kills again", Category = Category, TimeoutSeconds = 60f)]
        public static async Task ProtectionLapsesAfterSixSeconds(EngineTestContext ctx)
        {
            using var challenger = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            using var stranger = await PlayerAbilityFixture.CreateAsync(ctx, 8f);
            var ring = SpawnRing(ctx);
            await ctx.WaitFrameAsync();

            var duel = await StartAcceptedDuelAsync(ctx, challenger.Creature, target.Creature, ring);

            await HitAsync(ctx, stranger.Creature, challenger.Creature, 1);
            ctx.AssertEqual(DuelState.Interrupted, duel.State, "a non-lethal third-party blow voids the duel");

            await ctx.DelaySecondsAsync(2f);
            await HitAsync(ctx, stranger.Creature, target.Creature, LethalDamage);
            await WaitForSubdualAsync(ctx, target.Creature, "a duelist hit inside the window");

            await ctx.WaitUntilAsync(() => Duels.GetDuel(challenger.Creature) == null, 12f, "the protection window to end");
            await HitAsync(ctx, stranger.Creature, challenger.Creature, LethalDamage);
            await ctx.WaitUntilAsync(() => GetIsDead(challenger.Creature), 5f, "a lethal hit after the window to kill the duelist");
        }

        [EngineTest("A duelist's lethal hit subdues the loser whatever their settings and ends the duel", Category = Category, TimeoutSeconds = 60f)]
        public static async Task DuelistsLethalHitSubduesAndEndsTheDuel(EngineTestContext ctx)
        {
            using var challenger = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var ring = SpawnRing(ctx);
            await ctx.WaitFrameAsync();

            var duel = await StartAcceptedDuelAsync(ctx, challenger.Creature, target.Creature, ring);
            AssertSettingsOff(ctx, challenger, target);
            ctx.Assert(PlayerList.IsHostileTo(challenger.Creature, target.Creature) && PlayerList.IsHostileTo(target.Creature, challenger.Creature),
                "an accepted duel makes the two players hostile to each other");

            await HitAsync(ctx, challenger.Creature, target.Creature, LethalDamage);
            await WaitForSubdualAsync(ctx, target.Creature, "the loser");
            ctx.AssertEqual(DuelState.Active, duel.State, "a duelist's own blow does not void the duel");

            await ctx.WaitUntilAsync(() => Duels.GetDuel(target.Creature) == null, 8f, "the duel to end");
            ctx.AssertEqual(new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), DuelRules.GetOutcome(duel), "the result");
            ctx.Assert(!GetIsDead(challenger.Creature), "the winner stands");
            ctx.Assert(!PlayerList.IsHostileTo(challenger.Creature, target.Creature) && !PlayerList.IsHostileTo(target.Creature, challenger.Creature),
                "the players are no longer hostile once the duel is over");
        }

        [EngineTest("Both duelists falling in the same pass is a draw and both are subdued", Category = Category, TimeoutSeconds = 60f)]
        public static async Task SimultaneousFallsAreADraw(EngineTestContext ctx)
        {
            using var challenger = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var ring = SpawnRing(ctx);
            await ctx.WaitFrameAsync();

            var duel = await StartAcceptedDuelAsync(ctx, challenger.Creature, target.Creature, ring);

            await HitAsync(ctx, challenger.Creature, target.Creature, LethalDamage);
            await HitAsync(ctx, target.Creature, challenger.Creature, LethalDamage);
            await WaitForSubdualAsync(ctx, target.Creature, "the challenger's blow's victim");
            await WaitForSubdualAsync(ctx, challenger.Creature, "the target's blow's victim");

            await ctx.WaitUntilAsync(() => Duels.GetDuel(challenger.Creature) == null, 8f, "the duel to end");
            ctx.AssertEqual(new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None), DuelRules.GetOutcome(duel), "the result");
        }

        [EngineTest("A duelist who leaves the ring forfeits", Category = Category, TimeoutSeconds = 40f)]
        public static async Task LeavingTheRingForfeits(EngineTestContext ctx)
        {
            using var challenger = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var ring = SpawnRing(ctx);
            await ctx.WaitFrameAsync();

            var duel = await StartAcceptedDuelAsync(ctx, challenger.Creature, target.Creature, ring);

            var position = GetPosition(target.Creature);
            ObjectPlugin.SetPosition(target.Creature, Vector3(position.X - DuelRules.RingRadiusMeters - 2f, position.Y, position.Z));

            await ctx.WaitUntilAsync(() => Duels.GetDuel(challenger.Creature) == null, 8f, "the duel to end once a duelist is outside the ring");
            ctx.AssertEqual(new DuelOutcome(DuelOutcomeType.Forfeit, DuelSide.Challenger), DuelRules.GetOutcome(duel), "the result");
            ctx.Assert(duel.TargetForfeited && !duel.ChallengerForfeited, "only the duelist who left forfeited");
        }

        private static uint SpawnRing(EngineTestContext ctx)
        {
            var ring = CreateObject(ObjectType.Placeable, RingBlueprint, ctx.GetArenaLocation());
            ctx.Assert(GetIsObjectValid(ring), $"the ring terminal blueprint '{RingBlueprint}' spawns");
            ctx.Track(ring);
            return ring;
        }

        /// <summary>Challenges and accepts the way the ring's menus do, then returns the duel.</summary>
        private static async Task<DuelRecord> StartAcceptedDuelAsync(EngineTestContext ctx, uint challenger, uint target, uint ring)
        {
            ctx.AssertEqual(string.Empty, Duels.GetChallengeError(challenger, target, ring), "nothing stops the challenge");

            var challenged = false;
            await ctx.ExecuteInCreatureContextAsync(challenger, () => challenged = Duels.Challenge(challenger, target, ring));
            ctx.Assert(challenged, "the challenge is made");
            ctx.Assert(Duels.GetChallenge(target) != null, "the target has a challenge waiting");

            await ctx.ExecuteInCreatureContextAsync(target, () => Duels.Accept(target));
            var duel = Duels.GetDuel(challenger);
            ctx.Assert(duel != null && ReferenceEquals(duel, Duels.GetDuel(target)), "accepting starts one duel for both players");
            ctx.AssertEqual(DuelState.Active, duel.State, "a new duel is Active");
            return duel;
        }

        private static void AssertSettingsOff(EngineTestContext ctx, PlayerAbilityFixture challenger, PlayerAbilityFixture target)
        {
            ctx.Assert(!DB.Get<Player>(challenger.Id).Settings.IsSubdualModeEnabled &&
                       !DB.Get<Player>(target.Id).Settings.IsSubdualModeEnabled,
                "neither player has subdual mode on, so only the duel can subdue");
        }

        private static async Task HitAsync(EngineTestContext ctx, uint attacker, uint victim, int damage)
        {
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                Combat.ApplyTriggeredDamage(attacker, victim, damage, CombatDamageType.Physical));
        }

        private static async Task WaitForSubdualAsync(EngineTestContext ctx, uint player, string who)
        {
            await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<SubdualPenaltyStatusEffect>(player), 6f,
                $"{who} to be subdued");
            ctx.Assert(!GetIsDead(player), $"{who} is alive after being subdued");
        }
    }
}
