using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ForceLeapEngineTests
    {
        [EngineTest("Force Leap lands beside a target its damage kills", Category = "ForceLeap", TimeoutSeconds = 30f)]
        public static async Task LeapLandsWhenDamageKills(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -0.5f);
            SetAILevel(caster, AILevel.VeryLow);
            // The stock rat has 1 HP, so Force Leap's damage is always lethal.
            var target = ctx.SpawnCreature("nw_rat001", 9.5f);
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(caster, 50, 50);

            var targetPosition = GetPosition(target);
            var used = false;
            var denial = string.Empty;
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    used = UsePerkFeat.TryUseAbility(caster, target, FeatType.ForceLeap1, GetLocation(target));
                    if (!used)
                        denial = Ability.GetLastActivationDenialReason();
                });
                ctx.Assert(used, $"Force Leap I starts: {denial}");

                await ctx.WaitUntilAsync(() => !GetIsObjectValid(target) || GetIsDead(target), 5f,
                    "Force Leap's damage to kill the target");
                await ctx.WaitUntilAsync(() => GetDistanceBetweenLocations(GetLocation(caster),
                        Location(ctx.Arena, targetPosition, 0f)) <= 2f, 3f,
                    "the caster to land beside the killed target");
                ctx.Log($"Caster landed {GetDistanceBetweenLocations(GetLocation(caster), Location(ctx.Arena, targetPosition, 0f)):0.00}m from the target.");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                TrackCorpsePlaceable(ctx, target);
            }
        }

        private static void TrackCorpsePlaceable(EngineTestContext ctx, uint body)
        {
            if (!GetIsObjectValid(body))
                return;

            for (var nth = 1; ; nth++)
            {
                var placeable = GetNearestObject(ObjectType.Placeable, body, nth);
                if (!GetIsObjectValid(placeable) || GetDistanceBetween(body, placeable) > 10f)
                    return;

                if (GetLocalObject(placeable, Loot.CorpseBodyVariable) == body)
                {
                    ctx.Track(placeable);
                    return;
                }
            }
        }
    }
}
