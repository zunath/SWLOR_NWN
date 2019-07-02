//::///////////////////////////////////////////////
//:: Associate: Heartbeat
//:: NW_CH_AC1.nss
//:: Copyright (c) 2001 Bioware Corp.
//:://////////////////////////////////////////////
/*
    Move towards master or wait for him
*/
//:://////////////////////////////////////////////
//:: Created By: Preston Watamaniuk
//:: Created On: Nov 21, 2001
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
        object oMaster = GetMaster();
        //Seek out and disable undisabled traps
        object oTrap = GetNearestTrapToObject();
        if(GetIsObjectValid(oTrap) && GetDistanceToObject(oTrap) < 15.0 && GetDistanceToObject(oTrap) > 0.0)
        {
            object oTrapSaved = GetLocalObject(OBJECT_SELF, "NW_ASSOCIATES_LAST_TRAP");
            int nTrapDC = GetTrapDisarmDC(oTrap);
            int nSkill = GetSkillRank(SKILL_DISABLE_TRAP);
            nSkill = nSkill + 20 - nTrapDC;

            if(nSkill > 0 && GetSkillRank(SKILL_DISABLE_TRAP) > 0)
            {
                if( GetIsObjectValid(oMaster)
                    && nSkill > 0
                    && !IsInConversation(OBJECT_SELF)
                    && !GetIsInCombat()
                    && GetCurrentAction(OBJECT_SELF) != ACTION_REST
                    && GetCurrentAction(OBJECT_SELF) != ACTION_DISABLETRAP)
                {
                    ClearAllActions();
                    ActionUseSkill(SKILL_DISABLE_TRAP, oTrap);
                    ActionDoCommand(SetCommandable(TRUE));
                    ActionDoCommand(PlayVoiceChat(VOICE_CHAT_TASKCOMPLETE));
                    SetCommandable(FALSE);
                    return;
                }
            }
            else if(oTrap != oTrapSaved && GetSkillRank(SKILL_DISABLE_TRAP) > 0)
            {
                PlayVoiceChat(VOICE_CHAT_CANTDO);
                SetLocalObject(OBJECT_SELF, "NW_ASSOCIATES_LAST_TRAP", oTrap);
            }
        }
        if(GetIsObjectValid(oMaster) &&
            GetCurrentAction(OBJECT_SELF) != ACTION_FOLLOW &&
            GetCurrentAction(OBJECT_SELF) != ACTION_DISABLETRAP &&
            GetCurrentAction(OBJECT_SELF) != ACTION_OPENLOCK &&
            GetCurrentAction(OBJECT_SELF) != ACTION_REST &&
            GetCurrentAction(OBJECT_SELF) != ACTION_ATTACKOBJECT)
        {
            if(
               !GetIsObjectValid(GetAttackTarget()) &&
               !GetIsObjectValid(GetAttemptedSpellTarget()) &&
               !GetIsObjectValid(GetAttemptedAttackTarget()) &&
               !GetIsObjectValid(GetNearestCreature(CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY, OBJECT_SELF, 1, CREATURE_TYPE_PERCEPTION, PERCEPTION_SEEN))
              )
            {
                if(GetDistanceToObject(GetMaster()) > 6.0)
                {
                    if(GetAssociateState(NW_ASC_HAVE_MASTER))
                    {
                        if(!GetIsFighting(OBJECT_SELF))
                        {
                            if(!GetAssociateState(NW_ASC_MODE_STAND_GROUND))
                            {
                                if(GetDistanceToObject(GetMaster()) > GetFollowDistance())
                                {
                                    ClearAllActions();
                                    if(GetAssociateState(NW_ASC_AGGRESSIVE_STEALTH) || GetAssociateState(NW_ASC_AGGRESSIVE_SEARCH))
                                    {
                                         if(GetAssociateState(NW_ASC_AGGRESSIVE_STEALTH))
                                         {
                                            //ActionUseSkill(SKILL_HIDE, OBJECT_SELF);
                                            //ActionUseSkill(SKILL_MOVE_SILENTLY,OBJECT_SELF);
                                         }
                                         if(GetAssociateState(NW_ASC_AGGRESSIVE_SEARCH))
                                         {
                                            ActionUseSkill(SKILL_SEARCH, OBJECT_SELF);
                                         }
                                         MyPrintString("GENERIC SCRIPT DEBUG STRING ********** " + "Assigning Force Follow Command with Search and/or Stealth");
                                         ActionForceFollowObject(oMaster, GetFollowDistance());
                                    }
                                    else
                                    {
                                         MyPrintString("GENERIC SCRIPT DEBUG STRING ********** " + "Assigning Force Follow Normal");
                                         ActionForceFollowObject(oMaster, GetFollowDistance());
                                         //ActionForceMoveToObject(GetMaster(), TRUE, GetFollowDistance(), 5.0);
                                    }
                                }
                            }
                        }
                    }
                }
                else if(!GetAssociateState(NW_ASC_MODE_STAND_GROUND))
                {
                    if(GetIsObjectValid(oMaster))
                    {
                        if(GetCurrentAction(oMaster) != ACTION_REST)
                        {
                            ClearAllActions();
                            if(GetAssociateState(NW_ASC_AGGRESSIVE_STEALTH) || GetAssociateState(NW_ASC_AGGRESSIVE_SEARCH))
                            {
                                 if(GetAssociateState(NW_ASC_AGGRESSIVE_STEALTH))
                                 {
                                    //ActionUseSkill(SKILL_HIDE, OBJECT_SELF);
                                    //ActionUseSkill(SKILL_MOVE_SILENTLY,OBJECT_SELF);
                                 }
                                 if(GetAssociateState(NW_ASC_AGGRESSIVE_SEARCH))
                                 {
                                    ActionUseSkill(SKILL_SEARCH, OBJECT_SELF);
                                 }
                                 MyPrintString("GENERIC SCRIPT DEBUG STRING ********** " + "Assigning Force Follow Command with Search and/or Stealth");
                                 ActionForceFollowObject(oMaster, GetFollowDistance());
                            }
                            else
                            {
                                 MyPrintString("GENERIC SCRIPT DEBUG STRING ********** " + "Assigning Force Follow Normal");
                                 ActionForceFollowObject(oMaster, GetFollowDistance());
                            }
                        }
                    }
                }
            }
            else if(!GetIsObjectValid(GetAttackTarget()) &&
               !GetIsObjectValid(GetAttemptedSpellTarget()) &&
               !GetIsObjectValid(GetAttemptedAttackTarget()) &&
               !GetAssociateState(NW_ASC_MODE_STAND_GROUND))
            {
                //DetermineCombatRound();
            }

        }
        if(GetSpawnInCondition(NW_FLAG_HEARTBEAT_EVENT))
        {
            SignalEvent(OBJECT_SELF, EventUserDefined(1001));
        }
    }
}



