using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The engine side of the four Performance windows: opening and closing them, finding their view models, and reading
    /// the names and numbers they show. The windows only show state; input stays on the hot bar feats.
    /// </summary>
    public static class PerformanceWindows
    {
        public static GuiWindowType GetCueWindowType(PerformanceKind kind)
        {
            return kind == PerformanceKind.Dance ? GuiWindowType.PerformanceFootwork : GuiWindowType.PerformanceSession;
        }

        /// <summary>Opens the performer's cue window and attaches it to the session. Returns false when it could not be opened.</summary>
        public static bool OpenCueWindow(PerformanceSession session)
        {
            try
            {
                var player = session.Performer;
                var type = GetCueWindowType(session.Kind);
                if (!Gui.IsWindowOpen(player, type))
                    Gui.TogglePlayerWindow(player, type);

                if (Gui.GetPlayerWindow(player, type).ViewModel is not IPerformanceView view || !view.IsOpen)
                    return false;

                session.View = view;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance cue window failed to open: PlayerId={PlayerId}", session.PlayerId);
                return false;
            }
        }

        /// <summary>Detaches the cue window and, unless the player logged out, closes it.</summary>
        public static void CloseCueWindow(PerformanceSession session, PerformanceEndReason reason)
        {
            var view = session.View;
            session.View = null;
            view?.MarkEnded();

            if (view == null || reason == PerformanceEndReason.LoggedOut)
                return;

            try
            {
                var player = session.Performer;
                var type = GetCueWindowType(session.Kind);
                if (GetIsObjectValid(player) && Gui.IsWindowOpen(player, type))
                    Gui.ClosePlayerWindow(player, type);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance cue window failed to close: PlayerId={PlayerId}", session.PlayerId);
            }
        }

        /// <summary>
        /// The Options button: shows or hides the Perform window. A performer who closed their cue window gets it back too,
        /// so they are not left with floating text and no way to reopen it.
        /// </summary>
        public static void ToggleOptions(PerformanceSession session)
        {
            if (session.View is not { IsOpen: true })
                OpenCueWindow(session);

            Gui.TogglePlayerWindow(session.Performer, GuiWindowType.PerformanceOptions);
        }

        /// <summary>Tells an open cue window that the performer changed routine, set, song or tempo.</summary>
        public static void RefreshSetup(PerformanceSession session)
        {
            if (session.View is { IsOpen: true } view)
                view.ShowSetup(BuildSetup(session));
        }

        /// <summary>What a performer chose for the show, as the cue windows word it.</summary>
        public static PerformerSetup BuildSetup(PerformanceSession session)
        {
            var performer = session.Performer;
            var preference = Performance.GetPreference(performer);
            var setup = new PerformerSetup { TempoText = session.Tempo.GetLabel() };

            if (session.Kind == PerformanceKind.Dance)
            {
                var routineLevel = Perk.GetPerkLevel(performer, PerformanceRiderCatalog.GetUnlockPerk(session.Routine));
                var routine = PerformanceRiderText.GetName(session.Routine, routineLevel);
                var hasSecond = preference.SecondRoutine != PerformanceRoutineType.CantinaShuffle &&
                                Perk.GetPerkLevel(performer, PerkType.Showstopper) > 0 &&
                                Performance.IsRoutineUnlocked(performer, preference.SecondRoutine);
                return setup with
                {
                    RoutineText = hasSecond
                        ? $"{routine} + {PerformanceRiderText.GetName(preference.SecondRoutine)}"
                        : routine
                };
            }

            var setLevel = Perk.GetPerkLevel(performer, PerformanceRiderCatalog.GetUnlockPerk(session.Set));
            var set = PerformanceRiderText.GetName(session.Set, setLevel);
            var hasSecondSet = preference.SecondSet != PerformanceSetType.CantinaStandards &&
                               Perk.GetPerkLevel(performer, PerkType.Virtuoso) > 0 &&
                               Performance.IsSetUnlocked(performer, preference.SecondSet);
            var track = PerformanceSongs.ChooseSong(session.PlayerId, preference.SongTrackId);
            return setup with
            {
                SetText = hasSecondSet ? $"{set} + {PerformanceRiderText.GetName(preference.SecondSet)}" : set,
                SongText = track >= 0 ? Music.GetTrackName(track) : "No song",
                InstrumentText = GetIsObjectValid(session.Instrument)
                    ? GetName(session.Instrument)
                    : DescribeFamily(session.InstrumentFamily)
            };
        }

        /// <summary>The performer's own XP bonus and debt clearing for their audience, as the Perform window quotes them.</summary>
        public static PerformanceAudienceBonus GetAudienceBonus(uint performer, PerformanceKind kind)
        {
            var isDancer = kind == PerformanceKind.Dance;
            var xpBase = Math.Max(0, Stat.GetStatAdjustment(
                performer, isDancer ? StatType.PerformanceDanceXPBonus : StatType.PerformanceMusicXPBonus));
            var xpScaled = AbilityEffectScaling.ScaleValueBySourceSocial(
                performer, xpBase, PerformanceAudienceRules.XpBonusMaximum(xpBase));
            var debt = Math.Max(0, Stat.GetStatAdjustment(
                performer, isDancer ? StatType.PerformanceDanceDebtClearPercent : StatType.PerformanceMusicDebtClearPercent));
            return new PerformanceAudienceBonus(xpScaled, debt);
        }

        /// <summary>
        /// What the audience tick tells a performer's cue window: who follows them, the venue, and for a musician the band.
        /// Built only for an open window, so a closed one costs nothing.
        /// </summary>
        public static PerformerAudienceInfo BuildAudienceInfo(PerformanceAreaShow show, PerformerSnapshot performer)
        {
            var info = new PerformerAudienceInfo
            {
                VenueText = PerformanceAudienceText.Venue(performer.Venue),
                FollowingText = PerformanceAudienceText.Following(performer.Crowd, performer.WatcherCount),
                InspiringText = PerformanceAudienceText.Inspiring(performer.Crowd, performer.WatcherCount)
            };

            if (performer.Kind != PerformanceKind.Music)
                return info;

            var dancers = new List<string>();
            var troupe = 1;
            foreach (var other in show.Performers)
            {
                if (other == performer)
                    continue;

                if (PerformanceAudienceRules.AreInEachOthersRange(performer, other))
                    troupe++;

                if (other.Kind == PerformanceKind.Dance &&
                    PerformanceAudienceRules.IsWithinRange(performer.Position, other.Position, performer.RangeMeters))
                {
                    dancers.Add(PlayerName.GetDisplayName(performer.Performer, other.Performer));
                }
            }

            var leader = performer.BandLeader ?? performer;
            var isLeader = leader == performer;
            var harmony = performer.HarmonyActive ? performer.BankingBonusPercent : 0;
            var band = PerformanceAudienceText.Band(
                isLeader,
                performer.Partner != null,
                isLeader ? string.Empty : PlayerName.GetDisplayName(performer.Performer, leader.Performer),
                dancers,
                harmony,
                troupe);

            return info with
            {
                BandText = band,
                AudienceHearsText = PerformanceAudienceText.Hears(
                    performer.BandTrackId is >= 0 ? Music.GetTrackName(performer.BandTrackId.Value) : string.Empty)
            };
        }

        public static bool IsWatchingOpen(uint watcher)
        {
            return Gui.IsWindowOpen(watcher, GuiWindowType.PerformanceWatching);
        }

        /// <summary>Opens the Watching window for a watcher. Returns false when it could not be opened.</summary>
        public static bool OpenWatching(uint watcher)
        {
            try
            {
                Gui.TogglePlayerWindow(watcher, GuiWindowType.PerformanceWatching);
                return IsWatchingOpen(watcher);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance watching window failed to open: PlayerId={PlayerId}", GetObjectUUID(watcher));
                return false;
            }
        }

        public static void ShowWatching(uint watcher, PerformanceWatchView view)
        {
            if (Gui.GetPlayerWindow(watcher, GuiWindowType.PerformanceWatching).ViewModel is PerformanceWatchingViewModel model)
                model.ShowView(view);
        }

        public static void CloseWatching(uint watcher)
        {
            if (IsWatchingOpen(watcher))
                Gui.ClosePlayerWindow(watcher, GuiWindowType.PerformanceWatching);
        }

        private static string DescribeFamily(PerformanceInstrumentFamily family)
        {
            return family == PerformanceInstrumentFamily.Horns ? "Horn" : "Strings";
        }
    }
}
