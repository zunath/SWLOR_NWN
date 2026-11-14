using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>Every situation definition, built once from the <see cref="ISituationListDefinition"/> classes.</summary>
    public static class SituationCatalog
    {
        private static readonly Lazy<IReadOnlyDictionary<SituationType, SituationDetail>> _all = new(Load);

        public static IReadOnlyDictionary<SituationType, SituationDetail> All => _all.Value;

        public static SituationDetail Get(SituationType type)
        {
            return All.TryGetValue(type, out var detail) ? detail : null;
        }

        private static IReadOnlyDictionary<SituationType, SituationDetail> Load()
        {
            var situations = new Dictionary<SituationType, SituationDetail>();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ISituationListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var type in types)
            {
                var definition = (ISituationListDefinition)Activator.CreateInstance(type);
                foreach (var (situationType, detail) in definition.Build())
                {
                    if (situations.ContainsKey(situationType))
                        throw new InvalidOperationException($"Situation {situationType} is defined more than once.");

                    situations[situationType] = detail;
                }
            }

            return situations;
        }
    }
}
