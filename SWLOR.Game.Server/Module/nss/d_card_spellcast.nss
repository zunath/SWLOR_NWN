//::///////////////////////////////////////////////
//:: Default: On Spell Cast At
//:: NW_C2_DEFAULTB
//:: Copyright (c) 2001 Bioware Corp.
//:://////////////////////////////////////////////
/*
    This determines if the spell just cast at the
    target is harmful or not.
*/
//:://////////////////////////////////////////////
//:: Created By: Preston Watamaniuk
//:: Created On: Dec 6, 2001
//:://////////////////////////////////////////////

#include "NW_I0_GENERIC"
#include "d1_cards_jinc"
void main()
{
    object oCaster = GetLastSpellCaster();
    object oArea = GetArea (OBJECT_SELF);

    if (GetIsPC (oCaster))
        AssignCommand (oArea, ActionEndCardGame (CARD_GAME_END_CHEAT_SPELL, GetCardGamePlayerNumber (oCaster), oArea));

    if (GetIsAvatar() && oCaster == oArea)
        ActionUpdateAvatarHealth();

    if (GetCardID (OBJECT_SELF) && oCaster == oArea && GetLastSpell() == SPELL_RAISE_DEAD)
        ActionUseSpawn();

    if (GetLocalInt (OBJECT_SELF, "CARD_AI_BLOCK"))
    {
        int nEnemy = (GetOwner (OBJECT_SELF) == 1) ? 2 : 1;

        object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, 1, CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY, CREATURE_TYPE_IS_ALIVE, TRUE);

        if (oScan != OBJECT_INVALID)
            ActionMoveAwayFromObject (oScan, TRUE, 10.0f);

        return;
    }


    if(GetLastSpellHarmful())
    {
        if(
         !GetIsObjectValid(GetAttackTarget()) &&
         !GetIsObjectValid(GetAttemptedSpellTarget()) &&
         !GetIsObjectValid(GetAttemptedAttackTarget()) &&
         GetIsObjectValid(GetNearestCreature(CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY, OBJECT_SELF, 1, CREATURE_TYPE_PERCEPTION, PERCEPTION_SEEN))
        )
        {
            if(GetBehaviorState(NW_FLAG_BEHAVIOR_SPECIAL))
            {
                DetermineSpecialBehavior(oCaster);
            }
            else
            {
                DetermineCombatRound(oCaster);
            }
            //Shout Attack my target, only works with the On Spawn In setup
            SpeakString("NW_ATTACK_MY_TARGET", TALKVOLUME_SILENT_TALK);
            //Shout that I was attacked
            SpeakString("NW_I_WAS_ATTACKED", TALKVOLUME_SILENT_TALK);
        }
    }
    if(GetSpawnInCondition(NW_FLAG_SPELL_CAST_AT_EVENT))
    {
        SignalEvent(OBJECT_SELF, EventUserDefined(1011));
    }
}
