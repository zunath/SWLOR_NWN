using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The engine reports current HP plus temporary HP as a signed 16-bit value. These tests pin
    /// that limit on a 30,000 HP NPC (the NPC HP cap) and verify temporary HP grants stop at it.
    /// </summary>
    public static class HitPointLimitEngineTests
    {
        private const string LimitCategory = "HitPointLimit";
        private const int MaximumNPCHitPoints = 30000;
        private const int Limit = TemporaryHitPointEffects.MaximumReportedHitPoints;
        private const int Headroom = Limit - MaximumNPCHitPoints;
        // Guardian Ward IV's grant on a 30,000 HP NPC in the CombatLoad run.
        private const int OverflowingTemporaryHP = 4641;
        private const string FirstKey = "HIT_POINT_LIMIT_FIRST";
        private const string SecondKey = "HIT_POINT_LIMIT_SECOND";

        [EngineTest("Native HP reporting holds current plus temporary HP of exactly 32,767", Category = LimitCategory, TimeoutSeconds = 30f)]
        public static async Task NativeHitPointsHoldAtLimit(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var npc = await SpawnNPCAsync(ctx, arena, "nw_rat001", MaximumNPCHitPoints);

            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(Headroom), npc, 60f);
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP at the limit");

            await ctx.DelaySecondsAsync(6f);
            ctx.Assert(!GetIsDead(npc), "An NPC at exactly 32,767 combined HP must stay alive.");
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP after waiting");
        }

        [EngineTest("Native HP reporting past 32,767 wraps and reports the NPC dead until the pool is gone", Category = LimitCategory, TimeoutSeconds = 45f)]
        public static async Task NativeHitPointsWrapPastLimit(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var wrapped = (int)unchecked((short)(MaximumNPCHitPoints + OverflowingTemporaryHP));

            foreach (var immortal in new[] { true, false })
            {
                var label = immortal ? "immortal" : "mortal";
                var npc = await SpawnNPCAsync(ctx, arena, "nw_rat001", MaximumNPCHitPoints, immortal);

                // Raw effect on purpose: production grants are capped by TemporaryHitPointEffects.
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(OverflowingTemporaryHP), npc, 60f);
                ctx.AssertEqual(wrapped, GetCurrentHitPoints(npc), $"{label} native current HP");
                ctx.AssertEqual(MaximumNPCHitPoints, ObjectPlugin.GetCurrentHitPoints(npc), $"{label} base current HP");
                ctx.Assert(GetIsDead(npc), $"The {label} NPC must read as dead as soon as the pool overflows.");

                AssignCommand(npc, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(1), npc));
                await ctx.DelaySecondsAsync(2f);
                ctx.Assert(GetIsDead(npc), $"The {label} NPC must still read as dead while overflowed.");
                ctx.AssertEqual(OverflowingTemporaryHP, TemporaryHitPointEffects.GetRemaining(npc), $"{label} pool after damage while reported dead");
                ctx.AssertEqual(MaximumNPCHitPoints, ObjectPlugin.GetCurrentHitPoints(npc), $"{label} base HP after damage while reported dead");

                RemoveAllTemporaryHitPoints(npc);
                await ctx.WaitUntilAsync(() => !GetIsDead(npc), 5f, $"the {label} NPC to read alive once the pool is removed");
                ctx.AssertEqual(MaximumNPCHitPoints, GetCurrentHitPoints(npc), $"{label} reported HP after removing the pool");
            }

            ctx.SetResultDetail($"30,000 + {OverflowingTemporaryHP} temporary HP reported as {wrapped}. Immortal and mortal NPCs read dead at once, " +
                                "took no damage while overflowed, and read alive at 30,000 once the pool was removed.");
        }

        [EngineTest("Temporary HP grants stop at the engine's 32,767 HP limit", Category = LimitCategory, TimeoutSeconds = 45f)]
        public static async Task TemporaryHitPointGrantsStopAtLimit(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var npc = await SpawnNPCAsync(ctx, arena, "nw_rat001", MaximumNPCHitPoints);

            AbilityEffectScaling.ApplyTemporaryHPPercent(npc, npc, TemporaryHitPointEffectKey.GuardianWard, 15f, 60f);
            ctx.AssertEqual(Headroom, TemporaryHitPointEffects.GetRemaining(npc), "Guardian Ward pool at the limit");
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP at the limit");

            TemporaryHitPointEffects.ApplyFlat(npc, SecondKey, 1000, 60f);
            ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(npc, SecondKey), "a second pool with no room left");

            AssignCommand(npc, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(1000), npc));
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(npc) == Headroom - 1000,
                5f, "damage to be absorbed by the temporary HP pool");
            TemporaryHitPointEffects.ApplyFlat(npc, SecondKey, 5000, 60f);
            ctx.AssertEqual(1000, TemporaryHitPointEffects.GetRemaining(npc, SecondKey), "a second pool fills the absorbed room");
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP after refilling");

            // The cap is measured from maximum HP, so healing a wounded NPC under its pool cannot overflow.
            TemporaryHitPointEffects.Remove(npc, TemporaryHitPointEffectKey.GuardianWard);
            TemporaryHitPointEffects.Remove(npc, SecondKey);
            await ctx.WaitFrameAsync();
            ObjectPlugin.SetCurrentHitPoints(npc, 20000);
            TemporaryHitPointEffects.ApplyFlat(npc, FirstKey, 12000, 60f);
            ctx.AssertEqual(Headroom, TemporaryHitPointEffects.GetRemaining(npc), "a wounded NPC's pool");
            ApplyEffectToObject(DurationType.Instant, EffectHeal(10000), npc);
            await ctx.WaitUntilAsync(() => ObjectPlugin.GetCurrentHitPoints(npc) == MaximumNPCHitPoints,
                5f, "the heal to restore the NPC to full HP");
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP after healing");

            await ctx.DelaySecondsAsync(6f);
            ctx.Assert(!GetIsDead(npc), "A capped NPC must stay alive.");
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP after waiting");
        }

        [EngineTest("Temporary HP replaced at the HP limit waits for the engine to release the old pool", Category = LimitCategory, TimeoutSeconds = 30f)]
        public static async Task ReplacementAtLimitWaitsForReleasedPool(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var npc = await SpawnNPCAsync(ctx, arena, "nw_rat001", MaximumNPCHitPoints, immortal: false);

            TemporaryHitPointEffects.ApplyFlat(npc, FirstKey, 5000, 60f);
            ctx.AssertEqual(Headroom, TemporaryHitPointEffects.GetRemaining(npc), "first pool at the limit");
            await ctx.WaitFrameAsync();

            // The engine keeps counting a removed pool until the frame ends, so a same-frame
            // replacement at full size would overflow and read the NPC as dead.
            TemporaryHitPointEffects.ApplyFlat(npc, FirstKey, 5000, 60f);
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP in the replacement frame");
            ctx.Assert(!GetIsDead(npc), "A same-frame replacement must not read the NPC as dead.");
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(npc, FirstKey) == Headroom,
                3f, "the replaced pool to refill once the engine releases the old one");
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP after the refill");

            TemporaryHitPointEffects.Remove(npc, FirstKey);
            TemporaryHitPointEffects.ApplyFlat(npc, SecondKey, 5000, 60f);
            ctx.AssertEqual(Limit, GetCurrentHitPoints(npc), "reported HP when another pool follows a removal");
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(npc, SecondKey) == Headroom,
                3f, "the second pool to fill the released room");

            // A later removal cancels a grant that is still waiting for room.
            TemporaryHitPointEffects.ApplyFlat(npc, SecondKey, 5000, 60f);
            TemporaryHitPointEffects.Remove(npc, SecondKey);
            await ctx.DelaySecondsAsync(1f);
            ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(npc), "pools after a cancelled refill");
            ctx.AssertEqual(MaximumNPCHitPoints, GetCurrentHitPoints(npc), "reported HP after a cancelled refill");
            ctx.Assert(!GetIsDead(npc), "The NPC must stay alive throughout.");
        }

        [EngineTest("Guardian Ward IV on a 30,000 HP NPC stops at the HP limit and leaves it alive", Category = LimitCategory, TimeoutSeconds = 45f)]
        public static async Task GuardianWardOnMaximumHitPointNPCLeavesItAlive(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var npc = await SpawnNPCAsync(ctx, arena, "nw_bandit001", MaximumNPCHitPoints);
            ctx.SetNPCResources(npc, 100, 100);

            await ctx.ExecuteInCreatureContextAsync(npc, () =>
                ctx.Assert(UsePerkFeat.TryUseAbility(npc, npc, FeatType.GuardianWard4, GetLocation(npc)),
                    $"Guardian Ward IV must start on its caster: {Ability.GetLastActivationDenialReason()}"));
            await ctx.WaitUntilAsync(() => TemporaryHitPointEffects.GetRemaining(npc, TemporaryHitPointEffectKey.GuardianWard) > 0,
                10f, "Guardian Ward IV's temporary HP to land");

            var remaining = TemporaryHitPointEffects.GetRemaining(npc);
            ctx.Assert(remaining <= Headroom, $"Temporary HP must stop at {Headroom}, but the NPC holds {remaining}.");
            ctx.AssertEqual(MaximumNPCHitPoints + remaining, GetCurrentHitPoints(npc), "reported HP");

            await ctx.DelaySecondsAsync(6f);
            ctx.Assert(!GetIsDead(npc), "Guardian Ward IV must not kill a 30,000 HP NPC.");
            ctx.Assert(GetCurrentHitPoints(npc) > 0, $"Reported HP must stay positive, but is {GetCurrentHitPoints(npc)}.");
        }

        [EngineTest("Temporary HP grants below the HP limit keep their full amount", Category = LimitCategory, TimeoutSeconds = 30f)]
        public static async Task TemporaryHitPointGrantsBelowLimitAreUnchanged(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var npc = await SpawnNPCAsync(ctx, arena, "nw_rat001", 10000);

            var expected = Ability.ApplyCombatReadinessMagnitude(
                npc,
                AbilityEffectScaling.CalculateScaledPercentOfMaxHP(npc, npc, 15f));
            AbilityEffectScaling.ApplyTemporaryHPPercent(npc, npc, TemporaryHitPointEffectKey.GuardianWard, 15f, 60f);
            ctx.AssertEqual(expected, TemporaryHitPointEffects.GetRemaining(npc), "Guardian Ward pool");

            TemporaryHitPointEffects.ApplyFlat(npc, SecondKey, 8000, 60f);
            ctx.AssertEqual(8000, TemporaryHitPointEffects.GetRemaining(npc, SecondKey), "a stacked second pool");
            ctx.AssertEqual(10000 + expected + 8000, GetCurrentHitPoints(npc), "reported HP");

            await ctx.WaitFrameAsync();
            AbilityEffectScaling.ApplyTemporaryHPPercent(npc, npc, TemporaryHitPointEffectKey.GuardianWard, 15f, 60f);
            ctx.AssertEqual(expected, TemporaryHitPointEffects.GetRemaining(npc, TemporaryHitPointEffectKey.GuardianWard), "a replaced pool keeps its full amount at once");
        }

        private static async Task<uint> SpawnNPCAsync(
            EngineTestContext ctx,
            QuietArena arena,
            string resref,
            int maximumHitPoints,
            bool immortal = true)
        {
            var npc = arena.Spawn(resref, 0f, 0f);
            // Spawn initialization loads skin HP a frame after creation.
            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(npc);
            Stat.SetNPCMaxHitPoints(npc, maximumHitPoints, true);
            SetImmortal(npc, immortal);
            ctx.AssertEqual(maximumHitPoints, GetMaxHitPoints(npc), "NPC HP budget");
            ctx.AssertEqual(maximumHitPoints, GetCurrentHitPoints(npc), "NPC starts at full HP");
            return npc;
        }

        private static void RemoveAllTemporaryHitPoints(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == EffectTypeScript.TemporaryHitpoints)
                    RemoveEffect(creature, effect);
            }
        }
    }
}
