/**********************************/
/*          d1_cards_jcustom
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 1st March 2004
/**********************************/
/*  #include file
/*  Place all code for custom cards
/*  here.  Use of the below
/*  functions will prevent
/*  overwriting of custom content
/*  when a core game update is
/*  made.  Make all declarations
/*  to new functions or constants
/*  here.  NOTE: While this will
/*  prevent loss of code when
/*  updates are made, if there are
/*  new cards in an update, you
/*  will need to update your card
/*  number constants to take into
/*  account the new numbers of the
/*  new cards.
/**********************************/
/*  For examples of how to use
/*  these functions, locate the
/*  instance of this function in
/*  the main code for the core code
/*  use of it.
/**********************************/

void ActionUpkeepCustomStone (int nPlayer, object oStone)
{
    switch (GetStoneID (oStone))
    {
        default:    return;
    }
}

void ActionUseCustomAura (object oSelf, object oTarget, int nRemove = FALSE)
{
    switch (GetCardID (oSelf))
    {
        default:    return;
    }
}

void ActionUseCustomCreatureDeath (int nCard, int nPlayer, location lTarget)
{
    switch (nCard)
    {
        default:    DeleteLocalInt (OBJECT_SELF, "CARDS_CREATURE_CUSTOM_DEATH");    return;
    }
}

void ActionUseCustomCreatureKill (object oKiller, int nEnemy, location lTarget)
{
    switch (GetCardID (oKiller))
    {
        default:    return;
    }
}

void ActionUseCustomSacrifice (int nCard, int nUser, object oArea)
{
    switch (nCard)
    {
        default:    DeleteLocalInt (OBJECT_SELF, "CARDS_CREATURE_CUSTOM_SACRIFICE");    return;
    }
}

void ActionUseCustomSpawn (int nCard)
{
    switch (nCard)
    {
        /* STANDARD AURA CODE:
            ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectAreaOfEffect (AOE_MOB_CIRCGOOD, "d_card_auraa", "d_card_aurab", "d_card_aurac")), OBJECT_SELF);

            return;*/

        default:    return;
    }
}

void ActionUseCustomSpell (int nCard, int nPlayer, object oCentre)
{
    switch (nCard)
    {
        default:    return;
    }
}

void DoCustomCardDispel (object oTarget)
{

}

int GetAICustomCardEvaluation (struct sCard sInfo, int nMaxHand, int nMaxPower, int nPlayer, object oAvatar, object oCentre)
{
    int nRating;
    // fall-back rating system
    nRating = sInfo.nMagic * 2;
    return nRating;
}

int GetAICustomSacrificeEvaluation (struct sCard sInfo, int nMaxPower, int nPlayer, object oAvatar, object oCreature)
{
    int nRating;
    return nRating;
}

int GetCreatureFailedCustomUpkeep (int nCard, int nPlayer, object oCreature, object oCentre)
{
    switch (nCard)
    {
        default:    return FALSE;
    }

    return FALSE;
}

int GetHasCustomGlobalEffect (object oArea)
{
    return 0;
}

struct sCard GetCustomCardInfo (int nCard)
{
    struct sCard sReturn;

    sReturn.nCard = nCard;

    switch (nCard)
    {
        default:    return sReturn;
    }

    return sReturn;
}
