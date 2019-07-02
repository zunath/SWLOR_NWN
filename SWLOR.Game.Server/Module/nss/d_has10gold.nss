/**********************************/
/*           d_has10gold
/**********************************/
/*  Checks for gold.
/**********************************/

int StartingConditional()
{
    return GetGold (GetPCSpeaker()) >= 10;
}
