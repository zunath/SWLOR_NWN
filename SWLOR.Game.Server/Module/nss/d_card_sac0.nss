/**********************************/
/*          d_card_sac0
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 15th November 2003
/**********************************/
/*  Constructs the sacrifice list.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    int nStart = GetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_CYCLE");
    int nPlayer = GetCardGamePlayerNumber (GetPCSpeaker());
    int nToken = 0;

    struct sCard sInfo;

    if (!nStart)
        nStart = 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (GetArea (OBJECT_SELF)));
    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nStart, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID && nToken <= 9)
    {
        if (GetOwner (oCycle) == nPlayer)
        {
            sInfo = GetCardInfo (GetCardID (oCycle));

            if (sInfo.nSacrifice)
            {
                SetLocalObject (OBJECT_SELF, "CARD_SACRIFICE_OBJECT" + IntToString (++nToken), oCycle);
                SetCustomToken (990 + nToken, GetName (oCycle) + " (" + IntToString (GetCurrentHitPoints (oCycle)) + "/" + IntToString (GetMaxHitPoints (oCycle)) + ")");
            }
        }

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nStart, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (nToken > 9 && oCycle != OBJECT_INVALID)
    {
        SetLocalInt (OBJECT_SELF, "CARD_SACRIFICE_MORE", TRUE);
    }
    else
    {
        while (++nToken <= 9)
        {
            DeleteLocalObject (OBJECT_SELF, "CARD_SACRIFICE_OBJECT" + IntToString (nToken));
            SetCustomToken (990 + nToken, "CARD-ERROR");
        }
    }
}
