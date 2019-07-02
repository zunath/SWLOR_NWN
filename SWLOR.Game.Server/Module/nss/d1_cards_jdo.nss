/**********************************/
/*          d1_cards_jdo
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Do functions; Typically card
/*  effects.
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

void DoAuraAngelicDefender (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_ANGEL)
            if (nRemove)
                RemoveEffectByType (EFFECT_TYPE_DAMAGE_REDUCTION, oTarget, oSelf);
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectDamageReduction (1, DAMAGE_POWER_PLUS_FIVE))), oTarget);
}

void DoAuraArchangel (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);
    effect eAura = EffectLinkEffects (eVisual, EffectAttackIncrease (2));
    eAura = ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (2)));

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_ANGEL)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            }
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, eAura, oTarget);
}

void DoAuraBoneGolem (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);
    effect eAura = EffectLinkEffects (eVisual, EffectAttackIncrease (2));
    eAura = ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (2)));

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_SKELETON)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            }
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, eAura, oTarget);
}

void DoAuraDruid (object oSelf, object oTarget, int nRemove = FALSE)
{
    int nTarget = GetCardID (oTarget);
    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);
    effect eAura = EffectLinkEffects (eVisual, EffectAttackIncrease (2));
    eAura = ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (2)));

    if (nOwner == nEnemy)
        if (GetRacialType (oTarget) == RACIAL_TYPE_ANIMAL)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            }
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, eAura, oTarget);
}

void DoAuraGoblinWarlord (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);
    effect eAura = EffectLinkEffects (eVisual, EffectAttackIncrease (2));
    eAura = ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (2)));

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_GOBLIN)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            }
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, eAura, oTarget);
}

void DoAuraKoboldChief (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eHero = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE), EffectHaste());
    eHero = EffectLinkEffects (eHero, EffectAttackIncrease (5));
    eHero = ExtraordinaryEffect (EffectLinkEffects (eHero, EffectTemporaryHitpoints (50)));

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_KOBOLD)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_HASTE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_TEMPORARY_HITPOINTS, oTarget, oSelf);
            }
            else
            {
                ApplyEffectToObject (DURATION_TYPE_TEMPORARY, eHero, oTarget, CYCLE_TIME * 2.0f);
                ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_SUPER_HEROISM), oTarget);
            }
}

void DoAuraKoboldEngineer (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    if (sTarget.nSubType != CARD_SUBTYPE_SUMMON_KOBOLD)
        DelayCommand (5.0f, DoCardEffectKoboldEngineer (GetLocation (OBJECT_SELF)));
}

void DoAuraLich (object oSelf, object oTarget, int nRemove = FALSE)
{
    int nTarget = GetCardID (oTarget);
    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);

    if (nOwner == nEnemy)
        if (GetRacialType (oTarget) == RACIAL_TYPE_UNDEAD)
            if (nRemove)
                RemoveEffectByType (EFFECT_TYPE_SPELL_RESISTANCE_INCREASE, oTarget, oSelf);
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectSpellResistanceIncrease (20))), oTarget);
}

void DoAuraPlagueBearer (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);

    if (nOwner != nEnemy)
        if (sTarget.nSubType != CARD_SUBTYPE_SUMMON_RAT)
            if (nRemove)
                RemoveEffectByType (EFFECT_TYPE_DISEASE, oTarget, oSelf);
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (EffectVisualEffect (VFX_DUR_CESSATE_NEGATIVE), EffectDisease (DISEASE_VERMIN_MADNESS))), oTarget);
}

void DoAuraRatKing (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);
    effect eAura = EffectLinkEffects (eVisual, EffectAttackIncrease (2));
    eAura = ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (2)));

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_RAT)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            }
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, eAura, oTarget);
}

void DoAuraSteelGuardian (object oSelf, object oTarget, int nRemove = FALSE)
{
    int nTarget = GetCardID (oTarget);
    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);

    if (nOwner == nEnemy)
        if (nRemove)
            RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
        else
            ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (4))), oTarget);
}

void DoAuraWhiteWolf (object oSelf, object oTarget, int nRemove = FALSE)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_WOLF)
            if (nRemove)
                RemoveEffectByType (EFFECT_TYPE_HASTE, oTarget, oSelf);
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectHaste())), oTarget);
}

void DoAuraWolf (object oSelf, object oTarget, int nRemove)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_WOLF)
            if (nRemove)
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectAttackIncrease (1))), oTarget);
}

void DoAuraZombieLord (object oSelf, object oTarget, int nRemove)
{
    struct sCard sTarget = GetCardInfo (GetCardID (oTarget));

    int nOwner = GetOwner (oSelf);
    int nEnemy = GetOwner (oTarget);

    effect eVisual = EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE);
    effect eAura = EffectLinkEffects (eVisual, EffectAttackIncrease (2));
    eAura = ExtraordinaryEffect (EffectLinkEffects (eVisual, EffectACIncrease (2)));

    if (nOwner == nEnemy)
        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_ZOMBIE)
            if (nRemove)
            {
                RemoveEffectByType (EFFECT_TYPE_AC_INCREASE, oTarget, oSelf);
                RemoveEffectByType (EFFECT_TYPE_ATTACK_INCREASE, oTarget, oSelf);
            }
            else
                ApplyEffectToObject (DURATION_TYPE_PERMANENT, eAura, oTarget);
}

void DoCardAngelicChoir (int nCaster, object oCentre)
{
    int nNth = 1;
    int nCount, nApply;

    struct sCard sTarget;

    object oArea = GetArea (oCentre);
    object oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oTarget != OBJECT_INVALID)
    {

        sTarget = GetCardInfo (GetCardID (oTarget));

        if (sTarget.nSubType == CARD_SUBTYPE_SUMMON_ANGEL)
            if (GetOwner (oTarget) == nCaster)
            {
                SetLocalObject (OBJECT_SELF, "CARDS_ANGEL_TARGET_" + IntToString (++nCount), oTarget);
                nApply = TRUE;
            }

        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (nApply)
    {
        int nDown = nCount;

        while (nDown)
            DoCardEffectAngelicChoir (nCount, GetLocalObject (OBJECT_SELF, "CARDS_ANGEL_TARGET_" + IntToString (nDown--)));

        while (nDown <= nCount)
            DeleteLocalObject (OBJECT_SELF, "CARDS_ANGEL_TARGET_" + IntToString (++nDown));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardArmour (int nCaster, object oCentre)
{
    int nNth = 1;

    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oTarget = (GetHasCardEffect (CARD_SPELL_ARMOUR, oAvatar)) ? GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_ARMOUR) : oAvatar;

    if (oTarget != OBJECT_INVALID)
    {
        int nAC = (oTarget == oAvatar) ? 10 : GetAC (oTarget);
        int nBoost = (nAC - (nAC % 2)) / 2;

        DoCardEffectArmour (nBoost, oTarget);
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardAssassin (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea);

    if (oTarget != OBJECT_INVALID)
    {
        AssignCommand (oTarget, DestroyCardCreature());

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH_L), oTarget);
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardBoomerang (int nCaster, object oCentre)
{
    int nNth = 1;
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea);

    int nCard = GetCardID (oTarget);

    if (oTarget == OBJECT_INVALID && GetHasGenerators (nOpponent, oArea, 1))
    {
        int nUsed = GetIsPowerAvailable (nOpponent, oArea, 1);

        object oRemember = OBJECT_INVALID;

        oTarget = GetNearestObjectByTag ("d_magicgenerator", oCentre, nNth);

        while (oTarget != OBJECT_INVALID)
        {
            if (GetOwner (oTarget) == nOpponent)
            {
                oRemember = oTarget;

                if (!nUsed || (nUsed && !GetIsGeneratorUsed (oTarget)))
                    break;
            }

            oTarget = GetNearestObjectByTag ("d_magicgenerator", oCentre, ++nNth);
        }

        if (nUsed && oTarget == OBJECT_INVALID)
            oTarget = oRemember;

        nCard = CARD_GENERATOR_GENERIC;
    }

    if (oTarget != OBJECT_INVALID)
    {
        ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_PWSTUN), GetLocation (oTarget));

        struct sCard sInfo = GetCardInfo (nCard);

        int nOwner = GetCardOwner (oTarget);

        SetHasCardEffect (CARD_SPELL_BOOMERANG, oTarget);

        if (sInfo.nCard == CARD_GENERATOR_GENERIC)
        {
            SetPlotFlag (oTarget, FALSE);
            DestroyObject (oTarget);
        }
        else
            AssignCommand (oTarget, DestroyCardCreature (TRUE, TRUE));

        //Changed nOwner to nOpponent as magic generators caused all cards to vanish
        AssignCommand (oArea, ActionDrawSpecificCard (sInfo.nCard, nOpponent, oArea, FALSE));

        SendMessageToCardPlayers (sInfo.sName + " returned to hand.", oArea);
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardCounterspell (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oPlayer = GetAvatar (nOpponent, oCentre);

    int nLeft = GetHasCardEffect (CARD_SPELL_COUNTERSPELL, oPlayer) + 1;

    SendMessageToPC (GetCardGamePlayer (nOpponent, oArea), "You have " + IntToString (nLeft) + " counterspells in effect.");
    SendMessageToPC (GetCardGamePlayer (nCaster, oArea), IntToString (nLeft) + " counterspells on the opponent.");

    SetHasCardEffect (CARD_SPELL_COUNTERSPELL, oPlayer, nLeft);
}

void DoCardDeathPact (int nCaster, object oCentre)
{
    int nNth = 1;
    int nApplied;

    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, FALSE);

    if (oTarget != OBJECT_INVALID)
    {
        while (oTarget != OBJECT_INVALID && nNth <= 5)
        {
            nApplied += GetMaxHitPoints (oTarget);

            ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH), GetLocation (oTarget));

            AssignCommand (oTarget, DestroyCardCreature (TRUE, TRUE));

            oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, FALSE);
        }

        int nCap = GetMaxHitPoints (oAvatar) - GetCurrentHitPoints (oAvatar);
        nApplied = (nCap < nApplied) ? nCap : nApplied;

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (nApplied), oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_LOS_EVIL_10), oAvatar);

        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "Death Pact healed you by " + IntToString (nApplied) + " hit points.");
        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardDispelMagic (int nCaster, object oCentre)
{
    int nNth = 1;
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    DoCardEffectDispelMagic (GetAvatar (nCaster, oCentre));
    DoCardEffectDispelMagic (GetAvatar (nOpponent, oCentre));

    while (oTarget != OBJECT_INVALID)
    {
        DoCardEffectDispelMagic (oTarget);

        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    SetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, oArea, 0);
    SetHasCardEffect (CARD_SPELL_FLUX, oArea, 0);
    SetHasCardEffect (CARD_SPELL_POWER_STREAM, oArea, 0);
    SetHasCardEffect (CARD_SPELL_VORTEX, oArea, 0);
    SetHasCardEffect (CARD_SPELL_HIGHER_CALLING, oArea, 0);
}

void DoCardElixirOfLife (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oTarget = (GetCurrentHitPoints (oAvatar) <= GetPercentHitPoints (80, oAvatar)) ? oAvatar : GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_LIFE, nCaster, oArea);

    if (oTarget != OBJECT_INVALID)
    {
        int nNeed = GetMaxHitPoints (oTarget) - GetCurrentHitPoints (oTarget) + 10;
        nNeed = (nNeed - (nNeed % 10)) / 10;

        int nMana = GetIsPowerAvailable (nCaster, oArea);
        nMana = (nMana > nNeed) ? nNeed : nMana;

        ActionUsePower (nMana, nCaster, oArea);

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal ((nMana + 1) * 10), oTarget);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_X), oTarget);

        SignalEvent (oTarget, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardEnergyDisruption (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);

    SetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, oArea, GetHasCardEffect (CARD_SPELL_ENERGY_DISRUPTION, oArea) + 1);
}

void DoCardEyeOfTheBeholder (int nCaster, object oCentre)
{
    int nWorked;
    int nNth = 1;
    int nOpponent = (nCaster == 1) ? 2 : 1;

    struct sCard sScan;

    object oArea = GetArea (oCentre);
    object oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oTarget != OBJECT_INVALID)
    {
        sScan = GetCardInfo (GetCardID (oTarget));

        if (sScan.nSubType == CARD_SUBTYPE_SUMMON_BEHOLDER && GetOwner (oTarget) == nCaster)
        {
            int nScan, nBeam, nAvatar;

            string sMessage, sName;

            object oCycle = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_EYE_OF_THE_BEHOLDER);

            if (oCycle == OBJECT_INVALID)
            {
                oCycle = GetAvatar (nOpponent, oCentre);

                sName = GetName (GetCardGamePlayer (nOpponent, oArea));

                nAvatar = TRUE;
            }
            else
            {
                struct sCard sInfo = GetCardInfo (GetCardID (oCycle));

                sName = sInfo.sName;
            }

            switch (d10())
            {
                case 1:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        DoCardEffectMindControl (oCycle);

                        nBeam = VFX_BEAM_MIND;

                        sMessage = sScan.sName + " causes " + sName + " to swap sides.";
                    }

                    break;

                case 2:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        DoCardEffectParalyze (oCycle);

                        nBeam = VFX_BEAM_MIND;

                        sMessage = sScan.sName + " paralyzes " + sName;
                    }

                    break;

                case 3:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 2.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by half.";
                    }
                    else
                    {
                        AssignCommand (oCycle, DestroyCardCreature (TRUE, TRUE));

                        nBeam = VFX_BEAM_BLACK;

                        sMessage = sScan.sName + " disintegrates " + sName;
                    }

                    break;

                case 4:
                    nWorked = d8 (2) + 10;

                    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nWorked, DAMAGE_TYPE_MAGICAL), oCycle);

                    nBeam = VFX_BEAM_LIGHTNING;

                    sMessage = sScan.sName + " deals " + IntToString (nWorked) + " to " + sName;

                    break;

                case 5:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectSlow(), oCycle);

                        nBeam = VFX_BEAM_EVIL;

                        sMessage = sScan.sName + " slows " + sName;
                    }

                    break;

                case 6:
                    ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectACDecrease (FloatToInt (IntToFloat (GetAC (oCycle)) / 2.0f)), oCycle);

                    nBeam = VFX_BEAM_EVIL;

                    sMessage = sScan.sName + " halves " + sName + "'s armour class value.";

                    break;

                case 7:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectFrightened(), oCycle);

                        nBeam = VFX_BEAM_EVIL;

                        sMessage = sScan.sName + " frightens " + sName;
                    }

                    break;

                case 8:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectKnockdown(), oCycle);

                        nBeam = VFX_BEAM_EVIL;

                        sMessage = sScan.sName + " knocks down " + sName;
                    }

                    break;

                case 9:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectLinkEffects (EffectDeaf(), EffectBlindness()), oCycle);

                        nBeam = VFX_BEAM_EVIL;

                        sMessage = sScan.sName + " deafens and blinds " + sName;
                    }

                    break;

                case 10:
                    if (nAvatar)
                    {
                        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (oCycle)) / 4.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oCycle);

                        nBeam = VFX_BEAM_LIGHTNING;

                        sMessage = sScan.sName + " reduces " + sName + "'s avatars hit points by a quarter.";
                    }
                    else
                    {
                        ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectAttackDecrease (10), oCycle);

                        nBeam = VFX_BEAM_EVIL;

                        sMessage = sScan.sName + " reduces " + sName + "'s attack rating.";
                    }

                    break;
            }

            SendMessageToCardPlayers (sMessage, oArea);
            SignalEvent (oCycle, EventSpellCastAt (oArea, SPELL_FIREBALL));

            ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectBeam (nBeam, oTarget, BODY_NODE_CHEST), oCycle, 1.0f);

            SetHasCardEffect (CARD_SPELL_EYE_OF_THE_BEHOLDER, oCycle);
            AssignCommand (oCycle, DelayCommand (0.5, SetHasCardEffect (CARD_SPELL_EYE_OF_THE_BEHOLDER, oCycle, FALSE)));

            nWorked = TRUE;
        }

        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (!nWorked)
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardFireball (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea);

    if (oTarget == OBJECT_INVALID)
        oTarget = GetAvatar (nOpponent, oCentre);

    if (oTarget != OBJECT_INVALID)
    {
        int nFireball = 1 + GetIsPowerAvailable (nCaster, oArea);

        ActionUsePower (nFireball, nCaster, oArea);

        DoCardEffectFireball (nOpponent, nFireball, GetLocation (oTarget));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardFireShield (int nCaster, object oCentre)
{
    int nNth = 1;

    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oTarget = oAvatar;

    if (GetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oAvatar))
        oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_FIRE_SHIELD);

    if (oTarget != OBJECT_INVALID)
        DoCardEffectFireShield (oTarget);
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardFlux (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);

    SetHasCardEffect (CARD_SPELL_FLUX, oArea, GetHasCardEffect (CARD_SPELL_FLUX, oArea) + 1);
}

void DoCardHealingLight (int nCaster, object oCentre)
{
    int nNth = 1;

    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oTarget = oAvatar;

    if (oTarget != OBJECT_INVALID)
    {
        int nApplied = 30;

        object oCycle = oAvatar;

        while (nApplied > 0 && oTarget != OBJECT_INVALID)
        {
            int nNeeds = GetMaxHitPoints (oTarget) - GetCurrentHitPoints (oTarget);

            if (nNeeds > nApplied)
            {
                nNeeds = nApplied;
                nApplied = 0;
            }
            else
                nApplied -= nNeeds;

            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (nNeeds), oTarget);
            ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectBeam (VFX_BEAM_HOLY, oCycle, BODY_NODE_CHEST), oTarget, 2.0f);
            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_S), oTarget);

            //Uses temporary marker to prevent back healing.
            SetHasCardEffect (CARD_SPELL_HEALING_LIGHT, oTarget);
            AssignCommand (oTarget, DelayCommand (0.2, SetHasCardEffect (CARD_SPELL_HEALING_LIGHT, oTarget, FALSE)));

            SignalEvent (oTarget, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));

            oCycle = oTarget;
            oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_LIFE, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_HEALING_LIGHT);
        }
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardHigherCalling (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);

    SetHasCardEffect (CARD_SPELL_HIGHER_CALLING, oArea, 1);

    DoCardEffectHigherCalling (GetArea(oCentre), oCentre);
}


void DoCardHolyVengeance (int nCaster, object oCentre)
{
    int nNth = 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_HOLY_VENGEANCE);

    if (oTarget != OBJECT_INVALID)
    {
        while (oTarget != OBJECT_INVALID)
        {
            if (!GetHasCardEffect (CARD_SPELL_HOLY_VENGEANCE, oTarget))
            {
                DoCardEffectHolyVengeance (oTarget);

                break;
            }

            object oCycle = oTarget;
            oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_HOLY_VENGEANCE);

            while (oTarget != OBJECT_INVALID)
            {
                if (GetOwner (oTarget) == nCaster)
                    break;

                SetHasCardEffect (CARD_SPELL_HOLY_VENGEANCE, oTarget);
                AssignCommand (oTarget, DelayCommand (0.2, SetHasCardEffect (CARD_SPELL_HOLY_VENGEANCE, oTarget, FALSE)));

                oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_HOLY_VENGEANCE);
            }
        }
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardLifeDrain (int nCaster, object oCentre)
{
    int nNth = 1;
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oCycle;
    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oEnemy = GetAvatar (nOpponent, oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND_LIVING, nOpponent, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_LIFE_DRAIN);

    if (oTarget == OBJECT_INVALID)
        oTarget = oEnemy;

    if (oTarget != OBJECT_INVALID)
    {
        int nPowerDown, nDrain;
        int nPowerUp = 5 * (GetIsPowerAvailable (nCaster, oArea) + 1);
        int nToHeal = GetMaxHitPoints (oAvatar) - GetCurrentHitPoints (oAvatar);
        int nApplied = (nToHeal - (nToHeal % 5)) + 5;

        if (nApplied > nPowerUp)
            nApplied = nPowerUp;

        while (nApplied > 0 && oTarget != OBJECT_INVALID)
        {
            nDrain = GetCurrentHitPoints (oTarget);
            nDrain = (nDrain - (nDrain % 5)) + 5;

            if (nDrain > nApplied || oTarget == oEnemy)
            {
                nDrain = nApplied;
                nApplied = 0;
            }
            else
                nApplied -= nDrain;

            nPowerDown += nDrain / 5;

            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nDrain, DAMAGE_TYPE_NEGATIVE, DAMAGE_POWER_PLUS_FIVE), oTarget);
            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (nDrain), oAvatar);
            ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectBeam (VFX_BEAM_EVIL, oAvatar, BODY_NODE_CHEST), oTarget, 2.0f);
            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEAD_EVIL), oTarget);

            if (oCycle != OBJECT_INVALID)
                ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectBeam (VFX_BEAM_EVIL, oCycle, BODY_NODE_CHEST), oTarget, 2.0f);

            SetHasCardEffect (CARD_SPELL_LIFE_DRAIN, oTarget);
            AssignCommand (oTarget, DelayCommand (0.2, SetHasCardEffect (CARD_SPELL_LIFE_DRAIN, oTarget, FALSE)));

            SignalEvent (oTarget, EventSpellCastAt (oArea, SPELL_FIREBALL));

            oCycle = oTarget;
            oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND_LIVING, nOpponent, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_LIFE_DRAIN);

            if (oTarget == OBJECT_INVALID)
                oTarget = oEnemy;
        }

        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));

        if (nPowerDown - 1 > 0)
            ActionUsePower (nPowerDown - 1, nCaster, oArea);
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardLightningBolt (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_DEFEND, nOpponent, oArea);

    if (oTarget == OBJECT_INVALID)
        oTarget = GetAvatar (nOpponent, oCentre);

    if (oTarget != OBJECT_INVALID)
    {
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectLinkEffects (EffectDamage (20, DAMAGE_TYPE_ELECTRICAL, DAMAGE_POWER_ENERGY), EffectVisualEffect (VFX_IMP_LIGHTNING_M)), oTarget);

        SignalEvent (oTarget, EventSpellCastAt (oArea, SPELL_FIREBALL));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardMindControl (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);

    int nNth = 1;
    int nOpponent = (nCaster == 1) ? 2 : 1;
    int nPower = GetIsPowerAvailable (nCaster, oArea);

    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea);

    struct sCard sCheck = GetCardInfo (GetCardID (oTarget));

    if ((sCheck.nMagic + 1) > nPower)
    {
        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

        while (oTarget != OBJECT_INVALID)
        {
            sCheck = GetCardInfo (GetCardID (oTarget));

            if (sCheck.nCard && (sCheck.nMagic + 1) <= nPower && GetOwner (oTarget) == nOpponent)
                break;

            oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
        }
    }

    if (oTarget != OBJECT_INVALID)
    {
        DoCardEffectMindControl (oTarget);

        ActionUsePower (sCheck.nMagic + 1, nCaster, oArea);
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardMindOverMatter (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);

    int nSize = GetDiscardPileSize (nCaster, oArea);

    if (nSize)
    {
        int nRnd = Random (nSize) + 1;
        int nPile = GetDiscardPile (nRnd, nCaster, oArea);
        int nSelfCount, nContinue;

        while (nPile == CARD_SPELL_MIND_OVER_MATTER && nSelfCount != nSize)
        {
            SetLocalInt (OBJECT_SELF, "CARD_TEMP_IGNORE_PILE_ADDRESS_" + IntToString (++nSelfCount), nRnd);

            while (!nContinue || nContinue == nRnd)
            {
                int nMaxSelf = nSelfCount;
                nRnd = Random (nSize) + 1;
                nContinue = FALSE;

                while (--nMaxSelf >= 0 && nContinue != nRnd)
                    nContinue = GetLocalInt (OBJECT_SELF, "CARD_TEMP_IGNORE_PILE_ADDRESS_" + IntToString (nMaxSelf + 1));
            }

            nPile = GetDiscardPile (nRnd, nCaster, oArea);
        }

        if (nPile != CARD_SPELL_MIND_OVER_MATTER)
            ActionDiscardPileToHand (nPile, nCaster, oArea);
        else
            SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "Only Mind Over Matter cards in discard pile.  Cannot return Mind Over Matter from discard pile.");

        for (nRnd = 1; nRnd <= nSelfCount; nRnd++)
            DeleteLocalInt (OBJECT_SELF, "CARD_TEMP_IGNORE_PILE_ADDRESS_" + IntToString (nRnd));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardParalyze (int nCaster, object oCentre)
{
    int nApplied;
    int nNth = 1;
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_PARALYZE);

    if (oTarget != OBJECT_INVALID)
        DoCardEffectParalyze (oTarget);
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardPotionOfHeroism (int nCaster, object oCentre)
{
    int nNth = 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nCaster, oArea, CARD_SCAN_NO_EFFECT, CARD_SPELL_POTION_OF_HEROISM);

    if (oTarget != OBJECT_INVALID)
        DoCardEffectPotionOfHeroism (oTarget);
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardPowerStream (int nPlayer, object oCentre)
{
    object oArea = GetArea (oCentre);

    SetHasCardEffect (CARD_SPELL_POWER_STREAM, oArea, GetHasCardEffect (CARD_SPELL_POWER_STREAM, oArea) + 1);
}

void DoCardResurrect (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD, nCaster, oArea);

    struct sCard sInfo = GetCardInfo (GetCardID (oTarget));

    if (sInfo.nType == CARD_TYPE_MYTHICAL)
        if (GetHasCreatures (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea, 1, CARD_SCAN_CARD_ID, sInfo.nCard))
            oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD, nCaster, oArea, CARD_SCAN_IS_MYTHICAL, FALSE);

    if (oTarget != OBJECT_INVALID)
    {
        sInfo = GetCardInfo (GetCardID (oTarget));

        SetHasCardEffect (CARD_SPELL_RESURRECT, oTarget);

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectResurrection(), oTarget);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (GetMaxHitPoints (oTarget)), oTarget);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_RAISE_DEAD), oTarget);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_X), oTarget);

        SetOwner (nCaster, oTarget);

        if (sInfo.nCombat)
        {
            AssignCommand (oTarget, ClearAllActions());
            AssignCommand (oTarget, DetermineCombatRound());
        }
        else
            SetLocalInt (oTarget, "CARD_AI_BLOCK", TRUE);

        SendMessageToPC (GetCardGamePlayer (GetOwner (oTarget), oArea), "Resurrect targets " + sInfo.sName);

        SignalEvent (oTarget, EventSpellCastAt (oArea, SPELL_RAISE_DEAD, FALSE));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardSabotage (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);

    if (GetHasGenerators (nOpponent, oArea, 1))
        ActionDestroyPower (1, nOpponent, oArea);
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardScorchedEarth (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);
    object oTarget = GetFirstObjectInArea (oArea);

    while (oTarget != OBJECT_INVALID)
    {
        if (GetObjectType (oTarget) == OBJECT_TYPE_CREATURE
            && GetCardID (oTarget)
            && !GetIsAvatar (oTarget))
            {
                if (GetIsDead (oTarget))
                {
                    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_GAS_EXPLOSION_FIRE), oTarget);

                    AssignCommand (oTarget, DestroyCardCreature (TRUE, TRUE));
                }
                else
                {
                    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_FIREBALL), GetLocation (oTarget));
                    AssignCommand (oArea, DelayCommand (2.0f, ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_GAS_EXPLOSION_FIRE), oTarget)));

                    AssignCommand (oTarget, DestroyCardCreature (TRUE));
                }
            }

        oTarget = GetNextObjectInArea (oArea);
    }
}

void DoCardSimulacrum (int nCaster, object oCentre)
{
    int nOpponent = (nCaster == 1) ? 2 : 1;

    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nCaster, oCentre);
    object oTarget = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea);

    if (oTarget != OBJECT_INVALID)
        DoCardEffectSimulacrum (nCaster, oTarget, oCentre, oAvatar);
    else
        SendMessageToPC (GetCardGamePlayer (nCaster, oArea), "No valid target.  Spell wasted.");
}

void DoCardVortex (int nCaster, object oCentre)
{
    object oArea = GetArea (oCentre);

    SetHasCardEffect (CARD_SPELL_VORTEX, oArea, GetHasCardEffect (CARD_SPELL_VORTEX, oArea) + 1);
}

void DoCardWarpReality (int nCaster, object oCentre)
{
    int nNth = 1;
    object oArea = GetArea (oCentre);
    object oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oTarget != OBJECT_INVALID)
    {
        if (GetCardID (oTarget))
            DoCardEffectMindControl (oTarget, FALSE);

        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    ActionDestroyPower (-1, nCaster, oArea);
}

void DoCardWrathOfTheHorde (int nCaster, object oCentre)
{
    int nCount = 0;
    int nNth = 1;

    object oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    object oAvatar = GetAvatar (nCaster, oCentre);

    while (oTarget != OBJECT_INVALID)
    {
        if (GetCardID (oTarget) && GetOwner (oTarget) == nCaster)
            nCount += 1;

        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    if (nCount)
    {
        int nNth = 1;

        oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

        while (oTarget != OBJECT_INVALID)
        {
            if (GetCardID (oTarget) && GetOwner (oTarget) == nCaster && !GetHasCardEffect (CARD_SPELL_WRATH_OF_THE_HORDE, oTarget))
                DoCardEffectWrathOfTheHorde (nCount, oTarget);

            oTarget = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
        }
    }

    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_LOS_NORMAL_30), oAvatar);
}

void DoCardEffectAngelicChoir (int nBoost, object oTarget)
{
    effect eChoir = EffectLinkEffects (EffectVisualEffect (VFX_DUR_BARD_SONG), EffectACIncrease (nBoost));
    eChoir = EffectLinkEffects (eChoir, EffectAttackIncrease (nBoost));
    eChoir = EffectLinkEffects (eChoir, EffectDamageIncrease (nBoost));

    ApplyEffectToObject (DURATION_TYPE_PERMANENT, eChoir, oTarget);

    SetHasCardEffect (CARD_SPELL_ANGELIC_CHOIR, oTarget);
}

void DoCardEffectArmour (int nACBoost, object oTarget)
{
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectACIncrease (nACBoost, AC_DEFLECTION_BONUS), oTarget);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_MAGIC_PROTECTION), oTarget);

    SetHasCardEffect (CARD_SPELL_ARMOUR, oTarget);
}

void DoCardEffectCounterspell (int nPlayer, object oAvatar)
{
    object oArea = GetArea (oAvatar);

    int nOpponent = (nPlayer == 1) ? 2 : 1;
    int nLeft = GetHasCardEffect (CARD_SPELL_COUNTERSPELL, oAvatar) - 1;

    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DISPEL_DISJUNCTION), oAvatar);

    SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Your spell has been countered.  You have " + IntToString (nLeft) + " counterspells still in effect.");
    FloatingTextStringOnCreature("Counterspelled", GetCardGamePlayer (nPlayer, oArea));
    SendMessageToPC (GetCardGamePlayer (nOpponent, oArea), "One of your counterspells has just been used.  " + IntToString (nLeft) + " remaining on the opponent.");

    SetHasCardEffect (CARD_SPELL_COUNTERSPELL, oAvatar, nLeft);
}

void DoCardEffectDemonKnight (object oTarget)
{
    struct sCard sPrint = GetCardInfo (GetCardID (oTarget));

    if (sPrint.nSubType == CARD_SUBTYPE_SUMMON_DEMON)
        return;

    effect eEffect;

    int nGE = GetAlignmentGoodEvil (oTarget);
    int nLC = GetAlignmentLawChaos (oTarget);
    nGE = (nGE != ALIGNMENT_EVIL) ? ALIGNMENT_EVIL - nGE : nGE;
    nLC = (nLC != ALIGNMENT_CHAOTIC) ? ALIGNMENT_CHAOTIC - nLC : nLC;

    string sMessage = sPrint.sName;

    switch (nGE + nLC)
    {
        case 2:
            eEffect = EffectLinkEffects (EffectVisualEffect (VFX_DUR_PETRIFY), EffectPetrify());

            sMessage += " has been petrified.";

            break;

        case 3:
            eEffect = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_NEGATIVE), EffectConfused());

            sMessage += " has been confused.";

            SetHasCardEffect (CARD_SPELL_PARALYZE, oTarget);

            break;

        case 4:
            eEffect = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_NEGATIVE), EffectDamageDecrease (10));

            sMessage += " has had their damage decreased.";

            break;

        case 5:
            eEffect = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_NEGATIVE), EffectSlow());

            sMessage += " has been slowed.";

            break;

        case 6:
            eEffect = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_NEGATIVE), EffectAttackDecrease (10));

            sMessage += " has had their attack decreased.";

            break;

        case 7:
            eEffect = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_NEGATIVE), EffectSilence());

            sMessage += " has been silenced.";

            break;

    }

    ApplyEffectToObject (DURATION_TYPE_PERMANENT, eEffect, oTarget);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DOOM), oTarget);

    SetHasCardEffect (CARD_SUMMON_DEMON_KNIGHT, oTarget);

    SendMessageToCardPlayers (sMessage, GetArea (oTarget));
}

void DoCardEffectDispelMagic (object oTarget)
{
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_DISPEL), oTarget);

    if (GetIsClone (oTarget))
    {
        AssignCommand (oTarget, DestroyCardCreature (TRUE, TRUE));

        return;
    }

    effect eEffect = GetFirstEffect (oTarget);

    while (GetEffectType (eEffect) != EFFECT_TYPE_INVALIDEFFECT)
    {
        int nSub = GetEffectSubType (eEffect);

        if (nSub != SUBTYPE_EXTRAORDINARY && nSub != SUBTYPE_SUPERNATURAL)
            RemoveEffect (oTarget, eEffect);

        eEffect = GetNextEffect (oTarget);
    }

    if (GetHasCardEffect (CARD_SPELL_MIND_CONTROL, oTarget))
        DoCardEffectMindControl (oTarget, FALSE, TRUE);

    SetHasCardEffect (CARD_SPELL_ANGELIC_CHOIR, oTarget, FALSE);
    SetHasCardEffect (CARD_SPELL_ARMOUR, oTarget, FALSE);
    SetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oTarget, FALSE);
    SetHasCardEffect (CARD_SPELL_HOLY_VENGEANCE, oTarget, FALSE);
    SetHasCardEffect (CARD_SPELL_PARALYZE, oTarget, FALSE);
    SetHasCardEffect (CARD_SPELL_POTION_OF_HEROISM, oTarget, FALSE);
    SetHasCardEffect (CARD_SPELL_WRATH_OF_THE_HORDE, oTarget, FALSE);
    SetHasCardEffect (CARD_SUMMON_DEMON_KNIGHT, oTarget, FALSE);

    DoCustomCardDispel (oTarget);
}

void DoCardEffectFireball (int nTeamHarm, int nFireball, location lLoc)
{
    effect eFireball = EffectDamage (nFireball * 3, DAMAGE_TYPE_FIRE, DAMAGE_POWER_ENERGY);
    object oArea = GetAreaFromLocation (lLoc);
    object oFireball = GetFirstObjectInShape (SHAPE_SPHERE, RADIUS_SIZE_LARGE, lLoc);
    while (oFireball != OBJECT_INVALID)
    {
        if (GetOwner (oFireball) == nTeamHarm && !GetIsDead (oFireball))
        {    if (GetCardID (oFireball) || GetIsAvatar (oFireball))
            {
                AssignCommand(oArea, DelayCommand(0.05, ApplyEffectToObject (DURATION_TYPE_PERMANENT, eFireball, oFireball)));
                ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEAD_FIRE), oFireball);
                SignalEvent (oFireball, EventSpellCastAt (oArea, SPELL_FIREBALL));
            }
        }
        oFireball = GetNextObjectInShape (SHAPE_SPHERE, RADIUS_SIZE_LARGE, lLoc);
    }
    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_FIREBALL), lLoc);
}

void DoCardEffectFireShield (object oTarget)
{
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectLinkEffects (EffectVisualEffect (VFX_DUR_ELEMENTAL_SHIELD), EffectDamageShield (1, DAMAGE_BONUS_1d4, DAMAGE_TYPE_FIRE)), oTarget);

    SetHasCardEffect (CARD_SPELL_FIRE_SHIELD, oTarget);
}

void DoCardEffectHigherCalling (object oArea, object oCentre)
{
  effect eHigherCalling;
  eHigherCalling = EffectLinkEffects (EffectVisualEffect (VFX_DUR_CESSATE_NEGATIVE), EffectSlow ());
  eHigherCalling = EffectLinkEffects (EffectDamageDecrease(6), eHigherCalling);

    int nNth = 1;

    object oCreature = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    struct sCard sTarget = GetCardInfo (GetCardID (oCreature));

    while (oCreature != OBJECT_INVALID)
    {
        if(sTarget.nAttack > 2)
          ApplyEffectToObject (DURATION_TYPE_PERMANENT, eHigherCalling, oCreature);

        oCreature = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
        sTarget = GetCardInfo (GetCardID (oCreature));
    }

  SendMessageToCardPlayers ("Higher calling weakens all powerful creatures.", OBJECT_SELF);
}

void DoCardEffectHolyVengeance (object oTarget)
{
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, EffectLinkEffects (EffectVisualEffect (VFX_DUR_FREEDOM_OF_MOVEMENT), EffectDamageIncrease (4)), oTarget);

    SetHasCardEffect (CARD_SPELL_HOLY_VENGEANCE, oTarget);
}

void DoCardEffectKoboldEngineer (location lLoc)
{
    object oArea = GetAreaFromLocation (lLoc);
    object oDetonate = GetFirstObjectInShape (SHAPE_SPHERE, 5.0f, lLoc);

    while (oDetonate != OBJECT_INVALID)
    {
        if (GetObjectType (oDetonate) == OBJECT_TYPE_CREATURE
            && !GetIsDead (oDetonate)
            && !GetIsAvatar (oDetonate))
            {
                ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (d6 (10), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oDetonate);
                ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_COM_HIT_FIRE), oDetonate);

                SignalEvent (oDetonate, EventSpellCastAt (oArea, SPELL_FIREBALL));
            }

        oDetonate = GetNextObjectInShape (SHAPE_SPHERE, 5.0f, lLoc);
    }

    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_FIREBALL), lLoc);
    DestroyObject (OBJECT_SELF);
}

void DoCardEffectKoboldPogostick (object oTarget)
{
    int nPlayer = GetOwner (oTarget);
    int nOpponent = (nPlayer == 1) ? 2 : 1;

    object oArea = GetArea (oTarget);
    object oAttack = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nOpponent, oArea);

    if (oAttack == OBJECT_INVALID || d2() == 1)
        oAttack = GetAvatar (nOpponent, oTarget);

    FloatingTextStringOnCreature ("*boing*", oTarget, TRUE);

    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectDisappearAppear (GetLocation (oAttack)), oTarget, 3.5f);

    AssignCommand (oTarget, DelayCommand (3.5f, ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_SPIKE_TRAP), oAttack)));
    AssignCommand (oTarget, DelayCommand (3.5f, ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (25, DAMAGE_TYPE_BLUDGEONING, DAMAGE_POWER_PLUS_FIVE), oAttack)));
    AssignCommand (oTarget, DelayCommand (3.5f, SignalEvent (oAttack, EventSpellCastAt (oArea, SPELL_FIREBALL))));
    AssignCommand (oTarget, DelayCommand (3.6f, DestroyCardCreature (TRUE)));
}

void DoCardEffectMindControl (object oTarget, int nCanDispel = TRUE, int nReset = FALSE)
{
    int nOwner = GetOwner (oTarget);
    int nOpponent = (nReset) ? GetOriginalOwner (oTarget) :
                    (nOwner == 1) ? 2 : 1;

    SetOwner (nOpponent, oTarget);

                                          //-- 11/23/2004 bloodsong: clear combat flag added (TRUE)
                                          //-- also clear combat of the other combatant
    object oOther = GetAttackTarget(oTarget); //-- check if any anomalies if the target is an avatar
    ClearPersonalReputation(oTarget, oOther);
    ClearPersonalReputation(oOther, oTarget);
    AssignCommand (oTarget, ClearAllActions(TRUE));
    AssignCommand (oOther, ClearAllActions(TRUE));
    AssignCommand (oTarget, DetermineCombatRound());
    AssignCommand (oOther, DetermineCombatRound());

    if (nReset)
        SetHasCardEffect (CARD_SPELL_MIND_CONTROL, oTarget, FALSE);
    else if (nCanDispel)
        SetHasCardEffect (CARD_SPELL_MIND_CONTROL, oTarget);
    else
        SetOriginalOwner (nOpponent, oTarget);
}

void DoCardEffectParalyze (object oTarget, float fxCycle = 3.0f)
{
    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectLinkEffects (EffectParalyze(), EffectVisualEffect (VFX_DUR_PARALYZED)), oTarget, CYCLE_TIME * fxCycle);

    SetHasCardEffect (CARD_SPELL_PARALYZE, oTarget);
    AssignCommand (oTarget, DelayCommand (CYCLE_TIME * fxCycle, SetHasCardEffect (CARD_SPELL_PARALYZE, oTarget, FALSE)));
}

void DoCardEffectPhasing (object oTarget, int nPhaseOut = TRUE)
{
    if (nPhaseOut)
    {
        effect ePhase = EffectLinkEffects (EffectVisualEffect (VFX_DUR_CUTSCENE_INVISIBILITY), EffectCutsceneGhost());
        ePhase = EffectLinkEffects (ePhase, EffectCutsceneParalyze());
        ePhase = EffectLinkEffects (ePhase, EffectPetrify());

        ApplyEffectToObject (DURATION_TYPE_TEMPORARY, ExtraordinaryEffect (ePhase), oTarget, CYCLE_TIME);

        SetPlotFlag (oTarget, TRUE);
        AssignCommand (oTarget, DelayCommand (CYCLE_TIME, SetPlotFlag (oTarget, FALSE)));
    }

    AssignCommand (oTarget, DelayCommand (CYCLE_TIME * 2.0f, DoCardEffectPhasing (oTarget, !nPhaseOut)));
}

void DoCardEffectPotionOfHeroism (object oTarget)
{
    effect eHero = EffectLinkEffects (EffectVisualEffect (VFX_DUR_MIND_AFFECTING_POSITIVE), EffectHaste());
    eHero = EffectLinkEffects (eHero, EffectAttackIncrease (5));
    eHero = EffectLinkEffects (eHero, EffectTemporaryHitpoints (50));

    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, eHero, oTarget, CYCLE_TIME * 2.0f);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_SUPER_HEROISM), oTarget);

    SetHasCardEffect (CARD_SPELL_POTION_OF_HEROISM, oTarget);
    AssignCommand (oTarget, DelayCommand (CYCLE_TIME * 2.0f, SetHasCardEffect (CARD_SPELL_POTION_OF_HEROISM, oTarget, FALSE)));
}

void DoCardEffectSimulacrum (int nCaster, object oTarget, object oCentre, object oAvatar)
{
    object oArea = GetArea (oCentre);

    float fFacing = (nCaster == 1) ? 270.0f : 90.0f;
    float fDistance = (nCaster == 1) ? -1.5f : 1.5f;

    vector vSummon = GetPosition (oAvatar);

    location lLoc = Location (oArea, Vector (vSummon.x, vSummon.y + fDistance, vSummon.z), fFacing);

    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_SPELL_MANTLE_USE), oTarget);
    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_SUMMON_MONSTER_2), lLoc);

    object oClone = CopyObject (oTarget, lLoc);

    SetOwner (nCaster, oClone);
    SetOriginalOwner (nCaster, oClone);
    SetCardOwner (nCaster, oClone);
    SetHasCardEffect (CARD_SPELL_SIMULACRUM, oClone);

    AssignCommand (oClone, ClearAllActions());
    AssignCommand (oClone, DetermineCombatRound());
    AssignCommand (oClone, DelayCommand (0.5, SetIsDestroyable (TRUE)));
}

void DoCardEffectWrathOfTheHorde (int nBoost, object oTarget)
{
    effect eEffect = EffectLinkEffects (EffectACIncrease (nBoost), EffectAttackIncrease (nBoost));
    eEffect = EffectLinkEffects (eEffect, EffectVisualEffect (VFX_DUR_PROTECTION_GOOD_MAJOR));

    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, eEffect, oTarget, CYCLE_TIME * 4.0f);

    SetHasCardEffect (CARD_SPELL_WRATH_OF_THE_HORDE, oTarget);
    AssignCommand (oTarget, DelayCommand (CYCLE_TIME * 4.0f, SetHasCardEffect (CARD_SPELL_WRATH_OF_THE_HORDE, oTarget)));
}

void DoDeathFireElemental (int nPlayer, location lTarget)
{
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    effect eFireball = EffectDamage (50, DAMAGE_TYPE_FIRE, DAMAGE_POWER_ENERGY);

    object oArea = GetAreaFromLocation (lTarget);
    object oFireball = GetFirstObjectInShape (SHAPE_SPHERE, RADIUS_SIZE_LARGE, lTarget);

    while (oFireball != OBJECT_INVALID)
    {
        if (GetOwner (oFireball) == nEnemy
            && GetObjectType (oFireball) == OBJECT_TYPE_CREATURE
            && !GetIsDead (oFireball))
            {
                ApplyEffectToObject (DURATION_TYPE_INSTANT, eFireball, oFireball);
                ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEAD_FIRE), oFireball);

                SignalEvent (oFireball, EventSpellCastAt (oArea, SPELL_FIREBALL));
            }

        oFireball = GetNextObjectInShape (SHAPE_SPHERE, RADIUS_SIZE_LARGE, lTarget);
    }

    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_FIREBALL), lTarget);
}

void DoKillByVampire (object oKiller, int nPlayer, location lTarget)
{
    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectLinkEffects (EffectAttackIncrease (4), EffectACIncrease (4)), oKiller, CYCLE_TIME * 2.0f);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_M), oKiller);

    SendMessageToPC (GetCardGamePlayer (nPlayer, GetAreaFromLocation (lTarget)), "Vampire strengthens from kill.");
}

void DoKillByVampireMaster (object oKiller, int nPlayer, location lTarget)
{
    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_SUMMON_UNDEAD), lTarget);

    object oKiller = CreateObject (OBJECT_TYPE_CREATURE, GetCardTag (CARD_SUMMON_VAMPIRE, TRUE), lTarget);

    SetOwner (nPlayer, oKiller);
    SetOriginalOwner (nPlayer, oKiller);

    AssignCommand (oKiller, ClearAllActions());
    AssignCommand (oKiller, DetermineCombatRound());

    SendMessageToPC (GetCardGamePlayer (nPlayer, GetAreaFromLocation (lTarget)), "Vampire Master raises a new vampire.");
}

void DoKillByZombieLord (object oKiller, int nPlayer, location lTarget)
{
    ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectLinkEffects (EffectAttackIncrease (2), EffectACIncrease (2)), oKiller, CYCLE_TIME * 2.0f);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (20), oKiller);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_M), oKiller);

    SendMessageToPC (GetCardGamePlayer (nPlayer, GetAreaFromLocation (lTarget)), "Zombie Lord consumes corpse for boost.");
}

void DoSacrificeCougar (int nPlayer, object oArea)
{
    int nEnemy = (nPlayer == 1) ? 2 : 1;
    int nNth = 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID)
    {
        if (GetCardID (oCycle) && GetOwner (oCycle) == nEnemy)
        {
            if(GetIsPC(oCycle) == FALSE)
            {
              ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (10, DAMAGE_TYPE_POSITIVE, DAMAGE_POWER_PLUS_FIVE), oCycle);
              ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEAD_NATURE), oCycle);
              SignalEvent (oCycle, EventSpellCastAt (oArea, SPELL_FIREBALL));
            }

            SignalEvent (oCycle, EventSpellCastAt (oArea, SPELL_FIREBALL));
        }

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }
}

void DoSacrificeCow (int nPlayer, object oArea)
{
    int nNth = 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oAvatar = GetAvatar (nPlayer, oCentre);

    while (GetCardsInHand (nPlayer, oCentre, nNth++))
        continue;

    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (nNth), oAvatar);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_S), oAvatar);

    SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));
}

void DoSacrificeDeekin (int nPlayer, object oArea)
{
    int nNth = 1;

    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID)
    {
        if (GetOwner (oCycle) == nPlayer && GetCardID (oCycle) == CARD_SUMMON_KOBOLD)
            DoCardEffectKoboldPogostick (oCycle);

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }
}

void DoSacrificeDemonKnight (int nPlayer, object oArea)
{
    int nEnemy = (nPlayer == 1) ? 2 : 1;
    int nNth = 1;

    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID && GetDistanceToObject (oCycle) <= 10.0f)
    {
        if (GetCardID (oCycle) && GetOwner (oCycle) == nEnemy && !GetHasCardEffect (CARD_SUMMON_DEMON_KNIGHT, oCycle))
            DoCardEffectDemonKnight (oCycle);

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }
}

void DoSacrificeFaerieDragon (int nPlayer, object oArea)
{
    int nNth = 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));

    if (GetCardsInHand (nPlayer, oCentre))
    {
        int nRnd;

        struct sCard sCheck = GetCardInfo (GetCardsInHand (nPlayer, oCentre, nNth));

        while (sCheck.nCard)
        {
            if (sCheck.nMagic && sCheck.nMagic <= 2)
                nRnd += 1;

            sCheck = GetCardInfo (GetCardsInHand (nPlayer, oCentre, ++nNth));
        }

        if (nRnd)
        {
            nRnd = Random (nRnd) + 1;

            sCheck = GetCardInfo (GetCardsInHand (nPlayer, oCentre, --nNth));

            while (nRnd > 0 && nNth > 0)
            {
                if (sCheck.nMagic && sCheck.nMagic <= 2)
                    if (--nRnd == 0)
                    {
                        int nCount = 1;

                        string sTag = GetCardTag (sCheck.nCard);

                        object oCard = GetNearestObjectByTag (sTag, oCentre, nCount);

                        while (oCard != OBJECT_INVALID)
                        {
                            if (GetOwner (oCard) == nPlayer)
                            {
                                ActionAddPowerToPool (sCheck.nMagic, nPlayer, oArea, FALSE);

                                ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_MYSTICAL_EXPLOSION), oCard);

                                SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Faerie Dragon plays " + sCheck.sName);

                                ActionPlayCard (oCard);

                                break;
                            }

                            oCard = GetNearestObjectByTag (sTag, oCentre, nCount);
                        }

                        break;
                    }

                sCheck = GetCardInfo (GetCardsInHand (nPlayer, oCentre, --nNth));
            }
        }
    }
}

void DoSacrificeGoblinWitchdoctor (int nPlayer, object oArea)
{
    int nNth = 1;
    int nStore;

    object oTarget;
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oAvatar = GetAvatar (nPlayer, oCentre);
    object oCycle = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_DEFEND, nPlayer, oArea);

    if (oTarget != OBJECT_INVALID)
    {
        int nHeal = GetMaxHitPoints (oTarget);
        int nMax = GetMaxHitPoints (oAvatar) - GetCurrentHitPoints (oAvatar);

        if (nHeal > nMax)
            nHeal = nMax;

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage ((nHeal - (nHeal % 2)) / 2, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), OBJECT_SELF);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (nHeal), oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HARM), OBJECT_SELF);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_S), oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH), oTarget);

        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));

        AssignCommand (oTarget, DestroyCardCreature (TRUE));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "No valid sacrifice.");
}

void DoSacrificeHookHorror (int nPlayer, object oArea)
{
    int nNth = 1;
    int nStore;

    object oTarget;
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oCycle = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK, nPlayer, oArea);

    if (oTarget != OBJECT_INVALID)
    {
        int nDamage = GetMaxHitPoints (oTarget);
        int nEnemy = (nPlayer == 1) ? 2 : 1;

        object oAvatar = GetAvatar (nEnemy, oCentre);

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nDamage, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), OBJECT_SELF);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nDamage, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HARM), OBJECT_SELF);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HARM), oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH), oTarget);

        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_FIREBALL));

        AssignCommand (oTarget, DestroyCardCreature (TRUE));
    }
    else
        SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "No valid sacrifice.");
}

void DoSacrificeIntellectDevourer (int nPlayer, object oArea)
{
    int nNth = 6;

    while (--nNth >= 0)
    {
        int nSource = ((nPlayer == 1 && nNth > 1) || (nPlayer == 2 && nNth <= 1)) ? CARD_SOURCE_GAME_PLAYER_2 : CARD_SOURCE_GAME_PLAYER_1;
        int nDeck = (nSource == CARD_SOURCE_GAME_PLAYER_1) ? 1 : 2 ;
        int nCard = GetDrawnCard (nDeck, GetTotalCards (nSource, oArea), oArea);

        if (nCard)
        {
            ActionTransferCard (nCard, nSource, CARD_SOURCE_ALL_CARDS, oArea, OBJECT_INVALID);

            struct sCard sDestroy = GetCardInfo (nCard);

            SendMessageToCardPlayers ("Intellect Devourer destroys " + sDestroy.sName + " from " + GetName (GetCardGamePlayer (nDeck, oArea)) + "'s deck.", oArea);
        }
    }
}

void DoSacrificeKoboldKamikaze (int nPlayer, object oArea)
{
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    DoCardEffectFireball (nEnemy, 3, GetLocation (OBJECT_SELF));
}

void DoSacrificeJysirael (int nPlayer, object oArea)
{
    int nNth = 1;

    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID && GetDistanceToObject (oCycle) <= 10.0f)
    {
        if (GetCardID (oCycle) && GetOwner (oCycle) == nPlayer)
        {
            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_L), oCycle);
            ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (GetMaxHitPoints (oCycle) - GetCurrentHitPoints (oCycle)), oCycle);

            SignalEvent (oCycle, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));
        }

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, OBJECT_SELF, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }

    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_X), OBJECT_SELF);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (FloatToInt (IntToFloat (GetCurrentHitPoints (OBJECT_SELF)) / 2.0f), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), OBJECT_SELF);
}

void DoSacrificeMaidenOfParadise (int nPlayer, object oArea)
{
    ActionAddPowerToPool (3, nPlayer, oArea);

    SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Maiden of Paradise boosts magic pool by 3");
}

void DoSacrificeRevenant (int nPlayer, object oArea)
{
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oCycle = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK_LIVING, nEnemy, oArea);

    if (oCycle != OBJECT_INVALID)
    {
        AssignCommand (oCycle, DestroyCardCreature (TRUE));
        AssignCommand (oCycle, ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH), oCycle));
    }
}

void DoSacrificeShadowAssassin (int nPlayer, object oArea)
{
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oCycle = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_HIGHEST_ATTACK_LIVING, nEnemy, oArea);

    if (oCycle != OBJECT_INVALID)
    {
        AssignCommand (oCycle, DestroyCardCreature (TRUE));
        AssignCommand (oCycle, ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_DEATH), oCycle));
    }
}

void DoSacrificeSpiritGuardian (int nPlayer, object oArea)
{
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oAvatar = GetAvatar (nPlayer, oCentre);

    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (30), oAvatar);
    ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_M), oAvatar);

    SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));
}

void DoSacrificeUmberHulk (int nPlayer, object oArea)
{
    int nNth = 1;
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, nNth, CREATURE_TYPE_IS_ALIVE, TRUE);

    while (oCycle != OBJECT_INVALID)
    {
        if (GetCardID (oCycle)
            && GetOwner (oCycle) == nEnemy
            && !GetHasCardEffect (CARD_SPELL_PARALYZE, oCycle))
            DoCardEffectParalyze (oCycle);

        oCycle = GetNearestCreature (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oCentre, ++nNth, CREATURE_TYPE_IS_ALIVE, TRUE);
    }
}

void DoSpawnAtlantian()
{
    int nOwner = GetOwner (OBJECT_SELF);
    object oArea = GetArea (OBJECT_SELF);

    int nPower = GetIsPowerAvailable (nOwner, oArea);

    if(nPower == 0)
      return;

    if(nPower > 4)
      nPower = 4;

    ActionUsePower (nPower, nOwner, oArea);
    SetSpawnBoost (nPower);


    effect eBoost = EffectLinkEffects (EffectACIncrease (2 * nPower), EffectAttackIncrease (2 * nPower));
    eBoost = EffectLinkEffects (eBoost, EffectAbilityIncrease (ABILITY_STRENGTH, nPower));
    eBoost = EffectLinkEffects (eBoost, EffectAbilityIncrease (ABILITY_DEXTERITY, nPower));
    eBoost = EffectLinkEffects (eBoost, EffectAbilityIncrease (ABILITY_CONSTITUTION, nPower));
    eBoost = EffectLinkEffects (eBoost, EffectSavingThrowIncrease (SAVING_THROW_ALL, nPower));

    ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (eBoost), OBJECT_SELF);
}

void DoSpawnDragon()
{
    if (GetSpawnBoost())
        return;

    SetHasCardEffect (CARD_SUMMON_DRAGON, OBJECT_SELF);
    DelayCommand (0.2, SetHasCardEffect (CARD_SUMMON_DRAGON, OBJECT_SELF, FALSE));

    int nOwner = GetOwner (OBJECT_SELF);
    int nHunger = 50;

    object oArea = GetArea (OBJECT_SELF);
    object oPlayer = GetCardGamePlayer (nOwner, oArea);
    object oMorsel = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_DEFEND_LIVING, nOwner, oArea, CARD_SCAN_NO_EFFECT, CARD_SUMMON_DRAGON);

    while (nHunger && oMorsel != OBJECT_INVALID)
    {
        int nDamage = GetCurrentHitPoints (oMorsel);

        if (nDamage > nHunger)
            nDamage = nHunger;

        nHunger -= nDamage;

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nDamage, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oMorsel);

        struct sCard sInfo = GetCardInfo (GetCardID (oMorsel));

        SendMessageToPC (oPlayer, sInfo.sName + " eaten by dragon.");

        SignalEvent (oMorsel, EventSpellCastAt (oArea, SPELL_FIREBALL));

        SetHasCardEffect (CARD_SUMMON_DRAGON, oMorsel);
        AssignCommand (oMorsel, DelayCommand (0.2, SetHasCardEffect (CARD_SUMMON_DRAGON, oMorsel, FALSE)));

        oMorsel = GetCardGameCreature (CARD_SCAN_CREATURE_SCAN, CARD_CREATURE_SCAN_LOWEST_DEFEND_LIVING, nOwner, oArea, CARD_SCAN_NO_EFFECT, CARD_SUMMON_DRAGON);
    }

    if (nHunger > 0)
    {
        object oCentre = GetReferenceObject (nOwner, GetGameCentre (oArea));
        object oAvatar = GetAvatar (nOwner, oCentre);

        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nHunger, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_FIVE), oAvatar);

        SendMessageToPC (oPlayer, "Dragon takes a bite out of you.");
        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_FIREBALL));
    }

    SetSpawnBoost (TRUE);
}

void DoSpawnKoboldPogostick()
{
    DelayCommand (CYCLE_TIME / 2.0f, DoCardEffectKoboldPogostick (OBJECT_SELF));
}

void DoSpawnPhaseSpider()
{
    DoCardEffectPhasing (OBJECT_SELF, FALSE);
}

void DoSpawnPitFiend()
{
    int nOwner = GetOwner (OBJECT_SELF);

    object oArea = GetArea (OBJECT_SELF);
    object oPlayer = GetCardGamePlayer (nOwner, oArea);

    ActionDestroyPower (2, nOwner, oArea);
    SendMessageToPC (oPlayer, "Pit Fiend consumes magic generators.");

}

void DoUpkeepDiscardPile (int nPlayer, object oCentre)
{
    object oArea = GetArea (oCentre);

    int nMax = GetDiscardPileSize (nPlayer, oArea);
    int nSize = nMax;
    int nRevenant;
    int nSaeshen = GetHasCreatures (CARD_SCAN_CARD_ID, CARD_MYTHICAL_SAESHEN, nPlayer, oArea);

    while (nSize > 0)
    {
        struct sCard sInfo = GetCardInfo (GetDiscardPile (nSize, nPlayer, oArea));

        if (nSaeshen && sInfo.nCard == CARD_SUMMON_FAIRY_DRAGON)
        {
            RemoveFromDiscardPile (CARD_SUMMON_FAIRY_DRAGON, nPlayer, oArea);

            ActionUseSummon (CARD_SUMMON_FAIRY_DRAGON, nPlayer, oArea);

            nMax = GetDiscardPileSize (nPlayer, oArea);
            nSize = nMax + 1;
            nSaeshen -= 1;
        }
        else if (sInfo.nCard == CARD_SUMMON_REVENANT)
        {
            if (nRevenant)
            {
                RemoveFromDiscardPile (CARD_SUMMON_REVENANT, nPlayer, oArea);
                RemoveFromDiscardPile (nRevenant, nPlayer, oArea);

                ActionUseSummon (CARD_SUMMON_REVENANT, nPlayer, oArea);

                nMax = GetDiscardPileSize (nPlayer, oArea);
                nSize = nMax + 1;
                nRevenant = FALSE;
            }
        }
        else if (!nRevenant && sInfo.nType == CARD_TYPE_SUMMON)
            nRevenant = sInfo.nCard;

        nSize -= 1;
    }
}

void DoUpkeepDrainAvatar (int nDrain, int nPlayer, object oCentre, object oCreature)
{
    if (nDrain > 0)
    {
        struct sCard sCreature = GetCardInfo (GetCardID (oCreature));

        object oArea = GetArea (oCentre);

        object oAvatar = GetAvatar (nPlayer, oCentre);

        effect eBeam = EffectBeam (VFX_BEAM_EVIL, oCreature, BODY_NODE_HAND);
        effect eHit = EffectVisualEffect (VFX_IMP_DEATH);
        effect eDamage = EffectDamage (nDrain, DAMAGE_TYPE_NEGATIVE, DAMAGE_POWER_PLUS_FIVE);
        effect eHeal = EffectHeal (nDrain);

        ApplyEffectToObject (DURATION_TYPE_TEMPORARY, eBeam, oAvatar, 2.0f);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, eHit, oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, eDamage, oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, eHeal, oCreature);

        SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), sCreature.sName + " drains life.");
        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_FIREBALL));
    }
}

void DoUpkeepHealAvatar (int nHeal, int nCard, int nPlayer, object oCentre)
{
    struct sCard sCreature = GetCardInfo (nCard);

    object oArea = GetArea (oCentre);
    object oAvatar = GetAvatar (nPlayer, oCentre);

    int nCap = GetMaxHitPoints (oAvatar) - GetCurrentHitPoints (oAvatar);
    nHeal = (nCap > nHeal) ? nHeal : nCap;

    if (nHeal > 0)
    {
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectHeal (nHeal), oAvatar);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_IMP_HEALING_M), oAvatar);

        SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), sCreature.sName + " heals you by " + IntToString (nHeal) + " hit points.");
        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_CURE_MINOR_WOUNDS, FALSE));
    }
}

void DoUpkeepKoboldEngineer (int nPlayer, object oCentre, object oCreature)
{
    if (GetIsMine (oCreature))
        return;

    int nMines = GetLocalInt (oCreature, "CARDS_KOBOLD_MINES_LEFT");

    if (!nMines)
        nMines = 5;
    else if (nMines == 1)
        return;

    location lLoc = GetLocation (oCreature);

    object oMine = CreateObject (OBJECT_TYPE_CREATURE, "koboldmine", lLoc, FALSE, GetCardTag (CARD_SUMMON_KOBOLD_ENGINEER, TRUE));

    ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectInvisibility (INVISIBILITY_TYPE_IMPROVED)), oMine);
    ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectLinkEffects (EffectCutsceneGhost(), EffectCutsceneParalyze())), oMine);
    ApplyEffectAtLocation (DURATION_TYPE_TEMPORARY, ExtraordinaryEffect (EffectVisualEffect (VFX_DUR_GLYPH_OF_WARDING)), GetLocation (oMine), 6.0f);
    AssignCommand (oCreature, ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectAreaOfEffect (AOE_PER_GLYPH_OF_WARDING, "d_card_auraa", "d_card_aurab", "d_card_aurac")), oMine));

    SetOwner (nPlayer, oMine);
    SetHasCardEffect (CARD_SUMMON_KOBOLD_ENGINEER, oMine);
    SetLocalInt (oCreature, "CARDS_KOBOLD_MINES_LEFT", --nMines);
}

void DoUpkeepPainGolem (int nPlayer, object oCentre, object oCreature)
{
    object oArea = GetArea (oCentre);

    int nNth = 3;
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    while (GetCardsInHand (nEnemy, oCentre, ++nNth))
        continue;

    if (nNth > 4)
    {
        nNth -= 4;

        object oAvatar = GetAvatar (nEnemy, oCentre);

        ApplyEffectToObject (DURATION_TYPE_TEMPORARY, EffectBeam (VFX_BEAM_EVIL, oCreature, BODY_NODE_CHEST), oAvatar, 2.0f);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectDamage (nNth * 2, DAMAGE_TYPE_NEGATIVE, DAMAGE_POWER_PLUS_FIVE), oAvatar);

        SendMessageToPC (GetCardGamePlayer (nPlayer, oArea), "Pain Golem deals damage.");
        SignalEvent (oAvatar, EventSpellCastAt (oArea, SPELL_FIREBALL));
    }
}

void DoUpkeepSolarStone (int nPlayer, object oStone)
{
    if (!GetIsPowerAvailable (nPlayer, OBJECT_SELF, 2))
    {
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_GAS_EXPLOSION_FIRE), oStone);
        ApplyEffectToObject (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_LOS_NORMAL_10), oStone);

        SetPlotFlag (oStone, FALSE);
        DelayCommand (0.1, DestroyObject (oStone));

        SendMessageToPC (GetCardGamePlayer (nPlayer, OBJECT_SELF), "Solar Stone upkeep failed.  Sacrificing.");
    }
    else
    {
        ActionUsePower (2, nPlayer, OBJECT_SELF);

        int nEnemy = (nPlayer == 1) ? 2 : 1;
        int nSource = (nPlayer == 1) ? CARD_SOURCE_GAME_PLAYER_2 : CARD_SOURCE_GAME_PLAYER_1;
        int nCard = GetDrawnCard (nEnemy, GetTotalCards (nSource, OBJECT_SELF), OBJECT_SELF);

        if (nCard)
        {
            ActionTransferCard (nCard, nSource, CARD_SOURCE_ALL_CARDS, OBJECT_SELF, OBJECT_INVALID);

            struct sCard sDestroy = GetCardInfo (nCard);

            SendMessageToCardPlayers ("Solar Stone destroys " + sDestroy.sName + " from " + GetName (GetCardGamePlayer (nEnemy, OBJECT_SELF)) + "'s deck.", OBJECT_SELF);
        }
    }
}
