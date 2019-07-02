/**********************************/
/*           d_has100gold
/**********************************/
/*  Checks for gold.
/**********************************/

int StartingConditional()
{
    return GetGold (GetPCSpeaker()) >= 100;
}
