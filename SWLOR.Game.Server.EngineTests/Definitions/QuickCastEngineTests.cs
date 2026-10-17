using System.Collections.Generic;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Ability = SWLOR.Game.Server.Service.Ability;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class QuickCastEngineTests
{
    [EngineTest("Power Cell III self-casts through its native alias and restores stamina", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task NativePowerCellIII(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record =>
        {
            record.Perks[PerkType.PowerCell] = 3;
            record.Skills[SkillType.Devices].Rank = 50;
        });
        CreaturePlugin.AddFeat(player, FeatType.PowerCell3);
        ctx.SetResources(player, 100, 100);
        fixture.Update(record => record.Stamina = 50);
        SetLocalInt(player, Stat.SuppressNaturalRegenVariable, 1);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
            ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastPowerCell3, out _), "default-off setting rejects alias"));
        fixture.Update(record => record.Settings.SelfQuickCastEnabled = true);
        QuickCast.ApplyPreferences(player);
        ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.AutoCastPowerCell3), "self setting grants native alias");
        ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, FeatType.PowerCell3), "manual presentation is replaced");
        await ctx.ExecuteInCreatureContextAsync(player, () => UseNativeFeat(player, FeatType.AutoCastPowerCell3));
        await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<Feature.StatusEffectDefinition.PowerCell3StatusEffect>(player) &&
                                      !Activity.IsBusy(player), 8f, "self-targeted Power Cell impact");
        ctx.AssertEqual(61, Stat.GetCurrentStamina(player), "seven stamina cost and eighteen percent restoration");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
            ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastPowerCell3, out _), "normal cooldown blocks repeated cast"));
        fixture.Update(record => record.Settings.SelfQuickCastEnabled = false);
        QuickCast.ApplyPreferences(player);
        ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.PowerCell3), "disabling restores manual presentation");
        ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, FeatType.AutoCastPowerCell3), "disabling removes alias");
    }

    private static unsafe void UseNativeFeat(uint player, FeatType feat)
    {
        // Normal client hotkeys reach CNWSCreature.UseFeat with self as the alias target.
        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
        var position = GetPosition(player);
        using var vector = new Vector(position.X, position.Y, position.Z);
        native.UseFeat((ushort)feat, 0, player, GetArea(player), vector);
    }

    [EngineTest("Normal feat events self-cast multiple support abilities through the ordinary pipeline", Category = "QuickCast", TimeoutSeconds = 45f)]
    public static async Task NativeSupportAbilities(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record =>
        {
            record.Perks[PerkType.Innervate] = 1;
            record.Perks[PerkType.RousingShout] = 3;
            record.Skills[SkillType.Leadership].Rank = 50;
        });
        CreaturePlugin.AddFeat(player, FeatType.Innervate1);
        CreaturePlugin.AddFeat(player, FeatType.RousingShout3);
        ctx.SetResources(player, 100, 100);
        ObjectPlugin.SetCurrentHitPoints(player, 1);
        ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self), "new character defaults to manual targeting");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastInnervate1, out _), "stale alias cannot bypass default-off preference");
            ctx.AssertEqual(100, Stat.GetCurrentFP(player), "disabled shortcut spends nothing");
        });
        fixture.Update(record => record.Settings.SelfQuickCastEnabled = true);
        QuickCast.ApplyPreferences(player);
        foreach (var feat in new[] { FeatType.Innervate1, FeatType.RousingShout3 })
        {
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, feat), "manual presentation is replaced, not duplicated");
            ctx.Assert(CreaturePlugin.GetKnowsFeat(player, QuickCast.GetDisplayedFeat(player, feat)), "every eligible known support feat gets automatic presentation");
        }
        ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy), "self preference is independent");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastBenevolence1, out _), "unowned ability remains unavailable");
            UseNativeFeat(player, FeatType.AutoCastInnervate1);
        });
        await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > 1 && !Activity.IsBusy(player), 8f, "native feat event self-heal");
        ctx.AssertEqual(97, Stat.GetCurrentFP(player), "normal FP cost is charged");
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastInnervate1, out _), "normal recast prevents repeated activation");
            ctx.Assert(Ability.GetLastActivationDenialReason().Contains("can be used in"), "recast denial comes from the normal ability pipeline");
        });
        var beforeShoutHP = GetCurrentHitPoints(player);
        await ctx.ExecuteInCreatureContextAsync(player, () => UseNativeFeat(player, FeatType.AutoCastRousingShout3));
        await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > beforeShoutHP && !Activity.IsBusy(player), 8f, "second support ability from native hotkey route");
        ctx.AssertEqual(90, Stat.GetCurrentStamina(player), "Rousing Shout charges normal stamina");
        fixture.Update(record => record.Settings.SelfQuickCastEnabled = false);
        QuickCast.ApplyPreferences(player);
        ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.Innervate1), "disabling restores manual native feat");
        ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, FeatType.AutoCastInnervate1), "disabling removes automatic native feat");
    }

    [EngineTest("Normal hostile feat events use a live queued enemy and reject missing or stale targets", Category = "QuickCast", TimeoutSeconds = 45f)]
    public static async Task NativeCurrentEnemy(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record =>
        {
            record.Perks[PerkType.ForceDrain] = 1;
            record.Settings.EnemyQuickCastEnabled = true;
        });
        CreaturePlugin.AddFeat(player, FeatType.ForceDrain1);
        QuickCast.ApplyPreferences(player);
        ctx.SetResources(player, 100, 100);
        var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
        await ctx.DelaySecondsAsync(1f);
        ctx.MakeHostile(enemy);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 60f);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), enemy, 60f);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastForceDrain1, out var error) && error.Contains("Attack a living enemy"), "missing enemy does not start a cursor or cast");
                ctx.AssertEqual(100, Stat.GetCurrentFP(player), "failed shortcut spends no FP");
                ActionAttack(enemy);
            });
            await ctx.WaitUntilAsync(() => GetAttackTarget(player) == enemy, 10f, "native attack target");
            Ability.ClearLastCompletedAbilityImpactSummary(player);
            await ctx.ExecuteInCreatureContextAsync(player, () => UseNativeFeat(player, FeatType.AutoCastForceDrain1));
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player) != null && !Activity.IsBusy(player), 10f, "native hostile feat impact");
            ctx.AssertEqual("Force Drain I", Ability.GetLastCompletedAbilityImpactSummary(player).Ability.Name, "ordinary hostile ability pipeline completed");
            ctx.AssertEqual(96, Stat.GetCurrentFP(player), "ordinary FP cost is charged");
            await ctx.ExecuteInCreatureContextAsync(player, () => ClearAllActions(true));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastForceDrain1, out var error) && error.Contains("Attack a living enemy"), "canceled attack cannot reuse stale target"));
            fixture.Update(record => record.Settings.EnemyQuickCastEnabled = false);
            QuickCast.ApplyPreferences(player);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastForceDrain1, out var error) && error.Contains("disabled"), "old shortcut cannot bypass preference changes"));
        }
        finally { await ctx.ExecuteInCreatureContextAsync(player, () => ClearAllActions(true)); }
    }

    [EngineTest("Automatic targeting respects native incapacitation and permits rooted casts", Category = "QuickCast", TimeoutSeconds = 60f)]
    public static async Task IncapacitationGuards(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        SetPlotFlag(player, false);
        fixture.Update(record =>
        {
            record.Perks[PerkType.Innervate] = 1;
            record.Perks[PerkType.ForceDrain] = 1;
            record.Settings.SelfQuickCastEnabled = true;
            record.Settings.EnemyQuickCastEnabled = true;
        });
        CreaturePlugin.AddFeat(player, FeatType.Innervate1);
        CreaturePlugin.AddFeat(player, FeatType.ForceDrain1);
        QuickCast.ApplyPreferences(player);
        ctx.SetResources(player, 100, 100);
        ObjectPlugin.SetCurrentHitPoints(player, 1);
        var controls = new (string Name, Func<Effect> Create)[]
        {
            ("stun", EffectStunned), ("daze", EffectDazed), ("sleep", EffectSleep),
            ("paralysis", EffectParalyze), ("cutscene paralysis", EffectCutsceneParalyze),
            ("petrification", EffectPetrify), ("knockdown", EffectKnockdown),
            ("confusion", EffectConfused), ("fear", EffectFrightened),
            ("domination", EffectDominated), ("cutscene domination", EffectCutsceneDominated), ("turning", EffectTurned)
        };
        const string effectTag = "AUTOMATIC_TARGET_CONTROL_TEST";
        try
        {
            foreach (var control in controls)
            {
                ApplyEffectToObject(DurationType.Temporary, TagEffect(IgnoreEffectImmunity(control.Create()), effectTag), player, 60f);
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastInnervate1, out var error) && error.Contains("incapacitated"), $"{control.Name} blocks self casting");
                    ctx.Assert(!QuickCast.TryCast(player, FeatType.AutoCastForceDrain1, out error) && error.Contains("incapacitated"), $"{control.Name} blocks enemy casting before target resolution");
                    ctx.AssertEqual(1, GetCurrentHitPoints(player), "blocked casts do not heal");
                    ctx.AssertEqual(100, Stat.GetCurrentFP(player), "blocked casts do not spend FP");
                    ctx.Assert(!Activity.IsBusy(player), "blocked casts do not start activation");
                    ctx.Assert(!Recast.IsOnRecastDelay(player, Ability.GetAbilityDetail(FeatType.Innervate1).RecastGroup).Item1, "blocked casts do not start recast");
                });
                RemoveEffectByTag(player, effectTag);
                await ctx.WaitFrameAsync();
            }
            ApplyEffectToObject(DurationType.Temporary, TagEffect(EffectCutsceneImmobilize(), effectTag), player, 60f);
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(QuickCast.TryCast(player, FeatType.AutoCastInnervate1, out var error), $"root still permits cast: {error}"));
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > 1 && !Activity.IsBusy(player), 8f, "rooted heal");
            ctx.AssertEqual(97, Stat.GetCurrentFP(player), "rooted cast charges ordinary FP");
        }
        finally { RemoveEffectByTag(player, effectTag); }
    }
}
