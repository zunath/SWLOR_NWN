using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.MusicService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MusicPickerViewModel : GuiViewModelBase<MusicPickerViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "music_picker_content";
        public const string PersonalContentPartial = "MUSIC_PICKER_PERSONAL";
        public const string AreaContentPartial = "MUSIC_PICKER_AREA";
        public const int PageSize = 8;
        private const string OtherCategoryName = "Other";

        private MusicPickerMode _mode;
        private uint _jukebox;
        private int _page;
        private string _selectedCategory = string.Empty;
        private string[] _categories = Array.Empty<string>();
        private MusicPickerEntry[] _entries = Array.Empty<MusicPickerEntry>();
        private MusicPickerEntry[] _visible = Array.Empty<MusicPickerEntry>();

        public string WindowTitle { get => Get<string>(); set => Set(value); }
        public string HeaderText { get => Get<string>(); set => Set(value); }
        public string ResultText { get => Get<string>(); set => Set(value); }
        public string PageText { get => Get<string>(); set => Set(value); }
        public string NowPlayingText { get => Get<string>(); set => Set(value); }
        public string BattleThemeText { get => Get<string>(); set => Set(value); }
        public bool HasPrevious { get => Get<bool>(); set => Set(value); }
        public bool HasNext { get => Get<bool>(); set => Set(value); }
        public bool IsCollectionMode { get => Get<bool>(); set => Set(value); }

        public string SearchText
        {
            get => Get<string>();
            set
            {
                Set(value);
                _page = 0;
                RefreshResults();
            }
        }

        public bool ShowUncollected
        {
            get => Get<bool>();
            set
            {
                Set(value);
                _page = 0;
                RefreshResults();
            }
        }

        public GuiBindingList<string> CategoryNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> CategorySelected { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> SongNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> SongCategories { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> PlayEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var payload = initialPayload as MusicPickerPayload ?? new MusicPickerPayload(MusicPickerMode.Personal);
            _jukebox = payload.Jukebox;
            _mode = payload.Mode;

            // DMs can always play any track, whether they open the window by chat command or jukebox.
            if (GetIsDM(Player) || GetIsDMPossessed(Player))
                _mode = MusicPickerMode.DungeonMaster;

            IsCollectionMode = _mode != MusicPickerMode.DungeonMaster;
            _entries = BuildEntries(out var headerText);
            HeaderText = headerText;
            WindowTitle = _mode switch
            {
                MusicPickerMode.Personal => "Portable Music Player",
                MusicPickerMode.Jukebox => "Jukebox",
                _ => "Area Music",
            };

            _categories = new[] { string.Empty }
                .Concat(_entries.Select(entry => entry.Category).Distinct().OrderBy(name => name))
                .ToArray();
            var categoryNames = new GuiBindingList<string>();
            foreach (var category in _categories)
            {
                var count = _entries.Count(entry => category.Length == 0 || entry.Category == category);
                categoryNames.Add($"{(category.Length == 0 ? "All Songs" : category)} ({count})");
            }

            CategoryNames = categoryNames;
            CategorySelected = new GuiBindingList<bool>();
            SongNames = new GuiBindingList<string>();
            SongCategories = new GuiBindingList<string>();
            PlayEnabled = new GuiBindingList<bool>();
            ResultText = string.Empty;
            PageText = string.Empty;
            ShowUncollected = false;
            SearchText = string.Empty;
            SelectCategory(0);
            RefreshPlaybackText();

            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.ShowUncollected);
            ChangePartialView(ContentElement, _mode == MusicPickerMode.Personal ? PersonalContentPartial : AreaContentPartial);
        }

        private MusicPickerEntry[] BuildEntries(out string headerText)
        {
            if (_mode == MusicPickerMode.DungeonMaster)
            {
                var tracks = Music.GetAllSongs()
                    .Where(track => track.ID > 0)
                    .Select(track => Music.TryGetCollectibleSongByTrackId(track.ID, out var song)
                        ? new MusicPickerEntry(track.ID, Music.GetSongName(song), Music.GetCategoryName(song.Category), true)
                        : new MusicPickerEntry(track.ID, track.DisplayName, OtherCategoryName, true))
                    .OrderBy(entry => entry.Name)
                    .ToArray();
                headerText = $"All tracks: {tracks.Length}. Songs play for everyone in this area.";
                return tracks;
            }

            var ownerId = GetObjectUUID(Player);
            if (_mode == MusicPickerMode.Jukebox)
            {
                var jukeboxOwnerId = Music.GetJukeboxOwnerId(_jukebox);
                if (!string.IsNullOrWhiteSpace(jukeboxOwnerId))
                    ownerId = jukeboxOwnerId;
            }

            var collected = Music.GetCollectedTrackIds(ownerId);
            var entries = Music.GetCollectibleSongs()
                .Select(song => new MusicPickerEntry(
                    song.TrackId,
                    Music.GetSongName(song),
                    Music.GetCategoryName(song.Category),
                    collected.Contains(song.TrackId)))
                .OrderBy(entry => entry.Name)
                .ToArray();

            var total = entries.Length;
            if (_mode == MusicPickerMode.Personal)
            {
                headerText = $"Your collection: {collected.Count} of {total} songs. Only you hear them.";
            }
            else if (ownerId == GetObjectUUID(Player))
            {
                headerText = $"Your collection: {collected.Count} of {total} songs. Everyone in this area hears them.";
            }
            else
            {
                var ownerName = PlayerName.GetDisplayNameByPlayerId(Player, ownerId, "The owner");
                headerText = $"{ownerName}'s collection: {collected.Count} songs. Everyone in this area hears them.";
            }

            return entries;
        }

        public Action OnSelectCategory() => () => SelectCategory(NuiGetEventArrayIndex());

        private void SelectCategory(int index)
        {
            if (index < 0 || index >= _categories.Length)
                return;

            _selectedCategory = _categories[index];
            var selected = new GuiBindingList<bool>();
            for (var i = 0; i < _categories.Length; i++)
                selected.Add(i == index);

            CategorySelected = selected;
            _page = 0;
            RefreshResults();
        }

        private void RefreshResults()
        {
            var search = SearchText ?? string.Empty;
            var matches = _entries
                .Where(entry => _selectedCategory.Length == 0 || entry.Category == _selectedCategory)
                .Where(entry => entry.IsAvailable || ShowUncollected)
                .Where(entry => search.Length == 0 || entry.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var pages = Math.Max(1, (matches.Length + PageSize - 1) / PageSize);
            _page = Math.Clamp(_page, 0, pages - 1);
            _visible = matches.Skip(_page * PageSize).Take(PageSize).ToArray();

            var names = new GuiBindingList<string>();
            var categories = new GuiBindingList<string>();
            var playEnabled = new GuiBindingList<bool>();
            foreach (var entry in _visible)
            {
                names.Add(entry.IsAvailable ? entry.Name : $"{entry.Name} (not collected)");
                categories.Add(entry.Category);
                playEnabled.Add(entry.IsAvailable);
            }

            SongNames = names;
            SongCategories = categories;
            PlayEnabled = playEnabled;
            ResultText = matches.Length == 1 ? "1 song" : $"{matches.Length} songs";
            PageText = $"Page {_page + 1} / {pages}";
            HasPrevious = _page > 0;
            HasNext = _page + 1 < pages;
        }

        public Action OnClearSearch() => () => SearchText = string.Empty;
        public Action OnPreviousPage() => () => { _page--; RefreshResults(); };
        public Action OnNextPage() => () => { _page++; RefreshResults(); };

        public Action OnPlaySong() => () =>
        {
            if (!TryGetSelectedEntry(out var entry))
                return;

            if (_mode == MusicPickerMode.Personal)
                Music.PlayForPlayer(Player, entry.TrackId);
            else
                Music.PlayInArea(GetMusicArea(), entry.TrackId);

            FloatingTextStringOnCreature($"Now playing: {entry.Name}", Player, false);
            RefreshPlaybackText();
        };

        public Action OnSetBattleTheme() => () =>
        {
            if (_mode != MusicPickerMode.Personal || !TryGetSelectedEntry(out var entry))
                return;

            Music.SetBattleTheme(Player, entry.TrackId);
            FloatingTextStringOnCreature($"Battle theme: {entry.Name}", Player, false);
            RefreshPlaybackText();
        };

        public Action OnClearBattleTheme() => () =>
        {
            if (_mode != MusicPickerMode.Personal)
                return;

            Music.SetBattleTheme(Player, null);
            RefreshPlaybackText();
        };

        public Action OnStop() => () =>
        {
            if (_mode == MusicPickerMode.Personal)
                Music.StopForPlayer(Player);
            else
                Music.StopInArea(GetMusicArea());

            RefreshPlaybackText();
        };

        private bool TryGetSelectedEntry(out MusicPickerEntry entry)
        {
            entry = null;
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _visible.Length)
                return false;

            entry = _visible[index];
            return entry.IsAvailable;
        }

        private uint GetMusicArea()
        {
            return _mode == MusicPickerMode.Jukebox && GetIsObjectValid(_jukebox)
                ? GetArea(_jukebox)
                : GetArea(Player);
        }

        private void RefreshPlaybackText()
        {
            if (_mode == MusicPickerMode.Personal)
            {
                var trackId = Music.GetPersonalTrackId(Player);
                NowPlayingText = trackId == null
                    ? "Now playing: area music"
                    : $"Now playing: {Music.GetTrackName(trackId.Value)}";

                var battleThemeId = Music.GetBattleThemeId(Player);
                BattleThemeText = battleThemeId == null
                    ? "Battle theme: area music"
                    : $"Battle theme: {Music.GetTrackName(battleThemeId.Value)}";
                return;
            }

            var area = GetMusicArea();
            NowPlayingText = $"Now playing: {Music.GetTrackName(MusicBackgroundGetDayTrack(area))}";
            BattleThemeText = string.Empty;
        }
    }
}
