//::///////////////////////////////////////////////
//:: Default On Attacked
//:: NW_C2_DEFAULT5
//:: Copyright (c) 2001 Bioware Corp.
//:://////////////////////////////////////////////
/*
    If already fighting then ignore, else determine
    combat round
*/
//:://////////////////////////////////////////////
//:: Created By: Preston Watamaniuk
//:: Created On: Oct 16, 2001
//:://////////////////////////////////////////////
//:://////////////////////////////////////////////
//:: Modified By: Deva Winblood
//:: Modified On: Jan 4th, 2008
//:: Added Support for Mounted Combat Feat Support
//:://////////////////////////////////////////////

#include "nw_i0_generic"

//DMFI CODE ADDITIONS*****************************
void SafeFaction(object oCurrent, object oAttacker)
{
        AssignCommand(oAttacker, ClearAllActions());
        AssignCommand(oCurrent, ClearAllActions());
        // * Note: waiting for Sophia to make SetStandardFactionReptuation to clear all personal reputation
        if (GetStandardFactionReputation(STANDARD_FACTION_COMMONER, oAttacker) <= 10)
        {   SetLocalInt(oAttacker, "NW_G_Playerhasbeenbad", 10); // * Player bad
            SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 80, oAttacker);
        }
        if (GetStandardFactionReputation(STANDARD_FACTION_MERCHANT, oAttacker) <= 10)
        {   SetLocalInt(oAttacker, "NW_G_Playerhasbeenbad", 10); // * Player bad
            SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 80, oAttacker);
        }
        if (GetStandardFactionReputation(STANDARD_FACTION_DEFENDER, oAttacker) <= 10)
        {   SetLocalInt(oAttacker, "NW_G_Playerhasbeenbad", 10); // * Player bad
            SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 80, oAttacker);
        }


}
//END DMFI CODE ADDITIONS*************************

void main()
{
    ExecuteScript("crea_on_attacked", OBJECT_SELF);
    if(GetLocalInt(OBJECT_SELF, "IGNORE_NWN_EVENTS") == TRUE ||
       GetLocalInt(OBJECT_SELF, "IGNORE_NWN_ON_ATTACKED_EVENT") == TRUE) return;


//DMFI CODE ADDITIONS*****************************
    if ((GetIsPC(GetLastAttacker()) && (GetLocalInt(GetModule(), "dmfi_safe_factions")==1)))
        {
        SafeFaction(OBJECT_SELF, GetLastAttacker());
        SpeakString("DM ALERT:  Default non-hostile faction member attacked.  Player: "+GetName(GetLastAttacker()), TALKVOLUME_SILENT_SHOUT);
        SendMessageToAllDMs("DMFI Safe Faction setting is currently set to ignore.");
        SendMessageToPC(GetLastAttacker(), "Script Fired.");
        return;
        }
//END DMFI CODE ADDITIONS****************************

    if (!GetLocalInt(GetModule(),"X3_NO_MOUNTED_COMBAT_FEAT"))
        { // set variables on target for mounted combat
            SetLocalInt(OBJECT_SELF,"bX3_LAST_ATTACK_PHYSICAL",TRUE);
            SetLocalInt(OBJECT_SELF,"nX3_HP_BEFORE",GetCurrentHitPoints(OBJECT_SELF));
        } // set variables on target for mounted combat

    if(GetFleeToExit()) {
        // Run away!
        ActivateFleeToExit();
    } else if (GetSpawnInCondition(NW_FLAG_SET_WARNINGS)) {
        // We give an attacker one warning before we attack
        // This is not fully implemented yet
        SetSpawnInCondition(NW_FLAG_SET_WARNINGS, FALSE);

        //Put a check in to see if this attacker was the last attacker
        //Possibly change the GetNPCWarning function to make the check
    } else {
        object oAttacker = GetLastAttacker();
        if (!GetIsObjectValid(oAttacker)) {
            // Don't do anything, invalid attacker

        } else if (!GetIsFighting(OBJECT_SELF)) {
            // We're not fighting anyone else, so
            // start fighting the attacker
            if(GetBehaviorState(NW_FLAG_BEHAVIOR_SPECIAL)) {
                SetSummonHelpIfAttacked();
                DetermineSpecialBehavior(oAttacker);
            } else if (GetArea(oAttacker) == GetArea(OBJECT_SELF)) {
                SetSummonHelpIfAttacked();
                DetermineCombatRound(oAttacker);
            }

            //Shout Attack my target, only works with the On Spawn In setup
            SpeakString("NW_ATTACK_MY_TARGET", TALKVOLUME_SILENT_TALK);

            //Shout that I was attacked
            SpeakString("NW_I_WAS_ATTACKED", TALKVOLUME_SILENT_TALK);
        }
    }


    if(GetSpawnInCondition(NW_FLAG_ATTACK_EVENT))
    {
        SignalEvent(OBJECT_SELF, EventUserDefined(EVENT_ATTACKED));
    }
}

