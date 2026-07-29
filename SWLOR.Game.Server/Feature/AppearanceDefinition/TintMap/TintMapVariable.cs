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
    }
}
