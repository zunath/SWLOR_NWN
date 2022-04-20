
#include "dmfi_getln_inc"

void main()
{
    object oListener = OBJECT_SELF;
    object oPC = GetPCSpeaker();

    // attach our listener event
    SetLocalString(oListener, "dmfi_getln_mode", "name");
    DMFI_get_line(oPC, TALKVOLUME_TALK, "dmfi_univ_listen", oListener);
}
