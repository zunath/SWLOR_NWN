/**********************************/
/*          d_startcardgame
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/**********************************/
/*  Starts a game between this NPC
/*  and the speaker.  Note that
/*  certain NPC information must
/*  have been established prior to
/*  this scripts execution.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nRules = GetDefaultRules();

    if (nRules & CARD_RULE_LAST_DRAW_CONTINUE)
        nRules -= CARD_RULE_LAST_DRAW_CONTINUE;

    if (nRules & CARD_RULE_TRADE_ONE)
        nRules -= CARD_RULE_TRADE_ONE;

    if (nRules & CARD_RULE_TRADE_ALL)
        nRules -= CARD_RULE_TRADE_ALL;

    ActionStartCardGame (GetPCSpeaker(), OBJECT_SELF, nRules);
}
