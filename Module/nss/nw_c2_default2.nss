//:://////////////////////////////////////////////////
//:: NW_C2_DEFAULT2
/*
  Default OnPerception event handler for NPCs.

  Handles behavior when perceiving a creature for the
  first time.
 */
//:://////////////////////////////////////////////////

#include "nw_i0_generic"

void main()
{
    ExecuteScript("crea_perc_bef", OBJECT_SELF);

    ExecuteScript("crea_perc_aft", OBJECT_SELF);
}
