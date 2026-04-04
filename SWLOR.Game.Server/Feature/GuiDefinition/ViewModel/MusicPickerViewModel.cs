using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class MusicPickerViewModel : GuiViewModelBase<MusicPickerViewModel, GuiPayloadBase>
    {
        private readonly List<int> _songIds = new List<int>();
        private readonly List<string> _songDisplayNames = new List<string>();

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> SongNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            Search();

            WatchOnClient(model => model.SearchText);
        }

        private void Search()
        {
            var searchText = SearchText ?? string.Empty;

            _songIds.Clear();
            _songDisplayNames.Clear();

            var songs = new GuiBindingList<string>();
            foreach (var song in Music.GetAllSongs().OrderBy(x => x.DisplayName))
            {
                if (!string.IsNullOrWhiteSpace(searchText) &&
                    !song.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _songIds.Add(song.ID);
                _songDisplayNames.Add(song.DisplayName);
                songs.Add($"{song.ID}: {song.DisplayName}");
            }

            SongNames = songs;
        }

        public Action OnClickSearch() => Search;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            Search();
        };

        public Action OnSelectSong() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _songIds.Count)
            {
                return;
            }

            var area = GetArea(Player);
            if (!GetIsObjectValid(area))
            {
                return;
            }

            var songId = _songIds[index];
            var songName = _songDisplayNames[index];

            MusicBackgroundChangeDay(area, songId);
            MusicBackgroundChangeNight(area, songId);
            MusicBackgroundPlay(area);

            FloatingTextStringOnCreature($"Song Selected: {songName}", Player, false);
        };
    }
}
