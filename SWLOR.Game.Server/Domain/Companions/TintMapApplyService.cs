using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Writes the colours a creature, its worn items and a dropped item should show to the engine's material
    /// uniforms, and projects the native palette for robes.
    /// </summary>
    public class TintMapApplyService : ITintMapApplyService
    {
        /// <summary>How long a queued refresh waits so the engine has finished the change that caused it.</summary>
        public const float RefreshDelaySeconds = 0.2f;

        // Clients reload the materials when a coating effect message arrives; resend after that.
        private const float CoatRefreshDelaySeconds = 0.5f;

        private readonly IDroidService _droid;
        private readonly IProgFxCoatService _progFxCoat;
        private readonly ITwinPistolEquipmentService _twinPistolEquipment;

        public TintMapApplyService(IDroidService droid, IProgFxCoatService progFxCoat, ITwinPistolEquipmentService twinPistolEquipment)
        {
            _droid = droid;
            _progFxCoat = progFxCoat;
            _twinPistolEquipment = twinPistolEquipment;
        }

        public void QueueCoatRefresh(uint creature)
        {
            DelayCommand(CoatRefreshDelaySeconds, () => RefreshAfterCoatChange(creature));
        }

        private void RefreshAfterCoatChange(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            // Plain creature models take the client's own coat and have no tint state to resend.
            if (TintMapModelResolver.GetCurrentSelections(creature).Count == 0)
                return;

            // Applying or removing an environment-map effect makes the client reload the creature's
            // materials, which drops every shader parameter, dyes included. Writing only the coat
            // left parts at their default palette rows, so resend the complete tint state.
            ApplyCurrentColors(creature);
            NWNXLib.g_pAppManager.m_pServerExoApp.SetForceUpdate();
        }

        public void ApplyCurrentColors(uint creature, bool resetShaderOverrides = true)
        {
            if (!GetIsObjectValid(creature))
                return;

            PowerGloveFit.Apply(creature);
            HornFit.Apply(creature);
            _twinPistolEquipment.RefreshAppearance(creature);
            WeaponHandFit.Apply(creature);

            // Rebuild the complete tint state in one pass. Parameter-scoped wildcard resets are
            // not reliable for modular creatures: on the game client they can discard palette
            // rows already written for other layers, which leaves equipped clothing at row zero.
            // A complete reset also removes legacy wildcard values left by older implementations
            // before the current material-scoped values are installed.
            var selections = TintMapModelResolver.GetCurrentSelections(creature);
            var rendersRobeRgb = RobeModelRenderer.Apply(creature, selections);
            HelmetModelRenderer.Apply(creature, selections);
            NeckModelRenderer.Apply(creature);
            ShoulderModelRenderer.Apply(creature);
            PartSkinModelRenderer.Apply(creature);
            ProjectNativeRobeColors(creature, selections, rendersRobeRgb);
            ApplyEquippedHelmetColors(creature, selections, resetShaderOverrides);
            if (resetShaderOverrides)
                ResetMaterialShaderUniforms(creature);
            CloakTintRenderer.Apply(creature, selections);
            var creatureLayers = new HashSet<TintMapLayerType>();
            foreach (var selection in selections)
            {
                foreach (var layer in selection.Material.Layers)
                {
                    if (TintMapVariable.IsCreatureColorLayer(layer))
                    {
                        creatureLayers.Add(layer);
                        continue;
                    }

                    WriteMaterialColor(
                        creature,
                        selection.Material.Resref,
                        layer,
                        TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
                }
            }

            foreach (var layer in creatureLayers)
            {
                ApplyCreatureColor(
                    creature,
                    selections,
                    layer,
                    TintMapColorResolver.GetEffectiveCreatureColor(creature, layer));
            }

            // Install authored material exceptions after the model-wide semantic defaults.
            // Toolset edits retire TMC_* and preserve untouched sibling TM_* colors.
            foreach (var selection in selections)
            {
                foreach (var layer in selection.Material.Layers.Where(TintMapVariable.IsCreatureColorLayer))
                {
                    var saved = TintMapColorResolver.GetSavedColor(selection, layer);
                    if (saved > 0)
                        WriteMaterialColor(creature, selection.Material.Resref, layer,
                            TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
                }
            }

            _progFxCoat.Apply(creature, selections, resetShaderOverrides);
        }

        private void ProjectNativeRobeColors(uint creature, IReadOnlyList<TintMapMaterialSelection> selections,
            bool rendersRobeRgb)
        {
            // The client replays material uniforms on the body/head/attachments, but omits its
            // separate robe Gob. Tiny PLT resources carry these native palette values to the shader.
            var robes = selections.Where(selection => !rendersRobeRgb && selection.ArmorPart == AppearanceArmor.Robe).ToList();
            var creatureStateChanged = false;
            foreach (var layer in Enum.GetValues<TintMapLayerType>().Where(TintMapVariable.IsCreatureColorLayer))
            {
                var channel = TintMapColorResolver.GetCreatureColorChannel(layer);
                var nativeColor = GetColor(creature, channel);
                var active = robes.Any(selection => selection.Material.Layers.Contains(layer)) &&
                             TintMapColor.TryFromStoredValue(
                                 GetLocalInt(creature, TintMapColorResolver.GetCreatureCustomColorStateVariable(layer)), out _);
                var update = ResolveNativePaletteUpdate(creature, (int)layer, nativeColor,
                    active ? TintMapColorResolver.GetEffectiveCreatureColor(creature, layer).PaletteColorId : null);
                creatureStateChanged |= StoreNativePaletteUpdate(creature, (int)layer, update);
                if (update.Color != nativeColor)
                    SWLOR.NWN.API.NWScript.NWScript.SetColor(creature, channel, update.Color);
            }

            if (creatureStateChanged && _droid.IsDroid(creature))
            {
                var controller = _droid.GetControllerItem(creature);
                if (GetIsObjectValid(controller))
                {
                    var droid = _droid.LoadConstructedDroid(controller);
                    foreach (var layer in Enum.GetValues<TintMapLayerType>().Where(TintMapVariable.IsCreatureColorLayer))
                    {
                        foreach (var name in new[] { TintMapNativePaletteProjection.BaselineName((int)layer),
                                     TintMapNativePaletteProjection.LastAppliedName((int)layer) })
                        {
                            var value = GetLocalInt(creature, name);
                            if (value > 0)
                                droid.TintOverrides[name] = value;
                            else
                                droid.TintOverrides.Remove(name);
                        }
                    }
                    _droid.SaveConstructedDroid(controller, droid);
                }
            }

            var item = GetItemInSlot(InventorySlot.Chest, creature);
            if (!GetIsObjectValid(item))
                return;

            var changes = new List<(int Index, int Color)>();
            var itemStateChanged = false;
            foreach (var layer in Enum.GetValues<TintMapLayerType>().Where(layer => !TintMapVariable.IsCreatureColorLayer(layer)))
            {
                if (!TintMapColorResolver.TryGetArmorColorChannel(layer, out var channel))
                    continue;
                var colorIndex = ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Robe, channel);
                var nativeColor = GetItemAppearance(item, ItemAppearanceType.ArmorColor, colorIndex);
                var hasExplicitPreset = GetLocalInt(item,
                    ArmorColorIndexCalculator.GetPerPartOverrideVariableName(AppearanceArmor.Robe, channel)) > 0;
                var inheritedColor = ArmorColorIndexCalculator.ShouldUsePerPartColor(nativeColor, hasExplicitPreset)
                    ? nativeColor : 255;
                var selection = robes.FirstOrDefault(selection => selection.PaletteSource == item &&
                    selection.Material.Layers.Contains(layer) && TintMapColorResolver.GetSavedColor(selection, layer) > 0);
                var update = ResolveNativePaletteUpdate(item, colorIndex, nativeColor,
                    selection == null ? null : TintMapColorResolver.GetEffectiveColor(creature, selection, layer).PaletteColorId,
                    inheritedColor);
                itemStateChanged |= StoreNativePaletteUpdate(item, colorIndex, update);
                if (update.Color != nativeColor)
                    changes.Add((colorIndex, update.Color));
            }

            for (var index = 0; index < changes.Count; index++)
            {
                var change = changes[index];
                ItemPlugin.SetItemAppearance(item, ItemAppearanceType.ArmorColor, change.Index, change.Color,
                    updateCreatureAppearance: index == changes.Count - 1);
            }
            if (changes.Count > 0 && GetIsPC(creature))
            {
                // NWNX refreshes the client item without changing ownership. Its destroy packet
                // clears client quickbar references; resend only still-current server references.
                DelayCommand(RefreshDelaySeconds, () => RestoreNativePaletteQuickbar(creature, item));
            }
            if (itemStateChanged || changes.Count > 0)
                _droid.UpdateEquippedItemSnapshot(creature, item);
        }

        private void ApplyEquippedHelmetColors(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            bool resetShaderOverrides = true)
        {
            var helmet = GetItemInSlot(InventorySlot.Head, creature);
            if (!GetIsObjectValid(helmet))
                return;
            // The client does not replay creature rows onto its worn helmet attachment.
            // Publish the exact same effective colors on the equipped item itself.
            if (resetShaderOverrides)
                ResetMaterialShaderUniforms(helmet);
            foreach (var selection in selections.Where(selection => selection.IsWornHelmet))
            {
                foreach (var layer in selection.Material.Layers)
                    WriteMaterialColor(helmet, selection.Material.Resref, layer,
                        TintMapColorResolver.GetEffectiveColor(creature, selection, layer));
            }
        }

        private void RestoreNativePaletteQuickbar(uint creature, uint item)
        {
            if (!GetIsObjectValid(creature) || !GetIsPC(creature) || !GetIsObjectValid(item))
                return;
            for (var slot = 0; slot < 36; slot++)
            {
                var entry = PlayerPlugin.GetQuickBarSlot(creature, slot);
                if (entry.Item == item || entry.SecondaryItem == item)
                    PlayerPlugin.SetQuickBarSlot(creature, slot, entry);
            }
        }

        private TintMapNativePaletteProjection.Update ResolveNativePaletteUpdate(
            uint target, int channel, int nativeColor, int? projectedColor, int? inheritedColor = null)
        {
            return TintMapNativePaletteProjection.Resolve(nativeColor,
                GetLocalInt(target, TintMapNativePaletteProjection.BaselineName(channel)),
                GetLocalInt(target, TintMapNativePaletteProjection.LastAppliedName(channel)),
                projectedColor, inheritedColor);
        }

        private bool StoreNativePaletteUpdate(uint target, int channel, TintMapNativePaletteProjection.Update update)
        {
            var changed = false;
            foreach (var (name, value) in new[]
                     {
                         (TintMapNativePaletteProjection.BaselineName(channel), update.Baseline),
                         (TintMapNativePaletteProjection.LastAppliedName(channel), update.LastApplied)
                     })
            {
                if (GetLocalInt(target, name) == value)
                    continue;
                changed = true;
                if (value > 0)
                    SetLocalInt(target, name, value);
                else
                    DeleteLocalInt(target, name);
            }
            return changed;
        }

        public void ApplyCurrentItemColors(uint item)
        {
            if (!GetIsObjectValid(item) ||
                GetObjectType(item) != ObjectType.Item ||
                GetIsObjectValid(GetItemPossessor(item)))
            {
                return;
            }

            ResetMaterialShaderUniforms(item);

            foreach (var selection in TintMapModelResolver.GetWorldItemSelections(item))
            {
                foreach (var layer in selection.Material.Layers)
                {
                    WriteMaterialColor(
                        item,
                        selection.Material.Resref,
                        layer,
                        TintMapColorResolver.GetEffectiveColor(OBJECT_INVALID, selection, layer));
                }
            }
        }

        public void RefreshAfterColorChange(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            ApplyCurrentColorsAndPublish(creature);

            // Native palette changes and RGB-driven phenotype changes can replace client meshes
            // after the immediate update. Read current state again rather than replaying the edit's
            // captured color or materials, so a newer edit or part change always wins.
            DelayCommand(RefreshDelaySeconds, () =>
            {
                if (!GetIsObjectValid(creature))
                    return;

                ApplyCurrentColorsAndPublish(creature);
            });
        }

        public void ApplyColor(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            WriteMaterialColor(creature, selection.Material.Resref, layer, color);
        }

        private void ApplyCreatureColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            var materialResrefs = selections
                .Where(selection =>
                    selection.GetPaletteSource(layer) == creature &&
                    selection.Material.Layers.Contains(layer))
                .Select(selection => selection.Material.Resref)
                .Where(materialResref => !string.IsNullOrWhiteSpace(materialResref))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (materialResrefs.Count == 0)
            {
                return;
            }

            // The material-name-null tweak expands this one semantic row across every composed
            // child mesh. This is the same model-wide update used by the known-good PLT tinter.
            WriteMaterialColor(creature, string.Empty, layer, color);

            // Keep explicit records for the resolved attachments as well. Equipment changes
            // can replace a child mesh after the blanket update has been applied. Its material
            // must have the same semantic color when the client installs that replacement.
            // Write these after the wildcard: the native tweak removes matching named rows
            // when it receives a wildcard. Authored exceptions are applied by the caller last.
            foreach (var materialResref in materialResrefs)
            {
                WriteMaterialColor(creature, materialResref, layer, color);
            }
        }

        public void ApplyCurrentColorsAndPublish(uint creature)
        {
            // Individual picker edits update only the tint rows. A full NWNX reset also
            // clears unrelated shader overrides on the creature and worn attachments.
            ApplyCurrentColors(creature, resetShaderOverrides: false);
            NWNXLib.g_pAppManager.m_pServerExoApp.SetForceUpdate();
        }

        private void WriteMaterialColor(
            uint creature,
            string materialResref,
            TintMapLayerType layer,
            TintMapColorSelection color)
        {
            // Complete refreshes clear legacy parameters once, then only write rows. The native
            // material-name-null tweak replaces existing matching rows without needing a reset.
            // A scoped reset leaves a type-zero record which restores every material parameter
            // on the client, discarding other colors written earlier in that update.
            var layerDefinition = TintMapMaterialRegistry.GetLayer(layer);
            var shaderColor = color.CustomColor.HasValue
                ? TintMapShaderColor.Encode(color.CustomColor.Value)
                : TintMapMaterialRegistry.GetPaletteCoordinate(layer, color.PaletteColorId);

            // Preserve the proven scalar MTR transport. RGB occupies a separate lossless
            // range; palette rows and the native robe fallback retain their existing values.
            SetMaterialShaderUniformVec4(
                creature,
                materialResref,
                layerDefinition.UniformName,
                shaderColor);
        }
    }
}
