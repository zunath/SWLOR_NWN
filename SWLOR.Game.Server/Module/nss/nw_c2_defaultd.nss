//::///////////////////////////////////////////////
//:: Default: On User Defined
//:: NW_C2_DEFAULTD
//:: Copyright (c) 2002 Bioware Corp.
//:://////////////////////////////////////////////
/*
    Determines the course of action to be taken
    on a user defined event.
*/
//:://////////////////////////////////////////////
//:: Created By: Don Moar
//:: Created On: April 28, 2002
//:://////////////////////////////////////////////

void main()
{
    ExecuteScript("crea_on_userdef", OBJECT_SELF);

    if(GetLocalInt(OBJECT_SELF, "IGNORE_NWN_EVENTS") == TRUE ||
       GetLocalInt(OBJECT_SELF, "IGNORE_NWN_ON_USER_DEFINED_EVENT") == TRUE) return;
}
