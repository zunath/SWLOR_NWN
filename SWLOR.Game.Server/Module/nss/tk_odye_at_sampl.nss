// Krit's Omnidye

// Sets the dye to receive (sample) a color the next time it is used.

#include "tk_odye_include"

void main()
{
    // Get the dye that will be set.
    object oDye = GetLocalObject(OBJECT_SELF, DYE_ITEM);

    // Set the color index to be "sampling mode".
    SetLocalInt(oDye, DYE_INDEX, -2);

    // Rename the dye to be informative.
    SetName(oDye, GetName(oDye, TRUE) + ": matching mode");
}

