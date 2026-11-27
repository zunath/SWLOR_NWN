using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Keeps a droid's saved tint overrides in step with the colours its controller item records.
    /// </summary>
    public class TintMapDroidOverrideService : ITintMapDroidOverrideService
    {
        private readonly IDroidService _droid;

        public TintMapDroidOverrideService(IDroidService droid)
        {
            _droid = droid;
        }

        public void RemoveDroidOverrides(uint creature, IReadOnlyList<string> variableNames)
        {
            if (variableNames.Count == 0 || !_droid.IsDroid(creature))
                return;

            var controller = _droid.GetControllerItem(creature);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = _droid.LoadConstructedDroid(controller);
            var changed = false;
            foreach (var variableName in variableNames)
            {
                changed |= constructedDroid.TintOverrides.Remove(variableName);
            }

            if (changed)
                _droid.SaveConstructedDroid(controller, constructedDroid);
        }

        public void SaveDroidOverrides(
            uint creature,
            IReadOnlyList<string> variableNames,
            int savedColor)
        {
            if (variableNames.Count == 0 || !_droid.IsDroid(creature))
                return;

            var controller = _droid.GetControllerItem(creature);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = _droid.LoadConstructedDroid(controller);
            foreach (var variableName in variableNames)
            {
                constructedDroid.TintOverrides[variableName] = savedColor;
            }

            _droid.SaveConstructedDroid(controller, constructedDroid);
        }

        public void SaveDroidOverride(
            uint creature,
            TintMapMaterialSelection selection,
            TintMapLayerType layer,
            string variableName,
            int savedColor)
        {
            if (!_droid.IsDroid(creature))
                return;

            var paletteSource = selection.GetPaletteSource(layer);
            if (paletteSource != creature)
            {
                _droid.UpdateEquippedItemSnapshot(creature, paletteSource);
                return;
            }

            var controller = _droid.GetControllerItem(creature);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = _droid.LoadConstructedDroid(controller);
            if (savedColor > 0)
                constructedDroid.TintOverrides[variableName] = savedColor;
            else
                constructedDroid.TintOverrides.Remove(variableName);

            _droid.SaveConstructedDroid(controller, constructedDroid);
        }
    }
}
