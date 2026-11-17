using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Character
{
    public sealed class RobeModelCatalog
    {
        private readonly Dictionary<string, int> _phenotypes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, int> _basePhenotypes = new();

        public RobeModelCatalog(IEnumerable<(string Model, int Phenotype, int BasePhenotype)> definitions,
            IEnumerable<(int Phenotype, int BasePhenotype)> reserved = null)
        {
            foreach (var (phenotype, basePhenotype) in reserved ?? Array.Empty<(int, int)>())
            {
                if (phenotype is < 34 or > byte.MaxValue || basePhenotype is < 0 or >= 34 ||
                    !_basePhenotypes.TryAdd(phenotype, basePhenotype))
                    throw new ArgumentException("Invalid reserved robe phenotype.");
            }
            foreach (var (model, phenotype, basePhenotype) in definitions)
            {
                if (string.IsNullOrWhiteSpace(model) || phenotype is < 34 or > byte.MaxValue ||
                    basePhenotype is < 0 or >= 34)
                    throw new ArgumentException("Invalid robe rendering definition.");
                if (_phenotypes.ContainsKey(model) ||
                    (_basePhenotypes.TryGetValue(phenotype, out var previous) && previous != basePhenotype))
                    throw new ArgumentException("Conflicting robe rendering definitions.");
                _phenotypes.Add(model, phenotype);
                _basePhenotypes[phenotype] = basePhenotype;
            }
        }

        public int GetBasePhenotype(int phenotype) => _basePhenotypes.GetValueOrDefault(phenotype, phenotype);
        public bool Supports(string model) => !string.IsNullOrWhiteSpace(model) && _phenotypes.ContainsKey(model);

        /// <summary>
        /// Every catalogued robe renders through its generated body root, tinted or not. The root binds
        /// the garment to the wearer's bones; the separate native robe attachment animates on its own
        /// skeleton and can drift from the body during emotes and ability animations.
        /// </summary>
        public int ResolvePhenotype(int currentPhenotype, string robeModel)
        {
            var basePhenotype = GetBasePhenotype(currentPhenotype);
            if (string.IsNullOrWhiteSpace(robeModel) ||
                !_phenotypes.TryGetValue(robeModel, out var rendered) ||
                GetBasePhenotype(rendered) != basePhenotype)
                return basePhenotype;
            return rendered;
        }
    }
}
