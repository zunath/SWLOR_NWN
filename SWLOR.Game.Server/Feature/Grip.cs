using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature;

/// <summary>A cosmetic melee weapon grip preference. It changes combat poses and attacks, never combat stats.</summary>
public static class Grip
{
    private const string EnabledVariable = "DOUBLEHAND_ENABLED";
    private const string ModeVariable = "DOUBLEHAND_MODE";
    private const string TransitionTokenVariable = "GRIP_PARRY_TOKEN";
    private const string ParryOwnedVariable = "GRIP_PARRY_OWNED";
    private const string RightHandVariable = "GRIP_RIGHT_HAND";
    private const string LeftHandVariable = "GRIP_LEFT_HAND";

    // A named playback calls this before claiming the shared dodges alias.
    internal static void InvalidateTransition(uint creature, bool releaseMapping = true)
    {
        SetLocalInt(creature, TransitionTokenVariable, GetLocalInt(creature, TransitionTokenVariable) + 1);
        if (releaseMapping && GetLocalBool(creature, ParryOwnedVariable))
            ReplaceObjectAnimation(creature, "dodges", "");
        SetLocalBool(creature, ParryOwnedVariable, false);
    }

    // Reapplies only the carry alias, which a finished named animation clears even while a queued swing owns the combat aliases.
    internal static void RestoreCarryAlias(uint creature)
    {
        if (GetLocalInt(creature, ModeVariable) == 2)
            ReplaceObjectAnimation(creature, "plpause1", NamedAnimationPlayback.NoHoldName);
    }

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

    // Called before the katar owner restores its mappings, so releasing Grip cannot erase them.
    internal static void Refresh(uint creature, bool forceRefresh = false, bool playTransition = false)
    {
        if (!GetIsObjectValid(creature)) return;
        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        var left = GetItemInSlot(InventorySlot.LeftHand, creature);
        var desired = GetMode(GetLocalBool(creature, EnabledVariable),
            GetIsObjectValid(right) ? GetBaseItemType(right) : null, GetIsObjectValid(left) && left != right);
        var applied = GetLocalInt(creature, ModeVariable);
        if (desired != applied || right != GetLocalObject(creature, RightHandVariable) ||
            left != GetLocalObject(creature, LeftHandVariable) || playTransition)
            InvalidateTransition(creature);
        SetLocalObject(creature, RightHandVariable, right);
        SetLocalObject(creature, LeftHandVariable, left);
        // Equipment changes also invalidate timers while a queued swing owns the combat mappings.
        if (QueuedAttackAnimation.IsActive(creature)) return;
        ApplyTransition(desired, applied, forceRefresh,
            (source, destination) => ReplaceObjectAnimation(creature, source, destination));
        SetLocalInt(creature, ModeVariable, desired);
        if (ShouldPlayTransition(desired, applied, playTransition,
                GetIsObjectValid(right) && GetMode(true, GetBaseItemType(right), false) != 0))
        {
            var token = GetLocalInt(creature, TransitionTokenVariable);
            AssignCommand(creature, () =>
            {
                if (!GetIsObjectValid(creature) || GetLocalInt(creature, TransitionTokenVariable) != token ||
                    GetLocalInt(creature, ModeVariable) != desired ||
                    GetItemInSlot(InventorySlot.RightHand, creature) != right ||
                    GetItemInSlot(InventorySlot.LeftHand, creature) != left) return;
                // Clear the existing shoulder overlay by its original name before suppressing
                // future carry layers. A full-body pause clip is not a carry-layer replacement.
                ReplaceObjectAnimation(creature, "plpause1", "");
                var enabled = GetLocalBool(creature, EnabledVariable);
                if (enabled && GetMode(true, GetBaseItemType(right), false) == 2)
                {
                    // The native one-shot clears the carry overlay; show double-weapon parry.
                    ReplaceObjectAnimation(creature, "dodges", "plparryl");
                    SetLocalBool(creature, ParryOwnedVariable, true);
                    PlayAnimation(Animation.FireForgetDodgeSide);
                    DelayCommand(1.0f, () =>
                    {
                        if (!GetIsObjectValid(creature) || GetLocalInt(creature, TransitionTokenVariable) != token ||
                            !GetLocalBool(creature, ParryOwnedVariable)) return;
                        ReplaceObjectAnimation(creature, "dodges", "");
                        SetLocalBool(creature, ParryOwnedVariable, false);
                    });
                }
                else
                {
                    // Keep the existing one-handed activation and all deactivation emotes.
                    PlayAnimation(enabled ? Animation.ClassicJediStance : Animation.DoubleLSStance, 1.0f, 1.2f);
                }
                // Let the client remove the original overlay before changing its lookup.
                // Otherwise the removal can resolve to sw_nohold and leave plpause1 attached.
                DelayCommand(0.3f, () =>
                {
                    if (!GetIsObjectValid(creature) || GetLocalInt(creature, TransitionTokenVariable) != token ||
                        GetLocalInt(creature, ModeVariable) != 2 ||
                        GetItemInSlot(InventorySlot.RightHand, creature) != right ||
                        GetItemInSlot(InventorySlot.LeftHand, creature) != left) return;
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
