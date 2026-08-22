using System.Reflection;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Perk rank metadata used by PERK_LEVEL_* overrides.</summary>
    public static class CreaturePerkCatalog
    {
        public static IReadOnlyDictionary<int, CreaturePerkInfo> Build()
        {
            var result = new Dictionary<int, CreaturePerkInfo>();
            var perkMapField = typeof(PerkBuilder).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(field => field.FieldType == typeof(Dictionary<PerkType, PerkDetail>));
            foreach (var type in typeof(IPerkListDefinition).Assembly.GetTypes()
                         .Where(type => typeof(IPerkListDefinition).IsAssignableFrom(type) &&
                                        !type.IsAbstract && !type.IsInterface))
            {
                try
                {
                    if (Activator.CreateInstance(type) is not IPerkListDefinition definition)
                        continue;

                    // BuildPerks() finishes with PerkBuilder.Build(), which resolves feat icons through
                    // NWScript and is therefore unavailable in the desktop toolset. Perk definitions
                    // consistently declare each builder step as a private parameterless method. Invoke
                    // those declarations and read the completed builder before its runtime-only icon pass.
                    foreach (var method in type.GetMethods(
                                 BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                 .Where(method => !method.IsSpecialName &&
                                                  method.ReturnType == typeof(void) &&
                                                  method.GetParameters().Length == 0))
                    {
                        method.Invoke(definition, null);
                    }

                    var builderField = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                        .Single(field => field.FieldType == typeof(PerkBuilder));
                    var builder = (PerkBuilder)builderField.GetValue(definition)!;
                    var perks = (Dictionary<PerkType, PerkDetail>)perkMapField.GetValue(builder)!;
                    foreach (var (perk, detail) in perks)
                    {
                        var id = Convert.ToInt32(perk);
                        var grantedFeatDescriptions = detail.PerkLevels
                            .OrderBy(pair => pair.Key)
                            .SelectMany(pair => pair.Value.GrantedFeats.Select(feat => new
                            {
                                FeatId = (int)feat,
                                pair.Value.Description
                            }))
                            .Where(item => !string.IsNullOrWhiteSpace(item.Description))
                            .GroupBy(item => item.FeatId)
                            .ToDictionary(
                                group => group.Key,
                                group => group.Last().Description!.Trim());
                        result[id] = new CreaturePerkInfo(
                            id,
                            string.IsNullOrWhiteSpace(detail.Name) ? Humanize(perk.ToString()) : detail.Name,
                            detail.PerkLevels.Count == 0 ? 1 : detail.PerkLevels.Keys.Max(),
                            detail.PerkLevels.Values
                                .SelectMany(level => level.GrantedFeats)
                                .Select(feat => (int)feat)
                                .ToHashSet(),
                            grantedFeatDescriptions);
                    }
                }
                catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException)
                {
                }
            }

            return result;
        }

        private static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2")
                .Replace('_', ' ');
    }
}
