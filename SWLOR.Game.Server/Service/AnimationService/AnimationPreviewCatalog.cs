using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.AnimationService;

public static class AnimationPreviewCatalog
{
    public const string OtherCategory = "Other";
    public sealed record Entry(string Id, string DisplayName, AnimationClip Clip, IReadOnlyList<string> Categories);

    public static IReadOnlyDictionary<string, AnimationClip> Clips { get; } =
        new ReadOnlyDictionary<string, AnimationClip>(typeof(AuthoredAnimation)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AnimationClip))
            .ToDictionary(field => field.Name, field => (AnimationClip)field.GetValue(null), StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<Entry> Entries { get; private set; } = CreateEntries(Array.Empty<AbilityDetail>());

    [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
    public static void CacheCategories() => Entries = CreateEntries(Ability.GetAllAbilityDetails().Values, Perk.GetAllPerks());

    public static IReadOnlyList<Entry> CreateEntries(IEnumerable<AbilityDetail> abilities,
        IReadOnlyDictionary<PerkType, PerkDetail> perks = null)
    {
        // Some buffs and casts declare their perk without a combat skill. Use that perk's category
        // when needed; keep shared clips in every category where they have a playback binding.
        var categories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var ability in abilities)
        {
            var skill = typeof(SkillType).GetField(ability.SkillType.ToString())?.GetCustomAttribute<SkillAttribute>();
            var category = ability.SkillType != SkillType.Invalid && skill is { IsActive: true } ? skill.Name : null;
            if (category == null && perks != null && perks.TryGetValue(ability.EffectiveLevelPerkType, out var perk) && perk.IsActive)
            {
                var metadata = typeof(PerkCategoryType).GetField(perk.Category.ToString())?.GetCustomAttribute<PerkCategoryAttribute>();
                if (metadata is { IsActive: true }) category = metadata.Name.Split(" - ", 2)[0];
            }
            if (category == null) continue;
            foreach (var clip in new[] { ability.AuthoredAnimation, ability.QueuedAttackAnimation })
            {
                if (clip == null) continue;
                if (!categories.TryGetValue(clip.Name, out var names)) categories[clip.Name] = names = new(StringComparer.OrdinalIgnoreCase);
                names.Add(category);
            }
        }
        return Array.AsReadOnly(Clips.Select(pair => new Entry(pair.Key,
                Regex.Replace(pair.Key, "(?<=[a-z0-9])(?=[A-Z])", " "), pair.Value,
                Array.AsReadOnly(categories.TryGetValue(pair.Value.Name, out var names)
                    ? names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                    : new[] { OtherCategory })))
            .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static Entry[] Search(string query, string category = null, IEnumerable<Entry> entries = null) =>
        (entries ?? Entries).Where(entry => (string.IsNullOrEmpty(category) ||
            entry.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)) && Matches(entry, query)).ToArray();

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
        IsAllowed(ApplicationSettings.Get().ServerEnvironment, Authorization.GetAuthorizationLevel(GetController(player)));

    public static uint GetController(uint actor) => GetIsDMPossessed(actor) ? GetMaster(actor) : actor;
}
