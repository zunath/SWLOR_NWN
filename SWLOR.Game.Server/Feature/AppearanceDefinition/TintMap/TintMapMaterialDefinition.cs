using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public class TintMapMaterialDefinition
    {
        public string Name { get; }
        public string Resref { get; }
        public IReadOnlyList<TintMapLayerType> Layers { get; }

        public TintMapMaterialDefinition(string name, string resref, params TintMapLayerType[] layers)
        {
            if (string.IsNullOrWhiteSpace(resref) || resref.Length > 16)
                throw new ArgumentException("Material resrefs must contain 1-16 characters.", nameof(resref));
            if (layers == null || layers.Length == 0)
                throw new ArgumentException("Tint map materials must expose at least one layer.", nameof(layers));

            Name = name;
            Resref = resref;
            Layers = layers;
        }
    }
}
