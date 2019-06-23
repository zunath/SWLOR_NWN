//::///////////////////////////////////////////////
//:: ZEP_OPENCLOSE.nss
//:: Copyright (c) 2001 Bioware Corp.
//:: Modified by Dan Heidel 1/14/04 for CEP
//:://////////////////////////////////////////////
/*
    This function goes in the OnUse function of an
    openable placeable or door.  Do not use for
    placeables which have an inventory, only for those
    where clicking should trigger the open/close anim
    without opening up an inventory.  Eg: sarcophagii,
    grandfather clock and iron maiden.  If you wish for
    these items to have an inventory make a copy of the
    placeable with this function removed and the 'has
    inventory' box checked.

    No local variables are needed if the placeable is
    not a door.

    If the placeable is a door, CEP_L_GATEBLOCK is a
    local string containing the blueprint resref of the
    gateblock corresponding to the particular door.

    A gateblock is an invisible placeable with a walkmesh
    of the size, shape and orientation of the door.  In
    the case of CEP Door 01-11, there is a corresponding
    Gateblock 01-11.  Therefore, for Door01, the CEP_L_GATEBLOCK
    should be gateblock001 - the resref for Gateblock 01.

    When the door is closed, a gateblock placeable is created
    so that the door is impassable.  When the door is opened,
    the gateblock is destroyed so the gate is passable.

    GateBlock is a local object that stores the gateblock
    being used by a particular door.  No user intervention is
    needed other than to not create a local variable with the same
    name.

    In addition, zep_doorspawn must be placed in the heartbeat
    function of the door and zep_doorkill must be placed in the
    OnDestruct function of the door.

*/
//:://////////////////////////////////////////////
//:: Created By:  Brent
//:: Created On:  January 2002
//:: Modified by: Dan Heidel 1-21-04 for CEP
//:://////////////////////////////////////////////

#include "zep_inc_scrptdlg"

void main()
{
    string sGateBlock = GetLocalString(OBJECT_SELF, "CEP_L_GATEBLOCK");
    location lSelfLoc = GetLocation(OBJECT_SELF);
    int nIsOpen = GetIsOpen(OBJECT_SELF);

    if (GetLocked(OBJECT_SELF) == 1){
        //FloatingTextStringOnCreature("Locked", OBJECT_SELF);
        string sLockedMSG = GetStringByStrRef(nZEPDoorLocked,GENDER_MALE);
        SpeakString(sLockedMSG);
        return;
    }
    //if the object is locked, it cannot be opened or closed

    if (sGateBlock == ""){    // if the item is not a door
        if (nIsOpen == 0)
        {
            PlayAnimation(ANIMATION_PLACEABLE_OPEN);
        }
        else
        {
             PlayAnimation(ANIMATION_PLACEABLE_CLOSE);
         }
        return;
    }

    if (nIsOpen == 0)   //if the item is a door
    {
        object oSelf = OBJECT_SELF;
        PlayAnimation(ANIMATION_PLACEABLE_OPEN);
        if (GetLocalObject(oSelf, "GateBlock")!= OBJECT_INVALID)
        {
            DestroyObject(GetLocalObject(oSelf, "GateBlock"));
            SetLocalObject(oSelf, "GateBlock", OBJECT_INVALID);
        }
    }
    else
    {
        object oSelf = OBJECT_SELF;
        PlayAnimation(ANIMATION_PLACEABLE_CLOSE);
        SetLocalObject(oSelf, "GateBlock", CreateObject(OBJECT_TYPE_PLACEABLE, sGateBlock, lSelfLoc));
    }
}
