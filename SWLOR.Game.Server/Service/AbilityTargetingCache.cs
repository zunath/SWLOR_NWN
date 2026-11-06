using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// The targeting overrides that update the client's targeting data. The state lives in
/// <see cref="IAbilityTargetingCacheService"/>; every member forwards to the container's instance.
/// </summary>
public static class AbilityTargetingCache
{
    private static IAbilityTargetingCacheService Instance => FacadeAccessor.Resolve<IAbilityTargetingCacheService>();

    public static IReadOnlyDictionary<FeatType, AbilityTargetingDetail> GetAll() => Instance.GetAll();

    public static void Clear() => Instance.Clear();

    public static void Set(FeatType feat, AbilityTargetingDetail targeting) => Instance.Set(feat, targeting);
}
