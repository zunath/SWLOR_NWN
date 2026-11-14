using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PerformanceInstrumentEngineTests
{
    [EngineTest("Panpipe playing fits preserve cosmetics on stop and unequip", Category = "PerformanceInstruments", TimeoutSeconds = 90f)]
    public static Task PanpipeFitRestoresCosmetics(EngineTestContext ctx) => PlayingFitRestoresCosmetics(ctx, "panpipes");

    [EngineTest("Tambourine animation preserves cosmetics during play stop and unequip", Category = "PerformanceInstruments", TimeoutSeconds = 90f)]
    public static Task TambourineFitRestoresCosmetics(EngineTestContext ctx) => PlayingFitRestoresCosmetics(ctx, "tambourine");

    private static async Task PlayingFitRestoresCosmetics(EngineTestContext ctx, string resref)
    {
        const ObjectVisualTransformDataScopeType scope =
            (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2;
        var channels = new[] { ObjectVisualTransform.TranslateX, ObjectVisualTransform.TranslateY, ObjectVisualTransform.TranslateZ,
            ObjectVisualTransform.RotateY, ObjectVisualTransform.RotateZ, ObjectVisualTransform.RotateX };
        var authored = new[] { .011f, -.012f, .013f, 4f, 7f, 3f };
        foreach (var gender in new[] { Gender.Male, Gender.Female })
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            try
            {
                SetCreatureAppearanceType(player, AppearanceType.Human);
                SetGender(player, gender);
                SetPhenoType(PhenoType.Normal, player);
                fixture.Update(record => record.Perks[PerkType.PlayInstrument] = 1);
                var item = await ctx.EquipItemAsync(player, resref, InventorySlot.RightHand);
                for (var i = 0; i < channels.Length; i++)
                    SetObjectVisualTransform(item, channels[i], authored[i], nScope: scope);
                GameServices.Performance.SetRoleplay(player, true);
                await ctx.ExecuteInCreatureContextAsync(player, () => GameServices.Performance.TogglePlayInstrument(player, 1));
                await ctx.WaitUntilAsync(() => GameServices.Performance.TryGetSession(player, out _), 8f, "instrument to start performing");
                var fitted = channels.Select(channel => GetObjectVisualTransform(item, channel, nScope: scope)).ToArray();
                if (resref == "panpipes")
                    ctx.Assert(fitted.Where((value, i) => Math.Abs(value - authored[i]) > .0001f).Any(), "the playing fit is applied");
                else
                    for (var i = 0; i < channels.Length; i++)
                        ctx.Assert(Math.Abs(fitted[i] - authored[i]) < .0001f, "the authored drum grip does not change item cosmetics");
                for (var refresh = 0; refresh < 5; refresh++) HornFit.Apply(player);
                for (var i = 0; i < channels.Length; i++)
                    ctx.Assert(Math.Abs(GetObjectVisualTransform(item, channels[i], nScope: scope) - fitted[i]) < .0001f,
                        "repeated fit refresh does not compound its offset");
                if (gender == Gender.Male)
                    GameServices.Performance.EndPerformance(player, PerformanceEndReason.Stopped);
                else
                {
                    AssignCommand(player, () => { ClearAllActions(); ActionUnequipItem(item); });
                    await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, player) != item, 8f, "instrument to be unequipped");
                }
                await ctx.WaitUntilAsync(() => !GameServices.Performance.TryGetSession(player, out _), 8f, "the performance to stop");
                for (var i = 0; i < channels.Length; i++)
                    ctx.Assert(Math.Abs(GetObjectVisualTransform(item, channels[i], nScope: scope) - authored[i]) < .0001f,
                        "stopping or unequipping removes only the playing fit");
            }
            finally
            {
                if (GameServices.Performance.TryGetSession(player, out _)) GameServices.Performance.EndPerformance(player, PerformanceEndReason.Invalid);
                AssignCommand(player, () => DestroyObject(player));
            }
        }
    }

    [EngineTest("Performance recognizes existing microphones pipes tambourine and paired violins", Category = "PerformanceInstruments", TimeoutSeconds = 180f)]
    public static async Task ExistingPropsPerformAndStopWhenRemoved(EngineTestContext ctx)
    {
        foreach (var (resref, slot, family) in new[]
                 {
                     ("night_microplain", InventorySlot.LeftHand, PerformanceInstrumentFamily.Vocals),
                     ("night_microbrand", InventorySlot.LeftHand, PerformanceInstrumentFamily.Vocals),
                     ("panpipes", InventorySlot.RightHand, PerformanceInstrumentFamily.Pipes),
                     ("tambourine", InventorySlot.RightHand, PerformanceInstrumentFamily.Percussion),
                     ("violin_m", InventorySlot.LeftHand, PerformanceInstrumentFamily.BowedStrings),
                     ("violin_f", InventorySlot.LeftHand, PerformanceInstrumentFamily.BowedStrings)
                 })
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            try
            {
                fixture.Update(record => record.Perks[PerkType.PlayInstrument] = 1);
                Perk.SyncGrantedFeats(player, PerkType.PlayInstrument, 1, true);
                var item = await ctx.EquipItemAsync(player, resref, slot);
                var bow = OBJECT_INVALID;
                if (family == PerformanceInstrumentFamily.BowedStrings)
                {
                    ctx.Assert(!GameServices.Performance.HasInstrumentEquipped(player, out _), "a violin cannot perform without its bow");
                    bow = await ctx.EquipItemAsync(player, "violin_bow", InventorySlot.RightHand);
                }

                ctx.Assert(GameServices.Performance.HasInstrumentEquipped(player, out var recognized), $"{resref} is recognized without item locals");
                ctx.AssertEqual(family, recognized, $"{resref} selects the correct family");
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    ctx.Assert(UsePerkFeat.TryUseAbility(player, player, FeatType.PlayInstrument1, GetLocation(player)), "the music feat activates"));
                await ctx.WaitUntilAsync(() => GameServices.Performance.TryGetSession(player, out _), 8f, $"{resref} to start performing");
                GameServices.Performance.TryGetSession(player, out var session);
                ctx.AssertEqual(PerformanceKind.Music, session.Kind, "the prop uses the existing music audience channel");
                ctx.AssertEqual(family, session.InstrumentFamily, "the session retains the recognized family");
                ctx.AssertEqual(item, session.Instrument, "the session tracks the exact instrument");
                ctx.AssertEqual(bow, session.InstrumentBow, "the session tracks the exact required bow");
                var removed = bow != OBJECT_INVALID ? bow : item;
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionUnequipItem(removed);
                });
                await ctx.WaitUntilAsync(() => GetItemInSlot(bow != OBJECT_INVALID ? InventorySlot.RightHand : slot, player) != removed,
                    8f, "the required item to leave its equipment slot");
                await ctx.WaitUntilAsync(() => !GameServices.Performance.TryGetSession(player, out _), 8f, "removing a required item to end the performance");
                ctx.Assert(!GetHasFeat(FeatType.PerformanceNoteLow, player), "ending the performance removes its note feats");
            }
            finally
            {
                if (GameServices.Performance.TryGetSession(player, out _))
                    GameServices.Performance.EndPerformance(player, PerformanceEndReason.Invalid);
                AssignCommand(player, () => DestroyObject(player));
            }
        }
    }
}
