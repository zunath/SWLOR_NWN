#include "dmfi_nui_inc"

int StartingConditional()
{
    // set the custom tokens
    object oPC = DMFI_GetConversationPlayer();
    object oTarget = GetLocalObject(oPC, "dmfi_univ_target");

    string sName = GetName(oTarget);
    SetCustomToken(20680, sName);
    string sOrigName = GetName(oTarget, TRUE);
    SetCustomToken(20681, sOrigName);

    return TRUE;
}
