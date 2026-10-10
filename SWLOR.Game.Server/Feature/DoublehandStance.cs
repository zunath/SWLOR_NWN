using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>A cosmetic melee weapon grip preference. It changes combat poses and attacks, never combat stats.</summary>
public static class DoublehandStance
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
        Refresh(player, true, playTransition: true);
        SendMessageToPC(player, enabled
            ? "Alternate grip enabled: blades use two hands; staffs, spears and double blades use one hand. Keep the offhand empty."
            : "Alternate grip disabled.");
    }

    [NWNEventHandler(ScriptName.OnModuleRespawn)]
    public static void OnRespawn() => Refresh(GetLastRespawnButtonPresser(), true);

    // Called before the katar owner restores its mappings, so releasing Doublehand cannot erase them.
    internal static void Refresh(uint creature, bool forceRefresh = false, bool playTransition = false)
    {
        if (!GetIsObjectValid(creature) || QueuedAttackAnimation.IsActive(creature)) return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        var desired = GetMode(GetLocalBool(creature, EnabledVariable),
            GetIsObjectValid(right) ? GetBaseItemType(right) : null, GetIsObjectValid(left) && left != right);
        var applied = GetLocalInt(creature, ModeVariable);
        ApplyTransition(desired, applied, forceRefresh,
            (source, destination) => ReplaceObjectAnimation(creature, source, destination));
        SetLocalInt(creature, ModeVariable, desired);
        if (ShouldPlayTransition(desired, applied, playTransition,
                GetIsObjectValid(right) && GetMode(true, GetBaseItemType(right), false) != 0))
        {
            AssignCommand(creature, () =>
            {
                if (!GetIsObjectValid(creature) || GetLocalInt(creature, ModeVariable) != desired ||
                    GetItemInSlot(InventorySlot.RightHand, creature) != right) return;
                // Clear the existing shoulder overlay by its original name before suppressing
                // future carry layers. A full-body pause clip is not a carry-layer replacement.
                ReplaceObjectAnimation(creature, "plpause1", "");
                var enabled = GetLocalBool(creature, EnabledVariable);
                var token = GetLocalInt(creature, "GRIP_PARRY_TOKEN") + 1;
                SetLocalInt(creature, "GRIP_PARRY_TOKEN", token);
                if (enabled && GetMode(true, GetBaseItemType(right), false) == 2)
                {
                    // The native one-shot clears the carry overlay; show double-weapon parry.
                    ReplaceObjectAnimation(creature, "dodges", "plparryl");
                    PlayAnimation(Animation.FireForgetDodgeSide);
                    DelayCommand(1.0f, () =>
                    {
                        if (!GetIsObjectValid(creature) || GetLocalInt(creature, "GRIP_PARRY_TOKEN") != token) return;
                        ReplaceObjectAnimation(creature, "dodges", "");
                    });
                }
                else
                {
                    ReplaceObjectAnimation(creature, "dodges", "");
                    // Keep the existing one-handed activation and all deactivation emotes.
                    PlayAnimation(enabled ? Animation.ClassicJediStance : Animation.DoubleLSStance, 1.0f, 1.2f);
                }
                // Let the client remove the original overlay before changing its lookup.
                // Otherwise the removal can resolve to sw_nohold and leave plpause1 attached.
                DelayCommand(0.3f, () =>
                {
                    if (!GetIsObjectValid(creature) || GetLocalInt(creature, ModeVariable) != 2 ||
                        GetItemInSlot(InventorySlot.RightHand, creature) != right) return;
                    ReplaceObjectAnimation(creature, "plpause1", NamedAnimationPlayback.NoHoldName);
                });
            });
        }
    }

    /// <summary>Replays the stance gesture only for a new two-handed mode or an explicit toggle.</summary>
    public static bool ShouldPlayTransition(int desired, int applied, bool explicitToggle, bool supportedWeapon) =>
        (desired == 2 && applied != desired) || (explicitToggle && supportedWeapon);

    public static int GetMode(bool enabled, BaseItem? rightHand, bool hasOffhand) =>
        !enabled || hasOffhand ? 0 : rightHand switch
        {
            BaseItem.Lightsaber or BaseItem.Electroblade or
            BaseItem.Longsword or BaseItem.BastardSword or BaseItem.Katana or
            BaseItem.Scimitar or BaseItem.BattleAxe => 1,
            BaseItem.Saberstaff or BaseItem.TwinElectroBlade or
            BaseItem.GreatSword or BaseItem.QuarterStaff or BaseItem.MagicStaff or
            BaseItem.Halberd or BaseItem.Scythe or BaseItem.ShortSpear or BaseItem.Trident or
            BaseItem.TwoBladedSword or BaseItem.DoubleAxe => 2,
            _ => 0
        };

    public static IReadOnlyList<string> CombatSuffixes { get; } = Array.AsReadOnly(
        new[] { "slashl", "slashr", "slasho", "stab", "closeh", "closel", "reach",
            "readyl", "readyr", "parryl", "parryr" });

    private static string[] Sources(int mode) => mode == 1
        ? new[] { "1h", "2w" } : new[] { "2h", "2w", "pl" };

    public static void ApplyTransition(int desired, int applied, bool forceRefresh, Action<string, string> replace)
    {
        if (applied == 2 && desired != 2) replace("plpause1", "");
        if (desired == 2 && (desired != applied || forceRefresh)) replace("plpause1", NamedAnimationPlayback.NoHoldName);
        if (applied != 0 && applied != desired)
            foreach (var prefix in Sources(applied))
                foreach (var suffix in CombatSuffixes) replace(prefix + suffix, "");
        if (desired != 0 && (desired != applied || forceRefresh))
            foreach (var prefix in Sources(desired))
                foreach (var suffix in CombatSuffixes)
                    // Two-handed weapons retain their native parries and slashing attacks in either grip. Clearing also
                    // removes one-handed parry mappings from the previous version.
                    replace(prefix + suffix, desired == 2 && (suffix.StartsWith("parry") || suffix == "slashl" || suffix == "slashr" || suffix == "slasho" || suffix == "closeh" || suffix == "closel" || suffix == "reach")
                        ? "" : (desired == 1 ? "2h" : "1h") + suffix);
    }
}
