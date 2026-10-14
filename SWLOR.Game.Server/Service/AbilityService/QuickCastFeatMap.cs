using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AbilityService;

/// <summary>Native presentation IDs retain the identity of their ordinary ability feat.</summary>
public static class QuickCastFeatMap
{
    public const string AliasPrefix = "AutoCast";
    private static readonly IReadOnlyDictionary<FeatType, FeatType> _originals = BuildOriginals();
    private static readonly IReadOnlyDictionary<FeatType, FeatType> _aliases =
        _originals.ToDictionary(entry => entry.Value, entry => entry.Key);

    public static IReadOnlyDictionary<FeatType, FeatType> Originals => _originals;
    public static FeatType GetOriginal(FeatType feat) => _originals.TryGetValue(feat, out var original) ? original : feat;
    public static bool IsAlias(FeatType feat) => _originals.ContainsKey(feat);
    public static bool TryGetAlias(FeatType feat, out FeatType alias) => _aliases.TryGetValue(GetOriginal(feat), out alias);

    private static IReadOnlyDictionary<FeatType, FeatType> BuildOriginals()
    {
        var result = new Dictionary<FeatType, FeatType>();
        foreach (var feat in Enum.GetValues<FeatType>())
        {
            var name = feat.ToString();
            if (!name.StartsWith(AliasPrefix, StringComparison.Ordinal))
                continue;
            if (!Enum.TryParse<FeatType>(name.Substring(AliasPrefix.Length), out var original) || original == feat)
                throw new InvalidOperationException($"Automatic targeting feat {name} has no original feat.");
            result.Add(feat, original);
        }
        return result;
    }
}
