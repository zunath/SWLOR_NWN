using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Pages of part pictures for the Appearance Editor's selected slot. The editor sends the parts
    /// with <see cref="ShowParts"/> whenever its slot or part changes; a picked picture is worn
    /// through the editor, which then sends the new current part back.
    /// </summary>
    public class AppearancePartBrowserViewModel : GuiViewModelBase<AppearancePartBrowserViewModel, GuiPayloadBase>
    {
        public const int Columns = 5;
        public const int Rows = 3;
        public const int PageSize = Columns * Rows;

        private AppearanceEditorViewModel _editor;
        private List<int> _ids = new();
        private int _current = -1;
        private int _page;
        private Func<int, string> _key = _ => string.Empty;
        private Func<int, string> _label = id => id.ToString();
        private bool _frontOnly;
        private AppearancePreviewView _view = AppearancePreviewView.Front;

        private int PageCount => Math.Max(1, (_ids.Count + PageSize - 1) / PageSize);

        public bool IsPreviewViewChoiceVisible { get => Get<bool>(); set => Set(value); }
        public bool IsFrontViewSelected { get => Get<bool>(); set => Set(value); }
        public bool IsBackViewSelected { get => Get<bool>(); set => Set(value); }
        public bool IsAngleViewSelected { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> PageOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }

        public int PageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (value != _page && value >= 0 && value < PageCount)
                {
                    _page = value;
                    Publish();
                }
            }
        }

        public string PartNumberText { get => Get<string>(); set => Set(value); }

        public string Tile0Image { get => Get<string>(); set => Set(value); }
        public string Tile1Image { get => Get<string>(); set => Set(value); }
        public string Tile2Image { get => Get<string>(); set => Set(value); }
        public string Tile3Image { get => Get<string>(); set => Set(value); }
        public string Tile4Image { get => Get<string>(); set => Set(value); }
        public string Tile5Image { get => Get<string>(); set => Set(value); }
        public string Tile6Image { get => Get<string>(); set => Set(value); }
        public string Tile7Image { get => Get<string>(); set => Set(value); }
        public string Tile8Image { get => Get<string>(); set => Set(value); }
        public string Tile9Image { get => Get<string>(); set => Set(value); }
        public string Tile10Image { get => Get<string>(); set => Set(value); }
        public string Tile11Image { get => Get<string>(); set => Set(value); }
        public string Tile12Image { get => Get<string>(); set => Set(value); }
        public string Tile13Image { get => Get<string>(); set => Set(value); }
        public string Tile14Image { get => Get<string>(); set => Set(value); }
        public string Tile0Label { get => Get<string>(); set => Set(value); }
        public string Tile1Label { get => Get<string>(); set => Set(value); }
        public string Tile2Label { get => Get<string>(); set => Set(value); }
        public string Tile3Label { get => Get<string>(); set => Set(value); }
        public string Tile4Label { get => Get<string>(); set => Set(value); }
        public string Tile5Label { get => Get<string>(); set => Set(value); }
        public string Tile6Label { get => Get<string>(); set => Set(value); }
        public string Tile7Label { get => Get<string>(); set => Set(value); }
        public string Tile8Label { get => Get<string>(); set => Set(value); }
        public string Tile9Label { get => Get<string>(); set => Set(value); }
        public string Tile10Label { get => Get<string>(); set => Set(value); }
        public string Tile11Label { get => Get<string>(); set => Set(value); }
        public string Tile12Label { get => Get<string>(); set => Set(value); }
        public string Tile13Label { get => Get<string>(); set => Set(value); }
        public string Tile14Label { get => Get<string>(); set => Set(value); }
        public bool Tile0Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile1Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile2Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile3Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile4Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile5Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile6Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile7Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile8Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile9Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile10Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile11Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile12Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile13Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile14Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile0Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile1Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile2Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile3Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile4Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile5Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile6Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile7Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile8Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile9Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile10Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile11Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile12Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile13Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile14Visible { get => Get<bool>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            // Every bound value needs an entry before it is watched or re-sent.
            _editor = null;
            _ids = new List<int>();
            _current = -1;
            _page = 0;
            IsPreviewViewChoiceVisible = true;
            IsFrontViewSelected = true;
            IsBackViewSelected = false;
            IsAngleViewSelected = false;
            PageOptions = new GuiBindingList<GuiComboEntry> { new("Page 1 of 1", 0) };
            PageIndex = 0;
            PartNumberText = string.Empty;
            for (var tile = 0; tile < PageSize; tile++)
            {
                Set(string.Empty, $"Tile{tile}Image");
                Set(string.Empty, $"Tile{tile}Label");
                Set(false, $"Tile{tile}Selected");
                Set(false, $"Tile{tile}Visible");
            }

            WatchOnClient(model => model.PageIndex);
            WatchOnClient(model => model.PartNumberText);
        }

        /// <summary>
        /// Shows the editor's parts. The page follows the current part when it changes or moves
        /// off the page shown; paging alone never moves it.
        /// </summary>
        internal void ShowParts(AppearanceEditorViewModel editor, IReadOnlyList<int> ids, int current,
            Func<int, string> key, Func<int, string> label, bool frontOnly, AppearancePreviewView view)
        {
            var listChanged = !_ids.SequenceEqual(ids);
            var currentChanged = current != _current;
            _editor = editor;
            _ids = ids.ToList();
            _current = current;
            _key = key ?? (_ => string.Empty);
            _label = label ?? (id => id.ToString());
            _frontOnly = frontOnly;
            _view = frontOnly ? AppearancePreviewView.Front : view;
            var index = _ids.IndexOf(current);
            if (listChanged || (currentChanged && index >= 0 && index / PageSize != _page))
                _page = index >= 0 ? index / PageSize : 0;
            Publish();
        }

        private void Publish()
        {
            _page = Math.Clamp(_page, 0, PageCount - 1);
            for (var tile = 0; tile < PageSize; tile++)
            {
                var position = _page * PageSize + tile;
                var visible = position < _ids.Count;
                var id = visible ? _ids[position] : -1;
                SetIfChanged($"Tile{tile}Visible", visible);
                SetIfChanged($"Tile{tile}Image", visible ? AppearancePreview.Picture(_key(id), _view) : string.Empty);
                SetIfChanged($"Tile{tile}Label", visible ? _label(id) : string.Empty);
                SetIfChanged($"Tile{tile}Selected", visible && id == _current);
            }

            SetIfChanged(nameof(IsPreviewViewChoiceVisible), !_frontOnly);
            SetIfChanged(nameof(IsFrontViewSelected), _view == AppearancePreviewView.Front);
            SetIfChanged(nameof(IsBackViewSelected), _view == AppearancePreviewView.Back);
            SetIfChanged(nameof(IsAngleViewSelected), _view == AppearancePreviewView.Angle);

            var pages = PageCount;
            if (PageOptions == null || PageOptions.Count != pages)
            {
                var options = new GuiBindingList<GuiComboEntry>();
                for (var page = 0; page < pages; page++)
                    options.Add(new GuiComboEntry($"Page {page + 1} of {pages}", page));
                PageOptions = options;
            }
            SetIfChanged(nameof(PageIndex), _page);
        }

        private void SetIfChanged<TValue>(string propertyName, TValue value)
        {
            if (!EqualityComparer<TValue>.Default.Equals(Get<TValue>(propertyName), value))
                Set(value, propertyName);
        }

        public Action OnClickView(AppearancePreviewView view) => () =>
        {
            // A toggle button flips itself on click; send every button's state back.
            if (_editor != null && !_frontOnly)
                _editor.SelectPreviewView(view);
            OnPropertyChanged(nameof(IsFrontViewSelected));
            OnPropertyChanged(nameof(IsBackViewSelected));
            OnPropertyChanged(nameof(IsAngleViewSelected));
        };

        public Action OnClickTile(int tile) => () =>
        {
            var position = _page * PageSize + tile;
            if (tile < 0 || tile >= PageSize || position >= _ids.Count)
                return;
            _editor?.ApplyBrowserPart(_ids[position]);
        };

        public Action OnClickPrevious() => () =>
        {
            if (_page <= 0)
                return;
            _page--;
            Publish();
        };

        public Action OnClickNext() => () =>
        {
            if (_page >= PageCount - 1)
                return;
            _page++;
            Publish();
        };

        public Action OnClickGo() => () =>
        {
            if (!AppearanceEditorViewModel.TryParsePartNumber(PartNumberText, out var partId) || !_ids.Contains(partId))
            {
                SendMessageToPC(Player, ColorToken.Red($"Part #{PartNumberText?.Trim()} is not in this list."));
                return;
            }
            _editor?.ApplyBrowserPart(partId);
        };
    }
}
