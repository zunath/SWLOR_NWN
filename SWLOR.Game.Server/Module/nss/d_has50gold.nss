/**********************************/
/*           d_has50gold
/**********************************/
/*  Checks for gold.
/**********************************/

int StartingConditional()
{
    return GetGold (GetPCSpeaker()) >= 50;
}
