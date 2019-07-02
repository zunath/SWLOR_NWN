/**********************************/
/*          d_card_aurab
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 27th October 2003
/**********************************/
/*  OnHeartbeat for aura effects.
/**********************************/
/*  Only used for adding a
/*  continuous effect in the
/*  creature's aura.
/**********************************/

#include "d1_cards_jinc"

void main()
{
    object oSelf = GetAreaOfEffectCreator();

    int nCard = GetCardID (oSelf);
    int nOwner = GetOwner (oSelf);

    if (nCard == CARD_SUMMON_PLAGUE_BEARER)
    {
        object oCycle = GetFirstInPersistentObject();

        while (oCycle != OBJECT_INVALID)
        {
            int nEnter = GetCardID (oCycle);
            int nEnemy = GetOwner (oCycle);

            if (oCycle != oSelf && nOwner != nEnemy)
                if (nEnter != CARD_SUMMON_FERAL_RAT
                    && nEnter != CARD_SUMMON_PLAGUE_BEARER
                    && nEnter != CARD_SUMMON_RAT
                    && nEnter != CARD_SUMMON_RAT_KING)
                    ApplyEffectToObject (DURATION_TYPE_INSTANT, ExtraordinaryEffect (EffectLinkEffects (EffectVisualEffect (VFX_IMP_DISEASE_S), EffectDamage (2, DAMAGE_TYPE_NEGATIVE, DAMAGE_POWER_PLUS_FIVE))), oCycle);

            oCycle = GetNextInPersistentObject();
        }
    }
}
