int StartingConditional()
{
  object oOpponent = GetLocalObject(GetPCSpeaker(), "CARD_OPPONENT");
  SetCustomToken (768, GetName(oOpponent) + " challenges you to a game.");
  return TRUE;
}
