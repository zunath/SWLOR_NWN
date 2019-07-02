/**********************************/
/*          d1_gameowner_c10
/**********************************/
/*  Checks for a deck object.  Also
/*  checks if cards were purchase
/*  from the game owner.
/**********************************/

int StartingConditional()
{
    object oSpeaker = GetPCSpeaker();
    object oInv = GetFirstItemInInventory (oSpeaker);

    while (oInv != OBJECT_INVALID)
    {
        if (GetStringLeft (GetTag (oInv), 7) == "CardBag")
          return FALSE;

        oInv = GetNextItemInInventory (oSpeaker);
    }

    return !GetLocalInt (GetPCSpeaker(), "PurchasedCards");
}
