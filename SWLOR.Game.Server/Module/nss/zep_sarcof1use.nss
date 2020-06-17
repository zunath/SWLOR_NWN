//:://////////////////////////////////////////////
//:: Created By: Robert Babiak
//:: Created On: June 25, 2002
//:://////////////////////////////////////////////
//:: Modified By: Andrew Nobbs
//:: Modified On: September 23, 2002
//:: Modification: Removed unnecessary spaces.
//:://////////////////////////////////////////////
//::
//:: Modified By: John Hawkins
//:: Modified On: January, 2004
//:: Modification: Changed code
//::
//:://////////////////////////////////////////////
/*
    now the destination will be a waypoint with
    the tag of "WP_"+the tag of the object of
    origin.(the secret door) see the placeable's
    comments for further directions.
                    *****
    ***you never have to change this script!***
                    *****
*/
//:://////////////////////////////////////////////
//Yet further modified by: Loki Hakanin
//Summary of Alterations:
//-----------------------
//Created a function "SendAllAssociatesOfType" to encapsulate
//functionality in main function, and also to allow for potential
//of multople summons or henchmen or whatever.  Originaly script would
//only send first associate of a given type, this begavior
//now fixed.
//:://////////////////////////////////////////////

#include "zep_inc_scrptdlg"

//Sends oCreature to oDest instantly
void SendCreature(object oCreature, object oDest);

//Sends all creatures of nAssociateType associated with oUser to oDest.
void SendAllAssociatesOfType(int nAssociateType,object oUser, object oDest);

void main()
{
    object oidUser = GetLastUsedBy();
    string sDest = "WP_"+GetTag(OBJECT_SELF);
    object oidDest = GetObjectByTag(sDest);
    if (GetIsTrapped(OBJECT_SELF))
    {
    return;
    }
    if (GetLocked(OBJECT_SELF))
    {
        // See if we have the key and unlock if so
        string sKey = GetTrapKeyTag(OBJECT_SELF);
        object oKey = GetItemPossessedBy(oidUser, sKey);
        if (sKey != "" && GetIsObjectValid(oKey))
        {
            SendMessageToPC(oidUser, GetStringByStrRef(7945));
            SetLocked(OBJECT_SELF, FALSE);
        }
        else
        {
            // Print '*locked*' message and play sound
            DelayCommand(0.1, PlaySound("as_dr_locked2"));
            FloatingTextStringOnCreature("*"
                                         + GetStringByStrRef(8307)
                                         + "*",
                                         oidUser);
            SendMessageToPC(oidUser, GetStringByStrRef(8296));
            return;
        }
    }
    if(GetIsOpen(OBJECT_SELF))
        {
        AssignCommand(oidUser,ActionJumpToObject(oidDest,FALSE));
        //Send All Animal Companions
        SendAllAssociatesOfType(ASSOCIATE_TYPE_ANIMALCOMPANION,oidUser,oidDest);
        //Send all Dominated Creatures
        SendAllAssociatesOfType(ASSOCIATE_TYPE_DOMINATED,oidUser,oidDest);
        //Send all Familiars
        SendAllAssociatesOfType(ASSOCIATE_TYPE_FAMILIAR,oidUser,oidDest);
        //Send all henchmen
        SendAllAssociatesOfType(ASSOCIATE_TYPE_HENCHMAN,oidUser,oidDest);
        //Send all summons
        SendAllAssociatesOfType(ASSOCIATE_TYPE_SUMMONED,oidUser,oidDest);
        }
    else
        {
        DelayCommand(1.0,FloatingTextStringOnCreature(GetStringByStrRef(nZEPFoundSecret,GENDER_MALE),oidUser));
            {
            DelayCommand(0.1,AssignCommand(OBJECT_SELF,PlaySound("as_dr_stonmedop1")));
            DelayCommand(0.3,AssignCommand(OBJECT_SELF,PlayAnimation(ANIMATION_PLACEABLE_OPEN)));
            }
        }
}

void SendCreature(object oCreature, object oDest)
{
    if(oCreature != OBJECT_INVALID)
    {
        AssignCommand(oCreature, ClearAllActions());
        AssignCommand(oCreature, ActionJumpToObject(oDest,FALSE));
    }
}

void SendAllAssociatesOfType(int nAssociateType, object oUser, object oDest)
{
int nCounter=1;
object oCreatureToSend = GetAssociate(nAssociateType, oUser, nCounter);
while (GetIsObjectValid(oCreatureToSend))
    {
    SendCreature(oCreatureToSend, oDest);
    nCounter++;
    oCreatureToSend = GetAssociate(nAssociateType, oUser,nCounter);
    }
}

