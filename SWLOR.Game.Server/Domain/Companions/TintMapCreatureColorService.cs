using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Sets, resets and carries the colours of a creature's own layers (skin, hair, tattoos), and keeps a
    /// player's colours in the player record.
    /// </summary>
    public class TintMapCreatureColorService : ITintMapCreatureColorService
    {
        private readonly ITintMapApplyService _apply;
        private readonly IDBService _db;
        private readonly ITintMapDroidOverrideService _droidOverrides;

        public TintMapCreatureColorService(
            ITintMapApplyService apply,
            IDBService db,
            ITintMapDroidOverrideService droidOverrides)
        {
            _apply = apply;
            _db = db;
            _droidOverrides = droidOverrides;
        }

        public void CarryStoredCreatureCustomColors(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            var colors = new Dictionary<TintMapLayerType, TintMapColor>();
            foreach (var layer in Enum.GetValues<TintMapLayerType>())
            {
                if (!TintMapVariable.IsCreatureColorLayer(layer) ||
                    !TintMapColor.TryFromStoredValue(
                        GetLocalInt(creature, TintMapColorResolver.GetCreatureCustomColorStateVariable(layer)),
                        out var color))
                {
                    continue;
                }

                colors[layer] = color;
            }

            ApplyCreatureCustomColors(creature, colors);
        }

        public void ResetCreatureCustomColor(uint creature, TintMapLayerType layer)
        {
            if (!GetIsObjectValid(creature) || !TintMapVariable.IsCreatureColorLayer(layer))
                return;

            var variableNames = TintMapColorResolver.GetCreatureCustomColorVariables(creature)
                .Where(variableName =>
                    TintMapVariable.TryGetLayer(variableName, out var variableLayer) &&
                    variableLayer == layer)
                .ToList();
            variableNames.Add(TintMapColorResolver.GetCreatureCustomColorStateVariable(layer));
            foreach (var variableName in variableNames)
            {
                DeleteLocalInt(creature, variableName);
            }

            _droidOverrides.RemoveDroidOverrides(creature, variableNames);
            SavePlayerOverrides(creature);
            _apply.ApplyCurrentColorsAndPublish(creature);
        }

        public void SetCreatureCustomColor(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer,
            TintMapColor color)
        {
            if (!GetIsObjectValid(creature) ||
                selections == null ||
                !TintMapVariable.IsCreatureColorLayer(layer))
            {
                return;
            }

            var savedColor = color.ToStoredValue();
            var stateVariable = TintMapColorResolver.GetCreatureCustomColorStateVariable(layer);
            SetLocalInt(creature, stateVariable, savedColor);
            var variableNames = TintMapColorResolver.GetCreatureCustomColorVariables(creature)
                .Where(variableName =>
                    TintMapVariable.TryGetLayer(variableName, out var variableLayer) &&
                    variableLayer == layer)
                .ToHashSet(StringComparer.Ordinal);

            // The NUI can remain open while an equipment or body-part replacement completes.
            // Persist every currently rendered semantic material so model swaps and droid
            // snapshots retain the edit. ApplyCurrentColors publishes the semantic layer through
            // the model-wide material target without rebuilding the creature appearance.
            var currentSelections = TintMapModelResolver.GetCurrentSelections(creature);
            foreach (var selection in selections.Concat(currentSelections))
            {
                if (selection.GetPaletteSource(layer) != creature ||
                    !selection.Material.Layers.Contains(layer))
                {
                    continue;
                }

                var variableName = TintMapVariable.GetName(selection.Material.Resref, layer);
                variableNames.Add(variableName);
            }

            foreach (var variableName in variableNames)
            {
                SetLocalInt(creature, variableName, savedColor);
            }

            _droidOverrides.SaveDroidOverrides(
                creature,
                variableNames.Concat(new[] { stateVariable }).ToList(),
                savedColor);
            SavePlayerOverrides(creature);

            // Creature colors are semantic across the whole modular model: every registered
            // material whose tint mask uses this layer must receive the same value. The enabled
            // material-name-null tweak publishes one authoritative model-wide row.
            _apply.RefreshAfterColorChange(creature);
        }

        private void ApplyCreatureCustomColors(
            uint creature,
            IReadOnlyDictionary<TintMapLayerType, TintMapColor> colors)
        {
            if (colors.Count == 0)
                return;

            var selections = TintMapModelResolver.GetCurrentSelections(creature);
            foreach (var (layer, color) in colors)
            {
                SetCreatureCustomColor(creature, selections, layer, color);
            }
        }

        private void SavePlayerOverrides(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var player = _db.Get<Entity.Player>(GetObjectUUID(creature));
            if (player == null)
                return;

            var colors = TintMapColorResolver.GetCreatureTintSnapshotVariables(creature)
                .Select(name => new KeyValuePair<string, int>(name, GetLocalInt(creature, name)))
                .Where(entry => entry.Value > 0)
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            if (UpdatePlayerTintOverrides(player, colors))
                _db.Set(player);
        }

        private static bool UpdatePlayerTintOverrides(
            Entity.Player player, IReadOnlyDictionary<string, int> colors)
        {
            if (player.CreatureTintOverrides != null &&
                player.CreatureTintOverrides.Count == colors.Count &&
                colors.All(entry => player.CreatureTintOverrides.TryGetValue(entry.Key, out var saved) &&
                                    saved == entry.Value))
                return false;

            player.CreatureTintOverrides = new Dictionary<string, int>(colors, StringComparer.Ordinal);
            return true;
        }

        public void RestorePlayerOverrides(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var player = _db.Get<Entity.Player>(GetObjectUUID(creature));
            if (player?.CreatureTintOverrides == null)
                return;

            // A saved clear must win over stale locals from an older character export.
            foreach (var name in TintMapColorResolver.GetCreatureTintSnapshotVariables(creature).ToList())
                DeleteLocalInt(creature, name);
            foreach (var (name, value) in player.CreatureTintOverrides)
            {
                if (value > 0 && (TintMapVariable.IsCreatureColorStateName(name) ||
                    TintMapVariable.TryGetLayer(name, out var layer) && TintMapVariable.IsCreatureColorLayer(layer)))
                    SetLocalInt(creature, name, value);
            }
        }
    }
}
