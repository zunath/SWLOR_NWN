using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Exercises production Settings loaders and Save against a native PC and disposable Redis.
/// Client clicks, rendering, and a server process restart require separate verification.
/// </summary>
public static class SettingsTargetingEngineTests
{
    private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

    [EngineTest("Settings targeting saves independent preferences through legacy Redis reload cycles", Category = "QuickCast", TimeoutSeconds = 45f)]
    public static async Task SaveAndReloadPreferences(EngineTestContext ctx)
    {
        var template = Gui.GetWindowTemplate(GuiWindowType.Settings);
        ctx.AssertEqual(0, template.LayoutFindings.Count, "the production Settings layout has no warnings");
        Console.WriteLine("[SETTINGS_TARGETING_NUI] " + JsonConvert.SerializeObject(new
        {
            root = JObject.Parse(JsonDump(template.Window)),
            partials = template.PartialViewLayouts.ToDictionary(entry => entry.Key, entry => JObject.Parse(entry.Value)),
            geometry = template.InitialGeometry
        }));

        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
            .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var playerWindows = new Dictionary<GuiWindowType, GuiPlayerWindow>();
        foreach (var type in new[] { GuiWindowType.PlayerStatus, GuiWindowType.PlayerStatusPortrait, GuiWindowType.PlayerStatusPortraitSpace })
        {
            var statusTemplate = Gui.GetWindowTemplate(type);
            var window = statusTemplate.CreatePlayerWindowAction();
            window.ViewModel.Geometry = statusTemplate.InitialGeometry;
            playerWindows.Add(type, window);
        }
        windows.Add(fixture.Id, playerWindows);
        try
        {
            // Keep the legacy fields genuinely absent in Redis until the first production Save.
            var legacySettings = (JObject)JObject.Parse(DB.GetRawJson<Player>(fixture.Id))[nameof(Player.Settings)];
            legacySettings.Remove(nameof(PlayerSettings.SelfQuickCastEnabled));
            legacySettings.Remove(nameof(PlayerSettings.EnemyQuickCastEnabled));
            typeof(DB).GetMethod("SetUnindexedJsonProperty", BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(Player)).Invoke(null,
                    new object[] { fixture.Id, nameof(Player.Settings), legacySettings.ToString(Formatting.None) });
            var persistedLegacy = JObject.Parse(DB.GetRawJson<Player>(fixture.Id))[nameof(Player.Settings)];
            ctx.Assert(persistedLegacy[nameof(PlayerSettings.SelfQuickCastEnabled)] == null &&
                       persistedLegacy[nameof(PlayerSettings.EnemyQuickCastEnabled)] == null,
                "the starting Redis record has neither targeting preference");
            AssertPreferences(ctx, ReadPersisted(fixture.Id).Settings, false, false, "legacy reload");
            var legacyModel = LoadSettings(player);
            ctx.Assert(!legacyModel.SelfQuickCastEnabled && !legacyModel.EnemyQuickCastEnabled,
                "legacy preferences render as unchecked drafts");

            legacyModel.SelfQuickCastEnabled = true;
            legacyModel.EnemyQuickCastEnabled = true;
            AssertPreferences(ctx, ReadPersisted(fixture.Id).Settings, false, false, "unsaved legacy draft");
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self) &&
                       !QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy),
                "unsaved checkboxes do not change automatic targeting");
            var discardedDraft = LoadSettings(player);
            ctx.Assert(!discardedDraft.SelfQuickCastEnabled && !discardedDraft.EnemyQuickCastEnabled,
                "reopening without Save discards targeting drafts");

            var cases = new[] { (Self: true, Enemy: false), (Self: false, Enemy: true), (Self: true, Enemy: true), (Self: false, Enemy: false) };
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                foreach (var desired in cases)
                {
                    var previous = ReadPersisted(fixture.Id).Settings;
                    var model = LoadSettings(player);
                    model.SelfQuickCastEnabled = desired.Self;
                    model.EnemyQuickCastEnabled = desired.Enemy;
                    AssertPreferences(ctx, ReadPersisted(fixture.Id).Settings,
                        previous.SelfQuickCastEnabled, previous.EnemyQuickCastEnabled, $"cycle {cycle}: draft");
                    ctx.AssertEqual(previous.SelfQuickCastEnabled, QuickCast.IsEnabled(player, QuickCastTargetType.Self),
                        $"cycle {cycle}: self draft is unapplied");
                    ctx.AssertEqual(previous.EnemyQuickCastEnabled, QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy),
                        $"cycle {cycle}: enemy draft is unapplied");

                    Player cachedAfterSave = null;
                    await ctx.ExecuteInCreatureContextAsync(player, () =>
                    {
                        model.OnSave()();
                        cachedAfterSave = DB.Get<Player>(fixture.Id);
                        AssertPreferences(ctx, cachedAfterSave.Settings, desired.Self, desired.Enemy, $"cycle {cycle}: saved cache");
                        AssertPreferences(ctx, ReadPersisted(fixture.Id).Settings, desired.Self, desired.Enemy, $"cycle {cycle}: Redis");
                    });
                    await ctx.WaitFrameAsync();

                    var reloaded = DB.Get<Player>(fixture.Id);
                    ctx.Assert(!ReferenceEquals(cachedAfterSave, reloaded),
                        $"cycle {cycle}: reload crosses the production per-script cache boundary");
                    ctx.AssertEqual(fixture.Id, reloaded.Id, $"cycle {cycle}: settings remain owned by the same player");
                    AssertPreferences(ctx, reloaded.Settings, desired.Self, desired.Enemy, $"cycle {cycle}: uncached reload");
                    QuickCast.ApplyPreferences(player);
                    ctx.AssertEqual(desired.Self, QuickCast.IsEnabled(player, QuickCastTargetType.Self),
                        $"cycle {cycle}: reapplication restores self targeting");
                    ctx.AssertEqual(desired.Enemy, QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy),
                        $"cycle {cycle}: reapplication restores enemy targeting");
                    var reopened = LoadSettings(player);
                    ctx.AssertEqual(desired.Self, reopened.SelfQuickCastEnabled, $"cycle {cycle}: reopened self checkbox");
                    ctx.AssertEqual(desired.Enemy, reopened.EnemyQuickCastEnabled, $"cycle {cycle}: reopened enemy checkbox");
                }
            }
            ctx.SetResultDetail("Legacy Redis JSON without either preference; unsaved drafts remain unapplied; production Settings Save, raw Redis reads, real cache boundaries, fresh view models, ownership, and all four independent combinations verified through two cycles. Connected client clicks/rendering and process restart excluded.");
        }
        finally
        {
            foreach (var type in playerWindows.Keys)
                Gui.ClosePlayerWindow(player, type);
            windows.Remove(fixture.Id);
        }
    }

    private static SettingsViewModel LoadSettings(uint player)
    {
        var model = new SettingsViewModel();
        typeof(GuiViewModelBase<SettingsViewModel, GuiPayloadBase>).GetProperty("Player", InstancePrivate).SetValue(model, player);
        foreach (var method in new[] { "LoadGeneralView", "LoadIdentityView", "LoadChatView", "LoadTargetingView" })
            typeof(SettingsViewModel).GetMethod(method, InstancePrivate).Invoke(model, null);
        return model;
    }

    private static Player ReadPersisted(string id) => JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(id));

    private static void AssertPreferences(EngineTestContext ctx, PlayerSettings settings, bool self, bool enemy, string description)
    {
        ctx.AssertEqual(self, settings.SelfQuickCastEnabled, description + ": self preference");
        ctx.AssertEqual(enemy, settings.EnemyQuickCastEnabled, description + ": enemy preference");
    }
}
