using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// End-to-end combat smoke test: commands a base-game humanoid to attack a hostile-faction
    /// target and confirms the target's hit points drop, proving the native attack-roll and
    /// damage-roll hook pipeline runs end-to-end against real creatures.
    /// </summary>
    public static class CombatPipelineEngineTests
    {
        private const int Seed = 20260721;

        [EngineTest("A commanded attack against a hostile target reduces its hit points", Category = "Combat", TimeoutSeconds = 120f)]
        public static async Task CommandedAttackReducesTargetHitPoints(EngineTestContext ctx)
        {
            ctx.SeedRandom(Seed);

            // Tight spawn geometry, matching the behavior executor: the attacker starts within
            // melee reach, so no pathing is needed. Wider splits left the attacker stuck in
            // AttackObject at a constant ~3.5m across runs - walkmesh pathing at the arena
            // anchor never brought it into range.
            var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f, 0f);
            var target = ctx.SpawnCreature("nw_rat001", 1.0f, 0f);
            ctx.MakeHostile(target);

            // A large temporary-HP buffer keeps the 5-HP rat alive even if arena bystanders
            // engage it - a dead target before our attacker's first credited hit would
            // otherwise time this test out. The target is also pinned in place: a surviving
            // hostile rat otherwise runs off fighting bystanders and the attacker chases at
            // ~3.5m without ever reaching melee range (live-run diagnostic).
            ApplyEffectToObject(
                DurationType.Temporary,
                EffectTemporaryHitpoints(1000),
                target,
                3600f);
            ApplyEffectToObject(
                DurationType.Temporary,
                EffectCutsceneImmobilize(),
                target,
                3600f);

            var startingHP = GetCurrentHitPoints(target);

            AssignCommand(attacker, () => ActionAttack(target));

            // The default arena is an instanced copy of the module starting area, which contains
            // placed creatures that may also engage a Hostile-faction target. Requiring the
            // spawned attacker to be the last damager proves the damage came through our
            // commanded attack rather than a bystander.
            try
            {
                await ctx.WaitUntilAsync(
                    () => GetIsObjectValid(target) &&
                          GetCurrentHitPoints(target) < startingHP &&
                          GetLastDamager(target) == attacker,
                    90f,
                    "the commanded attacker's damage to lower the target's hit points below its starting value");
            }
            catch (EngineTestAssertionException ex)
            {
                throw new EngineTestAssertionException(
                    $"{ex.Message} (targetValid={GetIsObjectValid(target)}, targetHP={GetCurrentHitPoints(target)}/{startingHP}, " +
                    $"lastDamagerIsAttacker={GetLastDamager(target) == attacker}, distance={GetDistanceBetween(attacker, target)}, " +
                    $"attackerAction={GetCurrentAction(attacker)})");
            }
        }
    }
}
