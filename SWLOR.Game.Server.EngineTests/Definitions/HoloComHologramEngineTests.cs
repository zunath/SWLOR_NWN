using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// HoloCom call holograms are copies of the callers. They stand beside players who may be in combat,
    /// so hostile abilities must skip them and they must never join an enmity table.
    /// </summary>
    public static class HoloComHologramEngineTests
    {
        private const string Resref = "nw_bandit001";
        private const float FieldRadius = 8f;

        [EngineTest("HoloCom holograms are not hostile to enemies and never join enmity tables", Category = "HoloCom", TimeoutSeconds = 60f)]
        public static async Task HologramsAreNonCombatants(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var anchor = arena.Spawn(Resref, 8f, 90f);
            var isolated = new EngineTestContext(ctx.TestName, arena.Area, GetLocation(anchor));
            DestroyObject(anchor);
            try
            {
                await RunNonCombatantAsync(isolated);
            }
            finally
            {
                isolated.Cleanup();
            }
        }

        [EngineTest("A hostile area ability beside a HoloCom hologram hits the player but not the hologram", Category = "HoloCom", TimeoutSeconds = 60f)]
        public static async Task HostileAreaAbilitySkipsHologram(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var anchor = arena.Spawn(Resref, 8f, 90f);
            var isolated = new EngineTestContext(ctx.TestName, arena.Area, GetLocation(anchor));
            DestroyObject(anchor);
            try
            {
                await RunHostileAreaAbilityAsync(isolated);
            }
            finally
            {
                isolated.Cleanup();
            }
        }

        private static async Task RunNonCombatantAsync(EngineTestContext ctx)
        {
            using var sender = await PlayerAbilityFixture.CreateAsync(ctx, 0f);
            using var receiver = await PlayerAbilityFixture.CreateAsync(ctx, 14f);
            var enemy = ctx.SpawnCreature(Resref, -3f, 0f);
            var bystander = ctx.SpawnCreature(Resref, -3f, 3f);
            await ctx.DelaySecondsAsync(1f);
            ctx.MakeHostile(enemy);
            foreach (var creature in new[] { enemy, bystander })
                SetAILevel(creature, AILevel.VeryLow);

            var (holoSender, holoReceiver) = await StartCallAsync(ctx, sender.Creature, receiver.Creature);
            try
            {
                foreach (var hologram in new[] { holoSender, holoReceiver })
                {
                    ctx.Assert(HoloCom.IsHologram(hologram), "the copy is marked as a hologram");
                    ctx.Assert(!HoloCom.IsHologram(HoloCom.GetHoloGramOwner(hologram)), "the calling player is not marked as a hologram");
                    ctx.AssertEqual(HoloCom.HologramFactionId, CreaturePlugin.GetFaction(hologram), "hologram faction");
                    ctx.Assert(!GetIsReactionTypeHostile(hologram, enemy), "the enemy does not treat the hologram as hostile");
                    ctx.Assert(!GetIsReactionTypeHostile(enemy, hologram), "the hologram does not treat the enemy as hostile");
                    ctx.Assert(!GetIsEnemy(hologram, enemy), "the enemy does not consider the hologram an enemy");
                    ctx.Assert(!GetIsReactionTypeHostile(hologram, sender.Creature), "the caller does not treat the hologram as hostile");
                }

                // The callers themselves stay in combat with the enemy.
                ctx.Assert(GetIsReactionTypeHostile(sender.Creature, enemy), "the enemy still treats the calling player as hostile");

                var nearHologram = GetLocation(holoReceiver);
                ctx.Assert(!CombatAreaPulses.GetHostileCreatures(enemy, nearHologram, FieldRadius).Contains(holoReceiver),
                    "area pulses skip the hologram");
                ctx.Assert(CombatAreaPulses.GetHostileCreatures(enemy, nearHologram, FieldRadius).Contains(sender.Creature),
                    "area pulses still find the calling player beside the hologram");
                ctx.Assert(!AbilityTargeting.GetHostileTargetsNearLocation(enemy, nearHologram, FieldRadius, 0).Contains(holoReceiver),
                    "area ability targeting skips the hologram");

                await ctx.ExecuteInCreatureContextAsync(enemy, () =>
                {
                    Enmity.ModifyEnmity(enemy, holoReceiver, 100);
                    Enmity.ModifyEnmity(holoReceiver, enemy, 100);
                    Enmity.ModifyProximityEnmity(holoReceiver, enemy, 100);
                    Enmity.ModifyEnmity(bystander, enemy, 10);
                });

                ctx.AssertEqual(0, Enmity.GetEnmityTable(holoReceiver).Count, "hologram's enmity table size");
                ctx.Assert(!Enmity.GetEnmityTable(enemy).ContainsKey(holoReceiver), "the hologram is not on the enemy's enmity table");
                ctx.Assert(!Enmity.HasEnmity(holoReceiver), "the hologram has no enemies");
                ctx.Assert(Enmity.GetEnmityTable(enemy).ContainsKey(bystander), "ordinary creatures still build enmity");

                await ctx.DelaySecondsAsync(2f);
                foreach (var hologram in new[] { holoSender, holoReceiver })
                {
                    ctx.AssertEqual(OBJECT_INVALID, GetAttackTarget(hologram), "hologram attack target");
                    ctx.Assert(GetCurrentAction(hologram) != ActionType.AttackObject, "the hologram is not attacking");
                    ctx.Assert(!GetIsInCombat(hologram), "the hologram is out of combat");
                }
            }
            finally
            {
                await EndCallAsync(ctx, sender.Creature, receiver.Creature);
            }
        }

        private static async Task RunHostileAreaAbilityAsync(EngineTestContext ctx)
        {
            using var sender = await PlayerAbilityFixture.CreateAsync(ctx, 0f);
            using var receiver = await PlayerAbilityFixture.CreateAsync(ctx, 14f);
            var caster = ctx.SpawnCreature(Resref, -3f, -2f);
            await ctx.DelaySecondsAsync(1f);
            ctx.MakeHostile(caster);
            SetAILevel(caster, AILevel.VeryLow);

            var (holoSender, holoReceiver) = await StartCallAsync(ctx, sender.Creature, receiver.Creature);
            try
            {
                ctx.Assert(GetDistanceBetween(sender.Creature, holoReceiver) < FieldRadius - 1f,
                    "the hologram stands well inside a field centered on its caller");
                var hologramHP = GetCurrentHitPoints(holoReceiver);

                // Creeping Terror III centers its 8 m field on the targeted creature, the calling player.
                var ability = Ability.GetAbilityDetail(FeatType.CreepingTerror3);
                ctx.SetNPCPerkLevel(caster, ability.EffectiveLevelPerkType, ability.AbilityLevel);
                ctx.SetNPCResources(caster, 1000, 1000);
                CreaturePlugin.AddFeat(caster, FeatType.CreepingTerror3);
                Combat.SetAbilityHitResolutionOverride(true);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster, () =>
                        ctx.Assert(UsePerkFeat.TryUseAbility(caster, sender.Creature, FeatType.CreepingTerror3, GetLocation(sender.Creature)),
                            $"Creeping Terror III activates: {Ability.GetLastActivationDenialReason()}"));

                    await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<HobbleStatusEffect>(sender.Creature), 10f,
                        "the first pulse to hobble the calling player");
                    // Let a second pulse run so the hologram has had every chance to be hit.
                    await ctx.DelaySecondsAsync(3.5f);
                }
                finally
                {
                    Combat.SetAbilityHitResolutionOverride(null);
                }

                ctx.Assert(!StatusEffect.HasStatusEffect<HobbleStatusEffect>(holoReceiver), "the hologram is not hobbled");
                ctx.AssertEqual(hologramHP, GetCurrentHitPoints(holoReceiver), "hologram hit points");
                ctx.AssertEqual(0, Enmity.GetEnmityTable(holoReceiver).Count, "hologram's enmity table size");
                ctx.Assert(!Enmity.GetEnmityTable(caster).ContainsKey(holoReceiver), "the hologram is not on the caster's enmity table");
                foreach (var hologram in new[] { holoSender, holoReceiver })
                {
                    ctx.AssertEqual(OBJECT_INVALID, GetAttackTarget(hologram), "hologram attack target");
                    ctx.Assert(GetCurrentAction(hologram) != ActionType.AttackObject, "the hologram is not attacking");
                }
            }
            finally
            {
                await EndCallAsync(ctx, sender.Creature, receiver.Creature);
            }
        }

        private static async Task<(uint HoloSender, uint HoloReceiver)> StartCallAsync(EngineTestContext ctx, uint sender, uint receiver)
        {
            await ctx.ExecuteInCreatureContextAsync(sender, () => HoloCom.SetIsInCall(sender, receiver));
            var holoSender = HoloCom.GetHoloGram(sender);
            var holoReceiver = HoloCom.GetHoloGram(receiver);
            ctx.Assert(GetIsObjectValid(holoSender) && GetIsObjectValid(holoReceiver), "the call creates both holograms");
            ctx.Track(holoSender);
            ctx.Track(holoReceiver);
            await ctx.DelaySecondsAsync(1f);
            return (holoSender, holoReceiver);
        }

        private static Task EndCallAsync(EngineTestContext ctx, uint sender, uint receiver)
            => ctx.ExecuteInCreatureContextAsync(sender, () => HoloCom.SetIsInCall(sender, receiver, false));
    }
}
