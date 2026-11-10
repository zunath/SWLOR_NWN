using System.Reflection;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests;

// Install real definition data without PerkBuilder.Build's native icon lookup, into the perk tables of the installed
// FacadeContainer.
internal sealed class DroidPerkCacheScope : IDisposable
{
    private readonly Dictionary<PerkType, PerkDetail> _cache = PerkInternals.AllPerks;
    private readonly Dictionary<PerkType, Dictionary<int, int>> _tiers = PerkInternals.PerkLevelTiers;
    private readonly Dictionary<PerkType, PerkDetail> _original;
    private readonly Dictionary<PerkType, Dictionary<int, int>> _originalTiers;

    public DroidPerkCacheScope()
    {
        _original = new(_cache);
        _originalTiers = new(_tiers);
        _cache.Clear();
        _tiers.Clear();
        try
        {
            foreach (var type in typeof(IPerkListDefinition).Assembly.GetTypes()
                         .Where(type => !type.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(type)))
            {
                var definition = Activator.CreateInstance(type)!;
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                             .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 && !method.Name.Contains('<'))
                             .OrderBy(method => method.MetadataToken))
                    method.Invoke(definition, null);
                var builder = type.GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
                var definitions = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                    .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
                foreach (var (perk, detail) in definitions)
                {
                    _cache.Add(perk, detail);
                    _tiers.Add(perk, detail.PerkLevels.ToDictionary(entry => entry.Key, entry => Math.Clamp(
                        entry.Value.Requirements.OfType<PerkRequirementSkill>().Select(requirement => requirement.RequiredRank)
                            .DefaultIfEmpty(0).Max() / 10 + 1, 1, 5)));
                }
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        _cache.Clear();
        foreach (var entry in _original)
            _cache.Add(entry.Key, entry.Value);
        _tiers.Clear();
        foreach (var entry in _originalTiers)
            _tiers.Add(entry.Key, entry.Value);
    }
}
