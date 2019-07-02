/**********************************/
/*           d_has200gold
/**********************************/
/*  Checks for gold.
/**********************************/

int StartingConditional()
{
    return GetGold (GetPCSpeaker()) >= 200;
}
