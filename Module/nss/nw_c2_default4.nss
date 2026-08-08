//:://////////////////////////////////////////////////
//:: NW_C2_DEFAULT4
/*
  Default OnConversation event handler for NPCs.

 */
//:://////////////////////////////////////////////////
//:: Copyright (c) 2002 Floodgate Entertainment
//:: Created By: Naomi Novik
//:: Created On: 12/22/2002
//:://////////////////////////////////////////////////

#include "nw_i0_generic"

void RunDialogueTailHooks()
{
    // Send the user-defined event if appropriate
    if(GetSpawnInCondition(NW_FLAG_ON_DIALOGUE_EVENT))
    {
        SignalEvent(OBJECT_SELF, EventUserDefined(EVENT_DIALOGUE));
    }

    ExecuteScript("crea_convo_aft", OBJECT_SELF);
}

void main()
{
    DeleteLocalInt(OBJECT_SELF, "SWLOR_NUI_CONVO");
    // * if petrified, jump out
    if (GetHasEffect(EFFECT_TYPE_PETRIFY, OBJECT_SELF) == TRUE)
    {
        ExecuteScript("crea_convo_aft", OBJECT_SELF);
        return;
    }

    // * If dead, exit directly.
    if (GetIsDead(OBJECT_SELF) == TRUE)
    {
        ExecuteScript("crea_convo_aft", OBJECT_SELF);
        return;
    }

    // See if what we just 'heard' matches any of our
    // predefined patterns
    int nMatch = GetListenPatternNumber();
    object oShouter = GetLastSpeaker();

    if (nMatch == -1)
    {
        // Not a match -- start an ordinary conversation
        // * July 31 2004
        // * If only charmed then allow conversation
        // * so you can have a better chance of convincing
        // * people of lowering prices
        if (GetCommandable(OBJECT_SELF) ||
            GetHasEffect(EFFECT_TYPE_CHARMED) == TRUE)
        {
            ExecuteScript("crea_convo_bef", OBJECT_SELF);
            if (GetLocalInt(OBJECT_SELF, "SWLOR_NUI_CONVO"))
            {
                DeleteLocalInt(OBJECT_SELF, "SWLOR_NUI_CONVO");
                ClearActions(CLEAR_NW_C2_DEFAULT4_29);
                RunDialogueTailHooks();
                return;
            }

            ClearActions(CLEAR_NW_C2_DEFAULT4_29);
            BeginConversation();
        }
    }
    // Respond to shouts from friendly non-PCs only
    else if (GetIsObjectValid(oShouter)
               && !GetIsPC(oShouter)
               && GetIsFriend(oShouter))
    {
        object oIntruder = OBJECT_INVALID;
        // Determine the intruder if any
        if(nMatch == 4)
        {
            oIntruder = GetLocalObject(oShouter, "NW_BLOCKER_INTRUDER");
        }
        else if (nMatch == 5)
        {
            oIntruder = GetLastHostileActor(oShouter);
            if(!GetIsObjectValid(oIntruder))
            {
                oIntruder = GetAttemptedAttackTarget();
                if(!GetIsObjectValid(oIntruder))
                {
                    oIntruder = GetAttemptedSpellTarget();
                    if(!GetIsObjectValid(oIntruder))
                    {
                        oIntruder = OBJECT_INVALID;
                    }
                }
            }
        }

        // Actually respond to the shout
        //RespondToShout(oShouter, nMatch, oIntruder);
    }

    RunDialogueTailHooks();
}
