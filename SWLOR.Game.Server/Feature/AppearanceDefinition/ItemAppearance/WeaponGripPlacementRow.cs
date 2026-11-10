using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal sealed record WeaponGripPlacementRow(string Hand, int Model, WeaponGripHold Hold, WeaponGripFit Fit)
{
    internal static IEnumerable<WeaponGripPlacementRow> FromColumns(string hand, WeaponGripHold hold,
        string modelColumn, IReadOnlyList<string> valueColumns)
    {
        var models = modelColumn.Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        var columns = valueColumns.Select(column => column.Split(',')
            .Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray()).ToArray();
        if (columns.Length != 6 || columns.Any(column => column.Length != 1 && column.Length != models.Length))
            throw new ArgumentException("Weapon grip placement columns must match their model list.");
        for (var index = 0; index < models.Length; index++)
        {
            float Value(int column) => columns[column][columns[column].Length == 1 ? 0 : index];
            yield return new WeaponGripPlacementRow(hand, models[index], hold,
                new WeaponGripFit(new FitVector(Value(0), Value(1), Value(2)),
                    new FitVector(Value(3), Value(4), Value(5)), 1f));
        }
    }
}
