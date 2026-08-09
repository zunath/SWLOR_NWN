using System;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>The stable GFF/local-variable contract shared by the server and desktop Toolset.</summary>
    public static class TintMapVariable
    {
        public const string Prefix = "TM_";

        public static string GetName(string materialResref, TintMapLayerType layer)
        {
            if (string.IsNullOrWhiteSpace(materialResref))
                throw new ArgumentException("A tint material resref is required.", nameof(materialResref));

            return $"{Prefix}{materialResref}_{(int)layer}";
        }

        public static bool IsCreatureColorLayer(TintMapLayerType layer)
        {
            return layer is TintMapLayerType.Skin or
                TintMapLayerType.Hair or
                TintMapLayerType.Tattoo1 or
                TintMapLayerType.Tattoo2;
        }

        public static bool TryGetLayer(string variableName, out TintMapLayerType layer)
        {
            layer = default;
            if (string.IsNullOrWhiteSpace(variableName) ||
                !variableName.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var separator = variableName.LastIndexOf('_');
            if (separator < Prefix.Length ||
                !int.TryParse(variableName[(separator + 1)..], out var value) ||
                !Enum.IsDefined(typeof(TintMapLayerType), value))
            {
                return false;
            }

            layer = (TintMapLayerType)value;
            return true;
        }
    }
}
