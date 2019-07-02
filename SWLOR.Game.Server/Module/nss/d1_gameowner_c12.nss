/**********************************/
/*          d1_gameowner_c12
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  Prints selected card
/*  information.
/**********************************/

#include "d1_cards_jinc"

int StartingConditional()
{
    struct sCard sInfo = GetCardInfo (GetMenuSelection());

    string sPlus = (sInfo.nBoost) ? "+" : "";
    string sPrint = "\n  * " + sInfo.sName + "*" +
                    "\n" +
                    "\n    Type: " + PrintCardType (sInfo.nType) +
                    "\n    Sub-Type: " + PrintCardSubType (sInfo.nSubType) +
                    "\n" +
                    "\n    Cost: " + IntToString (sInfo.nCost * 2) + "gp" +
                    "\n    Rarity: " + PrintCardRarity (sInfo.nRarity) +
                    "\n" +
                    "\n    Magic Cost: " + IntToString (sInfo.nMagic) + sPlus +
                    "\n    Attack Value: " + IntToString (sInfo.nAttack) +
                    "\n    Defend Value: " + IntToString (sInfo.nDefend) +
                    "\n" +
                    "\n  " + sInfo.sDesc +
                    "\n" +
                    "\n  " + sInfo.sGame;

    SetCustomToken (675, sPrint);

    return TRUE;
}
