using System.Threading.Tasks;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class QuickCastEngineTests
{
    [EngineTest("Optional self-cast uses native player ownership, healing, resources and recast", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task SelfCastGuardsAndActivation(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Perks[PerkType.Innervate] = 1);
        CreaturePlugin.AddFeat(player, FeatType.Innervate1);
        ctx.SetResources(player, 100, 100);
        ObjectPlugin.SetCurrentHitPoints(player, 1);
        QuickCast.ResetSession(player);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out var error) && error.Contains("off"), "self casting is off by default");
                QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
                ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy), "self casting does not enable enemy casting");
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Benevolence I", out _), "unknown native feats are denied");
                ctx.Assert(QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out error), $"known self support activates: {error}; {Ability.GetLastActivationDenialReason()}");
            });
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > 1 && !Activity.IsBusy(player), 8f, "self-cast healing to complete");
            ctx.AssertEqual(97, Stat.GetCurrentFP(player), "the ordinary Innervate resource cost is charged");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out _), "the ordinary recast prevents repeated activation");
                ctx.Assert(Ability.GetLastActivationDenialReason().Contains("can be used in"), "recast denial comes from the regular ability pipeline");
            });
            QuickCast.ResetSession(player);
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self), "a fresh session returns to off");
        }
        finally { QuickCast.ResetSession(player); }
        ctx.SetResultDetail("Native PC known-feat guard, separate off-by-default modes, actual Innervate self-heal, FP cost and recast checked. Client hotkey/cursor behavior requires connected-client verification.");
    }

    [EngineTest("Optional enemy-cast resolves an actual current attack target", Category = "QuickCast", TimeoutSeconds = 40f)]
    public static async Task CurrentEnemyGuardsAndActivation(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Perks[PerkType.ForceDrain] = 1);
        CreaturePlugin.AddFeat(player, FeatType.ForceDrain1);
        ctx.SetResources(player, 100, 100);
        var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
        await ctx.DelaySecondsAsync(1f);
        ctx.MakeHostile(enemy);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 60f);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), enemy, 60f);
        QuickCast.ResetSession(player);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out var error) && error.Contains("off"), "enemy casting is off by default");
                QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, true);
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out error) && error.Contains("Attack an enemy first"), "missing attack target is denied without entering targeting mode");
                ActionAttack(enemy);
            });
            await ctx.WaitUntilAsync(() => GetAttackTarget(player) == enemy, 10f, "player to acquire the native current enemy");
            Ability.ClearLastCompletedAbilityImpactSummary(player);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out var error), $"current-enemy cast activates: {error}; {Ability.GetLastActivationDenialReason()}"));
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player) != null && !Activity.IsBusy(player), 10f, "the current-enemy cast to resolve");
            ctx.AssertEqual("Force Drain I", Ability.GetLastCompletedAbilityImpactSummary(player).Ability.Name, "the chosen ability completed through the regular impact pipeline");
            ctx.AssertEqual(96, Stat.GetCurrentFP(player), "the ordinary hostile-cast resource cost is charged");
            QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, false);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out var error) && error.Contains("off"), "turning the mode off blocks existing shortcuts"));
        }
        finally
        {
            await ctx.ExecuteInCreatureContextAsync(player, () => ClearAllActions(true));
            QuickCast.ResetSession(player);
        }
        ctx.SetResultDetail("Native PC current attack target, default-off/missing-target denial, real Force Drain impact, FP cost and disable guard checked. Client hotkey/cursor behavior requires connected-client verification.");
    }

    [EngineTest("Quick-cast native quickbar bindings require empty slots and preserve later edits", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task QuickbarRoundTrip(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.AddFeat(player, FeatType.Benevolence1);
        QuickCast.ResetSession(player);
        try
        {
            QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
            var original = PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty);
            PlayerPlugin.SetQuickBarSlot(player, 0, PlayerQuickBarSlot.UseSkill(0));
            PlayerPlugin.SetQuickBarSlot(player, 35, original);
            ctx.Assert(!QuickCast.TryBind(player, 0, QuickCastTargetType.Self, "Benevolence I", out _), "slot zero is rejected");
            ctx.Assert(!QuickCast.TryBind(player, 37, QuickCastTargetType.Self, "Benevolence I", out _), "slot 37 is rejected");
            ctx.Assert(!QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out var occupiedError) && occupiedError.Contains("empty"), "occupied shortcuts are rejected with empty-slot guidance");
            ctx.AssertEqual(QuickBarSlotType.Skill, PlayerPlugin.GetQuickBarSlot(player, 0).ObjectType, "rejecting a binding preserves the occupied shortcut");
            PlayerPlugin.SetQuickBarSlot(player, 0, original);
            ctx.Assert(QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "explicit first-slot binding succeeds");
            ctx.Assert(QuickCast.IsBoundCommand(PlayerPlugin.GetQuickBarSlot(player, 0), $"/castself {(int)FeatType.Benevolence1}"), "native quickbar contains a cursor-free chat command");
            ctx.Assert(QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "repeated binding retains the original slot");
            ctx.Assert(QuickCast.TryUnbind(player, 1, out _), "unbind succeeds");
            ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, 0).ObjectType, "unbind restores the originally empty slot");
            ctx.Assert(QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "a later explicit rebind succeeds");
            PlayerPlugin.SetQuickBarSlot(player, 0, PlayerQuickBarSlot.UseFeat(FeatType.Benevolence1));
            ctx.Assert(!QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "rebinding cannot overwrite a later manual edit");
            ctx.Assert(QuickCast.TryBind(player, 36, QuickCastTargetType.Self, "Benevolence I", out _), "last slot binds without off-by-one errors");
            QuickCast.ResetSession(player);
            ctx.AssertEqual(QuickBarSlotType.Feat, PlayerPlugin.GetQuickBarSlot(player, 0).ObjectType, "session reset preserves a player's subsequent manual edit");
            ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, 35).ObjectType, "session reset clears an unchanged bound slot");
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self), "session reset disables the mode");
        }
        finally { QuickCast.ResetSession(player); }
        ctx.SetResultDetail("Native quickbar command slots 1 and 36, invalid bounds, occupied-slot rejection, rebinding, empty-slot restoration, later manual edits and session reset verified. Headless fixture does not click the client's hotbar.");
    }
}
