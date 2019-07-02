/**********************************/
/*          d1_cards_jdec
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Declarations
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

struct sAI
{
    int nPlay;      //Used by the AI.  Not a database structure.
    int nRating;
    int nCost;
};

struct sCard
{
    string sName;       //Card Name
    string sDesc;       //Card Descriptive Text
    string sGame;       //Description of Game Effect of Card

    int nCard;          //Card ID Value
    int nDeck;          //Deck Types this card will be used in.
    int nType;          //Type of Card
    int nSubType;       //Sub-type of Card
    int nCost;          //Cost of card when sold.  Value is doubled for purchase from the game owner.
    int nRarity;        //Rarity of Card

    int nMagic;         //Magic Cost When Cast (+ cards just have their minimum cost here)
    int nAttack;        //Attack Value if a Summon card.  Needed for AI and target scanning.
    int nDefend;        //Defend Value if a Summon card.  Needed for AI and target scanning.

    int nBoost;         //Set to TRUE if card uses more magic when cast than its magic cost.
    int nCombat;        //Set to TRUE if card should engage in combat.
    int nHandDiscard;   //Set to TRUE if card should not go to discard pile when discarded from hand.
    int nNoDiscard;     //Set to TRUE if card should not go to discard pile when killed.
    int nSacrifice;     //Set to TRUE if card has a sacrifice effect.
};

/*  ACTION FUNCTIONS  */

void ActionAddPowerToPool (int nPower, int nPlayer, object oArea, int nBroadcast = TRUE);
void ActionAI();
void ActionBeginGame (object oPlayer1, object oPlayer2, object oArea);
void ActionChangeMusic (int nChangeType, object oArea = OBJECT_SELF);
void ActionDestroyCard (int nCard, int nPlayer, object oArea, int nNth = 1);
void ActionDestroyPower (int nAmount, int nPlayer, object oArea);
void ActionDiscardPileToHand (int nCard, int nPlayer, object oArea);
void ActionDrawCards (int nPlayer, int nTurn = FALSE);
void ActionDrawSpecificCard (int nCardID, int nPlayer, object oArea, int nNaturalDraw = TRUE);
void ActionEndCardGame (int nEndGame, int nPlayer, object oArea);
void ActionEndPrep (int nEnding);
void ActionLayoutCards (location lStart, int nLayoutSource, int nRows, int nColumns, object oSource);
void ActionLayoutHand (int nHandSize, int nPlayer, object oCentre);
void ActionPlayCard (object oCard);
void ActionPurchaseCards (object oPlayer, int nNumber);
void ActionRemoveCards (object oDeck, int nCard, int nNth, int nAllCards = FALSE, object oPlayer = OBJECT_INVALID);
void ActionRemoveCardsCopy (object oDeck, object oPlayer);
void ActionSpawnAvatar (location lLoc, int nPlayer, object oPlayer);
void ActionStartCardGame (object oPlayer1, object oPlayer2, int nVariants = CARD_RULE_NORMAL);
void ActionStopManagingCards (object oArea);
void ActionTransferCard (int nCard, int nSource, int nDestination, object oSource, object oDestination, int nNth = 1);
void ActionUpdateAvatarHealth (object oAvatar = OBJECT_SELF);
void ActionUpdateMagicPool (int nPlayer, object oArea);
void ActionUpkeepCreature (object oCreature);
void ActionUpkeepCustomStone (int nPlayer, object oStone);
void ActionUpkeepCycle (int nTurn);
void ActionUpkeepStone (object oStone);
void ActionUseAura (object oSelf, object oTarget, int nRemove = FALSE);
void ActionUseCreatureDeath (object oKiller);
void ActionUseCustomAura (object oSelf, object oTarget, int nRemove = FALSE);
void ActionUseCustomCreatureDeath (int nCard, int nPlayer, location lTarget);
void ActionUseCustomCreatureKill (object oKiller, int nEnemy, location lTarget);
void ActionUseCustomSacrifice (int nCard, int nUser, object oArea);
void ActionUseCustomSpawn (int nCard);
void ActionUseCustomSpell (int nCard, int nPlayer, object oCentre);
void ActionUseGenerator (int nCard, int nPlayer, object oArea);
void ActionUsePower (int nAmount, int nPlayer, object oArea);
void ActionUseSacrifice (int nUser);
void ActionUseSpawn();
void ActionUseSpell (int nCard, int nPlayer, object oArea);
void ActionUseStone (int nCard, int nPlayer, object oArea);
void ActionUseSummon (int nCard, int nPlayer, object oArea, int nCombat = TRUE);

/*  AI EVALUATION FUNCTIONS  */

int AIEvaluateCardAngelicChoir (int nMaxHand, int nPlayer);
int AIEvaluateCardAngelicHealer (int nMaxHand, int nPlayer, object oAvatar);
int AIEvaluateCardAngelicLight (int nMaxHand, int nPlayer);
int AIEvaluateCardArchangel (int nMaxHand, int nPlayer);
int AIEvaluateCardArmour (int nPlayer, object oAvatar);
int AIEvaluateCardAssassin (int nPlayer, int nEnemy, object oCentre);
int AIEvaluateCardAtlantian (int nMaxPower);
int AIEvaluateCardAvengingAngel (int nMaxHand, int nPlayer);
int AIEvaluateCardBear (int nMaxHand, int nPlayer);
int AIEvaluateCardBeholder (int nMaxHand);
int AIEvaluateCardBoneGolem (int nMaxHand, int nPlayer);
int AIEvaluateCardBoomerang (int nEnemy, object oCentre);
int AIEvaluateCardBulette (int nEnemy);
int AIEvaluateCardChaosWitch (int nEnemy, object oCentre);
int AIEvaluateCardCounterspell (int nEnemy);
int AIEvaluateCardDeathPact (object oAvatar, object oCentre);
int AIEvaluateCardDispelMagic (int nMaxHand, int nPlayer, int nEnemy, object oAvatar, object oEnemy, object oCentre);
int AIEvaluateCardDragon (int nPlayer, object oAvatar, object oCentre);
int AIEvaluateCardDruid (int nPlayer, object oCentre);
int AIEvaluateCardElixirOfLife (int nMaxPower, int nPlayer, object oAvatar, object oCentre);
int AIEvaluateCardEnergyDisruption (int nPlayer, int nEnemy);
int AIEvaluateCardEyeOfTheBeholder (int nMaxHand, int nPlayer, int nEnemy);
int AIEvaluateCardFairyDragon (int nMaxHand, int nPlayer);
int AIEvaluateCardFeralRat (int nMaxHand, int nPlayer);
int AIEvaluateCardFireball (int nMaxPower, int nEnemy, object oEnemy);
int AIEvaluateCardFireShield (int nPlayer, object oAvatar);
int AIEvaluateCardFlux (int nMaxHand, int nPlayer, int nEnemy, object oAvatar, object oEnemy);
int AIEvaluateCardGoblin (int nMaxHand, int nPlayer);
int AIEvaluateCardGoblinWarlord (int nMaxHand, int nPlayer);
int AIEvaluateCardGoblinWitchdoctor (int nMaxHand, int nPlayer, object oAvatar);
int AIEvaluateCardHealingLight (int nPlayer, object oAvatar);
int AIEvaluateCardHigherCalling (int nPlayer, object oCentre);
int AIEvaluateCardHolyVengeance (int nPlayer);
int AIEvaluateCardIntellectDevourer (int nMaxHand, int nPlayer, int nEnemy);
int AIEvaluateCardJysirael (int nMaxHand, int nPlayer, object oCentre);
int AIEvaluateCardKobold (int nMaxHand, int nPlayer);
int AIEvaluateCardKoboldChief (int nMaxHand, int nPlayer);
int AIEvaluateCardKoboldEngineer (int nMaxHand, int nPlayer, int nEnemy, object oCentre);
int AIEvaluateCardKoboldPogostick (int nMaxHand, int nPlayer, int nEnemy, object oEnemy);
int AIEvaluateCardLich (int nPlayer, object oCentre);
int AIEvaluateCardLifeDrain (int nEnemy, object oAvatar);
int AIEvaluateCardLightningBolt (int nEnemy, object oEnemy);
int AIEvaluateCardLoremaster (int nPlayer, object oCentre);
int AIEvaluateCardMaidenOfParadise (int nMaxHand, int nPlayer);
int AIEvaluateCardMindControl (int nMaxPower, int nPlayer, object oCentre);
int AIEvaluateCardMindOverMatter (int nPlayer, object oAvatar, object oCentre);
int AIEvaluateCardPainGolem (int nEnemy, object oCentre);
int AIEvaluateCardParalyze (int nPlayer, int nEnemy, object oCentre);
int AIEvaluateCardPitFiend (int nPlayer);
int AIEvaluateCardPlagueBearer (int nMaxHand, int nPlayer, object oCentre);
int AIEvaluateCardPotionOfHeroism (int nPlayer);
int AIEvaluateCardPowerStream (int nPlayer);
int AIEvaluateCardRatKing (int nMaxHand, int nPlayer);
int AIEvaluateCardResurrect (int nMaxPower, int nPlayer, object oAvatar, object oCentre);
int AIEvaluateCardSabotage (int nEnemy);
int AIEvaluateCardSaeshen (int nMaxHand, int nPlayer);
int AIEvaluateCardScorchedEarth (int nPlayer, object oCentre);
int AIEvaluateCardSeaHag (int nEnemy);
int AIEvaluateCardSimulacrum (int nPlayer, object oCentre);
int AIEvaluateCardSkeleton (int nMaxHand, int nPlayer);
int AIEvaluateCardSolarStone (int nMaxHand, int nPlayer, int nEnemy);
int AIEvaluateCardSpiritGuardian (object oAvatar);
int AIEvaluateCardTroglodyte (int nEnemy);
int AIEvaluateCardVampireMaster (object oAvatar);
int AIEvaluateCardVortex (int nPlayer);
int AIEvaluateCardWarpReality (int nPlayer, object oCentre);
int AIEvaluateCardWhiteStag (int nMaxHand, int nPlayer, object oAvatar);
int AIEvaluateCardWolf (int nMaxHand, int nPlayer);
int AIEvaluateCardWrathOfTheHorde (int nPlayer);
int AIEvaluateCardZombie (int nMaxHand, int nPlayer);
int AIEvaluateCardZombieLord (int nMaxHand, int nPlayer);
int AIEvaluateSacrificeCougar (int nEnemy, object oCreature);
int AIEvaluateSacrificeCow (object oAvatar);
int AIEvaluateSacrificeDeekin (int nPlayer, int nEnemy, object oEnemy);
int AIEvaluateSacrificeDemonKnight (int nEnemy, object oCreature);
int AIEvaluateSacrificeFairyDragon (int nPlayer, object oCentre);
int AIEvaluateSacrificeGoblinWitchdoctor (int nPlayer, object oAvatar);
int AIEvaluateSacrificeHookHorror (int nPlayer, object oEnemy);
int AIEvaluateSacrificeIntellectDevourer (int nPlayer, int nEnemy);
int AIEvaluateSacrificeJysirael (int nPlayer, object oCreature);
int AIEvaluateSacrificeKoboldKamikaze (int nEnemy, object oEnemy, object oCreature);
int AIEvaluateSacrificeMaidenOfParadise (int nPlayer);
int AIEvaluateSacrificeSpiritGuardian (object oAvatar);
int AIEvaluateSacrificeUmberHulk (int nEnemy);

/*  DO FUNCTIONS  */

void DoAuraAngelicDefender (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraArchangel (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraBoneGolem (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraDruid (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraGoblinWarlord (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraKoboldChief (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraKoboldEngineer (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraLich (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraPlagueBearer (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraRatKing (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraSteelGuardian (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraWhiteWolf (object oSelf, object oTarget, int nRemove = FALSE);
void DoAuraWolf (object oSelf, object oTarget, int nRemove);
void DoAuraZombieLord (object oSelf, object oTarget, int nRemove);
void DoCardAngelicChoir (int nCaster, object oCentre);
void DoCardArmour (int nCaster, object oCentre);
void DoCardAssassin (int nCaster, object oCentre);
void DoCardBoomerang (int nCaster, object oCentre);
void DoCardCounterspell (int nCaster, object oCentre);
void DoCardDeathPact (int nCaster, object oCentre);
void DoCardDispelMagic (int nCaster, object oCentre);
void DoCardElixirOfLife (int nCaster, object oCentre);
void DoCardEnergyDisruption (int nCaster, object oCentre);
void DoCardEyeOfTheBeholder (int nCaster, object oCentre);
void DoCardFireball (int nCaster, object oCentre);
void DoCardFireShield (int nCaster, object oCentre);
void DoCardFlux (int nCaster, object oCentre);
void DoCardHealingLight (int nCaster, object oCentre);
void DoCardHigherCalling (int nCaster, object oCentre);
void DoCardHolyVengeance (int nCaster, object oCentre);
void DoCardLifeDrain (int nCaster, object oCentre);
void DoCardLightningBolt (int nCaster, object oCentre);
void DoCardMindControl (int nCaster, object oCentre);
void DoCardMindOverMatter (int nCaster, object oCentre);
void DoCardParalyze (int nCaster, object oCentre);
void DoCardPotionOfHeroism (int nCaster, object oCentre);
void DoCardPowerStream (int nPlayer, object oCentre);
void DoCardResurrect (int nCaster, object oCentre);
void DoCardSabotage (int nCaster, object oCentre);
void DoCardScorchedEarth (int nCaster, object oCentre);
void DoCardSimulacrum (int nCaster, object oCentre);
void DoCardVortex (int nCaster, object oCentre);
void DoCardWrathOfTheHorde (int nCaster, object oCentre);
void DoCardEffectAngelicChoir (int nBoost, object oTarget);
void DoCardEffectArmour (int nACBoost, object oTarget);
void DoCardEffectCounterspell (int nPlayer, object oAvatar);
void DoCardEffectDemonKnight (object oTarget);
void DoCardEffectDispelMagic (object oTarget);
void DoCardEffectFireball (int nTeamHarm, int nFireball, location lLoc);
void DoCardEffectFireShield (object oTarget);
void DoCardEffectHigherCalling (object oArea, object oCentre);
void DoCardEffectHolyVengeance (object oTarget);
void DoCardEffectKoboldEngineer (location lLoc);
void DoCardEffectKoboldPogostick (object oTarget);
void DoCardEffectMindControl (object oTarget, int nCanDispel = TRUE, int nReset = FALSE);
void DoCardEffectParalyze (object oTarget, float fxCycle = 3.0f);
void DoCardEffectPhasing (object oTarget, int nPhaseOut = TRUE);
void DoCardEffectPotionOfHeroism (object oTarget);
void DoCardEffectSimulacrum (int nCaster, object oTarget, object oCentre, object oAvatar);
void DoCardEffectWrathOfTheHorde (int nBoost, object oTarget);
void DoCustomCardDispel (object oTarget);
void DoDeathFireElemental (int nPlayer, location lTarget);
void DoKillByVampire (object oKiller, int nPlayer, location lTarget);
void DoKillByVampireMaster (object oKiller, int nPlayer, location lTarget);
void DoKillByZombieLord (object oKiller, int nPlayer, location lTarget);
void DoSacrificeCougar (int nPlayer, object oArea);
void DoSacrificeCow (int nPlayer, object oArea);
void DoSacrificeDeekin (int nPlayer, object oArea);
void DoSacrificeDemonKnight (int nPlayer, object oArea);
void DoSacrificeFaerieDragon (int nPlayer, object oArea);
void DoSacrificeGoblinWitchdoctor (int nPlayer, object oArea);
void DoSacrificeHookHorror (int nPlayer, object oArea);
void DoSacrificeIntellectDevourer (int nPlayer, object oArea);
void DoSacrificeKoboldKamikaze (int nPlayer, object oArea);
void DoSacrificeJysirael (int nPlayer, object oArea);
void DoSacrificeMaidenOfParadise (int nPlayer, object oArea);
void DoSacrificeRevenant (int nPlayer, object oArea);
void DoSacrificeShadowAssassin (int nPlayer, object oArea);
void DoSacrificeSpiritGuardian (int nPlayer, object oArea);
void DoSacrificeUmberHulk (int nPlayer, object oArea);
void DoSpawnAtlantian();
void DoSpawnDragon();
void DoSpawnKoboldPogostick();
void DoSpawnPhaseSpider();
void DoSpawnPitFiend();
void DoUpkeepDiscardPile (int nPlayer, object oCentre);
void DoUpkeepDrainAvatar (int nDrain, int nPlayer, object oCentre, object oCreature);
void DoUpkeepHealAvatar (int nHeal, int nCard, int nPlayer, object oCentre);
void DoUpkeepKoboldEngineer (int nPlayer, object oCentre, object oCreature);
void DoUpkeepPainGolem (int nPlayer, object oCentre, object oCreature);
void DoUpkeepSolarStone (int nPlayer, object oStone);

/*  MISC FUNCTIONS  */

void AddToCardsSold (int nRarity = 0, int nCards = 1);
void AdjustPlayerResults (int nAdjust, int nResult, object oOpponent, object oPlayer);
void ClearAllCards (object oWaypoint, int nPlayer = FALSE);
void ClearDiscardPile (int nPlayer, object oArea);
void ClearGameArea (object oArea = OBJECT_SELF);
void DestroyCardCreature (int nNoCorpse = FALSE, int nNoDiscard = FALSE, int nNoImmunity = TRUE, int nNoInvincibility = TRUE);
void DiscardOnKill (object oTarget);
void InitialiseRarity();
void RemoveEffectByType (int nType, object oTarget, object oSource = OBJECT_INVALID);
void RemoveFromDiscardPile (int nCard, int nPlayer, object oArea, int nTopDown = TRUE);
void ResetGameVariables (object oArea = OBJECT_SELF);
void SendMessageToCardPlayers (string sMessage, object oArea);
void SendToDiscardPile (int nCard, int nPlayer, object oArea);
int AlphaNumeric (string sLetter);
int CalculatePlayerRating (object oPlayer);
string PrintAvatarHealth (object oCentre);
string PrintCardRarity (int nRarity);
string PrintCardSubType (int nType);
string PrintCardType (int nType);
string PrintDeckType (int nType);
string PrintDiscardPile (int nPlayer, object oArea);
string PrintPlayerGeneratorCounts (object oArea);
string PrintPlayerRating (object oPlayer);
string PrintRating (int nRating);
string PrintRules (int nRules);
string PrintWhiteSpace (int nSpaces);

/*  SET FUNCTIONS  */

void SetCardsForAnte (int nNumber, int nCard, int nPlayer, object oArea);
void SetCardGameDeck (int nPlayer, object oDeck, object oArea);
void SetCardGamePlayer (object oPlayer, int nNumber, object oArea);
void SetCardGameToggle (int nToggle, object oArea);
void SetCardOwner (int nOwner, object oTarget);
void SetDeckForAnte (int nPlayer, object oDeck, object oArea);
void SetDefaultRules (int nRules);
void SetDeckVariables (object oDeck);
void SetDiscardPile (int nCard, int nPile, int nPlayer, object oArea);
void SetGameTurn (int nTurn, object oArea = OBJECT_SELF);
void SetGeneratorID (int nID, object oGenerator);
void SetHasCardEffect (int nCardID, object oTarget, int nHasEffect = TRUE);
void SetHasPlayedGenerator (int nPlayer, int nUsed, object oArea);
void SetMagicGenerator (object oGenerator, int nUsed);
void SetMagicPool (int nAmount, int nPlayer, object oArea);
void SetNPCAnteBet (int nAnte, object oNPC = OBJECT_SELF);
void SetNPCDeck (int nPlayer, object oArea, int nDecktype = CARD_DECK_TYPE_RANDOM);
void SetNPCFixedDeck (int nPlayer, object oArea, int nDecktype = CARD_DECK_TYPE_RANDOM);
void SetNPCCardAI (int nAIDifficulty, object oNPC = OBJECT_SELF);
void SetNPCDeckType (int nDeckType, object oNPC = OBJECT_SELF);
void SetNPCGoldBet (int nGold, object oNPC = OBJECT_SELF);
void SetOriginalOwner (int nOwner, object oObject);
void SetOwner (int nOwner, object oObject);
void SetPlayerRating (int nRating, object oPlayer);
void SetPlayerResults (int nResults, int nResultType, int nOpponentRating, object oPlayer);
void SetReturnLocation (location lLoc, object oPlayer);
void SetSpawnBoost (int nBoost, object oTarget = OBJECT_SELF);
void SetStoneID (int nID, object oStone);
void SetTotalCardsSold (int nTotal, int nRarity);
void SetVariantRules (int nRules, object oArea);

/*  GET FUNCTIONS  */

int GetAICardEvaluation (struct sCard sInfo, int nMaxHand, int nMaxPower, int nPlayer, object oAvatar, object oCentre);
int GetAICustomCardEvaluation (struct sCard sInfo, int nMaxHand, int nMaxPower, int nPlayer, object oAvatar, object oCentre);
int GetAICustomSacrificeEvaluation (struct sCard sInfo, int nMaxPower, int nPlayer, object oAvatar, object oCreature);
int GetAIDifficulty (object oNPC);
int GetAIEvaluation (struct sCard sInfo, int nMaxHand, int nMaxPower, int nPlayer, object oAvatar, object oCentre);
int GetAISacrificeEvaluation (struct sCard sInfo, int nMaxPower, int nPlayer, object oAvatar, object oCreature);
object GetCardBag (object oPlayer, int nCheckIfFull = TRUE, int nCreateBag = FALSE);
int GetCardDeckType (object oNPC);
int GetCardGamePlayerNumber (object oPlayer);
int GetCardGameToggle (object oArea);
int GetCardID (object oCard);
int GetCardMaximum (object oArea);
int GetCardOwner (object oTarget);
int GetCardsForAnte (int nCard, int nPlayer, object oArea);
int GetCardsInDeck (int nCard, object oDeck);
int GetCardsInHand (int nPlayer, object oCentre, int nNth = 1);
int GetCardItemsInDeck (object oDeck, int nTypes = FALSE);
int GetCreatureFailedCustomUpkeep (int nCard, int nPlayer, object oCreature, object oCentre);
int GetCreatureFailedUpkeep (int nCard, int nPlayer, object oCreature, object oCentre);
int GetDeckMaximum (object oArea);
int GetDeckTagValidation (object oPlayer);
int GetDefaultRules();
int GetDiscardPile (int nPile, int nPlayer, object oArea);
int GetDiscardPileSize (int nPlayer, object oArea);
int GetDrawMaximum (object oArea);
int GetDrawnCard (int nPlayer, int nMaxSize, object oArea);
int GetDrawTerminate (object oArea = OBJECT_SELF);
int GetGameTurn (object oArea = OBJECT_SELF);
int GetGeneratorID (object oGenerator);
int GetGeneratorMaximum (object oArea);
int GetHasAssociates (object oPlayer);
int GetHasCardEffect (int nCardID, object oTarget);
int GetHasCreatures (int nScanType1, int nScanVar1, int nPlayer, object oArea, int nAmount = FALSE, int nScanType2 = FALSE, int nScanVar2 = FALSE, int nScanType3 = FALSE, int nScanVar3 = FALSE);
int GetHasCustomGlobalEffect (object oArea);
int GetHasGenerators (int nPlayer, object oArea, int nAmount = FALSE);
int GetHasPlayedGenerator (int nPlayer, object oArea);
int GetHasStones (int nStoneID, int nPlayer, object oArea, int nAmount = FALSE);
int GetIsAvatar (object oTarget = OBJECT_SELF);
int GetIsClone (object oTarget);
int GetIsDeckValid (object oDeck, object oArea);
int GetIsDebug ();
int GetIsGeneratorUsed (object oGenerator);
int GetIsMine (object oTarget);
int GetIsPowerAvailable (int nPlayer, object oArea, int nAmount = FALSE);
int GetIsVariantInPlay (int nRule, object oArea);
int GetLoremasterScan (int nPlayer, object oCentre);
int GetMagicPool (int nPlayer, object oArea);
int GetNPCAnteBet (object oNPC);
int GetNPCDeckType (object oNPC = OBJECT_SELF);
int GetNPCGoldBet (object oNPC);
int GetOriginalOwner (object oObject);
int GetOwner (object oObject);
int GetPercentHitPoints (int nPercent, object oTarget = OBJECT_SELF);
int GetPlayerRating (object oPlayer);
int GetPlayerResults (int nResultType, int nDifficulty, object oPlayer);
int GetRarityAllowed (int nRarity);
int GetRules (object oArea);
int GetSpawnBoost (object oTarget = OBJECT_SELF);
int GetStoneID (object oStone);
int GetTotalCards (int nSource, object oSource);
int GetTotalCardsSold (int nRarity);
int GetTotalGlobalSpells (object oArea);
string GetCardTag (int nCard, int nCreature = FALSE, int nStone = FALSE, int nGenerator = FALSE);
object GetAvatar (int nPlayer, object oReference);
object GetCardGameCreature (int nScanType1, int nScanVar1, int nPlayer, object oArea, int nScanType2 = FALSE, int nScanVar2 = FALSE, int nScanType3 = FALSE, int nScanVar3 = FALSE);
object GetCardGamePlayer (int nPlayer, object oArea);
object GetCardsArea ();
object GetDeck (object oPlayer, int nNth = 1, object oArea = OBJECT_INVALID);
object GetDeckForAnte (int nPlayer, object oArea);
object GetGameCentre (object oArea = OBJECT_SELF);
object GetReferenceObject (int nPlayer, object oCentre);
location GetReturnLocation (object oPlayer);
struct sCard GetCardInfo (int nCard);
struct sCard GetCustomCardInfo (int nCard);

/*  MENU FUNCTIONS  */

void ClearMenu();
void SetMenuCycle (int nValue);
void SetMenuCycleBack (int nValue);
void SetMenuCycleTotal (int nValue);
void SetMenuHasMore (int nMore = TRUE);
void SetMenuSelection (int nValue, int nID = 1);
void SetMenuSize (int nSize = 9);
void SetMenuValue (int nValue, int nMenuOption, int nID = 1);
void SetMenuObjectValue (object oObject, int nMenuOption, int nID = 1);
void SetMenuValueAmount (int nAmount = 1);
int GetMenuCycle();
int GetMenuCycleBack();
int GetMenuCycleTotal();
int GetMenuHasMore();
object GetMenuObjectValue (int nMenuOption, int nID = 1);
int GetMenuSelection (int nID = 1);
int GetMenuSize();
int GetMenuValue (int nMenuOption, int nID = 1);
int GetMenuValueAmount();
