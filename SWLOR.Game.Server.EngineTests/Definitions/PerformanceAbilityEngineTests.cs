using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Player fixture cases for Dance I-IV and Play Instrument I-IV (see PerformanceAbilityBehaviors).
    /// Runs on a headless native player: it exercises the real feat pipeline, hot bar, feats and busy state,
    /// but not a connected client, so the cue window can fall back to floating text and no key presses are sent.
    /// </summary>
    public static class PerformanceAbilityEngineTests
    {
        private const string AllowNoInstrumentVariable = "PERFORMANCE_ALLOW_NO_INSTRUMENT";
        private const string InstrumentResref = "guitar";
        private const int QuickbarSlotCount = 36;

        // The mode hot bar layout: cue buttons from slot 0, then Flourish, Options and the performer's own toggle feat (Stop).
        private const int FlourishSlot = 4;
        private const int OptionsSlot = 5;
        private const int StopSlot = 6;
        private const int FirstBarSlotCount = 12;

        private static readonly FeatType[] DanceCueFeats =
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp,
            FeatType.PerformanceStepDown, FeatType.PerformanceStepRight
        };

        private static readonly FeatType[] MusicCueFeats =
        {
            FeatType.PerformanceNoteLow, FeatType.PerformanceNoteMid, FeatType.PerformanceNoteHigh
        };

        public static bool Supports(FeatType feat) => GetKind(feat).HasValue;

        public static async Task RunCaseAsync(EngineTestContext ctx, FeatType feat)
        {
            ctx.Assert(Supports(feat), "performance fixture must have a specific observable outcome for this feat");
            var kind = GetKind(feat).Value;
            var isDance = kind == PerformanceKind.Dance;
            var perk = isDance ? PerkType.Dance : PerkType.PlayInstrument;
            var cueFeats = isDance ? DanceCueFeats : MusicCueFeats;
            var modeFeats = cueFeats.Concat(new[] { FeatType.PerformanceFlourish, FeatType.PerformanceOptions }).ToArray();
            var otherCueFeats = isDance ? MusicCueFeats : DanceCueFeats;
            var ability = Ability.GetAbilityDetail(feat);
            var level = ability.AbilityLevel;
            ctx.AssertEqual(perk, ability.EffectiveLevelPerkType, $"{feat} belongs to {perk}");

            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var instrument = OBJECT_INVALID;
            try
            {
                // Buy exactly this rank: a higher rank replaces the lower rank's feat, so the case owns one rank.
                fixture.Update(record => record.Perks[perk] = level);
                Perk.SyncGrantedFeats(player, perk, level, true);
                ctx.Assert(GetHasFeat(feat, player), $"buying {perk} rank {level} grants {feat}");
                ctx.Assert(!Performance.TryGetSession(player, out _), "the player starts with no performance");

                if (!isDance)
                {
                    ctx.AssertEqual(0, GetLocalInt(GetModule(), AllowNoInstrumentVariable), "the module does not waive the instrument");
                    ctx.Assert(!Performance.HasInstrumentEquipped(player, out _), "the fixture starts with no instrument");

                    var instrumentless = await UseAsync(ctx, player, feat);
                    ctx.Assert(instrumentless, $"{feat} activates through the player feat pipeline: {Ability.GetLastActivationDenialReason()}");
                    await ctx.WaitUntilAsync(() => !Activity.IsBusy(player), 5f, "the instrumentless activation to finish");
                    await ctx.WaitFrameAsync();
                    ctx.Assert(!Performance.TryGetSession(player, out _), "playing music with no instrument starts no session");
                    ctx.Assert(Activity.GetBusyType(player) != ActivityStatusType.Performing, "a refused performance leaves the player free");
                    ctx.Assert(!GetHasFeat(FeatType.PerformanceFlourish, player), "a refused performance grants no mode feats");

                    instrument = await ctx.EquipItemAsync(player, InstrumentResref, InventorySlot.RightHand);
                    ctx.Assert(Performance.HasInstrumentEquipped(player, out var family) && family == PerformanceInstrumentFamily.Strings,
                        "the equipped guitar is recognised as a strings instrument");
                }

                // Shortcuts inside the first bar must be replaced by the mode bar and come back afterwards.
                PlayerPlugin.SetQuickBarSlot(player, 3, PlayerQuickBarSlot.UseFeat(feat));
                PlayerPlugin.SetQuickBarSlot(player, 8, PlayerQuickBarSlot.UseSkill(0));
                PlayerPlugin.SetQuickBarSlot(player, 11, PlayerQuickBarSlot.UseFeat(feat));
                ctx.Assert(!string.IsNullOrEmpty(CreaturePlugin.SerializeQuickbar(player)), "the fixture quickbar serializes");
                var originalBar = SnapshotQuickbar(player);

                var activated = await UseAsync(ctx, player, feat);
                ctx.Assert(activated, $"{feat} activates through the player feat pipeline: {Ability.GetLastActivationDenialReason()}");
                await ctx.WaitUntilAsync(() => Performance.TryGetSession(player, out _), 8f, $"{feat} to start a performance session");
                Performance.TryGetSession(player, out var session);

                ctx.AssertEqual(player, session.Performer, "the session belongs to the activator");
                ctx.AssertEqual(kind, session.Kind, "the session kind follows the ability");
                ctx.AssertEqual(level, session.PerkLevel, "the session records the ability rank");
                ctx.AssertEqual(feat, session.ToggleFeat, "the session records the toggle feat that stops it");
                if (!isDance)
                {
                    ctx.AssertEqual(PerformanceInstrumentFamily.Strings, session.InstrumentFamily, "the session plays the guitar's family");
                    ctx.AssertEqual(instrument, session.Instrument, "the session tracks the equipped instrument");
                    ctx.AssertEqual(InventorySlot.RightHand, session.InstrumentSlot, "the session tracks the instrument's slot");
                }

                ctx.AssertEqual(ActivityStatusType.Performing, Activity.GetBusyType(player), "the performer is busy performing");
                foreach (var modeFeat in modeFeats)
                    ctx.Assert(GetHasFeat(modeFeat, player), $"performance mode grants {modeFeat}");
                foreach (var otherFeat in otherCueFeats)
                    ctx.Assert(!GetHasFeat(otherFeat, player), $"performance mode does not grant the other kind's {otherFeat}");

                for (var slot = 0; slot < cueFeats.Length; slot++)
                    ctx.Assert(IsFeat(player, slot, cueFeats[slot]), $"mode bar slot {slot} holds {cueFeats[slot]}");
                ctx.Assert(IsFeat(player, FlourishSlot, FeatType.PerformanceFlourish), "the mode bar holds Flourish");
                ctx.Assert(IsFeat(player, OptionsSlot, FeatType.PerformanceOptions), "the mode bar holds Options");
                ctx.Assert(IsFeat(player, StopSlot, feat), $"the mode bar holds {feat} as the Stop button");
                for (var slot = StopSlot + 1; slot < FirstBarSlotCount; slot++)
                    ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, slot).ObjectType, $"the first bar's slot {slot} is cleared");
                if (!isDance)
                    ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, cueFeats.Length).ObjectType, "the unused fourth cue slot is cleared");
                ctx.Assert(!string.IsNullOrEmpty(DB.Get<Player>(fixture.Id).PerformanceSavedHotBar), "the original hot bar is saved for recovery");

                // The busy state refuses the toggle feat as an ordinary activation; the feat router sends it to Stop instead.
                var again = await UseAsync(ctx, player, feat);
                ctx.Assert(!again, "the busy performer cannot start a second activation");
                ctx.Assert(Performance.TryGetSession(player, out _), "a refused second activation leaves the performance running");
                ctx.Assert(Performance.IsModeFeat(player, feat), "the performer's own toggle feat is routed to the performance handler");

                await ctx.ExecuteInCreatureContextAsync(player, () => Performance.HandleInput(player, feat));

                ctx.Assert(!Performance.TryGetSession(player, out _), "using the toggle feat again ends the performance");
                ctx.Assert(!Activity.IsBusy(player), "ending the performance frees the player");
                foreach (var modeFeat in modeFeats)
                    ctx.Assert(!GetHasFeat(modeFeat, player), $"ending the performance removes {modeFeat}");
                ctx.Assert(GetHasFeat(feat, player), $"{feat} is still known after the performance");
                ctx.Assert(!Performance.IsModeFeat(player, feat), "outside a performance the toggle feat is an ordinary activation again");

                var restoredBar = SnapshotQuickbar(player);
                for (var slot = 0; slot < QuickbarSlotCount; slot++)
                    ctx.AssertEqual(originalBar[slot], restoredBar[slot], $"ending the performance restores quickbar slot {slot}");
                ctx.Assert(string.IsNullOrEmpty(DB.Get<Player>(fixture.Id).PerformanceSavedHotBar), "the saved hot bar is cleared once restored");
            }
            finally
            {
                if (Performance.TryGetSession(player, out _))
                    Performance.EndPerformance(player, PerformanceEndReason.Invalid);
                await ctx.WaitFrameAsync();
                // Queued last so it runs after Dispose clears the player flag; otherwise the
                // fixture stays at the spawn point through the rest of the sweep.
                AssignCommand(player, () => DestroyObject(player));
            }
        }

        private static PerformanceKind? GetKind(FeatType feat)
        {
            if (feat is FeatType.Dance1 or FeatType.Dance2 or FeatType.Dance3 or FeatType.Dance4)
                return PerformanceKind.Dance;
            if (feat is FeatType.PlayInstrument1 or FeatType.PlayInstrument2 or FeatType.PlayInstrument3 or FeatType.PlayInstrument4)
                return PerformanceKind.Music;
            return null;
        }

        private static async Task<bool> UseAsync(EngineTestContext ctx, uint player, FeatType feat)
        {
            var activated = false;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                activated = UsePerkFeat.TryUseAbility(player, player, feat, GetLocation(player)));
            return activated;
        }

        private static string[] SnapshotQuickbar(uint player)
        {
            return Enumerable.Range(0, QuickbarSlotCount)
                .Select(slot =>
                {
                    var entry = PlayerPlugin.GetQuickBarSlot(player, slot);
                    return $"{entry.ObjectType}:{entry.INTParam1}:{entry.Resref}";
                })
                .ToArray();
        }

        private static bool IsFeat(uint player, int slot, FeatType feat)
        {
            var entry = PlayerPlugin.GetQuickBarSlot(player, slot);
            return entry.ObjectType == QuickBarSlotType.Feat && entry.INTParam1 == (int)feat;
        }
    }
}
