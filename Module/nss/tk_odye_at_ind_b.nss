// Krit's Omnidye

// Clears the rightmost digit in the numeric dye selection.
// (For the "Back" options.)

#include "tk_odye_include"

void main()
{
    SetLocalInt(OBJECT_SELF, DYE_SELECTION,
        GetLocalInt(OBJECT_SELF, DYE_SELECTION) / 10);
}

