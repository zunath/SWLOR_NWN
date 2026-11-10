using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>
/// The targeting overrides that update the client's targeting data. The dictionary that used to be static in
/// <c>Feature.AbilityDefinition.AbilityTargeting</c> is this instance's own: <c>Ability.CacheData</c> clears and refills
/// it when the abilities are cached, so it is state and not a table. The class keeps the validation and the client
/// refresh.
/// </summary>
public class AbilityTargetingCacheService : IAbilityTargetingCacheService
{
    private readonly Dictionary<FeatType, AbilityTargetingDetail> _targetingByFeat = new();

    public IReadOnlyDictionary<FeatType, AbilityTargetingDetail> GetAll()
    {
        return _targetingByFeat;
    }

    public void Clear()
    {
        _targetingByFeat.Clear();
    }

    public void Set(FeatType feat, AbilityTargetingDetail targeting)
    {
        _targetingByFeat[feat] = targeting;
    }
}
