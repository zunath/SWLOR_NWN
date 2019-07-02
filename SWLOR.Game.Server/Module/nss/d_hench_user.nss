void main()
{
  int nUser = GetUserDefinedEventNumber();

    if(nUser == 1001) //HEARTBEAT
    {
      // Try conversation again with master
      string sDialog = GetLocalString(OBJECT_SELF, "Dialog");
      if(GetStringLength(sDialog) > 0)
      {
        if(IsInConversation(OBJECT_SELF) == FALSE)
        {
          object oMaster = GetMaster();
          if(GetIsObjectValid(oMaster))
          {
            ClearAllActions();
            ActionForceMoveToObject(oMaster, TRUE);
            ActionStartConversation(oMaster, sDialog);
          }
        }
      }
  }

  //On Combat Round End
  if(nUser = 1003)
  {
    // See if a spell is queued up
    int iSpell = GetLocalInt(OBJECT_SELF, "SpellToCast");
    if(iSpell > 0)
    {
      // Just wait if currently casting a spell
      if(GetCurrentAction() == ACTION_CASTSPELL)
        return;
      object oTarget = GetLocalObject(OBJECT_SELF, "SpellToCastAt");
      string sMessage = "Casting " + GetStringByStrRef(StringToInt(Get2DAString("spells", "Name", iSpell))) + " on " + GetName(oTarget);
      object oPC = GetMaster(OBJECT_SELF);
      if(oPC == OBJECT_INVALID)
        oPC = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC);
      SendMessageToPC(oPC, sMessage);
      ClearAllActions();
      ActionCastSpellAtObject(iSpell, oTarget);
      ActionDoCommand(SetLocalInt(OBJECT_SELF, "SpellToCast", 0));
    }
  }
}
