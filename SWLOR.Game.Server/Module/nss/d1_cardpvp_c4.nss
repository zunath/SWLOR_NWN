int StartingConditional()
{

  string sMessage = "You await to see if your opponent accepts your challenge.";

  object oOpponent = GetLocalObject(GetPCSpeaker(), "CARD_OPPONENT");
  if(GetIsInCombat(oOpponent))
    sMessage = "Your opponent is currently in combat and cannot talk.  Wait and challenge them later.";
  if(IsInConversation(oOpponent))
    sMessage = "Your opponent is currently having another conversation.  You may have to challenge them again.";


  SetCustomToken(767, sMessage);

  return TRUE;
}
