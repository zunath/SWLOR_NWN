using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// What the Footwork and Session windows share: the quality, streak and Flourish readouts and the audience lines.
    /// The beat loop calls <see cref="ShowBeat"/> and <see cref="ShowBeatProgress"/>; both compare against what is already
    /// on the client first, because every assignment to a bound property sends a bind. Nothing here rebuilds the window.
    /// </summary>
    public abstract class PerformanceCueViewModelBase<TDerived> : GuiViewModelBase<TDerived, GuiPayloadBase>, IPerformanceView
        where TDerived : PerformanceCueViewModelBase<TDerived>
    {
        private const int QualityCount = 4;
        private static readonly string[] QualityNames = { "Rough", "Steady", "Great", "Dazzling" };

        private bool _isOpen;
        // A fresh window has shown nothing yet, so the first beat pushes every value.
        private float _shownProgress = -1f;
        private int _shownTier = -1;
        private int _shownPercent = -2;
        private int _shownStreak = -1;
        private int _shownMeter = -1;
        private int _shownOutcome = -1;
        private string _shownTempo;
        private string _shownVenue;
        private string _shownFollowing;
        private string _shownInspiring;

        public string TempoText { get => Get<string>(); set => Set(value); }
        public string VenueText { get => Get<string>(); set => Set(value); }
        public string FollowingText { get => Get<string>(); set => Set(value); }
        public string InspiringText { get => Get<string>(); set => Set(value); }
        public string StreakText { get => Get<string>(); set => Set(value); }
        public string FlourishText { get => Get<string>(); set => Set(value); }
        public string LastResultText { get => Get<string>(); set => Set(value); }
        public float BeatProgress { get => Get<float>(); set => Set(value); }
        public float FlourishProgress { get => Get<float>(); set => Set(value); }

        public string QualityRoughText { get => Get<string>(); set => Set(value); }
        public string QualitySteadyText { get => Get<string>(); set => Set(value); }
        public string QualityGreatText { get => Get<string>(); set => Set(value); }
        public string QualityDazzlingText { get => Get<string>(); set => Set(value); }
        public GuiColor QualityRoughColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor QualitySteadyColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor QualityGreatColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor QualityDazzlingColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor LastResultColor { get => Get<GuiColor>(); set => Set(value); }

        /// <summary>True while the window is on screen. The beat loop skips a closed window.</summary>
        public bool IsOpen => _isOpen;

        /// <summary>True when the window has a beat bar; only then does the beat loop step inside each beat.</summary>
        public abstract bool WantsBeatProgress { get; }

        protected abstract PerformanceKind Kind { get; }
        protected abstract string ContentElementId { get; }
        protected abstract string MainPartialName { get; }

        /// <summary>What the strip is made of in the last-result line: "cue" for steps, "note" for notes.</summary>
        protected abstract string CueNoun { get; }

        protected abstract void ShowCues(PerformanceSession session);
        protected abstract void ResetCueCache();
        protected abstract void ShowSetupCore(PerformerSetup setup);
        protected abstract void ShowAudienceCore(PerformerAudienceInfo info);

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var isLive = Performance.TryGetSession(Player, out var live) && live.Kind == Kind;
            var session = isLive ? live : CreatePreviewSession();

            ResetShown();
            ResetCueCache();

            // Assigned in full so a reopened window never keeps the last performance's values.
            AssignShared();
            ShowSetup(isLive ? PerformanceWindows.BuildSetup(session) : BuildPreviewSetup());
            ShowAudience(new PerformerAudienceInfo
            {
                VenueText = PerformanceAudienceText.Venue(isLive ? PerformanceVenue.GetRating(Player) : 0),
                FollowingText = PerformanceAudienceText.Following(0, 0),
                InspiringText = PerformanceAudienceText.Inspiring(0, 0)
            });

            _isOpen = true;
            ShowFrame(session);
            LastResultText = $"Follow the {CueNoun}s";
            LastResultColor = PerformanceWindowColors.Dim;

            ChangePartialView(ContentElementId, MainPartialName);
        }

        public override Action OnWindowClosed() => () => _isOpen = false;

        public void MarkEnded()
        {
            _isOpen = false;
        }

        // The beat loop checks IsOpen before calling, so a closed window is never visited.
        public void ShowBeat(PerformanceSession session, PerformanceBeatOutcome lastOutcome)
        {
            ShowFrame(session);
            ShowLastResult(lastOutcome);
        }

        // The cue strip, the beat bar and the readouts: each value is pushed only when it differs from what is on screen.
        private void ShowFrame(PerformanceSession session)
        {
            ShowCues(session);
            if (WantsBeatProgress)
                ShowBeatProgress(1f);
            ShowQuality(session);

            if (PerformanceBindCache.Changed(ref _shownStreak, session.Streak))
                StreakText = session.Streak.ToString();

            var meter = session.FlourishMeter;
            if (PerformanceBindCache.Changed(ref _shownMeter, meter))
            {
                var ready = meter >= PerformanceMath.FlourishMeterMax;
                FlourishText = ready
                    ? $"Flourish {meter} / {PerformanceMath.FlourishMeterMax} - ready"
                    : $"Flourish {meter} / {PerformanceMath.FlourishMeterMax}";
                FlourishProgress = meter / (float)PerformanceMath.FlourishMeterMax;
            }
        }

        public void ShowBeatProgress(float timeLeft)
        {
            if (!WantsBeatProgress || !PerformanceBindCache.Changed(ref _shownProgress, timeLeft))
                return;

            BeatProgress = timeLeft;
        }

        public void ShowSetup(PerformerSetup setup)
        {
            if (PerformanceBindCache.Changed(ref _shownTempo, setup.TempoText))
                TempoText = setup.TempoText;

            ShowSetupCore(setup);
        }

        public void ShowAudience(PerformerAudienceInfo info)
        {
            if (PerformanceBindCache.Changed(ref _shownVenue, info.VenueText))
                VenueText = info.VenueText;
            if (PerformanceBindCache.Changed(ref _shownFollowing, info.FollowingText))
                FollowingText = info.FollowingText;
            if (PerformanceBindCache.Changed(ref _shownInspiring, info.InspiringText))
                InspiringText = info.InspiringText;

            ShowAudienceCore(info);
        }

        // Starts a window with something to show before the first beat, and a preview for the debug command.
        private PerformanceSession CreatePreviewSession()
        {
            return new PerformanceSession(
                Player,
                Kind,
                PerformanceInstrumentFamily.Strings,
                PerformanceTempo.Relaxed,
                DateTime.UtcNow,
                new System.Random());
        }

        private static PerformerSetup BuildPreviewSetup()
        {
            return new PerformerSetup
            {
                RoutineText = "Not performing",
                SetText = "Not performing",
                SongText = "Not performing",
                InstrumentText = "Not performing",
                TempoText = PerformanceTempo.Relaxed.GetLabel()
            };
        }

        private void ResetShown()
        {
            _shownProgress = -1f;
            _shownTier = -1;
            _shownPercent = -2;
            _shownStreak = -1;
            _shownMeter = -1;
            _shownOutcome = -1;
            _shownTempo = null;
            _shownVenue = null;
            _shownFollowing = null;
            _shownInspiring = null;
        }

        private void AssignShared()
        {
            BeatProgress = 1f;
            FlourishProgress = 0f;
            StreakText = "0";
            FlourishText = $"Flourish 0 / {PerformanceMath.FlourishMeterMax}";
            LastResultText = string.Empty;
            LastResultColor = PerformanceWindowColors.Dim;
            QualityRoughText = QualityNames[0];
            QualitySteadyText = QualityNames[1];
            QualityGreatText = QualityNames[2];
            QualityDazzlingText = QualityNames[3];
            QualityRoughColor = PerformanceWindowColors.Dim;
            QualitySteadyColor = PerformanceWindowColors.Dim;
            QualityGreatColor = PerformanceWindowColors.Dim;
            QualityDazzlingColor = PerformanceWindowColors.Dim;
        }

        // The tier names are always shown; the current tier is highlighted and carries the percent of recent cues hit.
        private void ShowQuality(PerformanceSession session)
        {
            var tier = (int)session.Quality;
            var percent = session.RecordedCount >= PerformanceMath.QualityMinimumRecorded
                ? session.RecentHits * 100 / session.RecordedCount
                : -1;

            var tierChanged = PerformanceBindCache.Changed(ref _shownTier, tier);
            var percentChanged = PerformanceBindCache.Changed(ref _shownPercent, percent);
            if (!tierChanged && !percentChanged)
                return;

            for (var index = 0; index < QualityCount; index++)
            {
                var isCurrent = index == tier;
                if (!tierChanged && !isCurrent)
                    continue;

                SetQualityText(index, isCurrent && percent >= 0 ? $"{QualityNames[index]} {percent}%" : QualityNames[index]);
                if (tierChanged)
                    SetQualityColor(index, isCurrent ? PerformanceWindowColors.Green : PerformanceWindowColors.Dim);
            }
        }

        private void ShowLastResult(PerformanceBeatOutcome outcome)
        {
            if (!PerformanceBindCache.Changed(ref _shownOutcome, (int)outcome))
                return;

            switch (outcome)
            {
                case PerformanceBeatOutcome.Hit:
                    LastResultText = $"Hit last {CueNoun}";
                    LastResultColor = PerformanceWindowColors.Green;
                    break;
                case PerformanceBeatOutcome.Miss:
                    LastResultText = $"Missed last {CueNoun}";
                    LastResultColor = PerformanceWindowColors.Amber;
                    break;
                default:
                    LastResultText = "Held the rest";
                    LastResultColor = PerformanceWindowColors.Dim;
                    break;
            }
        }

        private void SetQualityText(int index, string text)
        {
            switch (index)
            {
                case 0: QualityRoughText = text; break;
                case 1: QualitySteadyText = text; break;
                case 2: QualityGreatText = text; break;
                default: QualityDazzlingText = text; break;
            }
        }

        private void SetQualityColor(int index, GuiColor color)
        {
            switch (index)
            {
                case 0: QualityRoughColor = color; break;
                case 1: QualitySteadyColor = color; break;
                case 2: QualityGreatColor = color; break;
                default: QualityDazzlingColor = color; break;
            }
        }
    }
}
