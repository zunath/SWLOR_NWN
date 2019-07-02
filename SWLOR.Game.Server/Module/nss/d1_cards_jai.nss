/**********************************/
/*          d1_cards_jai
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  AI Function
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

void ActionAI()
{
    object oCentre = GetGameCentre();

    int nPlayer, nNth;

    struct sCard sInfo;

    for (nPlayer = 1; nPlayer <= 2; nPlayer++)
    {
        object oPlayer = GetCardGamePlayer (nPlayer, OBJECT_SELF);

        if (GetIsPC (oPlayer))
            continue;

        object oRef = GetReferenceObject (nPlayer, oCentre);
        object oAvatar = GetAvatar (nPlayer, oCentre);

        int nSize = 1;
        int nHand = GetCardsInHand (nPlayer, oCentre, nSize);

        while (nHand)
        {
            SetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nSize), nHand);

            nHand = GetCardsInHand (nPlayer, oCentre, ++nSize);
        }

        if (nSize > 1)
        {
            int nMax = nSize - 1;
            int nAI = GetAIDifficulty (oPlayer);
            int nPowerMax = GetIsPowerAvailable (nPlayer, OBJECT_SELF);
            int nPlayGenerator = !GetHasPlayedGenerator (nPlayer, OBJECT_SELF);

            if (nPlayGenerator)
            {
                nNth = 1;
                int nGenerator = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nNth));

                while (nNth <= nMax)
                {
                    if (nGenerator == CARD_GENERATOR_GENERIC)
                        break;

                    nGenerator = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nNth));
                }

                if (nGenerator == CARD_GENERATOR_GENERIC)
                {
                    int nCycle = 1;

                    string sTag = GetCardTag (CARD_GENERATOR_GENERIC);

                    object oCard = GetNearestObjectByTag (sTag, oCentre, nCycle);

                    while (oCard != OBJECT_INVALID)
                    {
                        if (GetOwner (oCard) == nPlayer)
                            break;

                        oCard = GetNearestObjectByTag (sTag, oCentre, ++nCycle);
                    }

                    if (oCard != OBJECT_INVALID)
                    {
                        DelayCommand (CYCLE_TIME * CARD_AI_PLAY_GENERATOR, ActionPlayCard (oCard));

                        if (nAI == CARD_AI_DIFFICULTY_TRAINING)
                        {
                            for (nNth = 1; nNth <= nMax; nNth++)
                                DeleteLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nNth));

                            return;
                        }

                        nPowerMax += 1;
                    }
                }
            }

            struct sAI sCard, sCard1, sCard2, sCard3;

            int nPower = nPowerMax;
            int nRedundant;

            for (nNth = 1; nNth <= nMax; nNth++)
            {
                sInfo = GetCardInfo (GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nNth)));

                if (sInfo.nType != CARD_TYPE_GENERATOR && sInfo.nMagic + sInfo.nBoost <= nPowerMax)
                {
                    sCard.nPlay = sInfo.nCard;
                    sCard.nCost = sInfo.nMagic + sInfo.nBoost;
                    sCard.nRating = GetAICardEvaluation (sInfo, nMax, nPowerMax, nPlayer, oAvatar, oCentre);
                    if (sCard.nRating > 0 && nPower - sCard.nCost >= 0)
                    {
                        int nRedundancy;

                        if (sCard.nRating > 100)
                        {
                            struct sAI sDelete;

                            sCard1 = sCard;
                            sCard2 = sDelete;
                            sCard3 = sDelete;

                            break;
                        }
                        else
                        {
                            while (sCard.nRating > 0
                                   && ((!sCard1.nRating || (sCard1.nRating && !(nAI >= CARD_AI_DIFFICULTY_NORMAL && sCard2.nRating < sCard1.nRating) && !(nAI >= CARD_AI_DIFFICULTY_HARD && sCard3.nRating < sCard1.nRating) && sCard.nRating > sCard1.nRating))
                                       || (!sCard2.nRating || (sCard2.nRating && !(nAI >= CARD_AI_DIFFICULTY_NORMAL && sCard1.nRating < sCard2.nRating) && !(nAI >= CARD_AI_DIFFICULTY_HARD && sCard3.nRating < sCard2.nRating) && sCard.nRating > sCard2.nRating))
                                       || (!sCard3.nRating || (sCard3.nRating && !(nAI >= CARD_AI_DIFFICULTY_NORMAL && sCard1.nRating < sCard3.nRating) && !(nAI >= CARD_AI_DIFFICULTY_HARD && sCard2.nRating < sCard3.nRating) && sCard.nRating > sCard3.nRating))))
                                   {
                                       struct sAI sSwap;

                                       if (!sCard1.nRating || (sCard1.nRating && !(nAI >= CARD_AI_DIFFICULTY_NORMAL && sCard2.nRating < sCard1.nRating) && !(nAI >= CARD_AI_DIFFICULTY_HARD && sCard3.nRating < sCard1.nRating) && sCard.nRating > sCard1.nRating))
                                       {
                                           sSwap = sCard1;
                                           sCard1 = sCard;
                                           sCard = sSwap;
                                       }
                                       else if (!sCard2.nRating || (sCard2.nRating && !(nAI >= CARD_AI_DIFFICULTY_NORMAL && sCard1.nRating < sCard2.nRating) && !(nAI >= CARD_AI_DIFFICULTY_HARD && sCard3.nRating < sCard2.nRating) && sCard.nRating > sCard2.nRating))
                                       {
                                           sSwap = sCard2;
                                           sCard2 = sCard;
                                           sCard = sSwap;
                                       }
                                       else if (!sCard3.nRating || (sCard3.nRating && !(nAI >= CARD_AI_DIFFICULTY_NORMAL && sCard1.nRating < sCard3.nRating) && !(nAI >= CARD_AI_DIFFICULTY_HARD && sCard2.nRating < sCard3.nRating) && sCard.nRating > sCard3.nRating))
                                       {
                                           sSwap = sCard3;
                                           sCard3 = sCard;
                                           sCard = sSwap;
                                       }
                                   }

                            if (sCard.nPlay)
                            {
                                SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_PLAY_" + IntToString (++nRedundant), sCard.nPlay);
                                SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_COST_" + IntToString (nRedundant), sCard.nCost);
                                SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nRedundant), sCard.nRating);
                            }
                        }

                        nPower = nPowerMax - sCard1.nCost - sCard2.nCost - sCard3.nCost;

                        while (++nRedundancy <= 3
                               && (nPower < 0 || (!nPower && ((nAI >= CARD_AI_DIFFICULTY_NORMAL && !sCard2.nPlay)
                                                              || (nAI >= CARD_AI_DIFFICULTY_HARD && !sCard3.nPlay)))))
                            {
                                int nDouble;

                                struct sAI sDouble;

                                while (++nDouble <= nRedundant)
                                {
                                    if (GetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nDouble)) > sDouble.nRating)
                                    {
                                        sDouble.nPlay = GetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_PLAY_" + IntToString (nDouble));
                                        sDouble.nCost = GetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_COST_" + IntToString (nDouble));
                                        sDouble.nRating = GetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nDouble));

                                        if (!sCard1.nPlay || (sCard1.nPlay && sDouble.nCost < sCard1.nCost))
                                        {
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_PLAY_" + IntToString (nDouble), sCard1.nPlay);
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_COST_" + IntToString (nDouble), sCard1.nCost);
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nDouble), sCard1.nRating);

                                            sCard1 = sDouble;

                                            break;
                                        }
                                        else if (nAI >= CARD_AI_DIFFICULTY_NORMAL && (!sCard2.nPlay || (sCard2.nPlay && sDouble.nCost < sCard2.nCost)))
                                        {
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_PLAY_" + IntToString (nDouble), sCard2.nPlay);
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_COST_" + IntToString (nDouble), sCard2.nCost);
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nDouble), sCard2.nRating);

                                            sCard2 = sDouble;

                                            break;
                                        }
                                        else if (nAI >= CARD_AI_DIFFICULTY_HARD && (!sCard3.nPlay || (sCard3.nPlay && sDouble.nCost < sCard3.nCost)))
                                        {
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_PLAY_" + IntToString (nDouble), sCard3.nPlay);
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_COST_" + IntToString (nDouble), sCard3.nCost);
                                            SetLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nDouble), sCard3.nRating);

                                            sCard3 = sDouble;

                                            break;
                                        }
                                    }
                                }

                                nPower = nPowerMax - sCard1.nCost - sCard2.nCost - sCard3.nCost;
                            }
                    }
                }
            }

            for (nNth = 1; nNth <= nRedundant; nNth++)
            {
                DeleteLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_PLAY_" + IntToString (nNth));
                DeleteLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_COST_" + IntToString (nNth));
                DeleteLocalInt (OBJECT_SELF, "CARD_AI_TEMP_REDUNDANCY_RATING_" + IntToString (nNth));
            }

            for (nNth = 1; nNth <= 3; nNth++)
            {
                int nPlayCard = (nNth == 1) ? sCard1.nPlay : (nNth == 2) ? sCard2.nPlay : sCard3.nPlay;

                if (nPlayCard)
                {
                    sInfo = GetCardInfo (nPlayCard);

                    int nScan = 1;

                    float fDelay = (nNth == 1) ? CYCLE_TIME * CARD_AI_PLAY_CARD_1 :
                                   (nNth == 3) ? CYCLE_TIME * CARD_AI_PLAY_CARD_3 : CYCLE_TIME * CARD_AI_PLAY_CARD_2;

                    string sTag = GetCardTag (sInfo.nCard);

                    object oCard = GetNearestObjectByTag (sTag, oCentre, nScan);

                    while (oCard != OBJECT_INVALID)
                    {
                        if (GetOwner (oCard) == nPlayer)
                            break;

                        oCard = GetNearestObjectByTag (sTag, oCentre, ++nScan);
                    }

                    DelayCommand (fDelay, ActionPlayCard (oCard));
                }
            }

            for (nNth = 1; nNth <= nMax; nNth++)
                DeleteLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nNth));
        }
    }
}

int AIEvaluateCardAngelicChoir (int nMaxHand, int nPlayer)
{
    int nAngels = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ANGEL, nPlayer, OBJECT_SELF, CARD_SCAN_NO_EFFECT, CARD_SPELL_ANGELIC_CHOIR);
    int nRating = (nAngels > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nAngels > 1) ? CARD_AI_WEIGHT_WORTHY : 0;

    int nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sAngel = GetCardInfo (nScan);

            if (sAngel.nSubType == CARD_SUBTYPE_SUMMON_ANGEL)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardAngelicHealer (int nMaxHand, int nPlayer, object oAvatar)
{
    int nAngels = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ANGEL, nPlayer, OBJECT_SELF);
    int nRating = (nAngels > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nAngels > 1) ? CARD_AI_WEIGHT_WORTHY : 0;

    int nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_ANGELIC_DEFENDER
            || nScan == CARD_SUMMON_ARCHANGEL
            || nScan == CARD_SPELL_ANGELIC_CHOIR)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    nCount = GetCurrentHitPoints (oAvatar);
    nRating += (nCount < GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_WORTHY :
               (nCount < GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardAngelicLight (int nMaxHand, int nPlayer)
{
    int nAngels = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ANGEL, nPlayer, OBJECT_SELF);
    int nRating = (nAngels > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nAngels > 1) ? CARD_AI_WEIGHT_WORTHY : 0;

    int nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_ANGELIC_DEFENDER
            || nScan == CARD_SUMMON_ARCHANGEL
            || nScan == CARD_SPELL_ANGELIC_CHOIR)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardArchangel (int nMaxHand, int nPlayer)
{
    int nAngels = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ANGEL, nPlayer, OBJECT_SELF);
    int nRating = (nAngels > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nAngels > 1) ? CARD_AI_WEIGHT_WORTHY : 0;

    int nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sAngel = GetCardInfo (nScan);

            if (sAngel.nSubType == CARD_SUBTYPE_SUMMON_ANGEL || nScan == CARD_SPELL_ANGELIC_CHOIR)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardArmour (int nPlayer, object oAvatar)
{
    int nRating;

    if (!GetHasCardEffect (CARD_SPELL_ARMOUR, oAvatar))
        nRating += CARD_AI_WEIGHT_HIGH_IMPACT;

    if (!nRating)
    {
        int nCount = GetHasCreatures (CARD_SCAN_NO_EFFECT, CARD_SPELL_ARMOUR, nPlayer, OBJECT_SELF);
        nRating += (nCount > 3) ? CARD_AI_WEIGHT_LOW_IMPACT :
                   (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;
    }

    return nRating;
}

int AIEvaluateCardAssassin (int nPlayer, int nEnemy, object oCentre)
{
    int nCountO, nCountE, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan))
            if (GetOwner (oScan) == nPlayer)
                nCountO += 1;
            else
                nCountE += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCountE > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCountE > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;

    nCountO -= nCountE;
    nRating += (nCountO > 4) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > 0) ? CARD_AI_WEIGHT_NOT_RECOMMENDED :
               (nCountO < -4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCountO < 0) ? CARD_AI_WEIGHT_WORTHY : 0;

    return nRating;
}

int AIEvaluateCardAtlantian (int nMaxPower)
{
    return (nMaxPower > 4) ? CARD_AI_WEIGHT_HIGH_IMPACT :
           (nMaxPower > 2) ? CARD_AI_WEIGHT_WORTHY :
           (nMaxPower > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;
}

int AIEvaluateCardAvengingAngel (int nMaxHand, int nPlayer)
{
    int nAngels = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ANGEL, nPlayer, OBJECT_SELF);
    int nRating = (nAngels > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nAngels > 1) ? CARD_AI_WEIGHT_WORTHY : 0;

    int nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_ANGELIC_DEFENDER
            || nScan == CARD_SUMMON_ARCHANGEL
            || nScan == CARD_SPELL_ANGELIC_CHOIR)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardBear (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_DRUID, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_DRUID)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardBeholder (int nMaxHand)
{
    int nRating;
    int nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SPELL_EYE_OF_THE_BEHOLDER)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardBoneGolem (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_SKELETON, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sSkeleton = GetCardInfo (nScan);

            if (sSkeleton.nSubType == CARD_SUBTYPE_SUMMON_SKELETON)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardBoomerang (int nEnemy, object oCentre)
{
    int nCountE, nCountO, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan) && GetOwner (oScan) == nEnemy)
        {
            if (!GetHasCardEffect (CARD_SPELL_PARALYZE, oScan))
                nCountE += 1;
        }
        else
            nCountO += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCountE > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCountE > 0) ? CARD_AI_WEIGHT_WORTHY : 0;

    if (!nCountE)
    {
        if (!GetIsPowerAvailable (nEnemy, OBJECT_SELF, 3))
            nRating += CARD_AI_WEIGHT_WORTHY;

        if (!GetHasGenerators (nEnemy, OBJECT_SELF, 3))
            nRating += CARD_AI_WEIGHT_MEDIUM_IMPACT;

        if (!nRating)
            return CARD_AI_WEIGHT_LOSING_CARD;
    }
    else
    {
        nCountO -= nCountE;
        nRating += (nCountO > 4) ? CARD_AI_WEIGHT_WORTHY_LOSS :
                   (nCountO > 0) ? CARD_AI_WEIGHT_NOT_RECOMMENDED :
                   (nCountO < -4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                   (nCountO < 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;
    }

    return nRating;
}

int AIEvaluateCardBulette (int nEnemy)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ANIMAL, nEnemy, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardChaosWitch (int nEnemy, object oCentre)
{
    int nCountE, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan) && GetOwner (oScan) == nEnemy)
            nCountE += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return (nCountE > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
           (nCountE > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;
}

int AIEvaluateCardCounterspell (int nEnemy)
{
    return (GetHasGenerators (nEnemy, OBJECT_SELF, 4)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_WORTHY;
}

int AIEvaluateCardDeathPact (object oAvatar, object oCentre)
{
    int nCount = GetCurrentHitPoints (oAvatar);
    int nRating = (nCount <= GetPercentHitPoints (10, oAvatar)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount <= GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount <= GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount <= GetPercentHitPoints (90, oAvatar)) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_LOSING_CARD;

    if (nRating)
    {
        int nCorpses;
        nCount = 0;

        object oCorpse = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCorpses, CREATURE_TYPE_IS_ALIVE, FALSE);

        if (oCorpse == OBJECT_INVALID)
            return CARD_AI_WEIGHT_LOSING_CARD;

        while (oCorpse != OBJECT_INVALID)
        {
            if (GetCardID (oCorpse))
                nCount += 1;

            oCorpse = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCorpses, CREATURE_TYPE_IS_ALIVE, FALSE);
        }

        nRating += (nCount <= 0) ? CARD_AI_WEIGHT_LOSING_CARD :
                   (nCount <= 2) ? CARD_AI_WEIGHT_WORTHY_LOSS : 0;
    }

    return nRating;
}

int AIEvaluateCardDispelMagic (int nMaxHand, int nPlayer, int nEnemy, object oAvatar, object oEnemy, object oCentre)
{
    int nCount = GetTotalGlobalSpells (OBJECT_SELF);
    int nRating = (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_WORTHY_LOSS;
    int nGenO = GetHasGenerators (nPlayer, OBJECT_SELF);
    int nGenE = GetHasGenerators (nEnemy, OBJECT_SELF);

    nCount = GetHasCardEffect (CARD_SPELL_FLUX, OBJECT_SELF);
    if (nCount)
    {
        if (GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nPlayer, OBJECT_SELF, 1))
            nRating += CARD_AI_WEIGHT_WORTHY;

        int nCountE = GetCurrentHitPoints (oAvatar);

        if (nCountE < GetCurrentHitPoints (oEnemy) + 10)
            nRating += (nCountE < GetPercentHitPoints (33, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                       (nCountE < GetPercentHitPoints (66, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

        nCount *= nGenO + nGenE;
        nRating += (nCount > 9) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                   (nCount > 6) ? CARD_AI_WEIGHT_WORTHY :
                   (nCount > 3) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;
    }

    if (GetHasCardEffect (CARD_SPELL_VORTEX, OBJECT_SELF) || GetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, OBJECT_SELF))
        nRating += (nGenO > 6) ? 0 :
                   (nGenO > 4) ? CARD_AI_WEIGHT_LOW_IMPACT :
                   (nGenO > 2) ? CARD_AI_WEIGHT_WORTHY :
                   (nGenO > 0) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_HIGH_IMPACT;
    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SPELL_ARMOUR || nScan == CARD_SPELL_FIRE_SHIELD)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    nCount = 1;
    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan))
        {
            if (GetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oScan)
                || GetHasCardEffect (CARD_SPELL_ARMOUR, oScan)
                || GetHasCardEffect (CARD_SPELL_ANGELIC_CHOIR, oScan)
                || GetHasCardEffect (CARD_SPELL_MIND_CONTROL, oScan)
                || GetHasCardEffect (CARD_SPELL_SIMULACRUM, oScan)
                || GetHasCardEffect (CARD_SPELL_WRATH_OF_THE_HORDE, oScan)
                || GetHasCardEffect (CARD_SPELL_POTION_OF_HEROISM, oScan)
                || GetHasCardEffect (CARD_SPELL_HOLY_VENGEANCE, oScan))
                nRating += (GetOwner (oScan) == nPlayer) ? CARD_AI_WEIGHT_LOW_LOSS : CARD_AI_WEIGHT_LOW_IMPACT;

            if (GetHasCardEffect (CARD_SPELL_PARALYZE, oScan))
                nRating += (GetOwner (oScan) == nPlayer) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_LOW_LOSS;
        }
        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (GetHasCardEffect (CARD_SPELL_ARMOUR, oAvatar) || GetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oAvatar))
        nRating += CARD_AI_WEIGHT_MEDIUM_LOSS;

    if (GetHasCardEffect (CARD_SPELL_ARMOUR, oEnemy) || GetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oEnemy))
        nRating += CARD_AI_WEIGHT_WORTHY;
    return nRating;
}

int AIEvaluateCardDragon (int nPlayer, object oAvatar, object oCentre)
{
    int nCountO, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetOwner (oScan) == nPlayer)
            nCountO += GetCurrentHitPoints (oScan);

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCountO > 75) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCountO > 50) ? CARD_AI_WEIGHT_WORTHY : 0;

    if (!nRating)
    {
        nCount = GetCurrentHitPoints (oAvatar);
        nRating += (nCount > GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;
    }

    return nRating;
}

int AIEvaluateCardDruid (int nPlayer, object oCentre)
{
    int nCountO, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetOwner (oScan) == nPlayer && GetRacialType (oScan) == RACIAL_TYPE_ANIMAL)
            nCountO += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return (nCountO > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
           (nCountO > 2) ? CARD_AI_WEIGHT_WORTHY :
           (nCountO > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;
}

int AIEvaluateCardElixirOfLife (int nMaxPower, int nPlayer, object oAvatar, object oCentre)
{
    int nCount = GetCurrentHitPoints (oAvatar);
    int nRating = (nCount <= GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount <= GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount <= GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : 0;
    if (!nRating)
    {
        oAvatar = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_LIFE, nPlayer, OBJECT_SELF);
        if(oAvatar != OBJECT_INVALID)
        {
            nCount = GetCurrentHitPoints (oAvatar);
            nRating += (nCount <= GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                       (nCount <= GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                       (nCount <= GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;
        }
    }

    if (nRating)
        nRating += (nMaxPower * 10 > nCount) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_MEDIUM_LOSS;
    return nRating;
}

int AIEvaluateCardEnergyDisruption (int nPlayer, int nEnemy)
{
    int nCount = GetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, OBJECT_SELF);
    int nRating = (nCount > 4) ? CARD_AI_WEIGHT_LOW_LOSS :
                  (nCount > 2) ? CARD_AI_WEIGHT_NOT_RECOMMENDED :
                  (nCount > 0) ? CARD_AI_WEIGHT_NEGLIGIBLE : CARD_AI_WEIGHT_WORTHY;

    nCount = GetHasGenerators (nPlayer, OBJECT_SELF);
    nRating += (nCount > 4) ? CARD_AI_WEIGHT_LOW_IMPACT :
               (nCount > 2) ? CARD_AI_WEIGHT_NOT_RECOMMENDED :
               (nCount > 0) ? CARD_AI_WEIGHT_WORTHY_LOSS : CARD_AI_WEIGHT_MEDIUM_LOSS;

    int nCountE = GetHasGenerators (nEnemy, OBJECT_SELF);
    nRating += (nCount > nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCount > nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCount == nCountE) ? CARD_AI_WEIGHT_LOW_IMPACT :
               (nCount < nCountE) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCount < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_LOSS : 0;

    return nRating;
}

int AIEvaluateCardEyeOfTheBeholder (int nMaxHand, int nPlayer, int nEnemy)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_BEHOLDER, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 2) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_LOSING_CARD;

    if (nRating < 0)
        return nRating;

    int nCountE = GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nEnemy, OBJECT_SELF);
    nRating += (nCountE > 6 && nCount > 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCountE > 3) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_WORTHY;

    nCount = GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nPlayer, OBJECT_SELF);

    nRating += (nCount > nCountE * 2) ? CARD_AI_WEIGHT_NOT_RECOMMENDED :
               (nCount > nCountE) ? CARD_AI_WEIGHT_NEGLIGIBLE :
               (nCount == nCountE) ? CARD_AI_WEIGHT_LOW_IMPACT :
               (nCount < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCount < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : 0;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sBeholder = GetCardInfo (nScan);

            if (sBeholder.nSubType == CARD_SUBTYPE_SUMMON_BEHOLDER)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardFairyDragon (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_MYTHICAL_SAESHEN, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 0) ? CARD_AI_WEIGHT_HIGH_IMPACT : 0;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sCheck = GetCardInfo (nScan);

            if (sCheck.nMagic <= 2 && sCheck.nType != CARD_TYPE_GENERATOR)
                nRating += CARD_AI_WEIGHT_WORTHY;

            if (sCheck.nCard == CARD_MYTHICAL_SAESHEN)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardFeralRat (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_RAT_KING, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_RAT_KING)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardFireball (int nMaxPower, int nEnemy, object oEnemy)
{
    int nScan, nCount;
    int nFireball = nMaxPower * 3;

    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nEnemy, OBJECT_SELF);
    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oTarget, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);

    if (oTarget == OBJECT_INVALID)
        return CARD_AI_WEIGHT_LOSING_CARD;

    int nRating = (nFireball > GetCurrentHitPoints (oTarget)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_WORTHY;

    while (oScan != OBJECT_INVALID && GetDistanceBetween (oTarget, oScan) <= RADIUS_SIZE_LARGE)
    {
        if (GetOwner (oScan) == nEnemy)
            if (oScan == oEnemy)
                nRating += (nFireball > GetCurrentHitPoints (oScan)) ? CARD_AI_WEIGHT_WINNING_CARD : CARD_AI_WEIGHT_WORTHY;
            else if (GetCardID (oScan))
                nRating += (nFireball > GetCurrentHitPoints (oScan)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oTarget, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return nRating;
}

int AIEvaluateCardFireShield (int nPlayer, object oAvatar)
{
    int nRating;

    if (!GetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oAvatar))
        nRating += CARD_AI_WEIGHT_HIGH_IMPACT;

    if (!nRating)
    {
        int nCount = GetHasCreatures (CARD_SCAN_NO_EFFECT, CARD_SPELL_FIRE_SHIELD, nPlayer, OBJECT_SELF);
        nRating += (nCount > 3) ? CARD_AI_WEIGHT_LOW_IMPACT :
                   (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;
    }

    return nRating;
}

int AIEvaluateCardFlux (int nMaxHand, int nPlayer, int nEnemy, object oAvatar, object oEnemy)
{
    int nRating;
    int nCount = GetHasCardEffect (CARD_SPELL_FLUX, OBJECT_SELF);
    int nGenO = GetHasGenerators (nPlayer, OBJECT_SELF);
    int nGenE = GetHasGenerators (nEnemy, OBJECT_SELF);

    int nCountE = GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nEnemy, OBJECT_SELF);
    nRating += (nCountE > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCountE > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NOT_RECOMMENDED;

    nCountE = GetCurrentHitPoints (oAvatar);

    if (nCountE < GetCurrentHitPoints (oEnemy) + 10)
        nRating += (nCountE < GetPercentHitPoints (33, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_LOSS :
                   (nCountE < GetPercentHitPoints (66, oAvatar)) ? CARD_AI_WEIGHT_WORTHY_LOSS : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount *= nGenO + nGenE;
    nRating += (nCount > 9) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCount > 6) ? CARD_AI_WEIGHT_WORTHY :
               (nCount > 3) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    int nCountH = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCountH));

    while (nCountH <= nMaxHand)
    {
        if (nScan == CARD_SPELL_ELIXIR_OF_LIFE
            || nScan == CARD_SPELL_DEATH_PACT
            || nScan == CARD_SPELL_LIFE_DRAIN)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCountH));
    }

    return nRating;
}

int AIEvaluateCardGoblin (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_GOBLIN_WARLORD, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_GOBLIN_WARLORD)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardGoblinWarlord (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_GOBLIN, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 6) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 2) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sGoblin = GetCardInfo (nScan);

            if (sGoblin.nSubType == CARD_SUBTYPE_SUMMON_GOBLIN)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardGoblinWitchdoctor (int nMaxHand, int nPlayer, object oAvatar)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_GOBLIN_WARLORD, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_GOBLIN_WARLORD)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    object oScan = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND, nPlayer, OBJECT_SELF);

    int nCheck = GetCurrentHitPoints (oAvatar);
    nCount = GetMaxHitPoints (oScan);

    if (nCount > nCheck)
        nRating += (nCheck <= GetPercentHitPoints (33, oAvatar)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                   (nCheck <= GetPercentHitPoints (66, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardHealingLight (int nPlayer, object oAvatar)
{
    object oScan = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_LIFE, nPlayer, OBJECT_SELF);

    if (oScan == OBJECT_INVALID)
        return CARD_AI_WEIGHT_LOSING_CARD;

    int nCount = GetCurrentHitPoints (oScan);
    int nRating = (nCount < GetPercentHitPoints (50, oScan)) ? CARD_AI_WEIGHT_LOW_IMPACT :
                  (nCount < GetPercentHitPoints (75, oScan)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NOT_RECOMMENDED;

    nCount = GetCurrentHitPoints (oAvatar);
    nRating += (nCount < GetPercentHitPoints (50, oScan)) ? CARD_AI_WEIGHT_WORTHY :
               (nCount < GetPercentHitPoints (75, oScan)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_NOT_RECOMMENDED;

    return nRating;
}

int AIEvaluateCardHigherCalling (int nPlayer, object oCentre)
{
    int nRating, nCount, nCountO, nCountE;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
        {
            sInfo = GetCardInfo (nCard);

            if (GetOwner (oScan) == nPlayer)
            {
                if(sInfo.nAttack > 2)
                  nCountO += 1;
            }
            else
            {
                if(sInfo.nAttack > 2)
                  nCountE += 1;

            }
        }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (!nCountE)
        return CARD_AI_WEIGHT_LOSING_CARD;

    nRating += (nCountO > 5) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > 3) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO > 1) ? CARD_AI_WEIGHT_NOT_RECOMMENDED : CARD_AI_WEIGHT_LOW_IMPACT ;
    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCountO < nCountE * 3) ? CARD_AI_WEIGHT_HIGH_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateCardHolyVengeance (int nPlayer)
{
    object oScan = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nPlayer, OBJECT_SELF, CARD_SCAN_NO_EFFECT, CARD_SPELL_HOLY_VENGEANCE, CARD_SCAN_NO_EFFECT, CARD_SPELL_PARALYZE);

    if (oScan == OBJECT_INVALID)
        return CARD_AI_WEIGHT_LOSING_CARD;

    struct sCard sInfo = GetCardInfo (GetCardID (oScan));

    int nRating = (sInfo.nAttack == 0) ? CARD_AI_WEIGHT_WORTHY_LOSS :
                  (sInfo.nAttack <= 2) ? CARD_AI_WEIGHT_NEGLIGIBLE :
                  (sInfo.nAttack <= 4) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_MEDIUM_IMPACT;

    return nRating;
}

int AIEvaluateCardIntellectDevourer (int nMaxHand, int nPlayer, int nEnemy)
{
    int nDeckO = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nDeckE = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nCountO = GetTotalCards (nDeckO, OBJECT_SELF);
    int nCountE = GetTotalCards (nDeckE, OBJECT_SELF);
    int nRating = (nCountE > nCountO + 2) ? CARD_AI_WEIGHT_LOSING_CARD : 0;

    if (nRating < 0)
        return nRating;

    int nStones = GetHasStones (CARD_STONE_SOLAR, nPlayer, OBJECT_SELF) + GetHasStones (CARD_STONE_SOLAR, nEnemy, OBJECT_SELF);

    nRating += (nStones > 5) ? CARD_AI_WEIGHT_HIGH_IMPACT :
               (nStones > 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nStones > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

    int nCountH = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCountH));

    while (nCountH <= nMaxHand)
    {
        if (nScan == CARD_STONE_SOLAR)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCountH));
    }

    return nRating;
}

int AIEvaluateCardJysirael (int nMaxHand, int nPlayer, object oCentre)
{
    int nCount, nScan;
    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan) && GetOwner (oScan) == nPlayer)
            if (GetCurrentHitPoints (oScan) < GetPercentHitPoints (50, oScan))
                nCount += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : 0;

    nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_ARCHANGEL, nPlayer, OBJECT_SELF);
    nRating += (nCount > 3) ? CARD_AI_WEIGHT_WORTHY :
               (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : 0;

    nCount = 1;
    nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SPELL_ANGELIC_CHOIR)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardKobold (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_KOBOLD_CHIEF, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_KOBOLD_CHIEF)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardKoboldChief (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_KOBOLD, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 6) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 2) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sGoblin = GetCardInfo (nScan);

            if (sGoblin.nSubType == CARD_SUBTYPE_SUMMON_KOBOLD)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardKoboldEngineer (int nMaxHand, int nPlayer, int nEnemy, object oCentre)
{
    int nCount, nCountO, nCountE;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
            if (GetOwner (oScan) == nPlayer)
            {
                if (nCard == CARD_SUMMON_KOBOLD_CHIEF)
                    nCountO += 1;
            }
            else
            {
                sInfo = GetCardInfo (nCard);

                if (sInfo.nSubType == CARD_SUBTYPE_SUMMON_KOBOLD)
                    nCountE += 1;
            }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCountO > 3) ? CARD_AI_WEIGHT_WORTHY :
                  (nCountO > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nRating += (nCountE > 3) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountE > 1) ? CARD_AI_WEIGHT_LOW_LOSS : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_KOBOLD_CHIEF)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardKoboldPogostick (int nMaxHand, int nPlayer, int nEnemy, object oEnemy)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_KOBOLD_CHIEF, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    if (GetCurrentHitPoints (oEnemy) < 25)
        nRating += CARD_AI_WEIGHT_MEDIUM_IMPACT;

    if (GetCurrentHitPoints (GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nEnemy, OBJECT_SELF)) < 25)
        nRating += CARD_AI_WEIGHT_WORTHY;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_KOBOLD_CHIEF)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardLich (int nPlayer, object oCentre)
{
    int nCountO, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetOwner (oScan) == nPlayer && GetRacialType (oScan) == RACIAL_TYPE_UNDEAD)
            nCountO += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return (nCountO > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
           (nCountO > 2) ? CARD_AI_WEIGHT_WORTHY :
           (nCountO > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;
}

int AIEvaluateCardLifeDrain (int nEnemy, object oAvatar)
{
    int nCount = GetCurrentHitPoints (oAvatar);
    int nRating = (nCount < GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount < GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount < GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_LOSING_CARD;

    if (nRating < 0)
        return nRating;

    nCount = GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND_LIVING, nEnemy, OBJECT_SELF);
    nRating = (nCount > 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
              (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_LOSING_CARD;

    return nRating;
}

int AIEvaluateCardLightningBolt (int nEnemy, object oEnemy)
{
    object oScan = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_DEFEND, nEnemy, OBJECT_SELF);

    int nCount, nRating;

    if (oScan == OBJECT_INVALID)
        oScan = oEnemy;

    nCount = GetCurrentHitPoints (oScan);
    nRating += (nCount < GetPercentHitPoints (25, oScan)) ? CARD_AI_WEIGHT_WORTHY :
               (nCount < GetPercentHitPoints (50, oScan)) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    if (oScan == oEnemy)
    {
        if (20 > nCount)
            nRating += CARD_AI_WEIGHT_WINNING_CARD;
    }
    else if (20 > nCount)
        nRating += CARD_AI_WEIGHT_WORTHY;

    return nRating;
}

int AIEvaluateCardLoremaster (int nPlayer, object oCentre)
{
    int nCountO, nCount;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan) == CARD_SUMMON_LOREMASTER)
            nCountO += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCount > 2) ? CARD_AI_WEIGHT_LOW_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_MEDIUM_IMPACT;

    nRating = (GetCardsInHand (nPlayer, oCentre, GetDrawMaximum (OBJECT_SELF) - nCount)) ? CARD_AI_WEIGHT_LOW_LOSS : CARD_AI_WEIGHT_WORTHY;

    return nRating;
}

int AIEvaluateCardMaidenOfParadise (int nMaxHand, int nPlayer)
{
    int nCount = GetHasGenerators (nPlayer, OBJECT_SELF);
    int nRating = (nCount > 4) ? CARD_AI_WEIGHT_NEGLIGIBLE :
                  (nCount > 2) ? CARD_AI_WEIGHT_LOW_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_HIGH_IMPACT;

    int nCountO = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCountO));

    while (nCountO <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sCheck = GetCardInfo (nScan);

            if (sCheck.nMagic > nCountO || sCheck.nBoost)
                nRating += CARD_AI_WEIGHT_WORTHY;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCountO));
    }

    return nRating;
}

int AIEvaluateCardMindControl (int nMaxPower, int nPlayer, object oCentre)
{
    // subtract one to reflect new cost of card
    nMaxPower--;

    int nRating, nCount, nCountO, nCountE, nAttackO, nAttackE, nMagic;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
            if (GetOwner (oScan) == nPlayer)
            {
                nCountO += 1;

                sInfo = GetCardInfo (nCard);

                if (sInfo.nAttack > nAttackO)
                    nAttackO = sInfo.nAttack;
            }
            else
            {
                nCountE += 1;

                sInfo = GetCardInfo (nCard);

                if (sInfo.nAttack > nAttackE)
                    nAttackE = sInfo.nAttack;

                if (sInfo.nMagic <= nMaxPower && sInfo.nMagic > nMagic)
                    nMagic = sInfo.nMagic;
            }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (!nCountE)
        return CARD_AI_WEIGHT_LOSING_CARD;

    nRating += (nMagic > nMaxPower * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nMagic > nMaxPower) ? CARD_AI_WEIGHT_WORTHY :
               (nMagic < nMaxPower) ? CARD_AI_WEIGHT_WORTHY_LOSS : CARD_AI_WEIGHT_LOW_IMPACT;
    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;
    nRating += (nAttackE > nAttackO * 2) ? CARD_AI_WEIGHT_HIGH_IMPACT :
               (nAttackE > nAttackO) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nAttackE < nAttackO) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nAttackE < nAttackO * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateCardMindOverMatter (int nPlayer, object oAvatar, object oCentre)
{
    int nCount, nHealC, nSummonC, nRating;
    int nScan = GetDiscardPile (++nCount, nPlayer, OBJECT_SELF);
    int nMax = GetDiscardPileSize (nPlayer, OBJECT_SELF);

    if (!nMax)
        return CARD_AI_WEIGHT_LOSING_CARD;

    struct sCard sInfo;

    while (nCount <= nMax)
    {
        sInfo = GetCardInfo (nScan);

        if (nScan == CARD_SPELL_ELIXIR_OF_LIFE
            || nScan == CARD_SPELL_DEATH_PACT
            || nScan == CARD_SPELL_LIFE_DRAIN)
            nHealC += 1;

        if (sInfo.nType == CARD_TYPE_SUMMON || sInfo.nType == CARD_TYPE_MYTHICAL)
            nSummonC += 1;

        nScan = GetDiscardPile (++nCount, nPlayer, OBJECT_SELF);
    }

    int nPercent = FloatToInt (0.2 * IntToFloat (nMax));

    if (nPercent <= nHealC)
    {
        nCount = GetCurrentHitPoints (oAvatar);
        nRating += (nCount < GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                   (nCount < GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;
    }

    int nCountO, nCountE;
    nCount = 1;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan))
            if (GetOwner (oScan) == nPlayer)
                nCountO += 1;
            else
                nCountE += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (nPercent <= nSummonC)
        nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
                   (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
                   (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
                   (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;

    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_IMPACT :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_LOSS : CARD_AI_WEIGHT_LOW_LOSS;

    return nRating;
}

int AIEvaluateCardPainGolem (int nEnemy, object oCentre)
{
    return (GetCardsInHand (nEnemy, oCentre, 5)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;
}

int AIEvaluateCardParalyze (int nPlayer, int nEnemy, object oCentre)
{
    int nCountO, nCountE;
    int nCount = GetHasCreatures (CARD_SCAN_NO_EFFECT, CARD_SPELL_PARALYZE, nEnemy, OBJECT_SELF);
    int nRating = (nCount > 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;

    if (nRating > 0)
    {
        nCount = 1;

        object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

        while (oScan != OBJECT_INVALID)
        {
            if (GetCardID (oScan))
                if (GetOwner (oScan) == nPlayer)
                    nCountO += 1;
                else
                    nCountE += 1;

            oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
        }

        nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
                   (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
                   (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
                   (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;
    }

    return nRating;
}

int AIEvaluateCardPitFiend (int nPlayer)
{
    int nCount = GetHasGenerators (nPlayer, OBJECT_SELF);
    int nRating = (nCount > 5) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 3) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_NOT_RECOMMENDED : CARD_AI_WEIGHT_MEDIUM_LOSS;

    nCount = GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nPlayer, OBJECT_SELF);
    nRating = (nCount > 5) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
              (nCount > 3) ? CARD_AI_WEIGHT_WORTHY :
              (nCount > 0) ? CARD_AI_WEIGHT_NOT_RECOMMENDED : CARD_AI_WEIGHT_MEDIUM_LOSS;

    return nRating;
}

int AIEvaluateCardPlagueBearer (int nMaxHand, int nPlayer, object oCentre)
{
    int nCount, nCountO, nCountE;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
            if (GetOwner (oScan) == nPlayer)
            {
                if (nCard == CARD_SUMMON_RAT_KING)
                    nCountO += 1;
            }
            else
            {
                sInfo = GetCardInfo (nCard);

                if (sInfo.nSubType == CARD_SUBTYPE_SUMMON_RAT)
                    nCountE += 1;
            }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    int nRating = (nCountO > 3) ? CARD_AI_WEIGHT_WORTHY :
                  (nCountO > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nRating += (nCountE > 3) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountE > 1) ? CARD_AI_WEIGHT_LOW_LOSS : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_RAT_KING)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardPotionOfHeroism (int nPlayer)
{
    return (GetHasCreatures (CARD_SCAN_NO_EFFECT, CARD_SPELL_POTION_OF_HEROISM, nPlayer, OBJECT_SELF, FALSE, CARD_SCAN_NO_EFFECT, CARD_SPELL_PARALYZE) > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;
}

int AIEvaluateCardPowerStream (int nPlayer)
{
  // assume the deck is designed to use these cards effectively
  return CARD_AI_WEIGHT_MEDIUM_IMPACT;
}

int AIEvaluateCardRatKing (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_RAT, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 6) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 2) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sGoblin = GetCardInfo (nScan);

            if (sGoblin.nSubType == CARD_SUBTYPE_SUMMON_RAT)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardResurrect (int nMaxPower, int nPlayer, object oAvatar, object oCentre)
{
    int nCount, nCountO, nCountE, nRating;

    object oDead = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD, nPlayer, OBJECT_SELF);

    if (oDead == OBJECT_INVALID)
        return CARD_AI_WEIGHT_LOSING_CARD;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan))
            if (GetOwner (oScan) == nPlayer)
                nCountO += 1;
            else
                nCountE += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;

    sInfo = GetCardInfo (GetCardID (oDead));

    nCount = GetAISacrificeEvaluation (sInfo, nMaxPower, nPlayer, oAvatar, oDead);
    nRating += (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : 0;

    return nRating;
}

int AIEvaluateCardSabotage (int nEnemy)
{
    int nCount = GetHasGenerators (nEnemy, OBJECT_SELF);
    int nRating = (nCount > 4) ? CARD_AI_WEIGHT_NEGLIGIBLE :
                  (nCount > 2) ? CARD_AI_WEIGHT_LOW_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOSING_CARD;

    if (nRating < 0)
        return nRating;

    nCount = GetIsPowerAvailable (nEnemy, OBJECT_SELF);
    nRating += (nCount <= 2) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateCardSaeshen (int nMaxHand, int nPlayer)
{
    int nCount = 1;
    int nRating;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_FAIRY_DRAGON)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_FAIRY_DRAGON, nPlayer, OBJECT_SELF);
    nRating += (nCount > 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : 0;

    return nRating;
}

int AIEvaluateCardScorchedEarth (int nPlayer, object oCentre)
{
    int nRating, nCount, nCountO, nCountE, nAttackO, nAttackE;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
        {
            sInfo = GetCardInfo (nCard);

            if (GetOwner (oScan) == nPlayer)
            {
                nCountO += 1;
                nAttackO += sInfo.nAttack;
            }
            else
            {
                nCountE += 1;
                nAttackE += sInfo.nAttack;
            }
        }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (!nCountE)
        return CARD_AI_WEIGHT_LOSING_CARD;

    nRating += (nCountO > 5) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > 3) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO > 1) ? CARD_AI_WEIGHT_NOT_RECOMMENDED : CARD_AI_WEIGHT_LOW_IMPACT ;
    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;
    nRating += (nAttackO > nAttackE * 2) ? CARD_AI_WEIGHT_LOSING_CARD :
               (nAttackO > nAttackE) ? CARD_AI_WEIGHT_HIGH_LOSS :
               (nAttackO < nAttackE) ? CARD_AI_WEIGHT_WORTHY :
               (nAttackO < nAttackE * 2) ? CARD_AI_WEIGHT_HIGH_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardSeaHag (int nEnemy)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_RAT, nEnemy, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardSimulacrum (int nPlayer, object oCentre)
{
    int nRating, nCount, nCountO, nCountE, nAttackO, nAttackE;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
            if (GetOwner (oScan) == nPlayer)
            {
                nCountO += 1;

                sInfo = GetCardInfo (nCard);

                if (sInfo.nAttack > nAttackO)
                    nAttackO = sInfo.nAttack;
            }
            else
            {
                nCountE += 1;

                sInfo = GetCardInfo (nCard);

                if (sInfo.nAttack > nAttackE)
                    nAttackE = sInfo.nAttack;
            }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (!nCountE)
        return CARD_AI_WEIGHT_LOSING_CARD;

    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;
    nRating += (nAttackE > nAttackO * 2) ? CARD_AI_WEIGHT_HIGH_IMPACT :
               (nAttackE > nAttackO) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nAttackE < nAttackO) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nAttackE < nAttackO * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateCardSkeleton (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_BONE_GOLEM, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_BONE_GOLEM)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardSolarStone (int nMaxHand, int nPlayer, int nEnemy)
{
    int nDeckO = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nDeckE = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nCountO = GetTotalCards (nDeckO, OBJECT_SELF);
    int nCountE = GetTotalCards (nDeckE, OBJECT_SELF);
    int nRating = (nCountE > nCountO) ? CARD_AI_WEIGHT_LOSING_CARD : 0;

    if (nRating < 0)
        return nRating;

    int nStones = GetHasStones (CARD_STONE_SOLAR, nPlayer, OBJECT_SELF) + GetHasStones (CARD_STONE_SOLAR, nEnemy, OBJECT_SELF);

    nRating += (nStones > 5) ? CARD_AI_WEIGHT_LOW_IMPACT :
               (nStones > 2) ? CARD_AI_WEIGHT_WORTHY :
               (nStones > 0) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_HIGH_IMPACT;

    int nCountH = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCountH));

    while (nCountH <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_INTELLECT_DEVOURER)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCountH));
    }

    return nRating;
}

int AIEvaluateCardSpiritGuardian (object oAvatar)
{
    int nCount = GetCurrentHitPoints (oAvatar);

    return (nCount < GetPercentHitPoints (70, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;
}

int AIEvaluateCardTroglodyte (int nEnemy)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_GOBLIN, nEnemy, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardVampireMaster (object oAvatar)
{
    int nCount = GetCurrentHitPoints (oAvatar);

    return (nCount <= GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_WORTHY_LOSS :
           (nCount <= GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_NOT_RECOMMENDED : CARD_AI_WEIGHT_NEGLIGIBLE;
}

int AIEvaluateCardVortex (int nPlayer)
{
    int nCount = GetHasGenerators (nPlayer, OBJECT_SELF);
    int nRating = (nCount > 6) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 3) ? CARD_AI_WEIGHT_LOW_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_NOT_RECOMMENDED : CARD_AI_WEIGHT_LOSING_CARD;

    nCount = GetHasCardEffect (CARD_SPELL_VORTEX, OBJECT_SELF);
    nRating += (nCount > 2) ? CARD_AI_WEIGHT_NOT_RECOMMENDED :
               (nCount > 0) ? CARD_AI_WEIGHT_NEGLIGIBLE : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateCardWarpReality (int nPlayer, object oCentre)
{
    int nRating, nCount, nCountO, nCountE, nAttackO, nAttackE;

    struct sCard sInfo;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
        {
            sInfo = GetCardInfo (nCard);

            if (GetOwner (oScan) == nPlayer)
            {
                nCountO += 1;
                nAttackO += sInfo.nAttack;
            }
            else
            {
                nCountE += 1;
                nAttackE += sInfo.nAttack;
            }
        }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (!nCountE)
        return CARD_AI_WEIGHT_LOSING_CARD;

    // Adam tweaked numbers to make it "harder" for AI to play
    if (!nCountO)
        nRating += (nCountE > 8) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                   (nCountE > 5) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                   (nCountE > 3) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_WORTHY_LOSS;

    nRating += (nCountO > nCountE * 2) ? CARD_AI_WEIGHT_WORTHY_LOSS :
               (nCountO > nCountE) ? CARD_AI_WEIGHT_LOW_LOSS :
               (nCountO < nCountE) ? CARD_AI_WEIGHT_WORTHY :
               (nCountO < nCountE * 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_LOW_IMPACT;
    nRating += (nAttackO > nAttackE * 2) ? CARD_AI_WEIGHT_LOSING_CARD :
               (nAttackO > nAttackE) ? CARD_AI_WEIGHT_HIGH_LOSS :
               (nAttackO < nAttackE) ? CARD_AI_WEIGHT_WORTHY :
               (nAttackO < nAttackE * 2) ? CARD_AI_WEIGHT_HIGH_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = GetHasGenerators (nPlayer, OBJECT_SELF);
    nRating += (nCount > 5) ? CARD_AI_WEIGHT_HIGH_LOSS :
               (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_LOSS :
               (nCount > 1) ? CARD_AI_WEIGHT_LOW_LOSS : CARD_AI_WEIGHT_NEGLIGIBLE;

    return nRating;
}

int AIEvaluateCardWhiteStag (int nMaxHand, int nPlayer, object oAvatar)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_DRUID, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_WORTHY;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_DRUID)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    nCount = GetCurrentHitPoints (oAvatar);
    nRating += (nCount < GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nCount < GetPercentHitPoints (75, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateCardWolf (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_WOLF, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 6) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 2) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sWolf = GetCardInfo (nScan);

            if (sWolf.nSubType == CARD_SUBTYPE_SUMMON_WOLF)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardWrathOfTheHorde (int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_NO_EFFECT, CARD_SPELL_WRATH_OF_THE_HORDE, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 5) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 1) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_LOSING_CARD;

    return nRating;
}

int AIEvaluateCardZombie (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_ZOMBIE_LORD, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan == CARD_SUMMON_ZOMBIE_LORD)
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateCardZombieLord (int nMaxHand, int nPlayer)
{
    int nCount = GetHasCreatures (CARD_SCAN_CARD_SUBTYPE, CARD_SUBTYPE_SUMMON_ZOMBIE, nPlayer, OBJECT_SELF);
    int nRating = (nCount > 6) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount > 4) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nCount > 2) ? CARD_AI_WEIGHT_WORTHY :
                  (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;

    nCount = 1;
    int nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nCount));

    while (nCount <= nMaxHand)
    {
        if (nScan)
        {
            struct sCard sZombie = GetCardInfo (nScan);

            if (sZombie.nSubType == CARD_SUBTYPE_SUMMON_ZOMBIE)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;
        }

        nScan = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nCount));
    }

    return nRating;
}

int AIEvaluateSacrificeCougar (int nEnemy, object oCreature)
{
    int nCount = 1;
    int nKill, nCountE;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        if (GetCardID (oScan) && GetOwner (oScan) == nEnemy)
            if (GetCurrentHitPoints (oScan) - 10 <= 0)
                nKill += 1;
            else
                nCountE += 1;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return (nKill > nCountE) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;
}

int AIEvaluateSacrificeCow (object oAvatar)
{
    return (GetCurrentHitPoints (oAvatar) < GetPercentHitPoints (90, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NOT_RECOMMENDED;
}

int AIEvaluateSacrificeDeekin (int nPlayer, int nEnemy, object oEnemy)
{
    int nKobolds = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_SUMMON_KOBOLD, nPlayer, OBJECT_SELF);
    int nRating = (nKobolds > 3) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                  (nKobolds > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : 0;

    if (GetCurrentHitPoints (oEnemy) < 25 * nKobolds)
        nRating += CARD_AI_WEIGHT_MEDIUM_IMPACT;

    if (GetCurrentHitPoints (GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nEnemy, OBJECT_SELF)) < 25 * nKobolds)
        nRating += CARD_AI_WEIGHT_WORTHY;

    return nRating;
}

int AIEvaluateSacrificeDemonKnight (int nEnemy, object oCreature)
{
    int nCount, nRating;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID && GetDistanceBetween (oScan, oCreature) <= RADIUS_SIZE_LARGE)
    {
        if (GetCardID (oScan) && GetOwner (oScan) == nEnemy && !GetHasCardEffect (CARD_SUMMON_DEMON_KNIGHT, oScan))
            nRating += CARD_AI_WEIGHT_LOW_IMPACT;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nCount, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return nRating;
}

int AIEvaluateSacrificeFairyDragon (int nPlayer, object oCentre)
{
    int nCount = 1;
    int nApply, nRating;
    int nScan = GetCardsInHand (nPlayer, oCentre, nCount);

    while (nScan)
    {
        struct sCard sCheck = GetCardInfo (nScan);

        if (sCheck.nMagic <= 2 && sCheck.nType != CARD_TYPE_GENERATOR)
            nRating += CARD_AI_WEIGHT_WORTHY;

        nScan = GetCardsInHand (nPlayer, oCentre, ++nCount);
    }

    if (!nRating)
        return 0;

    return nRating;
}

int AIEvaluateSacrificeGoblinWitchdoctor (int nPlayer, object oAvatar)
{
    object oScan = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND, nPlayer, OBJECT_SELF);

    if (oScan == OBJECT_INVALID)
        return 0;

    int nCount = GetCurrentHitPoints (oAvatar);
    int nRating = (nCount <= GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount <= GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_NOT_RECOMMENDED;

    nCount = GetMaxHitPoints (oAvatar) - nCount;
    nRating += (GetMaxHitPoints (oScan) > nCount) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateSacrificeHookHorror (int nPlayer, object oEnemy)
{
    object oScan = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND, nPlayer, OBJECT_SELF);

    if (oScan == OBJECT_INVALID)
        return 0;

    int nCount = GetCurrentHitPoints (oEnemy);
    int nRating = (nCount <= GetPercentHitPoints (25, oEnemy)) ? CARD_AI_WEIGHT_HIGH_IMPACT :
                  (nCount <= GetPercentHitPoints (50, oEnemy)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_NOT_RECOMMENDED;

    nRating += (GetMaxHitPoints (oScan) > nCount) ? CARD_AI_WEIGHT_WINNING_CARD : CARD_AI_WEIGHT_MEDIUM_IMPACT;

    return nRating;
}

int AIEvaluateSacrificeIntellectDevourer (int nPlayer, int nEnemy)
{
    int nDeckO = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nDeckE = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nCountO = GetTotalCards (nDeckO, OBJECT_SELF);
    int nCountE = GetTotalCards (nDeckE, OBJECT_SELF);
    int nRating = (nCountE > nCountO + 2) ? CARD_AI_WEIGHT_LOSING_CARD : 0;

    if (nRating < 0)
        return 0;

    int nStones = GetHasStones (CARD_STONE_SOLAR, nPlayer, OBJECT_SELF) + GetHasStones (CARD_STONE_SOLAR, nEnemy, OBJECT_SELF);

    nRating += (nStones > 5) ? CARD_AI_WEIGHT_HIGH_IMPACT :
               (nStones > 2) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
               (nStones > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

    return nRating;
}

int AIEvaluateSacrificeJysirael (int nPlayer, object oCreature)
{
    int nRating, nCount;
    int nNth = 1;

    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID && GetDistanceBetween (oCreature, oCycle) <= 10.0f)
    {
        if (GetCardID (oCycle) && GetOwner (oCycle) == nPlayer)
        {
            nCount = GetCurrentHitPoints (oCycle);

            nRating += (nCount <= GetPercentHitPoints (25, oCycle)) ? CARD_AI_WEIGHT_WORTHY :
                       (nCount <= GetPercentHitPoints (50, oCycle)) ? CARD_AI_WEIGHT_LOW_IMPACT : CARD_AI_WEIGHT_NEGLIGIBLE;
        }

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return nRating;
}

int AIEvaluateSacrificeKoboldKamikaze (int nEnemy, object oEnemy, object oCreature)
{
    int nScan, nCount, nRating;
    int nFireball = 9;

    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);

    if (oScan == OBJECT_INVALID)
        return 0;

    while (oScan != OBJECT_INVALID)
    {
        if (GetOwner (oScan) == nEnemy)
            if (oScan == oEnemy)
                nRating += (nFireball > GetCurrentHitPoints (oScan)) ? CARD_AI_WEIGHT_WINNING_CARD : CARD_AI_WEIGHT_WORTHY;
            else if (GetCardID (oScan))
                nRating += (nFireball > GetCurrentHitPoints (oScan)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    return nRating;
}

int AIEvaluateSacrificeMaidenOfParadise (int nPlayer)
{
    int nCount = GetHasGenerators (nPlayer, OBJECT_SELF);

    return (nCount > 4) ? CARD_AI_WEIGHT_NEGLIGIBLE :
           (nCount > 2) ? CARD_AI_WEIGHT_LOW_IMPACT :
           (nCount > 0) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_HIGH_IMPACT;
}

int AIEvaluateSacrificeSpiritGuardian (object oAvatar)
{
    int nCount = GetCurrentHitPoints (oAvatar);

    return (nCount < GetPercentHitPoints (70, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : 0;
}

int AIEvaluateSacrificeUmberHulk (int nEnemy)
{
    int nCount = GetHasCreatures (CARD_SCAN_NO_EFFECT, CARD_SPELL_PARALYZE, nEnemy, OBJECT_SELF);

    return (nCount > 3) ? CARD_AI_WEIGHT_WORTHY :
           (nCount > 0) ? CARD_AI_WEIGHT_LOW_IMPACT : 0;
}

int GetAICardEvaluation (struct sCard sInfo, int nMaxHand, int nMaxPower, int nPlayer, object oAvatar, object oCentre)
{
    int nRating = GetAIEvaluation (sInfo, nMaxHand, nMaxPower, nPlayer, oAvatar, oCentre);
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oEnemy = GetAvatar (nEnemy, oCentre);

    switch (sInfo.nCard)
    {
        case CARD_MYTHICAL_JYSIRAEL:            nRating += AIEvaluateCardJysirael (nMaxHand, nPlayer, oCentre);                                 break;
        case CARD_MYTHICAL_SAESHEN:             nRating += AIEvaluateCardSaeshen (nMaxHand, nPlayer);                                           break;
        case CARD_SPELL_ANGELIC_CHOIR:          nRating += AIEvaluateCardAngelicChoir (nMaxHand, nPlayer);                                      break;
        case CARD_SPELL_ARMOUR:                 nRating += AIEvaluateCardArmour (nPlayer, oAvatar);                                             break;
        case CARD_SPELL_ASSASSIN:               nRating += AIEvaluateCardAssassin (nPlayer, nEnemy, oCentre);                                   break;
        case CARD_SPELL_BOOMERANG:              nRating += AIEvaluateCardBoomerang (nEnemy, oCentre);                                           break;
        case CARD_SPELL_COUNTERSPELL:           nRating += AIEvaluateCardCounterspell (nEnemy);                                                 break;
        case CARD_SPELL_DEATH_PACT:             nRating += AIEvaluateCardDeathPact (oAvatar, oCentre);                                          break;
        case CARD_SPELL_DISPEL_MAGIC:           nRating += AIEvaluateCardDispelMagic (nMaxHand, nPlayer, nEnemy, oAvatar, oEnemy, oCentre);     break;
        case CARD_SPELL_ELIXIR_OF_LIFE:         nRating += AIEvaluateCardElixirOfLife (nMaxPower, nPlayer, oAvatar, oCentre);                   break;
        case CARD_SPELL_ENERGY_DISRUPTION:      nRating += AIEvaluateCardEnergyDisruption (nPlayer, nEnemy);                                    break;
        case CARD_SPELL_EYE_OF_THE_BEHOLDER:    nRating += AIEvaluateCardEyeOfTheBeholder (nMaxHand, nPlayer, nEnemy);                          break;
        case CARD_SPELL_FIREBALL:               nRating += AIEvaluateCardFireball (nMaxPower, nEnemy, oEnemy);                                  break;
        case CARD_SPELL_FIRE_SHIELD:            nRating += AIEvaluateCardFireShield (nPlayer, oAvatar);                                         break;
        case CARD_SPELL_FLUX:                   nRating += AIEvaluateCardFlux (nMaxHand, nPlayer, nEnemy, oAvatar, oEnemy);                     break;
        case CARD_SPELL_HEALING_LIGHT:          nRating += AIEvaluateCardHealingLight (nPlayer, oAvatar);                                       break;
        case CARD_SPELL_HIGHER_CALLING:         nRating += AIEvaluateCardHigherCalling (nPlayer, oCentre);                                       break;
        case CARD_SPELL_HOLY_VENGEANCE:         nRating += AIEvaluateCardHolyVengeance (nPlayer);                                               break;
        case CARD_SPELL_LIFE_DRAIN:             nRating += AIEvaluateCardLifeDrain (nEnemy, oAvatar);                                           break;
        case CARD_SPELL_LIGHTNING_BOLT:         nRating += AIEvaluateCardLightningBolt (nEnemy, oEnemy);                                        break;
        case CARD_SPELL_MIND_CONTROL:           nRating += AIEvaluateCardMindControl (nMaxPower, nPlayer, oCentre);                             break;
        case CARD_SPELL_MIND_OVER_MATTER:       nRating += AIEvaluateCardMindOverMatter (nPlayer, oAvatar, oCentre);                            break;
        case CARD_SPELL_PARALYZE:               nRating += AIEvaluateCardParalyze (nPlayer, nEnemy, oCentre);                                   break;
        case CARD_SPELL_POTION_OF_HEROISM:      nRating += AIEvaluateCardPotionOfHeroism (nPlayer);                                             break;
        case CARD_SPELL_RESURRECT:              nRating += AIEvaluateCardResurrect (nMaxPower, nPlayer, oAvatar, oCentre);                      break;
        case CARD_SPELL_SABOTAGE:               nRating += AIEvaluateCardSabotage (nEnemy);                                                     break;
        case CARD_SPELL_SCORCHED_EARTH:         nRating += AIEvaluateCardScorchedEarth (nPlayer, oCentre);                                      break;
        case CARD_SPELL_SIMULACRUM:             nRating += AIEvaluateCardSimulacrum (nPlayer, oCentre);                                         break;
        case CARD_SPELL_VORTEX:                 nRating += AIEvaluateCardVortex (nPlayer);                                                      break;
        case CARD_SPELL_WARP_REALITY:           nRating += AIEvaluateCardWarpReality (nPlayer, oCentre);                                        break;
        case CARD_SPELL_WRATH_OF_THE_HORDE:     nRating += AIEvaluateCardWrathOfTheHorde (nPlayer);                                             break;
        case CARD_STONE_SOLAR:                  nRating += AIEvaluateCardSolarStone (nMaxHand, nPlayer, nEnemy);                                break;
        case CARD_SUMMON_ANGELIC_HEALER:        nRating += AIEvaluateCardAngelicHealer (nMaxHand, nPlayer, oAvatar);                            break;
        case CARD_SUMMON_ANGELIC_LIGHT:         nRating += AIEvaluateCardAngelicLight (nMaxHand, nPlayer);                                      break;
        case CARD_SUMMON_ANGELIC_DEFENDER:
        case CARD_SUMMON_ARCHANGEL:             nRating += AIEvaluateCardArchangel (nMaxHand, nPlayer);                                         break;
        case CARD_SUMMON_ATLANTIAN:             nRating += AIEvaluateCardAtlantian (nMaxPower);                                                 break;
        case CARD_SUMMON_AVENGING_ANGEL:        nRating += AIEvaluateCardAvengingAngel (nMaxHand, nPlayer);                                     break;
        case CARD_SUMMON_BEHOLDER:              nRating += AIEvaluateCardBeholder (nMaxHand);                                                   break;
        case CARD_SUMMON_BULETTE:               nRating += AIEvaluateCardBulette (nEnemy);                                                      break;
        case CARD_SUMMON_BONE_GOLEM:            nRating += AIEvaluateCardBoneGolem (nMaxHand, nPlayer);                                         break;
        case CARD_SUMMON_CHAOS_WITCH:           nRating += AIEvaluateCardChaosWitch (nEnemy, oCentre);                                          break;
        case CARD_SUMMON_DRAGON:                nRating += AIEvaluateCardDragon (nPlayer, oAvatar, oCentre);                                    break;
        case CARD_SUMMON_DRUID:                 nRating += AIEvaluateCardDruid (nPlayer, oCentre);                                              break;
        case CARD_SUMMON_FAIRY_DRAGON:          nRating += AIEvaluateCardFairyDragon (nMaxHand, nPlayer);                                       break;
        case CARD_SUMMON_GOBLIN:
        case CARD_SUMMON_GOBLIN_CROSSBOW:
        case CARD_SUMMON_GOBLIN_SHAMAN:         nRating += AIEvaluateCardGoblin (nMaxHand, nPlayer);                                            break;
        case CARD_SUMMON_GOBLIN_WARLORD:        nRating += AIEvaluateCardGoblinWarlord (nMaxHand, nPlayer);                                     break;
        case CARD_SUMMON_GOBLIN_WITCHDOCTOR:    nRating += AIEvaluateCardGoblinWitchdoctor (nMaxHand, nPlayer, oAvatar);                        break;
        case CARD_SUMMON_INTELLECT_DEVOURER:    nRating += AIEvaluateCardIntellectDevourer (nMaxHand, nPlayer, nEnemy);                         break;
        case CARD_SUMMON_KOBOLD_CHIEF:          nRating += AIEvaluateCardKoboldChief (nMaxHand, nPlayer);                                       break;
        case CARD_SUMMON_KOBOLD_ENGINEER:       nRating += AIEvaluateCardKoboldEngineer (nMaxHand, nPlayer, nEnemy, oCentre);                   break;
        case CARD_MYTHICAL_DEEKIN:
        case CARD_SUMMON_KOBOLD:
        case CARD_SUMMON_KOBOLD_KAMIKAZE:       nRating += AIEvaluateCardKobold (nMaxHand, nPlayer);                                            break;
        case CARD_SUMMON_KOBOLD_POGOSTICK:      nRating += AIEvaluateCardKoboldPogostick (nMaxHand, nPlayer, nEnemy, oEnemy);                   break;
        case CARD_SUMMON_LICH:                  nRating += AIEvaluateCardLich (nPlayer, oCentre);                                               break;
        case CARD_SUMMON_LOREMASTER:            nRating += AIEvaluateCardLoremaster (nPlayer, oCentre);                                         break;
        case CARD_SUMMON_MAIDEN_OF_PARADISE:    nRating += AIEvaluateCardMaidenOfParadise (nMaxHand, nPlayer);                                  break;
        case CARD_SUMMON_PAIN_GOLEM:            nRating += AIEvaluateCardPainGolem (nEnemy, oCentre);                                           break;
        case CARD_SUMMON_PIT_FIEND:             nRating += AIEvaluateCardPitFiend (nPlayer);                                                    break;
        case CARD_SUMMON_BEAR:
        case CARD_SUMMON_COUGAR:
        case CARD_SUMMON_COW:
        case CARD_SUMMON_GIANT_SPIDER:
        case CARD_SUMMON_PHASE_SPIDER:          nRating += AIEvaluateCardBear (nMaxHand, nPlayer);                                              break;
        case CARD_SUMMON_PLAGUE_BEARER:         nRating += AIEvaluateCardPlagueBearer (nMaxHand, nPlayer, oCentre);                             break;
        case CARD_SUMMON_RAT_KING:              nRating += AIEvaluateCardRatKing (nMaxHand, nPlayer);                                           break;
        case CARD_SUMMON_FERAL_RAT:
        case CARD_SUMMON_RAT:
        case CARD_SUMMON_SEA_HAG:               nRating += AIEvaluateCardSeaHag (nEnemy);                                                       break;
        case CARD_SUMMON_SEWER_RAT:             nRating += AIEvaluateCardFeralRat (nMaxHand, nPlayer);                                          break;
        case CARD_SUMMON_SPIRIT_GUARDIAN:       nRating += AIEvaluateCardSpiritGuardian (oAvatar);                                              break;
        case CARD_SUMMON_SKELETAL_ARCHER:
        case CARD_SUMMON_SKELETAL_WARRIOR:      nRating += AIEvaluateCardSkeleton (nMaxHand, nPlayer);                                          break;
        case CARD_SUMMON_TROGLODYTE:            nRating += AIEvaluateCardTroglodyte (nEnemy);                                                   break;
        case CARD_SUMMON_VAMPIRE_MASTER:        nRating += AIEvaluateCardVampireMaster (oAvatar);                                               break;
        case CARD_SUMMON_WHITE_STAG:            nRating += AIEvaluateCardWhiteStag (nMaxHand, nPlayer, oAvatar);                                break;
        case CARD_SUMMON_WHITE_WOLF:
        case CARD_SUMMON_WOLF:                  nRating += AIEvaluateCardWolf (nMaxHand, nPlayer);                                              break;
        case CARD_SUMMON_ZOMBIE:                nRating += AIEvaluateCardZombie (nMaxHand, nPlayer);                                            break;
        case CARD_SUMMON_ZOMBIE_LORD:           nRating += AIEvaluateCardZombieLord (nMaxHand, nPlayer);                                        break;
        default:    nRating += GetAICustomCardEvaluation (sInfo, nMaxHand, nMaxPower, nPlayer, oAvatar, oCentre);                               break;
    }

if (GetIsDebug())
  SendMessageToPC(GetFirstPC(), "Evaluating " + sInfo.sName + " with rating " + IntToString(nRating));

    return nRating;
}

int GetAIEvaluation (struct sCard sInfo, int nMaxHand, int nMaxPower, int nPlayer, object oAvatar, object oCentre)
{
    int nRating, nScan, nScanE, nScanO, nResult;
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    int nHandO = GetCardsInHand (nPlayer, GetReferenceObject (nPlayer, oCentre), 5);
    // Added by Adam - avoid playing spells if energy disruption in play
    // Changed to a penalty instead of a bonus
    if (GetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, OBJECT_SELF) && nHandO)
        nRating += CARD_AI_WEIGHT_LOW_LOSS;
    // Play cards that are "in deck" - excluded by Adam as all cards the AI plays are in deck
    //if (sInfo.nDeck & GetNPCDeckType (GetCardGamePlayer (nPlayer, GetArea (oCentre))))
    //    nRating += CARD_AI_WEIGHT_WORTHY;
    object oEnemy = GetAvatar (nEnemy, oCentre);
    object oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oScan != OBJECT_INVALID)
    {
        int nCard = GetCardID (oScan);

        if (nCard)
        {
            int nOwner = GetOwner (oScan);

            if (sInfo.nType == CARD_TYPE_MYTHICAL && nCard == sInfo.nCard)
                return nRating + CARD_AI_WEIGHT_LOSING_CARD;

            if (nCard == CARD_SUMMON_PAIN_GOLEM && nOwner == nEnemy && nHandO)
                nRating += CARD_AI_WEIGHT_WORTHY;

            if (!GetHasCardEffect (CARD_SPELL_PARALYZE, oScan))
                if (nOwner == nPlayer)
                    nScanO += 1;
                else
                    nScanE += 1;
        }

        oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nScan, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (sInfo.nType == CARD_TYPE_SUMMON || sInfo.nType == CARD_TYPE_MYTHICAL)
    {
        nResult = nScanE - nScanO;
        nRating += (nResult <= 0) ? CARD_AI_WEIGHT_NEGLIGIBLE :
                   (nResult <= 2) ? CARD_AI_WEIGHT_WORTHY :
                   (nResult <= 5) ? CARD_AI_WEIGHT_MEDIUM_IMPACT : CARD_AI_WEIGHT_HIGH_IMPACT;

        nScanO = GetCurrentHitPoints (oAvatar);
        nScanE = GetCurrentHitPoints (oEnemy);

        nRating += (nScanO <= GetPercentHitPoints (25, oAvatar)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                   (nScanO <= GetPercentHitPoints (50, oAvatar)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_NEGLIGIBLE;

        nRating += (nScanE >= GetPercentHitPoints (50, oEnemy)) ? CARD_AI_WEIGHT_MEDIUM_IMPACT :
                   (nScanE >= GetPercentHitPoints (25, oEnemy)) ? CARD_AI_WEIGHT_WORTHY : CARD_AI_WEIGHT_LOW_IMPACT;

        nResult = 1;
        nScanO = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (nResult));

        while (nResult <= nMaxHand)
        {
            if (nScanO == CARD_SPELL_WRATH_OF_THE_HORDE)
                nRating += CARD_AI_WEIGHT_LOW_IMPACT;

            nScanO = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_AI_HAND_STACK_" + IntToString (++nResult));
        }
        nScanO = GetHasGenerators (nPlayer, OBJECT_SELF);
        nScanE = GetHasGenerators (nEnemy, OBJECT_SELF);
        nResult = (nScanO + nScanE) * GetHasCardEffect (CARD_SPELL_FLUX, OBJECT_SELF);

        nRating += (nResult <= 0) ? 0 :
                   (nResult <= 4) ? CARD_AI_WEIGHT_LOW_LOSS :
                   (nResult <= 8) ? CARD_AI_WEIGHT_WORTHY_LOSS :
                   (nResult <= 12) ? CARD_AI_WEIGHT_MEDIUM_LOSS : CARD_AI_WEIGHT_HIGH_LOSS;

        if (sInfo.nSacrifice)
            nRating += GetAISacrificeEvaluation (sInfo, nMaxPower, nPlayer, oAvatar, oAvatar);


        // added by Adam as "tiebreaker" for different creatures
        nRating += sInfo.nMagic;

    }
    else if (sInfo.nType == CARD_TYPE_SPELL)
    {
        if (GetHasCardEffect (CARD_SPELL_COUNTERSPELL, oAvatar) && sInfo.nCard != CARD_SPELL_COUNTERSPELL)
            nRating += CARD_AI_WEIGHT_WORTHY_LOSS;

    }
    return nRating;
}

int GetAISacrificeEvaluation (struct sCard sInfo, int nMaxPower, int nPlayer, object oAvatar, object oCreature)
{
    int nRating;
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oEnemy = GetAvatar (nEnemy, oCreature);

    switch (sInfo.nCard)
    {
        case CARD_MYTHICAL_DEEKIN:                  nRating += AIEvaluateSacrificeDeekin (nPlayer, nEnemy, oEnemy);             break;
        case CARD_MYTHICAL_JYSIRAEL:                nRating += AIEvaluateSacrificeJysirael (nPlayer, oCreature);                break;
        case CARD_SUMMON_COUGAR:                    nRating += AIEvaluateSacrificeCougar (nEnemy, oCreature);                   break;
        case CARD_SUMMON_COW:                       nRating += AIEvaluateSacrificeCow (oAvatar);                                break;
        case CARD_MYTHICAL_ARKNETH:
        case CARD_SUMMON_DEMON_KNIGHT:              nRating += AIEvaluateSacrificeDemonKnight (nEnemy, oCreature);              break;
        case CARD_SUMMON_FAIRY_DRAGON:              nRating += AIEvaluateSacrificeFairyDragon (nPlayer, oCreature);             break;
        case CARD_SUMMON_GOBLIN_WITCHDOCTOR:        nRating += AIEvaluateSacrificeGoblinWitchdoctor (nPlayer, oAvatar);         break;
        case CARD_SUMMON_HOOK_HORROR:               nRating += AIEvaluateSacrificeHookHorror (nPlayer, oEnemy);                 break;
        case CARD_SUMMON_INTELLECT_DEVOURER:        nRating += AIEvaluateSacrificeIntellectDevourer (nPlayer, nEnemy);          break;
        case CARD_SUMMON_KOBOLD_KAMIKAZE:           nRating += AIEvaluateSacrificeKoboldKamikaze (nEnemy, oEnemy, oCreature);   break;
        case CARD_SUMMON_MAIDEN_OF_PARADISE:        nRating += AIEvaluateSacrificeMaidenOfParadise (nPlayer);                   break;
        case CARD_SUMMON_SHADOW_ASSASSIN:           nRating += AIEvaluateCardAssassin (nPlayer, nEnemy, oCreature);             break;
        case CARD_SUMMON_SPIRIT_GUARDIAN:           nRating += AIEvaluateSacrificeSpiritGuardian (oAvatar);                     break;
        case CARD_SUMMON_UMBER_HULK:                nRating += AIEvaluateSacrificeUmberHulk (nEnemy);                           break;
    }

    return nRating;
}
