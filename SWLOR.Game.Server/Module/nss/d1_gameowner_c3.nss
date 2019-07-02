/**********************************/
/*          d1_gameowner_c3
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 25th February 2004
/**********************************/
/*  Prints the result of the rule
/*  change.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    int nNth;
    int nRule = GetMenuSelection (1);
    int nType = GetMenuSelection (2);
    int nDefaults = GetDefaultRules();
    int nSize = GetMenuSize();

    for (; nNth <= nSize; nNth++)
    {
        int nValue;

        switch (nType)
        {
            case 1:
                nValue = (nNth == 0) ? CARD_RULE_DECK_20 :
                         (nNth == 1) ? CARD_RULE_DECK_30 :
                         (nNth == 2) ? CARD_RULE_DECK_40 :
                         (nNth == 3) ? CARD_RULE_DECK_50 :
                         (nNth == 4) ? CARD_RULE_DECK_60 : 0;

                break;

            case 2:
                nValue = (nNth == 0) ? CARD_RULE_DRAW_3 :
                         (nNth == 1) ? CARD_RULE_DRAW_5 :
                         (nNth == 2) ? CARD_RULE_DRAW_7 :
                         (nNth == 3) ? CARD_RULE_DRAW_9 : 0;

                break;

            case 3:
                nValue = (nNth == 0) ? CARD_RULE_RESTRICT_2 :
                         (nNth == 1) ? CARD_RULE_RESTRICT_3 :
                         (nNth == 2) ? CARD_RULE_RESTRICT_4 :
                         (nNth == 3) ? CARD_RULE_RESTRICT_X : 0;

                break;

            case 4:
                nValue = (nNth == 0) ? CARD_RULE_LIMIT_1 :
                         (nNth == 1) ? CARD_RULE_LIMIT_2 :
                         (nNth == 2) ? CARD_RULE_LIMIT_3 :
                         (nNth == 3) ? CARD_RULE_LIMIT_4 :
                         (nNth == 4) ? CARD_RULE_LIMIT_5 :
                         (nNth == 5) ? CARD_RULE_LIMIT_8 :
                         (nNth == 6) ? CARD_RULE_LIMIT_10 : 0;

                break;

            case 5:
                nValue = (nNth == 0) ? CARD_RULE_TRADE_ONE :
                         (nNth == 1) ? CARD_RULE_TRADE_ALL : 0;

                break;


            case 6:
                nValue = (nNth == 0) ? CARD_RULE_LAST_DRAW_CONTINUE :
                         (nNth == 1) ? CARD_RULE_LAST_DRAW_DEATH : 0;

                break;
        }

        if (nValue && nDefaults & nValue)
        {
            nDefaults -= nValue;

            SetDefaultRules (nDefaults);
        }
    }

    if (nRule != CARD_RULE_NORMAL)
    {
        nDefaults += nRule;

        SetDefaultRules (nDefaults);
    }

    SetCustomToken (670, PrintRules (nDefaults));

    if (GetMenuValue (0, 3) != nDefaults)
    {
        object oPC = GetFirstPC();

        while (oPC != OBJECT_INVALID)
        {
            SendMessageToPC (oPC, "NOTE: The default card rules have been altered.  The next game you start will have new rules.");

            oPC = GetNextPC();
        }
    }

    SetMenuValueAmount (3);

    return TRUE;
}
