// Krit's Omnidye

// Called after sampling a color, so that the name can be updated.

#include "tk_odye_include"

void main()
{
    // Rename the dye to be informative.
    SetDyeName(OBJECT_SELF, GetLocalInt(OBJECT_SELF, DYE_INDEX));
    // Provide a sound effect so it seems like something was done.
    AssignCommand(GetItemPossessor(OBJECT_SELF), PlaySound("as_na_steamshrt1"));
}
