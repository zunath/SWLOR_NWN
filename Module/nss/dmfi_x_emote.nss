//Smoking Function by Jason Robinson
location GetLocationAboveAndInFrontOf(object oPC, float fDist, float fHeight)
{
    float fDistance = -fDist;
    object oTarget = (oPC);
    object oArea = GetArea(oTarget);
    vector vPosition = GetPosition(oTarget);
    vPosition.z += fHeight;
    float fOrientation = GetFacing(oTarget);
    vector vNewPos = AngleToVector(fOrientation);
    float vZ = vPosition.z;
    float vX = vPosition.x - fDistance * vNewPos.x;
    float vY = vPosition.y - fDistance * vNewPos.y;
    fOrientation = GetFacing(oTarget);
    vX = vPosition.x - fDistance * vNewPos.x;
    vY = vPosition.y - fDistance * vNewPos.y;
    vNewPos = AngleToVector(fOrientation);
    vZ = vPosition.z;
    vNewPos = Vector(vX, vY, vZ);
    return Location(oArea, vNewPos, fOrientation);
}

//Smoking Function by Jason Robinson
void SmokePipe(object oActivator)
{
    string sEmote1 = "*puffs on a pipe*";
    string sEmote2 = "*inhales from a pipe*";
    string sEmote3 = "*pulls a mouthful of smoke from a pipe*";
    float fHeight = 1.7;
    float fDistance = 0.1;
    // Set height based on race and gender
    if (GetGender(oActivator) == GENDER_MALE)
    {
        switch (GetRacialType(oActivator))
        {
            case RACIAL_TYPE_HUMAN:
            case RACIAL_TYPE_HALFELF: fHeight = 1.7; fDistance = 0.12; break;
            case RACIAL_TYPE_ELF: fHeight = 1.55; fDistance = 0.08; break;
            case RACIAL_TYPE_GNOME:
            case RACIAL_TYPE_HALFLING: fHeight = 1.15; fDistance = 0.12; break;
            case RACIAL_TYPE_DWARF: fHeight = 1.2; fDistance = 0.12; break;
            case RACIAL_TYPE_HALFORC: fHeight = 1.9; fDistance = 0.2; break;
        }
    }
    else
    {
        // FEMALES
        switch (GetRacialType(oActivator))
        {
            case RACIAL_TYPE_HUMAN:
            case RACIAL_TYPE_HALFELF: fHeight = 1.6; fDistance = 0.12; break;
            case RACIAL_TYPE_ELF: fHeight = 1.45; fDistance = 0.12; break;
            case RACIAL_TYPE_GNOME:
            case RACIAL_TYPE_HALFLING: fHeight = 1.1; fDistance = 0.075; break;
            case RACIAL_TYPE_DWARF: fHeight = 1.2; fDistance = 0.1; break;
            case RACIAL_TYPE_HALFORC: fHeight = 1.8; fDistance = 0.13; break;
        }
    }
    location lAboveHead = GetLocationAboveAndInFrontOf(oActivator, fDistance, fHeight);
    // emotes
    switch (d3())
    {
        case 1: AssignCommand(oActivator, ActionSpeakString(sEmote1)); break;
        case 2: AssignCommand(oActivator, ActionSpeakString(sEmote2)); break;
        case 3: AssignCommand(oActivator, ActionSpeakString(sEmote3));break;
    }
    // glow red
    AssignCommand(oActivator, ActionDoCommand(ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_RED_5), oActivator, 0.15)));
    // wait a moment
    AssignCommand(oActivator, ActionWait(3.0));
    // puff of smoke above and in front of head
    AssignCommand(oActivator, ActionDoCommand(ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_SMOKE_PUFF), lAboveHead)));
    // if female, turn head to left
    if ((GetGender(oActivator) == GENDER_FEMALE) && (GetRacialType(oActivator) != RACIAL_TYPE_DWARF))
        AssignCommand(oActivator, ActionPlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_LEFT, 1.0, 5.0));
}

void EmoteDance(object oPC)
{
    object oRightHand = GetItemInSlot(INVENTORY_SLOT_RIGHTHAND,oPC);
    object oLeftHand =  GetItemInSlot(INVENTORY_SLOT_LEFTHAND,oPC);

    AssignCommand(oPC,ActionUnequipItem(oRightHand));
    AssignCommand(oPC,ActionUnequipItem(oLeftHand));

    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY2,1.0));
    AssignCommand(oPC,ActionDoCommand(PlayVoiceChat(VOICE_CHAT_LAUGH,oPC)));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_TALK_LAUGHING, 2.0, 2.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY1,1.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY3,2.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_GET_MID, 3.0, 1.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_TALK_FORCEFUL,1.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY2,1.0));
    AssignCommand(oPC,ActionDoCommand(PlayVoiceChat(VOICE_CHAT_LAUGH,oPC)));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_TALK_LAUGHING, 2.0, 2.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY1,1.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY3,2.0));
    AssignCommand(oPC,ActionDoCommand(PlayVoiceChat(VOICE_CHAT_LAUGH,oPC)));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_LOOPING_GET_MID, 3.0, 1.0));
    AssignCommand(oPC,ActionPlayAnimation( ANIMATION_FIREFORGET_VICTORY2,1.0));

    AssignCommand(oPC,ActionDoCommand(ActionEquipItem(oLeftHand,INVENTORY_SLOT_LEFTHAND)));
    AssignCommand(oPC,ActionDoCommand(ActionEquipItem(oRightHand,INVENTORY_SLOT_RIGHTHAND)));
}

void SitInNearestChair(object oPC)
{
    object oSit,oRightHand,oLeftHand,oChair,oCouch,oBenchPew,oStool;
    float fDistSit;int nth;
    // get the closest chair, couch bench or stool
   nth = 1;oChair = GetNearestObjectByTag("Chair", oPC,nth);
   while(oChair != OBJECT_INVALID &&  GetSittingCreature(oChair) != OBJECT_INVALID)
   {nth++;oChair = GetNearestObjectByTag("Chair", oPC,nth);}

   nth = 1;oCouch = GetNearestObjectByTag("Couch", oPC,nth);
   while(oCouch != OBJECT_INVALID && GetSittingCreature(oCouch) != OBJECT_INVALID)
      {nth++;oChair = GetNearestObjectByTag("Couch", oPC,nth);}

   nth = 1;oBenchPew = GetNearestObjectByTag("BenchPew", oPC,nth);
   while(oBenchPew != OBJECT_INVALID && GetSittingCreature(oBenchPew) != OBJECT_INVALID)
      {nth++;oChair = GetNearestObjectByTag("BenchPew", oPC,nth);}
    /* 1.27 bug
       nth = 1;oStool = GetNearestObjectByTag("Stool", oPC,nth);
       while(oStool != OBJECT_INVALID && GetSittingCreature(oStool) != OBJECT_INVALID)
          {nth++;oStool = GetNearestObjectByTag("Stool", oPC,nth);}
    */
    // get the distance between the user and each object (-1.0 is the result if no
    // object is found
    float fDistanceChair = GetDistanceToObject(oChair);
    float fDistanceBench = GetDistanceToObject(oBenchPew);
    float fDistanceCouch = GetDistanceToObject(oCouch);
    float fDistanceStool = GetDistanceToObject(oStool);

    // if any of the objects are invalid (not there), change the return value
    // to a high number so the distance math can work
    if (fDistanceChair == -1.0)
    {fDistanceChair =1000.0;}

    if (fDistanceBench == -1.0)
    {fDistanceBench = 1000.0;}

    if (fDistanceCouch == -1.0)
    {fDistanceCouch = 1000.0;}

    if (fDistanceStool == -1.0)
        {fDistanceStool = 1000.0;}

    // find out which object is closest to the PC
    if (fDistanceChair<fDistanceBench && fDistanceChair<fDistanceCouch && fDistanceChair<fDistanceStool)
        {oSit=oChair;fDistSit=fDistanceChair;}
    else if (fDistanceBench<fDistanceChair && fDistanceBench<fDistanceCouch && fDistanceBench<fDistanceStool)
        {oSit=oBenchPew;fDistSit=fDistanceBench;}
    else if (fDistanceCouch<fDistanceChair && fDistanceCouch<fDistanceBench && fDistanceCouch<fDistanceStool)
        {oSit=oCouch;fDistSit=fDistanceCouch;}
    else
//if (fDistanceStool<fDistanceChair && fDistanceStool<fDistanceBench && fDistanceStool<fDistanceCouch)
{oSit=oStool;fDistSit=fDistanceStool;}

 if(oSit !=  OBJECT_INVALID && fDistSit < 12.0)
    {
     // if no one is sitting in the object the PC is closest to, have him sit in it
     if (GetSittingCreature(oSit) == OBJECT_INVALID)
         {
           oRightHand = GetItemInSlot(INVENTORY_SLOT_RIGHTHAND,oPC);
           oLeftHand =  GetItemInSlot(INVENTORY_SLOT_LEFTHAND,oPC);
           AssignCommand(oPC,ActionMoveToObject(oSit,FALSE,2.0)); //:: Presumably this will be fixed in a patch so that Plares will not run to chair
           ActionUnequipItem(oRightHand); //:: Added to resolve clipping issues when seated
           ActionUnequipItem(oLeftHand);  //:: Added to resolve clipping issues when seated
           ActionDoCommand(AssignCommand(oPC,ActionSit(oSit)));

        }
      else
        {SendMessageToPC(oPC,"The nearest chair is already taken ");}
    }
  else
    {SendMessageToPC(oPC,"There are no chairs nearby");}
}

void main()
{
    int iEmote = GetLocalInt(OBJECT_SELF, "dmfi_univ_int");
    object oUser = OBJECT_SELF;
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    if (!GetIsObjectValid(oTarget))
        oTarget = oUser;
    float fDur = 9999.0f; //Duration

    switch(iEmote)
    {
        case 1: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DODGE_SIDE, 1.0)); break;
        case 2: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DRINK, 1.0)); break;
        case 3: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DODGE_DUCK, 1.0)); break;
        case 4: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_DEAD_BACK, 1.0, fDur)); break;
        case 5: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_DEAD_FRONT, 1.0, fDur)); break;
        case 6: AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0)); DelayCommand(3.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0)));break;
        case 7: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)); break;
        case 81: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_TALK_PLEADING, 1.0, fDur)); break;
        case 82: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_CONJURE1, 1.0, fDur)); break;
        case 83: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_CONJURE2, 1.0, fDur)); break;
        case 84: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_GET_LOW, 1.0, fDur)); break;
        case 85: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_GET_MID, 1.0, fDur)); break;
        case 86: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_MEDITATE, 1.0, fDur)); break;
        case 87: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_TALK_FORCEFUL, 1.0, fDur)); break;
        case 88: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_WORSHIP, 1.0, fDur)); break;
        case 10: if (!GetLocalInt(oTarget, "hls_emotemute")) FloatingTextStringOnCreature("*emote* commands are off", oTarget, FALSE);
                 else FloatingTextStringOnCreature("*emote* commands are on", oTarget, FALSE);
                 SetLocalInt(oTarget, "hls_emotemute", abs(GetLocalInt(oTarget, "hls_emotemute") - 1)); break;
        case 91: EmoteDance(oTarget); break;
        case 92: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_PAUSE_DRUNK, 1.0, fDur)); break;
        case 93: AssignCommand(oTarget, ActionForceFollowObject(GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC, oTarget), 2.0f)); break;
        case 94: SitInNearestChair(oTarget); break;
        case 95: AssignCommand(oTarget, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)); DelayCommand(1.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_DRINK, 1.0))); DelayCommand(3.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)));break;
        case 96: AssignCommand(oTarget, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)); DelayCommand(1.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0))); DelayCommand(3.0f, AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, fDur)));break;
        case 97: AssignCommand(oTarget, PlayAnimation( ANIMATION_LOOPING_SPASM, 1.0, fDur)); break;
        case 98: SmokePipe(oTarget); break;
        default: break;
    }
}
