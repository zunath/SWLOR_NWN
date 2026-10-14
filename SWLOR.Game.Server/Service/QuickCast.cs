using System.Collections.Generic;
using System.Linq;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Optional Settings-window casts bypass the client's feat cursor. Both modes must be
/// enabled each login; native feat buttons retain their normal target selection.
/// </summary>
public static class QuickCast
{
    private static readonly Dictionary<uint, HashSet<QuickCastTargetType>> _enabledModes = new();

    public static bool IsEnabled(uint player, QuickCastTargetType targetType) =>
        _enabledModes.TryGetValue(player, out var modes) && modes.Contains(targetType);

    public static void SetEnabled(uint player, QuickCastTargetType targetType, bool enabled)
    {
        if (enabled)
        {
            if (!_enabledModes.TryGetValue(player, out var modes))
                _enabledModes[player] = modes = new HashSet<QuickCastTargetType>();
            modes.Add(targetType);
        }
        else if (_enabledModes.TryGetValue(player, out var modes))
        {
            modes.Remove(targetType);
            if (modes.Count == 0)
                _enabledModes.Remove(player);
        }
    }

    public static IEnumerable<(FeatType Feat, string Name)> GetAvailableAbilities(uint player, QuickCastTargetType targetType) =>
        Ability.GetAllAbilityDetails()
            .Where(entry => QuickCastPolicy.IsEligible(entry.Value, targetType) && CreaturePlugin.GetKnowsFeat(player, entry.Key))
            .OrderBy(entry => entry.Value.Name)
            .Select(entry => (entry.Key, entry.Value.Name));

    private static bool TryResolveAbility(uint player, QuickCastTargetType targetType, string nameOrId, out FeatType feat, out string error)
    {
        feat = FeatType.Invalid;
        if (!GetIsPC(player) || GetIsDMPossessed(player))
        {
            error = "Quick casting is only available for your player character.";
            return false;
        }

        if (!IsEnabled(player, targetType))
        {
            error = "This quick-cast mode is off. Enable it under Settings > Targeting and click Save for this login.";
            return false;
        }

        if (!QuickCastPolicy.TryFindAbility(Ability.GetAllAbilityDetails(), nameOrId, targetType,
                candidate => CreaturePlugin.GetKnowsFeat(player, candidate), out feat))
        {
            error = "Choose a known, eligible single-target ability under Settings > Targeting. Reopen Settings to refresh your abilities.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryCast(uint player, QuickCastTargetType targetType, string nameOrId, out string error)
    {
        if (!TryResolveAbility(player, targetType, nameOrId, out var feat, out error))
            return false;

        // NUI buttons skip the native feat action that normally prevents controlled
        // creatures from acting. Include hidden native types such as knockdown.
        var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
        foreach (var effect in creature.m_appliedEffects)
        {
            if (!QuickCastPolicy.BlocksCasting((EffectTypeScript)effect.GetScriptEffectType(1)))
                continue;
            error = "You cannot quick cast while incapacitated.";
            return false;
        }

        var target = player;
        if (targetType == QuickCastTargetType.CurrentEnemy)
        {
            target = GetAttackTarget(player);
            if (!GetIsObjectValid(target) || target == player || GetIsDead(target) ||
                GetCurrentHitPoints(target) <= 0 || GetArea(target) != GetArea(player) ||
                !GetIsReactionTypeHostile(target, player) ||
                !WeaponAttackAnimation.HasQueuedAttack(
                    NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSObject(), target))
            {
                error = "You need a living current attack target in this area. Attack an enemy first, or use the normal feat button to choose a target.";
                return false;
            }
        }

        // This is the same activation/impact validation and resource/recast pipeline used
        // by feat clicks. The button does not grant feats or bypass ability requirements.
        return UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target));
    }

    public static void ResetSession(uint player) => _enabledModes.Remove(player);

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void ResetOnEnter() => ResetSession(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void ResetOnExit() => ResetSession(GetExitingObject());
}
