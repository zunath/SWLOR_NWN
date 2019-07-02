int StartingConditional()
{
  object oPC = GetPCSpeaker();
  if(GetLocalInt(oPC, "WAGER") > 0)
    return TRUE;
  return FALSE;
}
