//::///////////////////////////////////////////////
//:: tk_omnidye
//::
//:: Tag-based script.
//:://////////////////////////////////////////////
/*
    Causes a player to start a self-conversation
    upon item activation.

    Also initializes an item upon acquisition.
*/
//:://////////////////////////////////////////////
//:: Created By: The Krit
//:: Created On: October 11, 2007
//:://////////////////////////////////////////////


#include "x2_inc_switches"
#include "tk_odye_include"


void main()
{
    // We're only implementing activation and acquisition.
    int nEvent = GetUserDefinedItemEventNumber();
    if ( nEvent != X2_ITEM_EVENT_ACTIVATE  &&  nEvent != X2_ITEM_EVENT_ACQUIRE )
        return;

    // Signal that the script handled this.
    SetExecutedScriptReturnValue();

    // Initializations for when acquired.
    if ( nEvent == X2_ITEM_EVENT_ACQUIRE )
    {
        object oItem = GetModuleItemAcquired();
        // Set the name.
        SetDyeName(oItem, GetLocalInt(oItem, DYE_INDEX));
        // Set the post-sampling script.
        // (Cannot rely on template variable because a store might have
        //  cleared all local variables.)
        SetLocalString(oItem, "DYE_POSTSAMPLE_HANDLER", "tk_odye_setname");
        // Done.
        return;
    }

    // Activation:
    else
    {
        object oItem = GetItemActivated();
        object oPC = GetItemActivator();

        // Remember the item that was used.
        SetLocalObject(oPC, DYE_ITEM, oItem);

        // Start the conversation that will allow the dye's color to be set.
        AssignCommand(oPC, ClearAllActions());
        AssignCommand(oPC, ActionStartConversation(oPC, "tk_omnidye", TRUE, FALSE));
    }
}

