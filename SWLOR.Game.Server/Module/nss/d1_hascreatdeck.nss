/**********************************/
/*          d1_hascreatdeck
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 22nd November 2003
/**********************************/
/*  Checks for creature deck item.
/**********************************/

int StartingConditional()
{
    object oSpeaker = GetPCSpeaker();
    object oInv = GetFirstItemInInventory (oSpeaker);

    while (oInv != OBJECT_INVALID)
    {
        if (GetStringLeft (GetTag (oInv), 13) == "CreaturesDeck")
            break;

        oInv = GetNextItemInInventory (oSpeaker);
    }

    return oInv != OBJECT_INVALID;
}
