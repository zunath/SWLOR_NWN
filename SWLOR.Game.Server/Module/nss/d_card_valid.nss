/**********************************/
/*          d_card_valid
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 3rd November 2003
/**********************************/
/*  Checks for a valid deck.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    int nSelection = GetMenuSelection();

    // select deck based on menu choice
    object oDeck = GetMenuObjectValue (nSelection - 1);
    SetLocalObject(GetPCSpeaker(), "DECK_SELECTED", oDeck);

    // transfer physical cards to variables
    SetDeckVariables(oDeck);


    if(GetIsDeckValid(oDeck, OBJECT_INVALID))
    {
      return FALSE;
    }else{
      string sString = GetLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE");
      SetCustomToken (767, sString);
      return TRUE;
    }
}

