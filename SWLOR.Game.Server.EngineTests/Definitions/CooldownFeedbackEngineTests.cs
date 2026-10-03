using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CooldownFeedbackEngineTests
    {
        [EngineTest("Cooldown floating feedback is opt-in and throttled across abilities", Category = "CooldownFeedback", TimeoutSeconds = 30f)]
        public static async Task CooldownPreferenceAndThrottle(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var record = DB.Get<Player>(fixture.Id);
            record.RecastTimes[RecastGroup.ForceSanctuary] = DateTime.UtcNow.AddSeconds(30);
            DB.Set(record);
            const string lastSentVariable = "FEEDBACK_COOLDOWN_FLOATING_LAST_SENT";

            ctx.Assert(!Ability.CanUseAbility(player, player, FeatType.ForceSanctuary1, 1, GetLocation(player)),
                "a cooldown still denies activation with the preference off");
            ctx.Assert(Ability.GetLastActivationDenialReason().StartsWith("This ability can be used in ", StringComparison.Ordinal),
                $"the normal cooldown denial remains available for the combat log; actual denial: {Ability.GetLastActivationDenialReason()}");
            ctx.AssertEqual(string.Empty, GetLocalString(player, lastSentVariable),
                "the default preference sends no floating notice and consumes no throttle");

            record.Settings.DisplayCooldownFloatingText = true;
            DB.Set(record);
            ctx.Assert(!Ability.CanUseAbility(player, player, FeatType.ForceSanctuary1, 1, GetLocation(player)),
                $"the cooldown still denies activation with the preference on; actual denial: {Ability.GetLastActivationDenialReason()}");
            var firstSent = GetLocalString(player, lastSentVariable);
            ctx.Assert(long.TryParse(firstSent, out var timestamp) && timestamp > 0,
                "the real cooldown-denial path sends its first opted-in notice");
            ctx.Assert(!PlayerFeedback.ShowCooldownFloatingText(player, "Force Sanctuary", "29 seconds"),
                "immediate repeated attempts are suppressed");
            ctx.Assert(!PlayerFeedback.ShowCooldownFloatingText(player, "Radiant Lance III", "11 seconds"),
                "switching abilities shares the same throttle");
            ctx.AssertEqual(firstSent, GetLocalString(player, lastSentVariable),
                "suppressed attempts do not extend the throttle");

            await ctx.DelaySecondsAsync(2.1f);
            ctx.Assert(PlayerFeedback.ShowCooldownFloatingText(player, "Radiant Lance III", "9 seconds"),
                "the next notice is allowed after two seconds");
            record.Settings.DisplayCooldownFloatingText = false;
            DB.Set(record);
            await ctx.DelaySecondsAsync(2.1f);
            ctx.Assert(!PlayerFeedback.ShowCooldownFloatingText(player, "Force Sanctuary", "25 seconds"),
                "turning the saved preference off suppresses the next eligible notice");
            var npc = ctx.SpawnCreature("civilian", 2f);
            ctx.Assert(!PlayerFeedback.ShowCooldownFloatingText(npc, "Force Sanctuary", "25 seconds"),
                "NPC attempts cannot send player notices");
        }
    }
}
