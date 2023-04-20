using System;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Service.Music;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService.Component;
using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MusicManagerViewModel : GuiViewModelBase<MusicManagerViewModel, GuiPayloadBase>
    {
        private readonly List<string> _songNames = new();
        private readonly List<int> _songIds = new();

        private const int ListingsPerPage = 20;
        private bool _skipPaginationSearch;

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);

                if (!_skipPaginationSearch)
                    Search();
            }
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMusicSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public int SelectedMusicIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public GuiBindingList<string> MusicList
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> MusicToggled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            Search();
            WatchOnClient(model => model.SearchText);
        }

        public Action OnSelectMusic() => () =>
        {
            if (SelectedMusicIndex > -1)
                MusicToggled[SelectedMusicIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedMusicIndex = index;

            MusicToggled[SelectedMusicIndex] = true;
            IsMusicSelected = true;
        };

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            _songNames.Clear();
            _songIds.Clear();
            SelectedPageIndex = 0;
            SearchText = string.Empty;
            Search();
        };

        public Action OnClickPlayMusic() => () =>
        {
            var area = GetArea(Player);
            var musicName = _songNames[SelectedMusicIndex];
            var musicId = _songIds[SelectedMusicIndex];

            SendMessageToPC(Player, $"You are now playing the song: {musicName}");

            MusicBackgroundChangeDay(area, musicId);
            MusicBackgroundChangeNight(area, musicId);
            MusicBackgroundPlay(area);
        };

        public Action OnClicKStopMusic() => () =>
        {
            SendMessageToPC(Player, "Stopped music.");

            MusicBackgroundStop(GetArea(Player));
        };

        public void Search()
        {
            var allSongs = GetAllSongs();

            var pagedallSongs = allSongs
                .Skip(SelectedPageIndex * ListingsPerPage)
                .Take(ListingsPerPage)
                .ToList();

            var musicList = new GuiBindingList<string>();
            var musicToggled = new GuiBindingList<bool>();

            _songNames.Clear();
            _songIds.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var song in pagedallSongs)
                {
                    var songName = song.DisplayName;
                    var songID = song.ID;

                    musicList.Add(songName);
                    _songIds.Add(songID);
                    _songNames.Add(songName);
                    musicToggled.Add(false);

                    int totalRecordCount = allSongs.Count();
                    UpdatePagination(totalRecordCount);
                }
            }
            else
            {
                foreach (var song in allSongs)
                {
                    var songName = song.DisplayName;
                    var songID = song.ID;

                    if (GetStringUpperCase(songName).Contains(GetStringUpperCase(SearchText)))
                    {
                        musicList.Add(songName);
                        _songIds.Add(songID);
                        _songNames.Add(songName);
                        musicToggled.Add(false);

                        int totalRecordCount = musicList.Count();
                        UpdatePagination(totalRecordCount);
                    }
                }
            }

            MusicList = musicList;
            MusicToggled = musicToggled;
        }

        private void UpdatePagination(long totalRecordCount)
        {
            _skipPaginationSearch = true;
            var pageNumbers = new GuiBindingList<GuiComboEntry>();
            var pages = (int)(totalRecordCount / ListingsPerPage + (totalRecordCount % ListingsPerPage == 0 ? 0 : 1));

            // Always add page 1. In the event no creatures are found,
            // it still needs to be displayed.
            pageNumbers.Add(new GuiComboEntry($"Page 1", 0));
            for (var x = 2; x <= pages; x++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {x}", x - 1));
            }

            PageNumbers = pageNumbers;

            // In the event no results are found, default the index to zero
            if (pages <= 0)
                SelectedPageIndex = 0;
            // If current page is outside the new page bounds, set it to the last page in the list.
            else if (SelectedPageIndex > pages - 1)
                SelectedPageIndex = pages - 1;
            // If current page is negative, set it to zero.
            else if (SelectedPageIndex < 0)
                SelectedPageIndex = 0;
        }

        public Action OnClickPreviousPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex - 1;
            if (newPage < 0)
                newPage = 0;

            SelectedPageIndex = newPage;
            _skipPaginationSearch = false;
            Search();
        };

        public Action OnClickNextPage() => () =>
        {
            _skipPaginationSearch = true;
            var newPage = SelectedPageIndex + 1;
            if (newPage > PageNumbers.Count - 1)
                newPage = PageNumbers.Count - 1;

            SelectedPageIndex = newPage;
            _skipPaginationSearch = false;
            Search();
        };
    }
}