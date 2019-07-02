int StartingConditional()
{
  // make sure both players have enough gold

  object oPlayer = GetPCSpeaker();
  object oOpponent = GetLocalObject(oPlayer, "CARD_OPPONENT");

  if(GetArea(oPlayer) != GetArea(oOpponent))
  {
    SetCustomToken (767, "The two of you aren't in the same area.");
    return TRUE;
  }


  int nGold = GetLocalInt(oPlayer, "WAGER_GOLD");
  if(nGold = 0)
    return FALSE;

  if(GetGold(oPlayer) < nGold)
  {
    SetCustomToken (767, "You don't have enough gold to cover your wager.");
    return TRUE;
  }

  if(GetGold(oOpponent) < nGold)
  {
    SetCustomToken (767, "Your opponent doesn't have enough gold to accept your wager.");
    return TRUE;
  }


  return FALSE;
}

