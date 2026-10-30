using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The musician's cue window: three lanes (Low, Mid, High) of upcoming notes with a strike row, the band line,
    /// quality, streak and Flourish. Input stays on the hot bar; this window only shows state.
    /// </summary>
    public class PerformanceSessionViewModel : PerformanceCueViewModelBase<PerformanceSessionViewModel>
    {
        public const string ContentElement = "performance_session_content";
        public const string MainContentPartial = "PERFORMANCE_SESSION_MAIN";

        private const int LaneCount = 3;
        public const int UpcomingRows = PerformanceSession.VisibleBeatCount - 1;

        private static readonly PerformanceCue[] Lanes = { PerformanceCue.Low, PerformanceCue.Mid, PerformanceCue.High };

        private readonly PerformanceBeat[] _upcoming = new PerformanceBeat[UpcomingRows];
        private readonly bool[][] _shownLanes = new bool[LaneCount][];
        private readonly bool[] _freshLane = new bool[UpcomingRows];
        private readonly bool?[] _shownStrikes = new bool?[LaneCount];
        private string _shownInstrument;
        private string _shownSong;
        private string _shownSet;
        private string _shownBand;
        private string _shownHears;

        public string InstrumentText { get => Get<string>(); set => Set(value); }
        public string SongText { get => Get<string>(); set => Set(value); }
        public string SetText { get => Get<string>(); set => Set(value); }
        public string BandText { get => Get<string>(); set => Set(value); }
        public string AudienceHearsText { get => Get<string>(); set => Set(value); }

        // Each lane shows its note icon on the rows that need it, the furthest note first; the note due now sits in the
        // strike row. The icon is fixed per lane and only its visibility is bound: an empty resref draws a missing-texture box.
        public GuiBindingList<bool> LowLane { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> MidLane { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> HighLane { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public bool LowStrike { get => Get<bool>(); set => Set(value); }
        public bool MidStrike { get => Get<bool>(); set => Set(value); }
        public bool HighStrike { get => Get<bool>(); set => Set(value); }

        public override bool WantsBeatProgress => false;

        protected override PerformanceKind Kind => PerformanceKind.Music;
        protected override string ContentElementId => ContentElement;
        protected override string MainPartialName => MainContentPartial;
        protected override string CueNoun => "note";

        protected override void ResetCueCache()
        {
            _shownInstrument = null;
            _shownSong = null;
            _shownSet = null;
            _shownBand = null;
            _shownHears = null;
            for (var lane = 0; lane < LaneCount; lane++)
            {
                _shownLanes[lane] = null;
                _shownStrikes[lane] = null;
            }
        }

        protected override void ShowSetupCore(PerformerSetup setup)
        {
            if (PerformanceBindCache.Changed(ref _shownInstrument, setup.InstrumentText))
                InstrumentText = setup.InstrumentText;
            if (PerformanceBindCache.Changed(ref _shownSong, setup.SongText))
                SongText = setup.SongText;
            if (PerformanceBindCache.Changed(ref _shownSet, setup.SetText))
                SetText = setup.SetText;
        }

        protected override void ShowAudienceCore(PerformerAudienceInfo info)
        {
            if (PerformanceBindCache.Changed(ref _shownBand, info.BandText))
                BandText = info.BandText;
            if (PerformanceBindCache.Changed(ref _shownHears, info.AudienceHearsText))
                AudienceHearsText = info.AudienceHearsText;
        }

        protected override void ShowCues(PerformanceSession session)
        {
            session.CopyUpcomingTo(_upcoming);

            for (var lane = 0; lane < LaneCount; lane++)
            {
                var cue = Lanes[lane];

                // Row 0 is the furthest note, so the next note is the last row above the strike row.
                for (var row = 0; row < UpcomingRows; row++)
                    _freshLane[row] = GetNote(_upcoming[UpcomingRows - 1 - row], cue);

                ShowLane(lane);
                ShowStrike(lane, GetNote(session.Current, cue));
            }
        }

        private static bool GetNote(PerformanceBeat beat, PerformanceCue lane)
        {
            return !beat.IsRest && beat.Requires(lane);
        }

        // A lane's list is replaced as a whole, and only when one of its rows is different.
        private void ShowLane(int lane)
        {
            var shown = _shownLanes[lane];
            if (shown != null && IsSame(shown, _freshLane))
                return;

            _shownLanes[lane] = (bool[])_freshLane.Clone();
            var list = new GuiBindingList<bool>();
            foreach (var note in _freshLane)
                list.Add(note);

            switch (lane)
            {
                case 0: LowLane = list; break;
                case 1: MidLane = list; break;
                default: HighLane = list; break;
            }
        }

        private static bool IsSame(bool[] shown, bool[] fresh)
        {
            for (var row = 0; row < fresh.Length; row++)
            {
                if (shown[row] != fresh[row])
                    return false;
            }

            return true;
        }

        private void ShowStrike(int lane, bool note)
        {
            if (!PerformanceBindCache.Changed(ref _shownStrikes[lane], note))
                return;

            switch (lane)
            {
                case 0: LowStrike = note; break;
                case 1: MidStrike = note; break;
                default: HighStrike = note; break;
            }
        }
    }
}
