#include "d1_cards_jinc"

void main()
{
  object oPC = GetPCSpeaker();

  int nSelection = GetMenuSelection();
  object oDeck = GetMenuObjectValue (nSelection - 1);
  SetLocalObject(oPC, "DECK_SELECTED", oDeck);

  object oOpponent = GetLocalObject(oPC, "CARD_OPPONENT");
  SendMessageToPC(oOpponent, GetName(oPC) + " challenges you to a card game.");
  SetLocalObject(oOpponent, "CARD_OPPONENT", oPC);
  AssignCommand(oOpponent, ActionStartConversation(oOpponent, "d1_cardchal", TRUE, FALSE));
}
