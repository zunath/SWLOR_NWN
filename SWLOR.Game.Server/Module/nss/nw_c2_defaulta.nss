//::///////////////////////////////////////////////
//:: Default: On Rested
//:: NW_C2_DEFAULTA
//:: Copyright (c) 2002 Bioware Corp.
//:://////////////////////////////////////////////
/*
    Determines the course of action to be taken
    after having just rested.
*/
//:://////////////////////////////////////////////
//:: Created By: Don Moar
//:: Created On: April 28, 2002
//:://////////////////////////////////////////////

void main()
{
    ExecuteScript("crea_on_rested", OBJECT_SELF);

    if(GetLocalInt(OBJECT_SELF, "IGNORE_NWN_EVENTS") == TRUE ||
       GetLocalInt(OBJECT_SELF, "IGNORE_NWN_ON_RESTED_EVENT") == TRUE) return;

}
