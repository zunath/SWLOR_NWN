/**********************************/
/*          d1_cards_jget
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Get functions
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

int GetAIDifficulty (object oNPC)
{
    return GetLocalInt (oNPC, "CARD_AI_DIFFICULTY");
}

int GetCardDeckType (object oNPC)
{
    return GetLocalInt (oNPC, "CARD_DECK_TYPE");
}

int GetCardGamePlayerNumber (object oPlayer)
{
    return (GetCardGamePlayer (1, GetArea (oPlayer)) == oPlayer) ? 1 : 2;
}

int GetCardGameToggle (object oArea)
{
    return GetLocalInt (oArea, "CARDS_GAME_IN_PROGRESS");
}

int GetCardID (object oCard)
{
    string sTag = GetTag (oCard);

    int nLength = (GetStringLeft (sTag, 7) == "d_card_") ? 7 :
                  (GetStringLeft (sTag, 11) == "d1_card_cr_") ? 11 : 0;

    if (!nLength)
        return FALSE;

    return StringToInt (GetStringRight (sTag, GetStringLength (sTag) - nLength));
}

int GetCardMaximum (object oArea)
{
    int nRules = (oArea == MODULE) ? GetDefaultRules() : GetRules (oArea);

    return (nRules & CARD_RULE_RESTRICT_4) ? 4 :
           (nRules & CARD_RULE_RESTRICT_3) ? 3 :
           (nRules & CARD_RULE_RESTRICT_2) ? 2 :
           (nRules & CARD_RULE_RESTRICT_X) ? 100 : CARD_GAME_DEFAULT_MAX_CARDS_TYPE;
}

int GetCardOwner (object oTarget)
{
    return GetLocalInt (oTarget, "CARDS_CREATURE_DISCARD_OWNER");
}

int GetCardsForAnte (int nCard, int nPlayer, object oArea)
{
    return GetLocalInt (oArea, "CARDS_ANTE_CARDS_" + IntToString (nPlayer) + "_" + IntToString (nCard));
}

int GetCardsInDeck (int nCard, object oDeck)
{
    return GetLocalInt (oDeck, "CARDS_OF_TYPE_" + IntToString (nCard));
}

int GetCardsInHand (int nPlayer, object oCentre, int nNth = 1)
{
    int nCount = 1;

    object oCards = GetNearestObject (OBJECT_TYPE_PLACEABLE, oCentre, nCount);

    while (oCards != OBJECT_INVALID)
    {
        int nCard = GetCardID (oCards);

        if (nCard && GetOwner (oCards) == nPlayer && !GetLocalInt (oCards, "CARD_PLAYED_BLOCK"))
            if (--nNth <= 0)
                return nCard;

        oCards = GetNearestObject (OBJECT_TYPE_PLACEABLE, oCentre, ++nCount);
    }

    return FALSE;
}

int GetDeckMaximum (object oArea)
{
    int nRules = (oArea == MODULE) ? GetDefaultRules() : GetRules (oArea);

    return (nRules & CARD_RULE_DECK_20) ? 20 :
           (nRules & CARD_RULE_DECK_30) ? 30 :
           (nRules & CARD_RULE_DECK_40) ? 40 :
           (nRules & CARD_RULE_DECK_50) ? 50 :
           (nRules & CARD_RULE_DECK_60) ? 60 : CARD_GAME_DEFAULT_MAX_DECK_SIZE;
}

int GetDeckTagValidation (object oPlayer)
{
    object oInv = GetFirstItemInInventory (oPlayer);

    int nDeckCount;

    while (oInv != OBJECT_INVALID)
    {
        string sTag = GetTag (oInv);

        if (GetStringLeft (sTag, 13) == "CreaturesDeck")
        {
            object oItem = GetFirstItemInInventory (oPlayer);

            while (oItem != OBJECT_INVALID)
            {
                string sItemTag = GetTag (oItem);

                if ((oItem != oInv) && (GetStringLeft (sItemTag, 13) == "CreaturesDeck"))
                {
                    if (sItemTag == sTag)
                    {
                        string sNew = "CreaturesDeck" + IntToString (Random (30000));

                        CopyObject (oItem, GetLocation (oPlayer), oPlayer, sNew);
                        DestroyObject (oItem);

                        return FALSE;
                    }
                }

                oItem = GetNextItemInInventory (oPlayer);
            }
        }

        oInv = GetNextItemInInventory (oPlayer);
    }

    return TRUE;
}

int GetDefaultRules()
{
    int nDefault = GetLocalInt (MODULE, "CARDS_DEFAULT_RULES");

    if (!nDefault)
        nDefault = CARD_RULE_NORMAL;

    return nDefault;
}

int GetDiscardPile (int nPile, int nPlayer, object oArea)
{
    return GetLocalInt (oArea, "CARDS_DISCARDED_" + IntToString (nPlayer) + "_" + IntToString (nPile));
}

int GetDiscardPileSize (int nPlayer, object oArea)
{
    int nNth;

    while (GetDiscardPile (++nNth, nPlayer, oArea))
        continue;

    return nNth - 1;
}

int GetDrawMaximum (object oArea)
{
    int nRules = (oArea == MODULE) ? GetDefaultRules() : GetRules (oArea);

    return (nRules & CARD_RULE_DRAW_3) ? 3 :
           (nRules & CARD_RULE_DRAW_5) ? 5 :
           (nRules & CARD_RULE_DRAW_7) ? 7 :
           (nRules & CARD_RULE_DRAW_9) ? 9 : CARD_GAME_DEFAULT_MAX_HAND_SIZE;
}

int GetDrawnCard (int nPlayer, int nMaxSize, object oArea)
{
    if (nMaxSize < 1)
        return FALSE;

    int nSelect = Random (nMaxSize) + 1;
    int nCard, nCards;

    while (nSelect > 0)
        nSelect -= GetLocalInt (oArea, "CARDS_GAME_DECK_" + IntToString (nPlayer) + "_" + IntToString (++nCard));

    return nCard;
}

int GetDrawTerminate (object oArea = OBJECT_SELF)
{
    if (!GetIsVariantInPlay (CARD_RULE_LAST_DRAW_CONTINUE, oArea))
    {
        int nPlayer1 = GetTotalCards (CARD_SOURCE_GAME_PLAYER_1, oArea) <= 0;
        int nPlayer2 = GetTotalCards (CARD_SOURCE_GAME_PLAYER_2, oArea) <= 0;
        int nEnd = (nPlayer1 && nPlayer2) ? CARD_GAME_END_DRAW :
                   (nPlayer1 || nPlayer2) ? CARD_GAME_END_RESULT_DRAW : FALSE;

        if (nEnd)
        {
            int nPlayer = (nEnd == CARD_GAME_END_RESULT_DRAW) ? 1 :
                          (nPlayer1) ? 1 :
                          (nPlayer2) ? 2 : FALSE;

            ActionEndCardGame (nEnd, nPlayer, OBJECT_SELF);

            return TRUE;
        }
    }

    return FALSE;
}

int GetGameTurn (object oArea = OBJECT_SELF)
{
    return GetLocalInt (oArea, "GAME_TURN");
}

int GetGeneratorID (object oGenerator)
{
    return GetLocalInt (oGenerator, "CARDS_GENERATOR_ID");
}

int GetGeneratorMaximum (object oArea)
{
    int nRules = (oArea == MODULE) ? GetDefaultRules() : GetRules (oArea);

    return (nRules & CARD_RULE_LIMIT_1) ? 1 :
           (nRules & CARD_RULE_LIMIT_2) ? 2 :
           (nRules & CARD_RULE_LIMIT_3) ? 3 :
           (nRules & CARD_RULE_LIMIT_4) ? 4 :
           (nRules & CARD_RULE_LIMIT_5) ? 5 :
           (nRules & CARD_RULE_LIMIT_8) ? 8 :
           (nRules & CARD_RULE_LIMIT_10) ? 10 : CARD_GAME_DEFAULT_MAX_GENERATORS;
}

int GetHasAssociates (object oPlayer)
{
    int nAssociate = 1;

    while (nAssociate <= 5)
        if (GetAssociate (nAssociate++, oPlayer) != OBJECT_INVALID)
            return TRUE;

    return FALSE;
}

int GetHasCardEffect (int nCardID, object oTarget)
{
    return GetLocalInt (oTarget, "CARD_EFFECT_" + IntToString (nCardID));
}

int GetHasCreatures (int nScanType1, int nScanVar1, int nPlayer, object oArea, int nAmount = FALSE, int nScanType2 = FALSE, int nScanVar2 = FALSE, int nScanType3 = FALSE, int nScanVar3 = FALSE)
{
    int nFilterType2 = -1;
    int nFilter2 = -1;
    int nNth = 1;
    int nReturn;

    int nCardType = (nScanType1 == CARD_SCAN_CARD_SUBTYPE) ? nScanVar1 : (nScanType2 == CARD_SCAN_CARD_SUBTYPE) ? nScanVar2 : (nScanType3 == CARD_SCAN_CARD_SUBTYPE) ? nScanVar3 : FALSE;
    int nCountClones = (nScanType1 == CARD_SCAN_IS_CLONE) ? nScanVar1 : (nScanType2 == CARD_SCAN_IS_CLONE) ? nScanVar2 : (nScanType3 == CARD_SCAN_IS_CLONE) ? nScanVar3 : TRUE;
    int nCountMythical = (nScanType1 == CARD_SCAN_IS_MYTHICAL) ? nScanVar1 : (nScanType2 == CARD_SCAN_IS_MYTHICAL) ? nScanVar2 : (nScanType3 == CARD_SCAN_IS_MYTHICAL) ? nScanVar3 : TRUE;
    int nScanType = (nScanType1 == CARD_SCAN_CREATURE_SCAN) ? nScanVar1 : (nScanType2 == CARD_SCAN_CREATURE_SCAN) ? nScanVar2 : (nScanType3 == CARD_SCAN_CREATURE_SCAN) ? nScanVar3 : FALSE;

    int nEffectEva1 = (nScanType1 == CARD_SCAN_NO_EFFECT) ? FALSE : (nScanType1 == CARD_SCAN_HAS_EFFECT) ? TRUE : -1;
    int nEffectEva2 = (nScanType2 == CARD_SCAN_NO_EFFECT) ? FALSE : (nScanType2 == CARD_SCAN_HAS_EFFECT) ? TRUE : -1;
    int nEffectEva3 = (nScanType3 == CARD_SCAN_NO_EFFECT) ? FALSE : (nScanType3 == CARD_SCAN_HAS_EFFECT) ? TRUE : -1;
    int nEffectType1 = (nEffectEva1 != -1) ? nScanVar1 : FALSE;
    int nEffectType2 = (nEffectEva2 != -1) ? nScanVar2 : FALSE;
    int nEffectType3 = (nEffectEva3 != -1) ? nScanVar3 : FALSE;

    int nAlive = (nScanType == CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD
                  || nScanType == CARD_CREATURE_SCAN_HIGHEST_DEFEND_DEAD
                  || nScanType == CARD_CREATURE_SCAN_HIGHEST_LIFE_DEAD
                  || nScanType == CARD_CREATURE_SCAN_LOWEST_ATTACK_DEAD
                  || nScanType == CARD_CREATURE_SCAN_LOWEST_DEFEND_DEAD
                  || nScanType == CARD_CREATURE_SCAN_LOWEST_LIFE_DEAD) ? FALSE : TRUE;

    if (nScanType == CARD_CREATURE_SCAN_HIGHEST_ATTACK_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_HIGHEST_DEFEND_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_HIGHEST_LIFE_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_LOWEST_ATTACK_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_LOWEST_DEFEND_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_LOWEST_LIFE_UNDEAD)
        {
            nFilterType2 = CREATURE_TYPE_RACIAL_TYPE;
            nFilter2 = RACIAL_TYPE_UNDEAD;
        }

    string sTag = (nScanType1 == CARD_SCAN_CARD_ID) ? GetCardTag (nScanVar1, TRUE) : (nScanType2 == CARD_SCAN_CARD_ID) ? GetCardTag (nScanVar2, TRUE) : (nScanType3 == CARD_SCAN_CARD_ID) ? GetCardTag (nScanVar3, TRUE) : "";

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oScan = (sTag == "") ? GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, nAlive, nFilterType2, nFilter2) :
                                  GetNearestObjectByTag (sTag, oCentre, nNth);

    while (oScan != OBJECT_INVALID && (!nAmount || nAmount > nReturn))
    {
        if (GetOwner (oScan) == nPlayer)
        {
            int nCard = GetCardID (oScan);

            if (nCard && !GetIsMine (oScan)
                && (sTag == "" || (sTag != "" && ((nAlive && !GetIsDead (oScan)) || (!nAlive && GetIsDead (oScan)))))
                && (nCountClones || (!nCountClones && !GetIsClone (oScan)))
                && (!nEffectType1 || (nEffectType1 && (GetHasCardEffect (nEffectType1, oScan) >= 1) == nEffectEva1))
                && (!nEffectType2 || (nEffectType2 && (GetHasCardEffect (nEffectType2, oScan) >= 1) == nEffectEva2))
                && (!nEffectType3 || (nEffectType3 && (GetHasCardEffect (nEffectType3, oScan) >= 1) == nEffectEva3)))
                {
                    struct sCard sInfo = GetCardInfo (nCard);

                    if ((!nCardType || (nCardType && sInfo.nSubType == nCardType))
                        && (nCountMythical || (!nCountMythical && sInfo.nType != CARD_TYPE_MYTHICAL)))
                        {
                            if (nScanType)
                            {
                                switch (nScanType)
                                {
                                    case CARD_CREATURE_SCAN_HIGHEST_ATTACK_LIVING:
                                    case CARD_CREATURE_SCAN_HIGHEST_DEFEND_LIVING:
                                    case CARD_CREATURE_SCAN_LOWEST_ATTACK_LIVING:
                                    case CARD_CREATURE_SCAN_LOWEST_DEFEND_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            nReturn += 1;

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE_LIVING:
                                    case CARD_CREATURE_SCAN_LOWEST_LIFE_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD && GetCurrentHitPoints (oScan) != GetMaxHitPoints (oScan))
                                            nReturn += 1;

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE:
                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE_DEAD:
                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE_UNDEAD:
                                    case CARD_CREATURE_SCAN_LOWEST_LIFE:
                                    case CARD_CREATURE_SCAN_LOWEST_LIFE_DEAD:
                                    case CARD_CREATURE_SCAN_LOWEST_LIFE_UNDEAD:
                                        if (GetCurrentHitPoints (oScan) != GetMaxHitPoints (oScan))
                                            nReturn += 1;

                                        break;

                                    default:
                                        nReturn += 1;

                                        break;
                                }
                            }
                            else
                                nReturn += 1;
                        }
                }
        }

        oScan = (sTag == "") ? GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, nAlive, nFilterType2, nFilter2) :
                               GetNearestObjectByTag (sTag, oCentre, ++nNth);
    }

    return (!nAmount) ? nReturn : (nAmount > nReturn) ? FALSE : TRUE;
}

int GetHasGenerators (int nPlayer, object oArea, int nAmount = FALSE)
{
    int nNth = 1;
    int nCount;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

    while (oGenerator != OBJECT_INVALID && (!nAmount || nAmount > nCount))
    {
        if (GetOwner (oGenerator) == nPlayer)
            nCount += 1;

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
    }

    return (!nAmount) ? nCount : (nAmount > nCount) ? FALSE : TRUE;
}

int GetHasPlayedGenerator (int nPlayer, object oArea)
{
    return GetLocalInt (oArea, "CARD_GENERATOR_PLACED_" + IntToString (nPlayer));
}

int GetHasStones (int nStoneID, int nPlayer, object oArea, int nAmount = FALSE)
{
    int nNth = 1;
    int nCount;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oStone = GetNearestObjectByTag ("d_stone", oCentre, nNth);

    while (oStone != OBJECT_INVALID && (!nAmount || (nAmount && nAmount > nCount)))
    {
        if (GetOwner (oStone) == nPlayer && GetStoneID (oStone) == nStoneID)
            nCount += 1;

        oStone = GetNearestObjectByTag ("d_stone", oCentre, ++nNth);
    }

    return (!nAmount) ? nCount : (nAmount > nCount) ? FALSE : TRUE;
}

int GetIsAvatar (object oTarget = OBJECT_SELF)
{
    return GetStringLeft (GetTag (oTarget), 12) == "GAME_AVATAR_";
}

int GetIsClone (object oTarget)
{
    return GetHasCardEffect (CARD_SPELL_SIMULACRUM, oTarget);
}

int GetIsDeckValid (object oDeck, object oArea)
{
    DeleteLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE");

    object oPlayer = GetItemPossessor (oDeck);

    int nCard, nValue, nNumber;
    int nMax = GetCardMaximum (oArea);

    struct sCard sInfo;

    while (++nCard < CARD_MAX_ID)
    {
        nNumber = GetCardsInDeck (nCard, oDeck);

        if (nNumber > nMax && nCard != CARD_GENERATOR_GENERIC)
        {
            sInfo = GetCardInfo (nCard);

            SetLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE", "You have " + IntToString (nNumber) + " " + sInfo.sName + " cards in your deck.  This is above the game's limit of " + IntToString (nMax) + " cards.");

            return FALSE;
        }

        nValue += nNumber;
    }

    nMax = GetDeckMaximum (oArea);

    if (nValue > nMax)
    {
        SetLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE", "You have " + IntToString (nValue) + " cards in your deck.  This is above the game's maximum of " + IntToString (nMax));

        return FALSE;
    }

    int nMin = (nMax - (nMax % 2)) / 2;

    if (nValue < nMin)
    {
        SetLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE", "You have " + IntToString (nValue) + " cards in your deck.  This is less than the game's minimum of " + IntToString (nMin) + " cards.");

        return FALSE;
    }

    DeleteLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE");

    return TRUE;
}

int GetIsDebug ()
{
  if(GetLocalInt(GetModule(), "CARD_DEBUG") > 0)
    return TRUE;
  return FALSE;
}

int GetIsGeneratorUsed (object oGenerator)
{
    return GetLocalInt (oGenerator, "CARDS_GENERATOR_ON");
}

int GetIsMine (object oTarget)
{
    return GetHasCardEffect (CARD_SUMMON_KOBOLD_ENGINEER, oTarget);
}

int GetIsPowerAvailable (int nPlayer, object oArea, int nAmount = FALSE)
{
    int nNth = 1;
    int nPower = GetMagicPool (nPlayer, oArea);

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

    while (oGenerator != OBJECT_INVALID && (!nAmount || nAmount > nPower))
    {
        if (GetOwner (oGenerator) == nPlayer && !GetIsGeneratorUsed (oGenerator))
            nPower += 1;

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
    }

    return (!nAmount) ? nPower : (nAmount > nPower) ? FALSE : TRUE;
}

int GetIsVariantInPlay (int nRule, object oArea)
{
    return GetRules (oArea) & nRule;
}

int GetLoremasterScan (int nPlayer, object oCentre)
{
    int nNth = 1;
    int nCount;

    string sTag = GetCardTag (CARD_SUMMON_LOREMASTER, TRUE);

    object oCycle = GetNearestObjectByTag (sTag, oCentre, nNth);

    while (oCycle != OBJECT_INVALID)
    {                           //--2004-11/23 bloodsong: check if the loremaster belongs to the player
        if (!GetIsDead (oCycle) && GetOwner(oCycle) == nPlayer)
            nCount += 1;

        oCycle = GetNearestObjectByTag (sTag, oCentre, ++nNth);
    }

    return nCount;
}

int GetMagicPool (int nPlayer, object oArea)
{
    return GetLocalInt (oArea, "CARD_MANA_POOL_" + IntToString (nPlayer));
}

int GetNPCAnteBet (object oNPC)
{
    return GetLocalInt (oNPC, "CARD_ANTE_BET");
}

int GetNPCDeckType (object oNPC = OBJECT_SELF)
{
    return GetLocalInt (oNPC, "CARD_DECK_TYPE");
}

int GetNPCGoldBet (object oNPC)
{
    return GetLocalInt (oNPC, "CARD_GOLD_BET");
}

int GetOriginalOwner (object oObject)
{
    return GetLocalInt (oObject, "CARD_ORIGINALLY_OWNED_BY");
}

int GetOwner (object oObject)
{
    return GetLocalInt (oObject, "CARD_OWNED_BY_PLAYER");
}

int GetPercentHitPoints (int nPercent, object oTarget = OBJECT_SELF)
{
    return FloatToInt (IntToFloat (GetMaxHitPoints (oTarget)) * (IntToFloat (nPercent) / 100.0f));
}

int GetPlayerRating (object oPlayer)
{
  object oBag = GetCardBag(oPlayer, FALSE);
  if(oBag == OBJECT_INVALID)
    return 0;
  string sTag = GetTag(oBag);
  int nRating = StringToInt(GetStringRight(sTag, GetStringLength(sTag) - 7));
  return nRating;
}

int GetPlayerResults (int nResultType, int nDifficulty, object oPlayer)
{
    return GetLocalInt (oPlayer, "CARDS_GAME_RESULTS" + IntToString (nDifficulty) + "_" + IntToString (nResultType));
}

int GetRarityAllowed (int nRarity)
{
    if (nRarity == CARD_RARITY_COMMON || GetIsDebug())
        return TRUE;

    int nTotal = GetTotalCardsSold (nRarity);
    int nOneIn, nOutOf;

    switch (nRarity)
    {
        case CARD_RARITY_UNCOMMON:
            nOutOf = CARD_PURCHASE_RARITY_UNCOMMONS;
            nOneIn = (nTotal - (nTotal % CARD_PURCHASE_UNCOMMONS)) / CARD_PURCHASE_UNCOMMONS;

            break;

        case CARD_RARITY_RARE:
            nOutOf = CARD_PURCHASE_RARITY_RARES;
            nOneIn = (nTotal - (nTotal % CARD_PURCHASE_RARES)) / CARD_PURCHASE_RARES;

            break;

        case CARD_RARITY_VERY_RARE:
            nOutOf = CARD_PURCHASE_RARITY_VERY_RARES;
            nOneIn = (nTotal - (nTotal % CARD_PURCHASE_VERY_RARES)) / CARD_PURCHASE_VERY_RARES;

            break;

        case CARD_RARITY_ULTRA_RARE:
            nOutOf = CARD_PURCHASE_RARITY_ULTRA_RARES;
            nOneIn = (nTotal - (nTotal % CARD_PURCHASE_ULTRA_RARES)) / CARD_PURCHASE_ULTRA_RARES;

            break;
    }


    if (nOneIn >= Random (nOutOf) + 1)
    {
        AddToCardsSold (nRarity, -1);
        return TRUE;
    }

    return FALSE;
}

int GetRules (object oArea)
{
    return GetLocalInt (oArea, "CARDS_VARIANTS_IN_PLAY");
}

int GetSpawnBoost (object oTarget = OBJECT_SELF)
{
    return GetLocalInt (oTarget, "CARDS_CREATURE_POWER_BOOST");
}

int GetStoneID (object oStone)
{
    return GetLocalInt (oStone, "CARDS_STONE_ID");
}

int GetTotalCards (int nSource, object oSource)
{
    int nTotal, nNth;

    switch (nSource)
    {
        case CARD_SOURCE_COLLECTION:
            for (nNth = 1; nNth < CARD_MAX_ID; nNth++)
                nTotal += GetCardsInDeck (nNth, oSource);

            break;

        case CARD_SOURCE_DECK:
            for (nNth = 1; nNth < CARD_MAX_ID; nNth++)
                nTotal += GetCardsInDeck (nNth, oSource);

            break;

        case CARD_SOURCE_GAME_PLAYER_1:
            for (nNth = 1; nNth < CARD_MAX_ID; nNth++)
                nTotal += GetLocalInt (oSource, "CARDS_GAME_DECK_1_" + IntToString (nNth));

            break;

        case CARD_SOURCE_GAME_PLAYER_2:
            for (nNth = 1; nNth < CARD_MAX_ID; nNth++)
                nTotal += GetLocalInt (oSource, "CARDS_GAME_DECK_2_" + IntToString (nNth));

            break;

        default:
            break;
    }

    return nTotal;
}

int GetTotalCardsSold (int nRarity)
{
    return GetLocalInt (MODULE, "CARDS_TOTAL_SOLD_" + IntToString (nRarity));
}

int GetTotalGlobalSpells (object oArea)
{
    return GetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, oArea) +
           GetHasCardEffect (CARD_SPELL_FLUX, oArea) +
           GetHasCardEffect (CARD_SPELL_HIGHER_CALLING, oArea) +
           GetHasCardEffect (CARD_SPELL_POWER_STREAM, oArea) +
           GetHasCardEffect (CARD_SPELL_VORTEX, oArea) + GetHasCustomGlobalEffect (oArea);
}

string GetCardTag (int nCard, int nCreature = FALSE, int nStone = FALSE, int nGenerator = FALSE)
{
    string sTag = (nCreature) ? "d1_card_cr_" : (nStone) ? "d_stone_" : (nGenerator) ? "d_generator_" : "d_card_";

    return sTag + IntToString (nCard);
}

object GetAvatar (int nPlayer, object oReference)
{
    return GetNearestObjectByTag ("GAME_AVATAR_" + IntToString (nPlayer), oReference);
}

object GetCardGameCreature (int nScanType1, int nScanVar1, int nPlayer, object oArea, int nScanType2 = FALSE, int nScanVar2 = FALSE, int nScanType3 = FALSE, int nScanVar3 = FALSE)
{
    int nFilterType2 = -1;
    int nFilter2 = -1;
    int nNth = 1;
    int nReturn, nLife;

    int nCardType = (nScanType1 == CARD_SCAN_CARD_SUBTYPE) ? nScanVar1 : (nScanType2 == CARD_SCAN_CARD_SUBTYPE) ? nScanVar2 : (nScanType3 == CARD_SCAN_CARD_SUBTYPE) ? nScanVar3 : FALSE;
    int nCountClones = (nScanType1 == CARD_SCAN_IS_CLONE) ? nScanVar1 : (nScanType2 == CARD_SCAN_IS_CLONE) ? nScanVar2 : (nScanType3 == CARD_SCAN_IS_CLONE) ? nScanVar3 : TRUE;
    int nCountMythical = (nScanType1 == CARD_SCAN_IS_MYTHICAL) ? nScanVar1 : (nScanType2 == CARD_SCAN_IS_MYTHICAL) ? nScanVar2 : (nScanType3 == CARD_SCAN_IS_MYTHICAL) ? nScanVar3 : TRUE;
    int nScanType = (nScanType1 == CARD_SCAN_CREATURE_SCAN) ? nScanVar1 : (nScanType2 == CARD_SCAN_CREATURE_SCAN) ? nScanVar2 : (nScanType3 == CARD_SCAN_CREATURE_SCAN) ? nScanVar3 : FALSE;

    int nEffectEva1 = (nScanType1 == CARD_SCAN_NO_EFFECT) ? FALSE : (nScanType1 == CARD_SCAN_HAS_EFFECT) ? TRUE : -1;
    int nEffectEva2 = (nScanType2 == CARD_SCAN_NO_EFFECT) ? FALSE : (nScanType2 == CARD_SCAN_HAS_EFFECT) ? TRUE : -1;
    int nEffectEva3 = (nScanType3 == CARD_SCAN_NO_EFFECT) ? FALSE : (nScanType3 == CARD_SCAN_HAS_EFFECT) ? TRUE : -1;
    int nEffectType1 = (nEffectEva1 != -1) ? nScanVar1 : FALSE;
    int nEffectType2 = (nEffectEva2 != -1) ? nScanVar2 : FALSE;
    int nEffectType3 = (nEffectEva3 != -1) ? nScanVar3 : FALSE;

    int nAlive = (nScanType == CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD
                  || nScanType == CARD_CREATURE_SCAN_HIGHEST_DEFEND_DEAD
                  || nScanType == CARD_CREATURE_SCAN_HIGHEST_LIFE_DEAD
                  || nScanType == CARD_CREATURE_SCAN_LOWEST_ATTACK_DEAD
                  || nScanType == CARD_CREATURE_SCAN_LOWEST_DEFEND_DEAD
                  || nScanType == CARD_CREATURE_SCAN_LOWEST_LIFE_DEAD) ? FALSE : TRUE;

    if (nScanType == CARD_CREATURE_SCAN_HIGHEST_ATTACK_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_HIGHEST_DEFEND_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_HIGHEST_LIFE_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_LOWEST_ATTACK_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_LOWEST_DEFEND_UNDEAD
        || nScanType == CARD_CREATURE_SCAN_LOWEST_LIFE_UNDEAD)
        {
            nFilterType2 = CREATURE_TYPE_RACIAL_TYPE;
            nFilter2 = RACIAL_TYPE_UNDEAD;
        }

    string sTag = (nScanType1 == CARD_SCAN_CARD_ID) ? GetCardTag (nScanVar1, TRUE) : (nScanType2 == CARD_SCAN_CARD_ID) ? GetCardTag (nScanVar2, TRUE) : (nScanType3 == CARD_SCAN_CARD_ID) ? GetCardTag (nScanVar3, TRUE) : "";

    object oReturn;
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oScan = (sTag == "") ? GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, nAlive, nFilterType2, nFilter2) :
                                  GetNearestObjectByTag (sTag, oCentre, nNth);

    while (oScan != OBJECT_INVALID)
    {
        if (GetOwner (oScan) == nPlayer)
        {
            int nCard = GetCardID (oScan);

            if (nCard && !GetIsMine (oScan)
                && (sTag == "" || (sTag != "" && ((nAlive && !GetIsDead (oScan)) || (!nAlive && GetIsDead (oScan)))))
                && (nCountClones || (!nCountClones && !GetIsClone (oScan)))
                && (!nEffectType1 || (nEffectType1 && (GetHasCardEffect (nEffectType1, oScan) >= 1) == nEffectEva1))
                && (!nEffectType2 || (nEffectType2 && (GetHasCardEffect (nEffectType2, oScan) >= 1) == nEffectEva2))
                && (!nEffectType3 || (nEffectType3 && (GetHasCardEffect (nEffectType3, oScan) >= 1) == nEffectEva3)))
                {
                    struct sCard sInfo = GetCardInfo (nCard);

                    if ((!nCardType || (nCardType && sInfo.nSubType == nCardType))
                        && (nCountMythical || (!nCountMythical && sInfo.nType != CARD_TYPE_MYTHICAL)))
                        {
                            if (nScanType)
                            {
                                switch (nScanType)
                                {
                                    case CARD_CREATURE_SCAN_HIGHEST_ATTACK:
                                    case CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD:
                                    case CARD_CREATURE_SCAN_HIGHEST_ATTACK_UNDEAD:
                                        if (sInfo.nAttack >= nReturn)
                                        {
                                            nReturn = sInfo.nAttack;
                                            oReturn = oScan;
                                        }

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_ATTACK_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            if (sInfo.nAttack >= nReturn)
                                            {
                                                nReturn = sInfo.nAttack;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_DEFEND:
                                    case CARD_CREATURE_SCAN_HIGHEST_DEFEND_DEAD:
                                    case CARD_CREATURE_SCAN_HIGHEST_DEFEND_UNDEAD:
                                        if (sInfo.nDefend >= nReturn)
                                        {
                                            nReturn = sInfo.nDefend;
                                            oReturn = oScan;
                                        }

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_DEFEND_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            if (sInfo.nDefend >= nReturn)
                                            {
                                                nReturn = sInfo.nDefend;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE_DEAD:
                                        if (sInfo.nAttack >= nLife)
                                            if (GetMaxHitPoints (oScan) > nReturn)
                                            {
                                                nReturn = GetMaxHitPoints (oScan);
                                                nLife = sInfo.nAttack;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE:
                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE_UNDEAD:
                                        if (sInfo.nAttack >= nLife)
                                            if ((GetCurrentHitPoints (oScan) > nReturn) && GetCurrentHitPoints (oScan) != GetMaxHitPoints (oScan))
                                            {
                                                nReturn = GetCurrentHitPoints (oScan);
                                                nLife = sInfo.nAttack;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_HIGHEST_LIFE_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            if (sInfo.nAttack >= nLife)
                                                if ((GetCurrentHitPoints (oScan) > nReturn) && GetCurrentHitPoints (oScan) != GetMaxHitPoints (oScan))
                                                {
                                                    nReturn = GetCurrentHitPoints (oScan);
                                                    nLife = sInfo.nAttack;
                                                    oReturn = oScan;
                                                }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_ATTACK:
                                    case CARD_CREATURE_SCAN_LOWEST_ATTACK_DEAD:
                                    case CARD_CREATURE_SCAN_LOWEST_ATTACK_UNDEAD:
                                        if (sInfo.nAttack <= nReturn || !nReturn)
                                        {
                                            nReturn = sInfo.nAttack;
                                            oReturn = oScan;
                                        }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_ATTACK_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            if (sInfo.nAttack <= nReturn || !nReturn)
                                            {
                                                nReturn = sInfo.nAttack;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_DEFEND:
                                    case CARD_CREATURE_SCAN_LOWEST_DEFEND_DEAD:
                                    case CARD_CREATURE_SCAN_LOWEST_DEFEND_UNDEAD:
                                        if (sInfo.nDefend <= nReturn || !nReturn)
                                        {
                                            nReturn = sInfo.nDefend;
                                            oReturn = oScan;
                                        }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_DEFEND_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            if (sInfo.nDefend <= nReturn || !nReturn)
                                            {
                                                nReturn = sInfo.nDefend;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_LIFE_DEAD:
                                        if (sInfo.nAttack <= nLife || !nLife)
                                            if (GetMaxHitPoints (oScan) < nReturn || !nReturn)
                                            {
                                                nReturn = GetMaxHitPoints (oScan);
                                                nLife = sInfo.nAttack;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_LIFE:
                                    case CARD_CREATURE_SCAN_LOWEST_LIFE_UNDEAD:
                                        if (sInfo.nAttack <= nLife || !nLife)
                                            if ((GetCurrentHitPoints (oScan) < nReturn || !nReturn) && GetCurrentHitPoints (oScan) != GetMaxHitPoints (oScan))
                                            {
                                                nReturn = GetCurrentHitPoints (oScan);
                                                nLife = sInfo.nAttack;
                                                oReturn = oScan;
                                            }

                                        break;

                                    case CARD_CREATURE_SCAN_LOWEST_LIFE_LIVING:
                                        if (GetRacialType (oScan) != RACIAL_TYPE_UNDEAD)
                                            if (sInfo.nAttack <= nLife || !nLife)
                                                if ((GetCurrentHitPoints (oScan) < nReturn || !nReturn) && GetCurrentHitPoints (oScan) != GetMaxHitPoints (oScan))
                                                {
                                                    nReturn = GetCurrentHitPoints (oScan);
                                                    nLife = sInfo.nAttack;
                                                    oReturn = oScan;
                                                }

                                        break;
                                }
                            }
                            else
                                oReturn = oScan;
                        }
                }
        }

        oScan = (sTag == "") ? GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, nAlive, nFilterType2, nFilter2) :
                               GetNearestObjectByTag (sTag, oCentre, ++nNth);
    }

    return oReturn;
}

object GetCardGamePlayer (int nPlayer, object oArea)
{
    return GetLocalObject (oArea, "CARDS_PLAYER_" + IntToString (nPlayer));
}

object GetCardsArea ()
{
    int nNth;

    string sTag = "CardGame";

    object oArea = GetObjectByTag (sTag, nNth);

    while (oArea != OBJECT_INVALID)
    {
        if (!GetCardGameToggle (oArea))
            break;

        oArea = GetObjectByTag (sTag, ++nNth);
    }

    return oArea;
}

object GetDeck (object oPlayer, int nNth = 1, object oArea = OBJECT_INVALID)
{
    if (!GetIsPC (oPlayer))
      return OBJECT_INVALID;

    return GetLocalObject(oPlayer, "DECK_SELECTED");
}

object GetDeckForAnte (int nPlayer, object oArea)
{
    return GetLocalObject (oArea, "CARD_ANTE_DECK_" + IntToString (nPlayer));
}

object GetGameCentre (object oArea = OBJECT_SELF)
{
    int nNth;

    object oScan = GetObjectByTag ("CARD_GAME_CENTRAL_ARENA", nNth);

    while (oScan != OBJECT_INVALID)
    {
        if (GetArea (oScan) == oArea)
            break;

        oScan = GetObjectByTag ("CARD_GAME_CENTRAL_ARENA", ++nNth);
    }

    return oScan;
}

object GetReferenceObject (int nPlayer, object oCentre)
{
    return GetNearestObjectByTag ("CONCEDE_" + IntToString (nPlayer), oCentre);
}


location GetReturnLocation (object oPlayer)
{
    return GetLocalLocation (oPlayer, "CARDS_PLAYER_LOCATION");
}

