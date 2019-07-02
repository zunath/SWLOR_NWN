/**********************************/
/*          d1_cards_jup
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Upkeep functions for the main
/*  game.  NOTE: MUST be run from
/*  the area.
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

void ActionUpkeepCreature (object oCreature)
{
    int nCard = GetCardID (oCreature);
    int nPlayer = GetOwner (oCreature);

    string sName = GetName (oCreature);

    object oScan;
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre());
    object oAvatar = GetAvatar (nPlayer, oCentre);

    ChangeFaction (oCreature, GetObjectByTag ("FactionObjectPlayer" + IntToString (nPlayer)));

    ClearPersonalReputation (GetCardGamePlayer (1, OBJECT_SELF), oCreature);
    ClearPersonalReputation (GetCardGamePlayer (2, OBJECT_SELF), oCreature);

    if (GetCreatureFailedUpkeep (nCard, nPlayer, oCreature, oCentre))
    {
        SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), sName + " upkeep not met.  Sacrificing.");

        AssignCommand (oCreature, DestroyCardCreature (TRUE));

        return;
    }

    for (nPlayer = 1; nPlayer <= 2; nPlayer++)
    {
        object oPlayer = GetCardGamePlayer (nPlayer, OBJECT_SELF);

        if (GetIsPC (oPlayer) || GetOwner (oCreature) != nPlayer)
            continue;

        struct sCard sSacrifice = GetCardInfo (nCard);

        if (sSacrifice.nSacrifice)
        {
            int nRating = GetAISacrificeEvaluation (sSacrifice, GetIsPowerAvailable (nPlayer, OBJECT_SELF), nPlayer, oAvatar, oCreature);

            if (nRating > 0)
                AssignCommand (oCreature, ActionUseSacrifice (nPlayer));
        }
    }
}

void ActionUpkeepCycle (int nTurn)
{
    SendMessageToCardPlayers ("Turn #" + IntToString (nTurn), OBJECT_SELF);

    int nNth = 1;
    int nFlux, nVortex1, nVortex2, nHigherCalling;
    int nVortex = GetHasCardEffect (CARD_SPELL_VORTEX, OBJECT_SELF);

    object oCentre = GetGameCentre();
    object oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

    effect eFlux;

    while (oGenerator != OBJECT_INVALID)
    {
        if (GetOwner (oGenerator) == 1)
            nVortex1 += 1;
        else
            nVortex2 += 1;

        if (GetIsGeneratorUsed (oGenerator))
            SetMagicGenerator (oGenerator, FALSE);

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
    }

    if (nVortex)
    {
        nVortex += 1;
        nVortex1 = (nVortex1 - (nVortex1 % nVortex)) / nVortex;
        nVortex2 = (nVortex2 - (nVortex2 % nVortex)) / nVortex;

        SendMessageToPC (GetCardGamePlayer (1, OBJECT_SELF), "Vortex drains power: " + IntToString (nVortex1));
        SendMessageToPC (GetCardGamePlayer (2, OBJECT_SELF), "Vortex drains power: " + IntToString (nVortex2));

        nNth = 1;

        oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

        while (oGenerator != OBJECT_INVALID)
        {
            if (GetOwner (oGenerator) == 1)
            {
                if (--nVortex1 >= 0)
                    SetMagicGenerator (oGenerator, TRUE);
            }
            else
            {
                if (--nVortex2 >= 0)
                    SetMagicGenerator (oGenerator, TRUE);
            }

            oGenerator = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
        }
    }

    nFlux = GetHasCardEffect (CARD_SPELL_FLUX, OBJECT_SELF);

    if (nFlux)
    {
        nFlux *= (nNth - 1);

        eFlux = EffectLinkEffects (EffectVisualEffect (VFX_IMP_DOOM), EffectDamage (nFlux, DAMAGE_TYPE_ELECTRICAL, DAMAGE_POWER_PLUS_FIVE));

        SendMessageToCardPlayers ("Energy flux deals " + IntToString (nFlux) + " damage to all creatures and players.", OBJECT_SELF);
    }

    nHigherCalling = GetHasCardEffect (CARD_SPELL_HIGHER_CALLING, OBJECT_SELF);

    if (nHigherCalling)
    {
      if(GetIsDebug())
        SendMessageToPC(GetFirstPC(), "Higher Calling effect");

        DoCardEffectHigherCalling (OBJECT_SELF, oCentre);
    }


    nNth = 1;

    object oCreature = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCreature != OBJECT_INVALID)
    {
        if (nFlux)
            ApplyEffectToObject (DURATION_TYPE_INSTANT, eFlux, oCreature);

        if (GetCardID (oCreature) && !GetIsMine (oCreature))
            ActionUpkeepCreature (oCreature);

        oCreature = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    nNth = 1;

    oCreature = GetNearestObjectByTag ("d_stone", oCentre, nNth);

    while (oCreature != OBJECT_INVALID)
    {
        ActionUpkeepStone (oCreature);

        oCreature = GetNearestObjectByTag ("d_stone", oCentre, ++nNth);
    }

    for (nNth = 1; nNth <= 2; nNth++)
        DoUpkeepDiscardPile (nNth, oCentre);

    ActionDrawCards (1, nTurn);
    ActionDrawCards (2, nTurn);

    SetHasPlayedGenerator (1, FALSE, OBJECT_SELF);
    SetHasPlayedGenerator (2, FALSE, OBJECT_SELF);

    DelayCommand (1.0f, ActionAI());
}

void ActionUpkeepStone (object oStone)
{
    int nPlayer = GetOwner (oStone);

    switch (GetStoneID (oStone))
    {
        case CARD_STONE_SOLAR:      DoUpkeepSolarStone (nPlayer, oStone);       return;
        default:                    ActionUpkeepCustomStone (nPlayer, oStone);  return;
    }
}

int GetCreatureFailedUpkeep (int nCard, int nPlayer, object oCreature, object oCentre)
{
    int nNth;
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oAvatar = GetAvatar (nPlayer, oCentre);
    object oScan;

    switch (nCard)
    {
        case CARD_SUMMON_ANGELIC_HEALER:    DoUpkeepHealAvatar (3, nCard, nPlayer, oCentre);   return FALSE;
        case CARD_SUMMON_FAIRY_DRAGON:
            if (!GetIsPowerAvailable (nPlayer, OBJECT_SELF, 1))
            {
                if (!GetIsPowerAvailable (nEnemy, OBJECT_SELF, 1))
                {
                    int nUser = 1;

                    for (; nUser <= 2; nUser++)
                        DoSacrificeFaerieDragon (nUser, OBJECT_SELF);

                    return TRUE;
                }
                else
                {
                    SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Fairy Dragon upkeep failed.  Fairy Dragon switches sides.");

                    DoCardEffectMindControl (oCreature, TRUE);
                }
            }
            else
            {
                ActionUsePower (1, nPlayer, OBJECT_SELF);
                ActionUsePower (1, nEnemy, OBJECT_SELF);
            }

            break;

        case CARD_SUMMON_KOBOLD_ENGINEER:   DoUpkeepKoboldEngineer (nPlayer, oCentre, oCreature);   return FALSE;
        case CARD_SUMMON_LOREMASTER:
            if (!GetIsPowerAvailable (nPlayer, OBJECT_SELF, 2))
                return TRUE;
            else
            {
                ActionUsePower (2, nPlayer, OBJECT_SELF);

                SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Loremaster uses two magic generators.");
            }

            break;

        case CARD_SUMMON_PAIN_GOLEM:        DoUpkeepPainGolem (nPlayer, oCentre, oCreature);    return FALSE;
        case CARD_SUMMON_PIT_FIEND:
            if (!GetHasGenerators (nPlayer, OBJECT_SELF, 1))
            {
                struct sCard sScan;

                int nCountKill = 0;
                nNth = 1;

                oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

                while (oScan != OBJECT_INVALID && nCountKill != 2)
                {
                    sScan = GetCardInfo (GetCardID (oScan));

                    if (sScan.nCard && GetOwner (oScan) == nPlayer)
                    {
                        nCountKill += 1;

                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH), oScan);

                        AssignCommand (oScan, DestroyCardCreature (TRUE));

                        SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Pit Fiend gobbles " + sScan.sName);
                    }

                    oScan = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCreature, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
                }

                if (nCountKill != 2)
                {
                    SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Pit Fiend upkeep failed.  Pit Fiend switches sides.");

                    SetOwner (nEnemy, oCreature);
                    SetOriginalOwner (nEnemy, oCreature);

                    AssignCommand (oCreature, ClearAllActions());
                    AssignCommand (oCreature, DetermineCombatRound());
                }
                else
                    SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Pit Fiend consumes two creatures.");

                return FALSE;
            }
            else
            {
                ActionDestroyPower (1, nPlayer, OBJECT_SELF);
                SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Pit Fiend consumes magic generator.");
            }

            break;

        case CARD_SUMMON_REVENANT:
        case CARD_SUMMON_VAMPIRE_MASTER:    DoUpkeepDrainAvatar (10, nPlayer, oCentre, oCreature);  return FALSE;
        case CARD_SUMMON_WHITE_STAG:        DoUpkeepHealAvatar (5, nCard, nPlayer, oCentre);   return FALSE;
        default:                            return GetCreatureFailedCustomUpkeep (nCard, nPlayer, oCreature, oCentre);
    }

    return FALSE;
}
