using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Perform window, opened by the Options button on the performance hot bar: pick the routine or set, a second one with
    /// the capstone, the song, and the tempo, then Apply. Nothing changes until Apply.
    /// </summary>
    public class PerformanceOptionsViewModel : GuiViewModelBase<PerformanceOptionsViewModel, GuiPayloadBase>
    {
        private const int DanceTabId = 0;
        private const int InstrumentTabId = 1;
        public const string TabContentElement = "performance_options_tab_content";
        public const string DanceTabPartial = "PERFORMANCE_OPTIONS_TAB_DANCE";
        public const string InstrumentTabPartial = "PERFORMANCE_OPTIONS_TAB_INSTRUMENT";

        private static readonly PerformanceRoutineType[] AllRoutines = System.Enum.GetValues<PerformanceRoutineType>();
        private static readonly PerformanceSetType[] AllSets = System.Enum.GetValues<PerformanceSetType>();

        private static readonly GuiTabGroup<PerformanceOptionsViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<PerformanceOptionsViewModel, GuiPayloadBase>()
                .AddTab(DanceTabId, DanceTabPartial, model => model.RefreshDanceTab())
                .AddTab(InstrumentTabId, InstrumentTabPartial, model => model.RefreshInstrumentTab());

        private static readonly GuiToggleGroupSync TabToggles = new(DanceTabId, InstrumentTabId);

        private int _routineIndex;
        private int _setIndex;
        private bool _hasShowstopper;
        private bool _hasVirtuoso;
        private int _venueTotal;
        private PerformanceRoutineType[] _secondRoutineChoices = { PerformanceRoutineType.CantinaShuffle };
        private PerformanceSetType[] _secondSetChoices = { PerformanceSetType.CantinaStandards };
        private PerformanceSongOption[] _songs = System.Array.Empty<PerformanceSongOption>();

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        // Rule R4: this widget-bound property only forwards genuine user clicks.
        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        public GuiBindingList<string> RoutineNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RoutineNotes { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> RoutineSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> RoutineEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public GuiBindingList<string> SetNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> SetNotes { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> SetSelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> SetEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> SecondRoutineOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SecondRoutineIndex { get => Get<int>(); set => Set(value); }
        public bool SecondRoutineEnabled { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> SecondSetOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SecondSetIndex { get => Get<int>(); set => Set(value); }
        public bool SecondSetEnabled { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> SongOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SongIndex { get => Get<int>(); set => Set(value); }
        public bool SongEnabled { get => Get<bool>(); set => Set(value); }

        public int TempoValue { get => Get<int>(); set => Set(value); }

        public string VenueHeaderText { get => Get<string>(); set => Set(value); }
        public string VenueAreaText { get => Get<string>(); set => Set(value); }
        public string VenueFloorText { get => Get<string>(); set => Set(value); }
        public string AudienceText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var kind = Performance.TryGetSession(Player, out var session) ? session.Kind : PerformanceKind.Dance;
            var firstTab = kind == PerformanceKind.Music ? InstrumentTabId : DanceTabId;

            // Rule R3: assign every watched property before WatchOnClient. Each tab loads its own lists when it is selected.
            TabToggles.SyncTo(firstTab, value => TabToggleValue = value);
            TempoValue = (int)Performance.GetPreference(Player).Tempo;
            SecondRoutineIndex = 0;
            SecondSetIndex = 0;
            SongIndex = 0;

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.TempoValue);
            WatchOnClient(model => model.SecondRoutineIndex);
            WatchOnClient(model => model.SecondSetIndex);
            WatchOnClient(model => model.SongIndex);

            SelectTab(firstTab);
        }

        public override Action OnWindowClosed() => () => { };

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);

            // The client keeps no input values across the layout swap, so they are pushed again after it.
            Tabs.Select(this, TabContentElement, tabId, PushInputValues);
        }

        private void PushInputValues()
        {
            TabToggles.SyncTo(SelectedTabId, value => TabToggleValue = value);
            TempoValue = TempoValue;
            SecondRoutineIndex = SecondRoutineIndex;
            SecondSetIndex = SecondSetIndex;
            SongIndex = SongIndex;
        }

        private void RefreshDanceTab()
        {
            LoadDance();
            LoadVenue();
            RefreshAudienceText(DanceTabId);
        }

        private void RefreshInstrumentTab()
        {
            LoadInstrument();
            LoadVenue();
            RefreshAudienceText(InstrumentTabId);
        }

        public Action OnSelectRoutine() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= AllRoutines.Length || !Performance.IsRoutineUnlocked(Player, AllRoutines[index]))
                return;

            _routineIndex = index;
            RoutineSelected = BuildSelection(AllRoutines.Length, index);
        };

        public Action OnSelectSet() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= AllSets.Length || !Performance.IsSetUnlocked(Player, AllSets[index]))
                return;

            _setIndex = index;
            SetSelected = BuildSelection(AllSets.Length, index);
        };

        public Action OnApply() => () =>
        {
            if (SelectedTabId == DanceTabId)
                ApplyDance();
            else
                ApplyInstrument();

            var tempo = (PerformanceTempo)Math.Clamp(TempoValue, 0, System.Enum.GetValues<PerformanceTempo>().Length - 1);
            if (tempo != Performance.GetPreference(Player).Tempo)
                Performance.SetTempo(Player, tempo);

            SendMessageToPC(Player, "Performance options applied.");
            RefreshAudienceText(SelectedTabId);
        };

        private void ApplyDance()
        {
            var preference = Performance.GetPreference(Player);
            var routine = AllRoutines[Math.Clamp(_routineIndex, 0, AllRoutines.Length - 1)];
            if (routine != preference.Routine)
                Performance.SetRoutine(Player, routine);

            if (!_hasShowstopper)
                return;

            var second = _secondRoutineChoices[Math.Clamp(SecondRoutineIndex, 0, _secondRoutineChoices.Length - 1)];
            if (second != preference.SecondRoutine)
                Performance.SetSecondRoutine(Player, second);
        }

        private void ApplyInstrument()
        {
            var preference = Performance.GetPreference(Player);
            var set = AllSets[Math.Clamp(_setIndex, 0, AllSets.Length - 1)];
            if (set != preference.Set)
                Performance.SetSet(Player, set);

            if (_hasVirtuoso)
            {
                var second = _secondSetChoices[Math.Clamp(SecondSetIndex, 0, _secondSetChoices.Length - 1)];
                if (second != preference.SecondSet)
                    Performance.SetSecondSet(Player, second);
            }

            if (_songs.Length > 0)
            {
                var song = _songs[Math.Clamp(SongIndex, 0, _songs.Length - 1)];
                if (song.TrackId != preference.SongTrackId)
                    Performance.SetSong(Player, song.TrackId);
            }
        }

        // Routines: unlocked ones can be picked; locked ones show the Performance rank that unlocks them.
        private void LoadDance()
        {
            var preference = Performance.GetPreference(Player);
            var selected = Performance.IsRoutineUnlocked(Player, preference.Routine)
                ? preference.Routine
                : PerformanceRoutineType.CantinaShuffle;

            var names = new GuiBindingList<string>();
            var notes = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            for (var index = 0; index < AllRoutines.Length; index++)
            {
                var routine = AllRoutines[index];
                var perk = PerformanceRiderCatalog.GetUnlockPerk(routine);
                var level = Perk.GetPerkLevel(Player, perk);
                var unlocked = Performance.IsRoutineUnlocked(Player, routine);

                names.Add(PerformanceRiderText.GetName(routine, level));
                enabled.Add(unlocked);
                notes.Add(unlocked
                    ? PerformanceRiderText.GetRider(
                        routine,
                        Stat.GetStatAdjustment(Player, StatType.PerformanceLightFeetSpeedPercent),
                        Stat.GetStatAdjustment(Player, StatType.PerformanceRoutineAttributeBonus))
                    : PerformanceRiderText.GetLockedText(PerformanceRiderCatalog.GetRequiredRank(perk)));

                if (routine == selected)
                    _routineIndex = index;
            }

            RoutineNames = names;
            RoutineNotes = notes;
            RoutineEnabled = enabled;
            RoutineSelected = BuildSelection(AllRoutines.Length, _routineIndex);

            LoadSecondRoutine(preference);
        }

        private void LoadSecondRoutine(PerformancePreference preference)
        {
            _hasShowstopper = Perk.GetPerkLevel(Player, PerkType.Showstopper) > 0;
            SecondRoutineEnabled = _hasShowstopper;

            var choices = new List<PerformanceRoutineType> { PerformanceRoutineType.CantinaShuffle };
            var options = new GuiBindingList<GuiComboEntry>();
            if (_hasShowstopper)
            {
                options.Add(new GuiComboEntry("None", 0));
                foreach (var routine in AllRoutines)
                {
                    if (routine == PerformanceRoutineType.CantinaShuffle || !Performance.IsRoutineUnlocked(Player, routine))
                        continue;

                    options.Add(new GuiComboEntry(PerformanceRiderText.GetName(routine), choices.Count));
                    choices.Add(routine);
                }
            }
            else
            {
                options.Add(new GuiComboEntry(BuildCapstoneText("Showstopper", PerkType.Showstopper), 0));
            }

            _secondRoutineChoices = choices.ToArray();
            SecondRoutineOptions = options;
            SecondRoutineIndex = Math.Max(0, choices.IndexOf(preference.SecondRoutine));
        }

        // Sets work like routines, and the song comes from the Cantina songs the musician owns.
        private void LoadInstrument()
        {
            var preference = Performance.GetPreference(Player);
            var selected = Performance.IsSetUnlocked(Player, preference.Set)
                ? preference.Set
                : PerformanceSetType.CantinaStandards;

            var names = new GuiBindingList<string>();
            var notes = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            for (var index = 0; index < AllSets.Length; index++)
            {
                var set = AllSets[index];
                var perk = PerformanceRiderCatalog.GetUnlockPerk(set);
                var level = Perk.GetPerkLevel(Player, perk);
                var unlocked = Performance.IsSetUnlocked(Player, set);

                names.Add(PerformanceRiderText.GetName(set, level));
                enabled.Add(unlocked);
                notes.Add(unlocked
                    ? PerformanceRiderText.GetRider(
                        set,
                        Stat.GetStatAdjustment(Player, StatType.PerformanceSteadyHandsBonus),
                        Stat.GetStatAdjustment(Player, StatType.PerformanceAnglersLullabyBonus),
                        Stat.GetStatAdjustment(Player, StatType.PerformanceSetAttributeBonus))
                    : PerformanceRiderText.GetLockedText(PerformanceRiderCatalog.GetRequiredRank(perk)));

                if (set == selected)
                    _setIndex = index;
            }

            SetNames = names;
            SetNotes = notes;
            SetEnabled = enabled;
            SetSelected = BuildSelection(AllSets.Length, _setIndex);

            LoadSecondSet(preference);
            LoadSongs(preference);
        }

        private void LoadSecondSet(PerformancePreference preference)
        {
            _hasVirtuoso = Perk.GetPerkLevel(Player, PerkType.Virtuoso) > 0;
            SecondSetEnabled = _hasVirtuoso;

            var choices = new List<PerformanceSetType> { PerformanceSetType.CantinaStandards };
            var options = new GuiBindingList<GuiComboEntry>();
            if (_hasVirtuoso)
            {
                options.Add(new GuiComboEntry("None", 0));
                foreach (var set in AllSets)
                {
                    if (set == PerformanceSetType.CantinaStandards || !Performance.IsSetUnlocked(Player, set))
                        continue;

                    options.Add(new GuiComboEntry(PerformanceRiderText.GetName(set), choices.Count));
                    choices.Add(set);
                }
            }
            else
            {
                options.Add(new GuiComboEntry(BuildCapstoneText("Virtuoso", PerkType.Virtuoso), 0));
            }

            _secondSetChoices = choices.ToArray();
            SecondSetOptions = options;
            SecondSetIndex = Math.Max(0, choices.IndexOf(preference.SecondSet));
        }

        private void LoadSongs(PerformancePreference preference)
        {
            var playerId = GetObjectUUID(Player);
            _songs = PerformanceSongs.GetAvailableSongs(playerId).ToArray();

            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < _songs.Length; index++)
                options.Add(new GuiComboEntry(_songs[index].Name, index));

            if (_songs.Length == 0)
                options.Add(new GuiComboEntry("No Cantina songs collected", 0));

            var current = PerformanceSongs.ChooseSong(playerId, preference.SongTrackId);
            SongEnabled = _songs.Length > 0;
            SongOptions = options;
            SongIndex = Math.Max(0, Array.FindIndex(_songs, song => song.TrackId == current));
        }

        private void LoadVenue()
        {
            var breakdown = PerformanceVenue.GetBreakdown(Player);
            _venueTotal = breakdown.Total;
            VenueHeaderText = $"VENUE RATING {breakdown.Total} / {PerformanceMath.MaxVenueRating}";
            VenueAreaText = $"{breakdown.AreaText} +{breakdown.AreaRating}";
            VenueFloorText = breakdown.DanceFloor
                ? $"Standing on a Dance Floor +{PerformanceVenue.DanceFloorBonus}"
                : $"Stand on a Dance Floor for +{PerformanceVenue.DanceFloorBonus}";
        }

        // What the audience of this kind of performer gets: the performer's own XP bonus plus the venue, and the debt cleared.
        private void RefreshAudienceText(int tabId)
        {
            var kind = tabId == InstrumentTabId ? PerformanceKind.Music : PerformanceKind.Dance;
            var bonus = PerformanceWindows.GetAudienceBonus(Player, kind);
            AudienceText = $"Your audience: +{bonus.XpPercent + _venueTotal}% skill XP, {bonus.DebtPercent}% debt per minute";
        }

        private static string BuildCapstoneText(string capstone, PerkType perk)
        {
            return $"Requires {capstone} ({PerformanceRiderText.GetLockedText(PerformanceRiderCatalog.GetRequiredRank(perk))})";
        }

        private static GuiBindingList<bool> BuildSelection(int count, int selectedIndex)
        {
            var selection = new GuiBindingList<bool>();
            for (var index = 0; index < count; index++)
                selection.Add(index == selectedIndex);

            return selection;
        }
    }
}
