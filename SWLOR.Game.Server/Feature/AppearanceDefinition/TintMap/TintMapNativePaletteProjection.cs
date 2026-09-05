using System;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>Preserves native preset colors while projecting the current tint into a PLT palette.</summary>
    public static class TintMapNativePaletteProjection
    {
        private const string Prefix = "TMP_";

        public readonly record struct Update(int Color, int Baseline, int LastApplied);

        public static string BaselineName(int channel) => $"{Prefix}B_{channel}";
        public static string LastAppliedName(int channel) => $"{Prefix}L_{channel}";

        public static bool IsStateName(string name)
        {
            return name != null &&
                   (name.StartsWith(Prefix + "B_", StringComparison.Ordinal) ||
                    name.StartsWith(Prefix + "L_", StringComparison.Ordinal)) &&
                   int.TryParse(name[(Prefix.Length + 2)..], out var channel) &&
                   channel >= 0 && channel < 120;
        }

        public static int GetBaseline(int nativeColor, int baseline, int lastApplied, int? inheritedColor = null)
        {
            // A differing native value is a subsequent preset edit, not our projected color.
            return baseline is > 0 and <= 256 && lastApplied is > 0 and <= 256 &&
                   nativeColor == lastApplied - 1
                ? baseline - 1
                : inheritedColor ?? nativeColor;
        }

        public static Update Resolve(int nativeColor, int baseline, int lastApplied, int? projectedColor,
            int? inheritedColor = null)
        {
            if (!projectedColor.HasValue && baseline == 0 && lastApplied == 0)
                return new Update(nativeColor, 0, 0);

            var authoredColor = GetBaseline(nativeColor, baseline, lastApplied, inheritedColor);
            return projectedColor.HasValue
                ? new Update(projectedColor.Value, authoredColor + 1, projectedColor.Value + 1)
                : new Update(authoredColor, 0, 0);
        }
    }
}
