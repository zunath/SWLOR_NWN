using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The engine side of the four Performance windows: the logic that used to be static in
    /// <c>PerformanceWindows</c>, moved here unchanged. The windows only show state; input stays
    /// on the hot bar feats. The id reservation of the window-less cue strip is this instance's own state. The module-load
    /// handler runs on the container's instance; static callers reach it through <c>GameServices.PerformanceWindows</c>.
    /// </summary>
    public class PerformanceWindowsService : IPerformanceWindowsService
    {
        // The window-less cue strip sits above the performer's head, centered on the screen.
        private const int FallbackStripRow = -8;
        private const float FallbackStripOverlapSeconds = 0.5f;
        private const string GuiSystemName = "PerformanceWindows";

        private readonly IPerformanceVenueService _venue;
        private readonly IMusicService _music;
        private readonly IPerformanceSongService _songs;
        private readonly IPerformanceSnapshotService _snapshots;
        private readonly IGuiService _gui;
        private readonly Lazy<IPerformanceService> _performance;
        private readonly IPerkService _perk;
        private readonly IPlayerNameService _playerName;
        private readonly IStatService _stat;
        private IdReservation _fallbackStrip;

        public PerformanceWindowsService(
            IPerformanceVenueService venue,
            IMusicService music,
            IPerformanceSongService songs,
            IPerformanceSnapshotService snapshots,
            IGuiService gui,
            Lazy<IPerformanceService> performance,
            IPerkService perk,
            IPlayerNameService playerName,
            IStatService stat)
        {
            _venue = venue;
            _music = music;
            _songs = songs;
            _snapshots = snapshots;
            _gui = gui;
            _performance = performance;
            _perk = perk;
            _playerName = playerName;
            _stat = stat;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void ReserveGuiIds()
        {
            _fallbackStrip = ReserveIds(GuiSystemName, 1);
        }

        public void ShowFallbackStrip(uint performer, string text, float beatSeconds)
        {
            PostString(performer, text, -text.Length / 2, FallbackStripRow, ScreenAnchor.Center,
                beatSeconds + FallbackStripOverlapSeconds, GuiService.ColorWhite, GuiService.ColorWhite, _fallbackStrip.StartId, GuiService.TextName);
        }

        public void ClearFallbackStrip(uint performer)
        {
            PostString(performer, string.Empty, 0, FallbackStripRow, ScreenAnchor.Center, 0.1f,
                GuiService.ColorWhite, GuiService.ColorWhite, _fallbackStrip.StartId, GuiService.TextName);
        }

        public GuiWindowType GetCueWindowType(PerformanceKind kind)
        {
            return kind == PerformanceKind.Dance ? GuiWindowType.PerformanceFootwork : GuiWindowType.PerformanceSession;
        }

        public bool OpenCueWindow(PerformanceSession session)
        {
            try
            {
                var player = session.Performer;
                var type = GetCueWindowType(session.Kind);
                if (!IsWindowOpen(player, type))
                    TogglePlayerWindow(player, type);

                if (GetWindowViewModel(player, type) is not IPerformanceView view || !view.IsOpen)
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

        public void CloseCueWindow(PerformanceSession session, PerformanceEndReason reason)
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
                if (GetIsObjectValid(player) && IsWindowOpen(player, type))
                    ClosePlayerWindow(player, type);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance cue window failed to close: PlayerId={PlayerId}", session.PlayerId);
            }
        }

        public void ToggleOptions(PerformanceSession session)
        {
            if (session.View is not { IsOpen: true })
                OpenCueWindow(session);

            TogglePlayerWindow(session.Performer, GuiWindowType.PerformanceOptions);
        }

        public void RefreshSetup(PerformanceSession session)
        {
            if (session.View is not { IsOpen: true } view)
                return;

            view.ShowSetup(BuildSetup(session));
            view.ShowAudienceGets(BuildAudienceGets(session));
        }

        public string BuildAudienceGets(PerformanceSession session)
        {
            var snapshot = BuildSnapshot(
                session, _venue.GetRating(session.Performer), PerformanceSongs.AutomaticSong);
            return PerformanceAudienceText.AudienceGets(snapshot);
        }

        public PerformerSetup BuildSetup(PerformanceSession session)
        {
            var performer = session.Performer;
            var preference = GetPreference(performer);
            var setup = new PerformerSetup { TempoText = session.Tempo.GetLabel() };

            if (session.Kind == PerformanceKind.Dance)
            {
                var routineLevel = GetPerkLevel(performer, PerformanceRiderCatalog.GetUnlockPerk(session.Routine));
                var routine = PerformanceRiderText.GetName(session.Routine, routineLevel);
                var hasSecond = preference.SecondRoutine != PerformanceRoutineType.CantinaShuffle &&
                                GetPerkLevel(performer, PerkType.Showstopper) > 0 &&
                                IsRoutineUnlocked(performer, preference.SecondRoutine);
                return setup with
                {
                    RoutineText = hasSecond
                        ? $"{routine} + {PerformanceRiderText.GetName(preference.SecondRoutine)}"
                        : routine
                };
            }

            var setLevel = GetPerkLevel(performer, PerformanceRiderCatalog.GetUnlockPerk(session.Set));
            var set = PerformanceRiderText.GetName(session.Set, setLevel);
            var hasSecondSet = preference.SecondSet != PerformanceSetType.CantinaStandards &&
                               GetPerkLevel(performer, PerkType.Virtuoso) > 0 &&
                               IsSetUnlocked(performer, preference.SecondSet);
            var track = ChooseSong(session.PlayerId, preference.SongTrackId);
            return setup with
            {
                InstrumentFamily = session.InstrumentFamily,
                SetText = hasSecondSet ? $"{set} + {PerformanceRiderText.GetName(preference.SecondSet)}" : set,
                SongText = track >= 0 ? _music.GetTrackName(track) : "No song",
                InstrumentText = session.InstrumentFamily switch
                {
                    PerformanceInstrumentFamily.Vocals => "Microphone",
                    PerformanceInstrumentFamily.Pipes => "Panpipes",
                    _ => GetIsObjectValid(session.Instrument)
                        ? GetName(session.Instrument)
                        : DescribeFamily(session.InstrumentFamily)
                }
            };
        }

        public PerformanceAudienceBonus GetAudienceBonus(uint performer, PerformanceKind kind)
        {
            var isDancer = kind == PerformanceKind.Dance;
            var xpBase = Math.Max(0, GetStatAdjustment(
                performer, isDancer ? StatType.PerformanceDanceXPBonus : StatType.PerformanceMusicXPBonus));
            var xpScaled = ScaleBySocial(
                performer, xpBase, PerformanceAudienceRules.XpBonusMaximum(xpBase));
            var debt = Math.Max(0, GetStatAdjustment(
                performer, isDancer ? StatType.PerformanceDanceDebtClearPercent : StatType.PerformanceMusicDebtClearPercent));
            return new PerformanceAudienceBonus(xpScaled, debt);
        }

        public PerformerAudienceInfo BuildAudienceInfo(PerformanceAreaShow show, PerformerSnapshot performer)
        {
            var info = new PerformerAudienceInfo
            {
                VenueText = PerformanceAudienceText.Venue(performer.Venue),
                FollowingText = PerformanceAudienceText.Following(performer.Crowd, performer.WatcherCount),
                InspiringText = PerformanceAudienceText.Inspiring(performer.Crowd, performer.WatcherCount),
                AudienceGetsText = PerformanceAudienceText.AudienceGets(performer)
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
                    dancers.Add(GetDisplayName(performer.Performer, other.Performer));
                }
            }

            var leader = performer.BandLeader ?? performer;
            var isLeader = leader == performer;
            var harmony = performer.HarmonyActive ? performer.BankingBonusPercent : 0;
            var band = PerformanceAudienceText.Band(
                isLeader,
                performer.Partner != null,
                isLeader ? string.Empty : GetDisplayName(performer.Performer, leader.Performer),
                dancers,
                harmony,
                troupe);

            return info with
            {
                BandText = band,
                AudienceHearsText = PerformanceAudienceText.Hears(
                    performer.BandTrackId is >= 0 ? _music.GetTrackName(performer.BandTrackId.Value) : string.Empty)
            };
        }

        public bool IsWatchingOpen(uint watcher)
        {
            return IsWindowOpen(watcher, GuiWindowType.PerformanceWatching);
        }

        public bool OpenWatching(uint watcher)
        {
            try
            {
                TogglePlayerWindow(watcher, GuiWindowType.PerformanceWatching);
                return IsWatchingOpen(watcher);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance watching window failed to open: PlayerId={PlayerId}", GetObjectUUID(watcher));
                return false;
            }
        }

        public void ShowWatching(uint watcher, PerformanceWatchView view)
        {
            if (GetWindowViewModel(watcher, GuiWindowType.PerformanceWatching) is PerformanceWatchingViewModel model)
                model.ShowView(view);
        }

        public void CloseWatching(uint watcher)
        {
            if (IsWatchingOpen(watcher))
                ClosePlayerWindow(watcher, GuiWindowType.PerformanceWatching);
        }

        private static string DescribeFamily(PerformanceInstrumentFamily family)
        {
            return family switch
            {
                PerformanceInstrumentFamily.Horns => "Horn",
                PerformanceInstrumentFamily.Vocals => "Vocals",
                PerformanceInstrumentFamily.Pipes => "Pipes",
                PerformanceInstrumentFamily.BowedStrings => "Violin",
                PerformanceInstrumentFamily.Percussion => "Percussion",
                _ => "Strings"
            };
        }

        // The calls into the unconverted Gui, Perk, Stat, PlayerName and Performance statics, one line each so a test can answer them.

        /// <summary>Reserves GUI ids for a system through the unconverted <c>Gui</c> service.</summary>
        protected virtual IdReservation ReserveIds(string systemName, int amount) => _gui.ReserveIds(systemName, amount);

        protected virtual bool IsWindowOpen(uint player, GuiWindowType type) => _gui.IsWindowOpen(player, type);

        protected virtual void TogglePlayerWindow(uint player, GuiWindowType type) => _gui.TogglePlayerWindow(player, type);

        protected virtual void ClosePlayerWindow(uint player, GuiWindowType type) => _gui.ClosePlayerWindow(player, type);

        /// <summary>The view model of the player's window of that type.</summary>
        protected virtual object GetWindowViewModel(uint player, GuiWindowType type) => _gui.GetPlayerWindow(player, type).ViewModel;

        protected virtual int GetPerkLevel(uint creature, PerkType perk) => _perk.GetPerkLevel(creature, perk);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

        protected virtual string GetDisplayName(uint observer, uint target) => _playerName.GetDisplayName(observer, target);

        // The main service of the Performance service, reached lazily because it takes this one by constructor.
        protected virtual PerformancePreference GetPreference(uint player) => _performance.Value.GetPreference(player);

        protected virtual bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine) => _performance.Value.IsRoutineUnlocked(player, routine);

        protected virtual bool IsSetUnlocked(uint player, PerformanceSetType set) => _performance.Value.IsSetUnlocked(player, set);

        protected virtual PerformerSnapshot BuildSnapshot(PerformanceSession session, int venue, int songTrackId) =>
            _snapshots.Build(session, venue, songTrackId);

        protected virtual int ChooseSong(string playerId, int preferredTrackId) =>
            _songs.ChooseSong(playerId, preferredTrackId);

        protected virtual int ScaleBySocial(uint performer, int baseValue, int maximum) =>
            AbilityEffectScaling.ScaleValueBySourceSocial(performer, baseValue, maximum);
    }
}
