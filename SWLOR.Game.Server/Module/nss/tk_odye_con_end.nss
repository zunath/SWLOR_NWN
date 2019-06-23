// Krit's Omnidye

// Deletes all local variables used in the conversation (for tidiness).

#include "tk_odye_include"

void main()
{
    DeleteLocalInt(OBJECT_SELF, DYE_SELECTION);
    DeleteLocalObject(OBJECT_SELF, DYE_ITEM);
}

