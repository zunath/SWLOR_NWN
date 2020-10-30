//:://////////////////////////////////////////////////
//:: NW_C2_DEFAULT8
/*
  Default OnDisturbed event handler for NPCs.
 */
//:://////////////////////////////////////////////////
//:: Copyright (c) 2002 Floodgate Entertainment
//:: Created By: Naomi Novik
//:: Created On: 12/22/2002
//:://////////////////////////////////////////////////

#include "nw_i0_generic"

void main()
{
    ExecuteScript("crea_on_disturb", OBJECT_SELF);
    if(GetLocalInt(OBJECT_SELF, "IGNORE_NWN_EVENTS") == TRUE ||
       GetLocalInt(OBJECT_SELF, "IGNORE_NWN_ON_DISTURBED_EVENT") == TRUE) return;


    object oTarget = GetLastDisturbed();

    // If we've been disturbed and are not already fighting,
    // attack our disturber.
    if (GetIsObjectValid(oTarget) && !GetIsFighting(OBJECT_SELF)) {
        DetermineCombatRound(oTarget);
    }

    // Send the disturbed flag if appropriate.
    if(GetSpawnInCondition(NW_FLAG_DISTURBED_EVENT)) {
        SignalEvent(OBJECT_SELF, EventUserDefined(EVENT_DISTURBED));
    }

}
