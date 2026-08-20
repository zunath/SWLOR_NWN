#include "dmfi_nui_inc"

void main()
{
    object oPC = DMFI_GetConversationPlayer();
    object oTarget = GetLocalObject(oPC, "dmfi_univ_target");
    SetName(oTarget, "");
}
