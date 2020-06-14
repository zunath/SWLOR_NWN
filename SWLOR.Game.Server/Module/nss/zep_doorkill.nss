//::///////////////////////////////////////////////
//:: ZEP_DOORkill.nss
//:: Copyright (c) 2001 Bioware Corp.
//:: Modified by Dan Heidel 1/14/04 for CEP
//:: Modified by TJ Rayn (Bioware name: TheExcimer-500) 3/11/06 for CEP2
//::                   - added lines to create new placeable on death.
//:://////////////////////////////////////////////
/*
    Place in the OnDestruct function of a placeable door
    to ensure proper functioning.  See zep_openclose for
    further documentation.

    For create new placeable - add Local Variable on Object:
   Type: string, Variable Name: CEP_L_DIEREPLACE   Value: <use resref of object to create>.
*/
//:://////////////////////////////////////////////
//:: Created By:  Brent
//:: Created On:  January 2002
//:://////////////////////////////////////////////

void main()
{
    string sGateBlock = GetLocalString(OBJECT_SELF, "CEP_L_GATEBLOCK");
    location lSelfLoc = GetLocation(OBJECT_SELF);
    int nIsOpen = GetIsOpen(OBJECT_SELF);

    if (nIsOpen == 0)   //if the door is closed
    {
        object oSelf = OBJECT_SELF;
        if (GetLocalObject(oSelf, "GateBlock")!= OBJECT_INVALID)
        {
            DestroyObject(GetLocalObject(oSelf, "GateBlock"));
        }
    }

//Create New Placeable on Death
    string sDieReplace = GetLocalString(OBJECT_SELF, "CEP_L_DIEREPLACE");
    if (sDieReplace!="")
{CreateObject(OBJECT_TYPE_PLACEABLE, sDieReplace,lSelfLoc);}
}



