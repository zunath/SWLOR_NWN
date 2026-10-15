using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class InvisibilityEngagementEngineTests
    {
        private const string ActorResref = "nw_bandit001";
        private const string VibroknifeResref = "nw_wswdg001";
        private const int ResourcePool = 9999;
        private const int TemporaryHP = 1000;
        private const float InFlightSwingGraceSeconds = 1.5f;

        [EngineTest("Escape Artist invisibility stops engaged enemies until the activator is visible again", Category = "Vibroknife", TimeoutSeconds = 60f)]
        public static async Task EscapeArtistInvisibilityStopsEngagedEnemies(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var caster = arena.Spawn(ActorResref, 8f, 90f);
            // Autonomous NPC decisions could cast the added feat or swing on their own.
            SetAILevel(caster, AILevel.VeryLow);
            var struck = arena.Spawn(ActorResref, 10f, 270f);
            var secondEnemy = arena.Spawn(ActorResref, 6f, 90f);
            var enemies = new[] { (Creature: struck, Label: "the struck enemy"), (Creature: secondEnemy, Label: "the second enemy") };
            foreach (var (enemy, _) in enemies)
            {
                ctx.MakeHostile(enemy);
                // Heartbeat and combat-round AI only run for creatures above the VeryLow level.
                SetAILevel(enemy, AILevel.High);
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(TemporaryHP), enemy, 3600f);
            }
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(TemporaryHP), caster, 3600f);

            await ctx.WaitFrameAsync();
            ctx.SetResources(caster, ResourcePool, ResourcePool);
            CreaturePlugin.AddFeat(caster, FeatType.EscapeArtist1);
            await ctx.EquipItemAsync(caster, VibroknifeResref, InventorySlot.RightHand);
            ctx.SuppressNPCNaturalRegen(caster);
            foreach (var (enemy, _) in enemies)
                ctx.SuppressNPCNaturalRegen(enemy);

            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var (enemy, _) in enemies)
                    Enmity.ModifyEnmity(caster, enemy, 100);
                await WaitOrFailAsync(ctx,
                    () => IsSwingingAt(struck, caster) && IsSwingingAt(secondEnemy, caster),
                    15f,
                    "both enemies to be swinging at the activator",
                    () => Describe(caster, enemies));

                // An activator using the ability mid-fight has an attack to resume afterwards.
                AssignCommand(caster, () => ActionAttack(struck));
                await ctx.WaitUntilAsync(
                    () => GetCurrentAction(caster) == ActionType.AttackObject,
                    5f,
                    "the activator to be attacking before using the ability");

                var used = false;
                var denial = string.Empty;
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    used = UsePerkFeat.TryUseAbility(caster, struck, FeatType.EscapeArtist1, GetLocation(struck));
                    if (!used)
                        denial = Ability.GetLastActivationDenialReason();
                });
                ctx.Assert(used, $"Escape Artist must activate: {denial}");

                await ctx.WaitUntilAsync(() => IsInvisible(caster), 10f, "Escape Artist's invisibility on the activator");

                // A swing already in flight may still resolve; after that, no enemy may swing at or
                // target the hidden activator across an AI heartbeat and a combat round.
                await ctx.DelaySecondsAsync(InFlightSwingGraceSeconds);
                var quietSince = DateTime.UtcNow;
                for (var sample = 0; sample < 32; sample++)
                {
                    await ctx.DelaySecondsAsync(0.25f);
                    var window = (float)(DateTime.UtcNow - quietSince).TotalSeconds;
                    ctx.Assert(IsInvisible(caster),
                        $"sample {sample}: the activator must stay invisible instead of resuming its attack. {Describe(caster, enemies)}");
                    foreach (var (enemy, label) in enemies)
                    {
                        // The activator is the only creature on each enemy's table, so any swing
                        // would be at it.
                        ctx.Assert(GetAttackTarget(enemy) != caster && !Combat.HasRecentAttackActivity(enemy, window),
                            $"sample {sample}: {label} is still attacking the invisible activator. {Describe(caster, enemies)}");
                        ctx.Assert(Enmity.GetEnmityTable(enemy).ContainsKey(caster),
                            $"sample {sample}: {label} must keep its enmity toward the hidden activator");
                    }
                }

                RemoveInvisibility(caster);
                await WaitOrFailAsync(ctx,
                    () => IsSwingingAt(secondEnemy, caster),
                    15f,
                    "the second enemy to resume swinging at the activator once it is visible",
                    () => Describe(caster, enemies));
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }


        // Attack activity is recorded by the native attack-roll hook, so it reflects real swings.
        private static bool IsSwingingAt(uint enemy, uint target)
        {
            return GetAttackTarget(enemy) == target && Combat.HasRecentAttackActivity(enemy, 3f);
        }

        private static string Describe(uint caster, (uint Creature, string Label)[] enemies)
        {
            var parts = new List<string> { $"activator invisible {IsInvisible(caster)}, action {GetCurrentAction(caster)}" };
            foreach (var (enemy, label) in enemies)
            {
                parts.Add($"{label}: action {GetCurrentAction(enemy)}, targeting activator {GetAttackTarget(enemy) == caster}, " +
                          $"in combat {GetIsInCombat(enemy)}, swung in last 1s {Combat.HasRecentAttackActivity(enemy, 1f)}, " +
                          $"AI level {GetAILevel(enemy)}, enmity entries {Enmity.GetEnmityTable(enemy).Count}, " +
                          $"distance {GetDistanceBetween(enemy, caster):0.0}m");
            }

            return string.Join("; ", parts);
        }

        private static async Task WaitOrFailAsync(EngineTestContext ctx, Func<bool> condition, float timeoutSeconds,
            string description, Func<string> describeState)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                    ctx.Fail($"Timed out after {timeoutSeconds}s waiting for {description}. {describeState()}");
                await ctx.DelaySecondsAsync(0.25f);
            }
        }

        private static bool IsInvisible(uint creature)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (IsInvisibilityEffect(effect))
                    return true;
            }

            return false;
        }

        private static void RemoveInvisibility(uint creature)
        {
            // Collect first: removing an effect mid-iteration skips the next one.
            var invisibility = new List<Effect>();
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (IsInvisibilityEffect(effect))
                    invisibility.Add(effect);
            }

            foreach (var effect in invisibility)
                RemoveEffect(creature, effect);
        }

        private static bool IsInvisibilityEffect(Effect effect)
        {
            var type = GetEffectType(effect);
            return type == EffectTypeScript.Invisibility || type == EffectTypeScript.ImprovedInvisibility;
        }
    }
}
