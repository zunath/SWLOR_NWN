using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>
/// Cosmetic toggle for one-handed blades using two-handed combat animations.
/// It does not change weapon classification, damage, equipment or ability rules.
/// </summary>
public static class Grip
{
    private const string EnabledVariable = "DOUBLEHAND_ENABLED";
    private const string ModeVariable = "DOUBLEHAND_MODE";

    public static void Initialize(uint player)
    {
        if (!GetIsPC(player) || GetIsDM(player)) return;
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        SetLocalBool(player, EnabledVariable, dbPlayer?.Settings?.AlternateGripEnabled ?? false);
        Refresh(player, true);
    }

    public static void Toggle(uint player)
    {
        if (!GetIsPC(player) || GetIsDead(player)) return;
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        if (dbPlayer == null) return;
        var enabled = !GetLocalBool(player, EnabledVariable);
        dbPlayer.Settings ??= new PlayerSettings();
        dbPlayer.Settings.AlternateGripEnabled = enabled;
        DB.Set(dbPlayer);
        SetLocalBool(player, EnabledVariable, enabled);
        Refresh(player, true);
        SendMessageToPC(player, enabled ? "Alternate grip enabled: one-handed blades use two-handed combat animations. Keep the offhand empty." : "Alternate grip disabled.");
    }

    [NWNEventHandler(ScriptName.OnModuleRespawn)]
    public static void OnRespawn() => Refresh(GetLastRespawnButtonPresser(), true);

    /// <summary>
    /// Syncs the selected grip on login, equipment changes and queued attack release.
    /// Only swing aliases owned by an active queued ability are deferred.
    /// No activation/deactivation animation is played.
    /// </summary>
    internal static void Refresh(uint creature, bool forceRefresh = false)
    {
        if (!GetIsObjectValid(creature)) return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        var desired = GetMode(GetLocalBool(creature, EnabledVariable),
            GetIsObjectValid(right) ? GetBaseItemType(right) : null,
            GetIsObjectValid(left) && left != right);
        var applied = GetLocalInt(creature, ModeVariable);
        var queuedSwingKeys = QueuedAttackAnimation.GetActiveSwingKeys(creature);

        ApplyTransition(desired, applied, forceRefresh, (source, replacement) =>
        {
            if (!queuedSwingKeys.Contains(source))
                ReplaceObjectAnimation(creature, source, replacement);
        });
        SetLocalInt(creature, ModeVariable, desired);
    }

    /// <summary>Only eligible one-handed blades can use the alternate two-handed combat style.</summary>
    public static int GetMode(bool enabled, BaseItem? rightHand, bool hasOffhand) =>
        !enabled || hasOffhand ? 0 : rightHand switch
        {
            BaseItem.Lightsaber or BaseItem.Electroblade or
            BaseItem.Longsword or BaseItem.BastardSword or BaseItem.Katana or
            BaseItem.Scimitar or BaseItem.BattleAxe => 1,
            _ => 0
        };

    public static IReadOnlyList<string> CombatSuffixes { get; } = Array.AsReadOnly(
        new[] { "slashl", "slashr", "slasho", "stab", "closeh", "closel", "reach",
            "readyl", "readyr", "parryl", "parryr" });

    private static readonly string[] OneHandedAnimationSources = { "1h", "2w" };

    /// <summary>
    /// Changes ready, parry and auto-attack clips from 1h/2w to the corresponding
    /// 2h animation. Disabling removes only those mappings; it never touches
    /// dodges, plpause1, weapon holds, emotes or actual two-handed weapon styles.
    /// </summary>
    public static void ApplyTransition(int desired, int applied, bool forceRefresh, Action<string, string> replace)
    {
        if (applied == 1 && desired != 1)
            foreach (var source in OneHandedAnimationSources)
                foreach (var suffix in CombatSuffixes)
                    replace(source + suffix, "");

        if (desired == 1 && (applied != 1 || forceRefresh))
            foreach (var source in OneHandedAnimationSources)
                foreach (var suffix in CombatSuffixes)
                    replace(source + suffix, "2h" + suffix);
    }
}
