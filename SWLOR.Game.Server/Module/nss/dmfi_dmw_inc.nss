// VOICE CONFIGURATION - NEW IN 1.07 and UP

// Set this to 0 if you want to DISABLE listening by NPCs for performance reasons.
// See readme for additional information regarding possible issues and effects.
const int DMFI_LISTENING_GLOBAL = 1;


// NOTE: OMW_COLORS is an invisible object that must be present in your module.
// It has high ascii characters in the name and is used to get the color codes.
// This was ripped wholeheartedly by an example posted by Richterm on the bioboards.

string DST_COLOR_TAGS = GetName(GetObjectByTag("dem_color_text"));
string DST_COLOR_WHITE = GetSubString(DST_COLOR_TAGS, 0, 6);
string DST_COLOR_YELLOW = GetSubString(DST_COLOR_TAGS, 6, 6);
string DST_COLOR_MAGENTA = GetSubString(DST_COLOR_TAGS, 12, 6);
string DST_COLOR_CYAN = GetSubString(DST_COLOR_TAGS, 18, 6);
string DST_COLOR_RED = GetSubString(DST_COLOR_TAGS, 24, 6);
string DST_COLOR_GREEN = GetSubString(DST_COLOR_TAGS, 30, 6);
string DST_COLOR_BLUE = GetSubString(DST_COLOR_TAGS, 36, 6);

// Colors for each type of roll. Change the colors if you like.
string DMFI_ROLL_COLOR = DST_COLOR_CYAN;
string DST_COLOR_NORMAL = DST_COLOR_WHITE;

int DMW_START_CUSTOM_TOKEN = 8000;

//Retrieve targetting information
object oMySpeaker = GetLastSpeaker();
object oMyTarget = GetLocalObject(oMySpeaker, "dmfi_univ_target");
location lMyLoc = GetLocalLocation(oMySpeaker, "dmfi_univ_location");

// checks if a nearby object is destroyable
int dmwand_isnearbydestroyable();
// Check if the target can be created with CreateObject
int dmwand_istargetcreateable();
//Check if target is a destroyable object
int dmwand_istargetdestroyable();
// checks if the wand was NOT clicked on an object
int dmwand_istargetinvalid();
// check if the target has an inventory
int dmwand_istargetinventory();
//Check if the target is not the wand's user
int dmwand_istargetnotme();
//Check if target is an NPC or monster
int dmwand_istargetnpc();
//Check if the target is a PC
int dmwand_istargetpc();
//Check if the target is a PC and not me
int dmwand_istargetpcnme();
// Check if the target is a PC or NPC
// uses the CON score currently
int dmwand_istargetpcornpc();
//Check if the target is a PC or an npc and not me
int dmwand_istargetpcornpcnme();
// Check if target is a placeable
int dmwand_istargetplaceable();
//bulds the conversion
int dmwand_BuildConversationDialog(int nCurrent, int nChoice, string sConversation, string sParams);
int dmw_conv_ListPlayers(int nCurrent, int nChoice, string sParams = "");
int dmw_conv_Start(int nCurrent, int nChoice, string sParams = "");
void dmwand_BuildConversation(string sConversation, string sParams);
void dmwand_StartConversation();

// DMFI Color Text function.  It returns a colored string.
// sText is the string that will be colored and sColor is the color
// options: yellow, magenta, cyan, red, green, blue - truncated at first letter
// Ex: sMsg = ColorText(sMsg, "y");  //Add the include file - yields yellow colored msg.
string ColorText(string sText, string sColor);
string ColorText(string sText, string sColor)
{
    string sApply = DST_COLOR_NORMAL;
    string sTest = GetStringLowerCase(GetStringLeft(sColor, 1));
    if (sTest=="y")  sApply = DST_COLOR_YELLOW;
    else if (sTest == "m") sApply = DST_COLOR_MAGENTA;
    else if (sTest == "c") sApply = DST_COLOR_CYAN;
    else if (sTest == "r") sApply = DST_COLOR_RED;
    else if (sTest == "g") sApply = DST_COLOR_GREEN;
    else if (sTest == "b") sApply = DST_COLOR_BLUE;

    string sFinal = sApply + sText + DST_COLOR_NORMAL;
    return sFinal;
}


int dmwand_isnearbydestroyable()
{
   object oMyTest = GetFirstObjectInShape(SHAPE_CUBE, 0.6, lMyLoc, FALSE, OBJECT_TYPE_ALL);
   int nTargetType = GetObjectType(oMyTest);
   return (GetIsObjectValid(oMyTest) && (! GetIsPC(oMyTest)) && ((nTargetType == OBJECT_TYPE_ITEM) || (nTargetType == OBJECT_TYPE_PLACEABLE) || (nTargetType == OBJECT_TYPE_CREATURE)));
}

int dmwand_istargetcreateable()
{
   if(! GetIsObjectValid(oMyTarget)) { return FALSE; }

   int nTargetType = GetObjectType(oMyTarget);
   return ((nTargetType == OBJECT_TYPE_ITEM) || (nTargetType == OBJECT_TYPE_PLACEABLE) || (nTargetType == OBJECT_TYPE_CREATURE));
}

int dmwand_istargetdestroyable()
{
   if(! GetIsObjectValid(oMyTarget)) { return FALSE; }

   int nTargetType = GetObjectType(oMyTarget);
   if(! GetIsPC(oMyTarget))
   {
      return ((nTargetType == OBJECT_TYPE_ITEM) || (nTargetType == OBJECT_TYPE_PLACEABLE) || (nTargetType == OBJECT_TYPE_CREATURE));
   }
   return FALSE;
}

int dmwand_istargetinvalid()
{
   return !GetIsObjectValid(oMyTarget);
}

int dmwand_istargetinventory()
{
   return (GetIsObjectValid(oMyTarget) && GetHasInventory(oMyTarget));
}

int dmwand_istargetnotme()
{
   return (GetIsObjectValid(oMyTarget) && (oMySpeaker != oMyTarget));
}

int dmwand_istargetpc()
{
   return (GetIsObjectValid(oMyTarget) && GetIsPC(oMyTarget));
}

int dmwand_istargetpcnme()
{
   return (GetIsObjectValid(oMyTarget) && GetIsPC(oMyTarget) && (oMySpeaker != oMyTarget));
}

int dmwand_istargetpcornpc()
{
   return (GetIsObjectValid(oMyTarget) && GetAbilityScore(oMyTarget, ABILITY_CONSTITUTION));
}

int dmwand_istargetnpc()
{
   return (dmwand_istargetpcornpc() && (!GetIsPC(oMyTarget)));
}

int dmwand_istargetpcornpcnme()
{
   return (dmwand_istargetpcornpc() && (oMySpeaker != oMyTarget));
}

int dmwand_istargetplaceable()
{
   if(! GetIsObjectValid(oMyTarget)) { return FALSE; }

   int nTargetType = GetObjectType(oMyTarget);
   return (nTargetType == OBJECT_TYPE_PLACEABLE);
}

int dmw_conv_Start(int nCurrent, int nChoice, string sParams = "")
{
   string sText = "";
   string sCall = "";
   string sCallParams = "";

   switch(nCurrent)
   {
      case 0:
         nCurrent = 0;
         sText =       "Hello there, DM.  What can I do for you?";
         sCall =       "";
         sCallParams = "";
         break;

      case 1:
         nCurrent = 1;
         if(dmwand_istargetpcnme())
         {
            sText =       "Penguin this player.";
            sCall =       "func_Toad";
            sCallParams = "";
            break;
         }
      case 2:
         nCurrent = 2;
         if(dmwand_istargetpcnme())
         {
            sText =       "Unpenguin this player.";
            sCall =       "func_Untoad";
            sCallParams = "";
            break;
         }
      case 3:
         nCurrent = 3;
         if(dmwand_istargetpcnme())
         {
            sText =       "Boot this player.";
            sCall =       "func_KickPC";
            sCallParams = "";
            break;
         }
      case 4:
         nCurrent = 4;
         if(dmwand_istargetinvalid())
         {
            sText =       "List all players...";
            sCall =       "conv_ListPlayers";
            sCallParams = "func_PlayerListConv";
            break;
         }

      case 5:
         nCurrent = 5;
         if(dmwand_istargetpcnme())
         {
            sText =       "Jump this player to my location.";
            sCall =       "func_JumpPlayerHere";
            sCallParams = "";
            break;
         }
      case 6:
         nCurrent = 6;
         if(dmwand_istargetpcnme())
         {
            sText =       "Jump me to this player's location.";
            sCall =       "func_JumpToPlayer";
            sCallParams = "";
            break;
         }
      case 7:
         nCurrent = 7;
         if(dmwand_istargetpcnme())
         {
            sText =       "Jump this player's party to my location.";
            sCall =       "func_JumpPartyHere";
            sCallParams = "";
            break;
         }
      default:
         nCurrent = 0;
         sText =       "";
         sCall =       "";
         sCallParams = "";
         break;
   }

   SetLocalString(oMySpeaker, "dmw_dialog" + IntToString(nChoice), sText);
   SetLocalString(oMySpeaker, "dmw_function" + IntToString(nChoice), sCall);
   SetLocalString(oMySpeaker, "dmw_params" + IntToString(nChoice), sCallParams);

   return nCurrent;
}

void DMFI_untoad(object oTarget, object oUser)
{
    if (GetLocalInt(oTarget, "toaded")==1)
    {
        effect eMyEffect = GetFirstEffect(oTarget);
        while(GetIsEffectValid(eMyEffect))
        {
            if(GetEffectType(eMyEffect) == EFFECT_TYPE_POLYMORPH || GetEffectType(eMyEffect) == EFFECT_TYPE_CUTSCENE_PARALYZE)
                RemoveEffect(oTarget, eMyEffect);

            eMyEffect = GetNextEffect(oTarget);
        }
    }
    else
    {
        FloatingTextStringOnCreature("Dude, he is no toad!", oUser);
    }
}

void DMFI_toad(object oTarget, object oUser)
{
    //This function now toggles the toad status   hahnsoo: DMFI 1.08
    if (GetLocalInt(oTarget, "toaded") == 1)
    {
        effect eMyEffect = GetFirstEffect(oTarget);
        while(GetIsEffectValid(eMyEffect))
        {
            if(GetEffectType(eMyEffect) == EFFECT_TYPE_POLYMORPH || GetEffectType(eMyEffect) == EFFECT_TYPE_CUTSCENE_PARALYZE)
                RemoveEffect(oTarget, eMyEffect);

            eMyEffect = GetNextEffect(oTarget);
        }
        FloatingTextStringOnCreature("Removed Penguin status from " + GetName(oTarget), oUser, FALSE);
        SetLocalInt(oTarget, "toaded", 0);
    }
    else
    {
        effect ePenguin = EffectPolymorph(POLYMORPH_TYPE_PENGUIN);
        effect eParalyze = EffectCutsceneParalyze();
        AssignCommand(oTarget, ApplyEffectToObject(DURATION_TYPE_PERMANENT, ePenguin, oTarget));
        AssignCommand(oTarget, ApplyEffectToObject(DURATION_TYPE_PERMANENT, eParalyze, oTarget));
        SetLocalInt(oTarget, "toaded", 1);
        FloatingTextStringOnCreature("Added Penguin status to " + GetName(oTarget), oUser, FALSE);
    }
}

void DMFI_jail(object oOther, object oUser)
{
    if (GetIsObjectValid(oOther) && !GetIsDM(oOther) && oOther != oUser)
    {
        object oJail = GetObjectByTag("dmfi_jail");
        if (!GetIsObjectValid(oJail))
            oJail = GetObjectByTag("dmfi_jail_default");
        AssignCommand(oOther, ClearAllActions());
        AssignCommand(oOther, JumpToObject(oJail));
        SendMessageToPC(oUser, GetName(oOther) + " (" + GetPCPublicCDKey(oOther) + ")/IP: " + GetPCIPAddress(oOther) + " - has been sent to Jail.");
    }
}

void dmwand_KickPC(object oTarget, object oUser)
{
   // Create a lightning strike, thunder, scorch mark, and random small
   // lightnings at target's location
   location lMyLoc = GetLocation (oTarget);
   AssignCommand( oUser, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_LIGHTNING_M), lMyLoc));
   AssignCommand ( oUser, PlaySound ("as_wt_thundercl3"));
   object oScorch = CreateObject ( OBJECT_TYPE_PLACEABLE, "plc_weathmark", lMyLoc, FALSE);
   object oTargetArea = GetArea(oUser);
   int nXPos, nYPos, nCount;
   for(nCount = 0; nCount < 5; nCount++)
   {
      nXPos = Random(10) - 5;
      nYPos = Random(10) - 5;

      vector vNewVector = GetPositionFromLocation(lMyLoc);
      vNewVector.x += nXPos;
      vNewVector.y += nYPos;

      location lNewLoc = Location(oTargetArea, vNewVector, 0.0);
      AssignCommand( oUser, ApplyEffectAtLocation ( DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_LIGHTNING_S), lNewLoc));
   }
   DelayCommand ( 20.0, DestroyObject ( oScorch));

   // 2018-12-03 zunath - Disable the message that someone was booted because it's currently sending to every player, not just DMs. Probably an NWNX bug.
   //SendMessageToAllDMs (GetName(oTarget) + " was booted from the game.  PC CD KEY: " + GetPCPublicCDKey(oTarget) + " PC IP ADDRESS: " + GetPCIPAddress(oTarget));
   PrintString(GetName(oTarget) + " was booted from the game.  PC CD KEY: " + GetPCPublicCDKey(oTarget) + " PC IP ADDRESS: " + GetPCIPAddress(oTarget));

   // Kick the target out of the game
   BootPC(oTarget);
}

void dmwand_JumpPlayerHere()
{
   location lJumpLoc = GetLocation(oMySpeaker);
   AssignCommand(oMyTarget, ClearAllActions());
   AssignCommand(oMyTarget, ActionJumpToLocation(lJumpLoc));
}

//Added by hahnsoo, jumps a party to the DM
void dmwand_JumpPartyHere()
{
   location lJumpLoc = GetLocation(oMySpeaker);
    object oParty = GetFirstFactionMember(oMyTarget);
    while (GetIsObjectValid(oParty))
    {
        AssignCommand(oParty, ClearAllActions());
        AssignCommand(oParty, ActionJumpToLocation(lJumpLoc));
        oParty = GetNextFactionMember(oMyTarget);
    }
}

void dmwand_JumpToPlayer()
{
   location lJumpLoc = GetLocation(oMyTarget);
   AssignCommand(oMySpeaker, ActionJumpToLocation(lJumpLoc));
}

void dmwand_PlayerListConv(string sParams)
{
   int nPlayer = StringToInt(sParams);
   int nCache;
   int nCount;

   object oPlayer = GetLocalObject(oMySpeaker, "dmw_playercache" + IntToString(nPlayer));
   oMyTarget = oPlayer;
   SetLocalObject(oMySpeaker, "dmfi_univ_target", oMyTarget);

   //Go back to the first conversation level
   dmwand_BuildConversation("Start", "");
}

//::///////////////////////////////////////////////
//:: File: dmw_conv_inc
//::
//:: Conversation functions for the DM's Helper
//:://////////////////////////////////////////////

int dmwand_BuildConversationDialog(int nCurrent, int nChoice, string sConversation, string sParams)
{

   if(TestStringAgainstPattern(sConversation, "ListPlayers"))
   {
      return dmw_conv_ListPlayers(nCurrent, nChoice, sParams);
   }

   if(TestStringAgainstPattern(sConversation, "Start"))
   {
      return dmw_conv_Start(nCurrent, nChoice, sParams);
   }

   return FALSE;
}

void dmwand_BuildConversation(string sConversation, string sParams)
{
   int nLast;
   int nTemp;
   int nChoice = 1;
   int nCurrent = 1;
   int nMatch;

   if(TestStringAgainstPattern(sParams, "prev"))
   {
      //Get the number choice to start with
      nCurrent = GetLocalInt(oMySpeaker, "dmw_dialogprev");

      //Since we're going to the previous page, there will be a next
      SetLocalString(oMySpeaker, "dmw_dialog9", "Next ->");
      SetLocalString(oMySpeaker, "dmw_function9", "conv_" + sConversation);
      SetLocalString(oMySpeaker, "dmw_params9", "next");
      SetLocalInt(oMySpeaker, "dmw_dialognext", nCurrent);

      nChoice = 8;
      for(;nChoice >= 0; nChoice--)
      {
         int nTemp1 = nCurrent;
         int nTemp2 = nCurrent;
         nMatch = nTemp2;
         while((nCurrent == nMatch) && (nTemp2 > 0))
         {
            nTemp2--;
            nMatch = dmwand_BuildConversationDialog(nTemp2, nChoice, sConversation, sParams);
         }

         if(nTemp2 <= 0)
         {
            //we went back too far for some reason, so make this choice blank
            SetLocalString(oMySpeaker, "dmw_dialog" + IntToString(nChoice), "");
            SetLocalString(oMySpeaker, "dmw_function" + IntToString(nChoice), "");
            SetLocalString(oMySpeaker, "dmw_params" + IntToString(nChoice), "");
         }
         nLast = nTemp;
         nTemp = nTemp1;
         nTemp1 = nMatch;
         nCurrent = nMatch;
      }

      if(nMatch > 0)
      {
         SetLocalString(oMySpeaker, "dmw_dialog1", "<- previous");
         SetLocalString(oMySpeaker, "dmw_function1", "conv_" + sConversation);
         SetLocalString(oMySpeaker, "dmw_params1", "prev");
         SetLocalInt(oMySpeaker, "dmw_dialogprev", nLast);
      }

      //fill the NPC's dialog spot
      //(saved for last because the build process tromps on it)
      dmwand_BuildConversationDialog(0, 0, sConversation, sParams);
   }
   else
   {
      //fill the NPC's dialog spot
      dmwand_BuildConversationDialog(0, 0, sConversation, sParams);

      //No parameters specified, start at the top of the conversation
      if(sParams == "")
      {
         nChoice = 1;
         nCurrent = 1;
      }

      //A "next->" choice was selected
      if(TestStringAgainstPattern(sParams, "next"))
      {
         //get the number choice to start with
         nCurrent = GetLocalInt(oMySpeaker, "dmw_dialognext");

         //set this as the number for the "previous" choice to use
         SetLocalInt(oMySpeaker, "dmw_dialogprev", nCurrent);

         //Set the first dialog choice to be "previous"
         nChoice = 2;
         SetLocalString(oMySpeaker, "dmw_dialog1", "<- Previous");
         SetLocalString(oMySpeaker, "dmw_function1", "conv_" + sConversation);
         SetLocalString(oMySpeaker, "dmw_params1", "prev");
      }

      //Loop through to build the dialog list
      for(;nChoice <= 10; nChoice++)
      {
         nMatch = dmwand_BuildConversationDialog(nCurrent, nChoice, sConversation, sParams);
         //nLast will be the value of the choice before the last one
         nLast = nTemp;
         nTemp = nMatch;
         if(nMatch > 0) { nCurrent = nMatch; }
         if(nMatch == 0) { nLast = 0; }
         nCurrent++;
      }

      //If there were enough choices to fill 10 spots, make spot 9 a "next"
      if(nLast > 0)
      {
         SetLocalString(oMySpeaker, "dmw_dialog9", "Next ->");
         SetLocalString(oMySpeaker, "dmw_function9", "conv_" + sConversation);
         SetLocalString(oMySpeaker, "dmw_params9", "next");
         SetLocalInt(oMySpeaker, "dmw_dialognext", nLast);
      }
   }
}

int dmw_conv_ListPlayers(int nCurrent, int nChoice, string sParams = "")
{
   string sText = "";
   string sCall = "";
   string sCallParams = "";
   object oPlayer;
   int nCache;

   if((! TestStringAgainstPattern(sParams, "next")) && (! TestStringAgainstPattern(sParams, "prev")))
   {
      //This is the first time running this function, so cache the objects
      // of all players... we don't want our list swapping itself around every
      // time you change a page
      SetLocalString(oMySpeaker, "dmw_playerfunc", sParams);
      int nCount = 1;
      oPlayer = GetFirstPC();
      while(GetIsObjectValid(oPlayer))
      {
         SetLocalObject(oMySpeaker, "dmw_playercache" + IntToString(nCount), oPlayer);
         oPlayer = GetNextPC();
         nCount++;
      }
      nCount--;
      SetLocalInt(oMySpeaker, "dmw_playercache", nCount);
   }

   string sFunc = GetLocalString(oMySpeaker, "dmw_playerfunc");
   nCache = GetLocalInt(oMySpeaker, "dmw_playercache");

   switch(nCurrent)
   {
      case 0:
         nCurrent = 0;
         sText =       "Who would you like to work on?";
         sCall =       "";
         sCallParams = "";
         break;
      default:
         //Find the next player in the cache who is valid
         oPlayer = GetLocalObject(oMySpeaker, "dmw_playercache" + IntToString(nCurrent));
         while((! GetIsObjectValid(oPlayer)) && (nCurrent <= nCache))
         {
            nCurrent++;
            oPlayer = GetLocalObject(oMySpeaker, "dmw_playercache" + IntToString(nCurrent));
         }

         if(nCurrent > nCache)
         {
            //We've run out of cache, any other spots in this list should be
            //skipped
            nCurrent = 0;
            sText =       "";
            sCall =       "";
            sCallParams = "";
         }
         else
         {
            //We found a player, set up the list entry
            sText =       GetName(oPlayer) + " (" + GetPCPlayerName(oPlayer) + ")";
            sCall =       sFunc;
            sCallParams = IntToString(nCurrent);
         }
         break;
   }

   SetLocalString(oMySpeaker, "dmw_dialog" + IntToString(nChoice), sText);
   SetLocalString(oMySpeaker, "dmw_function" + IntToString(nChoice), sCall);
   SetLocalString(oMySpeaker, "dmw_params" + IntToString(nChoice), sCallParams);

   return nCurrent;
}

void dmwand_DoDialogChoice(int nChoice)
{
   string sCallFunction = GetLocalString(oMySpeaker, "dmw_function" + IntToString(nChoice));
   string sCallParams = GetLocalString(oMySpeaker, "dmw_params" + IntToString(nChoice));
   string sNav = "";

   string sStart = GetStringLeft(sCallFunction, 5);
   int nLen = GetStringLength(sCallFunction) - 5;
   string sCall = GetSubString(sCallFunction, 5, nLen);

   if(TestStringAgainstPattern("conv_", sStart))
   {
      dmwand_BuildConversation(sCall, sCallParams);
   }
   else
   {

      if(TestStringAgainstPattern("PlayerListConv", sCall))
      {
         dmwand_PlayerListConv(sCallParams);
         return;
      }

      if(TestStringAgainstPattern("Toad", sCall))
      {
         DMFI_toad(oMyTarget, oMySpeaker);
         return;
      }
      if(TestStringAgainstPattern("Untoad", sCall))
      {
         DMFI_untoad(oMyTarget, oMySpeaker);
         return;
      }
      if(TestStringAgainstPattern("KickPC", sCall))
      {
         dmwand_KickPC(oMyTarget, oMySpeaker);
         return;
      }

      if(TestStringAgainstPattern("JumpPlayerHere", sCall))
      {
         dmwand_JumpPlayerHere();
         return;
      }
      if(TestStringAgainstPattern("JumpToPlayer", sCall))
      {
         dmwand_JumpToPlayer();
         return;
      }
      if(TestStringAgainstPattern("JumpPartyHere", sCall))
      {
         dmwand_JumpPartyHere();
         return;
      }
   }
}

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


string dmwand_Alignment(object oEntity)
{
   string sReturnString;

   switch (GetAlignmentLawChaos(oEntity))
   {
      case ALIGNMENT_LAWFUL:   sReturnString = "Lawful "; break;
      case ALIGNMENT_NEUTRAL: sReturnString = "Neutral "; break;
      case ALIGNMENT_CHAOTIC:   sReturnString = "Chaotic ";  break;
   }

   switch (GetAlignmentGoodEvil(oEntity))
   {
      case ALIGNMENT_GOOD:   sReturnString = sReturnString + "Good"; break;
      case ALIGNMENT_NEUTRAL: sReturnString = sReturnString +  "Neutral"; break;
      case ALIGNMENT_EVIL:   sReturnString = sReturnString +  "Evil";  break;
   }

   if (sReturnString == "Neutral Neutral"){sReturnString = "True Neutral";}

   return sReturnString;
}

string dmwand_ClassLevel(object oEntity)
{
   string sReturnString;
   string sClass;
   string sClassOne;
   string sClassTwo;
   string sClassThree;
   int nLevelOne;
   int nLevelTwo;
   int nLevelThree;
   int iIndex;

   // Loop through all three classes and pull out info
   for(iIndex == 1;iIndex < 4;iIndex++)
   {
      switch (GetClassByPosition(iIndex,oEntity))
      {
         case CLASS_TYPE_ABERRATION:           sClass ="Aberration";break;
         case CLASS_TYPE_ANIMAL:               sClass ="Animal"; break;
         case CLASS_TYPE_ARCANE_ARCHER:        sClass ="Arcane Archer";break;
         case CLASS_TYPE_ASSASSIN:             sClass ="Assassin"; break;
         case CLASS_TYPE_BARBARIAN:            sClass ="Barbarian";break;
         case CLASS_TYPE_BARD:                 sClass ="Bard"; break;
         case CLASS_TYPE_BEAST:                sClass ="Beast"; break;
         case CLASS_TYPE_BLACKGUARD:           sClass ="Blackguard"; break;
         case CLASS_TYPE_CLERIC:               sClass ="Cleric"; break;
         case CLASS_TYPE_COMMONER:             sClass ="Commoner";break;
         case CLASS_TYPE_CONSTRUCT:            sClass ="Construct"; break;
         case CLASS_TYPE_DIVINECHAMPION:       sClass ="Divine Champion"; break;
         case CLASS_TYPE_DRAGON:               sClass ="Dragon"; break;
         case CLASS_TYPE_DRAGONDISCIPLE:       sClass ="Dragon Disciple"; break;
         case CLASS_TYPE_DRUID:                sClass ="Druid";break;
         case CLASS_TYPE_DWARVENDEFENDER:      sClass ="Dwarven Defender"; break;
         case CLASS_TYPE_ELEMENTAL:            sClass ="Elemental"; break;
         case CLASS_TYPE_FEY:                  sClass ="Fey";break;
         case CLASS_TYPE_FIGHTER:              sClass ="Fighter";  break;
         case CLASS_TYPE_GIANT:                sClass ="Giant";  break;
         case CLASS_TYPE_HARPER:               sClass ="Harper"; break;
         case CLASS_TYPE_HUMANOID:             sClass ="Humanoid"; break;
         case CLASS_TYPE_INVALID:              sClass ="";break;
         case CLASS_TYPE_MAGICAL_BEAST:        sClass ="Magical Beast"; break;
         case CLASS_TYPE_MONK:                 sClass ="Monk";   break;
         case CLASS_TYPE_OUTSIDER:             sClass ="Outsider"; break;
         case CLASS_TYPE_MONSTROUS:            sClass ="Monstrous"; break;
         case CLASS_TYPE_PALADIN:              sClass ="Paladin";break;
         case CLASS_TYPE_PALEMASTER:           sClass ="Palemaster"; break;
         case CLASS_TYPE_PURPLE_DRAGON_KNIGHT: sClass="Purple Dragon Knight"; break;
         case CLASS_TYPE_RANGER:               sClass ="Ranger";break;
         case CLASS_TYPE_ROGUE:                sClass ="Rogue";break;
         case CLASS_TYPE_SHADOWDANCER:         sClass ="Shadowdancer"; break;
         case CLASS_TYPE_SHAPECHANGER:         sClass ="Shapechanger";break;
         case CLASS_TYPE_SHIFTER:              sClass ="Shifter"; break;
         case CLASS_TYPE_SORCERER:             sClass ="Sorcerer";break;
         case CLASS_TYPE_UNDEAD:               sClass ="Undead";break;
         case CLASS_TYPE_VERMIN:               sClass ="Vermin"; break;
         case CLASS_TYPE_WEAPON_MASTER:        sClass ="Weapon Master"; break;
         case CLASS_TYPE_WIZARD:               sClass ="Wizard"; break;
      }

      // Now depending on which iteration we just went through
      // assign it to the proper class
      switch (iIndex)
      {
         case 1: sClassOne =   sClass;  break;
         case 2: sClassTwo =   sClass;  break;
         case 3: sClassThree = sClass;  break;
      }
   };

   // Now get all three class levels.  Wil be 0 if does class pos
   //does not exists
   nLevelOne =   GetLevelByPosition(1,oEntity);
   nLevelTwo =   GetLevelByPosition(2,oEntity);
   nLevelThree = GetLevelByPosition(3,oEntity);

   //Start building return string
   sReturnString = sClassOne + "(" + IntToString(nLevelOne) + ")" ;

   //If second class exists append to return string
   if(nLevelTwo > 0)
   {
      sReturnString =sReturnString + "/" + sClassTwo + "(" + IntToString(nLevelTwo) + ")";
   }

   //If third class exists append to return string
   if(nLevelThree > 0)
   {
      sReturnString =sReturnString + "/" + sClassThree + "(" + IntToString(nLevelThree) + ")";
   }

   return sReturnString;
}

string dmwand_Gender(object oEntity)
{
   switch (GetGender(oEntity))
   {
      case GENDER_MALE:   return "Male"; break;
      case GENDER_FEMALE: return "Female"; break;
      case GENDER_BOTH:   return "Both";  break;
      case GENDER_NONE:   return "None";  break;
      case GENDER_OTHER:  return "Other";  break;
   }

   return "Weirdo";
}

string dmwand_ItemInfo(object oItem, int iInt)
{
   string sReturnString = "";
   string sBaseType = "";
   string sStacked = "";
   string sIdentified = "";
   string sGPValue = "";
   string sACValue = "";
   string sProperties = "";

   switch(GetBaseItemType(oItem))
   {
      case BASE_ITEM_AMULET: sBaseType ="Amulet";break;
      case BASE_ITEM_ARMOR: sBaseType ="Armor";break;
      case BASE_ITEM_ARROW: sBaseType ="Arrow";break;
      case BASE_ITEM_BASTARDSWORD: sBaseType ="Bastard Sword";break;
      case BASE_ITEM_BATTLEAXE: sBaseType ="Battle Axe";break;
      case BASE_ITEM_BELT: sBaseType ="Belt";break;
      case BASE_ITEM_BLANK_POTION : sBaseType ="Blank Potion";break;
      case BASE_ITEM_BLANK_SCROLL : sBaseType ="Blank Scroll";break;
      case BASE_ITEM_BLANK_WAND : sBaseType ="Blank Wand";break;
      case BASE_ITEM_BOLT : sBaseType ="Bolt";break;
      case BASE_ITEM_BOOK: sBaseType ="Book";break;
      case BASE_ITEM_BOOTS: sBaseType ="Boots";break;
      case BASE_ITEM_BRACER: sBaseType ="Bracer";break;
      case BASE_ITEM_BULLET: sBaseType ="Bullet";break;
      case BASE_ITEM_CBLUDGWEAPON: sBaseType ="Bludgeoning Weap.";break;
      case BASE_ITEM_CLOAK: sBaseType ="Cloak";break;
      case BASE_ITEM_CLUB: sBaseType ="Club";break;
      case BASE_ITEM_CPIERCWEAPON: sBaseType ="Pierceing Weap.";break;
      case BASE_ITEM_CREATUREITEM: sBaseType ="Creature Item";break;
      case BASE_ITEM_CSLASHWEAPON: sBaseType ="Slash Weap.";break;
      case BASE_ITEM_CSLSHPRCWEAP: sBaseType ="Slash/Pierce Weap.";break;
      case BASE_ITEM_DAGGER: sBaseType ="Dagger";break;
      case BASE_ITEM_DART: sBaseType ="Dart";break;
      case BASE_ITEM_DIREMACE: sBaseType ="Mace";break;
      case BASE_ITEM_DOUBLEAXE: sBaseType ="Double Axe";break;
      case BASE_ITEM_DWARVENWARAXE : sBaseType ="Dwarven War Axe";break;
      case BASE_ITEM_ENCHANTED_POTION : sBaseType ="Enchanted Potion";break;
      case BASE_ITEM_ENCHANTED_SCROLL : sBaseType ="Enchanted Scroll";break;
      case BASE_ITEM_ENCHANTED_WAND : sBaseType ="Enchanted Wand";break;
      case BASE_ITEM_GEM: sBaseType ="Gem";break;
      case BASE_ITEM_GLOVES: sBaseType ="Gloves";break;
      case BASE_ITEM_GOLD: sBaseType ="Gold";break;
      case BASE_ITEM_GREATAXE: sBaseType ="Great Axe";break;
      case BASE_ITEM_GREATSWORD: sBaseType ="Great Sword";break;
      case BASE_ITEM_GRENADE : sBaseType ="Grenade";break;
      case BASE_ITEM_HALBERD: sBaseType ="Halberd";break;
      case BASE_ITEM_HANDAXE: sBaseType ="Hand Axe";break;
      case BASE_ITEM_HEALERSKIT: sBaseType ="Healers Kit";break;
      case BASE_ITEM_HEAVYCROSSBOW: sBaseType ="Heavy Xbow";break;
      case BASE_ITEM_HEAVYFLAIL: sBaseType ="Heavy Flail";break;
      case BASE_ITEM_HELMET: sBaseType ="Helmet";break;
      case BASE_ITEM_INVALID: sBaseType ="";break;
      case BASE_ITEM_KAMA: sBaseType ="Kama";break;
      case BASE_ITEM_KATANA: sBaseType ="Katana";break;
      case BASE_ITEM_KEY: sBaseType ="Key";break;
      case BASE_ITEM_KUKRI: sBaseType ="Kukri";break;
      case BASE_ITEM_LARGEBOX: sBaseType ="Large Box";break;
      case BASE_ITEM_LARGESHIELD: sBaseType ="Large Shield";break;
      case BASE_ITEM_LIGHTCROSSBOW: sBaseType ="Light Xbow";break;
      case BASE_ITEM_LIGHTFLAIL: sBaseType ="Light Flail";break;
      case BASE_ITEM_LIGHTHAMMER: sBaseType ="Light Hammer";break;
      case BASE_ITEM_LIGHTMACE: sBaseType ="Light Mace";break;
      case BASE_ITEM_LONGBOW: sBaseType ="Long Bow";break;
      case BASE_ITEM_LONGSWORD: sBaseType ="Long Sword";break;
      case BASE_ITEM_MAGICROD: sBaseType ="Magic Rod";break;
      case BASE_ITEM_MAGICSTAFF: sBaseType ="Magic Staff";break;
      case BASE_ITEM_MAGICWAND: sBaseType ="Magic Wand";break;
      case BASE_ITEM_MISCLARGE: sBaseType ="Misc. Large";break;
      case BASE_ITEM_MISCMEDIUM: sBaseType ="Misc. Medium";break;
      case BASE_ITEM_MISCSMALL: sBaseType ="Misc. Small";break;
      case BASE_ITEM_MISCTALL: sBaseType ="Misc. Small";break;
      case BASE_ITEM_MISCTHIN: sBaseType ="Misc. Thin";break;
      case BASE_ITEM_MISCWIDE: sBaseType ="Misc. Wide";break;
      case BASE_ITEM_MORNINGSTAR: sBaseType ="Morningstar";break;
      case BASE_ITEM_POTIONS: sBaseType ="Potion";break;
      case BASE_ITEM_QUARTERSTAFF: sBaseType ="Quarterstaff";break;
      case BASE_ITEM_RAPIER: sBaseType ="Rapier";break;
      case BASE_ITEM_RING: sBaseType ="Ring";break;
      case BASE_ITEM_SCIMITAR: sBaseType ="Scimitar";break;
      case BASE_ITEM_SCROLL: sBaseType ="Scroll";break;
      case BASE_ITEM_SCYTHE: sBaseType ="Scythe";break;
      case BASE_ITEM_SHORTBOW: sBaseType ="Shortbow";break;
      case BASE_ITEM_SHORTSPEAR: sBaseType ="Short Spear";break;
      case BASE_ITEM_SHORTSWORD: sBaseType ="Short Sword";break;
      case BASE_ITEM_SHURIKEN: sBaseType ="Shuriken";break;
      case BASE_ITEM_SICKLE: sBaseType ="Sickle";break;
      case BASE_ITEM_SLING: sBaseType ="Sling";break;
      case BASE_ITEM_SMALLSHIELD: sBaseType ="Small Shield";break;
      case BASE_ITEM_SPELLSCROLL: sBaseType ="Spell Scroll";break;
      case BASE_ITEM_THIEVESTOOLS: sBaseType ="Thieves Tools";break;
      case BASE_ITEM_THROWINGAXE: sBaseType ="Throwing Axe";break;
      case BASE_ITEM_TORCH: sBaseType ="Torch";break;
      case BASE_ITEM_TOWERSHIELD: sBaseType ="Tower Shield";break;
      case BASE_ITEM_TRAPKIT: sBaseType ="Trap Kit";break;
      case BASE_ITEM_TRIDENT: sBaseType ="Trident";break;
      case BASE_ITEM_TWOBLADEDSWORD: sBaseType ="2 Bladed Sword";break;
      case BASE_ITEM_WARHAMMER: sBaseType ="Warhammer";break;
      case BASE_ITEM_WHIP : sBaseType ="Whip";break;
  }

   sReturnString = sStacked + GetName(oItem) + " (" + sBaseType + ")";
   return sReturnString;
}

string dmwand_Inventory(object oEntity)
{

   string sBaseType;
   string sReturnString;

   sReturnString = sReturnString + "\nEquipped:\n";
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_ARMS, oMyTarget))){ sReturnString = sReturnString + "Arms: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_ARMS, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_BELT, oMyTarget))){ sReturnString = sReturnString + "Belt: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_BELT, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_BOOTS, oMyTarget))){ sReturnString = sReturnString + "Boots: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_BOOTS, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_CHEST, oMyTarget))){ sReturnString = sReturnString + "Chest: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_CHEST, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_CLOAK, oMyTarget))){ sReturnString = sReturnString + "Cloak: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_CLOAK, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_HEAD, oMyTarget))){ sReturnString = sReturnString + "Head: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_HEAD, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_LEFTHAND, oMyTarget))){ sReturnString = sReturnString + "Left Hand: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_LEFTHAND, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_LEFTRING, oMyTarget))){ sReturnString = sReturnString + "Left Ring: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_LEFTRING, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_NECK, oMyTarget))){ sReturnString = sReturnString + "Neck: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_NECK, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_RIGHTHAND, oMyTarget))){ sReturnString = sReturnString + "Right Hand: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_RIGHTHAND, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_RIGHTRING, oMyTarget))){ sReturnString = sReturnString + "Right Ring: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_RIGHTRING, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_ARROWS, oMyTarget))){ sReturnString = sReturnString + "Arrows: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_ARROWS, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_BOLTS, oMyTarget))){ sReturnString = sReturnString + "Bolts: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_BOLTS, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_BULLETS, oMyTarget))){ sReturnString = sReturnString + "Bullets: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_BULLETS, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_CARMOUR, oMyTarget))){ sReturnString = sReturnString + "Creature Armor: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_CARMOUR, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_CWEAPON_B, oMyTarget))){ sReturnString = sReturnString + "Creature Bite: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_CWEAPON_B, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_CWEAPON_L, oMyTarget))){ sReturnString = sReturnString + "Creature Left: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_CWEAPON_L, oMyTarget),0) + "\n"; }
   if(GetIsObjectValid(GetItemInSlot(INVENTORY_SLOT_CWEAPON_R, oMyTarget))){ sReturnString = sReturnString + "Creature Right: " + dmwand_ItemInfo(GetItemInSlot(INVENTORY_SLOT_CWEAPON_R, oMyTarget),0) + "\n"; }

   object oItem = GetFirstItemInInventory(oEntity);

   while(oItem != OBJECT_INVALID)
   {
      sReturnString = sReturnString + "\n" + dmwand_ItemInfo(oItem, 0);
      oItem = GetNextItemInInventory(oEntity);
   };

   return sReturnString;
}

string dmwand_Race(object oEntity)
{
   switch (GetRacialType(oEntity))
   {
      case RACIAL_TYPE_ABERRATION: return "Aberration"; break;
      case RACIAL_TYPE_ALL:   return "All"; break;
      case RACIAL_TYPE_ANIMAL:   return "Animal"; break;
      case RACIAL_TYPE_BEAST:   return "Beast"; break;
      case RACIAL_TYPE_CONSTRUCT:   return "Construct"; break;
      case RACIAL_TYPE_DRAGON:   return "Dragon"; break;
      case RACIAL_TYPE_DWARF:   return "Dwarf"; break;
      case RACIAL_TYPE_ELEMENTAL:   return "Elemental"; break;
      case RACIAL_TYPE_ELF:   return "Elf"; break;
      case RACIAL_TYPE_FEY:   return "Fey"; break;
      case RACIAL_TYPE_GIANT:   return "Giant"; break;
      case RACIAL_TYPE_GNOME:   return "Gnome"; break;
      case RACIAL_TYPE_HALFELF:   return "Half Elf"; break;
      case RACIAL_TYPE_HALFLING:   return "Halfling"; break;
      case RACIAL_TYPE_HALFORC:   return "Half Orc"; break;
      case RACIAL_TYPE_HUMAN:   return "Human"; break;
      case RACIAL_TYPE_HUMANOID_GOBLINOID:   return "Goblinoid"; break;
      case RACIAL_TYPE_HUMANOID_MONSTROUS:   return "Monstrous"; break;
      case RACIAL_TYPE_HUMANOID_ORC:   return "Orc"; break;
      case RACIAL_TYPE_HUMANOID_REPTILIAN:   return "Reptillian"; break;
      case RACIAL_TYPE_MAGICAL_BEAST:   return "Magical Beast"; break;
      case RACIAL_TYPE_OOZE: return "Ooze"; break;
      case RACIAL_TYPE_OUTSIDER:   return "Outsider"; break;
      case RACIAL_TYPE_SHAPECHANGER:   return "Shapechanger"; break;
      case RACIAL_TYPE_UNDEAD:   return "Undead"; break;
      case RACIAL_TYPE_VERMIN:   return "Vermin"; break;
   }

   return "Unknown";
}
int DMFI_GetNetWorth(object oTarget)
{
    int n;
    object oItem = GetFirstItemInInventory(oTarget);
    while(GetIsObjectValid(oItem))
    {
        n= n + GetGoldPieceValue(oItem);
        oItem = GetNextItemInInventory(oTarget);
    }


         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_ARMS, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_ARROWS, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_BELT, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_BOLTS, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_BOOTS, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_BULLETS, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_CARMOUR, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_CHEST, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_CLOAK, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_CWEAPON_B, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_CWEAPON_L, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_CWEAPON_R, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_HEAD, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_LEFTHAND, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_LEFTRING, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_NECK, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_RIGHTHAND, oTarget));
         n = n + GetGoldPieceValue(GetItemInSlot(INVENTORY_SLOT_RIGHTRING, oTarget));
    return n;
}

void DMFI_report(object oTarget, object oUser)
{
   string sSTR = IntToString(GetAbilityScore(oMyTarget,ABILITY_STRENGTH));
   string sINT = IntToString(GetAbilityScore(oMyTarget,ABILITY_INTELLIGENCE));
   string sDEX = IntToString(GetAbilityScore(oMyTarget,ABILITY_DEXTERITY));
   string sWIS = IntToString(GetAbilityScore(oMyTarget,ABILITY_WISDOM));
   string sCON = IntToString(GetAbilityScore(oMyTarget,ABILITY_CONSTITUTION));
   string sCHA = IntToString(GetAbilityScore(oMyTarget,ABILITY_CHARISMA));
   string sReport = "\n-------------------------------------------" +
                    "\nReported: " + IntToString(GetTimeHour()) + ":" + IntToString(GetTimeMinute()) +
                    "\nPlayer Name: " + GetPCPlayerName(oMyTarget) +
                    "\nPublic CDKey: " + GetPCPublicCDKey(oMyTarget) +
                    "\nChar Name:   " + GetName(oMyTarget) +
                    "\n-------------------------------------------" +
                    "\nRace:    " + dmwand_Race(oMyTarget) +
                    "\nClass:    " + dmwand_ClassLevel(oMyTarget) +
                    "\nXP:     " + IntToString(GetXP(oMyTarget)) +
                    "\nGender: " + dmwand_Gender(oMyTarget) +
                    "\nAlign:    " + dmwand_Alignment(oMyTarget) +
                    "\nDeity:  " + GetDeity(oMyTarget) +
                    "\n" +
                    "\nSTR:  " + sSTR +
                    "\nINT:   " + sINT +
                    "\nWIS:  " + sWIS +
                    "\nDEX:  " + sDEX +
                    "\nCON: " + sCON +
                    "\nCHA:  " + sCHA +
                    "\n" +
                    "\nHP:  " + IntToString(GetCurrentHitPoints(oMyTarget)) +
                    " of " + IntToString(GetMaxHitPoints(oMyTarget)) +
                    "\nAC:  " + IntToString(GetAC(oMyTarget)) +
                    "\nGold:  " + IntToString(GetGold(oMyTarget)) +
                    "\nNet Worth:  " + IntToString(DMFI_GetNetWorth(oMyTarget) + GetGold(oMyTarget)) +
                    "\nInventory:\n  " + dmwand_Inventory(oMyTarget) +
                    "\n-------------------------------------------";

    SendMessageToPC(oUser, sReport);
    SendMessageToAllDMs(sReport);
}
