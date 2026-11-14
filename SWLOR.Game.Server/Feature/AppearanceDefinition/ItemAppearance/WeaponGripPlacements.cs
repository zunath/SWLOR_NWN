using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal static class WeaponGripPlacements
{
    private static WeaponGripPlacementCatalog _catalog = new(Array.Empty<WeaponGripPlacementRow>());

    [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
    public static void Load()
    {
        var rows = new List<WeaponGripPlacementRow>();
        var count = Get2DARowCount("weaponfits");
        for (var row = 0; row < count; row++)
        {
            var hand = Get2DAString("weaponfits", "HAND", row);
            var hold = Enum.Parse<WeaponGripHold>(Get2DAString("weaponfits", "HOLD", row), true);
            var models = Get2DAString("weaponfits", "MODEL", row);
            var columns = new[] { "X", "Y", "Z", "HEADING", "PITCH", "ROLL" }
                .Select(column => Get2DAString("weaponfits", column, row)).ToArray();
            try
            {
                rows.AddRange(WeaponGripPlacementRow.FromColumns(hand, hold, models, columns));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Invalid weaponfits row {row} for {hand}.", exception);
            }
        }
        _catalog = new WeaponGripPlacementCatalog(rows);
    }

    internal static bool TryGet(string hand, int model, WeaponGripHold hold, out WeaponGripFit fit) =>
        _catalog.TryGet(hand, model, hold, out fit);
}
