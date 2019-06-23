// Krit's Omnidye

// Sets the dye based on the (standard) index selection.

#include "tk_odye_include"

void main()
{
    // Get the dye that will be set.
    object oDye = GetLocalObject(OBJECT_SELF, DYE_ITEM);

    // Get the dye's index.
    int nColor = GetLocalInt(OBJECT_SELF, DYE_SELECTION);

    // Set the color index to be used by the dying scripts.
    SetLocalInt(oDye, DYE_INDEX, nColor);

    // Rename the dye to be informative.
    SetDyeName(oDye, nColor);
}

