using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ChassisWorkshopViewModel : GuiViewModelBase<ChassisWorkshopViewModel, GuiPayloadBase>
    {
        public const string ContentElement = "chassis_workshop_content";
        public const string MainPartial = "CHASSIS_WORKSHOP_MAIN";

        private const string PickText = "Pick";
        private const string DoneText = "Done";
        private const string WornText = "Worn";
        private const string WearText = "Wear";

        // A drag of the slider or the picker reports many values; the last one is applied after this pause.
        private const float ApplyDelay = 0.25f;

        // The client drops a value that reaches it before the control showing it, and a slider left without a
        // value reports its minimum. So changes are only listened for from the tick after the values are sent
        // again, the way the Hotbars window does it.
        private bool _isListening;

        private AstromechPaintZone? _activeZone;
        private AstromechPaintZone _pendingZone;
        private GuiColor _pendingColor;
        private bool _colorFlushScheduled;
        private float _pendingScale;
        private float _appliedScale;
        private bool _hasPendingScale;
        private bool _scaleFlushScheduled;

        public GuiBindingList<string> ChassisNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ChassisButtonText { get => Get<GuiBindingList<string>>(); set => Set(value); }

        public GuiColor PrimaryTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SecondaryTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor TrimTint { get => Get<GuiColor>(); set => Set(value); }
        public string PrimaryPickText { get => Get<string>(); set => Set(value); }
        public string SecondaryPickText { get => Get<string>(); set => Set(value); }
        public string TrimPickText { get => Get<string>(); set => Set(value); }

        public bool IsPickerVisible { get => Get<bool>(); set => Set(value); }
        public string PickerLabel { get => Get<string>(); set => Set(value); }
        public GuiColor SelectedColor { get => Get<GuiColor>(); set => Set(value); }

        public float ScaleValue { get => Get<float>(); set => Set(value); }
        public string ScaleText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isListening = false;
            _activeZone = null;
            _pendingColor = null;
            _colorFlushScheduled = false;
            _hasPendingScale = false;
            _scaleFlushScheduled = false;

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

            LoadValues();
            ChangePartialView(ContentElement, MainPartial);

            var windowToken = WindowToken;
            DelayCommand(0f, () => Listen(windowToken));
        }

        private void LoadValues()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));

            var names = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            foreach (var chassis in AstromechChassisCatalog.All)
            {
                names.Add(chassis.Name);
                buttons.Add(WearText);
            }

            ChassisNames = names;
            ChassisButtonText = buttons;

            RefreshChassis(dbPlayer);
            RefreshPaint(dbPlayer);

            IsPickerVisible = _activeZone != null;
            PickerLabel = _activeZone == null ? string.Empty : $"Colour: {GetZoneName(_activeZone.Value)}";
            SelectedColor = _activeZone == null
                ? GuiColor.Grey
                : ToGuiColor(Astromech.GetPaint(dbPlayer, _activeZone.Value));

            _appliedScale = AstromechWorkshop.RoundScale(dbPlayer?.AppearanceScale ?? 1.0f);
            ScaleValue = _appliedScale;
            ScaleText = AstromechWorkshop.FormatScale(_appliedScale);
        }

        private void Listen(int windowToken)
        {
            if (WindowToken != windowToken || !Gui.IsWindowOpen(Player, WindowType))
                return;

            // Watching sends each value again, now that its control exists.
            LoadValues();
            WatchOnClient(model => model.ScaleValue);
            WatchOnClient(model => model.SelectedColor);
            _isListening = true;
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (!_isListening)
                return;

            if (propertyName == nameof(ScaleValue))
            {
                ScaleText = AstromechWorkshop.FormatScale(ScaleValue);
                QueueScale(ScaleValue);
            }
            else if (propertyName == nameof(SelectedColor) && _activeZone != null)
            {
                QueueColor(_activeZone.Value, SelectedColor);
            }
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

        // ------------------------------------------------------------------ chassis

        public Action OnClickChassis() => () =>
        {
            var chassis = AstromechWorkshop.GetChassisAt(NuiGetEventArrayIndex());
            if (chassis == null || !CanChange())
                return;

            FlushColor();
            Astromech.SetChassis(Player, chassis.Appearance);

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            RefreshChassis(dbPlayer);
            RefreshPaint(dbPlayer);
            if (_activeZone != null)
                SetPickerColor(ToGuiColor(Astromech.GetPaint(dbPlayer, _activeZone.Value)));
        };

        private void RefreshChassis(Player dbPlayer)
        {
            var worn = Astromech.GetChassis(dbPlayer);
            for (var index = 0; index < AstromechChassisCatalog.All.Count; index++)
            {
                var text = AstromechChassisCatalog.All[index].Appearance == worn.Appearance ? WornText : WearText;
                if (ChassisButtonText[index] != text)
                    ChassisButtonText[index] = text;
            }
        }

        // ------------------------------------------------------------------ paint

        public Action OnClickPick(AstromechPaintZone zone) => () =>
        {
            if (!CanChange())
                return;

            FlushColor();
            if (_activeZone == zone)
            {
                _activeZone = null;
                IsPickerVisible = false;
                PickerLabel = string.Empty;
            }
            else
            {
                _activeZone = zone;
                PickerLabel = $"Colour: {GetZoneName(zone)}";
                SetPickerColor(ToGuiColor(Astromech.GetPaint(DB.Get<Player>(GetObjectUUID(Player)), zone)));
                IsPickerVisible = true;
            }

            RefreshPickText();
        };

        public Action OnClickReset(AstromechPaintZone zone) => () =>
        {
            if (!CanChange())
                return;

            if (_pendingColor != null && _pendingZone == zone)
                _pendingColor = null;
            FlushColor();
            Astromech.ResetPaint(Player, zone);

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            RefreshPaint(dbPlayer);
            if (_activeZone == zone)
                SetPickerColor(ToGuiColor(Astromech.GetPaint(dbPlayer, zone)));
        };

        private void RefreshPaint(Player dbPlayer)
        {
            PrimaryTint = ToGuiColor(Astromech.GetPaint(dbPlayer, AstromechPaintZone.Primary));
            SecondaryTint = ToGuiColor(Astromech.GetPaint(dbPlayer, AstromechPaintZone.Secondary));
            TrimTint = ToGuiColor(Astromech.GetPaint(dbPlayer, AstromechPaintZone.Trim));
            RefreshPickText();
        }

        private void RefreshPickText()
        {
            PrimaryPickText = _activeZone == AstromechPaintZone.Primary ? DoneText : PickText;
            SecondaryPickText = _activeZone == AstromechPaintZone.Secondary ? DoneText : PickText;
            TrimPickText = _activeZone == AstromechPaintZone.Trim ? DoneText : PickText;
        }

        private void SetPickerColor(GuiColor color)
        {
            // A color the player did not pick is never applied: a report equal to the stored paint is skipped.
            _pendingColor = null;
            SelectedColor = color;
        }

        private void QueueColor(AstromechPaintZone zone, GuiColor color)
        {
            if (color == null || !CanChange())
                return;

            _pendingZone = zone;
            _pendingColor = color;
            if (_colorFlushScheduled)
                return;

            _colorFlushScheduled = true;
            var windowToken = WindowToken;
            DelayCommand(ApplyDelay, () =>
            {
                if (windowToken == WindowToken && Gui.IsWindowOpen(Player, WindowType))
                    FlushColor();
                else
                    _colorFlushScheduled = false;
            });
        }

        private void FlushColor()
        {
            _colorFlushScheduled = false;
            var color = _pendingColor;
            _pendingColor = null;
            if (color == null || !Astromech.IsAstromech(Player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var picked = new TintMapColor(color.R, color.G, color.B);
            if (picked == Astromech.GetPaint(dbPlayer, _pendingZone))
                return;

            Astromech.SetPaint(Player, _pendingZone, picked);
            RefreshPaint(DB.Get<Player>(GetObjectUUID(Player)));
        }

        private static GuiColor ToGuiColor(TintMapColor color)
        {
            return new GuiColor(color.Red, color.Green, color.Blue);
        }

        private static string GetZoneName(AstromechPaintZone zone)
        {
            return zone switch
            {
                AstromechPaintZone.Secondary => "Secondary",
                AstromechPaintZone.Trim => "Trim",
                _ => "Primary"
            };
        }

        // ------------------------------------------------------------------ size

        public Action OnClickSmaller() => () => StepScale(-1);

        public Action OnClickLarger() => () => StepScale(1);

        private void StepScale(int direction)
        {
            if (!CanChange())
                return;

            var scale = AstromechWorkshop.StepScale(ScaleValue, direction);
            ScaleValue = scale;
            ScaleText = AstromechWorkshop.FormatScale(scale);
            QueueScale(scale);
        }

        private void QueueScale(float scale)
        {
            if (!CanChange())
                return;

            _pendingScale = AstromechWorkshop.RoundScale(scale);
            _hasPendingScale = true;
            if (_scaleFlushScheduled)
                return;

            _scaleFlushScheduled = true;
            var windowToken = WindowToken;
            DelayCommand(ApplyDelay, () =>
            {
                if (windowToken == WindowToken && Gui.IsWindowOpen(Player, WindowType))
                    FlushScale();
                else
                    _scaleFlushScheduled = false;
            });
        }

        private void FlushScale()
        {
            _scaleFlushScheduled = false;
            if (!_hasPendingScale)
                return;

            _hasPendingScale = false;
            if (!Astromech.IsAstromech(Player) || Math.Abs(_pendingScale - _appliedScale) < 0.0001f)
                return;

            _appliedScale = _pendingScale;
            Astromech.SetScale(Player, _appliedScale);
        }

        // ------------------------------------------------------------------ closing

        public override Action OnWindowClosed() => () =>
        {
            // Whatever the last drag left is kept when the window closes before the pause ends.
            FlushColor();
            FlushScale();
        };
    }
}
