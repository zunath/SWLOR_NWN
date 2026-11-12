using System;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks;

/// <summary>
/// The process's native hooks: the trampolines of the 17 hook files, each with the engine functions it hooks, its install
/// guard and its bound sink. NWNX calls a trampoline for the life of the process, so their state is process-wide (static);
/// every instance of this class is a view of that one set, which the native entry creates for itself and the host hands
/// the game through its services. Installing (<see cref="Enable"/>) and binding (<see cref="Bind"/>) are separate:
/// the game enables each hook from the same handler and at the same moment as it always installed it, and a build binds
/// its sinks when it starts and unbinds them when it is let go of. A trampoline with no sink bound calls the engine
/// function itself; one with a sink reads it from a volatile field and makes one interface call, with no lock, lease or
/// allocation. Every member runs on the engine thread.
/// </summary>
public sealed class NativeHookTable : IHookControl, INativeSinkBinding
{
    private readonly INativeFunctions _native;

    /// <summary>The hooks of the running server: the engine's own symbols and NWNX's hook request.</summary>
    public NativeHookTable()
        : this(NwnxNativeFunctions.Instance)
    {
    }

    internal NativeHookTable(INativeFunctions native)
    {
        _native = native ?? throw new ArgumentNullException(nameof(native));
    }

    /// <summary>
    /// Finds the engine functions of <paramref name="hook"/> without hooking them. The host resolves every hook when it
    /// starts (its extensions' <c>Install</c>); <see cref="Enable"/> resolves a hook that was not resolved yet.
    /// </summary>
    public void Resolve(NativeHookId hook)
    {
        switch (hook)
        {
            case NativeHookId.ChatPortraitSpeakers: ChatPortraitSpeakersTrampolines.Resolve(_native); break;
            case NativeHookId.ClientJsonUtf8Guard: ClientJsonUtf8GuardTrampolines.Resolve(_native); break;
            case NativeHookId.CombatRecorderEffectHooks: CombatRecorderEffectHooksTrampolines.Resolve(_native); break;
            case NativeHookId.GetDamageRoll: GetDamageRollTrampolines.Resolve(_native); break;
            case NativeHookId.LegacyItemProperties: LegacyItemPropertiesTrampolines.Resolve(_native); break;
            case NativeHookId.NeckAppearanceSerialization: NeckAppearanceSerializationTrampolines.Resolve(_native); break;
            case NativeHookId.OnAIActionAttackObject: OnAIActionAttackObjectTrampolines.Resolve(_native); break;
            case NativeHookId.PlayerCreation: PlayerCreationTrampolines.Resolve(_native); break;
            case NativeHookId.PlayerFileCheckpoint: PlayerFileCheckpointTrampolines.Resolve(_native); break;
            case NativeHookId.PlayerListNamePrivacy: PlayerListNamePrivacyTrampolines.Resolve(_native); break;
            case NativeHookId.PlayerSlotAssignment: PlayerSlotAssignmentTrampolines.Resolve(_native); break;
            case NativeHookId.QuickbarFeatLoading: QuickbarFeatLoadingTrampolines.Resolve(_native); break;
            case NativeHookId.ResolveAttackRoll: ResolveAttackRollTrampolines.Resolve(_native); break;
            case NativeHookId.SavedPlayerInventory: SavedPlayerInventoryTrampolines.Resolve(_native); break;
            case NativeHookId.StaleDriveAfterJump: StaleDriveAfterJumpTrampolines.Resolve(_native); break;
            case NativeHookId.VitalityHitPoints: VitalityHitPointsTrampolines.Resolve(_native); break;
            case NativeHookId.WeaponAttackAnimation: WeaponAttackAnimationTrampolines.Resolve(_native); break;
            default: throw Unknown(hook);
        }
    }

    /// <inheritdoc/>
    public void Enable(NativeHookId hook)
    {
        switch (hook)
        {
            case NativeHookId.ChatPortraitSpeakers: ChatPortraitSpeakersTrampolines.Enable(_native); break;
            case NativeHookId.ClientJsonUtf8Guard: ClientJsonUtf8GuardTrampolines.Enable(_native); break;
            case NativeHookId.CombatRecorderEffectHooks: CombatRecorderEffectHooksTrampolines.Enable(_native); break;
            case NativeHookId.GetDamageRoll: GetDamageRollTrampolines.Enable(_native); break;
            case NativeHookId.LegacyItemProperties: LegacyItemPropertiesTrampolines.Enable(_native); break;
            case NativeHookId.NeckAppearanceSerialization: NeckAppearanceSerializationTrampolines.Enable(_native); break;
            case NativeHookId.OnAIActionAttackObject: OnAIActionAttackObjectTrampolines.Enable(_native); break;
            case NativeHookId.PlayerCreation: PlayerCreationTrampolines.Enable(_native); break;
            case NativeHookId.PlayerFileCheckpoint: PlayerFileCheckpointTrampolines.Enable(_native); break;
            case NativeHookId.PlayerListNamePrivacy: PlayerListNamePrivacyTrampolines.Enable(_native); break;
            case NativeHookId.PlayerSlotAssignment: PlayerSlotAssignmentTrampolines.Enable(_native); break;
            case NativeHookId.QuickbarFeatLoading: QuickbarFeatLoadingTrampolines.Enable(_native); break;
            case NativeHookId.ResolveAttackRoll: ResolveAttackRollTrampolines.Enable(_native); break;
            case NativeHookId.SavedPlayerInventory: SavedPlayerInventoryTrampolines.Enable(_native); break;
            case NativeHookId.StaleDriveAfterJump: StaleDriveAfterJumpTrampolines.Enable(_native); break;
            case NativeHookId.VitalityHitPoints: VitalityHitPointsTrampolines.Enable(_native); break;
            case NativeHookId.WeaponAttackAnimation: WeaponAttackAnimationTrampolines.Enable(_native); break;
            default: throw Unknown(hook);
        }
    }

    /// <inheritdoc/>
    public bool IsEnabled(NativeHookId hook)
    {
        return hook switch
        {
            NativeHookId.ChatPortraitSpeakers => ChatPortraitSpeakersTrampolines.IsEnabled,
            NativeHookId.ClientJsonUtf8Guard => ClientJsonUtf8GuardTrampolines.IsEnabled,
            NativeHookId.CombatRecorderEffectHooks => CombatRecorderEffectHooksTrampolines.IsEnabled,
            NativeHookId.GetDamageRoll => GetDamageRollTrampolines.IsEnabled,
            NativeHookId.LegacyItemProperties => LegacyItemPropertiesTrampolines.IsEnabled,
            NativeHookId.NeckAppearanceSerialization => NeckAppearanceSerializationTrampolines.IsEnabled,
            NativeHookId.OnAIActionAttackObject => OnAIActionAttackObjectTrampolines.IsEnabled,
            NativeHookId.PlayerCreation => PlayerCreationTrampolines.IsEnabled,
            NativeHookId.PlayerFileCheckpoint => PlayerFileCheckpointTrampolines.IsEnabled,
            NativeHookId.PlayerListNamePrivacy => PlayerListNamePrivacyTrampolines.IsEnabled,
            NativeHookId.PlayerSlotAssignment => PlayerSlotAssignmentTrampolines.IsEnabled,
            NativeHookId.QuickbarFeatLoading => QuickbarFeatLoadingTrampolines.IsEnabled,
            NativeHookId.ResolveAttackRoll => ResolveAttackRollTrampolines.IsEnabled,
            NativeHookId.SavedPlayerInventory => SavedPlayerInventoryTrampolines.IsEnabled,
            NativeHookId.StaleDriveAfterJump => StaleDriveAfterJumpTrampolines.IsEnabled,
            NativeHookId.VitalityHitPoints => VitalityHitPointsTrampolines.IsEnabled,
            NativeHookId.WeaponAttackAnimation => WeaponAttackAnimationTrampolines.IsEnabled,
            _ => throw Unknown(hook)
        };
    }

    /// <inheritdoc/>
    public void Bind(NativeSinkSet sinks)
    {
        if (sinks == null)
            throw new ArgumentNullException(nameof(sinks));

        ChatPortraitSpeakersTrampolines.Bind(sinks.ChatPortraitSpeakers);
        ClientJsonUtf8GuardTrampolines.Bind(sinks.ClientJsonUtf8Guard);
        CombatRecorderEffectHooksTrampolines.Bind(sinks.CombatRecorderEffectHooks);
        GetDamageRollTrampolines.Bind(sinks.GetDamageRoll);
        LegacyItemPropertiesTrampolines.Bind(sinks.LegacyItemProperties);
        NeckAppearanceSerializationTrampolines.Bind(sinks.NeckAppearanceSerialization);
        OnAIActionAttackObjectTrampolines.Bind(sinks.OnAIActionAttackObject);
        PlayerCreationTrampolines.Bind(sinks.PlayerCreation);
        PlayerFileCheckpointTrampolines.Bind(sinks.PlayerFileCheckpoint);
        PlayerListNamePrivacyTrampolines.Bind(sinks.PlayerListNamePrivacy);
        PlayerSlotAssignmentTrampolines.Bind(sinks.PlayerSlotAssignment);
        QuickbarFeatLoadingTrampolines.Bind(sinks.QuickbarFeatLoading);
        ResolveAttackRollTrampolines.Bind(sinks.ResolveAttackRoll);
        SavedPlayerInventoryTrampolines.Bind(sinks.SavedPlayerInventory);
        StaleDriveAfterJumpTrampolines.Bind(sinks.StaleDriveAfterJump);
        VitalityHitPointsTrampolines.Bind(sinks.VitalityHitPoints);
        WeaponAttackAnimationTrampolines.Bind(sinks.WeaponAttackAnimation);
    }

    /// <inheritdoc/>
    public void Unbind(NativeSinkSet sinks)
    {
        if (sinks == null)
            throw new ArgumentNullException(nameof(sinks));

        ChatPortraitSpeakersTrampolines.Unbind(sinks.ChatPortraitSpeakers);
        ClientJsonUtf8GuardTrampolines.Unbind(sinks.ClientJsonUtf8Guard);
        CombatRecorderEffectHooksTrampolines.Unbind(sinks.CombatRecorderEffectHooks);
        GetDamageRollTrampolines.Unbind(sinks.GetDamageRoll);
        LegacyItemPropertiesTrampolines.Unbind(sinks.LegacyItemProperties);
        NeckAppearanceSerializationTrampolines.Unbind(sinks.NeckAppearanceSerialization);
        OnAIActionAttackObjectTrampolines.Unbind(sinks.OnAIActionAttackObject);
        PlayerCreationTrampolines.Unbind(sinks.PlayerCreation);
        PlayerFileCheckpointTrampolines.Unbind(sinks.PlayerFileCheckpoint);
        PlayerListNamePrivacyTrampolines.Unbind(sinks.PlayerListNamePrivacy);
        PlayerSlotAssignmentTrampolines.Unbind(sinks.PlayerSlotAssignment);
        QuickbarFeatLoadingTrampolines.Unbind(sinks.QuickbarFeatLoading);
        ResolveAttackRollTrampolines.Unbind(sinks.ResolveAttackRoll);
        SavedPlayerInventoryTrampolines.Unbind(sinks.SavedPlayerInventory);
        StaleDriveAfterJumpTrampolines.Unbind(sinks.StaleDriveAfterJump);
        VitalityHitPointsTrampolines.Unbind(sinks.VitalityHitPoints);
        WeaponAttackAnimationTrampolines.Unbind(sinks.WeaponAttackAnimation);
    }

    private static ArgumentOutOfRangeException Unknown(NativeHookId hook) =>
        new(nameof(hook), hook, "No such native hook.");
}
