//::///////////////////////////////////////////////
//:: Associate: On Damaged
//:: NW_CH_AC6
//:: Copyright (c) 2001 Bioware Corp.
//:://////////////////////////////////////////////
/*
    If already fighting then ignore, else determine
    combat round
*/
//:://////////////////////////////////////////////
//:: Created By: Preston Watamaniuk
//:: Created On: Nov 19, 2001
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

    if(!GetAssociateState(NW_ASC_IS_BUSY))
    {
        if(!GetAssociateState(NW_ASC_MODE_STAND_GROUND) && GetCurrentAction() != ACTION_FOLLOW)
        {
            if(!GetIsObjectValid(GetAttackTarget()) && !GetIsObjectValid(GetAttemptedSpellTarget()))
            {
                if(GetIsObjectValid(GetLastHostileActor()))
                {
                    if(GetAssociateState(NW_ASC_MODE_DEFEND_MASTER))
                    {
                        if(!GetIsObjectValid(GetLastHostileActor(GetMaster())))
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
            else
            {
                object oTarget = GetAttackTarget();
                object oAttacker = GetLastDamager();
                if (GetIsObjectValid(oAttacker) && oTarget != oAttacker && GetIsEnemy(oAttacker) &&
                   ( GetTotalDamageDealt() > (GetMaxHitPoints(OBJECT_SELF) / 4) ||
                    GetHitDice(oAttacker) > GetHitDice(oTarget) ) )
                {
                  //ClearAllActions();
                  DetermineCombatRound(oAttacker);
                }
            }
        }
    }
    if(GetSpawnInCondition(NW_FLAG_DAMAGED_EVENT))
    {
        SignalEvent(OBJECT_SELF, EventUserDefined(1006));
    }
}
