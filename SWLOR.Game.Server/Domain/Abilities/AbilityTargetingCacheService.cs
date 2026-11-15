using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>
/// The targeting overrides that update the client's targeting data. The dictionary that used to be static in
/// <c>Feature.AbilityDefinition.AbilityTargeting</c> is this instance's own: <see cref="CacheData"/>, which the ability catalog
/// calls when the abilities are cached, clears and refills it, so it is state and not a table. The cache half of
/// <c>AbilityTargeting</c> (the load and its validation) moved here with the ability split (P5-3b); the client refresh stays in
/// <c>AbilityTargeting</c>.
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

    public void CacheData(IReadOnlyDictionary<FeatType, AbilityDetail> abilities)
    {
        Clear();

        foreach (var (feat, ability) in abilities)
        {
            if (ability.Targeting == null)
                continue;

            ValidateTargeting(feat, ability.Name, ability.Targeting);
            if (!ability.Targeting.UpdatesClientTargeting)
                continue;

            Set(feat, ability.Targeting);
        }

        Console.WriteLine($"Loaded {GetAll().Count} ability targeting overrides.");
    }

    private static void ValidateTargeting(FeatType feat, string abilityName, AbilityTargetingDetail targeting)
    {
        if (targeting.UpdatesClientTargeting &&
            (targeting.Spell == Spell.Invalid ||
             targeting.Spell == Spell.AllSpells))
        {
            throw new InvalidOperationException(
                $"Ability '{abilityName}' ({feat}) declares targeting metadata without an explicit spell.");
        }

        if (!Enum.IsDefined(typeof(AbilityTargetingShapeType), targeting.Shape))
        {
            throw new InvalidOperationException(
                $"Ability '{abilityName}' ({feat}) declares unsupported targeting shape '{targeting.Shape}'.");
        }

        ValidateSize(feat, targeting.SizeX, targeting.SizeY);
    }

    /// <summary>The targeting size checks the cache load and the client refresh share.</summary>
    public static void ValidateSize(FeatType feat, float sizeX, float sizeY)
    {
        if (!float.IsFinite(sizeX) || sizeX <= 0f)
        {
            throw new InvalidOperationException(
                $"Ability '{feat}' resolved invalid targeting size X '{sizeX}'.");
        }

        if (!float.IsFinite(sizeY) || sizeY < 0f)
        {
            throw new InvalidOperationException(
                $"Ability '{feat}' resolved invalid targeting size Y '{sizeY}'.");
        }
    }
}
