/**********************************/
/*          d1_cards_jinc
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th October 2003
/*  Updated On: 18th February 2004
/**********************************/
/*  #include
/*  Contains core code for the card
/*  game.  Most is in sub-headers.
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

#include "NW_I0_GENERIC"
#include "d1_cards_jcons"
#include "d1_cards_jcards"
#include "d1_cards_jdec"
#include "d1_cards_jcustom"
#include "d1_cards_jai"
#include "d1_cards_jdata"
#include "d1_cards_jdo"
#include "d1_cards_jgame"
#include "d1_cards_jget"
#include "d1_cards_jman"
#include "d1_cards_jmenu"
#include "d1_cards_jset"
#include "d1_cards_jup"
#include "d1_cards_juse"

void AddToCardsSold (int nRarity = 0, int nCards = 1)
{
    if (!nRarity && GetTotalCardsSold (0) <= 0)
        InitialiseRarity();

    SetTotalCardsSold (GetTotalCardsSold (nRarity) + nCards, nRarity);

    if (!nRarity)
        for (nRarity = 1; nRarity <= CARD_RARITY_ULTRA_RARE; nRarity++)
            SetTotalCardsSold (GetTotalCardsSold (nRarity) + nCards, nRarity);
}

void AdjustPlayerResults (int nAdjust, int nResult, object oOpponent, object oPlayer)
{
    int nOpponent, nPlayer, nScore = 2;

    // no penalty for losing or draw
    if (nResult != CARD_GAME_END_RESULT_WIN)
      return;

    nPlayer = GetPlayerRating(oPlayer);

    if (GetIsPC (oOpponent))
    {
        // beating higher score opponents leads to greater points.
        nOpponent = GetPlayerRating (oOpponent);

        if (nOpponent > nPlayer * 2)
        {
          SendMessageToPC(oPlayer, "You score extra points for defeating such a challenging opponent.");
          nScore = 3;
        }
        if (nOpponent * 2> nPlayer)
        {
          SendMessageToPC(oPlayer, "Seek out more challenging opponents to score more points.");
          nScore = 1;
        }
    }
    else
    {
        nOpponent = GetAIDifficulty (oOpponent);
        if(nOpponent == CARD_AI_DIFFICULTY_TRAINING)
          nScore = 0;
        if(nOpponent == CARD_AI_DIFFICULTY_EASY)
          nScore = 1;
        if(nOpponent == CARD_AI_DIFFICULTY_NORMAL)
          nScore = 2;
        if(nOpponent == CARD_AI_DIFFICULTY_HARD)
          nScore = 3;

        if(nPlayer > 100)
        {
          SendMessageToPC(oPlayer, "You must compete against human opponents to further your score.");
          nScore = 0;
        }
    }

    //SetPlayerResults (GetPlayerResults (nResult, nOpponent, oPlayer) + nAdjust, nResult, nOpponent, oPlayer);
    if(nScore > 0)
    {
      SendMessageToPC(oPlayer, "You earned " + IntToString(nScore) + " points with your win against " + GetName(oOpponent) + ".");
      SetPlayerRating (nPlayer + nScore, oPlayer);
    }
}

void ClearAllCards (object oWaypoint, int nPlayer = FALSE)
{
    object oCard = GetFirstObjectInArea (GetArea (oWaypoint));

    while (oCard != OBJECT_INVALID)
    {
        if (GetObjectType (oCard) == OBJECT_TYPE_PLACEABLE)
            if (GetCardID (oCard) && (!nPlayer || (nPlayer && GetOwner (oCard) == nPlayer)))
            {
                SetPlotFlag (oCard, FALSE);
                DelayCommand (0.1, DestroyObject (oCard));
            }

        oCard = GetNextObjectInArea (GetArea(oWaypoint));
    }
}

void ClearDiscardPile (int nPlayer, object oArea)
{
    int nNth;
    int nMax = GetDiscardPileSize (nPlayer, oArea);

    while (++nNth <= nMax)
        SetDiscardPile (0, nNth, nPlayer, oArea);
}

void ClearGameArea (object oArea = OBJECT_SELF)
{
    object oClear = GetFirstObjectInArea (oArea);

    while (oClear != OBJECT_INVALID)
    {
        string sTag = GetTag (oClear);
        if (sTag != "CARD_GAME_CENTRAL_ARENA"
            && sTag != "EmergencyExit"
            && sTag != "SpectatorExit"
            && sTag != "FactionObjectPlayer1"
            && sTag != "FactionObjectPlayer2"
            && !GetIsPC (oClear))
            {
                AssignCommand (oClear, SetPlotFlag (oClear, FALSE));
                AssignCommand (oClear, SetIsDestroyable (TRUE));

                AssignCommand (oClear, DelayCommand (0.2, DestroyObject (oClear)));
            }

        oClear = GetNextObjectInArea (oArea);
    }
}


void DestroyCardCreature (int nNoCorpse = FALSE, int nNoDiscard = FALSE, int nNoImmunity = TRUE, int nNoInvincibility = TRUE)
{
    if (nNoCorpse)
        SetIsDestroyable (TRUE);

    if (nNoInvincibility)
        SetPlotFlag (OBJECT_SELF, FALSE);

    if (nNoDiscard)
        DelayCommand (0.1, DestroyObject (OBJECT_SELF));
    else if (nNoImmunity)
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (GetMaxHitPoints() + 10, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), OBJECT_SELF);
    else
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDeath(), OBJECT_SELF);
}

void DiscardOnKill (object oTarget)
{
    struct sCard sKill = GetCardInfo (GetCardID (oTarget));

    if (!sKill.nCard || sKill.nNoDiscard)
        return;

    if (!GetHasCardEffect (CARD_SPELL_BOOMERANG, oTarget)
        && !GetHasCardEffect (CARD_SPELL_RESURRECT, oTarget)
        && !GetIsClone (oTarget))
        SendToDiscardPile (sKill.nCard, GetCardOwner (oTarget), GetArea (oTarget));
}

void InitialiseRarity()
{
    SetTotalCardsSold ((Random (CARD_PURCHASE_UNCOMMONS) + 1) * (2), CARD_RARITY_UNCOMMON);
    SetTotalCardsSold ((Random (CARD_PURCHASE_RARES) + 1) * (2), CARD_RARITY_RARE);
    SetTotalCardsSold ((Random (CARD_PURCHASE_VERY_RARES) + 1) * (2), CARD_RARITY_VERY_RARE);
    SetTotalCardsSold ((Random (CARD_PURCHASE_ULTRA_RARES) + 1) * (2), CARD_RARITY_ULTRA_RARE);
}

void RemoveEffectByType (int nType, object oTarget, object oSource = OBJECT_INVALID)
{
    effect eEffect = GetFirstEffect (oTarget);

    int nEffect = GetEffectType (eEffect);

    while (nEffect != EFFECT_TYPE_INVALIDEFFECT)
    {
        if (nType == nEffect)
            if (oSource == OBJECT_INVALID || (oSource != OBJECT_INVALID && GetEffectCreator (eEffect) == oSource))
                RemoveEffect (oTarget, eEffect);

        eEffect = GetNextEffect (oTarget);

        nEffect = GetEffectType (eEffect);
    }
}

void RemoveFromDiscardPile (int nCard, int nPlayer, object oArea, int nTopDown = TRUE)
{
    int nMax = GetDiscardPileSize (nPlayer, oArea);
    int nCycle = (nTopDown) ? nMax : 1;
    int nPile = GetDiscardPile (nCycle, nPlayer, oArea);

    while (nPile != nCard && nCycle <= nMax && nCycle > 0)
        nPile = (nTopDown) ? GetDiscardPile (--nCycle, nPlayer, oArea) : GetDiscardPile (++nCycle, nPlayer, oArea);

    if (nPile == nCard)
    {
        nMax += 1;

        while (++nCycle <= nMax)
            SetDiscardPile (GetDiscardPile (nCycle, nPlayer, oArea), nCycle - 1, nPlayer, oArea);
    }
}

void ResetGameVariables (object oArea = OBJECT_SELF)
{
    int nPlayer, nCard;

    for (nPlayer = 1; nPlayer <= 2; nPlayer++)
    {
        for (nCard = 1; nCard < CARD_MAX_ID; nCard++)
        {
            DeleteLocalInt (oArea, "CARDS_GAME_DECK_" + IntToString (nPlayer) + "_" + IntToString (nCard));
            SetCardsForAnte (0, nCard, nPlayer, oArea);
        }

        SetHasPlayedGenerator (nPlayer, FALSE, oArea);
        SetMagicPool (0, nPlayer, oArea);
        SetCardGamePlayer (OBJECT_INVALID, nPlayer, oArea);
        SetDeckForAnte (nPlayer, OBJECT_INVALID, oArea);

        ClearDiscardPile (nPlayer, oArea);
    }

    SetGameTurn (FALSE, oArea);
    SetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, oArea, FALSE);
    SetHasCardEffect (CARD_SPELL_FLUX, oArea, FALSE);
    SetHasCardEffect (CARD_SPELL_HIGHER_CALLING, oArea, FALSE);
    SetHasCardEffect (CARD_SPELL_POWER_STREAM, oArea, FALSE);
    SetHasCardEffect (CARD_SPELL_VORTEX, oArea, FALSE);
    SetVariantRules (FALSE, oArea);
    SetCardGameToggle (FALSE, oArea);
    DeleteLocalInt (oArea, "ENDING_GAME");
    DeleteLocalInt (oArea, "CARD_AVATAR_ALT_DEATH");
}

void SendToDiscardPile (int nCard, int nPlayer, object oArea)
{
    SetDiscardPile (nCard, GetDiscardPileSize (nPlayer, oArea) + 1, nPlayer, oArea);
}

void SendMessageToCardPlayers (string sMessage, object oArea)
{
    //modified to send to all PCs, including spectators
    object oPC = GetFirstObjectInArea(oArea);
    while(oPC != OBJECT_INVALID)
    {
      if(GetIsPC(oPC))
        SendMessageToPC (oPC, sMessage);
      oPC = GetNextObjectInArea(oArea);
    }
}

int AlphaNumeric (string sLetter)
{
    sLetter = GetStringLowerCase (sLetter);

    return (sLetter == "a") ? 1 : (sLetter == "b") ? 2 : (sLetter == "c") ? 3 :
           (sLetter == "d") ? 4 : (sLetter == "e") ? 5 : (sLetter == "f") ? 6 :
           (sLetter == "g") ? 7 : (sLetter == "h") ? 8 : (sLetter == "i") ? 9 :
           (sLetter == "j") ? 10 : (sLetter == "k") ? 11 : (sLetter == "l") ? 12 :
           (sLetter == "m") ? 13 : (sLetter == "n") ? 14 : (sLetter == "o") ? 15 :
           (sLetter == "p") ? 16 : (sLetter == "q") ? 17 : (sLetter == "r") ? 18 :
           (sLetter == "s") ? 19 : (sLetter == "t") ? 20 : (sLetter == "u") ? 21 :
           (sLetter == "v") ? 22 : (sLetter == "w") ? 23 : (sLetter == "x") ? 24 :
           (sLetter == "y") ? 25 : (sLetter == "z") ? 26 : FALSE;
}

int CalculatePlayerRating (object oPlayer)
{
    int nWinsApp = GetPlayerResults (CARD_GAME_END_RESULT_WIN, CARD_PLAYER_RATING_APPRENTICE, oPlayer);
    int nWinsNov = GetPlayerResults (CARD_GAME_END_RESULT_WIN, CARD_PLAYER_RATING_NOVICE, oPlayer);
    int nWinsPla = GetPlayerResults (CARD_GAME_END_RESULT_WIN, CARD_PLAYER_RATING_PLAYER, oPlayer);
    int nWinsExp = GetPlayerResults (CARD_GAME_END_RESULT_WIN, CARD_PLAYER_RATING_EXPERT, oPlayer);
    int nWinsMas = GetPlayerResults (CARD_GAME_END_RESULT_WIN, CARD_PLAYER_RATING_MASTER, oPlayer);
    int nLoseApp = GetPlayerResults (CARD_GAME_END_RESULT_LOSE, CARD_PLAYER_RATING_APPRENTICE, oPlayer);
    int nLoseNov = GetPlayerResults (CARD_GAME_END_RESULT_LOSE, CARD_PLAYER_RATING_NOVICE, oPlayer);
    int nLosePla = GetPlayerResults (CARD_GAME_END_RESULT_LOSE, CARD_PLAYER_RATING_PLAYER, oPlayer);
    int nLoseExp = GetPlayerResults (CARD_GAME_END_RESULT_LOSE, CARD_PLAYER_RATING_EXPERT, oPlayer);
    int nLoseMas = GetPlayerResults (CARD_GAME_END_RESULT_LOSE, CARD_PLAYER_RATING_MASTER, oPlayer);
    int nDrawApp = GetPlayerResults (CARD_GAME_END_RESULT_DRAW, CARD_PLAYER_RATING_APPRENTICE, oPlayer);
    int nDrawNov = GetPlayerResults (CARD_GAME_END_RESULT_DRAW, CARD_PLAYER_RATING_NOVICE, oPlayer);
    int nDrawPla = GetPlayerResults (CARD_GAME_END_RESULT_DRAW, CARD_PLAYER_RATING_PLAYER, oPlayer);
    int nDrawExp = GetPlayerResults (CARD_GAME_END_RESULT_DRAW, CARD_PLAYER_RATING_EXPERT, oPlayer);
    int nDrawMas = GetPlayerResults (CARD_GAME_END_RESULT_DRAW, CARD_PLAYER_RATING_MASTER, oPlayer);

    return (nWinsApp * CARD_PLAYER_RATING_APPRENTICE_X) - (nLoseApp * CARD_PLAYER_RATING_APPRENTICE_X) +
           (nWinsNov * CARD_PLAYER_RATING_NOVICE_X) - (nLoseNov * CARD_PLAYER_RATING_NOVICE_X) +
           (nWinsPla * CARD_PLAYER_RATING_PLAYER_X) - (nLosePla * CARD_PLAYER_RATING_PLAYER_X) +
           (nWinsExp * CARD_PLAYER_RATING_EXPERT_X) - (nLoseExp * CARD_PLAYER_RATING_EXPERT_X) +
           (nWinsMas * CARD_PLAYER_RATING_MASTER_X) - (nLoseMas * CARD_PLAYER_RATING_MASTER_X);
}

string PrintAvatarHealth (object oCentre)
{
    object oArea = GetArea (oCentre);

    return GetName (GetCardGamePlayer (1, oArea)) + " (" + IntToString (GetCurrentHitPoints (GetAvatar (1, oCentre))) + ") / " + GetName (GetCardGamePlayer (2, oArea)) + " (" + IntToString (GetCurrentHitPoints (GetAvatar (2, oCentre))) + ")";
}

string PrintCardRarity (int nRarity)
{
    return (nRarity == CARD_RARITY_COMMON) ? "Common" :
           (nRarity == CARD_RARITY_UNCOMMON) ? "Uncommon" :
           (nRarity == CARD_RARITY_RARE) ? "Rare" :
           (nRarity == CARD_RARITY_VERY_RARE) ? "Very Rare" :
           (nRarity == CARD_RARITY_ULTRA_RARE) ? "Ultra Rare" : "";
}

string PrintCardSubType (int nType)
{
    return (nType == CARD_SUBTYPE_SPELL_CONTINGENCY) ? "Contingency" :
           (nType == CARD_SUBTYPE_SPELL_BOOSTER) ? "Enhancer" :
           (nType == CARD_SUBTYPE_SPELL_BOOSTER_GLOBAL) ? "Global Enhancer" :
           (nType == CARD_SUBTYPE_SPELL_BOOSTER_MULTI) ? "Multitarget Enhancer" :
           (nType == CARD_SUBTYPE_SPELL_ENCHANT) ? "Enchantment" :
           (nType == CARD_SUBTYPE_SPELL_ENCHANT_GLOBAL) ? "Global Enchantment" :
           (nType == CARD_SUBTYPE_SPELL_ENCHANT_MULTI) ? "Multitarget Enchantment" :
           (nType == CARD_SUBTYPE_SPELL_INSTANT) ? "Arcana" :
           (nType == CARD_SUBTYPE_SPELL_INSTANT_GLOBAL) ? "Global Arcana" :
           (nType == CARD_SUBTYPE_SPELL_INSTANT_MULTI) ? "Multitarget Arcana" :
           (nType == CARD_SUBTYPE_SPELL_PENALTY) ? "Curse" :
           (nType == CARD_SUBTYPE_SPELL_PENALTY_GLOBAL) ? "Global Curse" :
           (nType == CARD_SUBTYPE_SPELL_PENALTY_MULTI) ? "Multitarget Curse" :
           (nType == CARD_SUBTYPE_STONE_DECK) ? "Deck Manipulation" :
           (nType == CARD_SUBTYPE_SUMMON_ABERRATION) ? "Aberration" :
           (nType == CARD_SUBTYPE_SUMMON_ANGEL) ? "Angel" :
           (nType == CARD_SUBTYPE_SUMMON_ANIMAL) ? "Animal" :
           (nType == CARD_SUBTYPE_SUMMON_BEAST) ? "Beast" :
           (nType == CARD_SUBTYPE_SUMMON_BEHOLDER) ? "Beholder" :
           (nType == CARD_SUBTYPE_SUMMON_BUGBEAR) ? "Bugbear" :
           (nType == CARD_SUBTYPE_SUMMON_DEMON) ? "Demon" :
           (nType == CARD_SUBTYPE_SUMMON_DRAGON) ? "Dragon" :
           (nType == CARD_SUBTYPE_SUMMON_DWARF) ? "Dwarf" :
           (nType == CARD_SUBTYPE_SUMMON_ELEMENTAL_AIR) ? "Air Elemental" :
           (nType == CARD_SUBTYPE_SUMMON_ELEMENTAL_EARTH) ? "Earth Elemental" :
           (nType == CARD_SUBTYPE_SUMMON_ELEMENTAL_FIRE) ? "Fire Elemental" :
           (nType == CARD_SUBTYPE_SUMMON_ELEMENTAL_WATER) ? "Water Elemental" :
           (nType == CARD_SUBTYPE_SUMMON_FEY) ? "Fey" :
           (nType == CARD_SUBTYPE_SUMMON_GIANT) ? "Giant" :
           (nType == CARD_SUBTYPE_SUMMON_GOBLIN) ? "Goblin" :
           (nType == CARD_SUBTYPE_SUMMON_GOLEM) ? "Golem" :
           (nType == CARD_SUBTYPE_SUMMON_GUARDIAN) ? "Guardian" :
           (nType == CARD_SUBTYPE_SUMMON_HUMAN) ? "Human" :
           (nType == CARD_SUBTYPE_SUMMON_KOBOLD) ? "Kobold" :
           (nType == CARD_SUBTYPE_SUMMON_LICH) ? "Lich" :
           (nType == CARD_SUBTYPE_SUMMON_RAT) ? "Rat" :
           (nType == CARD_SUBTYPE_SUMMON_SKELETON) ? "Skeleton" :
           (nType == CARD_SUBTYPE_SUMMON_SHADE) ? "Shade" :
           (nType == CARD_SUBTYPE_SUMMON_SPIDER) ? "Spider" :
           (nType == CARD_SUBTYPE_SUMMON_UNDEAD) ? "Undead" :
           (nType == CARD_SUBTYPE_SUMMON_VAMPIRE) ? "Vampire" :
           (nType == CARD_SUBTYPE_SUMMON_WITCH) ? "Witch" :
           (nType == CARD_SUBTYPE_SUMMON_WOLF) ? "Wolf" :
           (nType == CARD_SUBTYPE_SUMMON_ZOMBIE) ? "Zombie" : "";
}

string PrintCardType (int nType)
{
    return (nType == CARD_TYPE_GENERATOR) ? "Generator" :
           (nType == CARD_TYPE_MYTHICAL) ? "Mythical" :
           (nType == CARD_TYPE_SPELL) ? "Spell" :
           (nType == CARD_TYPE_STONE) ? "Stone" :
           (nType == CARD_TYPE_SUMMON) ? "Summon" : "";
}

string PrintDeckType (int nType)
{
    return (nType == CARD_DECK_TYPE_ANGELS) ? "Angels" :
           (nType == CARD_DECK_TYPE_ANIMALS) ? "Animals" :
           (nType == CARD_DECK_TYPE_BIG_CREATURES) ? "Big Creatures" :
           (nType == CARD_DECK_TYPE_FAST_CREATURES) ? "Fast Creatures" :
           (nType == CARD_DECK_TYPE_GOBLINS) ? "Goblins" :
           (nType == CARD_DECK_TYPE_KOBOLDS) ? "Kobolds" :
           (nType == CARD_DECK_TYPE_RATS) ? "Rats" :
           (nType == CARD_DECK_TYPE_SPELLS) ? "Spells" :
           (nType == CARD_DECK_TYPE_UNDEAD) ? "Undead" :
           (nType == CARD_DECK_TYPE_WOLVES) ? "Wolves" : "";
}

string PrintDiscardPile (int nPlayer, object oArea)
{
    int nMax = GetDiscardPileSize (nPlayer, oArea);

    string sPrint = "* Top to Bottom *";

    struct sCard sPrintOut;

    while (nMax > 0)
    {
        sPrintOut = GetCardInfo (GetDiscardPile (nMax--, nPlayer, oArea));

        sPrint += "\n  " + sPrintOut.sName;
    }

    return sPrint + "\n* End of Discard Pile *";
}

string PrintPlayerGeneratorCounts (object oArea)
{
    return "Magic Count: " +
           "\n  " + GetName (GetCardGamePlayer (1, oArea)) + " " + IntToString (GetHasGenerators (1, oArea)) +
           "\n  " + GetName (GetCardGamePlayer (2, oArea)) + " " + IntToString (GetHasGenerators (2, oArea));
}

string PrintPlayerRating (object oPlayer)
{

  int nPlayer, nNth = 1;
  string sMessage;

  // get PC scores (only in card areas for performance reasons)
  object oPC = GetFirstPC();
  while (oPC != OBJECT_INVALID)
  {
    if( ((GetTag(GetArea(oPC)) == "CardGame") ||
        (GetArea(oPC) == GetArea(oPlayer))) &&
        (GetIsDM(oPC) == FALSE) )
    {
      nPlayer++;
      SetLocalInt(OBJECT_SELF, "PLAYER_SCORE" + IntToString(nPlayer), GetPlayerRating(oPC));
      SetLocalString(OBJECT_SELF, "PLAYER_NAME" + IntToString(nPlayer), GetName(oPC));
    }
    oPC = GetNextPC();
  }


  // get NPC scores
  object oNPC = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oPlayer, nNth);
  while(oNPC != OBJECT_INVALID)
  {

    if(GetLocalInt(oNPC, "Ranking") > 0)
    {
      nPlayer++;
      SetLocalInt(OBJECT_SELF, "PLAYER_SCORE" + IntToString(nPlayer), GetLocalInt(oNPC, "Ranking"));
      SetLocalString(OBJECT_SELF, "PLAYER_NAME" + IntToString(nPlayer), GetName(oNPC));
    }
    nNth++;
    oNPC = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oPlayer, nNth);
  }

    sMessage =  "Your Rating: " + PrintRating (GetPlayerRating (oPlayer)) +
                " (" + IntToString(GetPlayerRating (oPlayer)) + ")" +
                "\n \n * TOP 10 SCORES * \n";


   // sort items
   int nMaxScore, nMax = 10, nFound = TRUE, nMaxPlayer;
   int nScore = 1, nScoreCount = 1;
   if(nMax > nPlayer)
     nMax = nPlayer;


   while((nScoreCount <= nMax) && (nFound == TRUE))
   {
    if(GetIsDebug())
      SendMessageToPC(GetFirstPC(), "nScoreCount: " + IntToString(nScoreCount));

     nFound = FALSE;
     nNth = 1;
     nMaxPlayer = 0;
     nMaxScore = 0;
     while(nNth <= nPlayer)
     {
       nScore = GetLocalInt(OBJECT_SELF, "PLAYER_SCORE" + IntToString(nNth));
       if( (nScore >= nMaxScore) &&
           (GetLocalInt(OBJECT_SELF, "PLAYER_SCORE_PRINTED" + IntToString(nNth)) == 0) )
       {
         nMaxPlayer = nNth;
         nMaxScore = nScore;
         nFound = TRUE;
       }
       nNth++;
     }
     if(nFound)
     {
       nScoreCount++;
       SetLocalInt(OBJECT_SELF, "PLAYER_SCORE_PRINTED" + IntToString(nMaxPlayer), 1);
       sMessage = sMessage + "\n " + GetLocalString(OBJECT_SELF, "PLAYER_NAME" + IntToString(nMaxPlayer)) +
        " (" + IntToString(GetLocalInt(OBJECT_SELF, "PLAYER_SCORE" + IntToString(nMaxPlayer))) + ")";
     }
   }

     nNth = 1;
     while(nNth <= nMax)
     {
        DeleteLocalInt(OBJECT_SELF, "PLAYER_SCORE" + IntToString(nNth));
        DeleteLocalInt(OBJECT_SELF, "PLAYER_SCORE_PRINTED" + IntToString(nNth));
        DeleteLocalString(OBJECT_SELF, "PLAYER_NAME" + IntToString(nNth));
       nNth++;
     }



   return sMessage;
}

string PrintRating (int nRating)
{
   //rewritten by Adam
    string sRating = "Newbie";
    if(nRating >= CARD_PLAYER_RATING_APPRENTICE)
      sRating = "Apprentice";
    if(nRating >= CARD_PLAYER_RATING_NOVICE)
      sRating = "Novice";
    if(nRating >= CARD_PLAYER_RATING_PLAYER)
      sRating = "Player";
    if(nRating >= CARD_PLAYER_RATING_EXPERT)
      sRating = "Expert";
    if(nRating >= CARD_PLAYER_RATING_MASTER)
      sRating = "Master";
    if(nRating >= CARD_PLAYER_RATING_COMPETITOR)
      sRating = "Competitor";
    if(nRating >= CARD_PLAYER_RATING_GRANDMASTER)
      sRating = "Grandmaster";
    if(nRating >= CARD_PLAYER_RATING_ELITE)
      sRating = "Elite";
    return sRating;
}

string PrintRules (int nRules)
{
    int nNth, nType;

    string sPrint;

    for (nType = 1; nType <= 6; nType++)
        for (nNth = 0; nNth <= 7; nNth++)
        {
            int nValue;

            string sType;

            switch (nType)
            {
                case 1:
                    nValue = (nNth == 0) ? CARD_RULE_DECK_20 :
                             (nNth == 1) ? CARD_RULE_DECK_30 :
                             (nNth == 2) ? CARD_RULE_DECK_40 :
                             (nNth == 3) ? CARD_RULE_DECK_50 :
                             (nNth == 4) ? CARD_RULE_DECK_60 :
                             (nNth == 5) ? CARD_RULE_NORMAL : 0;

                    sType = (nNth == 0) ? "Deck Size Limit: 20" :
                            (nNth == 1) ? "Deck Size Limit: 30" :
                            (nNth == 2) ? "Deck Size Limit: 40" :
                            (nNth == 3) ? "Deck Size Limit: 50" :
                            (nNth == 4) ? "Deck Size Limit: 60" :
                            (nNth == 5) ? "Deck Size Limit: " + IntToString (CARD_GAME_DEFAULT_MAX_DECK_SIZE) : "";

                    break;

                case 2:
                    nValue = (nNth == 0) ? CARD_RULE_DRAW_3 :
                             (nNth == 1) ? CARD_RULE_DRAW_5 :
                             (nNth == 2) ? CARD_RULE_DRAW_7 :
                             (nNth == 3) ? CARD_RULE_DRAW_9 :
                             (nNth == 4) ? CARD_RULE_NORMAL : 0;

                    sType = (nNth == 0) ? "Hand Size Limit: 3" :
                            (nNth == 1) ? "Hand Size Limit: 5" :
                            (nNth == 2) ? "Hand Size Limit: 7" :
                            (nNth == 3) ? "Hand Size Limit: 9" :
                            (nNth == 4) ? "Hand Size Limit: " + IntToString (CARD_GAME_DEFAULT_MAX_HAND_SIZE) : "";

                    break;

                case 3:
                    nValue = (nNth == 0) ? CARD_RULE_RESTRICT_2 :
                             (nNth == 1) ? CARD_RULE_RESTRICT_3 :
                             (nNth == 2) ? CARD_RULE_RESTRICT_4 :
                             (nNth == 3) ? CARD_RULE_RESTRICT_X :
                             (nNth == 4) ? CARD_RULE_NORMAL : 0;

                    sType = (nNth == 0) ? "Individual Card Type Limit: 2" :
                            (nNth == 1) ? "Individual Card Type Limit: 3" :
                            (nNth == 2) ? "Individual Card Type Limit: 4" :
                            (nNth == 3) ? "Individual Card Type Limit: None" :
                            (nNth == 4) ? "Individual Card Type Limit: " + IntToString (CARD_GAME_DEFAULT_MAX_CARDS_TYPE) : "";

                    break;

                case 4:
                    nValue = (nNth == 0) ? CARD_RULE_LIMIT_1 :
                             (nNth == 1) ? CARD_RULE_LIMIT_2 :
                             (nNth == 2) ? CARD_RULE_LIMIT_3 :
                             (nNth == 3) ? CARD_RULE_LIMIT_4 :
                             (nNth == 4) ? CARD_RULE_LIMIT_5 :
                             (nNth == 5) ? CARD_RULE_LIMIT_8 :
                             (nNth == 6) ? CARD_RULE_LIMIT_10 :
                             (nNth == 7) ? CARD_RULE_NORMAL : 0;

                    sType = (nNth == 0) ? "Generator In-Play Limit: 1" :
                            (nNth == 1) ? "Generator In-Play Limit: 2" :
                            (nNth == 2) ? "Generator In-Play Limit: 3" :
                            (nNth == 3) ? "Generator In-Play Limit: 4" :
                            (nNth == 4) ? "Generator In-Play Limit: 5" :
                            (nNth == 5) ? "Generator In-Play Limit: 8" :
                            (nNth == 6) ? "Generator In-Play Limit: 10" :
                            (nNth == 7) ? "Generator In-Play Limit: " + IntToString (CARD_GAME_DEFAULT_MAX_GENERATORS) : "";

                    break;

                case 5:
                    nValue = (nNth == 0) ? CARD_RULE_TRADE_ONE :
                             (nNth == 1) ? CARD_RULE_TRADE_ALL :
                             (nNth == 2) ? CARD_RULE_NORMAL : 0;

                    sType = (nNth == 0) ? "Trade Rule: One Card" :
                            (nNth == 1) ? "Trade Rule: Entire Deck" :
                            (nNth == 2) ? "Trade Rule: None" : "";

                    break;

                case 6:
                    nValue = (nNth == 0) ? CARD_RULE_LAST_DRAW_CONTINUE :
                             (nNth == 1) ? CARD_RULE_LAST_DRAW_DEATH :
                             (nNth == 2) ? CARD_RULE_NORMAL : 0;

                    sType = (nNth == 0) ? "Last Draw Rule: Continue" :
                            (nNth == 1) ? "Last Draw Rule: End Game" :
                            (nNth == 2) ? "Last Draw Rule: End Game" : "";

                    break;
            }

            if (nValue && (nValue == CARD_RULE_NORMAL || nValue & nRules))
            {
                sPrint += "\n " + sType;

                break;
            }
        }

    return sPrint;
}

string PrintWhiteSpace (int nSpaces)
{
    string sReturn;

    while (--nSpaces >= 0)
        sReturn += " ";

    return sReturn;
}
