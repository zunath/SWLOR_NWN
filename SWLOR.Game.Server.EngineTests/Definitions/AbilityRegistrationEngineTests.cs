using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Validates the reflection-based ability cache built in Ability.CacheData during
    /// OnModuleCacheBefore: every feat registered to an ability must have a real display name,
    /// and looking it back up by feat must return the same instance without throwing.
    /// </summary>
    public static class AbilityRegistrationEngineTests
    {
        [EngineTest("Every registered ability has a name and resolves without throwing", Category = "Ability")]
        public static async Task AllRegisteredAbilitiesResolve(EngineTestContext ctx)
        {
            var abilities = Ability.GetAllAbilityDetails();
            ctx.Assert(abilities.Count > 0, "Expected at least one ability to be registered in the live engine.");

            var count = 0;

            foreach (var (feat, detail) in abilities)
            {
                // Yield periodically so this sweep stays preemptable by the runner's cooperative timeout
                // and observes cancellation, so a timed-out sweep settles during the grace period.
                if (++count % 100 == 0)
                {
                    await ctx.WaitFrameAsync();
                }

                ctx.Assert(!string.IsNullOrWhiteSpace(detail.Name), $"Ability registered to feat '{feat}' has no Name.");

                AbilityDetail resolved;
                try
                {
                    resolved = Ability.GetAbilityDetail(feat);
                }
                catch (Exception ex)
                {
                    ctx.Fail($"Ability.GetAbilityDetail threw for feat '{feat}': {ex.Message}");
                    return;
                }

                ctx.Assert(ReferenceEquals(detail, resolved), $"GetAbilityDetail({feat}) should return the same cached instance as GetAllAbilityDetails.");
            }
        }
    }
}
