#include "d1_cards_jinc"
void main()
{
  // fill the store with cards each time
  object oStore = GetObjectByTag("CardShopStore");
  int nCards = 0;
  struct sCard sSell;

    object oInv = GetFirstItemInInventory (oStore);
    while (oInv != OBJECT_INVALID)
    {
        string sTag = GetTag (oInv);
        if(GetStringLeft(sTag, 8) == "di_card_")
          nCards++;
        oInv = GetNextItemInInventory (oStore);
    }

  int nMax = 90;
  int nRnd;
  int nCount = 0;
  while( (nCards < nMax) && (nCount < 200))
  {
    nRnd = Random (CARD_MAX_ID) + 1;
    if (nRnd >= CARD_MAX_ID)
      nRnd = CARD_MAX_ID - 1;
    sSell = GetCardInfo (nRnd);
    if (GetRarityAllowed (sSell.nRarity))
    {
      CreateItemOnObject("di_card_" + IntToString (nRnd), oStore);
      nCards++;
    }
    nCount++;
  }

  OpenStore(oStore, GetPCSpeaker());
}
