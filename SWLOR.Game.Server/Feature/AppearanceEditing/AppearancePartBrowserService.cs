using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Preview;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>
    /// The part picture, the part browser window and the body map: what the browser shows for the selected slot or weapon part,
    /// wearing a part picked there, and where the browser window sits beside the editor.
    /// </summary>
    public class AppearancePartBrowserService
    {
        private AppearancePartEditService AppearancePartEditService { get; }
        private AppearanceSwatchService AppearanceSwatchService { get; }
        private AppearanceTintEditorService AppearanceTintEditorService { get; }
        private IGuiService Gui { get; }
        private IWeaponAppearanceCatalog IWeaponAppearanceCatalog { get; }

        public AppearancePartBrowserService(AppearancePartEditService appearancePartEditService, AppearanceSwatchService appearanceSwatchService, AppearanceTintEditorService appearanceTintEditorService, IGuiService gui, IWeaponAppearanceCatalog iWeaponAppearanceCatalog)
        {
            AppearancePartEditService = appearancePartEditService;
            AppearanceSwatchService = appearanceSwatchService;
            AppearanceTintEditorService = appearanceTintEditorService;
            Gui = gui;
            IWeaponAppearanceCatalog = iWeaponAppearanceCatalog;
        }

        public void ClickPreviewView(IAppearanceEditorHost host)
        {
            if (host.Session.BrowserFrontOnly)
                return;
            SelectPreviewView(host, (AppearancePreviewView)(((int)host.Session.PreviewView + 1) % 3));
        }

        public void ClickPartPreview(IAppearanceEditorHost host)
        {
            if (IsPartBrowserOpen(host))
            {
                Gui.ClosePlayerWindow(WindowOwner(host), GuiWindowType.AppearancePartBrowser, host.Player);
                return;
            }
            if (!host.IsPartPreviewEnabled)
                return;
            Gui.TogglePlayerWindow(WindowOwner(host), GuiWindowType.AppearancePartBrowser, null, OBJECT_INVALID, host.Player);
            var browser = GetPartBrowser(host);
            if (browser == null)
                return;
            var scale = GetPlayerDeviceProperty(host.Player, PlayerDevicePropertyType.GuiScale) / 100f;
            var screenWidth = GetPlayerDeviceProperty(host.Player, PlayerDevicePropertyType.GuiWidth);
            var screenHeight = GetPlayerDeviceProperty(host.Player, PlayerDevicePropertyType.GuiHeight);
            // Keep the size the player last gave the browser; only its position follows the editor.
            var size = browser.Geometry;
            var placed = PlaceBeside(host.Geometry,
                size?.Width > 0f ? size.Width : AppearancePartBrowserDefinition.WindowWidth,
                size?.Height > 0f ? size.Height : AppearancePartBrowserDefinition.WindowHeight,
                screenWidth, screenHeight, scale);
            if (placed != null)
                browser.Geometry = placed;
            browser.FitGrid();
            SyncPartBrowser(host);
        }

        public void StepPart(IAppearanceEditorHost host, int direction)
        {
            if (host.Session.BrowserIds.Count == 0)
                return;
            var index = host.Session.BrowserIds.IndexOf(host.Session.BrowserCurrent);
            var next = index < 0
                ? 0
                : (index + direction + host.Session.BrowserIds.Count) % host.Session.BrowserIds.Count;
            ApplyBrowserPart(host, host.Session.BrowserIds[next]);
        }

        /// <summary>Wears a part picked in the editor or the part browser window.</summary>
        public void ApplyBrowserPart(IAppearanceEditorHost host, int partId)
        {
            if (host.IsPartEditorHidden)
                return;
            host.ToggleItemEquippedFlags();
            if (host.DoesNotHaveItemEquipped || !host.Session.BrowserIds.Contains(partId))
                return;
            host.Session.BrowserApply?.Invoke(partId);
        }

        /// <summary>Shows every picture from one side: the editor's part and the browser's pages.</summary>
        public void SelectPreviewView(IAppearanceEditorHost host, AppearancePreviewView view)
        {
            host.Session.PreviewView = view;
            host.SetIfChanged(nameof(AppearanceEditorViewModel.PreviewViewText), view.ToString());
            PublishBrowser(host);
        }

        /// <summary>
        /// Places a window of the given size beside the editor: on its right, or on its left when
        /// the right side is off screen. When neither side has room it goes against the screen
        /// edge that covers less of the editor.
        /// </summary>
        /// <param name="editor">The editor's geometry: X and Y in physical pixels, size in GUI units.</param>
        public static GuiRectangle PlaceBeside(GuiRectangle editor, float width, float height,
            float screenWidth, float screenHeight, float scale)
        {
            if (editor == null)
                return null;
            if (scale <= 0f)
                scale = 1f;
            var right = editor.X + editor.Width * scale;
            var left = editor.X - width * scale;
            float x;
            if (screenWidth <= 0f || right + width * scale <= screenWidth)
                x = right;
            else if (left >= 0f)
                x = left;
            else
            {
                var rightOverlap = right + width * scale - screenWidth;
                x = rightOverlap <= -left ? screenWidth - width * scale : 0f;
            }
            return FitWindowToScreen(new GuiRectangle(Math.Max(0f, x), editor.Y, width, height),
                screenWidth, screenHeight, scale);
        }

        /// <summary>The DM owns the window cache when the possessed creature is the window's Player.</summary>
        private static uint WindowOwner(IAppearanceEditorHost host) => GetIsDMPossessed(host.Player) ? GetMaster(host.Player) : host.Player;

        public bool IsPartBrowserOpen(IAppearanceEditorHost host) => Gui.IsWindowOpen(host.Player, GuiWindowType.AppearancePartBrowser);

        private AppearancePartBrowserViewModel GetPartBrowser(IAppearanceEditorHost host) => IsPartBrowserOpen(host)
            ? Gui.GetPlayerWindow(WindowOwner(host), GuiWindowType.AppearancePartBrowser)?.ViewModel as AppearancePartBrowserViewModel
            : null;

        private void SyncPartBrowser(IAppearanceEditorHost host) => GetPartBrowser(host)?.ShowParts((AppearanceEditorViewModel)host, host.Session.BrowserIds, host.Session.BrowserCurrent,
            host.Session.BrowserKey, host.Session.BrowserLabel, host.Session.BrowserFrontOnly, host.Session.PreviewView);

        public void ClosePartBrowser(IAppearanceEditorHost host)
        {
            if (IsPartBrowserOpen(host))
                Gui.ClosePlayerWindow(WindowOwner(host), GuiWindowType.AppearancePartBrowser, host.Player);
        }

        public void InitializePartBrowser(IAppearanceEditorHost host)
        {
            // Every bound value needs an entry before it is watched or re-sent.
            host.Session.PreviewView = AppearancePreviewView.Front;
            host.IsPreviewViewChoiceVisible = true;
            host.PreviewViewText = host.Session.PreviewView.ToString();
            host.PartPreviewImage = string.Empty;
            host.PartPreviewLabel = string.Empty;
            host.PartPreviewNumber = string.Empty;
            host.IsPartPreviewEnabled = false;
            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
            {
                host.Set(string.Empty, $"Map{slot}Image");
                host.Set(false, $"Map{slot}Selected");
            }
            foreach (var channel in AppearanceSwatchService.SwatchChannels)
            {
                host.Set(ArmorColorRegionCatalog.BuildPaletteRegion(255), $"SelectedPart{channel}Region");
                host.Set(new GuiColor(0, 0, 0), $"SelectedPart{channel}Tint");
                host.Set(false, $"SelectedPart{channel}Custom");
                host.Set(false, $"SelectedPart{channel}Selected");
                host.Set(string.Empty, $"SelectedPart{channel}Code");
            }
            foreach (var (name, _, _) in AppearanceSwatchService.BodyColorSwatches)
            {
                host.Set(ArmorColorRegionCatalog.BuildPaletteRegion(255), $"Body{name}Region");
                host.Set(new GuiColor(0, 0, 0), $"Body{name}Tint");
                host.Set(false, $"Body{name}Custom");
                host.Set(false, $"Body{name}Selected");
            }
            host.IsPartColorsVisible = false;
            host.PaletteSelectionPoints = new GuiBindingList<float>();
            host.IsPaletteSelectionVisible = false;
        }

        private string BodyPrefix(IAppearanceEditorHost host) => AppearancePreview.BodyPrefix(GetGender(host.Session.Target),
            Get2DAString("appearance", "RACE", (int)GetAppearanceType(host.Session.Target)));

        private void SetBrowserSource(IAppearanceEditorHost host, IEnumerable<int> ids, int current, Func<int, string> key,
            Func<int, string> label, Action<int> apply, bool frontOnly = false)
        {
            host.Session.BrowserFrontOnly = frontOnly;
            host.SetIfChanged(nameof(AppearanceEditorViewModel.IsPreviewViewChoiceVisible), !frontOnly);
            host.Session.BrowserIds = ids.ToList();
            host.Session.BrowserCurrent = current;
            host.Session.BrowserKey = key;
            host.Session.BrowserLabel = label ?? (id => AppearancePartNames.Get(key(id), id.ToString()));
            host.Session.BrowserApply = apply;
            PublishBrowser(host);
        }

        /// <summary>Nothing to browse: the part browser window closes with the part picture.</summary>
        public void ClearBrowser(IAppearanceEditorHost host)
        {
            SetBrowserSource(host, Array.Empty<int>(), -1, _ => string.Empty, null, null);
            ClosePartBrowser(host);
        }

        /// <summary>Shows the current part's picture and updates the part browser window when it is open.</summary>
        public void PublishBrowser(IAppearanceEditorHost host)
        {
            if (host.Session.BrowserIds == null)
                return;
            var view = host.Session.BrowserFrontOnly ? AppearancePreviewView.Front : host.Session.PreviewView;
            var current = host.Session.BrowserIds.Contains(host.Session.BrowserCurrent) ? host.Session.BrowserCurrent : -1;
            host.SetIfChanged(nameof(AppearanceEditorViewModel.PartPreviewImage), current >= 0 ? AppearancePreview.Picture(host.Session.BrowserKey(current), view) : string.Empty);
            host.SetIfChanged(nameof(AppearanceEditorViewModel.PartPreviewLabel), current >= 0 ? host.Session.BrowserLabel(current) : string.Empty);
            host.SetIfChanged(nameof(AppearanceEditorViewModel.PartPreviewNumber), current > 0 ? current.ToString() : string.Empty);
            host.SetIfChanged(nameof(AppearanceEditorViewModel.IsPartPreviewEnabled), host.Session.BrowserIds.Count > 0 && host.Session.BrowserApply != null);
            host.SetIfChanged(nameof(AppearanceEditorViewModel.PreviewViewText), view.ToString());
            SyncPartBrowser(host);
        }

        /// <summary>Points the browser at the parts of the selected body-map slot or weapon part.</summary>
        public void RefreshPartBrowser(IAppearanceEditorHost host)
        {
            if (host.IsPartEditorHidden || host.DoesNotHaveItemEquipped)
            {
                ClearBrowser(host);
                return;
            }

            var body = BodyPrefix(host);
            int CurrentPartId() => host.SelectedPartIndex >= 0 && host.SelectedPartIndex < host.Session.PartIds.Count ? host.Session.PartIds[host.SelectedPartIndex] : -1;

            if (host.IsAppearanceSelected)
            {
                var slot = AppearancePreview.FromBodyCategory(host.SelectedPartCategoryIndex);
                host.Session.SelectedSlot = slot;
                SetBrowserSource(host, host.Session.PartIds, CurrentPartId(),
                    id => slot == AppearanceEditorSlot.Head
                        ? AppearancePreview.HeadKey(body, id)
                        : AppearancePreview.ArmorPartKey(body, slot, id),
                    null, partId => SelectPartFromBrowser(host, partId));
            }
            else if (host.SelectedItemTypeIndex == 0)
            {
                var part = AppearancePreview.ToArmorPart(host.Session.SelectedSlot);
                if (part == AppearanceArmor.Invalid)
                {
                    host.Session.SelectedSlot = AppearanceEditorSlot.Chest;
                    part = AppearanceArmor.Torso;
                }
                AppearanceSwatchService.SyncSelectedPartSwatches(host);
                var (options, _, _) = host.GetArmorPartBinding(part);
                var slot = host.Session.SelectedSlot;
                // A retained bare style shows as the list's bare option; the item keeps it until the player picks a part.
                var current = DisplayedArmorStyle(host, part,
                    GetItemAppearance(host.GetItem(), ItemAppearanceType.ArmorModel, (int)part));
                SetBrowserSource(host, options?.Select(option => option.Value) ?? Array.Empty<int>(), current,
                    id => AppearancePreview.ArmorPartKey(body, slot, id),
                    id => id == 0 ? "None" : AppearancePartNames.Get(AppearancePreview.ArmorPartKey(body, slot, id), id.ToString()),
                    id => ApplyArmorPartFromBrowser(host, part, id));
            }
            else if (host.SelectedItemTypeIndex == 1)
            {
                SetBrowserSource(host, host.Session.PartIds, CurrentPartId(), AppearancePreview.HelmetKey, null, partId => SelectPartFromBrowser(host, partId));
            }
            else if (host.SelectedItemTypeIndex == 2)
            {
                SetBrowserSource(host, host.Session.PartIds, CurrentPartId(), id => AppearancePreview.CloakKey(body, id), null, partId => SelectPartFromBrowser(host, partId));
            }
            else
            {
                var names = host.PartOptions;
                var ids = host.Session.PartIds;
                var pictureKey = WeaponPictureKey(host);
                // A weapon definition's own part name wins over the picture's name.
                SetBrowserSource(host, ids, CurrentPartId(), pictureKey,
                    id => host.Session.PartIdToIndex.TryGetValue(id, out var index) && names != null && index < names.Count &&
                          names[index] != WeaponAppearanceBaseDefinition.DefaultPartName(id)
                        ? names[index]
                        : AppearancePartNames.Get(pictureKey(id), id.ToString()),
                    partId => SelectPartFromBrowser(host, partId), frontOnly: true);
            }
            RefreshBodyMap(host);
        }

        /// <summary>Picture key of a weapon part: the part model of the equipped weapon's item class.</summary>
        private Func<int, string> WeaponPictureKey(IAppearanceEditorHost host)
        {
            var type = GetBaseItemType(host.GetItem());
            if (!IWeaponAppearanceCatalog.Definitions.TryGetValue(type, out var appearance))
                return _ => string.Empty;
            var itemClass = Get2DAString("baseitems", "ItemClass", (int)type);
            if (appearance.IsSimple)
                return id => AppearancePreview.SimpleWeaponKey(itemClass, id);
            var part = host.SelectedPartCategoryIndex switch
            {
                0 => AppearanceWeapon.Top,
                1 => AppearanceWeapon.Middle,
                2 => AppearanceWeapon.Bottom,
                _ => AppearanceWeapon.Invalid
            };
            return id => AppearancePreview.WeaponPartKey(itemClass, part, id);
        }

        private void SelectPartFromBrowser(IAppearanceEditorHost host, int partId)
        {
            if (!host.Session.PartIdToIndex.TryGetValue(partId, out var index))
                return;
            if (host.SelectedPartIndex >= 0 && host.SelectedPartIndex < host.PartSelected.Count)
                host.PartSelected[host.SelectedPartIndex] = false;
            host.SelectedPartIndex = index;
            host.PartSelected[index] = true;
            host.LoadPart();
            host.Session.BrowserCurrent = partId;
            PublishBrowser(host);
            RefreshBodyMap(host);
        }

        private void ApplyArmorPartFromBrowser(IAppearanceEditorHost host, AppearanceArmor part, int partId)
        {
            var (options, _, select) = host.GetArmorPartBinding(part);
            if (options == null || options.All(option => option.Value != partId))
                return;
            var wasSkippingAdjustment = host.Session.SkipAdjustArmorPart;
            host.Session.SkipAdjustArmorPart = true;
            try
            {
                select(partId);
            }
            finally
            {
                host.Session.SkipAdjustArmorPart = wasSkippingAdjustment;
            }
            AppearancePartEditService.AdjustArmorPart(host, part, 0);
            host.Session.BrowserCurrent = GetItemAppearance(host.GetItem(), ItemAppearanceType.ArmorModel, (int)part);
            PublishBrowser(host);
            RefreshBodyMap(host);
        }

        /// <summary>
        /// Caps a saved window larger than the player's screen to it and pulls it back on screen.
        /// Never grows a window.
        /// </summary>
        /// <param name="screenWidth">Screen width in physical pixels (0 when unknown), like Geometry X.</param>
        /// <param name="screenHeight">Screen height in physical pixels (0 when unknown), like Geometry Y.</param>
        /// <param name="scale">GUI scale; a window's on-screen size is its width and height times this.</param>
        public static GuiRectangle FitWindowToScreen(GuiRectangle geometry, float screenWidth, float screenHeight, float scale)
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
        public void FitWindowToPlayerScreen(IAppearanceEditorHost host)
        {
            var scale = GetPlayerDeviceProperty(host.Player, PlayerDevicePropertyType.GuiScale) / 100f;
            var screenWidth = GetPlayerDeviceProperty(host.Player, PlayerDevicePropertyType.GuiWidth);
            var screenHeight = GetPlayerDeviceProperty(host.Player, PlayerDevicePropertyType.GuiHeight);
            var fitted = FitWindowToScreen(host.Geometry, screenWidth, screenHeight, scale);
            if (fitted != null && !ReferenceEquals(fitted, host.Geometry))
                host.Geometry = fitted;
        }

        public int DisplayedArmorStyle(IAppearanceEditorHost host, AppearanceArmor part, int style) =>
            ArmorAppearanceCatalog.Definitions.TryGetValue(GetAppearanceType(host.Session.Target), out var definition)
                ? ArmorAppearanceCatalog.DisplayedStyle(definition, part, style)
                : style;

        public void RefreshBodyMap(IAppearanceEditorHost host)
        {
            var body = BodyPrefix(host);
            var view = AppearancePreviewView.Front;
            var appearance = host.IsAppearanceSelected;
            var chest = GetItemInSlot(InventorySlot.Chest, host.Session.Target);
            var helmet = GetItemInSlot(InventorySlot.Head, host.Session.Target);
            var cloak = GetItemInSlot(InventorySlot.Cloak, host.Session.Target);

            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
            {
                string key;
                if (appearance)
                {
                    var part = ToCreaturePart(slot);
                    var id = part == null ? 0 : GetCreatureBodyPart(part.Value, host.Session.Target);
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
                        ? AppearancePreview.ArmorPartKey(body, slot, DisplayedArmorStyle(host, part,
                            GetItemAppearance(chest, ItemAppearanceType.ArmorModel, (int)part)))
                        : string.Empty;
                }
                host.SetIfChanged($"Map{slot}Image", AppearancePreview.Picture(key, view));
            }
            RefreshBodyMapSelection(host);
        }

        private void RefreshBodyMapSelection(IAppearanceEditorHost host)
        {
            var selected = host.IsAppearanceSelected ? host.Session.SelectedSlot
                : host.SelectedItemTypeIndex == 1 ? AppearanceEditorSlot.Head
                : host.SelectedItemTypeIndex == 2 ? AppearanceEditorSlot.Cloak
                : host.Session.SelectedSlot;
            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
                host.SetIfChanged($"Map{slot}Selected", slot == selected);
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

        public void ClickMapSlot(IAppearanceEditorHost host, AppearanceEditorSlot slot)
        {
            host.ToggleItemEquippedFlags();
            if (host.IsAppearanceSelected)
            {
                var category = AppearancePreview.BodyCategory(slot);
                if (category < 0 || host.DoesNotHaveItemEquipped)
                    return;
                host.Session.SelectedSlot = slot;
                if (host.SelectedPartCategoryIndex >= 0 && host.SelectedPartCategoryIndex < host.PartCategorySelected.Count)
                    host.PartCategorySelected[host.SelectedPartCategoryIndex] = false;
                host.SelectedPartCategoryIndex = category;
                host.PartCategorySelected[category] = true;
                host.LoadBodyParts();
                RefreshPartBrowser(host);
                return;
            }

            if (!host.IsEquipmentSelected)
                return;

            var itemType = slot switch
            {
                AppearanceEditorSlot.Head => 1,
                AppearanceEditorSlot.Cloak => 2,
                _ => 0
            };
            if (itemType != 0 && !GetIsObjectValid(GetItemInSlot(
                    itemType == 1 ? InventorySlot.Head : InventorySlot.Cloak, host.Session.Target)))
            {
                // Keep the map; an empty helmet or cloak slot has nothing to edit.
                SendMessageToPC(host.Player, ColorToken.Red($"No {(itemType == 1 ? "helmet" : "cloak")} is equipped."));
                return;
            }
            if (itemType == 0)
                host.Session.SelectedSlot = slot;
            if (host.SelectedItemTypeIndex != itemType)
                host.SelectedItemTypeIndex = itemType;
            if (itemType != 0 || host.DoesNotHaveItemEquipped)
            {
                RefreshBodyMapSelection(host);
                return;
            }

            host.Session.ColorTarget = AppearanceSwatchService.ToColorTarget(slot);
            AppearanceSwatchService.UpdateTargetedColor(host);
            AppearanceSwatchService.SyncSelectedPartSwatches(host);
            AppearanceSwatchService.UpdatePaletteSheetForChannel(host);
            AppearanceTintEditorService.LoadTintMapEditor(host);
            RefreshPartBrowser(host);
        }
    }
}
