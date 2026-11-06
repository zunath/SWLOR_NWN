using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>The targeting overrides that update the client's targeting data, by ability feat.</summary>
public interface IAbilityTargetingCacheService
{
    /// <summary>The cached overrides. The same instance every time; it changes as the cache is rebuilt.</summary>
    IReadOnlyDictionary<FeatType, AbilityTargetingDetail> GetAll();

    /// <summary>Drops every cached override.</summary>
    void Clear();

    /// <summary>Caches the override for this feat, replacing an earlier one.</summary>
    void Set(FeatType feat, AbilityTargetingDetail targeting);
}
