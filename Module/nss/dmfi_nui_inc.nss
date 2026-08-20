// Provides the active player to DMFI scripts launched by the NUI conversation runtime.
// Native DLG execution does not set this local and continues to use the engine speaker.
object DMFI_GetConversationPlayer()
{
    object oPlayer = GetLocalObject(OBJECT_SELF, "SWLOR_NUI_CONVERSATION_PLAYER");
    return GetIsObjectValid(oPlayer) ? oPlayer : GetPCSpeaker();
}

object DMFI_GetConversationLastSpeaker()
{
    object oPlayer = GetLocalObject(OBJECT_SELF, "SWLOR_NUI_CONVERSATION_PLAYER");
    return GetIsObjectValid(oPlayer) ? oPlayer : GetLastSpeaker();
}
