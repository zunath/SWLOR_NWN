using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Ghost Protocol enters stealth for up to 30 seconds. Its timer must end only the stealth it
/// started: leaving early and re-entering stealth by hand is ordinary stealth that outlasts it.
/// </summary>
public static class GhostProtocolStealthEngineTests
{
    private const float StealthWindowSeconds = 30f;

    [EngineTest("Ghost Protocol stealth ends after its window", Category = "GhostProtocolStealth", TimeoutSeconds = 90f)]
    public static async Task WindowEndsGhostProtocolStealth(EngineTestContext ctx)
    {
        using var player = await CreateStealthPlayerAsync(ctx);
        var creature = player.Creature;

        await CastGhostProtocolAsync(ctx, creature);
        await ctx.DelaySecondsAsync(StealthWindowSeconds - 3f);
        ctx.Assert(GetActionMode(creature, ActionMode.Stealth), "Ghost Protocol stealth lasts until its window closes");

        await ctx.WaitUntilAsync(() => !GetActionMode(creature, ActionMode.Stealth), 10f,
            "Ghost Protocol stealth to end when its window closes");
    }

    [EngineTest("Ghost Protocol window spares stealth entered after leaving early", Category = "GhostProtocolStealth", TimeoutSeconds = 90f)]
    public static async Task WindowSparesLaterManualStealth(EngineTestContext ctx)
    {
        using var player = await CreateStealthPlayerAsync(ctx);
        var creature = player.Creature;

        await CastGhostProtocolAsync(ctx, creature);
        await SetStealthAsync(ctx, creature, false);
        await ctx.DelaySecondsAsync(1f);
        await SetStealthAsync(ctx, creature, true);

        await ctx.DelaySecondsAsync(StealthWindowSeconds + 5f);
        ctx.Assert(GetActionMode(creature, ActionMode.Stealth),
            "stealth entered by hand after leaving Ghost Protocol early outlasts the Ghost Protocol window");

        await SetStealthAsync(ctx, creature, false);
    }

    private static async Task<PlayerAbilityFixture> CreateStealthPlayerAsync(EngineTestContext ctx)
    {
        var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.Perks[PerkType.Stealth] = 1;
            record.Perks[PerkType.GhostProtocol] = 1;
        });
        ctx.Assert(!GetActionMode(player.Creature, ActionMode.Stealth), "the player starts out of stealth");
        return player;
    }

    private static async Task CastGhostProtocolAsync(EngineTestContext ctx, uint creature)
    {
        var ability = Ability.GetAbilityDetail(FeatType.GhostProtocol);
        await ctx.ExecuteInCreatureContextAsync(creature,
            () => ability.ImpactAction(creature, creature, 1, GetLocation(creature)));
        await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth), 5f,
            "Ghost Protocol to enter stealth");
    }

    private static async Task SetStealthAsync(EngineTestContext ctx, uint creature, bool enabled)
    {
        await ctx.ExecuteInCreatureContextAsync(creature, () => SetActionMode(creature, ActionMode.Stealth, enabled));
        await ctx.WaitUntilAsync(() => GetActionMode(creature, ActionMode.Stealth) == enabled, 5f,
            enabled ? "the player to enter stealth" : "the player to leave stealth");
    }
}
