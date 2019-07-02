#include "d1_cards_jinc"
int StartingConditional()
{

  // Don't allow in the main campaign
  if(GetIsDebug())
    return TRUE;
  if(GetIsDM(GetPCSpeaker()))
    return TRUE;
  return FALSE;
}
