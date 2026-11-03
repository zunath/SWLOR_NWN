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
    /// through the editor, which then sends the new current part back. The grid fits as many
    /// columns and rows of pictures as the window holds.
    /// </summary>
    public class AppearancePartBrowserViewModel : GuiViewModelBase<AppearancePartBrowserViewModel, GuiPayloadBase>
    {
        public const string GridElement = "AE_PART_BROWSER_GRID";
        public const int MinColumns = 2;
        public const int MaxColumns = 8;
        public const int MaxRows = 5;
        public const int TileCount = MaxColumns * MaxRows;

        /// <summary>Grid partial for a size; narrow grids stack their pager on two rows.</summary>
        public static string Grid(int columns, int rows) => $"AE_PART_BROWSER_GRID_{columns}X{rows}";

        // Measured in the client at the default 620x590, which holds five columns and three rows.
        private const float TileColumnWidth = AppearanceEditorDefinition.TileSize + 14f;
        private const float TileRowHeight = AppearanceEditorDefinition.TileSize + 42f;
        private const float WindowWidthOverhead = 70f;
        private const float WindowHeightOverhead = 176f;
        /// <summary>The second pager row of a grid with <see cref="MinColumns"/> columns.</summary>
        private const float StackedPagerHeight = 40f;

        private int _columns = 5;
        private int _rows = 3;
        private int _resizeGeneration;
        private bool _compactPageLabels;

        private AppearanceEditorViewModel _editor;
        private List<int> _ids = new();
        private int _current = -1;
        private int _page;
        private Func<int, string> _key = _ => string.Empty;
        private Func<int, string> _label = id => id.ToString();
        private bool _frontOnly;
        private AppearancePreviewView _view = AppearancePreviewView.Front;

        private int PageSize => _columns * _rows;
        private int PageCount => Math.Max(1, (_ids.Count + PageSize - 1) / PageSize);

        /// <summary>Picture columns that fit a window this wide; a narrower window scrolls.</summary>
        internal static int ColumnsForWidth(float windowWidth) =>
            Math.Clamp((int)((windowWidth - WindowWidthOverhead) / TileColumnWidth), MinColumns, MaxColumns);

        /// <summary>Picture rows that fit a window this tall; a shorter window scrolls.</summary>
        internal static int RowsForHeight(float windowHeight, int columns)
        {
            var available = windowHeight - WindowHeightOverhead - (columns <= MinColumns ? StackedPagerHeight : 0f);
            return Math.Clamp((int)(available / TileRowHeight), 1, MaxRows);
        }

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
        public string Tile15Image { get => Get<string>(); set => Set(value); }
        public string Tile16Image { get => Get<string>(); set => Set(value); }
        public string Tile17Image { get => Get<string>(); set => Set(value); }
        public string Tile18Image { get => Get<string>(); set => Set(value); }
        public string Tile19Image { get => Get<string>(); set => Set(value); }
        public string Tile20Image { get => Get<string>(); set => Set(value); }
        public string Tile21Image { get => Get<string>(); set => Set(value); }
        public string Tile22Image { get => Get<string>(); set => Set(value); }
        public string Tile23Image { get => Get<string>(); set => Set(value); }
        public string Tile24Image { get => Get<string>(); set => Set(value); }
        public string Tile25Image { get => Get<string>(); set => Set(value); }
        public string Tile26Image { get => Get<string>(); set => Set(value); }
        public string Tile27Image { get => Get<string>(); set => Set(value); }
        public string Tile28Image { get => Get<string>(); set => Set(value); }
        public string Tile29Image { get => Get<string>(); set => Set(value); }
        public string Tile30Image { get => Get<string>(); set => Set(value); }
        public string Tile31Image { get => Get<string>(); set => Set(value); }
        public string Tile32Image { get => Get<string>(); set => Set(value); }
        public string Tile33Image { get => Get<string>(); set => Set(value); }
        public string Tile34Image { get => Get<string>(); set => Set(value); }
        public string Tile35Image { get => Get<string>(); set => Set(value); }
        public string Tile36Image { get => Get<string>(); set => Set(value); }
        public string Tile37Image { get => Get<string>(); set => Set(value); }
        public string Tile38Image { get => Get<string>(); set => Set(value); }
        public string Tile39Image { get => Get<string>(); set => Set(value); }
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
        public string Tile15Label { get => Get<string>(); set => Set(value); }
        public string Tile16Label { get => Get<string>(); set => Set(value); }
        public string Tile17Label { get => Get<string>(); set => Set(value); }
        public string Tile18Label { get => Get<string>(); set => Set(value); }
        public string Tile19Label { get => Get<string>(); set => Set(value); }
        public string Tile20Label { get => Get<string>(); set => Set(value); }
        public string Tile21Label { get => Get<string>(); set => Set(value); }
        public string Tile22Label { get => Get<string>(); set => Set(value); }
        public string Tile23Label { get => Get<string>(); set => Set(value); }
        public string Tile24Label { get => Get<string>(); set => Set(value); }
        public string Tile25Label { get => Get<string>(); set => Set(value); }
        public string Tile26Label { get => Get<string>(); set => Set(value); }
        public string Tile27Label { get => Get<string>(); set => Set(value); }
        public string Tile28Label { get => Get<string>(); set => Set(value); }
        public string Tile29Label { get => Get<string>(); set => Set(value); }
        public string Tile30Label { get => Get<string>(); set => Set(value); }
        public string Tile31Label { get => Get<string>(); set => Set(value); }
        public string Tile32Label { get => Get<string>(); set => Set(value); }
        public string Tile33Label { get => Get<string>(); set => Set(value); }
        public string Tile34Label { get => Get<string>(); set => Set(value); }
        public string Tile35Label { get => Get<string>(); set => Set(value); }
        public string Tile36Label { get => Get<string>(); set => Set(value); }
        public string Tile37Label { get => Get<string>(); set => Set(value); }
        public string Tile38Label { get => Get<string>(); set => Set(value); }
        public string Tile39Label { get => Get<string>(); set => Set(value); }

        public string Tile0Number { get => Get<string>(); set => Set(value); }
        public string Tile1Number { get => Get<string>(); set => Set(value); }
        public string Tile2Number { get => Get<string>(); set => Set(value); }
        public string Tile3Number { get => Get<string>(); set => Set(value); }
        public string Tile4Number { get => Get<string>(); set => Set(value); }
        public string Tile5Number { get => Get<string>(); set => Set(value); }
        public string Tile6Number { get => Get<string>(); set => Set(value); }
        public string Tile7Number { get => Get<string>(); set => Set(value); }
        public string Tile8Number { get => Get<string>(); set => Set(value); }
        public string Tile9Number { get => Get<string>(); set => Set(value); }
        public string Tile10Number { get => Get<string>(); set => Set(value); }
        public string Tile11Number { get => Get<string>(); set => Set(value); }
        public string Tile12Number { get => Get<string>(); set => Set(value); }
        public string Tile13Number { get => Get<string>(); set => Set(value); }
        public string Tile14Number { get => Get<string>(); set => Set(value); }
        public string Tile15Number { get => Get<string>(); set => Set(value); }
        public string Tile16Number { get => Get<string>(); set => Set(value); }
        public string Tile17Number { get => Get<string>(); set => Set(value); }
        public string Tile18Number { get => Get<string>(); set => Set(value); }
        public string Tile19Number { get => Get<string>(); set => Set(value); }
        public string Tile20Number { get => Get<string>(); set => Set(value); }
        public string Tile21Number { get => Get<string>(); set => Set(value); }
        public string Tile22Number { get => Get<string>(); set => Set(value); }
        public string Tile23Number { get => Get<string>(); set => Set(value); }
        public string Tile24Number { get => Get<string>(); set => Set(value); }
        public string Tile25Number { get => Get<string>(); set => Set(value); }
        public string Tile26Number { get => Get<string>(); set => Set(value); }
        public string Tile27Number { get => Get<string>(); set => Set(value); }
        public string Tile28Number { get => Get<string>(); set => Set(value); }
        public string Tile29Number { get => Get<string>(); set => Set(value); }
        public string Tile30Number { get => Get<string>(); set => Set(value); }
        public string Tile31Number { get => Get<string>(); set => Set(value); }
        public string Tile32Number { get => Get<string>(); set => Set(value); }
        public string Tile33Number { get => Get<string>(); set => Set(value); }
        public string Tile34Number { get => Get<string>(); set => Set(value); }
        public string Tile35Number { get => Get<string>(); set => Set(value); }
        public string Tile36Number { get => Get<string>(); set => Set(value); }
        public string Tile37Number { get => Get<string>(); set => Set(value); }
        public string Tile38Number { get => Get<string>(); set => Set(value); }
        public string Tile39Number { get => Get<string>(); set => Set(value); }
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
        public bool Tile15Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile16Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile17Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile18Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile19Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile20Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile21Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile22Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile23Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile24Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile25Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile26Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile27Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile28Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile29Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile30Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile31Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile32Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile33Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile34Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile35Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile36Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile37Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile38Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile39Selected { get => Get<bool>(); set => Set(value); }
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
        public bool Tile15Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile16Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile17Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile18Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile19Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile20Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile21Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile22Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile23Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile24Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile25Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile26Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile27Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile28Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile29Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile30Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile31Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile32Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile33Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile34Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile35Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile36Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile37Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile38Visible { get => Get<bool>(); set => Set(value); }
        public bool Tile39Visible { get => Get<bool>(); set => Set(value); }

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
            for (var tile = 0; tile < TileCount; tile++)
            {
                Set(string.Empty, $"Tile{tile}Image");
                Set(string.Empty, $"Tile{tile}Label");
                Set(string.Empty, $"Tile{tile}Number");
                Set(false, $"Tile{tile}Selected");
                Set(false, $"Tile{tile}Visible");
            }

            WatchOnClient(model => model.PageIndex);
            WatchOnClient(model => model.PartNumberText);
            FitGrid(force: true);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName != nameof(Geometry))
                return;
            // Rebuild once the drag settles, not on every step of it.
            var generation = ++_resizeGeneration;
            DelayCommand(0.3f, () =>
            {
                if (generation == _resizeGeneration)
                    FitGrid();
            });
        }

        /// <summary>
        /// Shows the grid that fits the window. The page keeps the worn part in view, or else the
        /// first picture that was shown.
        /// </summary>
        internal void FitGrid(bool force = false)
        {
            if (Geometry == null)
                return;
            var columns = ColumnsForWidth(Geometry.Width);
            var rows = RowsForHeight(Geometry.Height, columns);
            if (!force && columns == _columns && rows == _rows)
                return;

            var firstShown = _page * PageSize;
            var currentIndex = _ids.IndexOf(_current);
            var currentShown = currentIndex >= firstShown && currentIndex < firstShown + PageSize;
            _columns = columns;
            _rows = rows;
            _page = (currentShown ? currentIndex : firstShown) / PageSize;
            ChangePartialView(GridElement, Grid(columns, rows));
            Publish();

            // A layout swap rebuilds the controls, and input values are not re-sent (NuiLayoutRules R7).
            OnPropertyChanged(nameof(IsFrontViewSelected));
            OnPropertyChanged(nameof(IsBackViewSelected));
            OnPropertyChanged(nameof(IsAngleViewSelected));
            OnPropertyChanged(nameof(PageIndex));
            OnPropertyChanged(nameof(PartNumberText));
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
            for (var tile = 0; tile < TileCount; tile++)
            {
                var position = _page * PageSize + tile;
                var visible = tile < PageSize && position < _ids.Count;
                var id = visible ? _ids[position] : -1;
                SetIfChanged($"Tile{tile}Visible", visible);
                SetIfChanged($"Tile{tile}Image", visible ? AppearancePreview.Picture(_key(id), _view) : string.Empty);
                SetIfChanged($"Tile{tile}Label", visible ? _label(id) : string.Empty);
                SetIfChanged($"Tile{tile}Number", visible && id > 0 ? id.ToString() : string.Empty);
                SetIfChanged($"Tile{tile}Selected", visible && id == _current);
            }

            SetIfChanged(nameof(IsPreviewViewChoiceVisible), !_frontOnly);
            SetIfChanged(nameof(IsFrontViewSelected), _view == AppearancePreviewView.Front);
            SetIfChanged(nameof(IsBackViewSelected), _view == AppearancePreviewView.Back);
            SetIfChanged(nameof(IsAngleViewSelected), _view == AppearancePreviewView.Angle);

            var pages = PageCount;
            var compact = _columns <= MinColumns;
            if (PageOptions == null || PageOptions.Count != pages || compact != _compactPageLabels)
            {
                // A narrow grid has a narrow page box: "3 / 20" instead of "Page 3 of 20".
                _compactPageLabels = compact;
                var options = new GuiBindingList<GuiComboEntry>();
                for (var page = 0; page < pages; page++)
                    options.Add(new GuiComboEntry(compact ? $"{page + 1} / {pages}" : $"Page {page + 1} of {pages}", page));
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
