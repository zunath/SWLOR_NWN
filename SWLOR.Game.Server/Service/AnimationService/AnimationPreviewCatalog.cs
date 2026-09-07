using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AnimationService;

public static class AnimationPreviewCatalog
{
    public sealed record Entry(string Id, string DisplayName, AnimationClip Clip);

    public static IReadOnlyDictionary<string, AnimationClip> Clips { get; } =
        new ReadOnlyDictionary<string, AnimationClip>(typeof(AuthoredAnimation)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AnimationClip))
            .ToDictionary(field => field.Name, field => (AnimationClip)field.GetValue(null), StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<Entry> Entries { get; } = Array.AsReadOnly(Clips
        .Select(pair => new Entry(pair.Key, Regex.Replace(pair.Key, "(?<=[a-z0-9])(?=[A-Z])", " "), pair.Value))
        .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray());

    public static Entry[] Search(string query) => Entries.Where(entry => Matches(entry, query)).ToArray();

    public static bool Matches(Entry entry, string query)
    {
        var text = Normalize(query);
        return Normalize(entry.DisplayName).Contains(text, StringComparison.OrdinalIgnoreCase) ||
               Normalize(entry.Clip.Name).Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value) => string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c) && c != '_'));

    public static bool IsAllowed(ServerEnvironmentType environment, AuthorizationLevel authorization) =>
        environment == ServerEnvironmentType.Test || (authorization & (AuthorizationLevel.DM | AuthorizationLevel.Admin)) != 0;

    public static bool CanUse(uint player) => ApplicationSettings.Get().ServerEnvironment == ServerEnvironmentType.Test ||
        IsAllowed(ApplicationSettings.Get().ServerEnvironment, Authorization.GetAuthorizationLevel(player));
}
