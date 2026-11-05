using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Persisted targeting preferences apply to all eligible native ability buttons. The logic lives in
/// <see cref="IQuickCastService"/>; the event handlers stay here and every member forwards to the container's instance.
/// </summary>
public static class QuickCast
{
    private static IQuickCastService Instance => FacadeAccessor.Resolve<IQuickCastService>();

    public static FeatType GetOriginalFeat(FeatType feat) => Instance.GetOriginalFeat(feat);

    public static bool IsEnabled(PlayerSettings settings, QuickCastTargetType type) => Instance.IsEnabled(settings, type);

    public static bool IsEnabled(uint player, QuickCastTargetType type) => Instance.IsEnabled(player, type);

    internal static bool UsesAutomaticTarget(FeatType original, PlayerSettings settings) =>
        Instance.UsesAutomaticTarget(original, settings);

    public static FeatType GetDisplayedFeat(uint player, FeatType feat) => Instance.GetDisplayedFeat(player, feat);

    public static bool KnowsFeat(uint creature, FeatType feat) => Instance.KnowsFeat(creature, feat);

    public static void RemoveFeat(uint creature, FeatType feat) => Instance.RemoveFeat(creature, feat);

    public static void ApplyPreferences(uint player) => Instance.ApplyPreferences(player);

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void ApplyOnEnter() => Instance.ApplyOnEnter();

    [NWNEventHandler(ScriptName.OnQuickbarSetButtonAfter)]
    public static void ApplyOnQuickbarEdit() => Instance.ApplyOnQuickbarEdit();

    public static bool TryCast(uint player, FeatType alias, out string error) => Instance.TryCast(player, alias, out error);
}
