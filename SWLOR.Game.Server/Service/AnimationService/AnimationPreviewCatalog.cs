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
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AnimationService;

public static class AnimationPreviewCatalog
{
    public const string OtherCategory = "Other";
    public sealed record Entry(string Id, string DisplayName, AnimationClip Clip, IReadOnlyList<string> Categories,
        Animation? NativeAnimation = null, float NativeAnimationSpeed = 1f)
    {
        public string DurationText => NativeAnimation.HasValue ? "Native" : $"{Clip.Duration:0.##}s";
        public string Play(uint creature) => NativeAnimation.HasValue
            ? NamedAnimation.PlayNativePreview(creature, NativeAnimation.Value, NativeAnimationSpeed)
            : NamedAnimation.Play(creature, Clip);
    }

    public static IReadOnlyDictionary<string, AnimationClip> Clips { get; } = CreateClips();

    private static IReadOnlyDictionary<string, AnimationClip> CreateClips()
    {
        var clips = ActiveAbilityAnimationCatalog.Entries.ToDictionary(entry => entry.Id, entry => entry.Clip,
            StringComparer.OrdinalIgnoreCase);
        foreach (var field in typeof(AuthoredAnimation)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AnimationClip)))
            clips[field.Name] = (AnimationClip)field.GetValue(null);
        return new ReadOnlyDictionary<string, AnimationClip>(clips);
    }

    public static IReadOnlyList<Entry> Entries { get; private set; } = CreateEntries(Array.Empty<AbilityDetail>());

    [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
    public static void CacheCategories() => Entries = CreateEntries(Ability.GetAllAbilityDetails().Values, Perk.GetAllPerks());

    public static IReadOnlyList<Entry> CreateEntries(IEnumerable<AbilityDetail> abilities,
        IReadOnlyDictionary<PerkType, PerkDetail> perks = null,
        IEnumerable<AbilityAnimationEntry> authoredEntries = null)
    {
        // Some buffs and casts declare their perk without a combat skill. Use that perk's category
        // when needed; keep shared clips in every category where they have a playback binding.
        var categories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var nativePreviews = new Dictionary<string, (Animation Animation, float Speed)>(StringComparer.OrdinalIgnoreCase);
        var authored = (authoredEntries ?? ActiveAbilityAnimationCatalog.Entries)
            .ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in authored.Values)
        {
            if (!categories.TryGetValue(entry.Clip.Name, out var names))
                categories[entry.Clip.Name] = names = new(StringComparer.OrdinalIgnoreCase);
            names.Add(entry.Category);
        }
        foreach (var ability in abilities)
        {
            if (ability.NativeAnimationPreview.HasValue && ability.PreviewAnimation != null)
            {
                var key = ability.PreviewAnimation.Name;
                var native = (Animation: ability.NativeAnimationPreview.Value, Speed: ability.NativeAnimationPreviewSpeed);
                if (!float.IsFinite(native.Speed) || native.Speed <= 0f)
                    throw new InvalidOperationException($"Invalid native preview speed for animation '{key}'.");
                if (nativePreviews.TryGetValue(key, out var previous) && previous != native)
                    throw new InvalidOperationException($"Conflicting native previews for animation '{key}'.");
                nativePreviews[key] = native;
            }
            var skill = typeof(SkillType).GetField(ability.SkillType.ToString())?.GetCustomAttribute<SkillAttribute>();
            var category = ability.SkillType != SkillType.Invalid && skill is { IsActive: true } ? skill.Name : null;
            if (category == null && perks != null && perks.TryGetValue(ability.EffectiveLevelPerkType, out var perk) && perk.IsActive)
            {
                var metadata = typeof(PerkCategoryType).GetField(perk.Category.ToString())?.GetCustomAttribute<PerkCategoryAttribute>();
                if (metadata is { IsActive: true }) category = metadata.Name.Split(" - ", 2)[0];
            }
            if (category == null) continue;
            foreach (var clip in new[] { ability.AuthoredAnimation, ability.QueuedAttackAnimation, ability.PreviewAnimation })
            {
                if (clip == null) continue;
                if (!categories.TryGetValue(clip.Name, out var names)) categories[clip.Name] = names = new(StringComparer.OrdinalIgnoreCase);
                names.Add(category);
            }
        }
        return Array.AsReadOnly(Clips.Select(pair => new Entry(pair.Key,
                authored.TryGetValue(pair.Key, out var metadata) ? metadata.DisplayName :
                    Regex.Replace(pair.Key, "(?<=[a-z0-9])(?=[A-Z])", " "), pair.Value,
                Array.AsReadOnly(categories.TryGetValue(pair.Value.Name, out var names)
                    ? names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray()
                    : new[] { OtherCategory }),
                nativePreviews.TryGetValue(pair.Value.Name, out var native) ? native.Animation : null,
                nativePreviews.TryGetValue(pair.Value.Name, out var nativeSpeed) ? nativeSpeed.Speed : 1f))
            .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static Entry[] Search(string query, string category = null, IEnumerable<Entry> entries = null) =>
        (entries ?? Entries).Where(entry => (string.IsNullOrEmpty(category) ||
            entry.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)) && Matches(entry, query)).ToArray();

    public static bool Matches(Entry entry, string query)
    {
        var text = Normalize(query);
        return Normalize(entry.DisplayName).Contains(text, StringComparison.OrdinalIgnoreCase) ||
               Normalize(entry.Id).Contains(text, StringComparison.OrdinalIgnoreCase) ||
               Normalize(entry.Clip.Name).Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value) => string.Concat((value ?? "").Where(c => !char.IsWhiteSpace(c) && c != '_'));

    public static bool IsAllowed(ServerEnvironmentType environment, AuthorizationLevel authorization) =>
        environment == ServerEnvironmentType.Test || (authorization & (AuthorizationLevel.DM | AuthorizationLevel.Admin)) != 0;

    public static bool CanUse(uint player) => ApplicationSettings.Get().ServerEnvironment == ServerEnvironmentType.Test ||
        IsAllowed(ApplicationSettings.Get().ServerEnvironment, Authorization.GetAuthorizationLevel(GetController(player)));

    public static uint GetController(uint actor) => GetIsDMPossessed(actor) ? GetMaster(actor) : actor;
}
