using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.DisguiseWindow
{
    /// <summary>
    /// The disguise window's sound-set picker: the sound sets split into pages of 25, the page and entry the player has selected,
    /// and the preview. It owns the paging state and writes the picker bindings through the host.
    /// </summary>
    public class DisguiseSoundSetPicker
    {
        private const int SoundSetPageSize = 25;

        private readonly IDisguiseHost _host;
        private ICacheService Cache { get; }

        private readonly List<int> _soundSetIds = new();
        private readonly Dictionary<int, int> _soundSetIndexesById = new();
        private readonly List<GuiBindingList<GuiComboEntry>> _soundSetOptionPages = new();
        private bool _suppressSoundSetPageChange;
        private int _soundSetPageIndex;
        private int _selectedSoundSetId = -1;

        public DisguiseSoundSetPicker(IDisguiseHost host, ICacheService cache)
        {
            _host = host;
            Cache = cache;
        }

        /// <summary>The sound set the player has selected, or -1.</summary>
        public int SelectedSoundSetId => _selectedSoundSetId;

        /// <summary>True while the picker moves the page selector itself, so the page setter does not reload the entries.</summary>
        public bool IsPageChangeSuppressed => _suppressSoundSetPageChange;

        /// <summary>The page whose entries the picker shows.</summary>
        public int SoundSetPageIndex
        {
            get => _soundSetPageIndex;
            set => _soundSetPageIndex = value;
        }

        public void LoadSoundSetOptions()
        {
            _soundSetIds.Clear();
            _soundSetIndexesById.Clear();
            _soundSetOptionPages.Clear();

            var currentPage = new GuiBindingList<GuiComboEntry>();
            var optionIndex = 0;

            foreach (var (soundSetId, label) in Cache.GetSoundSets())
            {
                var absoluteIndex = _soundSetIds.Count;
                _soundSetIds.Add(soundSetId);
                _soundSetIndexesById[soundSetId] = absoluteIndex;

                if (currentPage.Count >= SoundSetPageSize)
                {
                    _soundSetOptionPages.Add(currentPage);
                    currentPage = new GuiBindingList<GuiComboEntry>();
                    optionIndex = 0;
                }

                currentPage.Add(new GuiComboEntry(label, optionIndex));
                optionIndex++;
            }

            _soundSetOptionPages.Add(currentPage);
            _soundSetPageIndex = 0;
            LoadSoundSetPageOptions(0);
        }

        public void SelectSoundSet(int soundSetId)
        {
            if (!_soundSetIndexesById.TryGetValue(soundSetId, out var absoluteIndex))
                absoluteIndex = _soundSetIds.Count > 0 ? 0 : -1;

            if (absoluteIndex < 0)
            {
                _selectedSoundSetId = -1;
                _soundSetPageIndex = 0;
                LoadSoundSetPageOptions(-1);
                return;
            }

            _selectedSoundSetId = _soundSetIds[absoluteIndex];
            _soundSetPageIndex = absoluteIndex / SoundSetPageSize;
            LoadSoundSetPageOptions(absoluteIndex % SoundSetPageSize);
        }

        public int GetDefaultSoundSetId()
        {
            return _soundSetIds.Count > 0
                ? _soundSetIds[0]
                : -1;
        }

        public int SanitizeSoundSetIndex(int index)
        {
            if (index < 0)
                return -1;

            var pageCount = GetCurrentSoundSetPageSize();
            if (pageCount == 0)
                return -1;

            return Math.Clamp(index, 0, pageCount - 1);
        }

        public int SanitizeSoundSetPageIndex(int index)
        {
            return Math.Clamp(index, 0, GetSoundSetPageCount() - 1);
        }

        private int GetCurrentSoundSetPageSize()
        {
            if (_soundSetPageIndex < 0 || _soundSetPageIndex >= _soundSetOptionPages.Count)
                return 0;

            return _soundSetOptionPages[_soundSetPageIndex].Count;
        }

        private int GetSoundSetPageCount()
        {
            return Math.Max(1, _soundSetOptionPages.Count);
        }

        private void LoadSoundSetPageNumbers()
        {
            var pageNumbers = new GuiBindingList<GuiComboEntry>();

            for (var pageIndex = 0; pageIndex < GetSoundSetPageCount(); pageIndex++)
            {
                pageNumbers.Add(new GuiComboEntry($"Page {pageIndex + 1}", pageIndex));
            }

            _host.SoundSetPageNumbers = pageNumbers;
        }

        public void LoadSoundSetPageOptions(int selectedPageIndex)
        {
            LoadSoundSetPageOptions(selectedPageIndex, false);
        }

        public void LoadSoundSetPageOptions(int selectedPageIndex, bool selectFirstWhenInvalid)
        {
            _soundSetPageIndex = Math.Clamp(_soundSetPageIndex, 0, GetSoundSetPageCount() - 1);
            LoadSoundSetPageNumbers();

            _host.SoundSetOptions = _soundSetOptionPages[_soundSetPageIndex];
            _suppressSoundSetPageChange = true;
            _host.SelectedSoundSetPageIndex = _soundSetPageIndex;
            _suppressSoundSetPageChange = false;

            if (selectFirstWhenInvalid &&
                (selectedPageIndex < 0 || selectedPageIndex >= GetCurrentSoundSetPageSize()))
            {
                selectedPageIndex = GetCurrentSoundSetPageSize() > 0 ? 0 : -1;
            }

            _host.SelectedSoundSetIndex = selectedPageIndex;
        }

        public int GetSelectedSoundSetIndexOnCurrentPage()
        {
            if (!_soundSetIndexesById.TryGetValue(_selectedSoundSetId, out var absoluteIndex))
                return -1;

            if (absoluteIndex / SoundSetPageSize != _soundSetPageIndex)
                return -1;

            return absoluteIndex % SoundSetPageSize;
        }

        public void SetSelectedSoundSetFromPageIndex(int pageIndex)
        {
            var absoluteIndex = _soundSetPageIndex * SoundSetPageSize + pageIndex;
            if (pageIndex < 0 || absoluteIndex < 0 || absoluteIndex >= _soundSetIds.Count)
            {
                _selectedSoundSetId = -1;
                _host.SoundSetName = string.Empty;
                return;
            }

            _selectedSoundSetId = _soundSetIds[absoluteIndex];
            _host.SoundSetName = _soundSetOptionPages[_soundSetPageIndex][pageIndex].Label;
        }

        public void RefreshSoundSetBindings()
        {
            if (_selectedSoundSetId < 0)
            {
                LoadSoundSetPageOptions(-1);
                return;
            }

            if (!_soundSetIndexesById.TryGetValue(_selectedSoundSetId, out var absoluteIndex))
            {
                SelectSoundSet(GetDefaultSoundSetId());
                return;
            }

            _soundSetPageIndex = absoluteIndex / SoundSetPageSize;
            LoadSoundSetPageOptions(absoluteIndex % SoundSetPageSize);
        }

        /// <summary>Plays the preview sound of the selected sound set to the player.</summary>
        public void PreviewSelectedSoundSet(uint player)
        {
            if (_selectedSoundSetId < 0)
                return;

            var previewSoundResref = Cache.GetSoundSetPreviewSoundResref(_selectedSoundSetId);
            if (string.IsNullOrWhiteSpace(previewSoundResref))
            {
                SendMessageToPC(player, ColorToken.Red("No preview sound is available for this sound set."));
                return;
            }

            PlayerPlugin.PlaySound(player, previewSoundResref, OBJECT_INVALID);
        }
    }
}
