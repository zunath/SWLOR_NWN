using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The audience tracker. It opens by itself for a watcher after their first counted minute and is updated only when the
    /// audience tick reports, or when the watcher clicks Follow. It never runs its own timer.
    /// </summary>
    public class PerformanceWatchingViewModel : GuiViewModelBase<PerformanceWatchingViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "performance_watching_content";
        public const string MainContentPartial = "PERFORMANCE_WATCHING_MAIN";

        private PerformanceWatchRow[] _rows = System.Array.Empty<PerformanceWatchRow>();
        private string _shownHeader;
        private string _shownBanked;
        private string _shownDebt;
        private string _shownLeaveWith;
        private float _shownFraction = -1f;

        public string HeaderText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> RowTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RowColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> FollowTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> FollowEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public float BankedFraction { get => Get<float>(); set => Set(value); }
        public string BankedText { get => Get<string>(); set => Set(value); }
        public string DebtText { get => Get<string>(); set => Set(value); }
        public string LeaveWithText { get => Get<string>(); set => Set(value); }

        /// <summary>The watcher hears the followed band's song. Clearing it keeps their own music.</summary>
        public bool PlayPerformerSong { get => Get<bool>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            HeaderText = "Not watching a performance";
            RowTexts = new GuiBindingList<string>();
            RowColors = new GuiBindingList<GuiColor>();
            FollowTexts = new GuiBindingList<string>();
            FollowEnabled = new GuiBindingList<bool>();
            BankedFraction = 0f;
            BankedText = string.Empty;
            DebtText = string.Empty;
            LeaveWithText = string.Empty;
            PlayPerformerSong = Performance.GetPreference(Player).PlayPerformerSong;
            _rows = System.Array.Empty<PerformanceWatchRow>();
            _shownHeader = null;
            _shownBanked = null;
            _shownDebt = null;
            _shownLeaveWith = null;
            _shownFraction = -1f;

            WatchOnClient(model => model.PlayPerformerSong);

            // A window opened from the debug command or reopened mid-show starts from the last tick's view.
            var view = PerformanceAudience.BuildWatchingView(Player);
            if (view != null)
                ShowView(view);

            ChangePartialView(ContentElement, MainContentPartial);

            // The client keeps no input values across the layout swap, so the checkbox is pushed again after it.
            PlayPerformerSong = PlayPerformerSong;
        }

        public override Action OnWindowClosed() => () => PerformanceAudience.OnWatchingWindowClosed(Player);

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName == nameof(PlayPerformerSong))
                PerformanceAudience.SetPlayPerformerSong(Player, PlayPerformerSong);
        }

        public Action OnFollowRow() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _rows.Length)
                return;

            // A performer the watcher picked offers Unfollow, which goes back to following the strongest performer.
            var row = _rows[index];
            var changed = row.IsChosen
                ? PerformanceAudience.Unfollow(Player, row.Performer)
                : PerformanceAudience.Follow(Player, row.Performer);
            if (changed)
                PerformanceAudience.RefreshWatching(Player);
        };

        /// <summary>Shows one watcher's audience tick. Values that did not change since the last tick are not sent.</summary>
        public void ShowView(PerformanceWatchView view)
        {
            ShowRows(view.Rows);

            if (PerformanceBindCache.Changed(ref _shownHeader, view.HeaderText))
                HeaderText = view.HeaderText;
            if (PerformanceBindCache.Changed(ref _shownFraction, view.BankedFraction))
                BankedFraction = view.BankedFraction;
            if (PerformanceBindCache.Changed(ref _shownBanked, view.BankedText))
                BankedText = view.BankedText;
            if (PerformanceBindCache.Changed(ref _shownDebt, view.DebtText))
                DebtText = view.DebtText;
            if (PerformanceBindCache.Changed(ref _shownLeaveWith, view.LeaveWithText))
                LeaveWithText = view.LeaveWithText;
        }

        private bool IsSameRows(System.Collections.Generic.IReadOnlyList<PerformanceWatchRow> rows)
        {
            if (_rows.Length != rows.Count)
                return false;

            for (var index = 0; index < _rows.Length; index++)
            {
                if (rows[index] != _rows[index])
                    return false;
            }

            return true;
        }

        // The performer list is replaced only when who is in range, or who is followed, is different from last time.
        private void ShowRows(System.Collections.Generic.IReadOnlyList<PerformanceWatchRow> rows)
        {
            if (IsSameRows(rows))
                return;

            _rows = rows.ToArray();

            var texts = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var followTexts = new GuiBindingList<string>();
            var followEnabled = new GuiBindingList<bool>();
            foreach (var row in _rows)
            {
                texts.Add($"{row.Name} - {row.Detail}");
                colors.Add(row.IsFollowed ? PerformanceWindowColors.Gold : PerformanceWindowColors.White);
                followTexts.Add(row.IsChosen ? "Unfollow" : row.IsFollowed ? "Following" : "Follow");
                followEnabled.Add(row.IsChosen || !row.IsFollowed);
            }

            RowTexts = texts;
            RowColors = colors;
            FollowTexts = followTexts;
            FollowEnabled = followEnabled;
        }
    }
}
