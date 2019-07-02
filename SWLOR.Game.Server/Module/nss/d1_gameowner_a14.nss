/**********************************/
/*          d1_gameowner_a14
/**********************************/
/*  Creates deck object being
/*  purchased.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    string sDeck = GetLocalString(OBJECT_SELF, "decktopurchase");

    object oPlayer = GetPCSpeaker();
    object oDeck = CreateObject (OBJECT_TYPE_ITEM, sDeck, GetLocation (OBJECT_SELF), FALSE);

    CopyObject (oDeck, GetLocation (oPlayer), oPlayer, "CreaturesDeck" + IntToString (Random (30000)) + IntToString (Random (30000)) + IntToString (Random (30000)));
    DestroyObject (oDeck);
}
