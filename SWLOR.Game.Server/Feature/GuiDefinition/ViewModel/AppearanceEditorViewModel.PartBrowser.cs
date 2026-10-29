using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Body map, selected part picture and colour indicators. Selection is shown with NUI
    /// highlights (encouraged glow and draw-list outlines), never with text. The part pictures
    /// are browsed in their own window, <see cref="AppearancePartBrowserViewModel"/>.
    /// </summary>
    public partial class AppearanceEditorViewModel
    {
        // Palette cell geometry shared with the definition; the outline overlay is drawn in these units.
        public const float PaletteCellWidth = 20f;
        public const float PaletteCellHeight = 16f;

        private static readonly AppearanceArmorColor[] SwatchChannels =
        {
            AppearanceArmorColor.Leather1, AppearanceArmorColor.Cloth1, AppearanceArmorColor.Metal1,
            AppearanceArmorColor.Leather2, AppearanceArmorColor.Cloth2, AppearanceArmorColor.Metal2
        };

        private static readonly (string Name, TintMapLayerType Layer, ColorChannel Channel)[] BodyColorSwatches =
        {
            ("Skin", TintMapLayerType.Skin, ColorChannel.Skin),
            ("Hair", TintMapLayerType.Hair, ColorChannel.Hair),
            ("Tattoo1", TintMapLayerType.Tattoo1, ColorChannel.Tattoo1),
            ("Tattoo2", TintMapLayerType.Tattoo2, ColorChannel.Tattoo2)
        };

        private AppearanceEditorSlot _selectedSlot = AppearanceEditorSlot.Chest;
        private List<int> _partIds = new();
        private List<int> _browserIds = new();
        private int _browserCurrent = -1;
        private Func<int, string> _browserKey = _ => string.Empty;
        private Func<int, string> _browserLabel = id => id.ToString();
        private Action<int> _browserApply;
        private bool _browserFrontOnly;
        private AppearancePreviewView _previewView = AppearancePreviewView.Front;
        private float[] _paletteOutline = Array.Empty<float>();

        /// <summary>The DM owns the window cache when the possessed creature is the window's Player.</summary>
        private uint WindowOwner => GetIsDMPossessed(Player) ? GetMaster(Player) : Player;

        // ---------------- selected part bindings ----------------
        public string PartPreviewImage { get => Get<string>(); set => Set(value); }
        public string PartPreviewLabel { get => Get<string>(); set => Set(value); }
        public bool IsPartPreviewEnabled { get => Get<bool>(); set => Set(value); }
        public string PreviewViewText { get => Get<string>(); set => Set(value); }

        /// <summary>Weapon and shield parts ship only a front picture, so they cannot turn.</summary>
        public bool IsPreviewViewChoiceVisible { get => Get<bool>(); set => Set(value); }

        /// <summary>Turns the pictures to the next view: front, back, angle, then front again.</summary>
        public Action OnClickPreviewView() => () =>
        {
            if (_browserFrontOnly)
                return;
            SelectPreviewView((AppearancePreviewView)(((int)_previewView + 1) % 3));
        };

        public Action OnClickPartPreview() => () =>
        {
            if (IsPartBrowserOpen())
            {
                Gui.ClosePlayerWindow(WindowOwner, GuiWindowType.AppearancePartBrowser, Player);
                return;
            }
            if (!IsPartPreviewEnabled)
                return;
            Gui.TogglePlayerWindow(WindowOwner, GuiWindowType.AppearancePartBrowser, null, OBJECT_INVALID, Player);
            var browser = GetPartBrowser();
            if (browser == null)
                return;
            var scale = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiScale) / 100f;
            var screenWidth = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiWidth);
            var screenHeight = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiHeight);
            var placed = PlaceBeside(Geometry, AppearancePartBrowserDefinition.WindowWidth,
                AppearancePartBrowserDefinition.WindowHeight, screenWidth, screenHeight, scale);
            if (placed != null)
                browser.Geometry = placed;
            SyncPartBrowser();
        };

        public Action OnClickPreviousPart() => () => StepPart(-1);

        public Action OnClickNextPart() => () => StepPart(1);

        /// <summary>Wears the part before or after the current one, wrapping at either end.</summary>
        private void StepPart(int direction)
        {
            if (_browserIds.Count == 0)
                return;
            var index = _browserIds.IndexOf(_browserCurrent);
            var next = index < 0
                ? 0
                : (index + direction + _browserIds.Count) % _browserIds.Count;
            ApplyBrowserPart(_browserIds[next]);
        }

        /// <summary>Wears a part picked in the editor or the part browser window.</summary>
        internal void ApplyBrowserPart(int partId)
        {
            ToggleItemEquippedFlags();
            if (DoesNotHaveItemEquipped || !_browserIds.Contains(partId))
                return;
            _browserApply?.Invoke(partId);
        }

        /// <summary>Shows every picture from one side: the editor's part and the browser's pages.</summary>
        internal void SelectPreviewView(AppearancePreviewView view)
        {
            _previewView = view;
            SetIfChanged(nameof(PreviewViewText), view.ToString());
            PublishBrowser();
        }

        /// <summary>
        /// Places a window of the given size beside the editor: on its right, or on its left when
        /// the right side is off screen, then pulled fully on screen.
        /// </summary>
        /// <param name="editor">The editor's geometry: X and Y in physical pixels, size in GUI units.</param>
        internal static GuiRectangle PlaceBeside(GuiRectangle editor, float width, float height,
            float screenWidth, float screenHeight, float scale)
        {
            if (editor == null)
                return null;
            if (scale <= 0f)
                scale = 1f;
            var x = editor.X + editor.Width * scale;
            if (screenWidth > 0f && x + width * scale > screenWidth)
                x = editor.X - width * scale;
            return FitWindowToScreen(new GuiRectangle(Math.Max(0f, x), editor.Y, width, height),
                screenWidth, screenHeight, scale);
        }

        private bool IsPartBrowserOpen() => Gui.IsWindowOpen(Player, GuiWindowType.AppearancePartBrowser);

        private AppearancePartBrowserViewModel GetPartBrowser() => IsPartBrowserOpen()
            ? Gui.GetPlayerWindow(WindowOwner, GuiWindowType.AppearancePartBrowser)?.ViewModel as AppearancePartBrowserViewModel
            : null;

        private void SyncPartBrowser() => GetPartBrowser()?.ShowParts(this, _browserIds, _browserCurrent,
            _browserKey, _browserLabel, _browserFrontOnly, _previewView);

        private void ClosePartBrowser()
        {
            if (IsPartBrowserOpen())
                Gui.ClosePlayerWindow(WindowOwner, GuiWindowType.AppearancePartBrowser, Player);
        }

        // ---------------- body map bindings ----------------
        public string MapHeadImage { get => Get<string>(); set => Set(value); }
        public string MapNeckImage { get => Get<string>(); set => Set(value); }
        public string MapChestImage { get => Get<string>(); set => Set(value); }
        public string MapBeltImage { get => Get<string>(); set => Set(value); }
        public string MapPelvisImage { get => Get<string>(); set => Set(value); }
        public string MapRobeImage { get => Get<string>(); set => Set(value); }
        public string MapCloakImage { get => Get<string>(); set => Set(value); }
        public string MapRightShoulderImage { get => Get<string>(); set => Set(value); }
        public string MapLeftShoulderImage { get => Get<string>(); set => Set(value); }
        public string MapRightBicepImage { get => Get<string>(); set => Set(value); }
        public string MapLeftBicepImage { get => Get<string>(); set => Set(value); }
        public string MapRightForearmImage { get => Get<string>(); set => Set(value); }
        public string MapLeftForearmImage { get => Get<string>(); set => Set(value); }
        public string MapRightHandImage { get => Get<string>(); set => Set(value); }
        public string MapLeftHandImage { get => Get<string>(); set => Set(value); }
        public string MapRightThighImage { get => Get<string>(); set => Set(value); }
        public string MapLeftThighImage { get => Get<string>(); set => Set(value); }
        public string MapRightShinImage { get => Get<string>(); set => Set(value); }
        public string MapLeftShinImage { get => Get<string>(); set => Set(value); }
        public string MapRightFootImage { get => Get<string>(); set => Set(value); }
        public string MapLeftFootImage { get => Get<string>(); set => Set(value); }
        public bool MapHeadSelected { get => Get<bool>(); set => Set(value); }
        public bool MapNeckSelected { get => Get<bool>(); set => Set(value); }
        public bool MapChestSelected { get => Get<bool>(); set => Set(value); }
        public bool MapBeltSelected { get => Get<bool>(); set => Set(value); }
        public bool MapPelvisSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRobeSelected { get => Get<bool>(); set => Set(value); }
        public bool MapCloakSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightShoulderSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftShoulderSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightBicepSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftBicepSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightForearmSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftForearmSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightHandSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftHandSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightThighSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftThighSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightShinSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftShinSelected { get => Get<bool>(); set => Set(value); }
        public bool MapRightFootSelected { get => Get<bool>(); set => Set(value); }
        public bool MapLeftFootSelected { get => Get<bool>(); set => Set(value); }

        // ---------------- colour indicator bindings ----------------

        public GuiRectangle SelectedPartLeather1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartLeather2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartCloth1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartCloth2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartMetal1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle SelectedPartMetal2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiColor SelectedPartLeather1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartLeather2Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartCloth1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartCloth2Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartMetal1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor SelectedPartMetal2Tint { get => Get<GuiColor>(); set => Set(value); }
        public bool SelectedPartLeather1Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartLeather2Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth1Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth2Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal1Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal2Custom { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartLeather1Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartLeather2Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth1Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartCloth2Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal1Selected { get => Get<bool>(); set => Set(value); }
        public bool SelectedPartMetal2Selected { get => Get<bool>(); set => Set(value); }
        public bool IsPartColorsVisible { get => Get<bool>(); set => Set(value); }
        public string SelectedPartColorsTitle { get => Get<string>(); set => Set(value); }

        public GuiRectangle BodySkinRegion { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle BodyHairRegion { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle BodyTattoo1Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiRectangle BodyTattoo2Region { get => Get<GuiRectangle>(); set => Set(value); }
        public GuiColor BodySkinTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor BodyHairTint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor BodyTattoo1Tint { get => Get<GuiColor>(); set => Set(value); }
        public GuiColor BodyTattoo2Tint { get => Get<GuiColor>(); set => Set(value); }
        public bool BodySkinCustom { get => Get<bool>(); set => Set(value); }
        public bool BodyHairCustom { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo1Custom { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo2Custom { get => Get<bool>(); set => Set(value); }
        public bool BodySkinSelected { get => Get<bool>(); set => Set(value); }
        public bool BodyHairSelected { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo1Selected { get => Get<bool>(); set => Set(value); }
        public bool BodyTattoo2Selected { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<float> PaletteSelectionPoints { get => Get<GuiBindingList<float>>(); set => Set(value); }
        public bool IsPaletteSelectionVisible { get => Get<bool>(); set => Set(value); }
        public bool IsUseBaseEnabled { get => Get<bool>(); set => Set(value); }

        private void InitializePartBrowser()
        {
            // Every bound value needs an entry before it is watched or re-sent.
            _previewView = AppearancePreviewView.Front;
            IsPreviewViewChoiceVisible = true;
            PreviewViewText = _previewView.ToString();
            PartPreviewImage = string.Empty;
            PartPreviewLabel = string.Empty;
            IsPartPreviewEnabled = false;
            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
            {
                Set(string.Empty, $"Map{slot}Image");
                Set(false, $"Map{slot}Selected");
            }
            foreach (var channel in SwatchChannels)
            {
                Set(BuildPaletteRegion(255), $"SelectedPart{channel}Region");
                Set(new GuiColor(0, 0, 0), $"SelectedPart{channel}Tint");
                Set(false, $"SelectedPart{channel}Custom");
                Set(false, $"SelectedPart{channel}Selected");
            }
            foreach (var (name, _, _) in BodyColorSwatches)
            {
                Set(BuildPaletteRegion(255), $"Body{name}Region");
                Set(new GuiColor(0, 0, 0), $"Body{name}Tint");
                Set(false, $"Body{name}Custom");
                Set(false, $"Body{name}Selected");
            }
            IsPartColorsVisible = false;
            PaletteSelectionPoints = new GuiBindingList<float>();
            IsPaletteSelectionVisible = false;
            IsUseBaseEnabled = false;
        }

        // ---------------- browser ----------------
        private string BodyPrefix() => AppearancePreview.BodyPrefix(GetGender(_target),
            Get2DAString("appearance", "RACE", (int)GetAppearanceType(_target)));

        private void SetBrowserSource(IEnumerable<int> ids, int current, Func<int, string> key,
            Func<int, string> label, Action<int> apply, bool frontOnly = false)
        {
            _browserFrontOnly = frontOnly;
            SetIfChanged(nameof(IsPreviewViewChoiceVisible), !frontOnly);
            _browserIds = ids.ToList();
            _browserCurrent = current;
            _browserKey = key;
            _browserLabel = label ?? (id => id.ToString());
            _browserApply = apply;
            PublishBrowser();
        }

        /// <summary>Nothing to browse: the part browser window closes with the part picture.</summary>
        private void ClearBrowser()
        {
            SetBrowserSource(Array.Empty<int>(), -1, _ => string.Empty, null, null);
            ClosePartBrowser();
        }

        /// <summary>Shows the current part's picture and updates the part browser window when it is open.</summary>
        private void PublishBrowser()
        {
            if (_browserIds == null)
                return;
            var view = _browserFrontOnly ? AppearancePreviewView.Front : _previewView;
            var current = _browserIds.Contains(_browserCurrent) ? _browserCurrent : -1;
            SetIfChanged(nameof(PartPreviewImage), current >= 0 ? AppearancePreview.Picture(_browserKey(current), view) : string.Empty);
            SetIfChanged(nameof(PartPreviewLabel), current >= 0 ? _browserLabel(current) : string.Empty);
            SetIfChanged(nameof(IsPartPreviewEnabled), _browserIds.Count > 0 && _browserApply != null);
            SetIfChanged(nameof(PreviewViewText), view.ToString());
            SyncPartBrowser();
        }

        private void SetIfChanged<TValue>(string propertyName, TValue value)
        {
            if (!EqualityComparer<TValue>.Default.Equals(Get<TValue>(propertyName), value))
                Set(value, propertyName);
        }

        /// <summary>Points the browser at the parts of the selected body-map slot or weapon part.</summary>
        private void RefreshPartBrowser()
        {
            if (IsPartEditorHidden || DoesNotHaveItemEquipped)
            {
                ClearBrowser();
                return;
            }

            var body = BodyPrefix();
            int CurrentPartId() => SelectedPartIndex >= 0 && SelectedPartIndex < _partIds.Count ? _partIds[SelectedPartIndex] : -1;

            if (IsAppearanceSelected)
            {
                var slot = AppearancePreview.FromBodyCategory(SelectedPartCategoryIndex);
                _selectedSlot = slot;
                SetBrowserSource(_partIds, CurrentPartId(),
                    id => slot == AppearanceEditorSlot.Head
                        ? AppearancePreview.HeadKey(body, id)
                        : AppearancePreview.ArmorPartKey(body, slot, id),
                    null, SelectPartFromBrowser);
            }
            else if (SelectedItemTypeIndex == 0)
            {
                var part = AppearancePreview.ToArmorPart(_selectedSlot);
                if (part == AppearanceArmor.Invalid)
                {
                    _selectedSlot = AppearanceEditorSlot.Chest;
                    part = AppearanceArmor.Torso;
                }
                SyncSelectedPartSwatches();
                var (options, _) = GetArmorPartBinding(part);
                var slot = _selectedSlot;
                var current = GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)part);
                SetBrowserSource(options?.Select(option => option.Value) ?? Array.Empty<int>(), current,
                    id => AppearancePreview.ArmorPartKey(body, slot, id),
                    id => id == 0 ? "None" : id.ToString(),
                    id => ApplyArmorPartFromBrowser(part, id));
            }
            else if (SelectedItemTypeIndex == 1)
            {
                SetBrowserSource(_partIds, CurrentPartId(), AppearancePreview.HelmetKey, null, SelectPartFromBrowser);
            }
            else if (SelectedItemTypeIndex == 2)
            {
                SetBrowserSource(_partIds, CurrentPartId(), id => AppearancePreview.CloakKey(body, id), null, SelectPartFromBrowser);
            }
            else
            {
                var names = PartOptions;
                var ids = _partIds;
                SetBrowserSource(ids, CurrentPartId(), WeaponPictureKey(),
                    id => _partIdToIndex.TryGetValue(id, out var index) && names != null && index < names.Count ? names[index] : id.ToString(),
                    SelectPartFromBrowser, frontOnly: true);
            }
            RefreshBodyMap();
        }

        /// <summary>Picture key of a weapon part: the part model of the equipped weapon's item class.</summary>
        private Func<int, string> WeaponPictureKey()
        {
            var type = GetBaseItemType(GetItem());
            if (!_weaponAppearances.TryGetValue(type, out var appearance))
                return _ => string.Empty;
            var itemClass = Get2DAString("baseitems", "ItemClass", (int)type);
            if (appearance.IsSimple)
                return id => AppearancePreview.SimpleWeaponKey(itemClass, id);
            var part = SelectedPartCategoryIndex switch
            {
                0 => AppearanceWeapon.Top,
                1 => AppearanceWeapon.Middle,
                2 => AppearanceWeapon.Bottom,
                _ => AppearanceWeapon.Invalid
            };
            return id => AppearancePreview.WeaponPartKey(itemClass, part, id);
        }

        private void SelectPartFromBrowser(int partId)
        {
            if (!_partIdToIndex.TryGetValue(partId, out var index))
                return;
            if (SelectedPartIndex >= 0 && SelectedPartIndex < PartSelected.Count)
                PartSelected[SelectedPartIndex] = false;
            SelectedPartIndex = index;
            PartSelected[index] = true;
            LoadPart();
            _browserCurrent = partId;
            PublishBrowser();
            RefreshBodyMap();
        }

        private void ApplyArmorPartFromBrowser(AppearanceArmor part, int partId)
        {
            var (options, select) = GetArmorPartBinding(part);
            if (options == null || options.All(option => option.Value != partId))
                return;
            var wasSkippingAdjustment = _skipAdjustArmorPart;
            _skipAdjustArmorPart = true;
            try
            {
                select(partId);
            }
            finally
            {
                _skipAdjustArmorPart = wasSkippingAdjustment;
            }
            AdjustArmorPart(part, 0);
            _browserCurrent = GetItemAppearance(GetItem(), ItemAppearanceType.ArmorModel, (int)part);
            PublishBrowser();
            RefreshBodyMap();
        }

        // ---------------- window size ----------------
        /// <summary>
        /// Caps a saved window larger than the player's screen to it and pulls it back on screen.
        /// Never grows a window.
        /// </summary>
        /// <param name="screenWidth">Screen width in physical pixels (0 when unknown), like Geometry X.</param>
        /// <param name="screenHeight">Screen height in physical pixels (0 when unknown), like Geometry Y.</param>
        /// <param name="scale">GUI scale; a window's on-screen size is its width and height times this.</param>
        internal static GuiRectangle FitWindowToScreen(GuiRectangle geometry, float screenWidth, float screenHeight, float scale)
        {
            if (geometry == null)
                return null;
            if (scale <= 0f)
                scale = 1f;
            var width = geometry.Width;
            var height = geometry.Height;
            var x = geometry.X;
            var y = geometry.Y;
            if (screenWidth > 0f)
            {
                width = Math.Min(width, screenWidth / scale);
                x = Math.Clamp(x, 0f, Math.Max(0f, screenWidth - width * scale));
            }
            if (screenHeight > 0f)
            {
                height = Math.Min(height, screenHeight / scale);
                y = Math.Clamp(y, 0f, Math.Max(0f, screenHeight - height * scale));
            }
            if (width == geometry.Width && height == geometry.Height && x == geometry.X && y == geometry.Y)
                return geometry;
            return new GuiRectangle(x, y, width, height);
        }

        /// <summary>Runs once when the window opens, never on a move, so a drag is never undone.</summary>
        private void FitWindowToPlayerScreen()
        {
            var scale = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiScale) / 100f;
            var screenWidth = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiWidth);
            var screenHeight = GetPlayerDeviceProperty(Player, PlayerDevicePropertyType.GuiHeight);
            var fitted = FitWindowToScreen(Geometry, screenWidth, screenHeight, scale);
            if (fitted != null && !ReferenceEquals(fitted, Geometry))
                Geometry = fitted;
        }

        // ---------------- body map ----------------
        private void RefreshBodyMap()
        {
            var body = BodyPrefix();
            var view = AppearancePreviewView.Front;
            var appearance = IsAppearanceSelected;
            var chest = GetItemInSlot(InventorySlot.Chest, _target);
            var helmet = GetItemInSlot(InventorySlot.Head, _target);
            var cloak = GetItemInSlot(InventorySlot.Cloak, _target);

            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
            {
                string key;
                if (appearance)
                {
                    var part = ToCreaturePart(slot);
                    var id = part == null ? 0 : GetCreatureBodyPart(part.Value, _target);
                    key = slot == AppearanceEditorSlot.Head
                        ? AppearancePreview.HeadKey(body, id)
                        : AppearancePreview.ArmorPartKey(body, slot, id);
                }
                else if (slot == AppearanceEditorSlot.Head)
                {
                    key = GetIsObjectValid(helmet)
                        ? AppearancePreview.HelmetKey(GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, 0))
                        : string.Empty;
                }
                else if (slot == AppearanceEditorSlot.Cloak)
                {
                    key = GetIsObjectValid(cloak)
                        ? AppearancePreview.CloakKey(body, GetItemAppearance(cloak, ItemAppearanceType.SimpleModel, 0))
                        : string.Empty;
                }
                else
                {
                    var part = AppearancePreview.ToArmorPart(slot);
                    key = GetIsObjectValid(chest)
                        ? AppearancePreview.ArmorPartKey(body, slot, GetItemAppearance(chest, ItemAppearanceType.ArmorModel, (int)part))
                        : string.Empty;
                }
                SetIfChanged($"Map{slot}Image", AppearancePreview.Picture(key, view));
            }
            RefreshBodyMapSelection();
        }

        private void RefreshBodyMapSelection()
        {
            var selected = IsAppearanceSelected ? _selectedSlot
                : SelectedItemTypeIndex == 1 ? AppearanceEditorSlot.Head
                : SelectedItemTypeIndex == 2 ? AppearanceEditorSlot.Cloak
                : _selectedSlot;
            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
                SetIfChanged($"Map{slot}Selected", slot == selected);
        }

        private static CreaturePart? ToCreaturePart(AppearanceEditorSlot slot) => slot switch
        {
            AppearanceEditorSlot.Head => CreaturePart.Head,
            AppearanceEditorSlot.Chest => CreaturePart.Torso,
            AppearanceEditorSlot.Pelvis => CreaturePart.Pelvis,
            AppearanceEditorSlot.RightBicep => CreaturePart.RightBicep,
            AppearanceEditorSlot.LeftBicep => CreaturePart.LeftBicep,
            AppearanceEditorSlot.RightForearm => CreaturePart.RightForearm,
            AppearanceEditorSlot.LeftForearm => CreaturePart.LeftForearm,
            AppearanceEditorSlot.RightHand => CreaturePart.RightHand,
            AppearanceEditorSlot.LeftHand => CreaturePart.LeftHand,
            AppearanceEditorSlot.RightThigh => CreaturePart.RightThigh,
            AppearanceEditorSlot.LeftThigh => CreaturePart.LeftThigh,
            AppearanceEditorSlot.RightShin => CreaturePart.RightShin,
            AppearanceEditorSlot.LeftShin => CreaturePart.LeftShin,
            AppearanceEditorSlot.RightFoot => CreaturePart.RightFoot,
            AppearanceEditorSlot.LeftFoot => CreaturePart.LeftFoot,
            _ => null
        };

        private static ColorTarget ToColorTarget(AppearanceEditorSlot slot) =>
            Enum.TryParse<ColorTarget>(slot.ToString(), out var target) ? target : ColorTarget.Global;

        public Action OnClickMapSlot(AppearanceEditorSlot slot) => () =>
        {
            ToggleItemEquippedFlags();
            if (IsAppearanceSelected)
            {
                var category = AppearancePreview.BodyCategory(slot);
                if (category < 0 || DoesNotHaveItemEquipped)
                    return;
                _selectedSlot = slot;
                if (SelectedPartCategoryIndex >= 0 && SelectedPartCategoryIndex < PartCategorySelected.Count)
                    PartCategorySelected[SelectedPartCategoryIndex] = false;
                SelectedPartCategoryIndex = category;
                PartCategorySelected[category] = true;
                LoadBodyParts();
                RefreshPartBrowser();
                return;
            }

            if (!IsEquipmentSelected)
                return;

            var itemType = slot switch
            {
                AppearanceEditorSlot.Head => 1,
                AppearanceEditorSlot.Cloak => 2,
                _ => 0
            };
            if (itemType != 0 && !GetIsObjectValid(GetItemInSlot(
                    itemType == 1 ? InventorySlot.Head : InventorySlot.Cloak, _target)))
            {
                // Keep the map; an empty helmet or cloak slot has nothing to edit.
                SendMessageToPC(Player, ColorToken.Red($"No {(itemType == 1 ? "helmet" : "cloak")} is equipped."));
                return;
            }
            if (itemType == 0)
                _selectedSlot = slot;
            if (SelectedItemTypeIndex != itemType)
                SelectedItemTypeIndex = itemType;
            if (itemType != 0 || DoesNotHaveItemEquipped)
            {
                RefreshBodyMapSelection();
                return;
            }

            _colorTarget = ToColorTarget(slot);
            UpdateTargetedColor();
            SyncSelectedPartSwatches();
            UpdatePaletteSheetForChannel();
            LoadTintMapEditor();
            RefreshPartBrowser();
        };

        // ---------------- colour indicators ----------------
        /// <summary>Called after a swatch's region, tint or custom flag changes.</summary>
        private void OnSwatchUpdated(ColorTarget target, AppearanceArmorColor channel)
        {
            if (target != ColorTarget.Global && target == ToColorTarget(_selectedSlot))
                MirrorPartSwatch(channel);
            RefreshPaletteSelection();
        }

        private void SyncSelectedPartSwatches()
        {
            var visible = IsEquipmentSelected && SelectedItemTypeIndex == 0 && ToColorTarget(_selectedSlot) != ColorTarget.Global;
            SetIfChanged(nameof(IsPartColorsVisible), visible);
            if (!visible)
                return;
            SetIfChanged(nameof(SelectedPartColorsTitle), PartColorsTitle(_selectedSlot));
            foreach (var channel in SwatchChannels)
                MirrorPartSwatch(channel);
            SyncSelectedPartSelection();
        }

        /// <summary>Heads the part colour rows with the selected part's name, e.g. "Left Shoulder".</summary>
        internal static string PartColorsTitle(AppearanceEditorSlot slot) =>
            string.Concat(slot.ToString().Select((letter, index) =>
                index > 0 && char.IsUpper(letter) ? " " + letter : letter.ToString()));

        private void MirrorPartSwatch(AppearanceArmorColor channel)
        {
            var source = $"{ToColorTarget(_selectedSlot)}{channel}";
            var region = Get<GuiRectangle>(source + "Region") ?? BuildPaletteRegion(255);
            var custom = Get<bool>(source + "Custom");
            Set(region, $"SelectedPart{channel}Region");
            Set(Get<GuiColor>(source + "Tint") ?? new GuiColor(0, 0, 0), $"SelectedPart{channel}Tint");
            SetIfChanged($"SelectedPart{channel}Custom", custom);
        }

        private void SyncSelectedPartSelection()
        {
            var partTarget = ToColorTarget(_selectedSlot);
            foreach (var channel in SwatchChannels)
                SetIfChanged($"SelectedPart{channel}Selected", _colorTarget == partTarget && _selectedColorChannel == channel);
            SetIfChanged(nameof(IsUseBaseEnabled), IsEquipmentSelected && SelectedItemTypeIndex == 0 && _colorTarget != ColorTarget.Global);
            RefreshPaletteSelection();
        }

        private void RefreshBodySwatches()
        {
            if (!IsAppearanceSelected)
                return;
            for (var index = 0; index < BodyColorSwatches.Length; index++)
            {
                var (name, layer, channel) = BodyColorSwatches[index];
                var colorId = GetColor(_target, channel);
                var hasCustom = TintMapColor.TryFromStoredValue(
                    GetLocalInt(_target, TintMapVariable.GetCreatureColorStateName(layer)), out var custom);
                var color = hasCustom ? custom : TintMapPaletteColors.GetColor(layer, colorId);
                var region = BuildPaletteRegion(colorId);
                SetIfChanged($"Body{name}Region", region);
                Set(new GuiColor(color.Red, color.Green, color.Blue), $"Body{name}Tint");
                SetIfChanged($"Body{name}Custom", hasCustom);
                SetIfChanged($"Body{name}Selected", SelectedColorCategoryIndex == index);
            }
            RefreshPaletteSelection();
        }

        private void RefreshItemBaseSwatches()
        {
            // Helmets and cloaks only use the six item channels.
            if (!IsEquipmentSelected || SelectedItemTypeIndex is not (1 or 2) || DoesNotHaveItemEquipped)
                return;
            foreach (var channel in SwatchChannels)
            {
                var name = $"Global{channel}";
                Set(BuildColorRegion(ColorTarget.Global, channel), name + "Region");
                UpdateColorSwatch(ColorTarget.Global, channel, name + "Region");
            }
        }

        public Action OnClickBaseSwatch(AppearanceArmorColor channel) => () =>
        {
            if (SelectedItemTypeIndex is 1 or 2)
            {
                // Helmet and cloak channels are the colour categories, in LoadColorCategoryOptions order.
                var category = channel switch
                {
                    AppearanceArmorColor.Leather1 => 0,
                    AppearanceArmorColor.Leather2 => 1,
                    AppearanceArmorColor.Cloth1 => 2,
                    AppearanceArmorColor.Cloth2 => 3,
                    AppearanceArmorColor.Metal1 => 4,
                    _ => 5
                };
                if (ColorCategorySelected != null && SelectedColorCategoryIndex < ColorCategorySelected.Count)
                    ColorCategorySelected[SelectedColorCategoryIndex] = false;
                SelectedColorCategoryIndex = category;
                if (ColorCategorySelected != null && category < ColorCategorySelected.Count)
                    ColorCategorySelected[category] = true;
                _colorTarget = ColorTarget.Global;
                _selectedColorChannel = channel;
                UpdateColorSelection();
                LoadTintMapEditor();
                return;
            }
            OnClickColorTarget(ColorTarget.Global, channel)();
        };

        public Action OnClickPartSwatch(AppearanceArmorColor channel) => () =>
            OnClickColorTarget(ToColorTarget(_selectedSlot), channel)();

        public Action OnMouseDownPartSwatch(AppearanceArmorColor channel) => () =>
            OnClickClearColor(ToColorTarget(_selectedSlot), channel)();

        public Action OnClickBodySwatch(int index) => () =>
        {
            if (!IsAppearanceSelected || index < 0 || index >= BodyColorSwatches.Length)
                return;
            if (ColorCategorySelected != null && SelectedColorCategoryIndex < ColorCategorySelected.Count)
                ColorCategorySelected[SelectedColorCategoryIndex] = false;
            SelectedColorCategoryIndex = index;
            if (ColorCategorySelected != null && index < ColorCategorySelected.Count)
                ColorCategorySelected[index] = true;
            LoadTintMapEditor();
            RefreshBodySwatches();
        };

        public Action OnClickUseBase() => () =>
        {
            if (!IsEquipmentSelected || SelectedItemTypeIndex != 0 || _colorTarget == ColorTarget.Global)
                return;
            ResetArmorColorToInheritance(_colorTarget, _selectedColorChannel);
        };

        private void UpdatePaletteSheetForChannel()
        {
            if (!IsEquipmentSelected || SelectedItemTypeIndex != 0)
                return;
            ColorSheetResref = _selectedColorChannel is AppearanceArmorColor.Metal1 or AppearanceArmorColor.Metal2
                ? "gui_pal_armor01"
                : "gui_pal_tattoo";
        }

        /// <summary>Outline the palette cell of the selected swatch; hidden for custom or inherited colours.</summary>
        private void RefreshPaletteSelection()
        {
            string source = null;
            if (IsAppearanceSelected && SelectedColorCategoryIndex >= 0 && SelectedColorCategoryIndex < BodyColorSwatches.Length)
                source = $"Body{BodyColorSwatches[SelectedColorCategoryIndex].Name}";
            else if (IsEquipmentSelected && SelectedItemTypeIndex is 0 or 1 or 2)
                source = $"{(SelectedItemTypeIndex == 0 ? _colorTarget : ColorTarget.Global)}{_selectedColorChannel}";

            var region = source == null ? null : Get<GuiRectangle>(source + "Region");
            var custom = source != null && Get<bool>(source + "Custom");
            float[] points = Array.Empty<float>();
            if (region != null && !custom && region.Width >= ColorSize)
            {
                var column = (int)(region.X / ColorSize);
                var row = (int)(region.Y / ColorSize);
                var left = column * PaletteCellWidth + 1f;
                var top = row * PaletteCellHeight + 1f;
                var right = left + PaletteCellWidth - 2f;
                var bottom = top + PaletteCellHeight - 2f;
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
    }
}
