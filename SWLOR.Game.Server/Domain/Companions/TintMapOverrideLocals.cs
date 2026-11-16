using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// The tint locals stored on an item or a droid and how they move between objects. They read and write
    /// object locals only, so the class needs no service and the droid service can call it without a cycle.
    /// </summary>
    public static class TintMapOverrideLocals
    {
        public static void RestoreDroidOverrides(
            uint droid,
            IReadOnlyDictionary<string, int> tintOverrides)
        {
            if (!GetIsObjectValid(droid) || tintOverrides == null)
                return;

            foreach (var (variableName, savedColor) in tintOverrides)
            {
                if (string.IsNullOrWhiteSpace(variableName) || savedColor <= 0)
                    continue;

                SetLocalInt(droid, variableName, savedColor);
            }
        }

        public static void ReplaceItemTintOverrides(uint sourceItem, uint targetItem)
        {
            if (!GetIsObjectValid(sourceItem) || !GetIsObjectValid(targetItem))
                return;

            var sourceOverrides = GetItemTintOverrides(sourceItem);
            var targetOverrides = GetItemTintOverrides(targetItem);

            foreach (var variableName in targetOverrides.Keys)
            {
                DeleteLocalInt(targetItem, variableName);
            }

            foreach (var (variableName, savedColor) in sourceOverrides)
            {
                SetLocalInt(targetItem, variableName, savedColor);
            }
        }

        public static Dictionary<string, int> GetItemTintOverrides(uint item)
        {
            var tintOverrides = new Dictionary<string, int>(StringComparer.Ordinal);
            var variableCount = ObjectPlugin.GetLocalVariableCount(item);
            for (var index = 0; index < variableCount; index++)
            {
                var variable = ObjectPlugin.GetLocalVariable(item, index);
                if (variable.Type != LocalVariableType.Int ||
                    (!variable.Key.StartsWith(TintMapVariable.Prefix, StringComparison.Ordinal) &&
                     !TintMapVariable.IsItemGlobalColorStateName(variable.Key) &&
                     !TintMapVariable.IsItemGlobalInheritanceStateName(variable.Key) &&
                     !TintMapNativePaletteProjection.IsStateName(variable.Key) &&
                     !ArmorColorIndexCalculator.IsPerPartOverrideVariableName(variable.Key)))
                {
                    continue;
                }

                tintOverrides[variable.Key] = GetLocalInt(item, variable.Key);
            }

            return tintOverrides;
        }
    }
}
