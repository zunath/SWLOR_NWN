using System.Collections.Generic;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service;

/// <summary>Persisted targeting preferences apply to all eligible native ability buttons.</summary>
public static class QuickCast
{
    private static readonly HashSet<uint> _applying = new();

    public static FeatType GetOriginalFeat(FeatType feat) => QuickCastFeatMap.GetOriginal(feat);

    private static PlayerSettings GetSettings(uint player)
    {
        if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            return null;
        var id = GetObjectUUID(player);
        return string.IsNullOrWhiteSpace(id) ? null : DB.Get<Player>(id)?.Settings;
    }

    public static bool IsEnabled(PlayerSettings settings, QuickCastTargetType type) => type switch
    {
        QuickCastTargetType.Self => settings?.SelfQuickCastEnabled == true,
        QuickCastTargetType.CurrentEnemy => settings?.EnemyQuickCastEnabled == true,
        _ => false
    };

    public static bool IsEnabled(uint player, QuickCastTargetType type) => IsEnabled(GetSettings(player), type);

    private static bool UsesAutomaticTarget(FeatType original, PlayerSettings settings)
    {
        if (settings == null || !Ability.IsFeatRegistered(original))
            return false;
        var ability = Ability.GetAbilityDetail(original);
        return IsEnabled(settings, QuickCastTargetType.Self) && QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self) ||
               IsEnabled(settings, QuickCastTargetType.CurrentEnemy) && QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy);
    }

    public static FeatType GetDisplayedFeat(uint player, FeatType feat)
    {
        var original = GetOriginalFeat(feat);
        return QuickCastFeatMap.TryGetAlias(original, out var alias) && UsesAutomaticTarget(original, GetSettings(player))
            ? alias : original;
    }

    public static bool KnowsFeat(uint creature, FeatType feat)
    {
        var original = GetOriginalFeat(feat);
        return CreaturePlugin.GetKnowsFeat(creature, original) ||
               QuickCastFeatMap.TryGetAlias(original, out var alias) && CreaturePlugin.GetKnowsFeat(creature, alias);
    }

    public static void RemoveFeat(uint creature, FeatType feat)
    {
        var original = GetOriginalFeat(feat);
        CreaturePlugin.RemoveFeat(creature, original);
        if (QuickCastFeatMap.TryGetAlias(original, out var alias))
            CreaturePlugin.RemoveFeat(creature, alias);
    }

    public static void ApplyPreferences(uint player)
    {
        var settings = GetSettings(player);
        if (settings == null || !_applying.Add(player))
            return;
        try
        {
            foreach (var (alias, original) in QuickCastFeatMap.Originals)
            {
                var hasOriginal = CreaturePlugin.GetKnowsFeat(player, original);
                var hasAlias = CreaturePlugin.GetKnowsFeat(player, alias);
                if (!hasOriginal && !hasAlias)
                    continue;
                var automatic = UsesAutomaticTarget(original, settings);
                var desired = automatic ? alias : original;
                var other = automatic ? original : alias;
                if (!(automatic ? hasAlias : hasOriginal))
                    CreaturePlugin.AddFeat(player, desired);
                if (automatic ? hasOriginal : hasAlias)
                    CreaturePlugin.RemoveFeat(player, other);
            }

            // Saved layouts and newly dragged feats may still carry either representation.
            // Only ability IDs change; labels, other shortcuts and slot positions are preserved.
            for (var index = 0; index < 36; index++)
            {
                var slot = PlayerPlugin.GetQuickBarSlot(player, index);
                if (slot.ObjectType != QuickBarSlotType.Feat)
                    continue;
                var current = (FeatType)slot.INTParam1;
                var original = GetOriginalFeat(current);
                if (!QuickCastFeatMap.TryGetAlias(original, out var alias))
                    continue;
                var desired = KnowsFeat(player, original) && UsesAutomaticTarget(original, settings) ? alias : original;
                if (current == desired)
                    continue;
                slot.INTParam1 = (int)desired;
                PlayerPlugin.SetQuickBarSlot(player, index, slot);
            }
        }
        finally { _applying.Remove(player); }
    }

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void ApplyOnEnter() => ApplyPreferences(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnQuickbarSetButtonAfter)]
    public static void ApplyOnQuickbarEdit() => ApplyPreferences(OBJECT_SELF);

    public static bool TryCast(uint player, FeatType alias, out string error)
    {
        var settings = GetSettings(player);
        if (settings == null || !QuickCastFeatMap.IsAlias(alias))
        {
            error = "Automatic targeting is only available for your player character's abilities.";
            return false;
        }
        var feat = GetOriginalFeat(alias);
        if (!Ability.IsFeatRegistered(feat) || !KnowsFeat(player, feat))
        {
            error = "You do not know this ability.";
            return false;
        }
        var ability = Ability.GetAbilityDetail(feat);
        var type = ability.IsHostileAbility ? QuickCastTargetType.CurrentEnemy : QuickCastTargetType.Self;
        if (!IsEnabled(settings, type) || !QuickCastPolicy.IsEligible(ability, type))
        {
            error = "Automatic targeting is disabled for this ability. Check Settings > Targeting.";
            return false;
        }

        var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
        foreach (var effect in creature.m_appliedEffects)
        {
            if (!QuickCastPolicy.BlocksCasting((EffectTypeScript)effect.GetScriptEffectType(1)))
                continue;
            error = "You cannot use an ability while incapacitated.";
            return false;
        }

        var target = player;
        if (type == QuickCastTargetType.CurrentEnemy)
        {
            target = GetAttackTarget(player);
            if (!GetIsObjectValid(target) || target == player || GetIsDead(target) || GetCurrentHitPoints(target) <= 0 ||
                GetArea(target) != GetArea(player) || !GetIsReactionTypeHostile(target, player) ||
                !WeaponAttackAnimation.HasQueuedAttack(
                    NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSObject(), target))
            {
                error = "Attack a living enemy first to use current-enemy targeting.";
                return false;
            }
        }
        error = string.Empty;
        return UsePerkFeat.TryUseAbility(player, target, feat, GetLocation(target));
    }
}
