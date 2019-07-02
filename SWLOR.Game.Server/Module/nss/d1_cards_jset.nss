/**********************************/
/*          d1_cards_jset
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Set functions
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

void SetCardsForAnte (int nNumber, int nCard, int nPlayer, object oArea)
{
    if (!nNumber)
        DeleteLocalInt (oArea, "CARDS_ANTE_CARDS_" + IntToString (nPlayer) + "_" + IntToString (nCard));
    else
        SetLocalInt (oArea, "CARDS_ANTE_CARDS_" + IntToString (nPlayer) + "_" + IntToString (nCard), nNumber);
}

void SetCardGameDeck (int nPlayer, object oDeck, object oArea)
{
    object oPlayer = GetCardGamePlayer (nPlayer, oArea);

    if (GetIsPC (oPlayer))
    {
        int nDestination = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
        int nNth;

        while (++nNth < CARD_MAX_ID)
            ActionTransferCard (nNth, CARD_SOURCE_ALL_CARDS, nDestination, OBJECT_INVALID, oArea, GetCardsInDeck (nNth, oDeck));
    }
    else
        SetNPCDeck (nPlayer, oArea, GetCardDeckType (oPlayer));
}

void SetCardGamePlayer (object oPlayer, int nNumber, object oArea)
{
    if (oPlayer == OBJECT_INVALID)
        DeleteLocalObject (oArea, "CARDS_PLAYER_" + IntToString (nNumber));
    else
        SetLocalObject (oArea, "CARDS_PLAYER_" + IntToString (nNumber), oPlayer);
}

void SetCardGameToggle (int nToggle, object oArea)
{
    if (!nToggle)
        DeleteLocalInt (oArea, "CARDS_GAME_IN_PROGRESS");
    else
        SetLocalInt (oArea, "CARDS_GAME_IN_PROGRESS", nToggle);
}

void SetCardOwner (int nOwner, object oTarget)
{
    if (!nOwner)
        DeleteLocalInt (oTarget, "CARDS_CREATURE_DISCARD_OWNER");
    else
        SetLocalInt (oTarget, "CARDS_CREATURE_DISCARD_OWNER", nOwner);
}

void SetDeckForAnte (int nPlayer, object oDeck, object oArea)
{
    if (oDeck == OBJECT_INVALID)
        DeleteLocalObject (oArea, "CARD_ANTE_DECK_" + IntToString (nPlayer));
    else
        SetLocalObject (oArea, "CARD_ANTE_DECK_" + IntToString (nPlayer), oDeck);
}

void SetDefaultRules (int nRules)
{
    if (!nRules || nRules == CARD_RULE_NORMAL)
        DeleteLocalInt (MODULE, "CARDS_DEFAULT_RULES");
    else
        SetLocalInt (MODULE, "CARDS_DEFAULT_RULES", nRules);
}

void SetDiscardPile (int nCard, int nPile, int nPlayer, object oArea)
{
    if (!nCard)
        DeleteLocalInt (oArea, "CARDS_DISCARDED_" + IntToString (nPlayer) + "_" + IntToString (nPile));
    else
        SetLocalInt (oArea, "CARDS_DISCARDED_" + IntToString (nPlayer) + "_" + IntToString (nPile), nCard);
}

void SetGameTurn (int nTurn, object oArea = OBJECT_SELF)
{
    if (!nTurn)
        DeleteLocalInt (oArea, "GAME_TURN");
    else
        SetLocalInt (oArea, "GAME_TURN", nTurn);
}

void SetGeneratorID (int nID, object oGenerator)
{
    if (!nID)
        DeleteLocalInt (oGenerator, "CARDS_GENERATOR_ID");
    else
        SetLocalInt (oGenerator, "CARDS_GENERATOR_ID", nID);
}

void SetHasCardEffect (int nCardID, object oTarget, int nHasEffect = TRUE)
{
    if (!nHasEffect)
        DeleteLocalInt (oTarget, "CARD_EFFECT_" + IntToString (nCardID));
    else
        SetLocalInt (oTarget, "CARD_EFFECT_" + IntToString (nCardID), nHasEffect);
}

void SetHasPlayedGenerator (int nPlayer, int nUsed, object oArea)
{
    if (!nUsed)
        DeleteLocalInt (oArea, "CARD_GENERATOR_PLACED_" + IntToString (nPlayer));
    else
        SetLocalInt (oArea, "CARD_GENERATOR_PLACED_" + IntToString (nPlayer), nUsed);
}

void SetMagicGenerator (object oGenerator, int nUsed)
{
    if (nUsed)
    {
        effect eEffect = GetFirstEffect (oGenerator);

        int nEffect = GetEffectType (eEffect);

        while (nEffect != EFFECT_TYPE_INVALIDEFFECT)
        {
            if (nEffect == EFFECT_TYPE_VISUALEFFECT && GetEffectSubType (eEffect) == SUBTYPE_SUPERNATURAL)
                RemoveEffect (oGenerator, eEffect);

            eEffect = GetNextEffect (oGenerator);

            nEffect = GetEffectType (eEffect);
        }

        SetLocalInt (oGenerator, "CARDS_GENERATOR_ON", TRUE);
    }
    else
    {
        int nVFX = (GetOwner (oGenerator) == 1) ? VFX_DUR_GLOW_RED : VFX_DUR_GLOW_BLUE;

        ApplyEffectToObject (DURATION_TYPE_PERMANENT, SupernaturalEffect (EffectVisualEffect (nVFX)), oGenerator);

        DeleteLocalInt (oGenerator, "CARDS_GENERATOR_ON");
    }
}

void SetMagicPool (int nAmount, int nPlayer, object oArea)
{
    if (!nAmount)
        DeleteLocalInt (oArea, "CARD_MANA_POOL_" + IntToString (nPlayer));
    else
        SetLocalInt (oArea, "CARD_MANA_POOL_" + IntToString (nPlayer), nAmount);
}

void SetNPCAnteBet (int nAnte, object oNPC = OBJECT_SELF)
{
  SetLocalInt (oNPC, "CARD_ANTE_BET", nAnte);
}

void SetNPCDeck (int nPlayer, object oArea, int nDecktype = CARD_DECK_TYPE_RANDOM)
{

    // generally speaking, used fixed decks (more challenging)
    if (nDecktype != CARD_DECK_TYPE_RANDOM)
    {
      SetNPCFixedDeck (nPlayer, oArea, nDecktype);
      return;
    }

    int nDeckMax = GetDeckMaximum (oArea);
    int nCardMax = GetCardMaximum (oArea);
    int nDeck = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nTemp = (nDeckMax - (nDeckMax % CARD_PURCHASE_LANDS)) / CARD_PURCHASE_LANDS;

    struct sCard sInfo;

    ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, nTemp);

    nDeckMax -= nTemp;

    while (nDeckMax > 0)
    {
        int nPurchase = Random (CARD_MAX_ID) + 1;
        int nPrevious = GetLocalInt (OBJECT_SELF, "BUILD_DECK" + IntToString (nPurchase));

        if (nPurchase == CARD_MAX_ID)
            nPurchase -= 1;

        sInfo = GetCardInfo (nPurchase);

        if (sInfo.nType != CARD_TYPE_GENERATOR)
        {
            if (nPrevious >= nCardMax)
                continue;

            if (sInfo.nDeck
                && nDecktype != CARD_DECK_TYPE_RANDOM
                && !(sInfo.nDeck & nDecktype))
                continue;
        }

        SetLocalInt (OBJECT_SELF, "BUILD_DECK" + IntToString (nPurchase), ++nPrevious);

        nDeckMax -= 1;

        ActionTransferCard (nPurchase, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea);
    }

    for (nDeckMax = 1; nDeckMax <= CARD_MAX_ID; nDeckMax++)
        DeleteLocalInt (OBJECT_SELF, "BUILD_DECK" + IntToString (nDeckMax));
}

// add this function by Adam in hopes to making the AI more challenging for Demon
// instead of semi-random decks, they are hopefully more balanced
void SetNPCFixedDeck (int nPlayer, object oArea, int nDecktype = CARD_DECK_TYPE_RANDOM)
{
    int nDeckMax = 40;  // set to fixed
    int nCardMax = 40;  // set to fixed
    int nDeck = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_1 : CARD_SOURCE_GAME_PLAYER_2;
    int nTemp = (nDeckMax - (nDeckMax % CARD_PURCHASE_LANDS)) / CARD_PURCHASE_LANDS;

    struct sCard sInfo;

    switch (nDecktype)
    {
        case CARD_DECK_TYPE_ANGELS:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_MYTHICAL_JYSIRAEL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_ANGELIC_CHOIR, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_HOLY_VENGEANCE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_ANGELIC_DEFENDER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_ANGELIC_HEALER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_ANGELIC_LIGHT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_ARCHANGEL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_AVENGING_ANGEL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SPELL_POTION_OF_HEROISM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_RESURRECT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_DISPEL_MAGIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
        break;
        case CARD_DECK_TYPE_ANIMALS:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SUMMON_COW, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_BEAR, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_COUGAR, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_WHITE_STAG, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_DRUID, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_GIANT_SPIDER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_FIREBALL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_RESURRECT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_ELIXIR_OF_LIFE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
        break;
        case CARD_DECK_TYPE_BIG_CREATURES:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 16);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_MAIDEN_OF_PARADISE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_DRAGON, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_ELDER_FIRE_ELEMENTAL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_BONE_GOLEM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_UMBER_HULK, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_VAMPIRE_MASTER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_WHITE_STAG, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_PIT_FIEND, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_DEMON_KNIGHT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_MYTHICAL_ARKNETH, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_VORTEX, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
        break;
        case CARD_DECK_TYPE_FAST_CREATURES:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 10);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_ATLANTIAN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_COW, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_GOBLIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_KOBOLD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_SEA_HAG, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_BULETTE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_TROGLODYTE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_RAT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_WOLF, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_WRATH_OF_THE_HORDE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_POWER_STREAM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_HOLY_VENGEANCE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_POTION_OF_HEROISM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_HIGHER_CALLING, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
        break;
        case CARD_DECK_TYPE_GOBLINS:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_DISPEL_MAGIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_GOBLIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_GOBLIN_CROSSBOW, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_GOBLIN_SHAMAN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_GOBLIN_WARLORD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_GOBLIN_WITCHDOCTOR, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_POTION_OF_HEROISM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_HOLY_VENGEANCE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_LIGHTNING_BOLT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_POWER_STREAM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
        break;
        case CARD_DECK_TYPE_KOBOLDS:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_MYTHICAL_DEEKIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_KOBOLD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_KOBOLD_CHIEF, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_KOBOLD_ENGINEER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_KOBOLD_KAMIKAZE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_KOBOLD_POGOSTICK, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_POTION_OF_HEROISM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_LIGHTNING_BOLT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_POWER_STREAM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
        break;
        case CARD_DECK_TYPE_RATS:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SUMMON_RAT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_RAT_KING, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_PLAGUE_BEARER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_FERAL_RAT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_POTION_OF_HEROISM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_LIGHTNING_BOLT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_ELIXIR_OF_LIFE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_PARALYZE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_POWER_STREAM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
        break;
        case CARD_DECK_TYPE_SPELLS:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SPELL_FIREBALL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_LIGHTNING_BOLT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_SCORCHED_EARTH, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_FLUX, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_SABOTAGE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_ELIXIR_OF_LIFE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_COUNTERSPELL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_HIGHER_CALLING, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
        break;
        case CARD_DECK_TYPE_UNDEAD:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SUMMON_ZOMBIE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_ZOMBIE_LORD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_VAMPIRE, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_VAMPIRE_MASTER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_SKELETAL_ARCHER, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_SKELETAL_WARRIOR, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_BONE_GOLEM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SUMMON_LICH, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_SHADOW_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SUMMON_REVENANT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_DEATH_PACT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
        break;
        case CARD_DECK_TYPE_WOLVES:
          ActionTransferCard (CARD_GENERATOR_GENERIC, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 13);
          ActionTransferCard (CARD_SUMMON_WOLF, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_WHITE_WOLF, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SUMMON_WHITE_STAG, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SUMMON_DRUID, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 3);
          ActionTransferCard (CARD_SPELL_LIGHTNING_BOLT, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_MIND_CONTROL, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 4);
          ActionTransferCard (CARD_SPELL_FIRE_SHIELD, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
          ActionTransferCard (CARD_SPELL_POTION_OF_HEROISM, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 1);
          ActionTransferCard (CARD_SPELL_ASSASSIN, CARD_SOURCE_ALL_CARDS, nDeck, OBJECT_INVALID, oArea, 2);
        break;
    }
}

void SetNPCCardAI (int nAIDifficulty, object oNPC = OBJECT_SELF)
{
    if (!nAIDifficulty)
        DeleteLocalInt (oNPC, "CARD_AI_DIFFICULTY");
    else
        SetLocalInt (oNPC, "CARD_AI_DIFFICULTY", nAIDifficulty);
}

void SetNPCDeckType (int nDeckType, object oNPC = OBJECT_SELF)
{
    if (!nDeckType)
        DeleteLocalInt (oNPC, "CARD_DECK_TYPE");
    else
        SetLocalInt (oNPC, "CARD_DECK_TYPE", nDeckType);
}

void SetNPCGoldBet (int nGold, object oNPC = OBJECT_SELF)
{
    if (!nGold)
        DeleteLocalInt (oNPC, "CARD_GOLD_BET");
    else
        SetLocalInt (oNPC, "CARD_GOLD_BET", nGold);
}

void SetOriginalOwner (int nOwner, object oObject)
{
    if (!nOwner)
        DeleteLocalInt (oObject, "CARD_ORIGINALLY_OWNED_BY");
    else
        SetLocalInt (oObject, "CARD_ORIGINALLY_OWNED_BY", nOwner);
}

void SetOwner (int nOwner, object oObject)
{
    int nVFX = (nOwner == 1) ? VFX_DUR_GLOW_RED : VFX_DUR_GLOW_BLUE;

    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectVisualEffect (VFX_DUR_ETHEREAL_VISAGE), oObject, 0.5);

    SetLocalInt (oObject, "CARD_OWNED_BY_PLAYER", nOwner);

    ChangeFaction (oObject, GetObjectByTag ("FactionObjectPlayer" + IntToString (nOwner)));

    if (GetStringLeft (GetTag (oObject), 7) != "d_card_")
    {
        effect eEffect = GetFirstEffect (oObject);

        int nEffect = GetEffectType (eEffect);

        while (nEffect != EFFECT_TYPE_INVALIDEFFECT)
        {
            if (nEffect == EFFECT_TYPE_VISUALEFFECT && GetEffectSubType (eEffect) == SUBTYPE_SUPERNATURAL)
                RemoveEffect (oObject, eEffect);

            eEffect = GetNextEffect (oObject);

            nEffect = GetEffectType (eEffect);
        }

        ApplyEffectToObject (DURATION_TYPE_PERMANENT, SupernaturalEffect (EffectVisualEffect (nVFX)), oObject);
    }
}

void SetPlayerRating (int nRating, object oPlayer)
{

  object oBag = GetCardBag(oPlayer, FALSE, TRUE);
  CopyObject(oBag, GetLocation(oPlayer), oPlayer, "CardBag" + IntToString(nRating));
  DestroyObject(oBag);
}

void SetPlayerResults (int nResults, int nResultType, int nOpponentRating, object oPlayer)
{
    if (!nResults)
        DeleteLocalInt (oPlayer, "CARDS_GAME_RESULTS" + IntToString (nOpponentRating) + "_" + IntToString (nResultType));
    else
        SetLocalInt (oPlayer, "CARDS_GAME_RESULTS" + IntToString (nOpponentRating) + "_" + IntToString (nResultType), nResults);
}

void SetReturnLocation (location lLoc, object oPlayer)
{
    SetLocalLocation (oPlayer, "CARDS_PLAYER_LOCATION", lLoc);
}

void SetSpawnBoost (int nBoost, object oTarget = OBJECT_SELF)
{
    if (!nBoost)
        DeleteLocalInt (oTarget, "CARDS_CREATURE_POWER_BOOST");
    else
        SetLocalInt (oTarget, "CARDS_CREATURE_POWER_BOOST", nBoost);
}

void SetStoneID (int nID, object oStone)
{
    if (!nID)
        DeleteLocalInt (oStone, "CARDS_STONE_ID");
    else
        SetLocalInt (oStone, "CARDS_STONE_ID", nID);
}

void SetTotalCardsSold (int nTotal, int nRarity)
{
    if (!nTotal)
        DeleteLocalInt (MODULE, "CARDS_TOTAL_SOLD_" + IntToString (nRarity));
    else
        SetLocalInt (MODULE, "CARDS_TOTAL_SOLD_" + IntToString (nRarity), nTotal);
}

void SetVariantRules (int nRules, object oArea)
{
    if (!nRules)
        DeleteLocalInt (oArea, "CARDS_VARIANTS_IN_PLAY");
    else
        SetLocalInt (oArea, "CARDS_VARIANTS_IN_PLAY", nRules);
}
