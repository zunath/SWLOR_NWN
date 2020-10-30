void CreateSetting(object oUser)
{
    object oSetting = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_setting", GetLocation(oUser));
    DelayCommand(0.5f, AssignCommand(oSetting, ActionSpeakString(GetLocalString(oUser, "EffectSetting") + " is currently set at " + FloatToString(GetLocalFloat(oUser, GetLocalString(oUser, "EffectSetting"))))));
    SetLocalObject(oSetting, "MyMaster", oUser);
    SetLocalInt(oSetting, "hls_Listening", 1); //listen to all text
    SetListening(oSetting, TRUE);          //be sure NPC is listening
}

//An FX Wand function
void FXWand_Firestorm(object oDM)
{

   // FireStorm Effect
       location lDMLoc = GetLocation ( oDM);


   // tell the DM object to rain fire and destruction
   AssignCommand ( oDM, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect ( VFX_FNF_METEOR_SWARM), lDMLoc));
   AssignCommand ( oDM, DelayCommand (1.0, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect (VFX_FNF_SCREEN_SHAKE), lDMLoc)));

   // create some fires
   object oTargetArea = GetArea(oDM);
   int nXPos, nYPos, nCount;
   for(nCount = 0; nCount < 15; nCount++)
  {
      nXPos = Random(30) - 15;
      nYPos = Random(30) - 15;

      vector vNewVector = GetPosition(oDM);
      vNewVector.x += nXPos;
      vNewVector.y += nYPos;

      location lFireLoc = Location(oTargetArea, vNewVector, 0.0);
      object oFire = CreateObject ( OBJECT_TYPE_PLACEABLE, "plc_flamelarge", lFireLoc, FALSE);
      object oDust = CreateObject ( OBJECT_TYPE_PLACEABLE, "plc_dustplume", lFireLoc, FALSE);
      DelayCommand ( 10.0, DestroyObject ( oFire));
      DelayCommand ( 14.0, DestroyObject ( oDust));
   }

}

//An FX Wand function
void FXWand_Earthquake(object oDM)
{
   // Earthquake Effect by Jhenne, 06/29/02
   // declare variables used for targetting and commands.
   location lDMLoc = GetLocation ( oDM);

   // tell the DM object to shake the screen
   AssignCommand( oDM, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_SCREEN_SHAKE), lDMLoc));
   AssignCommand ( oDM, DelayCommand( 2.8, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect ( VFX_FNF_SCREEN_BUMP), lDMLoc)));
   AssignCommand ( oDM, DelayCommand( 3.0, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect ( VFX_FNF_SCREEN_SHAKE), lDMLoc)));
   AssignCommand ( oDM, DelayCommand( 4.5, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect ( VFX_FNF_SCREEN_BUMP), lDMLoc)));
   AssignCommand ( oDM, DelayCommand( 5.8, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect ( VFX_FNF_SCREEN_BUMP), lDMLoc)));
   // tell the DM object to play an earthquake sound
   AssignCommand ( oDM, PlaySound ("as_cv_boomdist1"));
   AssignCommand ( oDM, DelayCommand ( 2.0, PlaySound ("as_wt_thunderds3")));
   AssignCommand ( oDM, DelayCommand ( 4.0, PlaySound ("as_cv_boomdist1")));
   // create a dust plume at the DM and clicking location
   object oTargetArea = GetArea(oDM);
   int nXPos, nYPos, nCount;
   for(nCount = 0; nCount < 15; nCount++)
   {
      nXPos = Random(30) - 15;
      nYPos = Random(30) - 15;

      vector vNewVector = GetPosition(oDM);
      vNewVector.x += nXPos;
      vNewVector.y += nYPos;

      location lDustLoc = Location(oTargetArea, vNewVector, 0.0);
      object oDust = CreateObject ( OBJECT_TYPE_PLACEABLE, "plc_dustplume", lDustLoc, FALSE);
      DelayCommand ( 4.0, DestroyObject ( oDust));
   }
}

//An FX Wand function
void FXWand_Lightning(object oDM, location lDMLoc)
{
   // Lightning Strike by Jhenne. 06/29/02
   // tell the DM object to create a Lightning visual effect at targetted location
   AssignCommand( oDM, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_LIGHTNING_M), lDMLoc));
   // tell the DM object to play a thunderclap
   AssignCommand ( oDM, PlaySound ("as_wt_thundercl3"));
   // create a scorch mark where the lightning hit
   object oScorch = CreateObject ( OBJECT_TYPE_PLACEABLE, "plc_weathmark", lDMLoc, FALSE);
   object oTargetArea = GetArea(oDM);
   int nXPos, nYPos, nCount;
   for(nCount = 0; nCount < 5; nCount++)
   {
      nXPos = Random(10) - 5;
      nYPos = Random(10) - 5;

      vector vNewVector = GetPositionFromLocation(lDMLoc);
      vNewVector.x += nXPos;
      vNewVector.y += nYPos;

      location lNewLoc = Location(oTargetArea, vNewVector, 0.0);
      AssignCommand( oDM, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_LIGHTNING_S), lNewLoc));
   }
   DelayCommand ( 20.0, DestroyObject ( oScorch));
}

void FnFEffect(object oUser, int iVFX, location lEffect, float fDelay)
{
    if (fDelay>2.0) FloatingTextStringOnCreature("Delay effect created", oUser, FALSE);
    DelayCommand( fDelay, ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(iVFX),lEffect));
}

void main()
{
    int iDayMusic, iNightMusic, iBattleMusic;
    int iEffect = GetLocalInt(OBJECT_SELF, "dmfi_univ_int");
    location lEffect = GetLocalLocation(OBJECT_SELF, "dmfi_univ_location");
    object oUser = OBJECT_SELF;
    float fDelay;
    float fDuration;
    float fBeamDuration;
    object oTarget;

    fDelay = GetLocalFloat(oUser, "dmfi_effectdelay");
    fDuration = GetLocalFloat(oUser, "dmfi_effectduration");
    fBeamDuration = GetLocalFloat(oUser, "dmfi_beamduration");

    if (!GetIsObjectValid(GetLocalObject(oUser, "dmfi_univ_target")))
        oTarget = oUser;
    else
        oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    switch(iEffect)
    {
        //SoU/HotU Duration Effects(must have a target)
        case 101: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BIGBYS_CLENCHED_FIST), oTarget, fDuration); break;
        case 102: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BIGBYS_CRUSHING_HAND), oTarget, fDuration); break;
        case 103: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BIGBYS_GRASPING_HAND), oTarget, fDuration); break;
        case 104: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BIGBYS_INTERPOSING_HAND), oTarget, fDuration); break;
        case 105: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_ICESKIN), oTarget, fDuration); break;
        case 106: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_INFERNO), oTarget, fDuration); break;
        case 107: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PIXIEDUST), oTarget, fDuration); break;
        case 108: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), oTarget, fDuration); break;
        case 109: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_FREEZE_ANIMATION), oTarget, fDuration); break;
        case 100: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_GHOSTLY_PULSE), oTarget, fDuration); break;
        //Magical Duration Effects
        case 10: ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_CALTROPS),lEffect, fDuration); break;
        case 11: ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_TENTACLE),lEffect, fDuration); break;
        case 12: ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_WEB_MASS),lEffect, fDuration); break;
        case 13: FnFEffect(oUser, VFX_FNF_GAS_EXPLOSION_MIND,lEffect, fDelay); break;
        case 14: FnFEffect(oUser, VFX_FNF_LOS_HOLY_30,lEffect, fDelay); break;
        case 15: FnFEffect(oUser, VFX_FNF_LOS_EVIL_30,lEffect, fDelay); break;
        case 16: FnFEffect(oUser, VFX_FNF_SMOKE_PUFF,lEffect, fDelay); break;
        case 17: FnFEffect(oUser, VFX_FNF_GAS_EXPLOSION_NATURE,lEffect, fDelay); break;
        case 18: FnFEffect(oUser, VFX_FNF_DISPEL_DISJUNCTION,lEffect, fDelay); break;
        case 19: FnFEffect(oUser, VFX_FNF_GAS_EXPLOSION_EVIL,lEffect, fDelay); break;
        //Magical Status Effects (must have a target)
        case 21: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROT_BARKSKIN), oTarget, fDuration); break;
        case 22: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROT_GREATER_STONESKIN), oTarget, fDuration); break;
        case 23: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_ENTANGLE), oTarget, fDuration); break;
        case 24: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_ETHEREAL_VISAGE), oTarget, fDuration); break;
        case 25: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_GHOSTLY_VISAGE), oTarget, fDuration); break;
        case 26: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_INVISIBILITY), oTarget, fDuration); break;
        case 27: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BARD_SONG), oTarget, fDuration); break;
        case 28: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_GLOBE_INVULNERABILITY), oTarget, fDuration); break;
        case 29: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PARALYZED), oTarget, fDuration); break;
        case 20: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROT_SHADOW_ARMOR), oTarget, fDuration); break;
        //Magical Burst Effects
        case 31: FnFEffect(oUser, VFX_FNF_FIREBALL,lEffect, fDelay); break;
        case 32: FnFEffect(oUser, VFX_FNF_FIRESTORM,lEffect, fDelay); break;
        case 33: FnFEffect(oUser, VFX_FNF_HORRID_WILTING,lEffect, fDelay); break;
        case 34: FnFEffect(oUser, VFX_FNF_HOWL_WAR_CRY,lEffect, fDelay); break;
        case 35: FnFEffect(oUser, VFX_FNF_IMPLOSION,lEffect, fDelay); break;
        case 36: FnFEffect(oUser, VFX_FNF_PWKILL,lEffect, fDelay); break;
        case 37: FnFEffect(oUser, VFX_FNF_PWSTUN,lEffect, fDelay); break;
        case 38: FnFEffect(oUser, VFX_FNF_SOUND_BURST,lEffect, fDelay); break;
        case 39: FnFEffect(oUser, VFX_FNF_STRIKE_HOLY,lEffect, fDelay); break;
        case 30: FnFEffect(oUser, VFX_FNF_WORD,lEffect, fDelay); break;
          //Lighting Effects
        case 41: ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BLACKOUT),lEffect, fDuration); break;
        case 42: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_ANTI_LIGHT_10),oTarget, fDuration); break;
        case 43: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_BLUE_20),oTarget, fDuration); break;
        case 44: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_GREY_20),oTarget, fDuration); break;
        case 45: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_ORANGE_20),oTarget, fDuration); break;
        case 46: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_PURPLE_20),oTarget, fDuration); break;
        case 47: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_RED_20),oTarget, fDuration); break;
        case 48: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_WHITE_20),oTarget, fDuration); break;
        case 49: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_YELLOW_20),oTarget, fDuration); break;
                 //Beam Effects
        case 50: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_CHAIN, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 51: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_COLD, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 52: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_EVIL, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 53: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_FIRE, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 54: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_FIRE_LASH, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 55: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_HOLY, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 56: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_LIGHTNING, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 57: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_MIND, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 58: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_ODD, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;
        case 59: ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_COLD, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_EVIL, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_FIRE, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_FIRE_LASH, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_HOLY, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_LIGHTNING, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_MIND, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration);
                ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectBeam(VFX_BEAM_ODD, oUser, BODY_NODE_CHEST, FALSE), oTarget, fBeamDuration); break;

        //Environmental Effects
        case 60: FnFEffect(oUser, VFX_FNF_NATURES_BALANCE,lEffect, fDelay);break;
        case 61: FXWand_Lightning(oTarget, lEffect); break;
        case 62: FXWand_Firestorm(oTarget); break;
        case 63: FXWand_Earthquake(oTarget); break;
        case 64: FnFEffect(oUser, VFX_FNF_ICESTORM,lEffect, fDelay); break;
        case 65: FnFEffect(oUser, VFX_FNF_SUNBEAM,lEffect, fDelay); break;
        case 66: SetWeather(GetArea(oUser), WEATHER_CLEAR); break;
        case 67: SetWeather(GetArea(oUser), WEATHER_RAIN); break;
        case 68: SetWeather(GetArea(oUser), WEATHER_SNOW); break;
        case 69: SetWeather(GetArea(oUser), WEATHER_USE_AREA_SETTINGS); break;
        //Summon Effects
        case 71: FnFEffect(oUser, VFX_FNF_SUMMON_MONSTER_1,lEffect, fDelay); break;
        case 72: FnFEffect(oUser, VFX_FNF_SUMMON_MONSTER_2,lEffect, fDelay); break;
        case 73: FnFEffect(oUser, VFX_FNF_SUMMON_MONSTER_3,lEffect, fDelay); break;
        case 74: FnFEffect(oUser, VFX_FNF_SUMMON_CELESTIAL,lEffect, fDelay); break;
        case 75: FnFEffect(oUser, VFX_FNF_SUMMONDRAGON,lEffect, fDelay); break;
        case 76: FnFEffect(oUser, VFX_FNF_SUMMON_EPIC_UNDEAD,lEffect, fDelay); break;
        case 77: FnFEffect(oUser, VFX_FNF_SUMMON_GATE,lEffect, fDelay); break;
        case 78: FnFEffect(oUser, VFX_FNF_SUMMON_UNDEAD,lEffect, fDelay); break;
        case 79: FnFEffect(oUser, VFX_FNF_UNDEAD_DRAGON,lEffect, fDelay); break;
        case 70: FnFEffect(oUser, VFX_FNF_WAIL_O_BANSHEES,lEffect, fDelay); break;
        //SoU/HotU Effects
        case 80: ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(322), oTarget, fDuration); break;
        case 81: ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(132), oTarget, fDuration); break;
        case 82: ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(133), oTarget, fDuration); break;
        case 83: ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(136), oTarget, fDuration); break;
        case 84: ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(137), oTarget, fDuration); break;
        case 85: FnFEffect(oUser, VFX_FNF_DEMON_HAND,lEffect, fDelay); break;
        case 86: FnFEffect(oUser, VFX_FNF_ELECTRIC_EXPLOSION,lEffect, fDelay); break;
        case 87: FnFEffect(oUser, VFX_FNF_GREATER_RUIN,lEffect, fDelay); break;
        case 88: FnFEffect(oUser, VFX_FNF_MYSTICAL_EXPLOSION,lEffect, fDelay); break;
        case 89: FnFEffect(oUser, VFX_FNF_SWINGING_BLADE,lEffect, fDelay); break;
        //Settings
        case 91:
        SetLocalString(oUser, "EffectSetting", "dmfi_effectduration");
        CreateSetting(oUser);
        break;
        case 92:
        SetLocalString(oUser, "EffectSetting", "dmfi_effectdelay");
        CreateSetting(oUser);
        break;
        case 93:
        SetLocalString(oUser, "EffectSetting", "dmfi_beamduration");
        CreateSetting(oUser);
        break;
        case 94: //Change Day Music
        iDayMusic = MusicBackgroundGetDayTrack(GetArea(oUser)) + 1;
        if (iDayMusic > 33) iDayMusic = 49;
        if (iDayMusic > 55) iDayMusic = 1;
        MusicBackgroundStop(GetArea(oUser));
        MusicBackgroundChangeDay(GetArea(oUser), iDayMusic);
        MusicBackgroundPlay(GetArea(oUser));
        break;
        case 95: //Change Night Music
        iNightMusic = MusicBackgroundGetDayTrack(GetArea(oUser)) + 1;
        if (iNightMusic > 33) iNightMusic = 49;
        if (iNightMusic > 55) iNightMusic = 1;
        MusicBackgroundStop(GetArea(oUser));
        MusicBackgroundChangeNight(GetArea(oUser), iNightMusic);
        MusicBackgroundPlay(GetArea(oUser));
        break;
        case 96: //Play Background Music
        MusicBackgroundPlay(GetArea(oUser));
        break;
        case 97: //Stop Background Music
        MusicBackgroundStop(GetArea(oUser));
        break;
        case 98: //Change and Play Battle Music
        iBattleMusic = MusicBackgroundGetBattleTrack(GetArea(oUser)) + 1;
        if (iBattleMusic < 34 || iBattleMusic > 48) iBattleMusic = 34;
        MusicBattleStop(GetArea(oUser));
        MusicBattleChange(GetArea(oUser), iBattleMusic);
        MusicBattlePlay(GetArea(oUser));
        break;
        case 99: //Stop Battle Music
        MusicBattleStop(GetArea(oUser));
        break;

        default: break;
    }
    DeleteLocalObject(oUser, "EffectTarget");
    return;
}
