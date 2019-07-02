int StartingConditional()
{
    if(GetItemPossessedBy(GetPCSpeaker(), "DeckBag") == OBJECT_INVALID)
      return TRUE;
    return FALSE;
}
