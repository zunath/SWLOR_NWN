//::///////////////////////////////////////////////
//:: Associate On Attacked
//:: NW_CH_AC5
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

#include "NW_I0_GENERIC"

void main()
{

    //Don't fight back - just cast spell if queued.
    if(GetLocalInt(OBJECT_SELF, "SpellToCast") > 0)
    {
        SignalEvent(OBJECT_SELF, EventUserDefined(1003));
        return;
    }

    SpeakString("NW_I_WAS_ATTACKED", TALKVOLUME_SILENT_TALK);
    if(!GetAssociateState(NW_ASC_IS_BUSY))
    {
        SetCommandable(TRUE);
        if(!GetAssociateState(NW_ASC_MODE_STAND_GROUND))
        {
            if(!GetIsObjectValid(GetAttackTarget()) && !GetIsObjectValid(GetAttemptedSpellTarget()))
            {
                if(GetIsObjectValid(GetLastAttacker()))
                {
                    if(GetAssociateState(NW_ASC_MODE_DEFEND_MASTER))
                    {
                        if(!GetIsObjectValid(GetLastAttacker(GetMaster())))
                        {
                            DetermineCombatRound();
                        }
                    }
                    else
                    {
                        DetermineCombatRound();
                    }

                }
            }
        }
    }
}

