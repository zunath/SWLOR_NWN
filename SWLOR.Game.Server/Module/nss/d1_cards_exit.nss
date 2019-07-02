#include "d1_cards_jinc"
void main()
{
  object oPC = GetLastUsedBy();
  object oConcede = GetNearestObjectByTag("CONCEDE_1", oPC);

  if(GetIsObjectValid(oConcede))
  {
    SendMessageToPC(oPC, "A game is still in progress.  Use this only if you are trapped here after a game ends.");
    return;
  }else{
    SendMessageToPC(oPC, "Using emergency exit.");
    object oExit = GetObjectByTag("GameStoreOwner");
    location lExit = GetLocation(oExit);
    if(oExit == OBJECT_INVALID)
      lExit = GetReturnLocation(oPC);

    AssignCommand(oPC, JumpToLocation(lExit));
  }



}
