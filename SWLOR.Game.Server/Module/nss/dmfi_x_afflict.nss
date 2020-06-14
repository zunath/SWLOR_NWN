#include "dmfi_db_inc"


object DMFI_NextTarget(object oTarget, object oUser)
{
    object oNew;

    if (GetIsPC(oTarget))
    {
        if (GetIsObjectValid(GetNextFactionMember(oTarget)))
            oNew = GetNextFactionMember(oTarget);
        else
            oNew = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC, oTarget, 1);
    }
    else
        oNew = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_NOT_PC, oTarget, 1);


    if (!GetIsObjectValid(oNew))
    {
        SendMessageToPC(oUser, "No valid target to transfer to.");
        oNew = oTarget;
    }

    SetLocalObject(oUser, "dmfi_univ_target", oNew);
    SetCustomToken(20680, GetName(oNew));
    FloatingTextStringOnCreature("Target changed to: "+ GetName(oNew), oUser);
    return oNew;
}

//DMFI Creates the "settings" creature
void CreateSetting(object oUser)
{
    object oSetting = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_setting", GetLocation(oUser));
    DelayCommand(0.5f, AssignCommand(oSetting, ActionSpeakString(GetLocalString(oUser, "EffectSetting") + " is currently set at " + FloatToString(GetLocalFloat(oUser, GetLocalString(oUser, "EffectSetting"))))));
    SetLocalObject(oSetting, "MyMaster", oUser);
    SetListenPattern(oSetting, "**", LISTEN_PATTERN); //listen to all text
    SetLocalInt(oSetting, "hls_Listening", 1); //listen to all text
    SetListening(oSetting, TRUE);          //be sure NPC is listening
}

//This function is for the DMFI Affliction Wand
void ReportImmunity(object oT, object oUser)
{
    SendMessageToPC(oUser, "Immunities Reported: (blank if none)");
    if (GetIsImmune(oT, IMMUNITY_TYPE_ABILITY_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Ability Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_AC_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE AC Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_ATTACK_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Attack Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_BLINDNESS))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Blindness");
    if (GetIsImmune(oT, IMMUNITY_TYPE_CHARM))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Charm");
    if (GetIsImmune(oT, IMMUNITY_TYPE_CONFUSED))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Confusion");
    if (GetIsImmune(oT, IMMUNITY_TYPE_CRITICAL_HIT))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Critical Hit");
    if (GetIsImmune(oT, IMMUNITY_TYPE_CURSED))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Cursed");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DAMAGE_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Damage Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DAMAGE_IMMUNITY_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Damage Immunity Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DAZED))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Dazed");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DEAFNESS))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Deafness");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DEATH))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Death");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DISEASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Disease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_DOMINATE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Dominate");
    if (GetIsImmune(oT, IMMUNITY_TYPE_ENTANGLE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Entangle");
    if (GetIsImmune(oT, IMMUNITY_TYPE_FEAR))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Fear");
    if (GetIsImmune(oT, IMMUNITY_TYPE_KNOCKDOWN))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Knockdown");
    if (GetIsImmune(oT, IMMUNITY_TYPE_MIND_SPELLS))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Mind Spells");
    if (GetIsImmune(oT, IMMUNITY_TYPE_MOVEMENT_SPEED_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Movement Speed Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_NEGATIVE_LEVEL))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Negative Level");
    if (GetIsImmune(oT, IMMUNITY_TYPE_PARALYSIS))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Paralysis");
    if (GetIsImmune(oT, IMMUNITY_TYPE_POISON))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Poison");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SAVING_THROW_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Saving Throw Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SILENCE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Silence");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SKILL_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Skill Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SLEEP))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Sleep");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SLOW))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Slow");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SNEAK_ATTACK))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Sneak Attack");
    if (GetIsImmune(oT, IMMUNITY_TYPE_SPELL_RESISTANCE_DECREASE))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Spell Resistance Decrease");
    if (GetIsImmune(oT, IMMUNITY_TYPE_STUN))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Stun");
    if (GetIsImmune(oT, IMMUNITY_TYPE_TRAP))
        SendMessageToPC(oUser, GetName(oT) + " IMMUNE Trap");
}

void CheckForEffect(effect eA, object oT, object oUser)
{
    int Result = FALSE;
    effect Check = GetFirstEffect(oT);

    while (GetIsEffectValid(Check))
    {
        if (Check == eA)
            Result = TRUE;

        Check = GetNextEffect(oT);
    }
    if (Result)
        FloatingTextStringOnCreature("Affliction Wand Saving Throw Failure: " + GetName(oT), oUser);
    else
        FloatingTextStringOnCreature("Affliction Wand Saving Throw Success: No Effect: " + GetName(oT), oUser);
}

void main()
{
    int iAfflict = GetLocalInt(OBJECT_SELF, "dmfi_univ_int");
    object oUser = OBJECT_SELF;
    effect eEffect;
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    float fDuration;
    int nDNum;
    effect eD;
    effect eA;
    effect eT;
    effect eVis;
    int nBug = 0;
    int nSaveAmount; float fSaveAmount;

    nDNum = GetLocalInt(oUser, "dmfi_damagemodifier");
    fDuration = GetLocalFloat(oUser, "dmfi_stunduration");
    fSaveAmount = GetLocalFloat(oUser, "dmfi_saveamount");

    nSaveAmount = FloatToInt(fSaveAmount);

    if (!(GetObjectType(oTarget) == OBJECT_TYPE_CREATURE) ||
        GetIsDM(oTarget))
    {
        FloatingTextStringOnCreature("You must target a valid creature!", oUser, FALSE);
        return;
    }
    switch(iAfflict)
    {
        case 11: eD= EffectDamage(d4(nDNum), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_SPARK_SMALL); break;
        case 12: eD = EffectDamage(d6(nDNum), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_LRG_RED); break;
        case 13: eD = EffectDamage(d8(nDNum), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_LRG_RED); break;
        case 14: eD = EffectDamage(d10(nDNum), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_SPARK_SMALL); break;
        case 15: eD = EffectDamage(d12(nDNum), DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_SPARK_SMALL); break;
        case 16: eD = EffectDamage(GetCurrentHitPoints(oTarget)/4, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_LRG_RED); break;
        case 17: eD = EffectDamage(GetCurrentHitPoints(oTarget)/2, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis = EffectVisualEffect(VFX_COM_BLOOD_LRG_RED); break;
        case 18: eD = EffectDamage(GetCurrentHitPoints(oTarget) * 3 / 4, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis =EffectVisualEffect(VFX_COM_CHUNK_RED_SMALL); break;
        case 19: eD = EffectDamage(GetCurrentHitPoints(oTarget)-1, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_PLUS_TWENTY);
                 eVis =EffectVisualEffect(VFX_COM_CHUNK_RED_SMALL); break;
        case 21: eA =EffectDisease(DISEASE_FILTH_FEVER); break;
        case 22: eA =EffectDisease(DISEASE_MINDFIRE); break;
        case 23: eA =EffectDisease(DISEASE_DREAD_BLISTERS); break;
        case 24: eA =EffectDisease(DISEASE_SHAKES); break;
        case 25: eA =EffectDisease(DISEASE_VERMIN_MADNESS); break;
        case 26: eA =EffectDisease(DISEASE_DEVIL_CHILLS); break;
        case 27: eA =EffectDisease(DISEASE_SLIMY_DOOM); break;
        case 28: eA =EffectDisease(DISEASE_RED_ACHE); break;
        case 29: eA =EffectDisease(DISEASE_ZOMBIE_CREEP); break;
        case 31: eA =EffectDisease(DISEASE_BLINDING_SICKNESS); break;
        case 32: eA =EffectDisease(DISEASE_CACKLE_FEVER); break;
        case 33: eA =EffectDisease(DISEASE_BURROW_MAGGOTS); break;
        case 34: eA =EffectDisease(DISEASE_RED_SLAAD_EGGS); break;
        case 35: eA =EffectDisease(DISEASE_DEMON_FEVER); break;
        case 36: eA =EffectDisease(DISEASE_GHOUL_ROT); break;
        case 37: eA =EffectDisease(DISEASE_MUMMY_ROT); break;
        case 38: eA =EffectDisease(DISEASE_SOLDIER_SHAKES); break;
        case 39: eA =EffectDisease(DISEASE_SOLDIER_SHAKES); break;
        case 41: eA =EffectPoison(POISON_TINY_SPIDER_VENOM); break;
        case 42: eA =EffectPoison(POISON_ARANEA_VENOM); break;
        case 43: eA =EffectPoison(POISON_MEDIUM_SPIDER_VENOM); break;
        case 44: eA = EffectPoison(POISON_CARRION_CRAWLER_BRAIN_JUICE); break;
        case 45: eA = EffectPoison(POISON_OIL_OF_TAGGIT); break;
        case 46: eA = EffectPoison(POISON_ARSENIC); break;
        case 47: eA = EffectPoison(POISON_GREENBLOOD_OIL); break;
        case 48: eA = EffectPoison(POISON_NITHARIT); break;
        case 49: eA = EffectPoison(POISON_PHASE_SPIDER_VENOM); break;
        case 51: eA = EffectPoison(POISON_LICH_DUST); break;
        case 52: eA = EffectPoison(POISON_SHADOW_ESSENCE); break;
        case 53: eA = EffectPoison(POISON_LARGE_SPIDER_VENOM); break;
        case 54: eA = EffectPoison(POISON_PURPLE_WORM_POISON); break;
        case 55: eA = EffectPoison(POISON_IRON_GOLEM); break;
        case 56: eA = EffectPoison(POISON_PIT_FIEND_ICHOR); break;
        case 57: eA = EffectPoison(POISON_WYVERN_POISON); break;
        case 58: eA = EffectPoison(POISON_BLACK_LOTUS_EXTRACT); break;
        case 59: eA = EffectPoison(POISON_GARGANTUAN_SPIDER_VENOM); break;
        case 60: eT = EffectPetrify(); break;
        case 61: eT = EffectBlindness(); break;
        case 62: eT = EffectCurse(4,4,4,4,4,4); break;
        case 63: eT = EffectFrightened(); break;
        case 64: eT = EffectStunned(); break;
        case 65: eT = EffectSilence(); break;
        case 66: eT = EffectSleep(); break;
        case 67: eT = EffectSlow(); break;
        case 68: eT = EffectKnockdown(); nBug = 1; break;
        case 69: eD = EffectDamage( GetCurrentHitPoints(oTarget)-1, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_NORMAL);
                 AssignCommand( oTarget, ClearAllActions());
                 AssignCommand( oTarget, ActionPlayAnimation( ANIMATION_LOOPING_DEAD_FRONT, 1.0, 99999.0));
                 DelayCommand(0.5, SetCommandable( FALSE, oTarget)); break;
        case 71: eA = EffectCutsceneDominated();break;
        case 72: eA = EffectCutsceneGhost(); break;
        case 73: eA = EffectCutsceneImmobilize(); break;
        case 74: eA = EffectCutsceneParalyze(); break;
        case 75: nBug = -1; break;  //special case for combo death effect
        case 81: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_POISON) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 82: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_DISEASE) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 83: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_BLINDNESS) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 84: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_CURSE) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 85: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_FRIGHTENED) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 86: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_STUNNED) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 87: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_SILENCE) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 88: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;
        case 89: SetCommandable(TRUE, oTarget);
                 AssignCommand(oTarget, ClearAllActions()); break;
        case 80: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if (GetEffectType(eEffect) == EFFECT_TYPE_PETRIFY) RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;//Added July 5, 2003

// 99 is a duplicate instance - simple copy. - Demetrious
        case 91: SetLocalString(oUser, "EffectSetting", "dmfi_stunduration");
                 CreateSetting(oUser);
        case 92: SetDMFIPersistentInt("dmfi", "DamageModifier", nDNum+1); SetCustomToken(20780, IntToString(nDNum+1));;  break;
        case 93:
                if (nDNum==1)
                {
                    FloatingTextStringOnCreature("Illegal operation:  Minimum modifier is 1.", oUser);
                    break;
                }
                else
                {
                    SetDMFIPersistentInt("dmfi", "DamageModifier", nDNum-1); SetCustomToken(20780, IntToString(nDNum-1)); ;break;
                    break;
                }
        case 94: ReportImmunity(oTarget, oUser); break;
        case 95: DMFI_NextTarget(oTarget, oUser); break;
        case 99: SetLocalString(oUser, "EffectSetting", "SaveEffectAmount");
                  CreateSetting(oUser); break;
        case 101: eT = EffectSavingThrowDecrease(SAVING_THROW_FORT, nSaveAmount); break;
        case 102: eT = EffectSavingThrowDecrease(SAVING_THROW_REFLEX, nSaveAmount); break;
        case 103: eT = EffectSavingThrowDecrease(SAVING_THROW_WILL, nSaveAmount); break;
        case 104: eT = EffectSavingThrowIncrease(SAVING_THROW_FORT, nSaveAmount); break;
        case 105: eT = EffectSavingThrowIncrease(SAVING_THROW_REFLEX, nSaveAmount); break;
        case 106: eT = EffectSavingThrowIncrease(SAVING_THROW_WILL, nSaveAmount); break;
        case 107: eT = EffectSavingThrowDecrease(SAVING_THROW_ALL, nSaveAmount); break;
        case 108: eT = EffectSavingThrowIncrease(SAVING_THROW_ALL, nSaveAmount); break;
        case 109: SetLocalString(oUser, "EffectSetting", "SaveEffectAmount");
                  CreateSetting(oUser);
        case 100: eEffect = GetFirstEffect(oTarget);
                 while (GetIsEffectValid(eEffect))
                 {
                    if ((GetEffectType(eEffect) == EFFECT_TYPE_SAVING_THROW_INCREASE)
                     ||(GetEffectType(eEffect) == EFFECT_TYPE_SAVING_THROW_DECREASE))
                            RemoveEffect(oTarget, eEffect);
                    eEffect = GetNextEffect(oTarget);
                 } break;//Added July 5, 2003



        default: break;
    }
//code down here to apply the effects an then go back and see if the
//player successfully saved or did not for the diseases and poisons.

    if ((GetEffectType(eD)!= EFFECT_TYPE_INVALIDEFFECT) ||
    (GetEffectType(eVis) != EFFECT_TYPE_INVALIDEFFECT))
    {
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, eD, oTarget);
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, eVis, oTarget);
        return;
    }
    if (GetEffectType(eA)!= EFFECT_TYPE_INVALIDEFFECT)
    {
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, eA, oTarget);
        DelayCommand(5.0, CheckForEffect(eA, oTarget, oUser));
        return;
    }
    if ((GetEffectType(eT)!= EFFECT_TYPE_INVALIDEFFECT) || (nBug ==1))
    {
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eT, oTarget, fDuration);

        if ((GetEffectType(eT)==EFFECT_TYPE_SAVING_THROW_INCREASE) ||
        (GetEffectType(eT)==EFFECT_TYPE_SAVING_THROW_DECREASE))
        {
            DelayCommand(1.0, FloatingTextStringOnCreature("Target Saves: Fortitude " + IntToString(GetFortitudeSavingThrow(oTarget))
                        + " Reflex " + IntToString(GetReflexSavingThrow(oTarget)) + " Will " + IntToString(GetWillSavingThrow(oTarget)), oUser));
        }
        return;
    }
    if (nBug == -1)
    {
        object oFollowMe = GetFirstFactionMember(oTarget, TRUE);

        if (!GetIsObjectValid(oFollowMe))
            oFollowMe = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC, oTarget, 1,CREATURE_TYPE_IS_ALIVE, TRUE);

        if (GetIsDM(oFollowMe) || GetIsDMPossessed(oFollowMe))
            oFollowMe = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC, oTarget, 2,CREATURE_TYPE_IS_ALIVE, TRUE);

        if (!GetIsObjectValid(oFollowMe))
            oFollowMe = oUser;

        AssignCommand(oFollowMe, ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectCutsceneDominated(), oTarget));
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectCutsceneGhost(), oTarget);
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), oTarget);
    }

    return;
}
