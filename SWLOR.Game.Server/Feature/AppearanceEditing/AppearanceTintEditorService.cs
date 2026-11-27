using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    public class AppearanceTintEditorService
    {
        private ITintMapCreatureColorService TintMapCreatureColor { get; }
        private ITintMapItemColorService TintMapItemColor { get; }

        public AppearanceTintEditorService(ITintMapCreatureColorService tintMapCreatureColor, ITintMapItemColorService tintMapItemColor)
        {
            TintMapCreatureColor = tintMapCreatureColor;
            TintMapItemColor = tintMapItemColor;
        }

        public static IReadOnlyList<TintMapMaterialSelection> GetCurrentTintMapSelections(IAppearanceEditorHost host)
        {
            return TintMapModelResolver.GetCurrentSelections(host.Session.Target);
        }

        public static void RefreshTintMapAvailability(IAppearanceEditorHost host)
        {
            host.Session.TintMapSelections = GetCurrentTintMapSelections(host);
            host.IsTintMapAvailable = host.Session.TintMapSelections.Count > 0;
        }

        public void LoadTintMapEditor(IAppearanceEditorHost host)
        {
            // Resetting legacy overrides is part of applying a color. Do not replace the
            // pending input with the old color halfway through that operation.
            if (host.Session.ApplyingTintColor)
                return;
            FlushPendingPickerColor(host);
            CommitCustomTintComponents(host);
            host.Session.TintEditGeneration++;
            host.Session.TintComponentCorrection = null;
            host.Session.HasTintComponentDraft = false;
            RefreshTintMapAvailability(host);
            host.RefreshBodySwatches();
            host.RefreshItemBaseSwatches();
            if (!TryGetEditableTintSelections(host, out var selections, out var layerType, out _))
            {
                host.IsCustomTintAvailable = false;
                host.IsCustomTintEditable = false;
                SetSelectedTintColor(host, GuiColor.Grey);
                return;
            }

            host.IsCustomTintAvailable = true;
            host.IsCustomTintEditable = selections.Count > 0 &&
                                   selections.All(SupportsRgb);
            host.CustomTintTooltip = host.IsCustomTintEditable ? "Apply an RGB color."
                : selections.Any(selection => selection.ArmorPart == AppearanceArmor.Robe)
                    ? "This body and robe combination supports preset colors only. Select a color from the palette above."
                    : selections.Any(selection => selection.IsWornHelmet)
                        ? "This helmet supports preset colors only on this species. Select a color from the palette above."
                        : "This part has no visible material for this color.";
            if (TryGetSelectedCustomColor(host, selections, layerType, out var customColor))
            {
                SetSelectedTintColor(host, new GuiColor(customColor.Red, customColor.Green, customColor.Blue));
                return;
            }
            if (host.IsEquipmentSelected && host.SelectedItemTypeIndex == 0)
            {
                // Native armor colors remain editable even when the current model does not
                // expose that channel. Empty part swatches inherit the global default.
                var paletteId = host.Session.ColorTarget == AppearanceColorTarget.Global
                    ? 255
                    : AppearanceColorService.GetArmorSwatchColor(host, host.GetItem(), AppearancePartEditService.GetArmorModelType(host, host.Session.ColorTarget), host.Session.SelectedColorChannel);
                if (paletteId == 255)
                    paletteId = GetItemAppearance(host.GetItem(), ItemAppearanceType.ArmorColor, (int)host.Session.SelectedColorChannel);
                var globalColor = TintMapPaletteColors.GetColor(layerType, paletteId);
                SetLoadedTintColor(host, globalColor, layerType, paletteId);
                return;
            }
            if (selections.Count == 0)
            {
                var paletteId = host.IsAppearanceSelected
                    ? GetColor(host.Session.Target, (ColorChannel)host.SelectedColorCategoryIndex)
                    : GetItemAppearance(host.GetItem(), ItemAppearanceType.ArmorColor, host.SelectedColorCategoryIndex);
                var fallbackColor = TintMapPaletteColors.GetColor(layerType, paletteId);
                SetLoadedTintColor(host, fallbackColor, layerType, paletteId);
                return;
            }
            var effectiveColors = selections
                .Select(selection => TintMapColorResolver.GetEffectiveDisplayColor(
                    host.Session.Target,
                    selection,
                    layerType))
                .ToList();
            var distinctColors = effectiveColors
                .Distinct()
                .ToList();

            if (distinctColors.Count == 1)
            {
                var color = distinctColors[0];
                var paletteId = host.IsAppearanceSelected
                    ? GetColor(host.Session.Target, (ColorChannel)host.SelectedColorCategoryIndex)
                    : GetItemAppearance(host.GetItem(), ItemAppearanceType.ArmorColor, host.SelectedColorCategoryIndex);
                SetLoadedTintColor(host, color, layerType, paletteId);
                return;
            }

            SetSelectedTintColor(host, GuiColor.Grey);
        }

        public static void SetLoadedTintColor(IAppearanceEditorHost host, TintMapColor color, TintMapLayerType layerType, int paletteId)
        {
            SetSelectedTintColor(host, new GuiColor(color.Red, color.Green, color.Blue));
        }

        public static bool SupportsRgb(TintMapMaterialSelection selection) =>
            RobeModelRenderer.SupportsRgb(selection) && HelmetModelRenderer.SupportsRgb(selection);

        public static bool TryGetSelectedCustomColor(IAppearanceEditorHost host, IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer, out TintMapColor color)
        {
            if (host.IsAppearanceSelected)
                return TintMapColor.TryFromStoredValue(
                    GetLocalInt(host.Session.Target, TintMapVariable.GetCreatureColorStateName(layer)), out color);
            if (host.SelectedItemTypeIndex != 0 || host.Session.ColorTarget == AppearanceColorTarget.Global)
                return TintMapColor.TryFromStoredValue(
                    GetLocalInt(host.GetItem(), TintMapVariable.GetItemGlobalColorStateName(layer)), out color);
            var customColors = selections
                .Where(selection => TintMapColorResolver.TryGetCustomColor(selection, layer, out _))
                .Select(selection => TintMapColorResolver.GetEffectiveDisplayColor(host.Session.Target, selection, layer))
                .Distinct().ToList();
            color = customColors.Count == 1 ? customColors[0] : default;
            return customColors.Count == 1;
        }

        public static void SetSelectedTintColor(IAppearanceEditorHost host, 
            GuiColor color,
            bool synchronizeComponents = true)
        {
            CancelTintPickerGesture(host);
            SynchronizeTintControlBindings(host, () =>
            {
                host.Session.LoadingTintColor = true;
                try
                {
                    if (synchronizeComponents)
                        host.SelectedTintColor = color;
                    else
                        host.Set(color, AppearanceEditorBindings.SelectedTintColor);
                }
                finally
                {
                    host.Session.LoadingTintColor = false;
                }
            });
        }

        public void ApplyCustomTintColor(IAppearanceEditorHost host, 
            GuiColor value,
            bool synchronizeComponents,
            bool synchronizePicker = true,
            Action<TintMapColor> capturedEdit = null)
        {
            if (host.Session.LoadingTintColor)
            {
                if (synchronizeComponents)
                    SynchronizeCustomTintComponents(host, value);
                return;
            }
            var apply = capturedEdit ?? CaptureTintColorEdit(host);
            if (apply == null)
            {
                LoadTintMapEditor(host);
                return;
            }

            var requestedColor = new TintMapColor(value.R, value.G, value.B);
            host.Session.ApplyingTintColor = true;
            try
            {
                apply(requestedColor);
                if (host.IsEquipmentSelected && host.SelectedItemTypeIndex == 0)
                    host.UpdateEditedColorSwatches();
            }
            finally
            {
                host.Session.ApplyingTintColor = false;
            }

            // Persist the actual RGB on the creature/item. Never change a native preset,
            // replace equipment, or echo normalized text into fields while the user types.
            if (synchronizePicker)
                SetSelectedTintColor(host, value, synchronizeComponents);
            else if (synchronizeComponents)
                SynchronizeCustomTintComponents(host, value);
        }

        public Action<TintMapColor> CaptureTintColorEdit(IAppearanceEditorHost host)
        {
            if (!TryGetEditableTintSelections(host, out var selections, out var layerType, out _) ||
                selections.Count == 0 || !selections.All(SupportsRgb))
                return null;

            // NUI hydrates selection binds before their setters run. A pending text edit
            // must retain its original layer and owner when the next selection loads.
            var target = host.Session.Target;
            var appearance = host.IsAppearanceSelected;
            var globalItem = host.SelectedItemTypeIndex != 0 || host.Session.ColorTarget == AppearanceColorTarget.Global;
            var item = appearance ? OBJECT_INVALID : host.GetItem();
            return color =>
            {
                if (!GetIsObjectValid(target) || !appearance && !GetIsObjectValid(item))
                    return;
                if (appearance)
                    TintMapCreatureColor.SetCreatureCustomColor(target, selections, layerType, color);
                else if (globalItem)
                    TintMapItemColor.SetGlobalItemCustomColor(target, selections, layerType, color, item);
                else
                    foreach (var selection in selections)
                        TintMapItemColor.SetColor(target, selection, layerType, color);
            };
        }

        public static void SynchronizeCustomTintComponents(IAppearanceEditorHost host, GuiColor color)
        {
            SynchronizeTintControlBindings(host, () =>
            {
                var wasLoading = host.Session.LoadingTintColor;
                host.Session.LoadingTintColor = true;
                try
                {
                    host.Set(color.R.ToString(), AppearanceEditorBindings.CustomTintRed);
                    host.Set(color.G.ToString(), AppearanceEditorBindings.CustomTintGreen);
                    host.Set(color.B.ToString(), AppearanceEditorBindings.CustomTintBlue);
                }
                finally
                {
                    host.Session.LoadingTintColor = wasLoading;
                }
            });
        }

        public static void SynchronizeTintControlBindings(IAppearanceEditorHost host, Action update)
        {
            var restoreWatches = host.Session.TintControlBindingsWatched;
            if (restoreWatches)
                SetTintControlBindingsWatched(host, false);

            try
            {
                update();
            }
            finally
            {
                if (restoreWatches)
                    SetTintControlBindingsWatched(host, true);
            }
        }

        public static void SetTintControlBindingsWatched(IAppearanceEditorHost host, bool watched)
        {
            NuiSetBindWatch(host.Player, host.WindowToken, AppearanceEditorBindings.SelectedTintColor, watched);
            NuiSetBindWatch(host.Player, host.WindowToken, AppearanceEditorBindings.CustomTintRed, watched);
            NuiSetBindWatch(host.Player, host.WindowToken, AppearanceEditorBindings.CustomTintGreen, watched);
            NuiSetBindWatch(host.Player, host.WindowToken, AppearanceEditorBindings.CustomTintBlue, watched);
            host.Session.TintControlBindingsWatched = watched;
        }

        public void SetCustomTintComponent(IAppearanceEditorHost host, string value, string propertyName)
        {
            host.Session.PendingPickerColor = null;
            var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
            var normalized = int.TryParse(digits, out var component)
                ? component <= byte.MaxValue ? digits : byte.MaxValue.ToString()
                : string.Empty;
            host.Set(normalized, propertyName);

            if (host.Session.LoadingTintColor)
                return;

            host.Session.TintComponentCorrection = normalized == value ? null : propertyName;
            if (!host.Session.HasTintComponentDraft)
                host.Session.TintComponentApply = CaptureTintColorEdit(host);
            host.Session.HasTintComponentDraft = true;

            // Keep the three inputs as a draft while typing; applying each digit used to
            // reload the old color and overwrite the other two channels mid-edit.
            var generation = ++host.Session.TintEditGeneration;
            var token = host.WindowToken;
            DelayCommand(0.4f, () =>
            {
                if (generation == host.Session.TintEditGeneration && token == host.WindowToken &&
                    host.IsWindowOpen())
                    CommitCustomTintComponents(host);
            });
        }

        public void CommitCustomTintComponents(IAppearanceEditorHost host)
        {
            if (!host.Session.HasTintComponentDraft || !byte.TryParse(host.CustomTintRed, out var red) ||
                !byte.TryParse(host.CustomTintGreen, out var green) ||
                !byte.TryParse(host.CustomTintBlue, out var blue))
                return;
            host.Session.HasTintComponentDraft = false;
            host.Session.TintEditGeneration++;
            var apply = host.Session.TintComponentApply;
            host.Session.TintComponentApply = null;
            if (apply != null)
                ApplyCustomTintColor(host, new GuiColor(red, green, blue), synchronizeComponents: false, capturedEdit: apply);
        }

        public static bool TryGetEditableTintSelections(IAppearanceEditorHost host, 
            out IReadOnlyList<TintMapMaterialSelection> selections,
            out TintMapLayerType layerType,
            out TintMapLayerDefinition layer)
        {
            selections = Array.Empty<TintMapMaterialSelection>();
            layerType = default;
            layer = null;

            if (!TryGetSelectedTintLayer(host, out layerType))
                return false;

            if (host.IsEquipmentSelected && !host.IsValidItem())
                return false;

            var paletteSource = host.IsAppearanceSelected ? host.Session.Target : host.GetItem();
            if (!GetIsObjectValid(paletteSource))
                return false;

            var restrictToArmorPart = host.IsEquipmentSelected &&
                                      host.SelectedItemTypeIndex == 0 &&
                                      host.Session.ColorTarget != AppearanceColorTarget.Global;
            var armorPart = restrictToArmorPart
                ? AppearancePartEditService.GetArmorModelType(host, host.Session.ColorTarget)
                : AppearanceArmor.Invalid;
            var selectedLayerType = layerType;
            selections = host.Session.TintMapSelections
                .Where(selection =>
                    selection.GetPaletteSource(selectedLayerType) == paletteSource &&
                    selection.Material.Layers.Contains(selectedLayerType) &&
                    (!restrictToArmorPart || selection.ArmorPart == armorPart))
                .ToList();
            // The native channel exists independently of the currently visible material.
            layer = TintMapMaterialRegistry.GetLayer(layerType);
            return true;
        }

        public static bool TryGetSelectedTintLayer(IAppearanceEditorHost host, out TintMapLayerType layerType)
        {
            layerType = default;
            if (host.IsAppearanceSelected)
            {
                layerType = host.SelectedColorCategoryIndex switch
                {
                    0 => TintMapLayerType.Skin,
                    1 => TintMapLayerType.Hair,
                    2 => TintMapLayerType.Tattoo1,
                    3 => TintMapLayerType.Tattoo2,
                    _ => default
                };
                return host.SelectedColorCategoryIndex is >= 0 and <= 3;
            }

            if (!host.IsEquipmentSelected)
                return false;

            if (host.SelectedItemTypeIndex == 0)
            {
                if (host.Session.ColorTarget == AppearanceColorTarget.Invalid)
                    return false;

                layerType = host.Session.SelectedColorChannel switch
                {
                    AppearanceArmorColor.Leather1 => TintMapLayerType.Leather1,
                    AppearanceArmorColor.Leather2 => TintMapLayerType.Leather2,
                    AppearanceArmorColor.Cloth1 => TintMapLayerType.Cloth1,
                    AppearanceArmorColor.Cloth2 => TintMapLayerType.Cloth2,
                    AppearanceArmorColor.Metal1 => TintMapLayerType.Metal1,
                    AppearanceArmorColor.Metal2 => TintMapLayerType.Metal2,
                    _ => default
                };
                return host.Session.SelectedColorChannel is >= AppearanceArmorColor.Leather1 and <= AppearanceArmorColor.Metal2;
            }

            if (host.SelectedItemTypeIndex is not (1 or 2))
                return false;

            layerType = host.SelectedColorCategoryIndex switch
            {
                0 => TintMapLayerType.Leather1,
                1 => TintMapLayerType.Leather2,
                2 => TintMapLayerType.Cloth1,
                3 => TintMapLayerType.Cloth2,
                4 => TintMapLayerType.Metal1,
                5 => TintMapLayerType.Metal2,
                _ => default
            };
            return host.SelectedColorCategoryIndex is >= 0 and <= 5;
        }

        public void BeginTintPickerGesture(IAppearanceEditorHost host, NuiMouseButton button, Action<TintMapColor> apply)
        {
            var color = host.Session.UnclaimedPickerColor;
            host.Session.UnclaimedPickerColor = null;
            host.Session.UnclaimedPickerGeneration++;
            if (button == NuiMouseButton.Left)
            {
                host.Session.TintPickerActive = true;
                host.Session.TintPickerApply = apply;
                if (color != null)
                {
                    QueueTintPickerColor(host, color);
                    FlushPendingPickerColor(host);
                }
            }
        }

        public static Action StageTintPickerColor(IAppearanceEditorHost host, GuiColor color)
        {
            // The native picker sends a click's color watch before mousedown.
            // Hold it briefly for that press, without applying unsolicited hydration.
            host.Session.UnclaimedPickerColor = color;
            var generation = ++host.Session.UnclaimedPickerGeneration;
            var token = host.WindowToken;
            return () =>
            {
                if (generation == host.Session.UnclaimedPickerGeneration && token == host.WindowToken)
                    host.Session.UnclaimedPickerColor = null;
            };
        }

        public static bool QueueTintPickerColor(IAppearanceEditorHost host, GuiColor value)
        {
            // A newly created NUI picker can report its default black value. Only
            // an explicit pointer gesture may turn a watched value into a tint edit.
            if (!host.Session.TintPickerActive)
                return false;

            host.Session.TintEditGeneration++;
            host.Session.TintComponentCorrection = null;
            host.Session.HasTintComponentDraft = false;
            host.Session.PendingPickerColor = value;
            host.Session.PendingPickerApply = host.Session.TintPickerApply;
            return true;
        }

        public void ReleaseTintPickerGesture(IAppearanceEditorHost host, NuiMouseButton button)
        {
            if (button != NuiMouseButton.Left || !host.Session.TintPickerActive)
                return;

            FlushPendingPickerColor(host);
            CancelTintPickerGesture(host);
        }

        public static void CancelTintPickerGesture(IAppearanceEditorHost host)
        {
            host.Session.UnclaimedPickerColor = null;
            host.Session.UnclaimedPickerGeneration++;
            host.Session.TintPickerActive = false;
            host.Session.TintPickerApply = null;
            host.Session.PendingPickerColor = null;
            host.Session.PendingPickerApply = null;
        }

        public void FlushPendingPickerColor(IAppearanceEditorHost host)
        {
            var color = host.Session.PendingPickerColor;
            var apply = host.Session.PendingPickerApply;
            host.Session.PendingPickerColor = null;
            host.Session.PendingPickerApply = null;
            if (color != null && apply != null)
                ApplyCustomTintColor(host, color, synchronizeComponents: true, synchronizePicker: false, capturedEdit: apply);
        }

        public static void StartArmorClientWatches(IAppearanceEditorHost host)
        {
            if (!host.IsWindowOpen() || !host.IsEquipmentSelected ||
                host.SelectedItemTypeIndex != 0 || !host.HasItemEquipped)
                return;
            host.WatchArmorSelections();
            host.Session.ArmorClientBindingsWatched = true;
        }

        public static void SuspendArmorClientWatches(IAppearanceEditorHost host)
        {
            host.Session.ArmorBindingGeneration++;
            if (!host.Session.ArmorClientBindingsWatched)
                return;
            foreach (var target in ArmorColorRegionCatalog.Targets.Where(target => target != AppearanceColorTarget.Global))
                NuiSetBindWatch(host.Player, host.WindowToken, target + "Selection", false);
            host.Session.ArmorClientBindingsWatched = false;
        }

        public static void ResumeArmorClientWatches(IAppearanceEditorHost host)
        {
            var generation = host.Session.ArmorBindingGeneration;
            // Option-list replacement can transiently select the first client combo entry.
            // Keep that hydration outside the watched editing path, including repeated tab visits.
            DelayCommand(3f, () =>
            {
                if (generation == host.Session.ArmorBindingGeneration)
                    StartArmorClientWatches(host);
            });
        }
    }
}
