using SWLOR.Game.Contracts;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Creates this build's sinks, one per hook file, for the native hooks to forward to. Both entries bind them through
/// <see cref="INativeSinkBinding"/>: the native entry when it boots (<c>ServerBootstrapper</c>), the host entry when it
/// activates the generation (<c>GameGeneration</c>). A sink's constructor only creates its own empty state.
/// </summary>
public static class NativeSinkFactory
{
    /// <summary>A new set of the game's sinks.</summary>
    public static NativeSinkSet Create()
    {
        return new NativeSinkSet(
            new ChatPortraitSpeakersSink(),
            new ClientJsonUtf8GuardSink(),
            new CombatRecorderEffectHooksSink(),
            new GetDamageRollSink(),
            new LegacyItemPropertiesSink(),
            new NeckAppearanceSerializationSink(),
            new OnAIActionAttackObjectSink(),
            new PlayerCreationSink(),
            new PlayerFileCheckpointSink(),
            new PlayerListNamePrivacySink(),
            new PlayerSlotAssignmentSink(),
            new QuickbarFeatLoadingSink(),
            new ResolveAttackRollSink(),
            new SavedPlayerInventorySink(),
            new StaleDriveAfterJumpSink(),
            new VitalityHitPointsSink(),
            new WeaponAttackAnimationSink());
    }
}
