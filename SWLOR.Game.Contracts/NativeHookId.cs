namespace SWLOR.Game.Contracts;

/// <summary>
/// The native hooks the game asks the process for, one per hook file of the game's <c>Native/</c> folder. Each id stands
/// for every engine function its file hooks (36 functions in all); <see cref="IHookControl.Enable"/> installs them
/// together, in the order and with the hook order values the file always used.
/// </summary>
public enum NativeHookId
{
    /// <summary>The nine <c>CNWSMessage::SendServerToPlayerChat_*</c> functions.</summary>
    ChatPortraitSpeakers,

    /// <summary><c>CNWMessage::ReadJSON</c>.</summary>
    ClientJsonUtf8Guard,

    /// <summary><c>CNWSEffectListHandler::OnApplyDamage</c> and <c>OnApplyHeal</c>, installed at the first combat recording.</summary>
    CombatRecorderEffectHooks,

    /// <summary><c>CNWSCreatureStats::GetDamageRoll</c>.</summary>
    GetDamageRoll,

    /// <summary><c>CNWSItem::LoadItem</c>, installed at module preload.</summary>
    LegacyItemProperties,

    /// <summary><c>CNWSMessage::AddItemAppearanceToMessage</c>.</summary>
    NeckAppearanceSerialization,

    /// <summary><c>CNWSCreature::AIActionAttackObject</c>.</summary>
    OnAIActionAttackObject,

    /// <summary><c>CServerExoAppInternal::ValidateCreateServerCharacter</c>.</summary>
    PlayerCreation,

    /// <summary><c>CNWSCreature::SaveCreature</c>, <c>LoadCreature</c> and <c>CNWSCreatureStats::ReadStatsFromGff</c>.</summary>
    PlayerFileCheckpoint,

    /// <summary><c>CNWSMessage::SendServerToPlayerPlayerList_Add</c>.</summary>
    PlayerListNamePrivacy,

    /// <summary><c>CNetLayerInternal::SetSlidingWindow</c>.</summary>
    PlayerSlotAssignment,

    /// <summary><c>CNWSCreature::LoadQuickButtons</c>.</summary>
    QuickbarFeatLoading,

    /// <summary><c>CNWSCreature::ResolveAttackRoll</c>.</summary>
    ResolveAttackRoll,

    /// <summary><c>CNWSItem::CompareItem</c>.</summary>
    SavedPlayerInventory,

    /// <summary><c>CNWSCreature::AIActionJumpToPoint</c> and <c>AddDriveAction</c>.</summary>
    StaleDriveAfterJump,

    /// <summary>The seven Vitality hit point functions (<c>CNWSCreature::GetMaxHitPoints</c>, <c>CalcStatModifier</c>, ...).</summary>
    VitalityHitPoints,

    /// <summary><c>CNWSMessage::ComputeUpdateRequired</c> and <c>WriteGameObjUpdate_UpdateObject</c>.</summary>
    WeaponAttackAnimation
}
