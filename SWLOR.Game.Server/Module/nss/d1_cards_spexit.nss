#include "d1_cards_jinc"
void main()
{
  object oPC = GetLastUsedBy();

  SendMessageToPC(oPC, "Using spectator exit.");
  object oExit = GetObjectByTag("GameStoreOwner");
  location lExit = GetLocation(oExit);
  AssignCommand(oPC, JumpToLocation(lExit));
}
