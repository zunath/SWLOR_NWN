/**********************************/
/*          d1_cards_jman
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Main Manage Deck Mechanics
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

object GetCardBag (object oPlayer, int nCheckIfFull = TRUE, int nCreateBag = FALSE)
{
    object oInv = GetFirstItemInInventory (oPlayer);
    int nDeckCount;
    while (oInv != OBJECT_INVALID)
    {
        string sTag = GetTag (oInv);
        if (GetStringLeft (sTag, 7) == "CardBag")
        {
          if(!nCheckIfFull)
            return oInv;

          // if bag is full, put inventory in player's main inventory.
          if(GetCardItemsInDeck(oInv, TRUE) > 35)
          {
            return oPlayer;
          }else{
            return oInv;
          }
        }

        oInv = GetNextItemInInventory (oPlayer);
    }
    // didn't find card bag
    if(nCreateBag)
    {
      oInv = CreateItemOnObject("cardbag", oPlayer);
      return oInv;
    }else{
      return OBJECT_INVALID;
    }
}

int GetCardItemsInDeck (object oDeck, int nTypes = FALSE)
{
  // create an invisible object to hold the items
  object oTmp = CreateObject(OBJECT_TYPE_PLACEABLE, "cardcounter", GetLocation(GetItemPossessor(oDeck)));
  object oTmpDeck = CopyObject(oDeck, GetLocation(oTmp), oTmp);
  object oInv = GetFirstItemInInventory (oTmp);
  int nDeckCount = 0;
  string sTag;
  while (oInv != OBJECT_INVALID)
  {
    sTag = GetTag(oInv);
    if (GetStringLeft (sTag, 13) != "CreaturesDeck")
    {
      if(nTypes)
      {
        nDeckCount++;
      }else{
        nDeckCount = nDeckCount + GetItemStackSize(oInv);
      }
    }
    oInv = GetNextItemInInventory (oTmp);
  }
  DestroyObject(oTmpDeck);
  DestroyObject(oTmp);
  return nDeckCount;

}

void ActionLayoutCards (location lStart, int nLayoutSource, int nRows, int nColumns, object oSource)
{
    if (nLayoutSource != CARD_SOURCE_DECK && nLayoutSource != CARD_SOURCE_COLLECTION)
        return;

    int nCard, nCards, nCountC;
    int nCountR = 1;

    vector vStart = GetPositionFromLocation (lStart);
    vector vCard;

    float fX = CARD_SIZE_X;
    float fY = CARD_SIZE_Y;
    float fFacing = GetFacingFromLocation (lStart);

    object oCard;
    object oArea = GetArea (OBJECT_SELF);
    oArea = (GetTag (oArea) == "ManageDeck") ? oArea :
            (GetTag (OBJECT_SELF) == "ManageDeck") ? OBJECT_SELF : OBJECT_INVALID;

    for (nCard = 1; nCard < CARD_MAX_ID; nCard++)
    {
        nCards = GetCardsInDeck (nCard, oSource);

        while (nCards-- > 0)
        {
            if (nCountR <= nRows)
            {
                if (++nCountC > nColumns)
                {
                    nCountR += 1;
                    nCountC = 1;

                    if (nCountR > nRows)
                        break;
                }

                vCard = Vector (vStart.x + (fX * IntToFloat (nCountC)), vStart.y - (fY * IntToFloat (nCountR)), vStart.z + 0.05);

                CreateObject (OBJECT_TYPE_PLACEABLE, GetCardTag (nCard), Location (oArea, vCard, fFacing));
            }
        }
    }
}

void ActionRemoveCards (object oDeck, int nCard, int nNth, int nAllCards = FALSE, object oPlayer = OBJECT_INVALID)
{
  if(GetIsDebug())
    SendMessageToPC(GetFirstPC(), "Total card count of current deck " +  IntToString(GetCardItemsInDeck(oDeck)));

  // create an invisible object to hold the items
  object oTmp = CreateObject(OBJECT_TYPE_PLACEABLE, "cardcounter", GetLocation(GetItemPossessor(oDeck)));
  object oTmpDeck = CopyObject(oDeck, GetLocation(oTmp), oTmp);
  if(oPlayer == OBJECT_INVALID)
    oPlayer = GetItemPossessor(oDeck);
  DestroyObject(oDeck);
  object oInv = GetFirstItemInInventory (oTmp);
  int nDeleted = 0;
  string sTag;
  while ((oInv != OBJECT_INVALID) && (nDeleted < nNth))
  {
    sTag = GetTag(oInv);
    if (GetStringLeft (sTag, 13) != "CreaturesDeck")
    {
      if (nAllCards)
      {
        DestroyObject(oInv);
      }else{
        if(sTag == "di_card_" + IntToString(nCard))
        {
          if(GetItemStackSize(oInv) > (nNth - nDeleted))
          {
            SetItemStackSize(oInv, GetItemStackSize(oInv) - nNth);
            if(GetIsDebug())
              SendMessageToPC(GetFirstPC(), "Setting item stack to " + GetName(oInv));
            nDeleted = nNth;
          }else{
            nDeleted = nDeleted + GetItemStackSize(oInv);
            DestroyObject(oInv);
            if(GetIsDebug())
              SendMessageToPC(GetFirstPC(), "Destroying " + GetName(oInv));

          }
        }
      }
    }
    oInv = GetNextItemInInventory (oTmp);
  }

  // make sure card hasn't been removed from the deck
  if(nDeleted < nNth)
  {
      oInv = GetFirstItemInInventory (oPlayer);
      while ((oInv != OBJECT_INVALID) && (nDeleted < nNth))
      {
        sTag = GetTag(oInv);
        if (GetStringLeft (sTag, 13) != "CreaturesDeck")
        {
          if (nAllCards)
          {
            DestroyObject(oInv);
          }else{
            if(sTag == "di_card_" + IntToString(nCard))
            {
              if(GetItemStackSize(oInv) > (nNth - nDeleted))
              {
                SetItemStackSize(oInv, GetItemStackSize(oInv) - nNth);
                if(GetIsDebug())
                  SendMessageToPC(GetFirstPC(), "Setting item stack to " + GetName(oInv));
                nDeleted = nNth;
              }else{
                nDeleted = nDeleted + GetItemStackSize(oInv);
                DestroyObject(oInv);
                if(GetIsDebug())
                  SendMessageToPC(GetFirstPC(), "Destroying " + GetName(oInv));
              }
            }
          }
        }
        oInv = GetNextItemInInventory (oPlayer);
      }
  }

  DelayCommand(0.5, ActionRemoveCardsCopy(oTmpDeck, oPlayer));
  DestroyObject(oTmpDeck, 0.9);
  DestroyObject(oTmp, 1.0);
}

// this function is required to work around a bug - otherwise the card is never removed
void ActionRemoveCardsCopy (object oDeck, object oPlayer)
{
  CopyObject(oDeck, GetLocation(oPlayer), oPlayer);
}

void ActionPurchaseCards (object oPlayer, int nNumber)
{
    AddToCardsSold (0, nNumber);

    int nLands = (nNumber - (nNumber % CARD_PURCHASE_LANDS)) / CARD_PURCHASE_LANDS;
    int nSource = CARD_SOURCE_COLLECTION;
    int nPrevious;

    struct sCard sRarity;

    if (nNumber >= CARD_PURCHASE_DECK)
    {
        oPlayer = CreateItemOnObject ("creaturesdeck", oPlayer);
        nSource = CARD_SOURCE_DECK;
    }

    ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nSource, OBJECT_INVALID, oPlayer, nLands);

    nNumber -= nLands;

    while (nNumber > 0)
    {
        int nPurchase = Random (CARD_MAX_ID) + 1;

        if (nPurchase == CARD_MAX_ID)
            nPurchase -= 1;

        sRarity = GetCardInfo (nPurchase);

        if (!GetRarityAllowed (sRarity.nRarity))
            continue;

        if (nPurchase != CARD_GENERATOR_GENERIC)
        {
            nPrevious = GetLocalInt (OBJECT_SELF, "BUILD_DECK" + IntToString (nPurchase));

            if (nPrevious >= 4)
                continue;
        }

        SetLocalInt (OBJECT_SELF, "BUILD_DECK" + IntToString (nPurchase), ++nPrevious);

        nNumber -= 1;

        ActionTransferCard (nPurchase, CARD_SOURCE_ALL_CARDS, nSource, OBJECT_INVALID, oPlayer, 1);
    }

    for (nNumber = 1; nNumber <= CARD_MAX_ID; nNumber++)
        DeleteLocalInt (OBJECT_SELF, "BUILD_DECK" + IntToString (nNumber));
}

void ActionTransferCard (int nCard, int nSource, int nDestination, object oSource, object oDestination, int nNth = 1)
{
    int nUpdate, nAbort;
    object oBag;

    switch (nDestination)
    {
        case CARD_SOURCE_ALL_CARDS:
            break;

        case CARD_SOURCE_GAME_PLAYER_1:
            nUpdate = GetLocalInt (oDestination, "CARDS_GAME_DECK_1_" + IntToString (nCard));

            SetLocalInt (oDestination, "CARDS_GAME_DECK_1_" + IntToString (nCard), nUpdate + nNth);

            break;

        case CARD_SOURCE_GAME_PLAYER_2:
            nUpdate = GetLocalInt (oDestination, "CARDS_GAME_DECK_2_" + IntToString (nCard));

            SetLocalInt (oDestination, "CARDS_GAME_DECK_2_" + IntToString (nCard), nUpdate + nNth);

            break;

        case CARD_SOURCE_ANTE_PLAYER_1:
            nUpdate = GetLocalInt (oDestination, "CARDS_ANTE_CARDS_1_" + IntToString (nCard));

            SetLocalInt (oDestination, "CARDS_ANTE_CARDS_1_" + IntToString (nCard), nUpdate + nNth);

            break;

        case CARD_SOURCE_ANTE_PLAYER_2:
            nUpdate = GetLocalInt (oDestination, "CARDS_ANTE_CARDS_2_" + IntToString (nCard));

            SetLocalInt (oDestination, "CARDS_ANTE_CARDS_2_" + IntToString (nCard), nUpdate + nNth);

            break;

        case CARD_SOURCE_COLLECTION:
            nUpdate = GetLocalInt (oDestination, "CARDS_OF_TYPE_" + IntToString (nCard));
            SetLocalInt (oDestination, "CARDS_OF_TYPE_" + IntToString (nCard), nUpdate + nNth);
            oBag = GetCardBag(oDestination, TRUE, TRUE);
            CreateItemOnObject("di_card_" + IntToString (nCard), oBag, nNth);

            break;

        case CARD_SOURCE_DECK:
            nUpdate = GetLocalInt (oDestination, "CARDS_OF_TYPE_" + IntToString (nCard));
            SetLocalInt (oDestination, "CARDS_OF_TYPE_" + IntToString (nCard), nUpdate + nNth);
            // create card objects
            CreateItemOnObject("di_card_" + IntToString (nCard), oDestination, nNth);
            break;

        default:
            nAbort = TRUE;

            break;
    }

    if (nAbort)
        return;

    switch (nSource)
    {
        case CARD_SOURCE_ALL_CARDS:
            break;

        case CARD_SOURCE_GAME_PLAYER_1:
            nUpdate = GetLocalInt (oSource, "CARDS_GAME_DECK_1_" + IntToString (nCard));

            SetLocalInt (oSource, "CARDS_GAME_DECK_1_" + IntToString (nCard), nUpdate - nNth);

            break;

        case CARD_SOURCE_GAME_PLAYER_2:
            nUpdate = GetLocalInt (oSource, "CARDS_GAME_DECK_2_" + IntToString (nCard));

            SetLocalInt (oSource, "CARDS_GAME_DECK_2_" + IntToString (nCard), nUpdate - nNth);

            break;

        case CARD_SOURCE_ANTE_PLAYER_1:
            nUpdate = GetLocalInt (oSource, "CARDS_ANTE_CARDS_1_" + IntToString (nCard));

            SetLocalInt (oSource, "CARDS_ANTE_CARDS_1_" + IntToString (nCard), nUpdate - nNth);

            break;

        case CARD_SOURCE_ANTE_PLAYER_2:
            nUpdate = GetLocalInt (oSource, "CARDS_ANTE_CARDS_2_" + IntToString (nCard));
            SetLocalInt (oSource, "CARDS_ANTE_CARDS_2_" + IntToString (nCard), nUpdate - nNth);

            break;

        case CARD_SOURCE_COLLECTION:
            nUpdate = GetLocalInt (oSource, "CARDS_OF_TYPE_" + IntToString (nCard));
            SetLocalInt (oSource, "CARDS_OF_TYPE_" + IntToString (nCard), nUpdate - nNth);

            break;

        case CARD_SOURCE_DECK:
            nUpdate = GetLocalInt (oSource, "CARDS_OF_TYPE_" + IntToString (nCard));
            SetLocalInt (oSource, "CARDS_OF_TYPE_" + IntToString (nCard), nUpdate - nNth);
            ActionRemoveCards (oSource, nCard, nNth);
            break;

        default:
            break;
    }
}

void SetDeckVariables (object oDeck)
{
  // clear existing variables
  int nCard;
  int nUpdate;
  while (++nCard < CARD_MAX_ID)
    SetLocalInt (oDeck, "CARDS_OF_TYPE_" + IntToString (nCard), 0);

  // create an invisible object to hold the items
  object oTmp = CreateObject(OBJECT_TYPE_PLACEABLE, "cardcounter", GetLocation(GetItemPossessor(oDeck)));
  object oTmpDeck = CopyObject(oDeck, GetLocation(oTmp), oTmp);
  object oInv = GetFirstItemInInventory (oTmp);
  int nDeckCount = 0;
  string sTag;
  while (oInv != OBJECT_INVALID)
  {
    sTag = GetTag(oInv);
    if (GetStringLeft (sTag, 13) != "CreaturesDeck")
    {
      nCard = StringToInt(GetStringRight(sTag, GetStringLength(sTag) - 8));
      nUpdate = GetLocalInt (oDeck, "CARDS_OF_TYPE_" + IntToString (nCard));
      nUpdate = nUpdate + GetItemStackSize(oInv);
      SetLocalInt (oDeck, "CARDS_OF_TYPE_" + IntToString (nCard), nUpdate);
    }
    oInv = GetNextItemInInventory (oTmp);
  }
  DestroyObject(oTmpDeck);
  DestroyObject(oTmp);
}
