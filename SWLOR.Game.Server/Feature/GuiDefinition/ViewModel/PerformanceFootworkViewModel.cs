using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The dancer's cue window: the NOW cue and the next five as icons with their hot bar slot, the time left on the beat,
    /// quality, streak and Flourish. Input stays on the hot bar; this window only shows state.
    /// </summary>
    public class PerformanceFootworkViewModel : PerformanceCueViewModelBase<PerformanceFootworkViewModel>
    {
        public const string ContentElement = "performance_footwork_content";
        public const string MainContentPartial = "PERFORMANCE_FOOTWORK_MAIN";
        public const int CueCount = PerformanceSession.VisibleBeatCount;

        private readonly PerformanceBeat[] _upcoming = new PerformanceBeat[CueCount - 1];
        private readonly string[] _shownImages = new string[CueCount];
        private readonly string[] _shownSlots = new string[CueCount];
        private string _shownRoutine;

        public string RoutineText { get => Get<string>(); set => Set(value); }

        // One icon and one slot number per tile: the NOW cue first, then the next five.
        public string Cue0Image { get => Get<string>(); set => Set(value); }
        public string Cue1Image { get => Get<string>(); set => Set(value); }
        public string Cue2Image { get => Get<string>(); set => Set(value); }
        public string Cue3Image { get => Get<string>(); set => Set(value); }
        public string Cue4Image { get => Get<string>(); set => Set(value); }
        public string Cue5Image { get => Get<string>(); set => Set(value); }
        public string Cue0Slot { get => Get<string>(); set => Set(value); }
        public string Cue1Slot { get => Get<string>(); set => Set(value); }
        public string Cue2Slot { get => Get<string>(); set => Set(value); }
        public string Cue3Slot { get => Get<string>(); set => Set(value); }
        public string Cue4Slot { get => Get<string>(); set => Set(value); }
        public string Cue5Slot { get => Get<string>(); set => Set(value); }

        public override bool WantsBeatProgress => true;

        protected override PerformanceKind Kind => PerformanceKind.Dance;
        protected override string ContentElementId => ContentElement;
        protected override string MainPartialName => MainContentPartial;
        protected override string CueNoun => "cue";

        protected override void ResetCueCache()
        {
            _shownRoutine = null;
            for (var index = 0; index < CueCount; index++)
            {
                _shownImages[index] = null;
                _shownSlots[index] = null;
            }
        }

        protected override void ShowSetupCore(PerformerSetup setup)
        {
            if (PerformanceBindCache.Changed(ref _shownRoutine, setup.RoutineText))
                RoutineText = setup.RoutineText;
        }

        protected override void ShowAudienceCore(PerformerAudienceInfo info)
        {
        }

        protected override void ShowCues(PerformanceSession session)
        {
            session.CopyUpcomingTo(_upcoming);

            ShowTile(0, session.Current);
            for (var index = 1; index < CueCount; index++)
                ShowTile(index, _upcoming[index - 1]);
        }

        // A tile changes only when the cue that scrolled into it is a different one.
        private void ShowTile(int index, PerformanceBeat beat)
        {
            var image = beat.IsRest ? string.Empty : PerformanceCueIcons.GetIcon(beat.Primary);
            var slot = beat.IsRest ? string.Empty : PerformanceCueIcons.GetSlotText(beat.Primary);

            if (PerformanceBindCache.Changed(ref _shownImages[index], image))
                SetImage(index, image);
            if (PerformanceBindCache.Changed(ref _shownSlots[index], slot))
                SetSlot(index, slot);
        }

        private void SetImage(int index, string value)
        {
            switch (index)
            {
                case 0: Cue0Image = value; break;
                case 1: Cue1Image = value; break;
                case 2: Cue2Image = value; break;
                case 3: Cue3Image = value; break;
                case 4: Cue4Image = value; break;
                default: Cue5Image = value; break;
            }
        }

        private void SetSlot(int index, string value)
        {
            switch (index)
            {
                case 0: Cue0Slot = value; break;
                case 1: Cue1Slot = value; break;
                case 2: Cue2Slot = value; break;
                case 3: Cue3Slot = value; break;
                case 4: Cue4Slot = value; break;
                default: Cue5Slot = value; break;
            }
        }
    }
}
