#include "dmfi_nui_inc"


#include "dmfi_getln_inc"

void main()
{
    object oListener = OBJECT_SELF;
    object oPC = DMFI_GetConversationPlayer();

    // attach our listener event
    SetLocalString(oListener, "dmfi_getln_mode", "name");
    DMFI_get_line(oPC, TALKVOLUME_TALK, "dmfi_univ_listen", oListener);
}
