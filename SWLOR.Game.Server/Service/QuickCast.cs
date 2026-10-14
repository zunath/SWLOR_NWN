using System.Collections.Generic;
using System.Linq;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Explicit command shortcuts bypass the client's feat cursor. Native feat buttons retain
/// their normal target selection. Both command modes must be enabled each login.
/// </summary>
public static class QuickCast
{
    private static readonly Dictionary<uint, HashSet<QuickCastTargetType>> _enabledModes = new();
    private static readonly Dictionary<uint, Dictionary<int, (QuickBarSlot Original, string Command)>> _bindings = new();

    public static (int Slot, string Command)[] GetBoundCommandsForExport(uint player) =>
        _bindings.TryGetValue(player, out var bindings)
            ? bindings.Select(entry => (entry.Key, entry.Value.Command)).ToArray()
            : Array.Empty<(int, string)>();

    public static string CommandName(QuickCastTargetType targetType) =>
        targetType == QuickCastTargetType.Self ? "castself" : "castenemy";

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

    public static IEnumerable<string> GetAvailableAbilities(uint player, QuickCastTargetType targetType) =>
        Ability.GetAllAbilityDetails()
            .Where(entry => QuickCastPolicy.IsEligible(entry.Value, targetType) && CreaturePlugin.GetKnowsFeat(player, entry.Key))
            .OrderBy(entry => entry.Value.Name)
            .Select(entry => $"{entry.Value.Name} ({(int)entry.Key})");

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
            error = $"This quick-cast mode is off. Use /{CommandName(targetType)} on to enable it for this login.";
            return false;
        }

        if (!QuickCastPolicy.TryFindAbility(Ability.GetAllAbilityDetails(), nameOrId, targetType,
                candidate => CreaturePlugin.GetKnowsFeat(player, candidate), out feat))
        {
            error = $"Choose a known, eligible single-target ability by exact name or ID. Use /{CommandName(targetType)} list.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryCast(uint player, QuickCastTargetType targetType, string nameOrId, out string error)
    {
        if (!TryResolveAbility(player, targetType, nameOrId, out var feat, out error))
            return false;

        // Chat commands skip the native feat action that normally prevents controlled
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
        // by feat clicks. The command does not grant feats or bypass ability requirements.
        return UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target));
    }

    public static bool TryBind(uint player, int slot, QuickCastTargetType targetType, string nameOrId, out string error)
    {
        if (slot is < 1 or > 36)
        {
            error = "Choose a quickbar slot from 1 to 36.";
            return false;
        }

        if (!TryResolveAbility(player, targetType, nameOrId, out var feat, out error))
            return false;

        var nativeSlot = slot - 1;
        var current = PlayerPlugin.GetQuickBarSlot(player, nativeSlot);
        _bindings.TryGetValue(player, out var bindings);
        (QuickBarSlot Original, string Command) previous = default;
        bindings?.TryGetValue(nativeSlot, out previous);
        if (!CanBindSlot(current, previous.Command))
        {
            error = "Choose an empty quickbar slot. Existing shortcuts cannot be replaced by quick casting.";
            return false;
        }

        var original = IsBoundCommand(current, previous.Command)
            ? previous.Original
            : current;
        var command = $"/{CommandName(targetType)} {(int)feat}";
        var ability = Ability.GetAbilityDetail(feat);
        var label = $"{(targetType == QuickCastTargetType.Self ? "Self" : "Enemy")}: {ability.Name}";
        PlayerPlugin.SetQuickBarSlot(player, nativeSlot, PlayerQuickBarSlot.Command(label, command));
        if (bindings == null)
            _bindings[player] = bindings = new Dictionary<int, (QuickBarSlot, string)>();
        bindings[nativeSlot] = (original, command);
        return true;
    }

    public static bool CanBindSlot(QuickBarSlot slot, string ownedCommand) =>
        slot != null && (slot.ObjectType == QuickBarSlotType.Empty ||
                         !string.IsNullOrEmpty(ownedCommand) && IsBoundCommand(slot, ownedCommand));

    public static bool IsBoundCommand(QuickBarSlot slot, string command) =>
        slot != null && slot.ObjectType == QuickBarSlotType.Command && slot.CommandLine == command;

    public static bool TryUnbind(uint player, int slot, out string error)
    {
        if (slot is < 1 or > 36 || !_bindings.TryGetValue(player, out var bindings) ||
            !bindings.TryGetValue(slot - 1, out var binding))
        {
            error = "There is no saved quick-cast binding for that slot during this login.";
            return false;
        }

        if (IsBoundCommand(PlayerPlugin.GetQuickBarSlot(player, slot - 1), binding.Command))
            PlayerPlugin.SetQuickBarSlot(player, slot - 1, binding.Original);
        bindings.Remove(slot - 1);
        if (bindings.Count == 0)
            _bindings.Remove(player);
        error = string.Empty;
        return true;
    }

    public static void ResetSession(uint player)
    {
        if (_bindings.TryGetValue(player, out var bindings))
        {
            if (GetIsObjectValid(player))
            {
                foreach (var slot in bindings.Keys.ToArray())
                    TryUnbind(player, slot + 1, out _);
            }
            else
                _bindings.Remove(player);
        }
        _enabledModes.Remove(player);
    }

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void ResetOnEnter() => ResetSession(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnModuleExit)]
    public static void ResetOnExit() => ResetSession(GetExitingObject());
}
