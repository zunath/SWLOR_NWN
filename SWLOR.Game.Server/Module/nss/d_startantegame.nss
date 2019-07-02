/**********************************/
/*          d_startcardgame
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/**********************************/
/*  Starts a game between this NPC
/*  and the speaker where the
/*  winner takes one card from the
/*  opponent.  Note that this is
/*  only for card ante, not gold.
/*  Note that certain NPC
/*  information must have been
/*  established prior to this
/*  scripts execution.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRules = GetDefaultRules();

    if (nRules & CARD_RULE_LAST_DRAW_CONTINUE)
        nRules -= CARD_RULE_LAST_DRAW_CONTINUE;

    if (nRules & CARD_RULE_TRADE_ALL)
        nRules -= CARD_RULE_TRADE_ALL;

    if (nRules & CARD_RULE_TRADE_ONE)
        nRules -= CARD_RULE_TRADE_ONE;


    if(GetNPCAnteBet(OBJECT_SELF))
        nRules = nRules + CARD_RULE_TRADE_ONE;

    ActionStartCardGame (GetPCSpeaker(), OBJECT_SELF, nRules);
}
