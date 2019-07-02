void main()
{
  // Added by Adam
  // Toggles game information on/off
  object oPlayer = GetLastUsedBy();
  AssignCommand (oPlayer, ClearAllActions());
  AssignCommand (oPlayer, ActionStartConversation (oPlayer, "d_gamedeckman", TRUE, FALSE));
  if(GetLocalInt(oPlayer, "GAMEINFO") == 1)
  {
    DeleteLocalInt(oPlayer, "GAMEINFO");
  }else{
    SetLocalInt(oPlayer, "GAMEINFO", 1);
  }

}
