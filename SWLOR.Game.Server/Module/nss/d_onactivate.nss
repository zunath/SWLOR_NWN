/**********************************/
/*           d_onactivate
/**********************************/
/*  OnItemActivated
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oItem = GetItemActivated();
    object oPC = GetItemActivator();
    object oTarget = GetItemActivatedTarget();
    object oArea = GetArea (oTarget);

    string sItem = GetTag (oItem);

    if (GetStringLeft (sItem, 13) == "CreaturesDeck")
    {
        int nCount = GetCardItemsInDeck(oItem);
        AssignCommand (oPC, SendMessageToPC(oPC, "This deck has " + IntToString(nCount) + " cards."));

    }

    if (sItem == "CARD_SACRIFICE")
        AssignCommand (oTarget, ActionUseSacrifice (GetCardGamePlayerNumber (oPC)));


}
