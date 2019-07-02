/**********************************/
/*          d1_gamedeck_c1
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 24th February 2004
/**********************************/
/*  Prints out certain in-game
/*  information on player/deck
/*  status.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    object oPlayer = GetPCSpeaker();
    object oArea = GetArea (oPlayer);
    object oCentre = GetGameCentre (oArea);

    int nPlayer = GetCardGamePlayerNumber (oPlayer);
    int nDeck = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;

    SetCustomToken (951, IntToString (GetTotalCards (nDeck, oArea)));
    SetCustomToken (952, PrintDiscardPile (nPlayer, oArea));
    SetCustomToken (953, PrintAvatarHealth (oCentre));
    SetCustomToken (954, PrintPlayerGeneratorCounts (oArea));

    return TRUE;
}
