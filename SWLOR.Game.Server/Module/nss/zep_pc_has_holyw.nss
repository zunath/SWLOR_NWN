//::///////////////////////////////////////////////
//:: zep_pc_has_holyw
//:://////////////////////////////////////////////
/*
    Checks if the PC has the special Holy Water item
    needed to destroy a demilich.
    Despite the name, this function will check for
    whatever item is specified by ZEP_DEMI_DEST_TAG.

    Also defines custom token 1001 to hold the name
    of the item specified by ZEP_DEMI_DEST_TAG.

    Used by conversation zep_demi_regen_c.
*/
//:://////////////////////////////////////////////
//:: Created by: ??
//:: Created on: ??
//:://////////////////////////////////////////////
//:: Modified by: The Krit
//:: Modified on: May 10, 2007
//:://////////////////////////////////////////////


#include "zep_inc_demi"


int StartingConditional()
{
    object oPC=GetPCSpeaker();

    // Check if PC has the demilich-destroying item.
    // (Referred to as "HolyWater" in code since that is the default.)
    object oHolyWater = GetItemPossessedBy(oPC, ZEP_DEMI_DEST_TAG);

    // If PC does not have the right item, return FALSE.
    if ( oHolyWater == OBJECT_INVALID )
        return FALSE;

    // Store the name of the special item in custom token 1001.
    SetCustomToken(1001, GetName(oHolyWater));

    // PC has the item, so return TRUE.
    return TRUE;
}

