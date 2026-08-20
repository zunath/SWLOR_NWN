#include "dmfi_nui_inc"

int StartingConditional()
{
    // set the custom tokens
    object oPC = DMFI_GetConversationPlayer();
    object oTarget = GetLocalObject(oPC, "dmfi_univ_target");

    string sName = GetDescription(oTarget);
    SetCustomToken(20682, sName);
    string sOrigName = GetDescription(oTarget, TRUE);
    SetCustomToken(20683, sOrigName);

    return TRUE;
}
