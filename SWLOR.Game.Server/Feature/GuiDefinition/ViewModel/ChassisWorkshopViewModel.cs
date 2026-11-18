using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Chassis Workshop. It follows the Appearance Editor: pictures to choose from, a swatch for each
    /// colour, the palette and the RGB picker for the swatch being painted, and the size on Settings.
    /// </summary>
    public class ChassisWorkshopViewModel : GuiViewModelBase<ChassisWorkshopViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "chassis_workshop_content";
        public const string ChassisPartial = "CHASSIS_WORKSHOP_CHASSIS";
        public const string SettingsPartial = "CHASSIS_WORKSHOP_SETTINGS";

        /// <summary>The window has a picture binding for this many chassis.</summary>
        public const int TileCount = 8;

        private const int ChassisTabId = 0;
        private const int SettingsTabId = 1;

        // A drag across the picker reports many colours; the last one is applied after this pause.
        private const float ApplyDelay = 0.1f;

        // Rule R4: tab registration and toggle sync are static and shared.
        private static readonly GuiTabGroup<ChassisWorkshopViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<ChassisWorkshopViewModel, GuiPayloadBase>()
                .AddTab(ChassisTabId, ChassisPartial)
                .AddTab(SettingsTabId, SettingsPartial);

        private static readonly GuiToggleGroupSync TabToggles = new(ChassisTabId, SettingsTabId);

        private static readonly string[] ColorBindings =
        {
            nameof(SelectedColor), nameof(CustomRed), nameof(CustomGreen), nameof(CustomBlue)
        };

        private readonly HashSet<string> _assigned = new();
        private bool _isListening;
        private bool _colorBindingsWatched;
        private AstromechPaintZone _zone;
        private AppearancePreviewView _view;
        private float[] _paletteOutline = Array.Empty<float>();
        // Two palette cells can hold one colour: the outline stays on the cell the player chose.
        private readonly Dictionary<AstromechPaintZone, int> _paletteChoice = new();

        // The picker reports a colour of its own when it is created, and a click's colour before its press.
        // Only a colour reported during a press on the picker is a choice (as in the Appearance Editor).
        private bool _pickerPressed;
        private GuiColor _unclaimedColor;
        private int _unclaimedGeneration;
        private TintMapColor? _pendingColor;
        private AstromechPaintZone _pendingZone;
        private bool _pendingFromPicker;
        private bool _flushScheduled;

        // Rule R4: this widget-bound property only forwards genuine clicks on a tab.
        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        public string Tile0Image { get => Get<string>(); set => Set(value); }
        public string Tile1Image { get => Get<string>(); set => Set(value); }
        public string Tile2Image { get => Get<string>(); set => Set(value); }
        public string Tile3Image { get => Get<string>(); set => Set(value); }
        public string Tile4Image { get => Get<string>(); set => Set(value); }
        public string Tile5Image { get => Get<string>(); set => Set(value); }
        public string Tile6Image { get => Get<string>(); set => Set(value); }
        public string Tile7Image { get => Get<string>(); set => Set(value); }
        public bool Tile0Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile1Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile2Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile3Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile4Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile5Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile6Selected { get => Get<bool>(); set => Set(value); }
        public bool Tile7Selected { get => Get<bool>(); set => Set(value); }

        public string PreviewImage { get => Get<string>(); set => Set(value); }
        public string PreviewLabel { get => Get<string>(); set => Set(value); }
        public string PreviewViewText { get => Get<string>(); set => Set(value); }

        public GuiColor PrimaryTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SecondaryTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor TrimTint { get => Get<GuiColor>(); set => Set(value); }
        public bool PrimarySelected { get => Get<bool>(); set => Set(value); }
        public bool SecondarySelected { get => Get<bool>(); set => Set(value); }
        public bool TrimSelected { get => Get<bool>(); set => Set(value); }

        public string ColorSheetResref { get => Get<string>(); set => Set(value); }
        public GuiBindingList<float> PaletteSelectionPoints { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public bool IsPaletteSelectionVisible { get => Get<bool>(); set => Set(value); }

        public GuiColor SelectedColor { get => Get<GuiColor>(); set => Set(value); }
        public string CustomRed { get => Get<string>(); set => Set(value); }
        public string CustomGreen { get => Get<string>(); set => Set(value); }
        public string CustomBlue { get => Get<string>(); set => Set(value); }

        public string ScaleText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isListening = false;
            _assigned.Clear();
            _colorBindingsWatched = false;
            _zone = AstromechPaintZone.Primary;
            _view = AppearancePreviewView.Front;
            _paletteOutline = Array.Empty<float>();
            _paletteChoice.Clear();
            CancelPickerPress();
            _pendingColor = null;
            _flushScheduled = false;

            if (!Astromech.IsAstromech(Player))
            {
                SendMessageToPC(Player, AstromechWorkshop.NotAstromechMessage);
                Gui.ClosePlayerWindow(Player, WindowType);
                return;
            }

            if (Space.IsPlayerInSpaceMode(Player))
            {
                SendMessageToPC(Player, AstromechWorkshop.InSpaceMessage);
                Gui.ClosePlayerWindow(Player, WindowType);
                return;
            }

            // Rule R3: every bound property is assigned before any of them is watched.
            PaletteSelectionPoints = new GuiBindingList<float>();
            LoadValues();
            TabToggles.SyncTo(ChassisTabId, value => TabToggleValue = value);

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.SelectedColor);
            WatchOnClient(model => model.CustomRed);
            WatchOnClient(model => model.CustomGreen);
            WatchOnClient(model => model.CustomBlue);
            _colorBindingsWatched = true;

            SelectTab(ChassisTabId);
            _isListening = true;
        }

        private void LoadValues()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            RefreshChassis(dbPlayer);
            RefreshPaint(dbPlayer);
            RefreshScale(dbPlayer);
        }

        private void SelectTab(int tabId)
        {
            if (tabId is not (ChassisTabId or SettingsTabId))
                return;

            FlushColor();
            CancelPickerPress();
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);
            // The tab's controls are new: send them their values, without taking the echo for a choice.
            Tabs.Select(this, ContentElement, tabId, () => WithoutColorWatches(RepublishBindings));
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (!_isListening)
                return;

            if (propertyName == nameof(SelectedColor))
                OnPickerColor(SelectedColor);
            else if (propertyName is nameof(CustomRed) or nameof(CustomGreen) or nameof(CustomBlue))
                OnRgbTyped();
        }

        private bool CanChange()
        {
            if (!Astromech.IsAstromech(Player))
            {
                SendMessageToPC(Player, AstromechWorkshop.NotAstromechMessage);
                return false;
            }

            if (Space.IsPlayerInSpaceMode(Player))
            {
                SendMessageToPC(Player, AstromechWorkshop.InSpaceMessage);
                return false;
            }

            return true;
        }

        /// <summary>Sends a value the first time and whenever it changes, so nothing is redrawn for no reason.</summary>
        private void SetIfChanged<TValue>(string propertyName, TValue value)
        {
            if (_assigned.Add(propertyName) || !EqualityComparer<TValue>.Default.Equals(Get<TValue>(propertyName), value))
                Set(value, propertyName);
        }

        // ------------------------------------------------------------------ chassis

        public Action OnClickTile(int tile) => () => Wear(tile);

        public Action OnClickPreviousChassis() => () => Wear(AstromechWorkshop.StepChassis(WornIndex(), -1));

        public Action OnClickNextChassis() => () => Wear(AstromechWorkshop.StepChassis(WornIndex(), 1));

        /// <summary>Turns every picture to the next view: front, back, angle.</summary>
        public Action OnClickView() => () =>
        {
            _view = AstromechWorkshop.NextView(_view);
            RefreshChassis(DB.Get<Player>(GetObjectUUID(Player)));
        };

        private int WornIndex()
        {
            var worn = Astromech.GetChassis(DB.Get<Player>(GetObjectUUID(Player)));
            for (var index = 0; index < AstromechChassisCatalog.All.Count; index++)
            {
                if (AstromechChassisCatalog.All[index].Appearance == worn.Appearance)
                    return index;
            }

            return 0;
        }

        private void Wear(int tile)
        {
            var chassis = AstromechWorkshop.GetChassisAt(tile);
            if (chassis == null || !CanChange())
                return;

            FlushColor();
            Astromech.SetChassis(Player, chassis.Appearance);

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            RefreshChassis(dbPlayer);
            RefreshPaint(dbPlayer);
        }

        private void RefreshChassis(Player dbPlayer)
        {
            var worn = Astromech.GetChassis(dbPlayer);
            for (var index = 0; index < Math.Min(TileCount, AstromechChassisCatalog.All.Count); index++)
            {
                var chassis = AstromechChassisCatalog.All[index];
                SetIfChanged($"Tile{index}Image", AppearancePreview.Picture(AstromechWorkshop.GetPictureKey(chassis), _view));
                SetIfChanged($"Tile{index}Selected", chassis.Appearance == worn.Appearance);
            }

            SetIfChanged(nameof(PreviewImage), AppearancePreview.Picture(AstromechWorkshop.GetPictureKey(worn), _view));
            SetIfChanged(nameof(PreviewLabel), worn.Name);
            SetIfChanged(nameof(PreviewViewText), _view.ToString());
        }

        // ------------------------------------------------------------------ paint

        /// <summary>Chooses the zone the palette and picker paint.</summary>
        public Action OnClickSwatch(AstromechPaintZone zone) => () =>
        {
            FlushColor();
            _zone = zone;
            RefreshPaint(DB.Get<Player>(GetObjectUUID(Player)));
        };

        /// <summary>A right-click returns the zone to the paint its chassis comes with.</summary>
        public Action OnMouseDownSwatch(AstromechPaintZone zone) => () =>
        {
            var button = (NuiMouseButton)JsonGetInt(JsonObjectGet(NuiGetEventPayload(), "mouse_btn"));
            if (button != NuiMouseButton.Right || !CanChange())
                return;

            if (_pendingColor != null && _pendingZone == zone)
                _pendingColor = null;
            FlushColor();
            Astromech.ResetPaint(Player, zone);
            RefreshPaint(DB.Get<Player>(GetObjectUUID(Player)));
        };

        public Action OnClickPalette(int paletteIndex) => () =>
        {
            if (paletteIndex < 0 || paletteIndex >= TintMapMaterialRegistry.PaletteColorCount || !CanChange())
                return;

            _pendingColor = null;
            _paletteChoice[_zone] = paletteIndex;
            Astromech.SetPaint(Player, _zone, AstromechWorkshop.GetPaletteColor(_zone, paletteIndex));
            RefreshPaint(DB.Get<Player>(GetObjectUUID(Player)));
        };

        public Action OnMouseDownPicker() => () =>
        {
            var button = (NuiMouseButton)JsonGetInt(JsonObjectGet(NuiGetEventPayload(), "mouse_btn"));
            var color = _unclaimedColor;
            _unclaimedColor = null;
            _unclaimedGeneration++;
            if (button != NuiMouseButton.Left)
                return;

            _pickerPressed = true;
            if (color != null)
                QueueColor(new TintMapColor(color.R, color.G, color.B), fromPicker: true);
        };

        public Action OnMouseUpPicker() => () =>
        {
            var button = (NuiMouseButton)JsonGetInt(JsonObjectGet(NuiGetEventPayload(), "mouse_btn"));
            if (button != NuiMouseButton.Left || !_pickerPressed)
                return;

            FlushColor();
            CancelPickerPress();
        };

        private void OnPickerColor(GuiColor color)
        {
            if (color == null)
                return;

            if (_pickerPressed)
            {
                QueueColor(new TintMapColor(color.R, color.G, color.B), fromPicker: true);
                return;
            }

            // A click's colour arrives before its press. Hold it briefly for that press and no longer.
            _unclaimedColor = color;
            var generation = ++_unclaimedGeneration;
            var windowToken = WindowToken;
            DelayCommand(ApplyDelay, () =>
            {
                if (generation == _unclaimedGeneration && windowToken == WindowToken)
                    _unclaimedColor = null;
            });
        }

        private void OnRgbTyped()
        {
            // As in the Appearance Editor, a box keeps digits only and nothing above 255.
            WithoutColorWatches(() =>
            {
                foreach (var name in new[] { nameof(CustomRed), nameof(CustomGreen), nameof(CustomBlue) })
                {
                    var typed = Get<string>(name);
                    var kept = AstromechWorkshop.NormalizeChannel(typed);
                    if (kept != typed)
                        Set(kept, name);
                }
            });

            if (AstromechWorkshop.TryParseRgb(CustomRed, CustomGreen, CustomBlue, out var color))
                QueueColor(color, fromPicker: false);
        }

        private void CancelPickerPress()
        {
            _pickerPressed = false;
            _unclaimedColor = null;
            _unclaimedGeneration++;
        }

        private void QueueColor(TintMapColor color, bool fromPicker)
        {
            if (!CanChange())
                return;

            _pendingZone = _zone;
            _pendingColor = color;
            _pendingFromPicker = fromPicker;
            if (_flushScheduled)
                return;

            _flushScheduled = true;
            var windowToken = WindowToken;
            DelayCommand(ApplyDelay, () =>
            {
                if (windowToken == WindowToken && Gui.IsWindowOpen(Player, WindowType))
                    FlushColor();
                else
                    _flushScheduled = false;
            });
        }

        private void FlushColor()
        {
            _flushScheduled = false;
            var pending = _pendingColor;
            _pendingColor = null;
            if (pending == null || !Astromech.IsAstromech(Player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            if (pending.Value == Astromech.GetPaint(dbPlayer, _pendingZone))
                return;

            Astromech.SetPaint(Player, _pendingZone, pending.Value);
            // The control the colour came from keeps what the player put in it.
            RefreshPaint(DB.Get<Player>(GetObjectUUID(Player)),
                keepPicker: _pendingFromPicker, keepFields: !_pendingFromPicker);
        }

        private void RefreshPaint(Player dbPlayer, bool keepPicker = false, bool keepFields = false)
        {
            SetIfChanged(nameof(PrimaryTint), ToGuiColor(Astromech.GetPaint(dbPlayer, AstromechPaintZone.Primary)));
            SetIfChanged(nameof(SecondaryTint), ToGuiColor(Astromech.GetPaint(dbPlayer, AstromechPaintZone.Secondary)));
            SetIfChanged(nameof(TrimTint), ToGuiColor(Astromech.GetPaint(dbPlayer, AstromechPaintZone.Trim)));
            SetIfChanged(nameof(PrimarySelected), _zone == AstromechPaintZone.Primary);
            SetIfChanged(nameof(SecondarySelected), _zone == AstromechPaintZone.Secondary);
            SetIfChanged(nameof(TrimSelected), _zone == AstromechPaintZone.Trim);
            SetIfChanged(nameof(ColorSheetResref), AstromechWorkshop.GetPaletteSheet(_zone));

            var paint = Astromech.GetPaint(dbPlayer, _zone);
            RefreshPaletteSelection(paint);
            WithoutColorWatches(() =>
            {
                if (!keepPicker)
                {
                    CancelPickerPress();
                    SetIfChanged(nameof(SelectedColor), ToGuiColor(paint));
                }

                if (!keepFields)
                {
                    SetIfChanged(nameof(CustomRed), paint.Red.ToString());
                    SetIfChanged(nameof(CustomGreen), paint.Green.ToString());
                    SetIfChanged(nameof(CustomBlue), paint.Blue.ToString());
                }
            });
        }

        /// <summary>Outlines the palette cell the paint is, as the Appearance Editor outlines its selection.</summary>
        private void RefreshPaletteSelection(TintMapColor paint)
        {
            var index = _paletteChoice.TryGetValue(_zone, out var chosen) &&
                        AstromechWorkshop.GetPaletteColor(_zone, chosen) == paint
                ? chosen
                : AstromechWorkshop.FindPaletteIndex(_zone, paint);
            var points = Array.Empty<float>();
            if (index >= 0)
            {
                var left = AppearanceEditorViewModel.PaletteHeaderWidth +
                           index % AstromechWorkshop.PaletteColumns * AppearanceEditorViewModel.PaletteCellWidth + 1f;
                var top = AppearanceEditorViewModel.PaletteHeaderHeight +
                          index / AstromechWorkshop.PaletteColumns * AppearanceEditorViewModel.PaletteCellHeight + 1f;
                var right = left + AppearanceEditorViewModel.PaletteCellWidth - 2f;
                var bottom = top + AppearanceEditorViewModel.PaletteCellHeight - 2f;
                points = new[] { left, top, right, top, right, bottom, left, bottom, left, top };
            }

            if (!points.SequenceEqual(_paletteOutline))
            {
                _paletteOutline = points;
                var list = new GuiBindingList<float>();
                foreach (var value in points)
                    list.Add(value);
                PaletteSelectionPoints = list;
            }

            SetIfChanged(nameof(IsPaletteSelectionVisible), points.Length > 0);
        }

        /// <summary>
        /// Runs an update of the picker and RGB boxes with their watches off, so the client's echo of a
        /// value the server set is not taken for the player's choice.
        /// </summary>
        private void WithoutColorWatches(Action update)
        {
            var restore = _colorBindingsWatched;
            if (restore)
                SetColorWatches(false);

            try
            {
                update();
            }
            finally
            {
                if (restore)
                    SetColorWatches(true);
            }
        }

        private void SetColorWatches(bool watched)
        {
            foreach (var binding in ColorBindings)
                NuiSetBindWatch(Player, WindowToken, binding, watched);
            _colorBindingsWatched = watched;
        }

        private static GuiColor ToGuiColor(TintMapColor color)
        {
            return new GuiColor(color.Red, color.Green, color.Blue);
        }

        // ------------------------------------------------------------------ size

        public Action OnClickSmaller() => () => StepScale(-1);

        public Action OnClickLarger() => () => StepScale(1);

        private void StepScale(int direction)
        {
            if (!CanChange())
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var current = AstromechWorkshop.RoundScale(dbPlayer?.AppearanceScale ?? 1.0f);
            var next = AstromechWorkshop.StepScale(current, direction);
            if (Math.Abs(next - current) < 0.0001f)
            {
                SendMessageToPC(Player, direction > 0 ? AstromechWorkshop.LargestMessage : AstromechWorkshop.SmallestMessage);
                return;
            }

            Astromech.SetScale(Player, next);
            RefreshScale(DB.Get<Player>(GetObjectUUID(Player)));
        }

        private void RefreshScale(Player dbPlayer)
        {
            SetIfChanged(nameof(ScaleText), $"Size: {AstromechWorkshop.FormatScale(dbPlayer?.AppearanceScale ?? 1.0f)}");
        }

        // ------------------------------------------------------------------ closing

        public override Action OnWindowClosed() => () =>
        {
            // Whatever the last drag or typed colour left is kept when the window closes before the pause ends.
            FlushColor();
        };
    }
}
