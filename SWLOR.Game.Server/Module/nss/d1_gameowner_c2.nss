/**********************************/
/*          d1_gameowner_c2
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 25th February 2004
/**********************************/
/*  Constructs options for default
/*  rule selected.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    int nNth;
    int nSize = 6;
    int nType = GetMenuSelection();

    string sPrint = (nType == 1) ? "What maximum deck size do you wish to set the default limit to?" :
                    (nType == 2) ? "What maximum hand size do you wish to set the default limit to?" :
                    (nType == 3) ? "What maximum limit on individual cards in a deck do you wish to set the default limit to?" :
                    (nType == 4) ? "What maximum limit on magic generators in play do you wish to set the default limit to?" :
                    (nType == 5) ? "What rules do you wish to set with regard to ante in multiplayer games?" :
                     "What rules do you wish to set with regard to what happens when a player draws their last card?";

    ClearMenu();
    SetMenuValue (GetDefaultRules(), 0, 3);

    for (; nNth <= nSize; nNth++)
    {
        int nValue;

        string sToken;

        switch (nType)
        {
            case 1:
                nValue = (nNth == 0) ? CARD_RULE_DECK_20 :
                         (nNth == 1) ? CARD_RULE_DECK_30 :
                         (nNth == 2) ? CARD_RULE_DECK_40 :
                         (nNth == 3) ? CARD_RULE_DECK_50 :
                         (nNth == 4) ? CARD_RULE_DECK_60 : 0;

                sToken = (nNth == 0) ? "20" :
                         (nNth == 1) ? "30" :
                         (nNth == 2) ? "40" :
                         (nNth == 3) ? "50" :
                         (nNth == 4) ? "60" : "";

                break;

            case 2:
                nValue = (nNth == 0) ? CARD_RULE_DRAW_3 :
                         (nNth == 1) ? CARD_RULE_DRAW_5 :
                         (nNth == 2) ? CARD_RULE_DRAW_7 :
                         (nNth == 3) ? CARD_RULE_DRAW_9 : 0;

                sToken = (nNth == 0) ? "3" :
                         (nNth == 1) ? "5" :
                         (nNth == 2) ? "7" :
                         (nNth == 3) ? "9" : "";

                break;

            case 3:
                nValue = (nNth == 0) ? CARD_RULE_RESTRICT_2 :
                         (nNth == 1) ? CARD_RULE_RESTRICT_3 :
                         (nNth == 2) ? CARD_RULE_RESTRICT_4 :
                         (nNth == 3) ? CARD_RULE_RESTRICT_X : 0;

                sToken = (nNth == 0) ? "2" :
                         (nNth == 1) ? "3" :
                         (nNth == 2) ? "4" :
                         (nNth == 3) ? "No Restriction" : "";

                break;

            case 4:
                nValue = (nNth == 0) ? CARD_RULE_LIMIT_1 :
                         (nNth == 1) ? CARD_RULE_LIMIT_2 :
                         (nNth == 2) ? CARD_RULE_LIMIT_3 :
                         (nNth == 3) ? CARD_RULE_LIMIT_4 :
                         (nNth == 4) ? CARD_RULE_LIMIT_5 :
                         (nNth == 5) ? CARD_RULE_LIMIT_8 :
                         (nNth == 6) ? CARD_RULE_LIMIT_10 : 0;

                sToken = (nNth == 0) ? "1" :
                         (nNth == 1) ? "2" :
                         (nNth == 2) ? "3" :
                         (nNth == 3) ? "4" :
                         (nNth == 4) ? "5" :
                         (nNth == 5) ? "8" :
                         (nNth == 6) ? "10" : "";

                break;

            case 5:
                nValue = (nNth == 0) ? CARD_RULE_TRADE_ONE :
                         (nNth == 1) ? CARD_RULE_TRADE_ALL : 0;

                sToken = (nNth == 0) ? "Winner takes one card from opponent." :
                         (nNth == 1) ? "Winner takes opponent's deck." : "";

                break;

            case 6:
                nValue = (nNth == 0) ? CARD_RULE_LAST_DRAW_CONTINUE :
                         (nNth == 1) ? CARD_RULE_LAST_DRAW_DEATH : 0;

                sToken = (nNth == 0) ? "Game continues until one player dies or concedes." :
                         (nNth == 1) ? "Game ends when a player draws their last card." : "";

                break;
        }

        SetCustomToken (660 + nNth, sToken);
        SetMenuValue (nValue, nNth, 1);
        SetMenuValue (nType, nNth, 2);
    }

    SetMenuValue (CARD_RULE_NORMAL, nSize + 1, 1);
    SetMenuValue (nType, nSize + 1, 2);
    SetCustomToken (670, sPrint);

    SetMenuSize (nSize + 1);
    SetMenuValueAmount (2);

    return TRUE;
}
