using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ShadowStepEngineTests
    {
        private const string ActorResref = "nw_bandit001";
        private const int TemporaryHP = 1000;
        private const float MaximumFacingDriftDegrees = 20f;
        private const float ArrivalDistanceMeters = 1.5f;

        [EngineTest("Shadow Step lands behind an engaged target that stays facing away while stunned", Category = "Espionage", TimeoutSeconds = 40f)]
        public static async Task ShadowStepKeepsEngagedTargetFacingAway(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var caster = arena.Spawn(ActorResref, 6f, 90f);
            var target = arena.Spawn(ActorResref, 9f, 270f);
            ctx.MakeHostile(target);
            foreach (var creature in new[] { caster, target })
            {
                // Normal AI timing so queued actions run on the next update.
                SetAILevel(creature, AILevel.High);
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(TemporaryHP), creature, 3600f);
            }

            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(caster);
            ctx.SuppressNPCNaturalRegen(target);
            ctx.SeedRandom(4417);
            var ability = new ShadowStepAbilityDefinition().BuildAbilities()[FeatType.ShadowStep1];

            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                // Mid-fight: an engaged target keeps turning toward whoever it is fighting, which
                // is the situation the tester reported.
                Enmity.ModifyEnmity(caster, target, 100);
                var engageDeadline = DateTime.UtcNow.AddSeconds(15);
                while (!(GetAttackTarget(target) == caster && Combat.HasRecentAttackActivity(target, 3f) &&
                         FacingDrift(target, BearingTo(target, caster)) < MaximumFacingDriftDegrees))
                {
                    if (DateTime.UtcNow >= engageDeadline)
                        ctx.Fail($"Timed out waiting for the target to swing at and face the caster: {Describe(caster, target)}");
                    await ctx.DelaySecondsAsync(0.25f);
                }

                var facingAtCast = GetFacing(target);
                var landingPoint = BehindPosition(target, facingAtCast);
                var evasionBeforeCast = Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment);

                // Drive the impact directly. The activation pipeline is covered by the Espionage
                // behavior cases; isolate the target-facing and arrival-stun assertions here.
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    ClearAllActions(true);
                    Ability.BeginAbilityImpact(caster, ability);
                    try
                    {
                        ability.ImpactAction(caster, target, 1, GetLocation(target));
                    }
                    finally
                    {
                        Ability.EndAbilityImpact(caster);
                    }
                });

                var stunDeadline = DateTime.UtcNow.AddSeconds(10);
                var closestToLandingPoint = float.MaxValue;
                while (!StatusEffect.HasStatusEffect<StunnedStatusEffect>(target) && DateTime.UtcNow < stunDeadline)
                {
                    closestToLandingPoint = Math.Min(closestToLandingPoint, System.Numerics.Vector3.Distance(GetPosition(caster), landingPoint));
                    await ctx.DelaySecondsAsync(0.1f);
                }
                ctx.Assert(StatusEffect.HasStatusEffect<StunnedStatusEffect>(target),
                    $"Shadow Step's arrival stun never landed (impact evasion delta {Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment) - evasionBeforeCast}, " +
                    $"closest approach to the landing point {closestToLandingPoint:0.0}m, cast-time facing {facingAtCast:0}°): {Describe(caster, target)}");

                // Sample across most of the 2s stun: the activator must stay behind a target
                // that keeps the facing it had when Shadow Step was cast.
                for (var sample = 0; sample < 6; sample++)
                {
                    var drift = FacingDrift(target, facingAtCast);
                    ctx.Assert(drift < MaximumFacingDriftDegrees,
                        $"sample {sample}: the stunned target turned {drift:0}° away from its cast-time facing {facingAtCast:0}° (now {GetFacing(target):0}°)");
                    ctx.Assert(Combat.IsAttackerBehindTarget(caster, target),
                        $"sample {sample}: the activator must be behind the target (target facing {GetFacing(target):0}°, bearing to activator {BearingTo(target, caster):0}°)");
                    await ctx.DelaySecondsAsync(0.25f);
                }
            }
            finally
            {
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }

        [EngineTest("Shadow Step teleports behind the target while idle and while attacking at both ranks", Category = "ShadowStep", TimeoutSeconds = 60f)]
        public static async Task TeleportsBehindTargetDuringMelee(EngineTestContext ctx)
        {
            var arena = await QuietArena.CreateAsync(ctx);
            var abilities = new ShadowStepAbilityDefinition().BuildAbilities();
            foreach (var feat in new[] { FeatType.ShadowStep1, FeatType.ShadowStep2 })
            foreach (var attacking in new[] { false, true })
            {
                var activator = arena.Spawn("nw_bandit001", 8f, 90f);
                var target = arena.Spawn("nw_bandit001", 9.5f, 270f);
                await ctx.WaitFrameAsync();
                foreach (var creature in new[] { activator, target })
                {
                    SetAILevel(creature, AILevel.VeryLow);
                    Stat.SetNPCMaxHitPoints(creature, 1000, true);
                }
                ctx.MakeHostile(target);

                if (attacking)
                {
                    AssignCommand(activator, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => GetCurrentAction(activator) == ActionType.AttackObject,
                        5f, "the activator to be attacking in melee before Shadow Step");
                    ctx.Assert(GetDistanceBetween(activator, target) < 2f, "The attack starts in melee range");
                }

                await ctx.ExecuteInCreatureContextAsync(activator, () =>
                {
                    // Exercise the impact with a live attack still on the native action queue.
                    // Check before yielding so combat resumption cannot mask a delayed jump.
                    var position = GetPosition(target);
                    var radians = GetFacing(target) * Math.PI / 180.0;
                    var destination = Location(GetArea(target), Vector3(
                        position.X - (float)Math.Cos(radians) * 1.5f,
                        position.Y - (float)Math.Sin(radians) * 1.5f,
                        position.Z), GetFacing(target));
                    abilities[feat].ImpactAction(activator, target, abilities[feat].AbilityLevel, GetLocation(target));
                    ctx.Assert(GetDistanceBetweenLocations(GetLocation(activator), destination) < 0.5f,
                        $"{feat} must teleport behind its target immediately (attacking={attacking})");
                });

                DestroyObject(activator);
                DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
        }

        private static string Describe(uint caster, uint target)
        {
            var enmity = Enmity.GetEnmityTable(target)
                .Select(entry => $"{(entry.Key == caster ? "caster" : GetResRef(entry.Key))}={entry.Value}");
            return $"target action {GetCurrentAction(target)}, targeting caster {GetAttackTarget(target) == caster}, " +
                   $"swung in last 3s {Combat.HasRecentAttackActivity(target, 3f)}, " +
                   $"target facing {GetFacing(target):0}° vs bearing to caster {BearingTo(target, caster):0}°, " +
                   $"distance {GetDistanceBetween(caster, target):0.0}m, caster action {GetCurrentAction(caster)}, " +
                   $"enmity table [{string.Join(", ", enmity)}]";
        }

        private static System.Numerics.Vector3 BehindPosition(uint target, float facing)
        {
            var position = GetPosition(target);
            var radians = facing * Math.PI / 180.0;
            return new System.Numerics.Vector3(
                position.X - (float)Math.Cos(radians) * ArrivalDistanceMeters,
                position.Y - (float)Math.Sin(radians) * ArrivalDistanceMeters,
                position.Z);
        }

        private static float BearingTo(uint from, uint to)
        {
            var origin = GetPosition(from);
            var destination = GetPosition(to);
            var degrees = (float)(Math.Atan2(destination.Y - origin.Y, destination.X - origin.X) * 180.0 / Math.PI);
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private static float FacingDrift(uint creature, float expectedFacing)
        {
            var difference = Math.Abs(GetFacing(creature) - expectedFacing) % 360f;
            return difference > 180f ? 360f - difference : difference;
        }
    }
}
