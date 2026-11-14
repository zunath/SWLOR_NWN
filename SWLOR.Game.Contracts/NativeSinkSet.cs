using System;

namespace SWLOR.Game.Contracts;

/// <summary>One game build's sinks, one per hook file, which <see cref="INativeSinkBinding"/> binds and unbinds together.</summary>
public sealed class NativeSinkSet
{
    public NativeSinkSet(
        IChatPortraitSpeakersSink chatPortraitSpeakers,
        IClientJsonUtf8GuardSink clientJsonUtf8Guard,
        ICombatRecorderEffectHooksSink combatRecorderEffectHooks,
        IGetDamageRollSink getDamageRoll,
        ILegacyItemPropertiesSink legacyItemProperties,
        INeckAppearanceSerializationSink neckAppearanceSerialization,
        IOnAIActionAttackObjectSink onAIActionAttackObject,
        IPlayerCreationSink playerCreation,
        IPlayerFileCheckpointSink playerFileCheckpoint,
        IPlayerListNamePrivacySink playerListNamePrivacy,
        IPlayerSlotAssignmentSink playerSlotAssignment,
        IQuickbarFeatLoadingSink quickbarFeatLoading,
        IResolveAttackRollSink resolveAttackRoll,
        ISavedPlayerInventorySink savedPlayerInventory,
        IStaleDriveAfterJumpSink staleDriveAfterJump,
        IVitalityHitPointsSink vitalityHitPoints,
        IWeaponAttackAnimationSink weaponAttackAnimation)
    {
        ChatPortraitSpeakers = chatPortraitSpeakers ?? throw new ArgumentNullException(nameof(chatPortraitSpeakers));
        ClientJsonUtf8Guard = clientJsonUtf8Guard ?? throw new ArgumentNullException(nameof(clientJsonUtf8Guard));
        CombatRecorderEffectHooks = combatRecorderEffectHooks ?? throw new ArgumentNullException(nameof(combatRecorderEffectHooks));
        GetDamageRoll = getDamageRoll ?? throw new ArgumentNullException(nameof(getDamageRoll));
        LegacyItemProperties = legacyItemProperties ?? throw new ArgumentNullException(nameof(legacyItemProperties));
        NeckAppearanceSerialization = neckAppearanceSerialization ?? throw new ArgumentNullException(nameof(neckAppearanceSerialization));
        OnAIActionAttackObject = onAIActionAttackObject ?? throw new ArgumentNullException(nameof(onAIActionAttackObject));
        PlayerCreation = playerCreation ?? throw new ArgumentNullException(nameof(playerCreation));
        PlayerFileCheckpoint = playerFileCheckpoint ?? throw new ArgumentNullException(nameof(playerFileCheckpoint));
        PlayerListNamePrivacy = playerListNamePrivacy ?? throw new ArgumentNullException(nameof(playerListNamePrivacy));
        PlayerSlotAssignment = playerSlotAssignment ?? throw new ArgumentNullException(nameof(playerSlotAssignment));
        QuickbarFeatLoading = quickbarFeatLoading ?? throw new ArgumentNullException(nameof(quickbarFeatLoading));
        ResolveAttackRoll = resolveAttackRoll ?? throw new ArgumentNullException(nameof(resolveAttackRoll));
        SavedPlayerInventory = savedPlayerInventory ?? throw new ArgumentNullException(nameof(savedPlayerInventory));
        StaleDriveAfterJump = staleDriveAfterJump ?? throw new ArgumentNullException(nameof(staleDriveAfterJump));
        VitalityHitPoints = vitalityHitPoints ?? throw new ArgumentNullException(nameof(vitalityHitPoints));
        WeaponAttackAnimation = weaponAttackAnimation ?? throw new ArgumentNullException(nameof(weaponAttackAnimation));
    }

    public IChatPortraitSpeakersSink ChatPortraitSpeakers { get; }
    public IClientJsonUtf8GuardSink ClientJsonUtf8Guard { get; }
    public ICombatRecorderEffectHooksSink CombatRecorderEffectHooks { get; }
    public IGetDamageRollSink GetDamageRoll { get; }
    public ILegacyItemPropertiesSink LegacyItemProperties { get; }
    public INeckAppearanceSerializationSink NeckAppearanceSerialization { get; }
    public IOnAIActionAttackObjectSink OnAIActionAttackObject { get; }
    public IPlayerCreationSink PlayerCreation { get; }
    public IPlayerFileCheckpointSink PlayerFileCheckpoint { get; }
    public IPlayerListNamePrivacySink PlayerListNamePrivacy { get; }
    public IPlayerSlotAssignmentSink PlayerSlotAssignment { get; }
    public IQuickbarFeatLoadingSink QuickbarFeatLoading { get; }
    public IResolveAttackRollSink ResolveAttackRoll { get; }
    public ISavedPlayerInventorySink SavedPlayerInventory { get; }
    public IStaleDriveAfterJumpSink StaleDriveAfterJump { get; }
    public IVitalityHitPointsSink VitalityHitPoints { get; }
    public IWeaponAttackAnimationSink WeaponAttackAnimation { get; }
}
