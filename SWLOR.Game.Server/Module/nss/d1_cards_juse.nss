/**********************************/
/*          d1_cards_juse
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  ActionUse functions
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/


void ActionUseAura (object oSelf, object oTarget, int nRemove = FALSE)
{
    if (GetIsDead (oTarget) || GetIsAvatar (oTarget))
        return;

    switch (GetCardID (oSelf))
    {
        case CARD_SUMMON_ANGELIC_DEFENDER:  DoAuraAngelicDefender (oSelf, oTarget, nRemove);    return;
        case CARD_SUMMON_ARCHANGEL:         DoAuraArchangel (oSelf, oTarget, nRemove);          return;
        case CARD_SUMMON_BONE_GOLEM:        DoAuraBoneGolem (oSelf, oTarget, nRemove);          return;
        case CARD_SUMMON_DRUID:             DoAuraDruid (oSelf, oTarget, nRemove);              return;
        case CARD_SUMMON_GOBLIN_WARLORD:    DoAuraGoblinWarlord (oSelf, oTarget, nRemove);      return;
        case CARD_SUMMON_KOBOLD_CHIEF:      DoAuraKoboldChief (oSelf, oTarget, nRemove);        return;
        case CARD_SUMMON_KOBOLD_ENGINEER:   DoAuraKoboldEngineer (oSelf, oTarget, nRemove);     return;
        case CARD_SUMMON_LICH:              DoAuraLich (oSelf, oTarget, nRemove);               return;
        case CARD_SUMMON_PLAGUE_BEARER:     DoAuraPlagueBearer (oSelf, oTarget, nRemove);       return;
        case CARD_SUMMON_RAT_KING:          DoAuraRatKing (oSelf, oTarget, nRemove);            return;
        case CARD_SUMMON_STEEL_GUARDIAN:    DoAuraSteelGuardian (oSelf, oTarget, nRemove);      return;
        case CARD_SUMMON_WHITE_WOLF:        DoAuraWhiteWolf (oSelf, oTarget, nRemove);          return;
        case CARD_SUMMON_WOLF:              DoAuraWolf (oSelf, oTarget, nRemove);               return;
        case CARD_SUMMON_ZOMBIE_LORD:       DoAuraZombieLord (oSelf, oTarget, nRemove);         return;
        default:                            ActionUseCustomAura (oSelf, oTarget, nRemove);      return;
    }
}

void ActionUseCreatureDeath (object oKiller)
{
    int nCard = GetCardID (OBJECT_SELF);
    int nKiller = GetCardID (oKiller);
    int nPlayer = GetOwner (OBJECT_SELF);
    int nEnemy = (nPlayer == 1) ? 2 : 1;

    object oArea = GetArea (OBJECT_SELF);

    location lTarget = GetLocation (OBJECT_SELF);

    if (GetIsAvatar())
    {
        AssignCommand (GetArea (OBJECT_SELF), DelayCommand (RoundsToSeconds (CARD_GAME_DEFAULT_GRACE_PERIOD), ActionEndPrep (GetLocalInt (oArea, "CARD_AVATAR_ALT_DEATH"))));

        return;
    }

    DiscardOnKill (OBJECT_SELF);

    if (oKiller == OBJECT_SELF)
        return;

    switch (nCard)
    {
        case CARD_SUMMON_ELDER_FIRE_ELEMENTAL:  DoDeathFireElemental (nPlayer, lTarget);                return;
        default:                                ActionUseCustomCreatureDeath (nCard, nPlayer, lTarget); break;
    }

    if (GetLocalInt (OBJECT_SELF, "CARDS_CREATURE_CUSTOM_DEATH"))
        return;

    switch (nKiller)
    {
        case CARD_SUMMON_VAMPIRE:           DoKillByVampire (oKiller, nEnemy, lTarget);             return;
        case CARD_SUMMON_VAMPIRE_MASTER:    DoKillByVampireMaster (oKiller, nEnemy, lTarget);       return;
        case CARD_SUMMON_ZOMBIE_LORD:       DoKillByZombieLord (oKiller, nEnemy, lTarget);          return;
        default:                            ActionUseCustomCreatureKill (oKiller, nEnemy, lTarget); return;
    }
}

void ActionUseSacrifice (int nUser)
{
    struct sCard sInfo = GetCardInfo (GetCardID (OBJECT_SELF));

    if (!sInfo.nSacrifice)
        return;

    object oArea = GetArea (OBJECT_SELF);

    SendMessageToCardPlayers (sInfo.sName + " sacrificed by " + GetName (GetCardGamePlayer (nUser, oArea)) + ".", oArea);

    switch (sInfo.nCard)
    {
        case CARD_MYTHICAL_ARKNETH:             DoSacrificeDemonKnight (nUser, oArea);                  return;
        case CARD_MYTHICAL_DEEKIN:              DoSacrificeDeekin (nUser, oArea);                       return;
        case CARD_MYTHICAL_JYSIRAEL:            DoSacrificeJysirael (nUser, oArea);                     return;
        case CARD_SUMMON_COUGAR:                DoSacrificeCougar (nUser, oArea);                       break;
        case CARD_SUMMON_COW:                   DoSacrificeCow (nUser, oArea);                          break;
        case CARD_SUMMON_DEMON_KNIGHT:          DoSacrificeDemonKnight (nUser, oArea);                  break;
        case CARD_SUMMON_FAIRY_DRAGON:          DoSacrificeFaerieDragon (nUser, oArea);                 break;
        case CARD_SUMMON_GOBLIN_WITCHDOCTOR:    DoSacrificeGoblinWitchdoctor (nUser, oArea);            return;
        case CARD_SUMMON_HOOK_HORROR:           DoSacrificeHookHorror (nUser, oArea);                   return;
        case CARD_SUMMON_INTELLECT_DEVOURER:    DoSacrificeIntellectDevourer (nUser, oArea);            break;
        case CARD_SUMMON_KOBOLD_KAMIKAZE:       DoSacrificeKoboldKamikaze (nUser, oArea);               break;
        case CARD_SUMMON_MAIDEN_OF_PARADISE:    DoSacrificeMaidenOfParadise (nUser, oArea);             break;
        case CARD_SUMMON_REVENANT:              DoSacrificeRevenant (nUser, oArea);                     break;
        case CARD_SUMMON_SHADOW_ASSASSIN:       DoSacrificeShadowAssassin (nUser, oArea);               break;
        case CARD_SUMMON_SPIRIT_GUARDIAN:       DoSacrificeSpiritGuardian (nUser, oArea);               break;
        case CARD_SUMMON_UMBER_HULK:            DoSacrificeUmberHulk (nUser, oArea);                    break;
        default:                                ActionUseCustomSacrifice (sInfo.nCard, nUser, oArea);   break;
    }

    if (GetLocalInt (OBJECT_SELF, "CARDS_CREATURE_CUSTOM_SACRIFICE"))
        return;

    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_PWKILL), GetLocation (OBJECT_SELF));

    DestroyCardCreature (TRUE);
}

void ActionUseSpawn()
{
    int nCard = GetCardID (OBJECT_SELF);

    switch (nCard)
    {
        case CARD_SUMMON_ATLANTIAN:         DoSpawnAtlantian();                     return;
        case CARD_SUMMON_DRAGON:            DoSpawnDragon();                        return;
        case CARD_SUMMON_PIT_FIEND:         DoSpawnPitFiend();                      return;
        case CARD_SUMMON_KOBOLD_POGOSTICK:  DoSpawnKoboldPogostick();               return;
        case CARD_SUMMON_PHASE_SPIDER:      DoSpawnPhaseSpider();                   return;

        case CARD_SUMMON_ANGELIC_DEFENDER:
        case CARD_SUMMON_ARCHANGEL:
        case CARD_SUMMON_BONE_GOLEM:
        case CARD_SUMMON_DRUID:
        case CARD_SUMMON_GOBLIN_WARLORD:
        case CARD_SUMMON_KOBOLD_CHIEF:
        case CARD_SUMMON_LICH:
        case CARD_SUMMON_PLAGUE_BEARER:
        case CARD_SUMMON_RAT_KING:
        case CARD_SUMMON_STEEL_GUARDIAN:
        case CARD_SUMMON_WHITE_WOLF:
        case CARD_SUMMON_WOLF:
        case CARD_SUMMON_ZOMBIE_LORD:
            ApplyEffectToObject (DURATION_TYPE_PERMANENT, ExtraordinaryEffect (EffectAreaOfEffect (AOE_MOB_CIRCGOOD, "d_card_auraa", "d_card_aurab", "d_card_aurac")), OBJECT_SELF);

            return;

        default:                            ActionUseCustomSpawn (nCard);           return;
    }
}

void ActionUseSpell (int nCard, int nPlayer, object oArea)
{
    object oCentre = GetReferenceObject (nPlayer, GetGameCentre (oArea));
    object oAvatar = GetAvatar (nPlayer, oCentre);

    AssignCommand (oAvatar, ClearAllActions());
    AssignCommand (oAvatar, ActionPlayAnimation (ANIMATION_LOOPING_CONJURE1, 1.0f, 3.0f));

    if (GetHasCardEffect (CARD_SPELL_COUNTERSPELL, oAvatar))
    {
        DoCardEffectCounterspell (nPlayer, oAvatar);

        return;
    }

    switch (nCard)
    {
        case CARD_SPELL_ENERGY_DISRUPTION:     DoCardEnergyDisruption (nPlayer, oCentre);      return;
        case CARD_SPELL_FLUX:                  DoCardFlux (nPlayer, oCentre);                  return;
        case CARD_SPELL_VORTEX:                DoCardVortex (nPlayer, oCentre);                return;
        case CARD_SPELL_ANGELIC_CHOIR:          DoCardAngelicChoir (nPlayer, oCentre);          return;
        case CARD_SPELL_ARMOUR:                 DoCardArmour (nPlayer, oCentre);                return;
        case CARD_SPELL_ASSASSIN:               DoCardAssassin (nPlayer, oCentre);              return;
        case CARD_SPELL_BOOMERANG:              DoCardBoomerang (nPlayer, oCentre);             return;
        case CARD_SPELL_COUNTERSPELL:           DoCardCounterspell (nPlayer, oCentre);          return;
        case CARD_SPELL_DEATH_PACT:             DoCardDeathPact (nPlayer, oCentre);             return;
        case CARD_SPELL_DISPEL_MAGIC:           DoCardDispelMagic (nPlayer, oCentre);           return;
        case CARD_SPELL_ELIXIR_OF_LIFE:         DoCardElixirOfLife (nPlayer, oCentre);          return;
        case CARD_SPELL_EYE_OF_THE_BEHOLDER:    DoCardEyeOfTheBeholder (nPlayer, oCentre);      return;
        case CARD_SPELL_FIREBALL:               DoCardFireball (nPlayer, oCentre);              return;
        case CARD_SPELL_FIRE_SHIELD:            DoCardFireShield (nPlayer, oCentre);            return;
        case CARD_SPELL_HEALING_LIGHT:          DoCardHealingLight (nPlayer, oCentre);          return;
        case CARD_SPELL_HIGHER_CALLING:         DoCardHigherCalling (nPlayer, oCentre);         return;
        case CARD_SPELL_HOLY_VENGEANCE:         DoCardHolyVengeance (nPlayer, oCentre);         return;
        case CARD_SPELL_LIFE_DRAIN:             DoCardLifeDrain (nPlayer, oCentre);             return;
        case CARD_SPELL_LIGHTNING_BOLT:         DoCardLightningBolt (nPlayer, oCentre);         return;
        case CARD_SPELL_MIND_CONTROL:           DoCardMindControl (nPlayer, oCentre);           return;
        case CARD_SPELL_MIND_OVER_MATTER:       DoCardMindOverMatter (nPlayer, oCentre);        return;
        case CARD_SPELL_PARALYZE:               DoCardParalyze (nPlayer, oCentre);              return;
        case CARD_SPELL_POTION_OF_HEROISM:      DoCardPotionOfHeroism (nPlayer, oCentre);       return;
        case CARD_SPELL_POWER_STREAM:           DoCardPowerStream (nPlayer, oCentre);           return;
        case CARD_SPELL_RESURRECT:              DoCardResurrect (nPlayer, oCentre);             return;
        case CARD_SPELL_SABOTAGE:               DoCardSabotage (nPlayer, oCentre);              return;
        case CARD_SPELL_SCORCHED_EARTH:         DoCardScorchedEarth (nPlayer, oCentre);         return;
        case CARD_SPELL_SIMULACRUM:             DoCardSimulacrum (nPlayer, oCentre);            return;
        case CARD_SPELL_WARP_REALITY:           DoCardWarpReality (nPlayer, oCentre);           return;
        case CARD_SPELL_WRATH_OF_THE_HORDE:     DoCardWrathOfTheHorde (nPlayer, oCentre);       return;
        default:                                ActionUseCustomSpell (nCard, nPlayer, oCentre); return;
    }
}

void ActionUseSummon (int nCard, int nPlayer, object oArea, int nCombat = TRUE)
{
    float fFacing = (nPlayer == 1) ? 270.0f : 90.0f;
    float fDistance = (nPlayer == 1) ? -1.5f : 1.5f;

    object oCentre = GetGameCentre (oArea);
    object oAvatar = GetAvatar (nPlayer, oCentre);

    vector vSummon = GetPosition (oAvatar);

    location lLoc = Location (oArea, Vector (vSummon.x, vSummon.y + fDistance, vSummon.z), fFacing);

    ApplyEffectAtLocation (DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_SUMMON_MONSTER_1), lLoc);

    object oCreature = CreateObject (OBJECT_TYPE_CREATURE, GetCardTag (nCard, TRUE), lLoc);

    SetOwner (nPlayer, oCreature);
    SetOriginalOwner (nPlayer, oCreature);
    SetCardOwner (nPlayer, oCreature);
    AssignCommand(oCreature, ActionUseSpawn());

    if (nCombat)
    {
        AssignCommand (oCreature, ClearAllActions());
        AssignCommand (oCreature, DetermineCombatRound());
    }
    else
        SetLocalInt (oCreature, "CARD_AI_BLOCK", TRUE);
}
