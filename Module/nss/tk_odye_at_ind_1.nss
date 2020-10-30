// Krit's Omnidye

// Adds a "1" to the right of the numeric dye selection.

#include "tk_odye_include"

void main()
{
    SetLocalInt(OBJECT_SELF, DYE_SELECTION,
        10 * GetLocalInt(OBJECT_SELF, DYE_SELECTION) + 1);
}

