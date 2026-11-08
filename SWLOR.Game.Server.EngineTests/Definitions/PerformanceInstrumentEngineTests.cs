using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PerformanceInstrumentEngineTests
{
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
                    ctx.Assert(!Performance.HasInstrumentEquipped(player, out _), "a violin cannot perform without its bow");
                    bow = await ctx.EquipItemAsync(player, "violin_bow", InventorySlot.RightHand);
                }

                ctx.Assert(Performance.HasInstrumentEquipped(player, out var recognized), $"{resref} is recognized without item locals");
                ctx.AssertEqual(family, recognized, $"{resref} selects the correct family");
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    ctx.Assert(UsePerkFeat.TryUseAbility(player, player, FeatType.PlayInstrument1, GetLocation(player)), "the music feat activates"));
                await ctx.WaitUntilAsync(() => Performance.TryGetSession(player, out _), 8f, $"{resref} to start performing");
                Performance.TryGetSession(player, out var session);
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
                await ctx.WaitUntilAsync(() => !Performance.TryGetSession(player, out _), 8f, "removing a required item to end the performance");
                ctx.Assert(!GetHasFeat(FeatType.PerformanceNoteLow, player), "ending the performance removes its note feats");
            }
            finally
            {
                if (Performance.TryGetSession(player, out _))
                    Performance.EndPerformance(player, PerformanceEndReason.Invalid);
                AssignCommand(player, () => DestroyObject(player));
            }
        }
    }
}
