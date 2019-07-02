//::///////////////////////////////////////////////
//:: Associate: On Dialogue
//:: NW_CH_AC4
//:: Copyright (c) 2001 Bioware Corp.
//:://////////////////////////////////////////////
/*
    Determines the course of action to be taken
    by the generic script after dialogue or a
    shout is initiated.
*/
//:://////////////////////////////////////////////
//:: Created By: Preston Watamaniuk
//:: Created On: Oct 24, 2001
//:://////////////////////////////////////////////


#include "NW_I0_GENERIC"
#include "nw_o0_itemmaker"

void main()
{

// Spell casting listening
  int iListen = GetListenPatternNumber();
  string sCommand;

  if (iListen == 101)
  {


    object oShouter = GetLastSpeaker();

    // ignore non-master
    if(!(GetMaster(OBJECT_SELF) == oShouter))
      return;

    sCommand = GetMatchedSubstring(0);


    int iSpace = FindSubString(sCommand, " ");
    string sAction;
    string sSpell;
    int iSpell = 0;
    object oTarget;

    if(iSpace > 0)
    {
      sAction = GetStringLowerCase(GetSubString(sCommand, 0, iSpace));
      sCommand = GetStringRight(sCommand, GetStringLength(sCommand) - GetStringLength(sAction) - 1);
    }else{
      sAction = sCommand;
    }

    // use nearest object
    if(sAction == "use")
    {
      int iCreatureCount = 1;
      object oTarget = GetNearestObject();
      string sTarget = sCommand;
      SpeakString("Looking for " + sCommand);
      while(iCreatureCount < 20)
      {
        if(GetStringLowerCase(GetName(oTarget)) == GetStringLowerCase(sTarget))
        {
          iCreatureCount = 21;
        }else{
          iCreatureCount++;
          oTarget = GetNearestObject(OBJECT_TYPE_ALL, OBJECT_SELF, iCreatureCount);
        }
      }
      if(oTarget == OBJECT_INVALID)
      {
        SpeakString("I can't find that.");
        return;
      }else{
        SetAssociateState(NW_ASC_MODE_STAND_GROUND,TRUE);
        ClearAllActions();
        ActionForceMoveToObject(oTarget);
        ActionInteractObject(oTarget);
        ActionDoCommand(SetAssociateState(NW_ASC_MODE_STAND_GROUND,FALSE));
        return;
      }
    }

    // mostly for testing
    if(sAction == "vfx")
    {
      SpeakString(sCommand);
      effect eVFX = EffectVisualEffect(StringToInt(sCommand));
      ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eVFX, OBJECT_SELF, 10.0);
    }

    if(sAction == "stealth")
    {
      ActionUseSkill(SKILL_HIDE, OBJECT_SELF);
    }


    if(sAction == "start")
    {
      iSpace = FindSubString(sCommand, " ");
      if(sCommand == "hiding")
      {
        ActionUseSkill(SKILL_HIDE, OBJECT_SELF);
      }
    }

    if(sAction == "stop")
    {
      iSpace = FindSubString(sCommand, " ");
      if(sCommand == "hiding")
      {
        // this breaks stealth
        SpeakString("Understood.");
        ActionUseSkill(SKILL_HIDE, OBJECT_SELF);
        ActionUseSkill(SKILL_MOVE_SILENTLY,OBJECT_SELF);
      }
    }

    // equip melee/ranged
    if(sAction == "equip")
    {
      if(sCommand == "ranged")
      {
        ActionEquipMostDamagingRanged();
      }
      if(sCommand == "melee")
      {
        ActionEquipMostDamagingMelee();
      }
    }

    if(sAction == "cast")
    {
      iSpace = FindSubString(sCommand, " on ");


      if(iSpace == -1)
        iSpace = FindSubString(sCommand, " at ");

      if(iSpace == -1)
      {
        sSpell = sCommand;
        oTarget = OBJECT_SELF;
      }else{
        sSpell = GetStringLowerCase(GetStringLeft(sCommand, iSpace));

        // Find the target
        string sTarget = GetStringLowerCase(GetStringRight(sCommand, GetStringLength(sCommand) - iSpace - 4));
        if(sTarget == "me")
        {
          oTarget = oShouter;
        }

        if(sTarget == "yourself")
        {
          oTarget = OBJECT_SELF;
        }

        if(sTarget == "you")
        {
          oTarget = OBJECT_SELF;
        }

        if(sTarget == "enemy")
        {
          oTarget = GetNearestCreature(CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY);
        }
        if(sTarget == "wizard")
        {
          oTarget = GetNearestCreature(CREATURE_TYPE_CLASS, CLASS_TYPE_WIZARD);
        }
        if(sTarget == "cleric")
        {
          oTarget = GetNearestCreature(CREATURE_TYPE_CLASS, CLASS_TYPE_CLERIC);
        }
        if(sTarget == "sorceror")
        {
          oTarget = GetNearestCreature(CREATURE_TYPE_CLASS, CLASS_TYPE_SORCERER);
        }


        // if not me or you, try to find named object
        if(oTarget == OBJECT_INVALID)
        {
          int iCreatureCount = 1;
          oTarget = GetNearestObject();
          while(iCreatureCount < 20)
          {
            if(GetStringLowerCase(GetName(oTarget)) == GetStringLowerCase(sTarget))
            {
              iCreatureCount = 21;
            }else{
              iCreatureCount++;
              oTarget = GetNearestObject(OBJECT_TYPE_ALL, OBJECT_SELF, iCreatureCount);
            }
          }
        }
      }

      // Loop through array built with OnLoad event
      // Find the spell name
      int nMaxSpells = 900;
      int i = 0;
      string sName;
      for (i = 0; i<=nMaxSpells; i++)
      {
        sName = GetLocalArrayString(GetModule(), "SpellName", i);
        PrintString(sName);
        if(sName == sSpell)
        {
          iSpell = i;
          i = 900;
        }
      }

      if(iSpell > 0)
      {
        if(!(oTarget == OBJECT_INVALID))
        {
          if(GetHasSpell(iSpell, OBJECT_SELF))
          {
            object oPC = oShouter;
            if(GetIsInCombat())
            {
              SetLocalInt(OBJECT_SELF, "SpellToCast", iSpell);
              SetLocalObject(OBJECT_SELF, "SpellToCastAt", oTarget);
              ClearAllActions(TRUE);
              SignalEvent(OBJECT_SELF, EventUserDefined(1003));
              string sMessage = "Waiting to cast " + GetStringByStrRef(StringToInt(Get2DAString("spells", "Name", iSpell))) + " on " + GetName(oTarget);
              SendMessageToPC(oPC, sMessage);
            }else{
              ClearAllActions(TRUE);
              ActionCastSpellAtObject(iSpell, oTarget);
              string sMessage = "Casting " + GetStringByStrRef(StringToInt(Get2DAString("spells", "Name", iSpell))) + " on " + GetName(oTarget);
              SendMessageToPC(oPC, sMessage);
            }
          }else{
            SpeakString("I don't have that spell memorized.");
          }
        }else{
          SpeakString("I can't find that target.");
        }
      }else{
        SpeakString("I'm not familiar with that spell.");
      }

    }

  }



    if(GetCommandable() || GetCurrentAction() != ACTION_OPENLOCK)
    {
        int nMatch = GetListenPatternNumber();
        object oShouter = GetLastSpeaker();
        object oIntruder;
        if (nMatch == -1)
        {
            if(GetAssociate(ASSOCIATE_TYPE_HENCHMAN, GetMaster()) == OBJECT_SELF)
            {
                ClearAllActions();
                BeginConversation();
            }
            else if(!GetIsObjectValid(GetMaster()))
            {
                ClearAllActions();
                BeginConversation();
            }
            else if(GetAssociate(ASSOCIATE_TYPE_FAMILIAR, GetMaster()) == OBJECT_SELF ||
                    GetAssociate(ASSOCIATE_TYPE_ANIMALCOMPANION, GetMaster()) == OBJECT_SELF)
            {
                ClearAllActions();
                BeginConversation();
            }
        }
        else if(GetIsObjectValid(oShouter) && GetMaster() == oShouter)
        {
            SetCommandable(TRUE);
            RespondToShout(oShouter, nMatch, oIntruder);
            if(GetSpawnInCondition(NW_FLAG_ON_DIALOGUE_EVENT))
            {
                SignalEvent(OBJECT_SELF, EventUserDefined(1004));
            }
        }
    }
}

