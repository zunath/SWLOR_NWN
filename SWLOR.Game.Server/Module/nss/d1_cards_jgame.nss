/**********************************/
/*          d1_cards_jgame
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Main Game Mechanics
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

void ActionAddPowerToPool (int nPower, int nPlayer, object oArea, int nBroadcast = TRUE)
{
    if (!nPower)
        return;

    SetMagicPool (GetMagicPool (nPlayer, oArea) + nPower, nPlayer, oArea);

    ActionUpdateMagicPool (nPlayer, oArea);

    if (nBroadcast)
        SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Magic Added To Pool: " + IntToString (nPower));
}

void ActionBeginGame (object oPlayer1, object oPlayer2, object oArea)
{
    int nPlayer;

    object oCentre = GetGameCentre (oArea);

    vector vStart = GetPosition (oCentre);
    vector vAvatar, vPCStart, vConcede, vGameInfo;

    float fSeperate = CARD_PLAY_DISTANCE;
    float fFacing;

    for (nPlayer = 1; nPlayer <= 2; nPlayer++)
    {
        object oPlayer = (nPlayer == 1) ? oPlayer1 : oPlayer2;

        if (nPlayer == 1)
        {
            vAvatar = Vector (vStart.x, vStart.y + fSeperate, vStart.z);
            vPCStart = Vector (vAvatar.x, vAvatar.y + 5.0f, vAvatar.z);
            vConcede = Vector (vPCStart.x + 13.0f, vPCStart.y, vPCStart.z);
            vGameInfo = Vector (vPCStart.x + 11.0f, vPCStart.y, vPCStart.z);
            fFacing = 270.0f;
        }
        else
        {
            vAvatar = Vector (vStart.x, vStart.y - fSeperate, vStart.z);
            vPCStart = Vector (vAvatar.x, vAvatar.y - 5.0f, vAvatar.z);
            vConcede = Vector (vPCStart.x - 13.0f, vPCStart.y, vPCStart.z);
            vGameInfo = Vector (vPCStart.x - 11.0f, vPCStart.y, vPCStart.z);
            fFacing = 90.0;
        }

        ActionSpawnAvatar (Location (oArea, vAvatar, fFacing), nPlayer, oPlayer);

        CreateObject (OBJECT_TYPE_PLACEABLE, "concedegame", Location (oArea, vConcede, fFacing + 90.0f), FALSE, "CONCEDE_" + IntToString (nPlayer));
        CreateObject (OBJECT_TYPE_PLACEABLE, "gamepedestal", Location (oArea, vGameInfo, fFacing + 90.0f), FALSE, "GAMEINFO_" + IntToString (nPlayer));


        effect ePlayer = EffectLinkEffects (EffectVisualEffect (VFX_DUR_CUTSCENE_INVISIBILITY), EffectInvisibility (INVISIBILITY_TYPE_IMPROVED));
        ePlayer = EffectLinkEffects (ePlayer, EffectHaste());
        ePlayer = EffectLinkEffects (ePlayer, EffectSkillIncrease(FEAT_SKILL_FOCUS_SPELLCRAFT, 20));
        ePlayer = EffectLinkEffects (ePlayer, EffectDamageResistance(DAMAGE_TYPE_FIRE, 200));

        if (GetIsPC (oPlayer))
        {
            AssignCommand (oPlayer, ClearAllActions());
            AssignCommand (oPlayer, JumpToLocation (Location (oArea, vPCStart, fFacing)));

            ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (ePlayer), oPlayer);
            //-- 10-30-04: bloodsong
            //-- make player PLOT to avoid accidental death.
            SetPlotFlag(oPlayer, TRUE);
        }
    }

    DelayCommand (RoundsToSeconds (1), ExecuteScript ("d_card_turn", oArea));
}

void ActionChangeMusic (int nChangeType, object oArea = OBJECT_SELF)
{
    int nNewTrack;

    switch (nChangeType)
    {
        case CARD_GAME_MUSIC_END_MP:
            MusicBattleStop (oArea);
            break;

        case CARD_GAME_MUSIC_END_SP_LOSE:
            MusicBattleStop (oArea);

            MusicBackgroundChangeDay (oArea, TRACK_EVILDUNGEON1);
            MusicBackgroundChangeNight (oArea, TRACK_EVILDUNGEON1);
            MusicBackgroundPlay (oArea);

            DelayCommand (10.0, MusicBackgroundStop (oArea));

            break;

        case CARD_GAME_MUSIC_END_SP_WIN:
            MusicBattleStop (oArea);

            MusicBackgroundChangeDay (oArea, TRACK_THEME_NWN);
            MusicBackgroundChangeNight (oArea, TRACK_THEME_NWN);
            MusicBackgroundPlay (oArea);

            DelayCommand (10.0, MusicBackgroundStop (oArea));

            break;

        case CARD_GAME_MUSIC_START:
            switch (Random(14) + 1)
            {
                case 1: nNewTrack = TRACK_BATTLE_DRAGON;        break;
                case 2: nNewTrack = TRACK_BATTLE_ARIBETH;       break;
                case 3: nNewTrack = TRACK_BATTLE_ENDBOSS;       break;
                case 4: nNewTrack = TRACK_BATTLE_FORESTBOSS;    break;
                case 5: nNewTrack = TRACK_BATTLE_LIZARDBOSS;    break;
                case 6: nNewTrack = TRACK_BATTLE_CITYBOSS;      break;
                case 7: nNewTrack = TRACK_BATTLE_DUNGEON1;      break;
                case 8: nNewTrack = TRACK_BATTLE_DUNGEON2;      break;
                case 9: nNewTrack = TRACK_BATTLE_DUNGEON3;      break;
                case 10: nNewTrack = TRACK_BATTLE_FOREST1;      break;
                case 11: nNewTrack = TRACK_BATTLE_FOREST2;      break;
                case 12: nNewTrack = TRACK_BATTLE_CITY1;      break;
                case 13: nNewTrack = TRACK_BATTLE_CITY2;      break;
                case 14: nNewTrack = TRACK_BATTLE_CITY3;      break;
            }

            MusicBattleChange (oArea, nNewTrack);
            MusicBattlePlay (oArea);

            break;
    }
}

void ActionDestroyCard (int nCard, int nPlayer, object oArea, int nNth = 1)
{
    int nCount = 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oCards = GetNearestObject (OBJECT_TYPE_PLACEABLE, oCentre, nCount);

    while (oCards != OBJECT_INVALID && nNth > 0)
    {
        if (GetCardID (oCards) == nCard && GetOwner (oCards) == nPlayer)
        {
            SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), GetName (oCards) + " in your hand has been destroyed.");

            SetPlotFlag (oCards, FALSE);
            DestroyObject (oCards);
            DestroyObject (oCards, 0.1);

            nNth -= 1;
        }

        oCards = GetNearestObject (OBJECT_TYPE_PLACEABLE, oCentre, ++nCount);
    }
}

void ActionDestroyPower (int nAmount, int nPlayer, object oArea)
{
    int nNth = 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

    while (oGenerator != OBJECT_INVALID && (nAmount > 0 || nAmount == -1))
    {
        if (GetOwner (oGenerator) == nPlayer)
        {
            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectLinkEffects (EffectVisualEffect (VFX_FNF_GAS_EXPLOSION_FIRE), EffectVisualEffect (VFX_FNF_SUMMON_MONSTER_1)), oGenerator);

            if (nAmount != -1)
                SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "One of your magic generators has been destroyed.");

            SetPlotFlag (oGenerator, FALSE);
            DestroyObject (oGenerator);
            SendToDiscardPile (CARD_GENERATOR_GENERIC, nPlayer, oArea);

            if (nAmount != -1)
                nAmount -= 1;
        }

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
    }
}

void ActionDiscardPileToHand (int nCard, int nPlayer, object oArea)
{
    AssignCommand (oArea, ActionDrawSpecificCard (nCard, nPlayer, oArea, FALSE));

    RemoveFromDiscardPile (nCard, nPlayer, oArea);

    struct sCard sInfo = GetCardInfo (nCard);

    SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), sInfo.sName + " returned from discard pile to hand.");
}

void ActionDrawCards (int nPlayer, int nTurn = FALSE)
{
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre());
    object oPlayer = GetCardGamePlayer (nPlayer, OBJECT_SELF);

    int nCurrent, nNth, nDelete, nHand;
    int nDeck = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nLoremaster = GetLoremasterScan (nPlayer, oCentre);
    int nPowerStream = GetHasCardEffect (CARD_SPELL_POWER_STREAM, OBJECT_SELF);
    if(nPowerStream > 0)
    {
      string sMessage = "Power Stream causes all players to draw " + IntToString(nPowerStream) + "extra card";
      if(nPowerStream > 1)
      {
        sMessage = sMessage + "s.";
      }else{
        sMessage = sMessage + ".";
      }
      SendMessageToPC (oPlayer, sMessage);
    }
    int nMax = GetDrawMaximum (OBJECT_SELF);
    int nDrawNth = (nTurn <= 1) ? nMax + nLoremaster + nPowerStream: 1 + nLoremaster + nPowerStream;
    int nCard = GetCardsInHand (nPlayer, oCentre, ++nCurrent);
    int nIgnore = TRUE;

    while (nCard)
    {
        if (nCard != CARD_GENERATOR_GENERIC)
            nIgnore = FALSE;

        SetLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nCurrent), nCard);

        nCard = GetCardsInHand (nPlayer, oCentre, ++nCurrent);
    }

    nHand = nCurrent - 1;

    while (--nCurrent >= nMax)
    {
        do
        {
            nCard = GetLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (++nDelete));

            if (nCard && (nCard != CARD_GENERATOR_GENERIC || nIgnore))
            {
                struct sCard sDiscard = GetCardInfo (nCard);

                SendMessageToPC (oPlayer, "Discarding: " + sDiscard.sName);

                if (!sDiscard.nHandDiscard)
                    SendToDiscardPile (sDiscard.nCard, nPlayer, OBJECT_SELF);

                DeleteLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nDelete));

                while (++nDelete <= nHand)
                    SetLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nDelete - 1), GetLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nDelete)));

                nHand -= 1;
                nDelete = 0;

                break;
            }

        } while (nCard);
    }

    int nMaxCards = GetTotalCards (nDeck, OBJECT_SELF);
    int nPower = GetHasGenerators (nPlayer, OBJECT_SELF, 1);

    while (--nDrawNth >= 0)
    {
        int nDraw = GetDrawnCard (nPlayer, nMaxCards, OBJECT_SELF);

        if (!nPower)
        {
            if (GetLocalInt (OBJECT_SELF, "CARDS_GAME_DECK_" + IntToString (nPlayer) + "_" + IntToString (CARD_GENERATOR_GENERIC)))
                nDraw = CARD_GENERATOR_GENERIC;

            nPower = TRUE;
        }

        if (nDraw > 0 && nDraw < CARD_MAX_ID)
        {
            ActionTransferCard (nDraw, nDeck, CARD_SOURCE_ALL_CARDS, OBJECT_SELF, OBJECT_INVALID);

            struct sCard sDrawn = GetCardInfo (nDraw);

            SendMessageToPC (oPlayer, "Draw: " + sDrawn.sName);

            SetLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (++nHand), nDraw);

            nMaxCards -= 1;
        }
        else
        {
            if (GetIsVariantInPlay (CARD_RULE_LAST_DRAW_CONTINUE, OBJECT_SELF))
            {
                SendMessageToPC (oPlayer, "You have run out of cards in your deck.");
                break;
            }
            else
            {
                object oAvatar = GetAvatar (nPlayer, oCentre);

                SetLocalInt (OBJECT_SELF, "CARD_AVATAR_ALT_DEATH", CARD_GAME_END_DRAW);

                AssignCommand (oAvatar, SetPlotFlag (oAvatar, FALSE));
                AssignCommand (oAvatar, ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (GetMaxHitPoints (oAvatar) + 10, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oAvatar));

                return;
            }
        }
    }

    SendMessageToPC (oPlayer, "Cards Remaining: " + IntToString (GetTotalCards (nDeck, OBJECT_SELF)));

    ActionLayoutHand (nHand, nPlayer, oCentre);

    if (nTurn >= 1)
    {
        int nEnergy = GetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, OBJECT_SELF);

        if (nEnergy > 0)
        {
            int nDifference = nMax - nHand;
            int nDisrupt = (nEnergy - 1) + nDifference;

            ActionUsePower (nDisrupt, nPlayer, OBJECT_SELF);

            SendMessageToPC (oPlayer, "Energy Disruption drains " + IntToString (nDisrupt) + " power for having " + IntToString (nDifference) + " cards below " + IntToString (nMax) + ".");
        }
    }
}

void ActionDrawSpecificCard (int nCardID, int nPlayer, object oArea, int nNaturalDraw = TRUE)
{
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));

    int nCurrent, nNth, nHand;
    int nDeck = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nCard = GetCardsInHand (nPlayer, oCentre, ++nCurrent);

    if (!nNaturalDraw)
        nDeck = CARD_SOURCE_ALL_CARDS;

    while (nCard)
    {
        SetLocalInt (oArea, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nCurrent), nCard);

        nCard = GetCardsInHand (nPlayer, oCentre, ++nCurrent);
    }

    nHand = nCurrent - 1;

    if (nCardID > 0 && nCardID < CARD_MAX_ID)
    {
        ActionTransferCard (nCardID, nDeck, CARD_SOURCE_ALL_CARDS, oArea, OBJECT_INVALID);

        struct sCard sDrawn = GetCardInfo (nCardID);

        if (nNaturalDraw)
            SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Draw: " + sDrawn.sName);

        SetLocalInt (oArea, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (++nHand), nCardID);
    }

    ActionLayoutHand (nHand, nPlayer, oCentre);
}

void ActionEndCardGame (int nEndGame, int nPlayer, object oArea)
{
    string sMessage;

    int nOpponent = (nPlayer == 1) ? 2 : 1;
    int nWinner;

    switch (nEndGame)
    {
        case CARD_GAME_END_CHEAT_ATTACK:
            sMessage = GetName (GetCardGamePlayer (nPlayer, oArea)) + " has cheated by attacking a summoned creature.  " + GetName (GetCardGamePlayer (nOpponent, oArea)) + " wins the game.";
            nWinner = nOpponent;
            break;

        case CARD_GAME_END_CHEAT_SPELL:
            sMessage = GetName (GetCardGamePlayer (nPlayer, oArea)) + " has cheated by casting a spell at a summoned creature.  " + GetName (GetCardGamePlayer (nOpponent, oArea)) + " wins the game.";
            nWinner = nOpponent;
            break;

        case CARD_GAME_END_CONCEDE:
            sMessage = GetName (GetCardGamePlayer (nPlayer, oArea)) + " has conceded.  " + GetName (GetCardGamePlayer (nOpponent, oArea)) + " wins the game.";
            nWinner = nOpponent;
            break;

        case CARD_GAME_END_DRAW:
            sMessage = GetName (GetCardGamePlayer (nPlayer, oArea)) + " has run out of cards.  " + GetName (GetCardGamePlayer (nOpponent, oArea)) + " wins the game.";
            nWinner = nOpponent;
            break;

        case CARD_GAME_END_RESULT_DRAW:
            sMessage = "The game has ended as a draw.";
            nWinner = FALSE;
            break;

        case CARD_GAME_END_RESULT_WIN:
            sMessage = GetName (GetCardGamePlayer (nPlayer, oArea)) + " wins the game.";
            nWinner = nPlayer;
            break;

        case CARD_GAME_END_RESULT_LOSE:
            sMessage = GetName (GetCardGamePlayer (nOpponent, oArea)) + " wins the game.";
            nWinner = nOpponent;
            break;
    }

    SendMessageToCardPlayers (sMessage, oArea);

    int nLoser = (nWinner == 1) ? 2 : (nWinner == 2) ? 1 : FALSE;

    object oWinner = GetCardGamePlayer (nWinner, oArea);
    object oLoser = GetCardGamePlayer (nLoser, oArea);

    int nMusic = (oWinner != OBJECT_INVALID && !GetIsPC (oWinner)) ? CARD_GAME_MUSIC_END_SP_LOSE :
                 (oWinner != OBJECT_INVALID && !GetIsPC (oLoser)) ? CARD_GAME_MUSIC_END_SP_WIN : CARD_GAME_MUSIC_END_MP;

    ActionChangeMusic (nMusic, oArea);

    if (nWinner && (GetIsVariantInPlay (CARD_RULE_TRADE_ONE, oArea) || GetIsVariantInPlay (CARD_RULE_TRADE_ALL, oArea)))
    {
        struct sCard sInfo;

        int nAnte, nCard;
        int nRemove = (nWinner == 1) ? CARD_SOURCE_ANTE_PLAYER_2 : (nWinner == 2) ? CARD_SOURCE_ANTE_PLAYER_1 : FALSE;
        int nGain = (!GetIsPC (GetCardGamePlayer (nWinner, oArea))) ? CARD_SOURCE_ALL_CARDS : CARD_SOURCE_COLLECTION;

        string sWinner = GetName (oWinner);

        for (nCard = 1; nCard <= CARD_MAX_ID; nCard++)
        {
            nAnte = GetCardsForAnte (nCard, nLoser, OBJECT_SELF);

            if (nAnte)
            {
                sInfo = GetCardInfo (nCard);

                SendMessageToCardPlayers (sWinner + " wins " + IntToString (nAnte) + " " + sInfo.sName + " for ante.", oArea);

                // give card
                ActionTransferCard (nCard, nRemove, nGain, oArea, oWinner, nAnte);


                // remove the physical card for PCs
                if (GetIsPC (oLoser))
                {
                  object oDeck = GetLocalObject(oLoser, "DECK_SELECTED");
                  if(GetIsDebug())
                  {
                    sInfo = GetCardInfo (nCard);
                    SendMessageToPC(GetFirstPC(), "Removing " + sInfo.sName + " from " + GetName(oDeck));
                  }
                  ActionRemoveCards (oDeck, nCard, 1, FALSE, oLoser);
                }
            }
        }
    }

    for (nPlayer = 1; nPlayer <= 2; nPlayer++)
    {
        object oPlayer = GetCardGamePlayer (nPlayer, oArea);

        nEndGame = (nPlayer == nWinner) ? CARD_GAME_END_RESULT_WIN :
                   (nPlayer == nLoser) ? CARD_GAME_END_RESULT_LOSE :
                    CARD_GAME_END_RESULT_DRAW;

        if (GetIsPC (oPlayer))
        {
            int nOpponent = (nPlayer == 1) ? 2 : 1;

            AdjustPlayerResults (1, nEndGame, GetCardGamePlayer (nOpponent, oArea), oPlayer);

            AssignCommand (oPlayer, DelayCommand (CYCLE_TIME - 2.2f, ClearAllActions()));
            AssignCommand (oPlayer, DelayCommand (CYCLE_TIME - 2.1f, JumpToLocation (GetReturnLocation (oPlayer))));
            AssignCommand (oPlayer, DelayCommand (CYCLE_TIME - 1.5f, ClearAllActions()));

            DelayCommand (CYCLE_TIME - 2.0f, RemoveEffectByType (EFFECT_TYPE_VISUALEFFECT, oPlayer));
            DelayCommand (CYCLE_TIME - 2.0f, RemoveEffectByType (EFFECT_TYPE_HASTE, oPlayer));
            DelayCommand (CYCLE_TIME - 2.0f, RemoveEffectByType (EFFECT_TYPE_DAMAGE_RESISTANCE, oPlayer));
            DelayCommand (CYCLE_TIME - 2.0f, RemoveEffectByType (EFFECT_TYPE_INVISIBILITY, oPlayer));
            //-- 10-30-04: bloodsong
            //-- make player PLOT to avoid accidental death / now taken off
            SetPlotFlag(oPlayer, FALSE);
        }

        if (GetNPCGoldBet (oPlayer) && oWinner != OBJECT_INVALID)
        {
            if (nPlayer == nWinner)
                AssignCommand (oWinner, TakeGoldFromCreature (GetNPCGoldBet (oPlayer), oLoser, TRUE));
            else
                GiveGoldToCreature (oWinner, GetNPCGoldBet (oPlayer));
        }
    }

    ClearGameArea();

    DelayCommand (CYCLE_TIME - 1.0f, ResetGameVariables());
}

void ActionEndPrep (int nEnding)
{
    if (GetLocalInt (OBJECT_SELF, "ENDING_GAME"))
        return;

    object oCentre = GetGameCentre();
    object oPlayer1 = GetAvatar (1, oCentre);
    object oPlayer2 = GetAvatar (2, oCentre);

    if (!nEnding)
        nEnding = CARD_GAME_END_RESULT_WIN;

    if (oPlayer1 == OBJECT_INVALID || oPlayer2 == OBJECT_INVALID)
    {
        SetLocalInt (OBJECT_SELF, "ENDING_GAME", TRUE);

        if (oPlayer1 == OBJECT_INVALID && oPlayer2 == OBJECT_INVALID)
            ActionEndCardGame (CARD_GAME_END_RESULT_DRAW, 0, OBJECT_SELF);
        else if (oPlayer1 == OBJECT_INVALID)
            ActionEndCardGame (nEnding, 2, OBJECT_SELF);
        else
            ActionEndCardGame (nEnding, 1, OBJECT_SELF);
    }
}

void ActionLayoutHand (int nHandSize, int nPlayer, object oCentre)
{
    ClearAllCards (oCentre, nPlayer);

    int nNth, nCard;

    float fX = (nPlayer == 1) ? 8.0f : -8.0f;
    float fY = (nPlayer == 1) ? 8.0f : -8.0f;
    float fMove = (nPlayer == 1) ? -CARD_SIZE_X : CARD_SIZE_X;
    float fFacing = (nPlayer == 1) ? 90.0f : 270.0f;

    vector vStart = GetPosition (GetAvatar (nPlayer, oCentre));
    vector vCards = Vector (vStart.x + fX, vStart.y + fY, vStart.z);

    for (nNth = 1; nNth <= nHandSize; nNth++)
    {
        nCard = GetLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nNth));

        object oCard = CreateObject (OBJECT_TYPE_PLACEABLE, GetCardTag (nCard), Location (OBJECT_SELF, Vector (vCards.x + (fMove * IntToFloat (nNth)), vCards.y, vCards.z + 0.05), fFacing));

        SetOwner (nPlayer, oCard);

        DeleteLocalInt (OBJECT_SELF, "CARD_HAND_PLAYER_" + IntToString (nPlayer) + IntToString (nNth));
    }
}

void ActionPlayCard (object oCard)
{
    if (GetLocalInt (oCard, "CARD_PLAYED_BLOCK"))
        return;

    object oArea = GetArea (oCard);

    string sTag = GetTag (oArea);

    if (sTag == "CardGame")
    {
        struct sCard sInfo = GetCardInfo (GetCardID (oCard));

        int nPlayer = GetOwner (oCard);

        object oPlayer = GetCardGamePlayer (nPlayer, oArea);

        switch (sInfo.nType)
        {
            case CARD_TYPE_GENERATOR:
                if (!GetHasPlayedGenerator (nPlayer, oArea))
                {
                    if (!GetHasGenerators (nPlayer, oArea, GetGeneratorMaximum (oArea)))
                    {
                        SendMessageToCardPlayers (GetName (oPlayer) + " plays " + sInfo.sName, oArea);

                        SetHasPlayedGenerator (nPlayer, TRUE, oArea);

                        ActionUseGenerator (sInfo.nCard, nPlayer, oArea);

                        SetPlotFlag (oCard, FALSE);
                        DestroyObject (oCard);
                        SetLocalInt (oCard, "CARD_PLAYED_BLOCK", TRUE);
                    }
                    else
                        SendMessageToPC (oPlayer, "You have reached the limit of generators allowed.");
                }
                else
                    SendMessageToPC (oPlayer, "You may not place more than one generator per turn.");

                break;

            default:
                if (GetIsPowerAvailable (nPlayer, oArea, sInfo.nMagic))
                {
                    if (sInfo.nType == CARD_TYPE_MYTHICAL && (GetHasCreatures (CARD_SCAN_CARD_ID, sInfo.nCard, 1, oArea, 1, CARD_SCAN_IS_MYTHICAL, FALSE) || GetHasCreatures (CARD_SCAN_CARD_ID, sInfo.nCard, 2, oArea, 1, CARD_SCAN_IS_MYTHICAL, FALSE)))
                    {
                        SendMessageToPC (oPlayer, "This is a Mythical card.  Only one of these may be in play at any one time.");
                        break;
                    }

                    SendMessageToCardPlayers (GetName (oPlayer) + " plays " + sInfo.sName, oArea);

                    ActionUsePower (sInfo.nMagic, nPlayer, oArea);

                    switch (sInfo.nType)
                    {
                        case CARD_TYPE_SPELL:
                            ActionUseSpell (sInfo.nCard, nPlayer, oArea);
                            SendToDiscardPile (sInfo.nCard, nPlayer, oArea);

                            break;

                        case CARD_TYPE_STONE:
                            ActionUseStone (sInfo.nCard, nPlayer, oArea);

                            break;

                        case CARD_TYPE_MYTHICAL:
                        case CARD_TYPE_SUMMON:
                            ActionUseSummon (sInfo.nCard, nPlayer, oArea);

                            break;
                    }

                    SetPlotFlag (oCard, FALSE);
                    DestroyObject (oCard);
                    SetLocalInt (oCard, "CARD_PLAYED_BLOCK", TRUE);

                    if (sInfo.nSacrifice && GetIsPC (oPlayer))
                    {
                        // changed by Adam - effect goes on pedestal
                        object oInfo = GetNearestObjectByTag("GAMEINFO_" + IntToString(nPlayer), oPlayer);
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEAD_EVIL), oInfo);
                        SendMessageToPC (oPlayer, "You have cast a creature that can be sacrificed.");
                    }
                }
                else
                    SendMessageToPC (oPlayer, "You do not have enough power to play this card.");

                break;
        }

        if (!IsInConversation (oPlayer))
        {
          // Added by Adam - toggle on and off
          if(GetLocalInt(oPlayer, "GAMEINFO") == 1)
          {
            AssignCommand (oPlayer, DelayCommand (0.1, ClearAllActions()));
            AssignCommand (oPlayer, DelayCommand (0.2, ActionStartConversation (oPlayer, "d_gamedeckman", TRUE, FALSE)));
          }
        }
    }
    else
    {
        SetPlotFlag (oCard, FALSE);
        DestroyObject (oCard);
        SetLocalInt (oCard, "CARD_PLAYED_BLOCK", TRUE);
    }
}

void ActionSpawnAvatar (location lLoc, int nPlayer, object oPlayer)
{
    object oAvatar = CreateObject (OBJECT_TYPE_CREATURE, "avatar", lLoc, FALSE, "GAME_AVATAR_" + IntToString (nPlayer));

    AssignCommand (oAvatar, SetFacing (GetFacingFromLocation (lLoc)));

    if (!GetIsPC (oPlayer))
        SetCreatureAppearanceType (oAvatar, GetAppearanceType (oPlayer));

    SetOriginalOwner (nPlayer, oAvatar);
    DelayCommand (0.2, SetOwner (nPlayer, oAvatar));

    AssignCommand (oAvatar, DelayCommand (0.2, ActionDoCommand (ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (EffectCutsceneGhost(), EffectCutsceneParalyze())), oAvatar))));
}

void ActionStartCardGame (object oPlayer1, object oPlayer2, int nVariants = CARD_RULE_NORMAL)
{

    object oArea = GetCardsArea();

    if (oArea == OBJECT_INVALID)
    {
        string sWait = "All game areas are occupied.  You must wait.";

        if (GetIsPC (oPlayer1))
            SendMessageToPC (oPlayer1, sWait);

        if (GetIsPC (oPlayer2))
            SendMessageToPC (oPlayer2, sWait);

        return;
    }

    // mark area as explored
    ExploreAreaForPlayer(oArea, oPlayer1);
    ExploreAreaForPlayer(oArea, oPlayer2);

    ActionChangeMusic (CARD_GAME_MUSIC_START, oArea);
    ClearGameArea (oArea);

    SetVariantRules (nVariants, oArea);

    int nPlayer, nAbort;

    object oDeck1, oDeck2, oAss1, oAss2;

    if (GetIsPC (oPlayer1))
    {
        oDeck1 = GetDeck (oPlayer1, 1, oArea);

        if (oDeck1 == OBJECT_INVALID)
        {
            if (GetIsPC (oPlayer1))
                SendMessageToPC (oPlayer1, GetLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE"));

            if (GetIsPC (oPlayer2))
                SendMessageToPC (oPlayer2, GetName (oPlayer1) + "'s deck was invalid.  The game will not commence.");

            nAbort = TRUE;
        }

        if (GetHasAssociates (oPlayer1))
        {

            if (GetIsPC (oPlayer1))
            {
                AssignCommand (oPlayer1, SpeakString("You're not allowed to play if you have associates."));
                SendMessageToPC (oPlayer1, "You're not allowed to play if you have associates.");
            }

            if (GetIsPC (oPlayer2))
                SendMessageToPC (oPlayer2, GetName (oPlayer1) + " has associates in play, and cannot enter the game.");

            nAbort = TRUE;
        }
    }

    if (GetIsPC (oPlayer2))
    {
        oDeck2 = GetDeck (oPlayer2, 1, oArea);

        if (oDeck2 == OBJECT_INVALID)
        {
            if (GetIsPC (oPlayer2))
                SendMessageToPC (oPlayer2, GetLocalString (OBJECT_SELF, "CARD_DECK_INVALID_MESSAGE"));

            if (GetIsPC (oPlayer1))
                SendMessageToPC (oPlayer1, GetName (oPlayer2) + "'s deck was invalid.  The game will not commence.");

            nAbort = TRUE;
        }

        if (GetHasAssociates (oPlayer2))
        {
            if (GetIsPC (oPlayer2))
            {
                AssignCommand (oPlayer2, SpeakString("You're not allowed to play if you have associates."));
                SendMessageToPC (oPlayer2, "You're not allowed to play if you have associates.");
            }

            if (GetIsPC (oPlayer1))
                SendMessageToPC (oPlayer1, GetName (oPlayer2) + " has associates in play, and cannot enter the game.");

            nAbort = TRUE;
        }
    }

    if (nAbort)
        return;

    for (nPlayer = 1; nPlayer <= 2; nPlayer++)
    {
        object oPlayer = (nPlayer == 1) ? oPlayer1 : oPlayer2;
        object oDeck = (nPlayer == 1) ? oDeck1 : oDeck2;

        int nAnte;
        int nDestination = (nPlayer == 1) ? CARD_SOURCE_ANTE_PLAYER_1 : CARD_SOURCE_ANTE_PLAYER_2;

        if (GetIsPC (oPlayer))
            SetReturnLocation (GetLocation (oPlayer), oPlayer);

        SetCardGamePlayer (oPlayer, nPlayer, oArea);
        SetCardGameDeck (nPlayer, oDeck, oArea);

        if (GetIsVariantInPlay (CARD_RULE_TRADE_ONE, oArea))
        {
            int nSource = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;

            nAnte = GetDrawnCard (nPlayer, GetTotalCards (nSource, oArea), oArea);

            ActionTransferCard (nAnte, nSource, nDestination, oArea, oArea);

            if (oDeck != OBJECT_INVALID)
                SetDeckForAnte (nPlayer, oDeck, oArea);

            SendMessageToCardPlayers (GetName (GetCardGamePlayer (nPlayer, oArea)) + " submits card for ante.", oArea);
        }
        else if (GetIsVariantInPlay (CARD_RULE_TRADE_ALL, oArea))
        {
            for (nAbort = 1; nAbort <= CARD_MAX_ID; nAbort++)
            {
                nAnte = GetLocalInt (oArea, "CARDS_GAME_DECK_" + IntToString (nPlayer) + "_" + IntToString (nAbort));

                if (nAnte)
                    ActionTransferCard (nAbort, CARD_SOURCE_ALL_CARDS, nDestination, OBJECT_INVALID, oArea, nAnte);
            }

            if (oDeck != OBJECT_INVALID)
                SetDeckForAnte (nPlayer, oDeck, oArea);

            SendMessageToCardPlayers (GetName (GetCardGamePlayer (nPlayer, oArea)) + " submits entire deck for ante.", oArea);
        }
    }

    ActionBeginGame (oPlayer1, oPlayer2, oArea);

    SetCardGameToggle (TRUE, oArea);
}

void ActionUpdateAvatarHealth (object oAvatar = OBJECT_SELF)
{
    location lLoc = GetLocation (oAvatar);

    int nPlayer = GetOriginalOwner (oAvatar);
    int nCurrent = GetCurrentHitPoints (oAvatar);

    FloatingTextStringOnCreature (IntToString (nCurrent) + " hp", GetCardGamePlayer (nPlayer, GetArea (oAvatar)), TRUE);

    object oLight = GetNearestObjectByTag ("AVATAR_LIGHT_" + IntToString (nPlayer), oAvatar);

    string sResRef = GetLocalString (oLight, "AVATAR_LIGHT_TYPE");
    string sType = (nCurrent <= GetPercentHitPoints (33, oAvatar)) ? "Red" :
                   (nCurrent <= GetPercentHitPoints (66, oAvatar)) ? "Yellow" : "Blue";

    if (oLight != OBJECT_INVALID)
        if (sResRef != sType)
        {
            AssignCommand (oLight, SetPlotFlag (oLight, FALSE));
            AssignCommand (oLight, DelayCommand (0.2, DestroyObject (oLight)));
        }
        else
            return;

    oLight = CreateObject (OBJECT_TYPE_PLACEABLE, "plc_sol" + GetStringLowerCase (sType), lLoc, FALSE, "AVATAR_LIGHT_" + IntToString (nPlayer));

    SetPlotFlag (oLight, TRUE);
    SetLocalString (oLight, "AVATAR_LIGHT_TYPE", sType);

    AssignCommand (oLight, DelayCommand (0.2, PlayAnimation (ANIMATION_PLACEABLE_ACTIVATE)));
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectCutsceneGhost()), oLight);
}

void ActionUpdateMagicPool (int nPlayer, object oArea)
{
    int nScan = 1;
    int nMagic = GetMagicPool (nPlayer, oArea);

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oClear = GetNearestObject (OBJECT_TYPE_PLACEABLE, oCentre, nScan);

    while (oClear != OBJECT_INVALID)
    {
        string sTag = GetTag (oClear);

        if (GetStringLeft (sTag, 16) == "CARD_POOL_LIGHT_" && StringToInt (GetStringRight (sTag, 1)) == nPlayer)
        {
            AssignCommand (oClear, SetPlotFlag (oClear, FALSE));
            AssignCommand (oClear, DelayCommand (0.2, DestroyObject (oClear)));
        }

        oClear = GetNearestObject (OBJECT_TYPE_PLACEABLE, oCentre, ++nScan);
    }

    float fFacing = (nPlayer == 1) ? 180.0f : 0.0;
    float fMove = (nPlayer == 1) ? -2.5f : 2.5f;
    float fX = (nPlayer == 1) ? 10.0f : -10.0f;
    float fY = (nPlayer == 1) ? 1.0f : -1.0f;

    vector vAvatar = GetPosition (GetAvatar (nPlayer, oCentre));
    vector vStart = Vector (vAvatar.x + fX, vAvatar.y + fY, vAvatar.z);

    while (nMagic > 0)
    {
        object oLight = CreateObject (OBJECT_TYPE_PLACEABLE, "plc_solcyan", Location (oArea, Vector (vStart.x + (fMove * nMagic--), vStart.y, vStart.z), fFacing), FALSE, "CARD_POOL_LIGHT_" + IntToString (nPlayer));

        AssignCommand (oLight, DelayCommand (0.2, PlayAnimation (ANIMATION_PLACEABLE_ACTIVATE)));
        ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectCutsceneGhost()), oLight);
    }
}

void ActionUseGenerator (int nCard, int nPlayer, object oArea)
{
    int nNth = 1;
    int nGCount, nPCount, nPower;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

    while (oGenerator != OBJECT_INVALID)
    {
        if (GetOwner (oGenerator) == nPlayer)
        {
            int nID = GetGeneratorID (oGenerator);

            SetLocalInt (OBJECT_SELF, "CARDS_TEMP_GENERATOR_" + IntToString (nID), ++nGCount);

            if (GetIsGeneratorUsed (oGenerator))
                SetLocalInt (OBJECT_SELF, "CARDS_TEMP_GENERATOR_POWER_" + IntToString (nID), ++nPCount);

            DestroyObject (oGenerator);
        }

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
    }

    SetLocalInt (OBJECT_SELF, "CARDS_TEMP_GENERATOR_" + IntToString (nCard), ++nGCount);

    float fFacing = (nPlayer == 1) ? 180.0f : 0.0;
    float fMove = (nPlayer == 1) ? -2.5f : 2.5f;
    float fX = (nPlayer == 1) ? 10.0f : -10.0f;
    float fY = (nPlayer == 1) ? 4.0f : -4.0f;

    vector vAvatar = GetPosition (GetAvatar (nPlayer, oCentre));
    vector vStart = Vector (vAvatar.x + fX, vAvatar.y + fY, vAvatar.z);

    int nMax = nGCount;
    nGCount = 0;

    while (++nGCount <= nMax)
    {
        object oGenerator = CreateObject (OBJECT_TYPE_PLACEABLE, GetCardTag (CARD_GENERATOR_GENERIC, FALSE, FALSE, TRUE), Location (oArea, Vector (vStart.x + (fMove * nGCount), vStart.y, vStart.z), fFacing));

        nPower = (--nPCount >= 0) ? TRUE : FALSE;

        SetOwner (nPlayer, oGenerator);
        SetGeneratorID (CARD_GENERATOR_GENERIC, oGenerator);
        SetMagicGenerator (oGenerator, nPower);
    }

    DeleteLocalInt (OBJECT_SELF, "CARDS_TEMP_GENERATOR_" + IntToString (CARD_GENERATOR_GENERIC));
    DeleteLocalInt (OBJECT_SELF, "CARDS_TEMP_GENERATOR_POWER_" + IntToString (CARD_GENERATOR_GENERIC));
}

void ActionUsePower (int nAmount, int nPlayer, object oArea)
{
    if (nAmount <= 0)
        return;

    string sPool;

    int nNth = 1;
    int nPool = GetMagicPool (nPlayer, oArea);

    if (nPool)
    {
        if (nPool >= nAmount)
        {
            SetMagicPool (nPool - nAmount, nPlayer, oArea);

            SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Magic Pool Used: " + IntToString (nAmount));
        }
        else
        {
            nAmount -= nPool;

            SetMagicPool (0, nPlayer, oArea);

            sPool = "  Magic Pool Used: " + IntToString (nPool);
        }

        ActionUpdateMagicPool (nPlayer, oArea);

        if (sPool == "")
            return;
    }

    SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Magic Generators Used: " + IntToString (nAmount) + sPool);

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

    while (oGenerator != OBJECT_INVALID && nAmount > 0)
    {
        if (GetOwner (oGenerator) == nPlayer && !GetIsGeneratorUsed (oGenerator))
        {
            SetMagicGenerator (oGenerator, TRUE);

            nAmount -= 1;
        }

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
    }
}

void ActionUseStone (int nCard, int nPlayer, object oArea)
{
    int nNth = 1;
    int nSCount;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oStone = GetNearestObjectByTag ("d_stone", oCentre, nNth);

    while (oStone != OBJECT_INVALID)
    {
        if (GetOwner (oStone) == nPlayer)
        {
            SetLocalInt (OBJECT_SELF, "CARDS_TEMP_STONE_" + IntToString (++nSCount), GetStoneID (oStone));

            DestroyObject (oStone);
        }

        oStone = GetNearestObjectByTag ("d_stone", oStone, ++nNth);
    }

    SetLocalInt (OBJECT_SELF, "CARDS_TEMP_STONE_" + IntToString (++nSCount), nCard);

    float fFacing = (nPlayer == 1) ? 180.0f : 0.0;
    float fMove = (nPlayer == 1) ? -2.5f : 2.5f;
    float fX = (nPlayer == 1) ? 10.0f : -10.0f;
    float fY = (nPlayer == 1) ? 6.0f : -6.0f;

    vector vAvatar = GetPosition (GetAvatar (nPlayer, oCentre));
    vector vStart = Vector (vAvatar.x + fX, vAvatar.y + fY, vAvatar.z);

    int nMax = nSCount;
    nSCount = 0;

    while (++nSCount <= nMax)
    {
        int nRebuild = GetLocalInt (OBJECT_SELF, "CARDS_TEMP_STONE_" + IntToString (nSCount));

        object oStone = CreateObject (OBJECT_TYPE_PLACEABLE, GetCardTag (nRebuild, FALSE, TRUE), Location (oArea, Vector (vStart.x + (fMove * nSCount), vStart.y, vStart.z), fFacing));

        SetOwner (nPlayer, oStone);
        SetStoneID (nRebuild, oStone);

        DeleteLocalInt (OBJECT_SELF, "CARDS_TEMP_STONE_" + IntToString (nSCount));
    }
}
