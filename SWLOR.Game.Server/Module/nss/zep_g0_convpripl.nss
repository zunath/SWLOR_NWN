//::///////////////////////////////////////////////
//:: zep_g0_convpripl
//:://////////////////////////////////////////////
/*
    Cause a placeable object to start a
    private conversation with the PC.
    (Private conversations seem more appropriate for
    conversations with no spoken words, such as one
    that describes interactions with an object.)

    Use this script as the OnUsed event
    of a placeable object that is flagged as
    useable, has NO inventory, and is NOT static.

    c.f. nw_g0_convplac.nss
*/
//:://////////////////////////////////////////////
//:: Created By: The Krit
//:: Created On: Jan 08, 2008
//:: Based on nw_g0_convplac by Sydney Tang (BioWare)
//:://////////////////////////////////////////////

void main()
{
    ActionStartConversation(GetLastUsedBy(), "", TRUE, FALSE);
}
