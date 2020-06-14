//::///////////////////////////////////////////////
//:: DMFI - Universal Wand executable
//:: dmfi_execute
//:://////////////////////////////////////////////
/*
    Processing for all DMFI wands & widgets universal conversation choices.

    Credits:
        Bioware - Dicebag
        Arawen - Skill Check Wand (implemented with the Dicebag)
        Jhenne (tallonzek@hotmail.com)   \ Authors of the original FX Wand,
        Doppleganger                     / DM Wand and Emote Wand
        Demetrious - XP wand
        Dezran (dezran@roguepenguin.com) - Rod of Affliction
        Lurker - Music Wand
        Oddbod - FX wand improvements
        Ty Worsham (volition) - Sound Creator Beta
        OldManWhistler - NPC corpse functions

        hahnsoo (hahns_shin@hotmail.com) - Final Improved FX wand, Universal wand scripts,
                                           Encounter wand, DM Voice scripts, Faction wand,
                                           Spirelands Resting system
        J.R.R.Tolkien - References to the One Ring.
*/
//:://////////////////////////////////////////////
//:: Created By: The DMFI Team
//:: Created On:
//:://////////////////////////////////////////////
//:: 2007.04.12 hahnsoo and Demetrious - version 1.08a
//:: 2007.12.12 Merle - fixes to DMFI rest system
//:: 2008.05.25 tsunami282 - updated for NWN 1.69 (DMFI OnPlayerChat event handling)
//:: 2008.05.26 tsunami282 - XP wand: grant percent XP based on each party member's level, not selected party member

#include "dmfi_db_inc"
#include "dmfi_dmw_inc"
#include "x2_inc_toollib"
#include "dmfi_plychat_inc"
#include "dmfi_plchlishk_i"
#include "dmfi_getln_inc"

int iNightMusic;
int iDayMusic;
int iBattleMusic;

////////////////////////////////////////////////////////////////////////
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

////////////////////////////////////////////////////////////////////////
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

////////////////////////////////////////////////////////////////////////
//DMFI Processes the dice rolls
void RollDemBones(object oUser, int iBroadcast, int iMod = 0, string sAbility = "", int iNum = 1, int iSide = 20)
{

    string sString = "";
    int iRoll = 0;
    int iTotal = 0;
    //Build the string
    sString = sAbility+"Roll " + IntToString(iNum) + "d" + IntToString(iSide) + ": ";
    while (iNum > 1)
    {
        iRoll = Random(iSide) + 1;
        iTotal = iTotal + iRoll;
        sString = sString + IntToString(iRoll) + " + ";
        iNum--;
    }
    iRoll = Random(iSide) + 1;
    iTotal = iTotal + iRoll;
    sString = sString + IntToString(iRoll);
    if (iMod)
    {
        iTotal = iTotal + iMod;
        sString = sString + " + Modifier: " + IntToString(iMod);
    }
    sString = sString + " = Total: " + IntToString(iTotal);

    //Perform appropriate animation
    if (GetLocalInt(oUser, "dmfi_dice_no_animate")!=1)
    {
        switch (GetLocalInt(oUser, "dmfi_univ_int"))
        {
        case 71: AssignCommand(oUser, PlayAnimation(ANIMATION_LOOPING_TALK_PLEADING, 1.0, 5.0f)); break;
        case 72: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_PAUSE_SCRATCH_HEAD, 1.0)); break;
        case 73: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_TAUNT, 1.0)); break;
        case 74: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_LEFT, 1.0)); break;
        case 78: AssignCommand(oUser, PlayAnimation(ANIMATION_LOOPING_GET_LOW, 1.0, 5.0f)); break;
        case 81: AssignCommand(oUser, PlayAnimation(ANIMATION_LOOPING_CONJURE1, 1.0, 5.0f)); break;
        case 82: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE, 1.0)); break;
        case 83: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_TAUNT, 1.0)); break;
        case 84: AssignCommand(oUser, PlayAnimation(ANIMATION_LOOPING_LISTEN, 1.0, 5.0f)); break;
        case 85: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_PAUSE_SCRATCH_HEAD, 1.0)); break;
        case 89: AssignCommand(oUser, ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BARD_SONG), oUser, 6.0f)); break;
        case 91: AssignCommand(oUser, PlayAnimation(ANIMATION_LOOPING_TALK_PLEADING, 1.0, 5.0f)); break;
        case 95: AssignCommand(oUser, PlayAnimation(ANIMATION_LOOPING_CONJURE2, 1.0, 5.0f)); break;
        case 97: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_TAUNT, 1.0)); break;
        case 98: AssignCommand(oUser, PlayAnimation(ANIMATION_FIREFORGET_DODGE_DUCK, 1.0)); break;
        default: AssignCommand(oUser, PlayAnimation (ANIMATION_LOOPING_GET_MID, 1.0, 3.0)); break;
        }
    }

    sString = ColorText(sString, "cyan");
    //--------------------------------------------------------
    switch (iBroadcast)
    {
    case 3: break;                             //dm only
    case 1: AssignCommand(oUser, SpeakString(sString , TALKVOLUME_SHOUT)); break;
    case 2: AssignCommand(oUser, SpeakString(sString)); break;
    default: if (GetIsPC(oUser)) SendMessageToPC(oUser, sString);break;
    }
    //--------------------------------------------------------
    AssignCommand(oUser, SpeakString( sString, TALKVOLUME_SILENT_SHOUT));
    return;
}

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI PC Dicebag
void DoDiceBagFunction(int iDice, object oUser, int iDMOverride = 0)
{
    string sAbility = "";
    int iTrain =0;
    int iNum = 0;
    int iSide = 0;
    int iMod = 0;
    int iLeft;
    if (iDice < 100)
        iLeft = StringToInt(GetStringLeft(IntToString(iDice), 1));
    else
        iLeft = 10;
    int iRight = StringToInt(GetStringRight(IntToString(iDice), 1));
    switch (iDice)
    {
    case 61: iNum = 1; iSide = 20; sAbility="Strength Check, "; iMod = GetAbilityModifier(ABILITY_STRENGTH, oUser); break;
    case 62: iNum = 1; iSide = 20; sAbility="Dexterity Check, "; iMod = GetAbilityModifier(ABILITY_DEXTERITY, oUser); break;
    case 63: iNum = 1; iSide = 20; sAbility="Constitution Check, "; iMod = GetAbilityModifier(ABILITY_CONSTITUTION, oUser); break;
    case 64: iNum = 1; iSide = 20; sAbility="Intelligence Check, "; iMod = GetAbilityModifier(ABILITY_INTELLIGENCE, oUser); break;
    case 65: iNum = 1; iSide = 20; sAbility="Wisdom Check, "; iMod = GetAbilityModifier(ABILITY_WISDOM, oUser); break;
    case 66: iNum = 1; iSide = 20; sAbility="Charisma Check, "; iMod = GetAbilityModifier(ABILITY_CHARISMA, oUser); break;
    case 67: iNum = 1; iSide = 20; sAbility="Fortitude Save, "; iMod = GetFortitudeSavingThrow(oUser); break;
    case 68: iNum = 1; iSide = 20; sAbility="Reflex Save, "; iMod = GetReflexSavingThrow(oUser); break;
    case 69: iNum = 1; iSide = 20; sAbility="Will Save, "; iMod = GetWillSavingThrow(oUser); break;

    case 71: iNum = 1; iSide = 20; iTrain = 1; sAbility="Animal Empathy Check, "; iMod = GetSkillRank(SKILL_ANIMAL_EMPATHY, oUser); break;
    case 72: iNum = 1; iSide = 20; sAbility="Appraise Check, "; iMod = GetSkillRank(SKILL_APPRAISE, oUser); break;
    case 73: iNum = 1; iSide = 20; sAbility="Bluff Check, "; iMod = GetSkillRank(SKILL_BLUFF, oUser); break;
    case 74: iNum = 1; iSide = 20; sAbility="Concentration Check, "; iMod = GetSkillRank(SKILL_CONCENTRATION, oUser); break;
    case 75: iNum = 1; iSide = 20; sAbility="Craft Armor Check, "; iMod = GetSkillRank(SKILL_CRAFT_ARMOR, oUser); break;
    case 76: iNum = 1; iSide = 20; sAbility="Craft Trap Check, "; iMod = GetSkillRank(SKILL_CRAFT_TRAP, oUser); break;
    case 77: iNum = 1; iSide = 20; sAbility="Craft Weapon Check, "; iMod = GetSkillRank(SKILL_CRAFT_WEAPON, oUser); break;
    case 78: iNum = 1; iSide = 20; iTrain = 1; sAbility="Disable Trap Check, "; iMod = GetSkillRank(SKILL_DISABLE_TRAP, oUser); break;
    case 79: iNum = 1; iSide = 20; sAbility="Discipline Check, "; iMod = GetSkillRank(SKILL_DISCIPLINE, oUser); break;

    case 81: iNum = 1; iSide = 20; sAbility="Heal Check, "; iMod = GetSkillRank(SKILL_HEAL, oUser); break;
    case 82: iNum = 1; iSide = 20; sAbility="Hide Check, "; iMod = GetSkillRank(SKILL_HIDE, oUser); break;
    case 83: iNum = 1; iSide = 20; sAbility="Intimidate Check, "; iMod = GetSkillRank(SKILL_INTIMIDATE, oUser); break;
    case 84: iNum = 1; iSide = 20; sAbility="Listen Check, "; iMod = GetSkillRank(SKILL_LISTEN, oUser); break;
    case 85: iNum = 1; iSide = 20; sAbility="Lore Check, "; iMod = GetSkillRank(SKILL_LORE, oUser); break;
    case 86: iNum = 1; iSide = 20; sAbility="Move Silently Check, "; iMod = GetSkillRank(SKILL_MOVE_SILENTLY, oUser); break;
    case 87: iNum = 1; iSide = 20; iTrain = 1; sAbility="Open Lock Check, "; iMod = GetSkillRank(SKILL_OPEN_LOCK, oUser); break;
    case 88: iNum = 1; iSide = 20; sAbility="Parry Check, "; iMod = GetSkillRank(SKILL_PARRY, oUser); break;
    case 89: iNum = 1; iSide = 20; sAbility="Perform Check, "; iMod = GetSkillRank(SKILL_PERFORM, oUser); break;
    // 2008.07.30 morderon - added ride check
    case 90: iNum = 1; iSide = 20; sAbility="Ride, "; iMod = GetSkillRank(SKILL_RIDE, oUser); break;
    case 91: iNum = 1; iSide = 20; sAbility="Persuade Check, "; iMod = GetSkillRank(SKILL_PERSUADE, oUser); break;
    case 92: iNum = 1; iSide = 20; iTrain = 1; sAbility="Pick Pocket Check, "; iMod = GetSkillRank(SKILL_PICK_POCKET, oUser); break;
    case 93: iNum = 1; iSide = 20; sAbility="Search Check, "; iMod = GetSkillRank(SKILL_SEARCH, oUser); break;
    case 94: iNum = 1; iSide = 20; iTrain = 1; sAbility="Set Trap Check, "; iMod = GetSkillRank(SKILL_SET_TRAP, oUser); break;
    case 95: iNum = 1; iSide = 20; iTrain = 1; sAbility="Spellcraft Check, "; iMod = GetSkillRank(SKILL_SPELLCRAFT, oUser); break;
    case 96: iNum = 1; iSide = 20; sAbility="Spot Check, "; iMod = GetSkillRank(SKILL_SPOT, oUser); break;
    case 97: iNum = 1; iSide = 20; sAbility="Taunt Check, "; iMod = GetSkillRank(SKILL_TAUNT, oUser); break;
    case 98: iNum = 1; iSide = 20; iTrain = 1; sAbility="Tumble Check, "; iMod = GetSkillRank(SKILL_TUMBLE, oUser); break;
    case 99: iNum = 1; iSide = 20; iTrain = 1; sAbility="Use Magic Device Check, "; iMod = GetSkillRank(SKILL_USE_MAGIC_DEVICE, oUser); break;

    case 101: SetLocalInt(oUser, "dmfi_dicebag", 2); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 2, oUser); SetCustomToken(20681, "Local"); FloatingTextStringOnCreature("Broadcast Mode set to Local", oUser, FALSE); return; break;
    case 102: SetLocalInt(oUser, "dmfi_dicebag", 1); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 1, oUser); SetCustomToken(20681, "Global"); FloatingTextStringOnCreature("Broadcast Mode set to Global", oUser, FALSE); return; break;
    case 103: SetLocalInt(oUser, "dmfi_dicebag", 0); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 0, oUser); SetCustomToken(20681, "Private"); FloatingTextStringOnCreature("Broadcast Mode set to Private", oUser, FALSE); return; break;
    default: iNum = iRight;

        switch (iLeft)
        {
        case 1: iSide = 4; break;
        case 2: iSide = 6; break;
        case 3: iSide = 8; break;
        case 4: iSide = 10; break;
        case 5: iSide = 20; break;
        }
        break;
    }
    if ((iTrain)&&(iMod==0))
    {
        string sMsg = ColorText("No dice roll:  Skill requires training", "red");
        SendMessageToPC(oUser, sMsg);
        AssignCommand(oUser, SpeakString( sMsg, TALKVOLUME_SILENT_SHOUT));
        return;
    }


    int iTell = GetLocalInt(oUser, "dmfi_dicebag");

    if (iDMOverride)
        iTell = iDMOverride;

    RollDemBones(oUser, iTell, iMod, sAbility, iNum, iSide);
}

////////////////////////////////////////////////////////////////////////
//By OldManWhistler for the DMFI Control Wand
void DestroyAllItems()
{
    if (GetIsDead(OBJECT_SELF))
    {
        object oItem = GetFirstItemInInventory();
        while (GetIsObjectValid(oItem))
        {
            DestroyObject(oItem);
            oItem = GetNextItemInInventory();
        }
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_ARMS)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_ARROWS)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_BELT)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_BOLTS)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_BOOTS)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_BULLETS)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_CARMOUR)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_CHEST)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_CLOAK)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_CWEAPON_B)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_CWEAPON_L)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_CWEAPON_R)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_HEAD)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_LEFTHAND)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_LEFTRING)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_NECK)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_RIGHTHAND)))
            DestroyObject(oItem);
        if (GetIsObjectValid(oItem=GetItemInSlot(INVENTORY_SLOT_RIGHTRING)))
            DestroyObject(oItem);
    }
}

////////////////////////////////////////////////////////////////////////
// Function to destroy a target, by OldManWhistler, for the DMFI Control Wand
void DestroyCreature(object oTarget)
{
    AssignCommand(oTarget,SetIsDestroyable(TRUE,FALSE,FALSE));
    DestroyObject(oTarget);
}

////////////////////////////////////////////////////////////////////////
//DMFI NPC Control Wand
void DoControlFunction(int iFaction, object oUser)
{
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    object oArea = GetArea(oUser);
    object oChange;
    float fAlignShift;
    int nAlignShift;
    int nReport;
    int nMessage;

    object oAlignTarget = GetNearestObject(OBJECT_TYPE_CREATURE, oUser);

    fAlignShift = GetLocalFloat(oUser, "dmfi_reputation");

    if (fAlignShift == 0.0f)
        fAlignShift = 10.0f;


    nAlignShift = FloatToInt(fAlignShift);

    switch (iFaction)
    {
    case 10: //Toggle the state of all the encounters in the area
        if (GetLocalInt(oArea, "dmfi_encounter_inactive"))
        {
            oChange = GetFirstObjectInArea(oArea);
            while (GetIsObjectValid(oChange))
            {
                if (GetObjectType(oChange) == OBJECT_TYPE_ENCOUNTER)
                    SetEncounterActive(TRUE, oChange);
                oChange = GetNextObjectInArea(oArea);
            }
            FloatingTextStringOnCreature("Bioware encounters are active",oUser, FALSE);
            SetLocalInt(oArea, "dmfi_encounter_inactive", FALSE);
        }
        else
        {
            oChange = GetFirstObjectInArea(oArea);
            while (GetIsObjectValid(oChange))
            {
                if (GetObjectType(oChange) == OBJECT_TYPE_ENCOUNTER)
                    SetEncounterActive(FALSE, oChange);
                if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE)
                {
                    if (GetIsEncounterCreature(oChange))
                        DestroyObject(oChange); //Nuke any encounter creatures in the area
                }
                oChange = GetNextObjectInArea(oArea);
            }
            FloatingTextStringOnCreature("Bioware encounters deactivated",oUser, FALSE);
            SetLocalInt(oArea, "dmfi_encounter_inactive", TRUE);
        }
        break;
    case 11: ChangeToStandardFaction(oTarget, STANDARD_FACTION_HOSTILE);  break;
    case 12: ChangeToStandardFaction(oTarget, STANDARD_FACTION_COMMONER); break;
    case 13: ChangeToStandardFaction(oTarget, STANDARD_FACTION_DEFENDER); break;
    case 14: ChangeToStandardFaction(oTarget, STANDARD_FACTION_MERCHANT); break;
    case 15: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
                ChangeToStandardFaction(oChange, STANDARD_FACTION_HOSTILE);
            oChange = GetNextObjectInArea(oArea);
        }break;
    case 16: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
                ChangeToStandardFaction(oChange, STANDARD_FACTION_COMMONER);
            oChange = GetNextObjectInArea(oArea);
        }break;
    case 17: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
                ChangeToStandardFaction(oChange, STANDARD_FACTION_DEFENDER);
            oChange = GetNextObjectInArea(oArea);
        }break;
    case 18: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
                ChangeToStandardFaction(oChange, STANDARD_FACTION_MERCHANT);
            oChange = GetNextObjectInArea(oArea);
        }break;
    case 21: oChange = GetFirstPC();
        while (GetIsObjectValid(oChange))
        {
            SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 0, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 100, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 100, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 100, oChange);
            oChange = GetNextPC();
        }break;
    case 22: oChange = GetFirstPC();
        while (GetIsObjectValid(oChange))
        {
            SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 20, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 91, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 100, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 50, oChange);
            oChange = GetNextPC();
        }break;
    case 23: oChange = GetFirstPC();
        while (GetIsObjectValid(oChange))
        {
            SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 0 , oChange);
            SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 0, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 0, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 0, oChange);
            oChange = GetNextPC();
        }break;
    case 24: oChange = GetFirstPC();
        while (GetIsObjectValid(oChange))
        {
            SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 100, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 100, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 100, oChange);
            SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 100, oChange);
            oChange = GetNextPC();
        }break;
    case 25: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE)
            {
                SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 0, oChange);
                SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 0, oChange);
                SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 0, oChange);
                SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 0, oChange);
            }
            oChange = GetNextObjectInArea(oArea);
        }break;
    case 26: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE)
            {
                AssignCommand(oChange, ClearAllActions(TRUE));
                SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 50, oChange);
                SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 50, oChange);
                SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 50, oChange);
                SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 50, oChange);
            }
            oChange = GetNextObjectInArea(oArea);
        }break;
    case 31: SetLocalObject(oUser, "dmfi_customfaction1", oTarget); nMessage = -1; break;
    case 32: SetLocalObject(oUser, "dmfi_customfaction2", oTarget); nMessage = -1;break;
    case 33: SetLocalObject(oUser, "dmfi_customfaction3", oTarget); nMessage = -1;break;
    case 34: SetLocalObject(oUser, "dmfi_customfaction4", oTarget); nMessage = -1;break;
    case 35: SetLocalObject(oUser, "dmfi_customfaction5", oTarget); nMessage = -1;break;
    case 36: SetLocalObject(oUser, "dmfi_customfaction6", oTarget); nMessage = -1;break;
    case 37: SetLocalObject(oUser, "dmfi_customfaction7", oTarget); nMessage = -1;break;
    case 38: SetLocalObject(oUser, "dmfi_customfaction8", oTarget); nMessage = -1;break;
    case 39: SetLocalObject(oUser, "dmfi_customfaction9", oTarget); nMessage = -1;break;
    case 41: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction1")); nMessage = -1;break;
    case 42: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction2")); nMessage = -1;break;
    case 43: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction3")); nMessage = -1;break;
    case 44: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction4")); nMessage = -1;break;
    case 45: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction5")); nMessage = -1;break;
    case 46: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction6")); nMessage = -1;break;
    case 47: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction7")); nMessage = -1;break;
    case 48: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction8")); nMessage = -1;break;
    case 49: ChangeFaction(oTarget, GetLocalObject(oUser, "dmfi_customfaction9")); nMessage = -1;break;
    case 51: RemoveHenchman(GetMaster(oTarget), oTarget);
        SetLocalObject(oUser, "dmfi_henchman", oTarget); nMessage = -1;break;
    case 52: RemoveHenchman(oTarget, GetAssociate(ASSOCIATE_TYPE_HENCHMAN, oTarget));
        AddHenchman(oTarget, GetLocalObject(oUser, "dmfi_henchman")); nMessage = -1;break;
    case 61: AssignCommand(oTarget, ClearAllActions()); AssignCommand(oTarget, ActionMoveAwayFromObject(oUser, TRUE)); nMessage = -1;break;
    case 62: AssignCommand(oTarget, ClearAllActions()); AssignCommand(oTarget, ActionForceMoveToObject(oUser, TRUE, 2.0f, 30.0f)); nMessage = -1;break;
    case 63: AssignCommand(oTarget, ClearAllActions()); AssignCommand(oTarget, ActionRandomWalk());nMessage = -1; break;
    case 64: AssignCommand(oTarget, ClearAllActions()); AssignCommand(oTarget, ActionRest());nMessage = -1; break;
    case 65: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
            {
                AssignCommand(oChange, ClearAllActions()); AssignCommand(oChange, ActionMoveAwayFromObject(oUser, TRUE));
            }
            oChange = GetNextObjectInArea(oArea);
        }nMessage = -1; break;
    case 66: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
            {
                AssignCommand(oChange, ClearAllActions()); AssignCommand(oChange, ActionForceMoveToObject(oUser, TRUE, 2.0f, 30.0f));
            }
            oChange = GetNextObjectInArea(oArea);
        }nMessage = -1; break;
    case 67: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
            {
                AssignCommand(oChange, ClearAllActions()); AssignCommand(oChange, ActionRandomWalk());
            }
            oChange = GetNextObjectInArea(oArea);
        }nMessage = -1; break;
    case 68: oChange = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oChange))
        {
            if (GetObjectType(oChange) == OBJECT_TYPE_CREATURE && !GetIsPC(oChange))
            {
                AssignCommand(oChange, ClearAllActions()); AssignCommand(oChange, ActionRest());
            }
            oChange = GetNextObjectInArea(oArea);
        } nMessage = -1;break;
    case 69: ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDisappear(), oTarget);
        DestroyObject(oTarget, 1.0); nMessage = -1;break;
    case 70: DestroyCreature(oTarget); nMessage = -1;break;
    case 71: AssignCommand(oTarget, SetIsDestroyable(FALSE, TRUE, TRUE)); nMessage = -1;break;
    case 72: AssignCommand(oTarget, SetIsDestroyable(FALSE, FALSE, TRUE)); nMessage = -1;break;
    case 73: AssignCommand(oTarget, SetIsDestroyable(FALSE, FALSE, FALSE));nMessage = -1; break;
    case 74: AssignCommand(oTarget, SetIsDestroyable(TRUE, FALSE, FALSE));nMessage = -1; break;
    case 75: AssignCommand(oTarget, SetIsDestroyable(FALSE, TRUE, TRUE));
        DelayCommand(0.1, AssignCommand(oTarget, ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectDeath(), oTarget))); nMessage = -1;break;
    case 76: AssignCommand(oTarget, SetIsDestroyable(FALSE, FALSE, TRUE));
        DelayCommand(0.1, AssignCommand(oTarget, ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectDeath(), oTarget))); nMessage = -1;break;
    case 77: AssignCommand(oTarget, SetIsDestroyable(FALSE, FALSE, FALSE));
        DelayCommand(0.1, AssignCommand(oTarget, ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectDeath(), oTarget))); nMessage = -1;break;
    case 78: AssignCommand(oTarget, SetIsDestroyable(TRUE, FALSE, FALSE));
        DelayCommand(0.1, AssignCommand(oTarget, ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectDeath(), oTarget)));nMessage = -1; break;
    case 79: AssignCommand(oTarget, DestroyAllItems());
        DelayCommand(1.0, DestroyCreature(oTarget));nMessage = -1;break;
    case 81:  //AdjustReputation(oAlignTarget, oTarget, nAlignShift);
        AdjustReputation(oTarget, oAlignTarget, nAlignShift);
        nReport = GetReputation(oAlignTarget, oTarget);
        FloatingTextStringOnCreature("Current Reputation: "+ GetName(oTarget) + " vs. " +GetName(oAlignTarget)+": " + IntToString(nReport), oUser);
        nReport = GetReputation(oTarget, oAlignTarget);
        FloatingTextStringOnCreature("Current Reputation: "+ GetName(oAlignTarget) + " vs. " +GetName(oTarget)+": " + IntToString(nReport), oUser);
        break;
    case 82:  //AdjustReputation(oAlignTarget, oTarget, -nAlignShift);
        AdjustReputation(oTarget, oAlignTarget, -nAlignShift);
        nReport = GetReputation(oAlignTarget, oTarget);
        FloatingTextStringOnCreature("Current Reputation: "+ GetName(oTarget) + " vs. " +GetName(oAlignTarget)+": " + IntToString(nReport), oUser);
        nReport = GetReputation(oTarget, oAlignTarget);
        FloatingTextStringOnCreature("Current Reputation: "+ GetName(oAlignTarget) + " vs. " +GetName(oTarget)+": " + IntToString(nReport), oUser);
        break;
    case 83:  SetLocalString(oUser, "EffectSetting", "dmfi_reputation");
        CreateSetting(oUser);nMessage = -1; break;
    case 84:  nReport = GetReputation(oAlignTarget, oTarget);
        FloatingTextStringOnCreature("Current Reputation: "+ GetName(oTarget) + " vs. " +GetName(oAlignTarget)+": " + IntToString(nReport), oUser);
        nReport = GetReputation(oTarget, oAlignTarget);
        FloatingTextStringOnCreature("Current Reputation: "+ GetName(oAlignTarget) + " vs. " +GetName(oTarget)+": " + IntToString(nReport), oUser);
        nMessage = -1;break;
    case 9:  {
            if (GetLocalInt(GetModule(), "dmfi_safe_factions")!=1)
            {
                SetLocalInt(GetModule(), "dmfi_safe_factions", 1);
                SetDMFIPersistentInt("dmfi", "dmfi_safe_factions", 1, oUser);
                FloatingTextStringOnCreature("Default non-hostile faction should ignore PC attacks",oUser, FALSE);
            }
            else
            {
                SetLocalInt(GetModule(), "dmfi_safe_factions", 0);
                SetDMFIPersistentInt("dmfi", "dmfi_safe_factions", 0, oUser);
                FloatingTextStringOnCreature("Bioware faction behavior restored",oUser, FALSE);
            }
        }

    default: nMessage = -1;break;

    }

    if (nMessage!=-1)
    {
        if (GetIsImmune(oTarget, IMMUNITY_TYPE_BLINDNESS))
            FloatingTextStringOnCreature("Targeted creature is blind immune - no attack will occur until new perception event is fired", oUser);
        else
        {
            effect eInvis =EffectBlindness();
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eInvis, oTarget, 6.1);
            FloatingTextStringOnCreature("Faction Adjusted - Perception event will fire in 6 seconds", oUser);
        }
    }

}

////////////////////////////////////////////////////////////////////////
void IdenStuff(object oTarget)
{
    object oItem = GetFirstItemInInventory(oTarget);
    while (GetIsObjectValid(oItem))
    {
        if (GetIdentified(oItem)==FALSE)
            SetIdentified(oItem, TRUE);

        oItem = GetNextItemInInventory(oTarget);
    }
}

////////////////////////////////////////////////////////////////////////
void TakeStuff(int Level, object oTarget, object oUser)
{
    object oItem = GetFirstItemInInventory(oTarget);
    while (GetIsObjectValid(oItem))
    {
        DestroyObject(oItem);
        oItem = GetNextItemInInventory(oTarget);
    }

    if (Level == 1)
    {
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_ARMS,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_ARROWS,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_BELT,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_BOLTS,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_BOOTS,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_BULLETS,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_CARMOUR,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_CHEST,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_CLOAK,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_CWEAPON_B,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_CWEAPON_L,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_CWEAPON_R,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_HEAD,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_LEFTHAND,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_LEFTRING,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_NECK,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_RIGHTHAND,oTarget));
        DestroyObject(GetItemInSlot(INVENTORY_SLOT_RIGHTRING,oTarget));
    }
    FloatingTextStringOnCreature("DM Intervention:  Inventory Destroyed by DM", oTarget);
}

////////////////////////////////////////////////////////////////////////
void TakeUber(object oTarget)
{
    int nMultiplier;
    if (GetHitDice(oTarget)<11)
        nMultiplier = 1;
    else if (GetHitDice(oTarget)<16)
        nMultiplier = 2;
    else if (GetHitDice(oTarget)<20)
        nMultiplier = 3;
    else
        nMultiplier = 5;
    object oItem = GetFirstItemInInventory(oTarget);
    while (GetIsObjectValid(oItem))
    {
        if (GetGoldPieceValue(oItem)>1000*nMultiplier*GetHitDice(oTarget))
            DestroyObject(oItem);
        oItem = GetNextItemInInventory(oTarget);
    }
    FloatingTextStringOnCreature("DM Intervention:  Uber type items have been removed", oTarget);
}

////////////////////////////////////////////////////////////////////////
void RotateMe(object oTarget, int Amount, object oUser)
{
    location lLocation = GetLocation (oTarget);
    if (GetObjectType(oTarget) != OBJECT_TYPE_PLACEABLE)
    {
        oTarget = GetNearestObject(OBJECT_TYPE_PLACEABLE, oUser);
        FloatingTextStringOnCreature("Target was not a placable, used placeable closest to your avitar", oUser);
    }
    if (Amount == -2)
    {
        AssignCommand(oTarget, SetFacing(90.0));
        return;
    }
    if (Amount == -1)
    {
        AssignCommand(oTarget, SetFacing(0.0));
        return;
    }
    if (GetIsObjectValid(oTarget))
        AssignCommand(oTarget, SetFacing(GetFacing(oTarget)+Amount));
}

////////////////////////////////////////////////////////////////////////
void DMFI_Object (object oTarget, int Action, object oUser)
{
    location lLocation = GetLocation (oTarget);
    if (GetObjectType(oTarget) != OBJECT_TYPE_PLACEABLE)
    {
        oTarget = GetNearestObject(OBJECT_TYPE_PLACEABLE, oUser);
        FloatingTextStringOnCreature("Target was not a placable, used placeable closest to your avitar", oUser);
    }
    if (GetIsObjectValid(oTarget))
    {
        if (Action==1)
        {
            DestroyObject(oTarget);
            DelayCommand(2.0, FloatingTextStringOnCreature(GetName(oTarget) + "destroyed.  If 'static', you must leave and return to see effect.", oUser));
        }
        else if (Action ==2)
        {
            AssignCommand(oTarget, PlayAnimation(ANIMATION_PLACEABLE_DEACTIVATE));
            DelayCommand(0.4,SetPlaceableIllumination(oTarget, FALSE));
            DelayCommand(0.5,RecomputeStaticLighting(GetArea(oTarget)));
        }
        else if (Action ==3)
        {
            AssignCommand(oTarget, PlayAnimation(ANIMATION_PLACEABLE_ACTIVATE));
            DelayCommand(0.4,SetPlaceableIllumination(oTarget, TRUE));
            DelayCommand(0.5,RecomputeStaticLighting(GetArea(oTarget)));
        }
    }
}

////////////////////////////////////////////////////////////////////////
void dmwand_SwapDayNight(int nDay)
{
    int nCurrentHour;
    int nCurrentMinute = GetTimeMinute();
    int nCurrentSecond = GetTimeSecond();
    int nCurrentMilli = GetTimeMillisecond();

    nCurrentHour = ((nDay == 1)?7:19);

    SetTime(nCurrentHour, nCurrentMinute, nCurrentSecond, nCurrentMilli);
}

////////////////////////////////////////////////////////////////////////
void dmwand_AdvanceTime(int nHours)
{
    int nCurrentHour = GetTimeHour();
    int nCurrentMinute = GetTimeMinute();
    int nCurrentSecond = GetTimeSecond();
    int nCurrentMilli = GetTimeMillisecond();

    nCurrentHour += nHours;
    SetTime(nCurrentHour, nCurrentMinute, nCurrentSecond, nCurrentMilli);
}

////////////////////////////////////////////////////////////////////////
void DMFI_Align(object oUser, object oTarget, int nAlign, int nParty)
{
    if (GetObjectType(oTarget)== OBJECT_TYPE_CREATURE)
    {
        int nAmount = GetLocalInt(oUser, "dmfi_alignshift");

        if (nParty)
        {
            object oParty = GetFirstFactionMember(oTarget, TRUE);
            while (GetIsObjectValid(oParty))
            {
                AdjustAlignment(oParty, nAlign, nAmount);
                oParty = GetNextFactionMember(oTarget, TRUE);
            }
            FloatingTextStringOnCreature("Party Alignment shifted by " + IntToString(nAmount), oUser);
        }
        else
        {
            AdjustAlignment(oTarget, nAlign, nAmount);
            FloatingTextStringOnCreature("Target Alignment shifted by " + IntToString(nAmount), oUser);
        }
    }
    else
        FloatingTextStringOnCreature("Must target a creature for this action", oUser);

}

////////////////////////////////////////////////////////////////////////
void DMFI_Roll(object oUser)
{
    object oStoreState = GetItemPossessedBy(oUser, "dmfi_dmw");
    int n = GetLocalInt(oUser, "dmfi_alignshift");
    if (n == 1)
        n = 2;
    else if (n ==2)
        n = 5;
    else if (n ==5)
        n = 10;
    else if (n == 10)
        n = 1;
    FloatingTextStringOnCreature("Adjustment changed to " + IntToString(n), oUser);
    SetLocalInt(oUser, "dmfi_alignshift", n);
    SetCustomToken(20781, IntToString(n));
    SetDMFIPersistentInt("dmfi", "dmfi_alignshift", n, oUser);
}


////////////////////////////////////////////////////////////////////////
int GetAreaXAxis(object oArea)
{

    location locTile;
    int iX = 0;
    int iY = 0;
    vector vTile = Vector(0.0, 0.0, 0.0);

    for (iX = 0; iX < 32; ++iX)
    {
        vTile.x = IntToFloat(iX);
        locTile = Location(oArea, vTile, 0.0);
        int iRes = GetTileMainLight1Color(locTile);
        if (iRes > 32 || iRes < 0)
            return(iX);
    }

    return 32;
}

////////////////////////////////////////////////////////////////////////
int GetAreaYAxis(object oArea)
{
    location locTile;
    int iX = 0;
    int iY = 0;
    vector  vTile = Vector(0.0, 0.0, 0.0);

    for (iY = 0; iY < 32; ++iY)
    {
        vTile.y = IntToFloat(iY);
        locTile = Location(oArea, vTile, 0.0);
        int iRes = GetTileMainLight1Color(locTile);
        if (iRes > 32 || iRes < 0)
            return(iY);
    }

    return 32;
}

////////////////////////////////////////////////////////////////////////
void TilesetMagic(object oUser, int nEffect, int nType)
{
    int iXAxis = GetAreaXAxis(GetArea(oUser));
    int iYAxis = GetAreaYAxis(GetArea(oUser));
    int nBase = GetLocalInt(GetModule(), "dmfi_tileset");

// nType definitions:
// 0 fill
// 1 flood
// 2 groundcover

// nBase definitions:
// 0 default
// 1 Sewer and City - raise the fill effect to -0.1


    float ZEffectAdjust = 0.0;
    float ZTypeAdjust = 0.1; //default is groundcover
    float ZTileAdjust = 0.0;
    float ZFinalAxis;

/*
if (nEffect == X2_TL_GROUNDTILE_ICE)
    ZEffectAdjust = -1.0;  // lower the effect based on trial and error
*/
    if (nEffect == X2_TL_GROUNDTILE_SEWER_WATER)
        ZEffectAdjust = 0.8;

//now sep based on nType
    if (nType == 0)  //fill
        ZTypeAdjust=-2.0;
    else if (nType ==1)
        ZTypeAdjust = 2.0;

    ZFinalAxis = ZEffectAdjust + ZTypeAdjust + ZTileAdjust;

//special case for filling of water and sewer regions
    if ((nBase==1) && (nType==0))
        ZFinalAxis = -0.1;

    TLResetAreaGroundTiles(GetArea(oUser), iXAxis, iYAxis);
    TLChangeAreaGroundTiles(GetArea(oUser), nEffect , iXAxis, iYAxis, ZFinalAxis);
}

////////////////////////////////////////////////////////////////////////
//New DM Wand by Demetrious
void DoNewDMThingy(int iChoice, object oUser)
{
    location lLocation = GetLocalLocation(oUser, "dmfi_univ_location");
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    int iXAxis = GetAreaXAxis(GetArea(oUser));
    int iYAxis = GetAreaYAxis(GetArea(oUser));
    object oCopy; object oParty;
    int n; string sName;

    switch (iChoice)
    {
    case 11: TakeStuff(1, oTarget, oUser); break;
    case 12: TakeStuff(0, oTarget, oUser); break;
    case 13: IdenStuff(oTarget); break;
    case 14: TakeUber(oTarget); break;
    case 15: DMFI_NextTarget(oTarget, oUser);break;
    case 20: DMFI_NextTarget(oTarget, oUser);break;
    case 21: DMFI_Align(oUser, oTarget, ALIGNMENT_GOOD, 0);break;
    case 22: DMFI_Align(oUser, oTarget, ALIGNMENT_EVIL, 0);break;
    case 23: DMFI_Align(oUser, oTarget, ALIGNMENT_LAWFUL, 0);break;
    case 24: DMFI_Align(oUser, oTarget, ALIGNMENT_CHAOTIC, 0);break;
    case 25: DMFI_Align(oUser, oTarget, ALIGNMENT_GOOD, 1);break;
    case 26: DMFI_Align(oUser, oTarget, ALIGNMENT_EVIL, 1);break;
    case 27: DMFI_Align(oUser, oTarget, ALIGNMENT_LAWFUL, 1);break;
    case 28: DMFI_Align(oUser, oTarget, ALIGNMENT_CHAOTIC, 1);break;
    case 29: DMFI_Roll(oUser); break;
    case 31:   SendMessageToPC(oUser, "Item name: "+GetName(oTarget));
        SendMessageToPC(oUser, "Item value: "+IntToString(GetGoldPieceValue(oTarget)));
        if (GetDroppableFlag(oTarget)) SendMessageToPC(oUser, "Droppable");
        else SendMessageToPC(oUser, "Not droppable");
        if (GetItemCursedFlag(oTarget)) SendMessageToPC(oUser, "Cursed");
        else SendMessageToPC(oUser, "Not cursed");
        if (GetPlotFlag(oTarget)) SendMessageToPC(oUser, "Plot related");
        else SendMessageToPC(oUser, "Not plot related");
        if (GetStolenFlag(oTarget)) SendMessageToPC(oUser, "Stolen");
        else SendMessageToPC(oUser, "Not stolen");
        SendMessageToPC(oUser, "Charges remaining: " + IntToString(GetItemCharges(oTarget)));
        break;

    case 32: if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            SetPlotFlag(oTarget, FALSE); DestroyObject(oTarget);
            FloatingTextStringOnCreature(GetName(oTarget)+": Item destroyed", oUser);
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;
    case 33: if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            SetItemCharges(oTarget, 0);
            FloatingTextStringOnCreature( GetName(oTarget)+": Remaining charges removed", oUser);
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;


    case 34: if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            SetItemCharges(oTarget, 999);
            FloatingTextStringOnCreature( GetName(oTarget)+": Item fully recharged",oUser); break;
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;

    case 35: if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            if (GetDroppableFlag(oTarget))
            {
                SetDroppableFlag(oTarget, FALSE);
                FloatingTextStringOnCreature(GetName(oTarget)+": can NOT be dropped", oUser);
            }
            else
            {
                SetDroppableFlag(oTarget, TRUE);
                FloatingTextStringOnCreature( GetName(oTarget)+": can be dropped", oUser);
            }
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;

    case 36:   if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            if (GetItemCursedFlag(oTarget))
            {
                SetItemCursedFlag(oTarget, FALSE);
                FloatingTextStringOnCreature(GetName(oTarget)+": NOT cursed", oUser);
            }
            else
            {
                SetItemCursedFlag(oTarget, TRUE);
                FloatingTextStringOnCreature( GetName(oTarget)+": set to CURSED", oUser);
            }
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;

    case 37:  if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            if (GetPlotFlag(oTarget))
            {
                SetPlotFlag(oTarget, FALSE);
                FloatingTextStringOnCreature(GetName(oTarget)+": NOT plot related", oUser);
            }
            else
            {
                SetPlotFlag(oTarget, TRUE);
                FloatingTextStringOnCreature( GetName(oTarget)+": set to PLOT", oUser);
            }
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;
    case 38:   if (GetObjectType(oTarget)==OBJECT_TYPE_ITEM)
        {
            if (GetStolenFlag(oTarget))
            {
                SetStolenFlag(oTarget, FALSE);
                FloatingTextStringOnCreature(GetName(oTarget)+": NOT stolen", oUser);
            }
            else
            {
                SetStolenFlag(oTarget, TRUE);
                FloatingTextStringOnCreature( GetName(oTarget)+": set to Stolen", oUser);
            }
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target. Target item directly from inventory screen", oUser);
        }
        break;


    case 41: DMFI_Object(oTarget, 1, oUser); break;
    case 42: DMFI_Object(oTarget, 2, oUser);break;
    case 43: DMFI_Object(oTarget, 3, oUser); break;
    case 45: RotateMe(oTarget, -2, oUser);break;
    case 46: RotateMe(oTarget, -1, oUser);break;
    case 47: RotateMe(oTarget, 30, oUser);break;
    case 48: RotateMe(oTarget, 45, oUser);break;
    case 49: RotateMe(oTarget, 90, oUser);break;
    case 40: RotateMe(oTarget, 180, oUser);break;
    case 51: dmwand_AdvanceTime(1);break;
    case 52: dmwand_AdvanceTime(4);break;
    case 53: dmwand_AdvanceTime(8);break;
    case 54: dmwand_AdvanceTime(24);break;
    case 55: dmwand_SwapDayNight(0);break;
    case 50: dmwand_SwapDayNight(1);break;
    case 56: SetWeather(GetArea(oUser), WEATHER_CLEAR); break;
    case 57: SetWeather(GetArea(oUser), WEATHER_RAIN); break;
    case 58: SetWeather(GetArea(oUser), WEATHER_SNOW); break;
    case 59: SetWeather(GetArea(oUser), WEATHER_USE_AREA_SETTINGS); break;
    case 60: DMFI_report(oTarget, oUser); break;
    case 61: DMFI_toad(oTarget, oUser); break;
    case 62: DMFI_jail(oTarget, oUser); break;
    case 63: AssignCommand(oUser, AddToParty( oUser, GetFactionLeader(oTarget)));break;
    case 64: RemoveFromParty(oUser);break;
    case 65: ExploreAreaForPlayer(GetArea(oTarget), oTarget); FloatingTextStringOnCreature("Map Given: Target", oUser);break;
    case 66: {
            FloatingTextStringOnCreature("Map Given: Party", oUser);
            object oParty = GetFirstFactionMember(oTarget,TRUE);
            while (GetIsObjectValid(oParty))
            {
                ExploreAreaForPlayer(GetArea(oTarget), oTarget);
                oParty = GetNextFactionMember(oTarget,TRUE);
            }
            break;
        }
    case 67: ExportAllCharacters();break;
    case 68: dmwand_KickPC(oTarget, oUser);break;
    case 69: sName = GetModuleName();
        StartNewModule(sName);break;
    case 71: TilesetMagic(oUser, X2_TL_GROUNDTILE_WATER, 0);break;
    case 72: TilesetMagic(oUser, X2_TL_GROUNDTILE_ICE, 0);break;
    case 73: TilesetMagic(oUser, X2_TL_GROUNDTILE_LAVA, 0) ;break;
    case 74: TilesetMagic(oUser, X2_TL_GROUNDTILE_SEWER_WATER, 0);break;
    case 75: TilesetMagic(oUser, X2_TL_GROUNDTILE_WATER, 1);break;
    case 76: TilesetMagic(oUser, X2_TL_GROUNDTILE_ICE, 1);break;
    case 77: TilesetMagic(oUser, X2_TL_GROUNDTILE_LAVA, 1) ;break;
    case 78: TilesetMagic(oUser, X2_TL_GROUNDTILE_SEWER_WATER, 1);break;
    case 79: TLResetAreaGroundTiles(GetArea(oUser), iXAxis, iYAxis); break;
    case 81: TilesetMagic(oUser, X2_TL_GROUNDTILE_ICE, 2);break;
    case 82: TilesetMagic(oUser, X2_TL_GROUNDTILE_GRASS, 2);break;
    case 83: TilesetMagic(oUser, X2_TL_GROUNDTILE_CAVEFLOOR, 2) ;break;
    case 89: TLResetAreaGroundTiles(GetArea(oUser), iXAxis, iYAxis); break;
    case 91: StoreCampaignObject("dmfi", "dmfi_copyplayer1", oTarget);
        FloatingTextStringOnCreature("Target stored", oUser);break;
    case 92:   oParty = GetFirstFactionMember(oTarget, TRUE);
        n=1;
        while (GetIsObjectValid(oParty))
        {
            StoreCampaignObject("dmfi", "dmfi_copyplayer"+IntToString(n), oParty);
            SendMessageToPC(oUser, GetName(oParty) + " stored");
            n=n+1;
            oParty = GetNextFactionMember(oTarget, TRUE);
        }
        FloatingTextStringOnCreature("Party stored", oUser);
        break;

    case 93:n=1;
        oCopy = RetrieveCampaignObject("dmfi", "dmfi_copyplayer"+IntToString(n), lLocation);
        while (GetIsObjectValid(oCopy))
        {
            ChangeToStandardFaction(oCopy, STANDARD_FACTION_COMMONER);

            n=n+1;
            oCopy = RetrieveCampaignObject("dmfi", "dmfi_copyplayer"+IntToString(n), lLocation);
            AssignCommand(oCopy, SetIsDestroyable(FALSE, TRUE, TRUE));
        }
        break;
    case 101: SetLocalInt(GetModule(), "dmfi_tileset" , 0);   break;
    case 102: SetLocalInt(GetModule(), "dmfi_tileset" , 1);  break; //sewer/city

    default: break;
    }

}

////////////////////////////////////////////////////////////////////////
//This is for the DMFI Dicebag Wand
void DoDMDiceBagFunction(int iDice, object oUser)
{
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    if (!GetIsObjectValid(oTarget))
        oTarget = oUser;
    int iOverride = GetLocalInt(oUser, "dmfi_dicebag");
    object oArea = GetArea(oUser);
    object oRoll;
    int iLeft;
    if (iDice < 100)
        iLeft = StringToInt(GetStringLeft(IntToString(iDice), 1));
    else
        iLeft = 10;
    switch (iLeft)
    {
    case 1:
    case 2:
    case 3:
    case 4: //Single Creature Roll
        DoDiceBagFunction(iDice+50, oTarget, iOverride); break;
    case 5:
    case 6:
    case 7:
    case 8://All PCs/NPCs in the area
        oRoll = GetFirstObjectInArea(oArea);
        while (GetIsObjectValid(oRoll))
        {
            if ((GetIsPC(oTarget) && GetIsPC(oRoll)) || (!GetIsPC(oTarget) && !GetIsPC(oRoll) && GetObjectType(oRoll) == OBJECT_TYPE_CREATURE))
                DoDiceBagFunction(iDice+10, oRoll, iOverride);
            oRoll = GetNextObjectInArea(oArea);
        }
        break;
    case 10: {
            switch (iDice)
            {
            case 101: SetLocalInt(oUser, "dmfi_dicebag", 2); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 2, oUser); SetCustomToken(20681, "Local"); FloatingTextStringOnCreature("Broadcast Mode set to Local", oUser, FALSE); return; break;
            case 102: SetLocalInt(oUser, "dmfi_dicebag", 1); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 1, oUser); SetCustomToken(20681, "Global"); FloatingTextStringOnCreature("Broadcast Mode set to Global", oUser, FALSE); return; break;
            case 103: SetLocalInt(oUser, "dmfi_dicebag", 0); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 0, oUser); SetCustomToken(20681, "Private"); FloatingTextStringOnCreature("Broadcast Mode set to Private", oUser, FALSE); return; break;
            case 104: SetLocalInt(oUser, "dmfi_dicebag", 3); SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 3, oUser); SetCustomToken(20681, "DM Only"); FloatingTextStringOnCreature("Broadcast Mode set to DM Only", oUser, FALSE); return; break;
            case 105: DMFI_NextTarget(oTarget, oUser);break;
            case 106: {
                    if (GetLocalInt(oUser, "dmfi_dice_no_animate")==1)
                    {
                        SetLocalInt(oUser, "dmfi_dice_no_animate", 0);
                        FloatingTextStringOnCreature("Rolls will show animation", oUser);
                    }
                    else
                    {
                        SetLocalInt(oUser, "dmfi_dice_no_animate", 1);
                        FloatingTextStringOnCreature("Rolls will NOT show animation", oUser);
                    }
                }
            }
        }
    default: break;

/*
Demetrious - Saving code for all pcs in case I find a way to put it back into the dicebag.

              //All PCs
              oRoll = GetFirstPC();
              while (GetIsObjectValid(oRoll))
              {
                DoDiceBagFunction(iDice, oRoll, iOverride);
                oRoll = GetNextPC();
              }break;
*/


    }
}

////////////////////////////////////////////////////////////////////////
void DoOneRingFunction(int iRing, object oUser)
{
    switch (iRing)
    {
    case 1: SetLocalString(oUser, "dmfi_univ_conv", "afflict"); break;
    case 2: SetLocalString(oUser, "dmfi_univ_conv", "faction"); break;
    case 3: SetLocalString(oUser, "dmfi_univ_conv", "dicebag"); break;
    case 4: SetLocalString(oUser, "dmfi_univ_conv", "dmw"); break;
    case 5: SetLocalString(oUser, "dmfi_univ_conv", "emote"); break;
    case 6: SetLocalString(oUser, "dmfi_univ_conv", "encounter"); break;
    case 7: SetLocalString(oUser, "dmfi_univ_conv", "fx"); break;
    case 8: SetLocalString(oUser, "dmfi_univ_conv", "music"); break;
    case 91: SetLocalString(oUser, "dmfi_univ_conv", "sound"); break;
    case 92: SetLocalString(oUser, "dmfi_univ_conv", "voice"); break;
    case 93: SetLocalString(oUser, "dmfi_univ_conv", "xp"); break;
    case 94: SetLocalString(oUser, "dmfi_univ_conv", "buff");break;
    default: SetLocalString(oUser, "dmfi_univ_conv", "dmw"); break;
    }
    AssignCommand(oUser, ClearAllActions());
    AssignCommand(oUser, ActionStartConversation(OBJECT_SELF, "dmfi_universal", TRUE));
}

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI Sound FX Wand
void DoSoundFunction(int iSound, object oUser)
{

    location lLocation = GetLocalLocation(oUser, "dmfi_univ_location");
    float fDuration;
    float fDelay;
    object oTarget;

    if (GetIsDMPossessed(oUser))
    {
        fDuration = GetLocalFloat(GetMaster(oUser), "dmfi_effectduration");
        fDelay = GetLocalFloat(GetMaster(oUser), "dmfi_sound_delay");
    }
    else
    {
        fDuration = GetLocalFloat(oUser, "dmfi_effectduration");
        fDelay = GetLocalFloat(oUser, "dmfi_sound_delay");
    }

    switch (iSound)
    {
    case 11: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_batsflap1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 12: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_bugsscary1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 13: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_crptvoice1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 14: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_orcgrunt1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 15: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_minepick2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 16: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_ratssqeak1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 17: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_rockfallg1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 18: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_rockfalgl2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 19: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_wt_gustcavrn1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 21: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_belltower3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 22: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_claybreak3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 23: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_glasbreak2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 24: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_gongring3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 25: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_marketgrp4"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 26: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_cv_millwheel1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 27: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_sawing1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 28: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_cv_bellwind1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 29: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_cv_smithhamr2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 31: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_firelarge1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 32: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_lavapillr1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 33: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_lavafire1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 34: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_firelarge2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 35: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_surf2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 36: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_drips1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 37: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_waterlap1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 38: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_stream4"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 39: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_na_waterfall2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 41: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_crynight3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 42: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_bushmove1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 43: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_birdsflap2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 44: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_grassmove3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 45: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_hawk1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 46: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_na_leafmove3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 47: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_gulls2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 48: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_songbirds1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 49: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_an_toads1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 51: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_mg_beaker1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 52: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_mg_cauldron1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 53: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_mg_chntmagic1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 54: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_mg_crystalev1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 55: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_mg_crystalic1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 56: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("al_mg_portal1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 57: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_mg_telepin1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 58: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_mg_telepout1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 59: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_mg_frstmagic1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 61: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_tavclap1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 62: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_battlegrp7"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 63: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_laughincf2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 64: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_comtntgrp3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 65: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_chantingm2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 66: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_cryingf2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 67: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_laughingf3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 68: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_chantingf2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 69: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_wailingm6"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 71: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_evilchantm"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 72: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_crows2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 73: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_wailingcf1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 74: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_crptvoice2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 75: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_lafspook2"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 76: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_owlhoot1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 77: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_an_wolfhowl1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 78: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_screamf3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 79: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_pl_zombiem3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 81: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_wt_gustsoft1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 82: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_wt_thundercl3"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 83: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_wt_thunderds4"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;
    case 84: oTarget = CreateObject(OBJECT_TYPE_PLACEABLE, "plc_invisobj", lLocation); DelayCommand(fDelay, AssignCommand(oTarget, PlaySound("as_wt_gusforst1"))); DelayCommand(20.0f, DestroyObject(oTarget)); break;

        //Settings
    case 91:
        SetLocalString(oUser, "EffectSetting", "dmfi_effectduration");
        CreateSetting(oUser);
        break;
    case 92:
        SetLocalString(oUser, "EffectSetting", "dmfi_sound_delay");
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
    return;
}

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI DM Voice
void DoVoiceFunction(int iSay, object oUser)
{
    object oMod = GetModule();
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    location lLocation = GetLocalLocation(oUser, "dmfi_univ_location");
    object oVoice;
    string sSay;

    // Invalid target code - Loiter mode
    if (!GetIsObjectValid(oTarget))
    {
        switch (iSay)
        {
        case 8:
            // // XXXX DM Spy Functionality - Currently BROKEN
            // SetDMFIPersistentInt("dmfi", "dmfi_DMSpy", abs(GetDMFIPersistentInt("dmfi", "dmfi_DMSpy", oUser) - 1), oUser);
            // if (GetDMFIPersistentInt("dmfi", "dmfi_DMSpy", oUser) == 1)
            //     FloatingTextStringOnCreature("DM Spy is on.", oUser, FALSE);
            // else
            //     FloatingTextStringOnCreature("DM Spy is off.", oUser, FALSE);
            // break;

            // v1.09 - eavesdrop at location
            {
                int hooknum = GetLocalInt(oUser, "dmfi_MyListenerHook");
                if (hooknum != 0) RemoveListenerHook(hooknum);
                int hookparty = GetLocalInt(oUser, "dmfi_MyListenerPartyMode");
                int hookbcast = GetLocalInt(oUser, "dmfi_MyListenerBcastMode");
                hooknum = AppendListenerHook(2, OBJECT_INVALID, lLocation,
                        DMFI_CHANNELMASK_TALK|DMFI_CHANNELMASK_WHISPER,
                        hookparty, hookbcast, oUser);
                if (hooknum != 0)
                {
                    // move ditto voice to this location (destroying any existing one)
                    if (GetIsObjectValid(GetLocalObject(oUser, "dmfi_MyVoice")))
                    {
                        DestroyObject(GetLocalObject(oUser, "dmfi_MyVoice"));
                        DeleteLocalObject(oUser, "dmfi_MyVoice");
                        FloatingTextStringOnCreature("You have destroyed your previous Voice", oUser, FALSE);
                    }
                    oVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", lLocation);
                    //Sets the Voice as the object to throw to.
                    SetLocalObject(oUser, "dmfi_VoiceTarget", oVoice);
                    //Set Ownership of the Voice to the User
                    SetLocalObject(oUser, "dmfi_MyVoice", oVoice);
                    DelayCommand(1.0f, FloatingTextStringOnCreature("The Voice is operational", oUser, FALSE));
                }
                else
                {
                    SendMessageToPC(oUser, "ERROR: could not append listener hook!");
                }
                SetLocalInt(oUser, "dmfi_MyListenerHook", hooknum);
            }
            break;

        // case 9: //Destroy any existing Voice attached to the user
        //     if (GetIsObjectValid(GetLocalObject(oUser, "dmfi_MyVoice")))
        //     {
        //         DestroyObject(GetLocalObject(oUser, "dmfi_MyVoice"));
        //         DeleteLocalObject(oUser, "dmfi_MyVoice");
        //         FloatingTextStringOnCreature("You have destroyed your previous Voice", oUser, FALSE);
        //     }
        //     //Create the Voice
        //     oVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", lLocation);
        //     //Sets the Voice as the object to throw to.
        //     SetLocalObject(oUser, "dmfi_VoiceTarget", oVoice);
        //     //Set Ownership of the Voice to the User
        //     SetLocalObject(oUser, "dmfi_MyVoice", oVoice);
        //     DelayCommand(1.0f, FloatingTextStringOnCreature("The Voice is operational", oUser, FALSE));
        //     break;

        case 9:
            // v1.09 - Toggle location range eavesdropping
            {
                int partylisten = GetLocalInt(oUser, "dmfi_MyListenerPartyMode");
                partylisten++;
                if (partylisten > 2) partylisten = 0;
                SetLocalInt(oUser, "dmfi_MyListenerPartyMode", partylisten);
                string sRange;
                if (partylisten == 0) sRange = "EARSHOT";
                else if (partylisten == 1) sRange = "AREA";
                else sRange = "MODULE";
                DelayCommand(1.0f, FloatingTextStringOnCreature("Location eavesdrop mode for new eavesdroppers set to " + sRange, oUser, FALSE));
            }
            break;

        // Create a Loiter Voice
        default:
            oVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", lLocation);
            SetLocalInt(oVoice, "dmfi_Loiter", 1);
            SetLocalString(oVoice, "dmfi_LoiterSay", GetDMFIPersistentString("dmfi", "hls206" + IntToString(iSay)));
            break;
        }
    }

    // You targetted yourself = Record Mode
    else if (oTarget == oUser)
    {
        switch (iSay)
        {
        // Toggle the mute / unmute NPC function
        case 8: SetDMFIPersistentInt("dmfi", "dmfi_AllMute", abs(GetDMFIPersistentInt("dmfi", "dmfi_AllMute") - 1));
            if (GetDMFIPersistentInt("dmfi", "dmfi_AllMute") == 1)
                FloatingTextStringOnCreature("All NPC conversations are muted", oUser, FALSE);
            else
                FloatingTextStringOnCreature("All NPC conversations are unmuted", oUser, FALSE);
            break;

        //     // XXXX Create a Ditto Voice - Duplicate functionality
        // case 9: //Destroy any existing Voice attached to the user
        //     if (GetIsObjectValid(GetLocalObject(oUser, "dmfi_MyVoice")))
        //     {
        //         DestroyObject(GetLocalObject(oUser, "dmfi_MyVoice"));
        //         DeleteLocalObject(oUser, "dmfi_MyVoice");
        //         FloatingTextStringOnCreature("You have destroyed your previous Voice", oUser, FALSE);
        //     }
        //     //Create the Voice
        //     oVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", lLocation);
        //
        //     SetLocalObject(oUser, "dmfi_VoiceTarget", oVoice);
        //     //Set Ownership of the Voice to the User
        //     SetLocalObject(oUser, "dmfi_MyVoice", oVoice);
        //     DelayCommand(1.0f, FloatingTextStringOnCreature("The Voice is operational", oUser, FALSE));
        //     break;

        case 9:
            {
                // v1.09 - toggle eavesdrop bcast - user/alldms
                int hookbcast = GetLocalInt(oUser, "dmfi_MyListenerBcastMode");
                hookbcast = !hookbcast;
                SetLocalInt(oUser, "dmfi_MyListenerBcastMode", hookbcast);
                DelayCommand(1.0f, FloatingTextStringOnCreature("DM-Broadcast mode for new eavesdroppers set to " + (hookbcast ? "ON" : "OFF"), oUser, FALSE));
            }

        case 10:
            // v1.09 - cancel eavesdrop mode
            {
                int hooknum = GetLocalInt(oUser, "dmfi_MyListenerHook");
                if (hooknum != 0)
                {
                    RemoveListenerHook(hooknum);
                    DeleteLocalInt(oUser, "dmfi_MyListenerHook");
                }

                // destroy any existing ditto voice
                if (GetIsObjectValid(GetLocalObject(oUser, "dmfi_MyVoice")))
                {
                    DestroyObject(GetLocalObject(oUser, "dmfi_MyVoice"));
                    DeleteLocalObject(oUser, "dmfi_MyVoice");
                    FloatingTextStringOnCreature("You have destroyed your previous Voice", oUser, FALSE);
                }
            }
            break;

        default:
            // record a new phrase
            FloatingTextStringOnCreature("Ready to record new phrase", oUser, FALSE);
            SetLocalInt(oUser, "hls_EditPhrase", 20600 + iSay);
            // set up to capture next spoken line of text
            DMFI_get_line(oUser, TALKVOLUME_TALK, "dmfi_univ_listen", OBJECT_SELF);
            break;
        }
    }

    // You targeted an NPC or Object - Say Something!
    else
    {
        switch (iSay)
        {
        // Toggle a SINGLE NPC mute / unmute function
        case 8: SetLocalInt(oTarget, "dmfi_Mute", abs(GetLocalInt(oTarget, "dmfi_Mute") - 1));
            break;

        case 9:
            // XXXXX Set a Single NPC to listen and make it your target - VOICE WIDGET FUNCTION
            // SetLocalObject(oUser, "dmfi_VoiceTarget", oTarget);
            // if (!GetIsPC(oTarget))
            // {
            //     FloatingTextStringOnCreature(GetName(oTarget) + " is listening", oUser, FALSE);
            //     SetListenPattern(oTarget, "**", LISTEN_PATTERN); //listen to all text
            //     SetLocalInt(oTarget, "hls_Listening", 1); //listen to all text
            //     SetListening(oTarget, TRUE);      //be sure NPC is listening
            // }
            // //You Targetted a PC - make a voice follow that sucker and listen.
            // else
            // {
            //     //delete any valid following voices to stop duplicates
            //     if (GetIsObjectValid(GetLocalObject(oTarget, "dmfi_VoiceFollow")))
            //     {
            //         DestroyObject(GetLocalObject(oUser, "dmfi_VoiceFollow"));
            //         FloatingTextStringOnCreature("The prior voice following this character was destroyed", oUser, FALSE);
            //     }
            //
            //     //Create the Voice
            //     oVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", lLocation);
            //     //Sets the Voice as the object to throw to.
            //     DelayCommand(2.0, SetLocalObject(oTarget, "dmfi_VoiceFollow", oVoice)); //only set this for finding a duplicate later
            //     DelayCommand(2.0, SetLocalObject(oVoice, "dmfi_follow", oTarget));  //set up the player as something to follow
            //     DelayCommand(1.0f, FloatingTextStringOnCreature("The Voice will follow and listen to " +GetName(oTarget), oUser, FALSE));
            // }
            // break;

            // v1.09 - eavesdrop on pc
            {
                int hooknum = GetLocalInt(oUser, "dmfi_MyListenerHook");
                if (hooknum != 0) RemoveListenerHook(hooknum);
                int hookparty = GetLocalInt(oUser, "dmfi_MyListenerPartyMode");
                int hookbcast = GetLocalInt(oUser, "dmfi_MyListenerBcastMode");
                hooknum = AppendListenerHook(1, oTarget, lLocation,
                        DMFI_CHANNELMASK_TALK|DMFI_CHANNELMASK_WHISPER,
                        hookparty, hookbcast, oUser);
                if (hooknum != 0)
                {
                    SetLocalObject(oUser, "dmfi_VoiceTarget", oTarget);
                    if (GetIsPC(oTarget))
                    {
                        // targetted PC -
                        // delete any valid following voices to stop duplicates
                        object oVoice = GetLocalObject(oTarget, "dmfi_VoiceFollow");
                        if (GetIsObjectValid(oVoice))
                        {
                            DestroyObject(oVoice);
                            DeleteLocalObject(oTarget, "dmfi_VoiceFollow");
                            FloatingTextStringOnCreature("The prior voice following this character was destroyed", oUser, FALSE);
                        }

                        // 08.05.13 tsunami282 - we don't use following voices anymore
                        // // Create the Voice
                        // oVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", lLocation);
                        // // Sets the Voice as the object to throw to.
                        // DelayCommand(2.0, SetLocalObject(oTarget, "dmfi_VoiceFollow", oVoice)); //only set this for finding a duplicate later
                        // DelayCommand(2.0, SetLocalObject(oVoice, "dmfi_follow", oTarget));  //set up the player as something to follow
                        // DelayCommand(1.0f, FloatingTextStringOnCreature("The Voice will follow " +GetName(oTarget), oUser, FALSE));
                    }
                    else
                    {
                        // targetted NPC - nothing else needed to do
                    }
                }
                else
                {
                    SendMessageToPC(oUser, "ERROR: could not append listener hook!");
                }
                SetLocalInt(oUser, "dmfi_MyListenerHook", hooknum);
            }
            break;

        case 10:
            // v1.09 - Toggle PC single/party eavesdropping
            {
                // v1.09 - toggle eavesdrop mode - single/party
                int partylisten = GetLocalInt(oUser, "dmfi_MyListenerPartyMode");
                partylisten++;
                if (partylisten > 1) partylisten = 0;
                SetLocalInt(oUser, "dmfi_MyListenerPartyMode", partylisten);
                DelayCommand(1.0f, FloatingTextStringOnCreature("PC eavesdrop mode for new eavesdroppers set to " + (partylisten ? "PARTY" : "PC ONLY"), oUser, FALSE));
            }
            break;
        default:
            sSay = GetDMFIPersistentString("dmfi", "hls206" + IntToString(iSay));
            AssignCommand(oTarget, SpeakString(sSay));
            break;
        }
    }
}

////////////////////////////////////////////////////////////////////////
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

////////////////////////////////////////////////////////////////////////
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

////////////////////////////////////////////////////////////////////////
void DoAfflictFunction(int iAfflict, object oUser)
{
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

    if (GetIsDMPossessed(oUser))
    {
        nDNum = GetLocalInt(GetMaster(oUser), "dmfi_damagemodifier");
        fDuration = GetLocalFloat(GetMaster(oUser), "dmfi_stunduration");
        fSaveAmount = GetLocalFloat(GetMaster(oUser), "dmfi_saveamount");
    }
    else
    {
        nDNum = GetLocalInt(oUser, "dmfi_damagemodifier");
        fDuration = GetLocalFloat(oUser, "dmfi_stunduration");
        fSaveAmount = GetLocalFloat(oUser, "dmfi_saveamount");
    }

    nSaveAmount = FloatToInt(fSaveAmount);

    if (!(GetObjectType(oTarget) == OBJECT_TYPE_CREATURE) ||
        GetIsDM(oTarget))
    {
        FloatingTextStringOnCreature("You must target a valid creature!", oUser, FALSE);
        return;
    }
    switch (iAfflict)
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

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI XP Wand
void DoXPFunction(int iXP, object oUser)
{
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    object oPartyMember;
    int iHD;
    int iParty = 0;
    int iPercent = 0;
    int iReward = 0;
    int iGold = 0;
    int iValue = 0;

    string sFloating = "DM Granted Bonus";

    switch (iXP)
    {
    case 8: DMFI_NextTarget(oTarget, oUser); return; break;
    case 11: sFloating =  "Roleplaying Bonus"; iPercent = 1; break;
    case 12: sFloating =  "Roleplaying Bonus"; iPercent = 2; break;
    case 13: sFloating =  "Roleplaying Bonus"; iPercent = 3; break;
    case 14: sFloating =  "Roleplaying Bonus"; iPercent = 4; break;
    case 15: sFloating =  "Roleplaying Bonus"; iPercent = 5; break;
    case 21: sFloating =  "Main Plot Bonus"; iPercent = 10; break;
    case 22: sFloating =  "Main Plot Bonus"; iPercent = 20; break;
    case 23: sFloating =  "Main Plot Bonus"; iPercent = 25; break;
    case 24: sFloating =  "Main Plot Bonus"; iPercent = 33; break;
    case 25: sFloating =  "Main Plot Bonus"; iPercent = 50; break;
    case 31: sFloating =  "Main Plot Bonus"; iPercent = 10; iParty = 1; break;
    case 32: sFloating =  "Main Plot Bonus"; iPercent = 20; iParty = 1; break;
    case 33: sFloating =  "Main Plot Bonus"; iPercent = 25; iParty = 1; break;
    case 34: sFloating =  "Main Plot Bonus"; iPercent = 33; iParty = 1; break;
    case 35: sFloating =  "Main Plot Bonus"; iPercent = 50; iParty = 1; break;
    case 41: sFloating =  "Best In Game Bonus"; iPercent = 2; break;
    case 42: sFloating =  "Best In Game Bonus"; iPercent = 5; break;
    case 43: sFloating =  "Best In Game Bonus"; iPercent = 10; break;
    case 44: sFloating =  "Best In Game Bonus"; iPercent = 20; break;
    case 45: sFloating =  "Best In Game Bonus"; iPercent = 25; break;
    case 51: iParty = 1; iReward = 100; break;
    case 52: iParty = 1; iReward = 250; break;
    case 53: iParty = 1; iReward = 500; break;
    case 54: iParty = 1; iReward = 1000; break;
    case 55: iParty = 1; iReward = 2000; break;
    case 61:   iHD = GetHitDice(oTarget);
        SendMessageToPC(oUser, GetName(oTarget) +" has received " + IntToString(GetLocalInt(oPartyMember, "dmfi_XPGiven")) + " DMFI WAND XP this session.");
        SendMessageToPC(oUser, GetName(oTarget) +" currently has " + IntToString(GetXP(oTarget)) + " total XP.");
        SendMessageToPC(oUser, GetName(oTarget) +" currently needs " + IntToString(((iHD * (iHD + 1)) / 2 * 1000) - GetXP(oTarget)) + " to level.");
        SendMessageToPC(oUser, GetName(oTarget) +" has "+ IntToString(GetGold(oTarget)) + " gp.");
        SendMessageToPC(oUser, GetName(oTarget) +" has items totaling " + IntToString(DMFI_GetNetWorth(oTarget)) + " in gp value.");
        return; break;
    case 62:   oPartyMember=GetFirstFactionMember(oTarget, TRUE);
        while (GetIsObjectValid(oPartyMember)==TRUE)
        {
            iGold = iGold + GetGold(oPartyMember);
            iValue = iValue + DMFI_GetNetWorth(oPartyMember);
            SendMessageToPC(oUser, GetName(oPartyMember) +" has " + IntToString(GetXP(oPartyMember)) + " XP total.");
            oPartyMember = GetNextFactionMember(oTarget, TRUE);
        }
        SendMessageToPC(oUser, "The party has a total of "+ IntToString(iGold) + " gp.");
        SendMessageToPC(oUser, "The party has items totaling " + IntToString(iValue) + " in gp value.");
        return; break;
    case 63:   oPartyMember=GetFirstFactionMember(oTarget, TRUE);
        while (GetIsObjectValid(oPartyMember)==TRUE)
        {
            SendMessageToPC(oUser, GetName(oPartyMember) +" has received " + IntToString(GetLocalInt(oPartyMember, "dmfi_XPGiven")) + " DMFI WAND XP this session.");
            oPartyMember = GetNextFactionMember(oTarget, TRUE);
        }
        return; break;
    case 64:   oPartyMember=GetFirstFactionMember(oTarget, TRUE);
        while (GetIsObjectValid(oPartyMember)==TRUE)
        {
            int iHD = GetHitDice(oPartyMember);
            SendMessageToPC(oUser, GetName(oPartyMember) + " is level " + IntToString(GetHitDice(oPartyMember)) + " and needs " + IntToString(((iHD * (iHD + 1)) / 2 * 1000) - GetXP(oPartyMember)) + " XP to level up.");
            oPartyMember = GetNextFactionMember(oTarget, TRUE);
        }
        return; break;
    case 71:  sFloating =  "DM XP PENALTY"; iReward = -50; break;
    case 72:  sFloating =  "DM XP PENALTY"; iReward = -100; break;
    case 73:  sFloating =  "DM XP PENALTY"; iReward = -250; break;
    case 74:  sFloating =  "DM XP PENALTY"; iReward = -500; break;
    case 75:  sFloating =  "DM XP PENALTY"; iReward = -1000; break;
    case 76:  sFloating =  "DM XP PENALTY"; iReward = -2000; break;
    case 77:  sFloating =  "DM XP PENALTY"; iReward = -((GetHitDice(oTarget) * (GetHitDice(oTarget)-1))/2 * 1000); break;
    case 78:  sFloating =  "DM XP PENALTY"; iReward = -((GetHitDice(oTarget) * (GetHitDice(oTarget)-1))/2 * 1000) - (((GetHitDice(oTarget)-1)*1000)/2); break;
    case 79:  sFloating =  "DM XP PENALTY"; iReward = -((GetXP(oTarget))-(GetXP(oTarget))+1); break;
    default: return;
    }

    if (iParty==1)
    {
        // 2008.05.26 tsunami282 - grant percent XP based on each party member's level, not selected party member
        int bUsePercent = FALSE;
        if (iReward==0) bUsePercent = TRUE;

        oPartyMember=GetFirstFactionMember(oTarget, TRUE);
        while (GetIsObjectValid(oPartyMember))
        {
            if (bUsePercent) iReward = (GetHitDice(oPartyMember)*iPercent*10);
            GiveXPToCreature(oPartyMember, iReward);
            SetLocalInt(oPartyMember, "dmfi_XPGiven", GetLocalInt(oPartyMember, "dmfi_XPGiven") + iReward);
            FloatingTextStringOnCreature(sFloating + ": " + IntToString(iReward), oPartyMember, FALSE);
            SendMessageToAllDMs(GetName(oPartyMember) +" received a "+GetLocalString(oUser, "BonusType")+ " experience reward of "+ IntToString(iReward)+ ".");
            oPartyMember = GetNextFactionMember(oTarget, TRUE);
        }
        // SendMessageToAllDMs("The entire party was granted "+ IntToString(iReward)+ " XP.");
    }

    else  //single player code
    {
        if (iReward==0)
            iReward = (GetHitDice(oTarget)*iPercent*10);

        int nPrior = GetXP(oTarget);

        SetXP(oTarget, nPrior+iReward);

        SetLocalInt(oTarget, "dmfi_XPGiven", GetLocalInt(oTarget, "dmfi_XPGiven") + iReward);
        FloatingTextStringOnCreature(sFloating + ": " + IntToString(iReward), oTarget, FALSE);
        SendMessageToAllDMs(GetName(oTarget) +" received a "+GetLocalString(oUser, "BonusType")+ " experience reward of "+ IntToString(iReward)+ ".");

    }
    return;
}

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI Music Wand
void DoMusicFunction(int iMusic, object oUser)
{
    int iSet;
    switch (iMusic)
    {
    case 11: MusicBackgroundPlay(GetArea(oUser)); return; break;
    case 12: MusicBackgroundStop(GetArea(oUser)); DelayCommand(1.0, MusicBackgroundStop(GetArea(oUser))); return; break;
    case 13: iSet = TRACK_BATTLE_WINTER; break;
    case 14: iSet = TRACK_BATTLE_DESERT; break;
    case 15: iSet = TRACK_DESERT_DAY; break;
    case 16: iSet = TRACK_DESERT_NIGHT; break;
    case 17: iSet = TRACK_WINTER_DAY;break;
    case 18: iSet = TRACK_HOTU_UNDERMOUNTAIN; break;
    case 19: iSet = TRACK_HOTU_WATERDEEP; break;
    case 21: iSet = TRACK_HOTU_BATTLE_BOSS1; break;
    case 22: iSet = TRACK_HOTU_BATTLE_BOSS2; break;
    case 23: iSet = TRACK_HOTU_BATTLE_HELL; break;
    case 24: iSet = TRACK_HOTU_THEME;break;
    case 25: iSet = TRACK_HOTU_REBELCAMP;break;
    case 26: iSet = TRACK_HOTU_QUEEN;break;
    case 27: iSet = TRACK_HOTU_DRACOLICH;break;
    case 28: iSet = TRACK_HOTU_FIREPLANE;break;
    case 29: iSet = TRACK_HOTU_HELLFROZEOVER;break;
    case 31: iSet = 34; break;
    case 32: iSet = 35; break;
    case 33: iSet = 36; break;
    case 34: iSet = 37; break;
    case 35: iSet = 38; break;
    case 36: iSet = 39; break;
    case 37: iSet = 40; break;
    case 38: iSet = 41; break;
    case 39: iSet = 42; break;
    case 41: iSet = 43; break;
    case 42: iSet = 44; break;
    case 43: iSet = 45; break;
    case 44: iSet = 46; break;
    case 45: iSet = 47; break;
    case 46: iSet = 48; break;
    case 51: iSet = 15; break;
    case 52: iSet = 16; break;
    case 53: iSet = 17; break;
    case 54: iSet = 18; break;
    case 55: iSet = 19; break;
    case 56: iSet = 20; break;
    case 57: iSet = 21; break;
    case 58: iSet = 29; break;
    case 61: iSet = 22; break;
    case 62: iSet = 23; break;
    case 63: iSet = 24; break;
    case 64: iSet = 56; break;
    case 65: iSet = 25; break;
    case 66: iSet = 26; break;
    case 67: iSet = 27; break;
    case 68: iSet = 49; break;
    case 69: iSet = 50; break;
    case 71: iSet = 28; break;
    case 72: iSet = 7; break;
    case 73: iSet = 8; break;
    case 74: iSet = 9; break;
    case 75: iSet = 10; break;
    case 76: iSet = 11; break;
    case 77: iSet = 12; break;
    case 78: iSet = 13; break;
    case 79: iSet = 14; break;
    case 81: iSet = 1; break;
    case 82: iSet = 2; break;
    case 83: iSet = 3; break;
    case 84: iSet = 4; break;
    case 85: iSet = 5; break;
    case 86: iSet = 6; break;
    case 91: iSet = 30; break;
    case 92: iSet = 31; break;
    case 93: iSet = 32; break;
    case 94: iSet = 33; break;
    case 95: iSet = 51; break;
    case 96: iSet = 52; break;
    case 97: iSet = 53; break;
    case 98: iSet = 54; break;
    case 99: iSet = 55; break;
    default: break;
    }

    MusicBackgroundStop(GetArea(oUser));
    MusicBackgroundChangeDay(GetArea(oUser), iSet);
    MusicBackgroundChangeNight(GetArea(oUser), iSet);
    MusicBackgroundPlay(GetArea(oUser));
    return;
}

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI Encounter Wand
void Spawn(string sCreature, location lCreature, int iTF = FALSE)
{
    CreateObject(OBJECT_TYPE_CREATURE, sCreature, lCreature, iTF);
}

////////////////////////////////////////////////////////////////////////
void CopyMon(object oMon, location lEncounter)
{
    object oCreature = CopyObject(oMon, lEncounter);
    effect eEffect = GetFirstEffect(oMon);
    while (GetIsEffectValid(eEffect))
    {
        ApplyEffectToObject(DURATION_TYPE_PERMANENT, eEffect, oCreature);
        eEffect = GetNextEffect(oMon);
    }
}

////////////////////////////////////////////////////////////////////////
void CreateCustomEncounter(string Template, location lEncounter)
{
    object oWP = GetWaypointByTag(Template);
    int n = 1;
    object oMon = GetNearestCreatureToLocation(CREATURE_TYPE_IS_ALIVE, TRUE, GetLocation(oWP), n);

    while (GetIsObjectValid(oMon)  && (GetDistanceBetween(oWP, oMon)<8.0) && (n<9))
    {
        DelayCommand(IntToFloat(n), CopyMon(oMon, lEncounter));
        n=n+1;
        oMon = GetNearestCreatureToLocation(CREATURE_TYPE_IS_ALIVE, TRUE, GetLocation(oWP), n);
    }
}

////////////////////////////////////////////////////////////////////////
void CreateEncounter(int iEncounter, location lEncounter, object oUser)
{
    SetLocalInt(oUser, "EncounterType", iEncounter);
    switch (iEncounter)
    {
    case 11: //Animal - Low Badger Encounter
        SetLocalString(oUser, "EncounterName", "Low Badger");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_BADGER", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_BADGER", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BADGER", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BADGER", lEncounter, FALSE));
        break;
    case 12: //Animal - Low Canine Encounter
        SetLocalString(oUser, "EncounterName", "Low Canine");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_WOLF", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_WOLF", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_WOLF", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_WOLF", lEncounter, FALSE));
        break;
    case 13: //Animal - Low Feline Encounter
        SetLocalString(oUser, "EncounterName", "Low Feline");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_COUGAR", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_COUGAR", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_COUGAR", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_COUGAR", lEncounter, FALSE));
        break;
    case 14: //Animal - Low Bear Encounter
        SetLocalString(oUser, "EncounterName", "Low Bear (Boss)");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_BEARBLCK", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_BEARBLCK", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BEARBLCK", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BEARBRWN", lEncounter, FALSE));
        break;
    case 15: //Animal - Boar Encounter
        SetLocalString(oUser, "EncounterName", "Boar (Boss)");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_BOAR", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_BOAR", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BOAR", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BOARDIRE", lEncounter, FALSE));
        break;
    case 16: //Animal - Medium Feline Encounter
        SetLocalString(oUser, "EncounterName", "Medium Feline");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_LION", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_LION", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_LION", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_LION", lEncounter, FALSE));
        break;
    case 17: //Animal - High Canine Encounter
        SetLocalString(oUser, "EncounterName", "High Canine");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_DIREWOLF", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_DIREWOLF", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_DIREWOLF", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DIREWOLF", lEncounter, FALSE));
        break;
    case 18: //Animal - High Feline Encounter
        SetLocalString(oUser, "EncounterName", "High Feline");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_DIRETIGER", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_BEASTMALAR001", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BEASTMALAR001", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BEASTMALAR001", lEncounter, FALSE));
        break;
    case 19: //Animal - High Bear Encounter
        SetLocalString(oUser, "EncounterName", "High Bear");
        CreateObject(OBJECT_TYPE_CREATURE, "NW_BEARDIRE", lEncounter, FALSE);
        DelayCommand(1.0f, Spawn("NW_BEARDIRE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BEARDIRE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BEARDIREBOSS", lEncounter, FALSE));
        break;

    case 21: //Construct - Flesh Golem
        SetLocalString(oUser, "EncounterName", "Flesh Golem");
        DelayCommand(1.0f, Spawn("NW_GOLFLESH", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GOLFLESH", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GOLFLESH", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GOLFLESH", lEncounter, FALSE));
        break;
    case 22: //Construct - Minogan
        SetLocalString(oUser, "EncounterName", "Minogon");
        DelayCommand(1.0f, Spawn("NW_MINOGON", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_MINOGON", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_MINOGON", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_MINOGON", lEncounter, FALSE));
        break;
    case 23: //Construct - Clay Golem
        SetLocalString(oUser, "EncounterName", "Clay Golem");
        DelayCommand(1.0f, Spawn("NW_GolClay", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GolClay", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GolClay", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GolClay", lEncounter, FALSE));
        break;
    case 24: //Construct - Bone Golem
        SetLocalString(oUser, "EncounterName", "Bone Golem");
        DelayCommand(1.0f, Spawn("NW_GolBone", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GolBone", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GolBone", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GolBone", lEncounter, FALSE));
        break;
    case 25: //Construct - Helmed Horror
        SetLocalString(oUser, "EncounterName", "Helmed Horror");
        DelayCommand(1.0f, Spawn("NW_HELMHORR", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_HELMHORR", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_HELMHORR", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_HELMHORR", lEncounter, FALSE));
        break;
    case 26: //Construct - Stone Golem
        SetLocalString(oUser, "EncounterName", "Stone Golem");
        DelayCommand(1.0f, Spawn("NW_GOLSTONE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GOLSTONE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GOLSTONE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GOLSTONE", lEncounter, FALSE));
        break;
    case 27: //Construct - Battle Horror
        SetLocalString(oUser, "EncounterName", "Battle Horror");
        DelayCommand(1.0f, Spawn("NW_BATHORROR", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BATHORROR", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BATHORROR", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BATHORROR", lEncounter, FALSE));
        break;
    case 28: //Construct - Shield Guardian
        SetLocalString(oUser, "EncounterName", "Shield Guardian");
        DelayCommand(1.0f, Spawn("NW_SHGUARD", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SHGUARD", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SHGUARD", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SHGUARD", lEncounter, FALSE));
        break;
    case 29: //Construct - Iron Golem
        SetLocalString(oUser, "EncounterName", "Iron Golem");
        DelayCommand(1.0f, Spawn("NW_GOLIRON", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GOLIRON", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GOLIRON", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GOLIRON", lEncounter, FALSE));
        break;
    case 31: //Dragon - Adult White Dragon
        SetLocalString(oUser, "EncounterName", "Adult White Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGWHITE001", lEncounter, FALSE));
        break;
    case 32: //Dragon - Adult Black Dragon
        SetLocalString(oUser, "EncounterName", "Adult Black Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGBLACK001", lEncounter, FALSE));
        break;
    case 33: //Dragon - Adult Green Dragon
        SetLocalString(oUser, "EncounterName", "Adult Green Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGGREEN001", lEncounter, FALSE));
        break;
    case 34: //Dragon - Adult Blue Dragon
        SetLocalString(oUser, "EncounterName", "Adult Blue Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGBLUE001", lEncounter, FALSE));
        break;
    case 35: //Dragon - Adult Red Dragon
        SetLocalString(oUser, "EncounterName", "Adult Red Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGRED001", lEncounter, FALSE));
        break;
    case 36: //Dragon - Old White Dragon
        SetLocalString(oUser, "EncounterName", "Old White Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGWHITE002", lEncounter, FALSE));
        break;
    case 37: //Dragon - Old Blue Dragon
        SetLocalString(oUser, "EncounterName", "Old Blue Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGBLUE002", lEncounter, FALSE));
        break;
    case 38: //Dragon - Old Red Dragon
        SetLocalString(oUser, "EncounterName", "Old Red Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGRED002", lEncounter, FALSE));
        break;
    case 39: //Dragon - Ancient Red Dragon
        SetLocalString(oUser, "EncounterName", "Ancient Red Dragon");
        DelayCommand(1.0f, Spawn("NW_DRGRED003", lEncounter, FALSE));
        break;
    case 41: //Elemental - Air Elemental
        SetLocalString(oUser, "EncounterName", "Air Elemental");
        DelayCommand(1.0f, Spawn("NW_AIR", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_AIR", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_AIR", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_AIR", lEncounter, FALSE));
        break;
    case 42: //Elemental - Earth Elemental
        SetLocalString(oUser, "EncounterName", "Earth Elemental");
        DelayCommand(1.0f, Spawn("NW_EARTH", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_EARTH", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_EARTH", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_EARTH", lEncounter, FALSE));
        break;
    case 43: //Elemental - Fire Elemental
        SetLocalString(oUser, "EncounterName", "Fire Elemental");
        DelayCommand(1.0f, Spawn("NW_FIRE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_FIRE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_FIRE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_FIRE", lEncounter, FALSE));
        break;
    case 44: //Elemental - Water Elemental
        SetLocalString(oUser, "EncounterName", "Water Elemental");
        DelayCommand(1.0f, Spawn("NW_WATER", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_WATER", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_WATER", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_WATER", lEncounter, FALSE));
        break;
    case 45: //Elemental - Huge Air Elemental
        SetLocalString(oUser, "EncounterName", "Huge Air Elemental");
        DelayCommand(1.0f, Spawn("NW_AIRHUGE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_AIRHUGE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_AIRHUGE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_AIRHUGE", lEncounter, FALSE));
        break;
    case 46: //Elemental - Huge Earth Elemental
        SetLocalString(oUser, "EncounterName", "Huge Earth Elemental");
        DelayCommand(1.0f, Spawn("NW_EARTHHUGE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_EARTHHUGE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_EARTHHUGE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_EARTHHUGE", lEncounter, FALSE));
        break;
    case 47: //Elemental - Huge Fire Elemental
        SetLocalString(oUser, "EncounterName", "Huge Fire Elemental");
        DelayCommand(1.0f, Spawn("NW_FIREHUGE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_FIREHUGE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_FIREHUGE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_FIREHUGE", lEncounter, FALSE));
        break;
    case 48: //Elemental - Huge Water Elemental
        SetLocalString(oUser, "EncounterName", "Huge Water Elemental");
        DelayCommand(1.0f, Spawn("NW_WATERHUGE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_WATERHUGE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_WATERHUGE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_WATERHUGE", lEncounter, FALSE));
        break;
    case 49: //Elemental - Elemental Swarm
        SetLocalString(oUser, "EncounterName", "Elemental Swarm");
        DelayCommand(1.0f, Spawn("NW_AIRGREAT", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_EARTHGREAT", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_FIREGREAT", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_WATERGREAT", lEncounter, FALSE));
        break;
    case 51: //Giant - Low Ogre
        SetLocalString(oUser, "EncounterName", "Low Ogre");
        DelayCommand(1.0f, Spawn("NW_OGRE01", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_OGRE01", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_OGRE02", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_OGRE02", lEncounter, FALSE));
        break;
    case 52: //Giant - Low Troll
        SetLocalString(oUser, "EncounterName", "Low Troll");
        DelayCommand(1.0f, Spawn("NW_TROLL", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_TROLL", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_TROLL", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_TROLL", lEncounter, FALSE));
        break;
    case 53: //Giant - High Ogre
        SetLocalString(oUser, "EncounterName", "High Ogre");
        DelayCommand(1.0f, Spawn("NW_OGRECHIEF01", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_OGRECHIEF02", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_OGRECHIEF01", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_OGREMAGE02", lEncounter, FALSE));
        break;
    case 54: //Giant - High Troll
        SetLocalString(oUser, "EncounterName", "High Troll");
        DelayCommand(1.0f, Spawn("NW_TROLLCHIEF", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_TROLLCHIEF", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_TROLLWIZ", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_TROLLWIZ", lEncounter, FALSE));
        break;
    case 55: //Giant - Ettin
        SetLocalString(oUser, "EncounterName", "Ettin");
        DelayCommand(1.0f, Spawn("NW_ETTIN", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_ETTIN", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_ETTIN", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ETTIN", lEncounter, FALSE));
        break;
    case 56: //Giant - Hill Giant
        SetLocalString(oUser, "EncounterName", "Hill Giant");
        DelayCommand(1.0f, Spawn("NW_GNTHILL", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GNTHILL", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GNTMOUNT", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GNTMOUNT", lEncounter, FALSE));
        break;
    case 57: //Giant - Frost Giant
        SetLocalString(oUser, "EncounterName", "Frost Giant");
        DelayCommand(1.0f, Spawn("NW_GNTFROST", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GNTFROST", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GNTFROST", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GNTFROST", lEncounter, FALSE));
        break;
    case 58: //Giant - Fire Giant
        SetLocalString(oUser, "EncounterName", "Fire Giant");
        DelayCommand(1.0f, Spawn("NW_GNTFIRE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GNTFIRE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GNTFIRE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GNTFIRE", lEncounter, FALSE));
        break;
    case 59: //Giant - Ogre Mage (Boss)
        SetLocalString(oUser, "EncounterName", "Ogre Mage (Boss)");
        DelayCommand(1.0f, Spawn("nw_ogreboss", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("nw_ogreboss", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_OGREMAGEBOSS", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_OGREMAGEBOSS", lEncounter, FALSE));
        break;
    case 61: //Humanoid - Goblin
        SetLocalString(oUser, "EncounterName", "Goblin");
        DelayCommand(1.0f, Spawn("NW_GOBLINA", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GOBLINA", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GOBLINA", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GOBLINB", lEncounter, FALSE));
        break;
    case 62: //Humanoid - Kobold
        SetLocalString(oUser, "EncounterName", "Kobold");
        DelayCommand(1.0f, Spawn("NW_KOBOLD002", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_KOBOLD002", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_KOBOLD002", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_KOBOLD001", lEncounter, FALSE));
        break;
    case 63: //Humanoid - Low Orc
        SetLocalString(oUser, "EncounterName", "Low Orc");
        DelayCommand(1.0f, Spawn("NW_ORCB", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_ORCA", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_ORCA", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ORCA", lEncounter, FALSE));
        break;
    case 64: //Humanoid - High Orc (Wiz)
        SetLocalString(oUser, "EncounterName", "High Orc (Wiz)");
        DelayCommand(1.0f, Spawn("NW_OrcChiefA", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_ORCCHIEFB", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_ORCCHIEFB", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ORCWIZA", lEncounter, FALSE));
        break;
    case 65: //Humanoid - Bugbear
        SetLocalString(oUser, "EncounterName", "Bugbear");
        DelayCommand(1.0f, Spawn("NW_BUGBEARA", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BUGBEARA", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BUGBEARA", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BUGBEARB", lEncounter, FALSE));
        break;
    case 66: //Humanoid - Lizardfolk
        SetLocalString(oUser, "EncounterName", "Lizardfolk");
        DelayCommand(1.0f, Spawn("NW_OLDWARRA", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_OLDWARRA", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_OLDWARRA", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_OLDWARB", lEncounter, FALSE));
        break;
    case 67: //Humanoid - Minotaur (Wiz)
        SetLocalString(oUser, "EncounterName", "Minotaur (Wiz)");
        DelayCommand(1.0f, Spawn("NW_MINOTAUR", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_MINOTAUR", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_MINOTAUR", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_MINWIZ", lEncounter, FALSE));
        break;
    case 68: //Humanoid - Fey
        SetLocalString(oUser, "EncounterName", "Fey (Mixed)");
        DelayCommand(1.0f, Spawn("NW_GRIG", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GRIG", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_PIXIE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_PIXIE", lEncounter, FALSE));
        break;
    case 69: //Humanoid -  Yuan-Ti (Mixed)
        SetLocalString(oUser, "EncounterName", "Yuan-Ti (Mixed)");
        DelayCommand(1.0f, Spawn("NW_YUAN_TI001", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_YUAN_TI001", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_YUAN_TI002", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_YUAN_TI003", lEncounter, FALSE));
        break;
    case 71: //Insects - Fire Beetle
        SetLocalString(oUser, "EncounterName", "Fire Beetle");
        DelayCommand(1.0f, Spawn("NW_BTLFIRE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BTLFIRE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BTLFIRE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BTLFIRE", lEncounter, FALSE));
        break;
    case 72: //Insects - Spitting Fire Beetle
        SetLocalString(oUser, "EncounterName", "Spitting Fire Beetle");
        DelayCommand(1.0f, Spawn("NW_BTLFIRE02", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BTLFIRE02", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BTLFIRE02", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BTLFIRE02", lEncounter, FALSE));
        break;
    case 73: //Insects - Low Beetle (Mixed)
        SetLocalString(oUser, "EncounterName", "Low Beetle (Mixed)");
        DelayCommand(1.0f, Spawn("NW_BTLBOMB", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BTLBOMB", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BTLSTINK", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BTLFIRE02", lEncounter, FALSE));
        break;
    case 74: //Insects - Giant Spider
        SetLocalString(oUser, "EncounterName", "Giant Spider");
        DelayCommand(1.0f, Spawn("NW_SPIDGIANT", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SPIDGIANT", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SPIDGIANT", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SPIDGIANT", lEncounter, FALSE));
        break;
    case 75: //Insects - Sword Spider
        SetLocalString(oUser, "EncounterName", "Sword Spider");
        DelayCommand(1.0f, Spawn("NW_SPIDSWRD", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SPIDSWRD", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SPIDSWRD", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SPIDSWRD", lEncounter, FALSE));
        break;
    case 76: //Insects - Wraith Spider
        SetLocalString(oUser, "EncounterName", "Wraith Spider");
        DelayCommand(1.0f, Spawn("NW_SPIDWRA", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SPIDWRA", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SPIDWRA", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SPIDWRA", lEncounter, FALSE));
        break;
    case 77: //Insects - Stag Beetle
        SetLocalString(oUser, "EncounterName", "Stag Beetle");
        DelayCommand(1.0f, Spawn("NW_BTLSTAG", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BTLSTAG", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BTLSTAG", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BTLSTAG", lEncounter, FALSE));
        break;
    case 78: //Insects - Dire Spider
        SetLocalString(oUser, "EncounterName", "Dire Spider");
        DelayCommand(1.0f, Spawn("NW_SPIDDIRE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SPIDDIRE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SPIDDIRE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SPIDDIRE", lEncounter, FALSE));
        break;
    case 79: //Insects - Queen Spider
        SetLocalString(oUser, "EncounterName", "Queen Spider");
        DelayCommand(1.0f, Spawn("NW_SPIDERBOSS", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SPIDERBOSS", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SPIDERBOSS", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SPIDERBOSS", lEncounter, FALSE));
        break;
    case 81: //Undead - Low Zombie
        SetLocalString(oUser, "EncounterName", "Zombie");
        DelayCommand(1.0f, Spawn("NW_ZOMBIE01", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_ZOMBIE02", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_ZOMBIE01", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ZOMBIE02", lEncounter, FALSE));
        break;
    case 82: //Undead - Low Skeleton
        SetLocalString(oUser, "EncounterName", "Low Skeleton");
        DelayCommand(1.0f, Spawn("NW_SKELETON", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SKELETON", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SKELETON", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SKELETON", lEncounter, FALSE));
        break;
    case 83: //Undead - Ghoul
        SetLocalString(oUser, "EncounterName", "Ghoul");
        DelayCommand(1.0f, Spawn("NW_GHOUL", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GHOUL", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GHOUL", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GHOUL", lEncounter, FALSE));
        break;
    case 84: //Undead - Shadow
        SetLocalString(oUser, "EncounterName", "Shadow");
        DelayCommand(1.0f, Spawn("NW_SHADOW", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SHADOW", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SHADOW", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SHADOW", lEncounter, FALSE));
        break;
    case 85: //Undead - Mummy
        SetLocalString(oUser, "EncounterName", "Mummy");
        DelayCommand(1.0f, Spawn("NW_MUMMY", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_MUMMY", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_MUMMY", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_MUMMY", lEncounter, FALSE));
        break;
    case 86: //Undead - High Skeleton
        SetLocalString(oUser, "EncounterName", "High Skeleton (Mixed)");
        DelayCommand(1.0f, Spawn("NW_SKELWARR01", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_SKELWARR02", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_SKELMAGE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_SKELPRIEST", lEncounter, FALSE));
        break;
    case 87: //Undead - Curst (Mixed)
        SetLocalString(oUser, "EncounterName", "Curst (Mixed)");
        DelayCommand(1.0f, Spawn("NW_CURST001", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_CURST002", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_CURST003", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_CURST004", lEncounter, FALSE));
        break;
    case 88: //Undead - Doom Knight
        SetLocalString(oUser, "EncounterName", "Doom Knight");
        DelayCommand(1.0f, Spawn("NW_DOOMKGHT", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_DOOMKGHT", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DOOMKGHT", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_DOOMKGHT", lEncounter, FALSE));
        break;
    case 89: //Undead - Vampire (Mixed)
        SetLocalString(oUser, "EncounterName", "Vampire (Mixed)");
        DelayCommand(1.0f, Spawn("NW_VAMPIRE001", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_VAMPIRE002", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_VAMPIRE003", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_VAMPIRE004", lEncounter, FALSE));
        break;
    case 91: //NPC - Low Gypsy
        SetLocalString(oUser, "EncounterName", "Low Gypsy");
        DelayCommand(1.0f, Spawn("NW_GYPMALE", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_GYPMALE", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_GYPFEMALE", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_GYPFEMALE", lEncounter, FALSE));
        break;
    case 92: //NPC - Low Bandit
        SetLocalString(oUser, "EncounterName", "Low Bandit");
        DelayCommand(1.0f, Spawn("NW_BANDIT001", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BANDIT001", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BANDIT001", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BANDIT002", lEncounter, FALSE));
        break;
    case 93: //NPC - Medium Bandit (Mixed)
        SetLocalString(oUser, "EncounterName", "Medium Bandit (Mixed)");
        DelayCommand(1.0f, Spawn("NW_BANDIT005", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_BANDIT002", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_BANDIT003", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_BANDIT004", lEncounter, FALSE));
        break;
    case 94: //NPC - Low Mercenary (Mixed)
        SetLocalString(oUser, "EncounterName", "Low Mercenary (Mixed)");
        DelayCommand(1.0f, Spawn("NW_HUMANMERC001", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_HALFMERC001", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DWARFMERC001", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ELFMERC001", lEncounter, FALSE));
        break;
    case 95: //NPC - Elf Ranger
        SetLocalString(oUser, "EncounterName", "Elf Ranger");
        DelayCommand(1.0f, Spawn("NW_ELFRANGER005", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_ELFRANGER005", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_ELFRANGER005", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ELFRANGER005", lEncounter, FALSE));
        break;
    case 96: //NPC - Low Drow (Mixed)
        SetLocalString(oUser, "EncounterName", "Low Drow (Mixed)");
        DelayCommand(1.0f, Spawn("NW_DROWFIGHT005", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_DROWMAGE005", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DROWROGUE005", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_DROWCLER005", lEncounter, FALSE));
        break;
    case 97: //NPC - Medium Mercenary (Mixed)
        SetLocalString(oUser, "EncounterName", "Medium Mercenary (Mixed)");
        DelayCommand(1.0f, Spawn("NW_HUMANMERC004", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_HALFMERC004", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DWARFMERC004", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ELFMERC004", lEncounter, FALSE));
        break;
    case 98: //NPC - High Drow (Mixed)
        SetLocalString(oUser, "EncounterName", "High Drow (Mixed)");
        DelayCommand(1.0f, Spawn("NW_DROWFIGHT020", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_DROWMAGE020", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DROWROGUE020", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_DROWCLER020", lEncounter, FALSE));
        break;
    case 99: //NPC - High Mercenary (Mixed)
        SetLocalString(oUser, "EncounterName", "High Mercenary (Mixed)");
        DelayCommand(1.0f, Spawn("NW_HUMANMERC006", lEncounter, FALSE));
        DelayCommand(2.0f, Spawn("NW_HALFMERC006", lEncounter, FALSE));
        DelayCommand(3.0f, Spawn("NW_DWARFMERC006", lEncounter, FALSE));
        DelayCommand(4.0f, Spawn("NW_ELFMERC006", lEncounter, FALSE));
        break;
    case 101: // Custom Encounters
        CreateCustomEncounter("DMFI_E1", lEncounter); break;
    case 102:  CreateCustomEncounter("DMFI_E2", lEncounter); break;
    case 103:  CreateCustomEncounter("DMFI_E3", lEncounter); break;
    case 104:  CreateCustomEncounter("DMFI_E4", lEncounter); break;
    case 105:  CreateCustomEncounter("DMFI_E5", lEncounter); break;
    case 106:  CreateCustomEncounter("DMFI_E6", lEncounter); break;
    case 107:  CreateCustomEncounter("DMFI_E7", lEncounter); break;
    case 108:  CreateCustomEncounter("DMFI_E8", lEncounter); break;
    case 109:  CreateCustomEncounter("DMFI_E9", lEncounter); break;
    default:
        break;
    }
    return;
}

////////////////////////////////////////////////////////////////////////
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
    for (nCount = 0; nCount < 15; nCount++)
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

////////////////////////////////////////////////////////////////////////
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
    for (nCount = 0; nCount < 15; nCount++)
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

////////////////////////////////////////////////////////////////////////
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
    for (nCount = 0; nCount < 5; nCount++)
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

////////////////////////////////////////////////////////////////////////
void FnFEffect(object oUser, int iVFX, location lEffect, float fDelay)
{
    if (fDelay>2.0) FloatingTextStringOnCreature("Delay effect created", oUser, FALSE);
    DelayCommand( fDelay, ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(iVFX),lEffect));
}

////////////////////////////////////////////////////////////////////////
void CreateEffects(int iEffect, location lEffect, object oUser)
{
    float fDelay;
    float fDuration;
    float fBeamDuration;
    object oTarget;

    if (GetIsDMPossessed(oUser))
    {
        fDelay = GetLocalFloat(GetMaster(oUser), "dmfi_effectdelay");
        fDuration = GetLocalFloat(GetMaster(oUser), "dmfi_effectduration");
        fBeamDuration = GetLocalFloat(GetMaster(oUser), "dmfi_beamduration");
    }
    else
    {
        fDelay = GetLocalFloat(oUser, "dmfi_effectdelay");
        fDuration = GetLocalFloat(oUser, "dmfi_effectduration");
        fBeamDuration = GetLocalFloat(oUser, "dmfi_beamduration");
    }

    if (!GetIsObjectValid(GetLocalObject(oUser, "dmfi_univ_target")))
        oTarget = oUser;
    else
        oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    switch (iEffect)
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

////////////////////////////////////////////////////////////////////////
//This function is for the DMFI Emote Wand
void DoEmoteFunction(int iEmote, object oUser)
{
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    if (!GetIsObjectValid(oTarget))
        oTarget = oUser;
    float fDur = 9999.0f; //Duration

    switch (iEmote)
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

////////////////////////////////////////////////////////////////////////
void DoBuff (int iChoice, object oUser)
{
    int nChoice = 0;
    string sType;
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    int Party = GetLocalInt(oUser, "dmfi_buff_party");
    int CL;
    int nSpell1 = SPELL_ALL_SPELLS;
    int nSpell2 = SPELL_ALL_SPELLS;
    int nSpell3 = SPELL_ALL_SPELLS;


    switch (iChoice)
    {
    case 10: nChoice = -1; break;
    case 11: nChoice = SPELL_AURA_OF_VITALITY; break;
    case 12: nChoice = SPELL_BARKSKIN; break;
    case 13: nChoice = SPELL_BATTLETIDE; break;
    case 14: nChoice = SPELL_BLESS;  break;
    case 16: nChoice = SPELL_CLAIRAUDIENCE_AND_CLAIRVOYANCE; break;
    case 17: nChoice = SPELL_CLARITY;  break;
    case 18: nChoice = SPELL_DEATH_WARD;  break;
    case 19: nChoice = SPELL_DISPLACEMENT; break;
    case 20: nChoice = -1;  break;
    case 21: nChoice = SPELL_DIVINE_FAVOR;  break;
    case 22: nChoice = SPELL_DIVINE_POWER;  break;
    case 23: nChoice = SPELL_ENDURE_ELEMENTS; break;
    case 24: nChoice = SPELL_ENTROPIC_SHIELD; break;
    case 25: nChoice = SPELL_ELEMENTAL_SHIELD; break;
    case 26: nChoice = SPELL_ENERGY_BUFFER;  break;
    case 27: nChoice = SPELL_ETHEREAL_VISAGE;  break;
    case 28: nChoice = SPELL_GHOSTLY_VISAGE; break;
    case 29: nChoice = SPELL_GLOBE_OF_INVULNERABILITY; break;
    case 30: nChoice = -1;  break;
    case 31: nChoice = SPELL_SANCTUARY; break;
    case 32: nChoice = SPELL_GREATER_STONESKIN; break;
    case 33: nChoice = SPELL_GREATER_SPELL_MANTLE; break;
    case 34: nChoice = SPELL_HASTE;  break;
    case 35: nChoice = SPELL_INVISIBILITY;  break;
    case 36: nChoice = SPELL_IMPROVED_INVISIBILITY; break;
    case 37: nChoice = SPELL_LESSER_MIND_BLANK;break;
    case 38: nChoice = SPELL_LESSER_SPELL_MANTLE; break;
    case 39: nChoice = SPELL_MAGE_ARMOR; break;
    case 40: nChoice = -1;  break;
    case 41: nChoice = SPELL_MESTILS_ACID_SHEATH; break;
    case 42: nChoice = SPELL_MONSTROUS_REGENERATION; break;
    case 43: nChoice = SPELL_PRAYER;  break;
    case 44: nChoice = SPELL_PREMONITION; break;
    case 45: nChoice = SPELL_PROTECTION_FROM_ELEMENTS; break;
    case 46: nChoice = SPELL_PROTECTION_FROM_SPELLS; break;
    case 47: nChoice = SPELL_REGENERATE; break;
    case 48: nChoice = SPELL_RESIST_ELEMENTS;  break;
    case 49: nChoice = SPELL_SHADOW_SHIELD; break;
    case 50: nChoice = -1;  break;
    case 51: nChoice = SPELL_SHIELD; break;
    case 52: nChoice = SPELL_SPELL_MANTLE; break;
    case 53: nChoice = SPELL_SPELL_RESISTANCE; break;
    case 54: nChoice = SPELL_STONE_BONES; break;
    case 55: nChoice = SPELL_STONESKIN; break;
    case 56: nChoice = SPELL_TENSERS_TRANSFORMATION; break;
    case 57: nChoice = SPELL_TRUE_SEEING; break;
    case 58: nChoice = SPELL_DARKNESS;  break;
    case 59: nChoice = SPELL_WAR_CRY; break;
    case 60: nChoice = -1;    break;
    case 61: sType = "BARD_DEF"; break;
    case 62: sType = "BARD_OFF";   break;
    case 63: sType = "CLERIC_DEF"; break;
    case 64: sType = "CLERIC_OFF";  break;
    case 65: sType = "DRUID_DEF"; break;
    case 66: sType = "DRUID_OFF"; break;
    case 67: sType = "MAGE_DEF";  break;
    case 68: sType = "MAGE_OFF"; break;
    case 70: nChoice = -1;    break;
    case 71: sType = "ARMOR";    break;
    case 72: sType = "ELEMENTAL"; break;
    case 73: sType = "INVIS";  break;
    case 74: sType = "MELEE";  break;
    case 75: sType = "MIND";  break;
    case 76: sType = "SHIELD";  break;
    case 77: sType = "SP_PROT"; break;
    case 78: sType = "STEALTH"; break;

    case 81: DMFI_NextTarget(oTarget, oUser); nChoice = -1; break;
    case 82: SetLocalString(oUser, "dmfi_buff_level", "LOW"); nChoice = -1;
        FloatingTextStringOnCreature("Buff level LOW", oUser);
        SetCustomToken(20782, "Low");
        SetDMFIPersistentString("dmfi", "dmfi_buff_level", "LOW", oUser);
        break;
    case 83: SetLocalString(oUser, "dmfi_buff_level", "MID"); nChoice = -1;
        FloatingTextStringOnCreature("Buff level MID", oUser);
        SetCustomToken(20782, "Mid");
        SetDMFIPersistentString("dmfi", "dmfi_buff_level", "MID", oUser);
        break;
    case 84: SetLocalString(oUser, "dmfi_buff_level", "HIGH"); nChoice = -1;
        FloatingTextStringOnCreature("Buff level HIGH", oUser);
        SetCustomToken(20782, "High");
        SetDMFIPersistentString("dmfi", "dmfi_buff_level", "HIGH", oUser);
        break;
    case 85: SetLocalString(oUser, "dmfi_buff_level", "EPIC"); nChoice = -1;
        FloatingTextStringOnCreature("Buff level EPIC", oUser);
        SetCustomToken(20782, "Epic");
        SetDMFIPersistentString("dmfi", "dmfi_buff_level", "EPIC", oUser);
        break;
    case 86: {
            if (GetLocalInt(oUser, "dmfi_buff_party")==1)
            {
                SetLocalInt(oUser, "dmfi_buff_party", 0);
                FloatingTextStringOnCreature("Buff set to single target", oUser);
                SetCustomToken(20783, "Single Target");
                SetDMFIPersistentInt("dmfi","dmfi_buff_party", 0, oUser);
            }
            else
            {
                SetLocalInt(oUser, "dmfi_buff_party", 1);
                FloatingTextStringOnCreature("Buff set to party mode", oUser);
                SetCustomToken(20783, "Party");
                SetDMFIPersistentInt("dmfi","dmfi_buff_party", 1, oUser);
            }
        }
    case 80: nChoice = -1; break;
    default: nChoice = -1; break;
    }


    if (nChoice==-1)
        return;

//set caster level based on set level
    string sLevel = GetLocalString(oUser, "dmfi_buff_level");

    if (sLevel == "LOW") CL = 5;
    else if (sLevel == "MID") CL = 10;
    else if (sLevel == "HIGH") CL = 15;
    else if (sLevel == "EPIC") CL = 20;

    if (nChoice == 0)  //only get here if nChoice NOT set
    {
        string BUFF_TYPE = sType + "_" + sLevel;

        if (BUFF_TYPE == "BARD_DEF_LOW")
        {
            nSpell1 = SPELL_RESISTANCE;
            nSpell2 = SPELL_MAGE_ARMOR;
            nSpell3 = SPELL_GHOSTLY_VISAGE;
        }
        else if (BUFF_TYPE =="BARD_OFF_LOW")
        {
            nSpell1 = SPELL_BULLS_STRENGTH;
            nSpell2 = SPELL_MAGE_ARMOR;
            nSpell3 = SPELL_MAGIC_WEAPON;
        }
        else if (BUFF_TYPE == "BARD_DEF_MID")
        {
            nSpell1 = SPELL_IMPROVED_INVISIBILITY;
            nSpell2 = SPELL_GHOSTLY_VISAGE;
            nSpell3 = SPELL_CLARITY;
        }
        else if (BUFF_TYPE == "BARD_OFF_MID")
        {
            nSpell1 = SPELL_WAR_CRY;
            nSpell2 = SPELL_SUMMON_CREATURE_V;
            nSpell3 = SPELL_ETHEREAL_VISAGE;
        }
        else if (BUFF_TYPE == "BARD_DEF_HIGH")
        {
            nSpell1 = SPELL_ETHEREAL_VISAGE;
            nSpell2 = SPELL_IMPROVED_INVISIBILITY;
            nSpell3 = SPELL_HASTE;
        }
        else if (BUFF_TYPE == "BARD_OFF_HIGH")
        {
            nSpell1 = SPELL_ETHEREAL_VISAGE;
            nSpell2 = SPELL_SUMMON_CREATURE_V;
            nSpell3 = SPELL_WAR_CRY;
        }
        else if (BUFF_TYPE == "BARD_DEF_EPIC")
        {
            nSpell1 = SPELL_ETHEREAL_VISAGE;
            nSpell2 = SPELL_ENERGY_BUFFER;
            nSpell3 = SPELL_IMPROVED_INVISIBILITY;
        }
        else if (BUFF_TYPE == "BARD_OFF_EPIC")
        {
            nSpell1 = SPELL_ETHEREAL_VISAGE;
            nSpell2 = SPELL_SUMMON_CREATURE_VI;
            nSpell3 = SPELL_MASS_HASTE;
        }

        else if (BUFF_TYPE == "MAGE_DEF_LOW")
        {
            nSpell1 = SPELL_CLARITY;
            nSpell2 = SPELL_GHOSTLY_VISAGE;
            nSpell3 = SPELL_PROTECTION_FROM_ELEMENTS;
        }
        else if (BUFF_TYPE == "MAGE_OFF_LOW")
        {
            nSpell1 = SPELL_GHOSTLY_VISAGE;
            nSpell2 = SPELL_DEATH_ARMOR;
            nSpell3 = SPELL_HASTE;
        }
        else if (BUFF_TYPE == "MAGE_DEF_MID")
        {
            nSpell1 = SPELL_LESSER_SPELL_MANTLE;
            nSpell2 = SPELL_STONESKIN;
            nSpell3 = SPELL_ELEMENTAL_SHIELD;
        }
        else if (BUFF_TYPE == "MAGE_OFF_MID")
        {
            nSpell1 = SPELL_SPELL_MANTLE;
            nSpell2 = SPELL_IMPROVED_INVISIBILITY;
            nSpell3 = SPELL_SUMMON_CREATURE_V;
        }
        else if (BUFF_TYPE == "MAGE_DEF_HIGH")
        {
            nSpell1 = SPELL_SPELL_MANTLE;
            nSpell2 = SPELL_SANCTUARY;
            nSpell3 = SPELL_MINOR_GLOBE_OF_INVULNERABILITY;
        }
        else if (BUFF_TYPE == "MAGE_OFF_HIGH")
        {
            nSpell1 = SPELL_ETHEREAL_VISAGE;
            nSpell2 = SPELL_SUMMON_CREATURE_VIII;
            nSpell3 = SPELL_SPELL_MANTLE;
        }
        else if (BUFF_TYPE == "MAGE_DEF_EPIC")
        {
            nSpell1 = SPELL_PREMONITION;
            nSpell2 = SPELL_SPELL_MANTLE;
            nSpell3 = SPELL_GLOBE_OF_INVULNERABILITY;
        }
        else if (BUFF_TYPE == "MAGE_OFF_EPIC")
        {
            nSpell1 = SPELL_PREMONITION;
            nSpell2 = SPELL_MORDENKAINENS_SWORD;
            nSpell3 = SPELL_GLOBE_OF_INVULNERABILITY;
        }
        else if (BUFF_TYPE == "CLERIC_DEF_LOW")
        {
            nSpell1 = SPELL_PROTECTION_FROM_ELEMENTS;
            nSpell2 = SPELL_CLARITY;
            nSpell3 = SPELL_DARKVISION;
        }
        else if (BUFF_TYPE == "CLERIC_OFF_LOW")
        {
            nSpell1 = SPELL_PRAYER;
            nSpell2 = SPELL_MAGIC_VESTMENT;
            nSpell3 = SPELL_BULLS_STRENGTH;
        }
        else if (BUFF_TYPE == "CLERIC_MID_DEF")
        {
            nSpell1 = SPELL_SANCTUARY;
            nSpell2 = SPELL_SPELL_RESISTANCE;
            nSpell3 = SPELL_TRUE_SEEING;
        }
        else if (BUFF_TYPE == "CLERIC_OFF_MID")
        {
            nSpell1 = SPELL_SUMMON_CREATURE_VI;
            nSpell2 = SPELL_BATTLETIDE;
            nSpell3 = SPELL_MONSTROUS_REGENERATION;
        }
        else if (BUFF_TYPE == "CLERIC_DEF_HIGH")
        {
            nSpell1 = SPELL_SANCTUARY;
            nSpell2 = SPELL_REGENERATE;
            nSpell3 = SPELL_MONSTROUS_REGENERATION;
        }
        else if (BUFF_TYPE == "CLERIC_OFF_HIGH")
        {
            nSpell1 = SPELL_SUMMON_CREATURE_VIII;
            nSpell2 = SPELL_REGENERATE;
            nSpell3 = SPELL_BATTLETIDE;
        }
        else if (BUFF_TYPE == "CLERIC_DEF_EPIC")
        {
            nSpell1 = SPELL_UNDEATHS_ETERNAL_FOE;
            nSpell2 = SPELL_REGENERATE;
            nSpell3 = SPELL_SANCTUARY;
        }
        else if (BUFF_TYPE == "CLERIC_OFF_EPIC")
        {
            nSpell1 = SPELL_SUMMON_CREATURE_IX;
            nSpell2 = SPELL_REGENERATE;
            nSpell3 = SPELL_BATTLETIDE;
        }
        else if (BUFF_TYPE == "DRUID_DEF_LOW")
        {
            nSpell1 = SPELL_PROTECTION_FROM_ELEMENTS;
            nSpell2 = SPELL_BARKSKIN;
            nSpell3 = SPELL_ONE_WITH_THE_LAND;
        }
        else if (BUFF_TYPE == "DRUID_OFF_LOW")
        {
            nSpell1 = SPELL_GREATER_MAGIC_FANG;
            nSpell2 = SPELL_BULLS_STRENGTH;
            nSpell3 = SPELL_BLOOD_FRENZY;
        }
        else if (BUFF_TYPE == "DRUID_DEF_MID")
        {
            nSpell1 = SPELL_SPELL_RESISTANCE;
            nSpell2 = SPELL_MONSTROUS_REGENERATION;
            nSpell3 = SPELL_STONESKIN;
        }
        else if (BUFF_TYPE == "DRUID_OFF_MID")
        {
            nSpell1 = SPELL_STONESKIN;
            nSpell2 = SPELL_FREEDOM_OF_MOVEMENT;
            nSpell3 = SPELL_MASS_CAMOFLAGE;
        }
        else if (BUFF_TYPE == "DRUID_DEF_HIGH")
        {
            nSpell1 = SPELL_PREMONITION;
            nSpell2 = SPELL_TRUE_SEEING;
            nSpell3 = SPELL_GREATER_STONESKIN;
        }
        else if (BUFF_TYPE == "DRUID_OFF_HIGH")
        {
            nSpell1 = SPELL_SUMMON_CREATURE_VIII;
            nSpell2 = SPELL_AURA_OF_VITALITY;
            nSpell3 = SPELL_ENERGY_BUFFER;
        }
        else if (BUFF_TYPE == "DRUID_DEF_EPIC")
        {
            nSpell1 = SPELL_ELEMENTAL_SWARM;
            nSpell2 = SPELL_PREMONITION;
            nSpell3 = SPELL_TRUE_SEEING;
        }
        else if (BUFF_TYPE == "DRUID_OFF_EPIC")
        {
            nSpell1 = SPELL_PREMONITION;
            nSpell2 = SPELL_SHAPECHANGE;
            nSpell3 = SPELL_AURA_OF_VITALITY;
        }
        else if (BUFF_TYPE == "AMROR_LOW")
        {
            nSpell1 = SPELL_MAGE_ARMOR;
            nSpell2 = SPELL_INVISIBILITY_PURGE;
        }
        else if (BUFF_TYPE == "ARMOR_MID")
        {
            nSpell1 = SPELL_MAGE_ARMOR;
            nSpell2 = SPELL_DARKVISION;
            nSpell3 = SPELL_INVISIBILITY_PURGE;
        }
        else if (BUFF_TYPE == "ARMOR_HIGH")
        {
            nSpell1 = SPELL_MAGE_ARMOR;
            nSpell2 = SPELL_STONESKIN;
            nSpell3 = SPELL_GHOSTLY_VISAGE;
        }
        else if (BUFF_TYPE == "ARMOR_EPIC")
        {
            nSpell1 = SPELL_GHOSTLY_VISAGE;
            nSpell2 = SPELL_MAGE_ARMOR;
            nSpell3 = SPELL_PREMONITION;
        }
        else if (BUFF_TYPE == "ELEMENTAL_LOW")
        {
            nSpell1 = SPELL_RESISTANCE;
            nSpell2 = SPELL_ENDURE_ELEMENTS;
            nSpell3 = SPELL_ENDURANCE;
        }
        else if (BUFF_TYPE == "ELEMENTAL_MID")
        {
            nSpell1 = SPELL_RESISTANCE;
            nSpell2 = SPELL_RESIST_ELEMENTS;
            nSpell3 = SPELL_ENDURANCE;
        }
        else if (BUFF_TYPE == "ELEMENTAL_HIGH")
        {
            nSpell1 = SPELL_ENDURANCE;
            nSpell2 = SPELL_STONESKIN;
            nSpell3 = SPELL_PROTECTION_FROM_ELEMENTS;
        }
        else if (BUFF_TYPE == "ELEMENTAL_EPIC")
        {
            nSpell1 = SPELL_STONESKIN;
            nSpell2 = SPELL_ENERGY_BUFFER;
            nSpell3 = SPELL_ENDURANCE;
        }
        else if (BUFF_TYPE == "INVIS_LOW")
        {
            nSpell1 = SPELL_CATS_GRACE;
            nSpell2 = SPELL_INVISIBILITY;
        }
        else if (BUFF_TYPE == "INVIS_MID")
        {
            nSpell1 = SPELL_CATS_GRACE;
            nSpell2 = SPELL_MAGE_ARMOR;
            nSpell3 = SPELL_INVISIBILITY_SPHERE;
        }
        else if (BUFF_TYPE == "INVIS_HIGH")
        {
            nSpell1 = SPELL_MAGE_ARMOR;
            nSpell2 = SPELL_CATS_GRACE;
            nSpell3 = SPELL_IMPROVED_INVISIBILITY;
        }
        else if (BUFF_TYPE == "INVIS_EPIC")
        {
            nSpell1 = SPELL_MAGE_ARMOR;
            nSpell2 = SPELL_HASTE;
            nSpell3 = SPELL_SANCTUARY;
        }
        else if (BUFF_TYPE == "MELEE_LOW")
        {
            nSpell1 = SPELL_MAGIC_WEAPON;
            nSpell2 = SPELL_BULLS_STRENGTH;
            nSpell3 =  SPELL_STONE_BONES;
        }
        else if (BUFF_TYPE == "MELEE_MID")
        {
            nSpell1 = SPELL_BULLS_STRENGTH;
            nSpell2 = SPELL_STONESKIN;
            nSpell3 = SPELL_GREATER_MAGIC_WEAPON;
        }
        else if (BUFF_TYPE == "MELEE_HIGH")
        {
            nSpell1 = SPELL_ENDURANCE;
            nSpell2 = SPELL_GREATER_STONESKIN;
            nSpell3 = SPELL_KEEN_EDGE;
        }
        else if (BUFF_TYPE == "MELEE_EPIC")
        {
            nSpell1 = SPELL_TENSERS_TRANSFORMATION;
            nSpell2 = SPELL_PREMONITION;
            nSpell3 = SPELL_BULLS_STRENGTH;
        }
        else if (BUFF_TYPE == "MIND_LOW")
        {
            nSpell1 = SPELL_RESISTANCE;
            nSpell2 = SPELL_CLARITY;
        }
        else if (BUFF_TYPE == "MIND_MID")
        {
            nSpell1 = SPELL_RESISTANCE;
            nSpell2 = SPELL_OWLS_WISDOM;
            nSpell3 = SPELL_LESSER_MIND_BLANK;
        }
        else if (BUFF_TYPE == "MIND_HIGH")
        {
            nSpell1 = SPELL_OWLS_WISDOM;
            nSpell2 = SPELL_MAGE_ARMOR;
            nSpell3 = SPELL_LESSER_MIND_BLANK;
        }
        else if (BUFF_TYPE == "MIND_EPIC")
        {
            nSpell1 = SPELL_OWLS_WISDOM;
            nSpell2 = SPELL_LESSER_MIND_BLANK;
            nSpell3 = SPELL_HASTE;
        }
        else if (BUFF_TYPE == "SHIELD_LOW")
        {
            nSpell1 = SPELL_SHIELD;
            nSpell2 = SPELL_INVISIBILITY;
        }
        else if (BUFF_TYPE == "SHIELD_MID")
        {
            nSpell1 = SPELL_SHIELD;
            nSpell2 = SPELL_PRAYER;
            nSpell3 = SPELL_INVISIBILITY_SPHERE;
        }
        else if (BUFF_TYPE == "SHIELD_HIGH")
        {
            nSpell1 = SPELL_SHIELD;
            nSpell2 = SPELL_GHOSTLY_VISAGE;
            nSpell3 = SPELL_ELEMENTAL_SHIELD;
        }
        else if (BUFF_TYPE == "SHIELD_EPIC")
        {
            nSpell1 = SPELL_SHIELD;
            nSpell2 = SPELL_SHADOW_SHIELD;
            nSpell3 = SPELL_SPELL_MANTLE;
        }
        else if (BUFF_TYPE == "SP_PROT_LOW")
        {
            nSpell1 = SPELL_SHIELD;
            nSpell2 = SPELL_RESISTANCE;
            nSpell3 = SPELL_GHOSTLY_VISAGE;
        }
        else if (BUFF_TYPE == "SP_PROT_MID")
        {
            nSpell1 = SPELL_RESISTANCE;
            nSpell2 = SPELL_SHIELD;
            nSpell3 = SPELL_LESSER_SPELL_MANTLE;
        }
        else if (BUFF_TYPE == "SP_PROT_HIGH")
        {
            nSpell1 = SPELL_SHIELD;
            nSpell2 = SPELL_ETHEREAL_VISAGE;
            nSpell3 = SPELL_GLOBE_OF_INVULNERABILITY;
        }
        else if (BUFF_TYPE == "SP_PROT_EPIC")
        {
            nSpell1 = SPELL_PROTECTION_FROM_SPELLS;
            nSpell2 = SPELL_GREATER_SPELL_MANTLE;
            nSpell3 = SPELL_ETHEREAL_VISAGE;
        }
        else if (BUFF_TYPE == "STEALTH_LOW")
        {
            nSpell1 = SPELL_CATS_GRACE;
            nSpell2 = SPELL_CLAIRAUDIENCE_AND_CLAIRVOYANCE;
        }
        else if (BUFF_TYPE == "STEALTH_MID")
        {
            nSpell1 = SPELL_CATS_GRACE;
            nSpell2 = SPELL_CLAIRAUDIENCE_AND_CLAIRVOYANCE;
            nSpell3 = SPELL_DISPLACEMENT;
        }
        else if (BUFF_TYPE == "STEALTH_HIGH")
        {
            nSpell1 = SPELL_CLAIRAUDIENCE_AND_CLAIRVOYANCE;
            nSpell2 = SPELL_CATS_GRACE;
            nSpell3 = SPELL_IMPROVED_INVISIBILITY;
        }
        else if (BUFF_TYPE == "STEALTH_EPIC")
        {
            nSpell1 = SPELL_CATS_GRACE;
            nSpell2 = SPELL_ETHEREAL_VISAGE;
            nSpell3 = SPELL_IMPROVED_INVISIBILITY;
        }
    }
    else
    {
        nSpell1 = nChoice;   //set up the single buffs if they were initialized by the choice
    }

    string sParty = "target";
    if (Party==1)
    {
        sParty = "party";
        object oParty = GetFirstFactionMember(oTarget, FALSE);
        while (GetIsObjectValid(oParty))
        {
            AssignCommand(oTarget, ClearAllActions());
            if (nSpell1!=SPELL_ALL_SPELLS)
                AssignCommand(oTarget, ActionCastSpellAtObject(nSpell1, oTarget, METAMAGIC_ANY, TRUE, CL, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            if (nSpell2!=SPELL_ALL_SPELLS)
                AssignCommand(oTarget, ActionCastSpellAtObject(nSpell2, oTarget, METAMAGIC_ANY, TRUE, CL, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            if (nSpell3!=SPELL_ALL_SPELLS)
                AssignCommand(oTarget, ActionCastSpellAtObject(nSpell3, oTarget, METAMAGIC_ANY, TRUE, CL, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            oParty = GetNextFactionMember (oTarget);
        }
    }
    else
    {
        AssignCommand(oTarget, ClearAllActions());
        if (nSpell1!=SPELL_ALL_SPELLS)
            AssignCommand(oTarget, ActionCastSpellAtObject(nSpell1, oTarget, METAMAGIC_ANY, TRUE, CL, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
        if (nSpell2!=SPELL_ALL_SPELLS)
            AssignCommand(oTarget, ActionCastSpellAtObject(nSpell2, oTarget, METAMAGIC_ANY, TRUE, CL, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
        if (nSpell3!=SPELL_ALL_SPELLS)
            AssignCommand(oTarget, ActionCastSpellAtObject(nSpell3, oTarget, METAMAGIC_ANY, TRUE, CL, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
    }
    SendMessageToPC(oUser, "Buffs Applied to " + sParty + ".  Caster Level: " + IntToString(CL));
}

////////////////////////////////////////////////////////////////////////
void ToggleRestVariable(int iCurrent, int iChange, int iDefault, string sTextMessage = "", object oUser = OBJECT_INVALID, string sArea = "")
{
    string sOnOff = "ON";
    if (iCurrent & iChange) //If the variable already exists
    {
        if (iDefault)
        {
            sOnOff = "ON";
        }
        else
        {
            sOnOff = "OFF";
        } //Remove the variable
        SetDMFIPersistentInt("dmfi", "dmfi_r_" + sArea, iCurrent & ~iChange);
        if (sTextMessage != "")
            FloatingTextStringOnCreature(sTextMessage + sOnOff, oUser, FALSE);
    }
    else //if the variable doesn't already exist
    {
        if (iDefault)
        {
            sOnOff = "OFF";
        }
        else
        {
            sOnOff = "ON";
        } //Add the variable
        SetDMFIPersistentInt("dmfi", "dmfi_r_" + sArea, iCurrent | iChange);
        if (sTextMessage != "")
            FloatingTextStringOnCreature(sTextMessage + sOnOff, oUser, FALSE);
    }
}

////////////////////////////////////////////////////////////////////////
void DoRestFunction(int iRest, object oUser)
{
    int iCurrentMod = GetDMFIPersistentInt("dmfi", "dmfi_r_");
    object oTarget = GetLocalObject(oUser, "dmfi_univ_target");
    object oArea = GetArea(oUser);
    object oLoop;
    string sAreaTag = GetTag(oArea);
    int iCurrentArea = GetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag);

    switch (iRest)
    {
    //Rest All PCs in the area for DMs, Rest for PCs
    case 7:
        if (GetIsDM(oUser))
        {
            oLoop = GetFirstPC();
            while (GetIsObjectValid(oLoop))
            {
                if (oArea == GetArea(oLoop))
                    ForceRest(oLoop);
                oLoop = GetNextPC();
            }
        }
        else
        {
            SetLocalInt(oUser, "dmfi_r_bypass", TRUE); AssignCommand(oUser, ActionRest());
            SetLocalInt(oUser, "dmfi_r_init", TRUE);
            int iTime = GetTimeSecond() + GetTimeMinute() * 60 + GetTimeHour() * 3600 + GetCalendarDay() * 24 * 3600 + GetCalendarMonth() *3600 * 24 * 28 + GetCalendarYear() * 24 * 28 * 12 * 3600;
            SetLocalInt(oUser, "dmfi_r_startseconds", iTime);
            AssignCommand(oUser, ActionRest());
        } break;
    case 8:
        SetLocalString(oUser, "dmfi_univ_conv", "pc_emote");
        AssignCommand(oUser, ClearAllActions());
        AssignCommand(oUser, ActionStartConversation(OBJECT_SELF, "dmfi_universal", TRUE)); break;
    case 9:
        SetLocalString(oUser, "dmfi_univ_conv", "pc_dicebag");
        AssignCommand(oUser, ClearAllActions());
        AssignCommand(oUser, ActionStartConversation(OBJECT_SELF, "dmfi_universal", TRUE)); break;
    case 11: //Set Unlimited Rest (module): default is ON
        iCurrentMod = iCurrentMod & ~0x00000002; //Remove No Rest, if it exists
        //Toggle the current Unlimited Rest Variable
        ToggleRestVariable(iCurrentMod, 0x00000001, TRUE, "GLOBAL: Unlimited Rest is ", oUser); break;
    case 12: //Set No Rest (module): default is OFF
        iCurrentMod = iCurrentMod | 0x00000001; //Remove Unlimited Rest, if it exists
        //Toggle the current No Rest Variable
        ToggleRestVariable(iCurrentMod, 0x00000002, FALSE, "GLOBAL: No Rest is ", oUser); break;
    case 13: //Limit Rest by Time: default is OFF
        ToggleRestVariable(iCurrentMod, 0x00000004, FALSE, "GLOBAL: Limited Rest - Time is ", oUser); break;
    case 14: //Limit Rest by Placeables: default is OFF
        ToggleRestVariable(iCurrentMod, 0x00000008, FALSE, "GLOBAL: Limited Rest - Placeables is ", oUser); break;
    case 15: //Limit Rest by Armor: default is OFF
        ToggleRestVariable(iCurrentMod, 0x00000010, FALSE, "GLOBAL: Limited Rest - Armor is ", oUser); break;
    case 16: //Limit Hit Points healed from resting: default is OFF
        ToggleRestVariable(iCurrentMod, 0x00000020, FALSE, "GLOBAL: Limit Hit Points is ", oUser); break;
    case 17: //Allow spell memorization: default is ON
        ToggleRestVariable(iCurrentMod, 0x00000040, TRUE, "GLOBAL: Spell Memorization is ", oUser); break;
    case 21: //Set Unlimited Rest (Local)
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod & ~0x00000002; //Remove No Rest, if it exists
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000001, TRUE, "LOCAL: Unlimited Rest is ", oUser, sAreaTag);
        break;
    case 22: //Set No Rest (module)
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod | 0x00000001; //Remove Unlimited Rest, if it exists
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000002, FALSE, "LOCAL: No Rest is ", oUser, sAreaTag);
        break;
    case 23: //Limit Rest by Time: default is OFF
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000004, FALSE, "LOCAL: Limited Rest - Time is ", oUser, sAreaTag);
        break;
    case 24: //Limit Rest by Placeables: default is OFF
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000008, FALSE, "LOCAL: Limited Rest - Placeables is ", oUser, sAreaTag);
        break;
    case 25: //Limit Rest by Armor: default is OFF
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000010, FALSE, "LOCAL: Limited Rest - Armor is ", oUser, sAreaTag);
        break;
    case 26: //Limit Hit Points healed from resting: default is OFF
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000020, FALSE, "LOCAL: Limit Hit Points is ", oUser, sAreaTag);
        break;
    case 27: //Allow spell memorization: default is ON
        if (iCurrentArea & 0x00000080)
            iCurrentMod = iCurrentArea;
        iCurrentMod = iCurrentMod | 0x00000080; //Add Area Override bitflag
        ToggleRestVariable(iCurrentMod, 0x00000040, FALSE, "LOCAL: Spell Restriction is ", oUser, sAreaTag);
        break;
    case 28: //Reset area to module defaults
        FloatingTextStringOnCreature("Area set to module defaults", oUser, FALSE);
        SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, 0x00000000);
        break;
    case 31: //Set Time Limit to 1 game hour per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 1 game hour per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000100);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 1 game hour per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000100);
        } break;
    case 32: //Set Time Limit to 2 game hours per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 2 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000200);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 2 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000200);
        } break;
    case 33: //Set Time Limit to 4 game hours per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 4 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000300);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 4 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000300);
        } break;
    case 34: //Set Time Limit to 6 game hours per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 6 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000400);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 6 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000400);
        } break;
    case 35: //Set Time Limit to 8 game hours per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 8 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000500);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 8 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000500);
        }    break;
    case 36: //Set Time Limit to 12 game hours per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 12 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000600);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 12 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000600);
        }   break;
    case 37: //Set Time Limit to 24 game hours per day
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Time Limit set to 24 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00000700);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00000f00; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Time Limit set to 24 game hours per rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00000700);
        }
    case 41: //Toggle placeable flag: DMFI Placeables (tag = dmfi_rest), by default ON
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00001000, TRUE, "LOCAL: DMFI Placeables are ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00001000, TRUE, "GLOBAL: DMFI Placeables are ", oUser);
        break;
    case 42: //Toggle placeable flag: Campfires
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00002000, FALSE, "LOCAL: Campfire Placeables are ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00002000, FALSE, "GLOBAL: Campfire Placeables are ", oUser);
        break;
    case 43: //Toggle placeable flag: Bed Rolls
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00004000, FALSE, "LOCAL: Bed Roll Placeables are ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00004000, FALSE, "GLOBAL: Bed Roll Placeables are ", oUser);
        break;
    case 44: //Toggle placeable flag: Beds
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00008000, FALSE, "LOCAL: Bed Placeables are ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00008000, FALSE, "GLOBAL: Bed Placeables are ", oUser);
        break;
    case 45: //Toggle placeable flag: Tents
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00010000, FALSE, "LOCAL: Tent Placeables are ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00010000, FALSE, "GLOBAL: Tent Placeables are ", oUser);
        break;
    case 46: //Toggle placeable flag: Ignore Druids
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00020000, FALSE, "LOCAL: Ignore Druids for Placeable Checks is ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00020000, FALSE, "GLOBAL: Ignore Druids for Placeable Checks is ", oUser);
        break;
    case 47: //Toggle placeable flag: Ignore Rangers
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00040000, FALSE, "LOCAL: Ignore Rangers for Placeable Checks is ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00040000, FALSE, "GLOBAL: Ignore Rangers for Placeable Checks is ", oUser);
        break;
    case 48: //Toggle placeable flag: Ignore Barbarians
        if (iCurrentArea & 0x00000080)
            ToggleRestVariable(iCurrentArea, 0x00080000, FALSE, "LOCAL: Ignore Barbarians for Placeable Checks is ", oUser, sAreaTag);
        else
            ToggleRestVariable(iCurrentMod, 0x00080000, FALSE, "GLOBAL: Ignore Barbarians for Placeable Checks is ", oUser);
        break;
    case 51: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 2 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00100000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 2 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00100000);
        }    break;
    case 52: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 6 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00200000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 6 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00200000);
        }    break;
    case 53: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 11 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00300000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 11 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00300000);
        } break;
    case 54: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 16 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00400000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 16 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00400000);
        } break;
    case 55: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 31 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00500000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 31 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00500000);
        }    break;
    case 56: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 41 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00600000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 41 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00600000);
        } break;
    case 57: //Set Armor Weight Restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: Armor Restriction set to 46 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x00700000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x00f00000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: Armor Restriction set to 46 pounds", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x00700000);
        } break;
    case 61: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: No hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x01000000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: No hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x01000000);
        }    break;
    case 62: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: 1 hitpoint/level regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x02000000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: 1 hitpoint/level regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x02000000);
        }    break;
    case 63: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: (CON) hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x03000000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: (CON) hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x03000000);
        }    break;
    case 64: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: 10 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x04000000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: 10 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x04000000);
        } break;
    case 65: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: 25 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x05000000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: 25 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x05000000);
        } break;
    case 66: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: 50 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea | 0x06000000);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: 50 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod | 0x06000000);
        } break;
    case 67: //Set Hit Point restrictions
        if (iCurrentArea & 0x00000080)
        {
            iCurrentArea = iCurrentArea & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("LOCAL: 100 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_" + sAreaTag, iCurrentArea);
        }
        else
        {
            iCurrentMod = iCurrentMod & ~0x0f000000; //Erase current setting
            FloatingTextStringOnCreature("GLOBAL: 100 percent of hitpoints regained on rest", oUser, FALSE);
            SetDMFIPersistentInt("dmfi", "dmfi_r_", iCurrentMod);
        } break;
    case 101: //Use Rest Conversation Toggle
        if (GetIsDM(oUser))
            ToggleRestVariable(iCurrentMod, 0x10000000, TRUE, "GLOBAL: Rest Conversation is ", oUser);
        else
        {
            SetLocalInt(oUser, "dmfi_r_alternate", ANIMATION_LOOPING_MEDITATE); SetLocalInt(oUser, "dmfi_r_bypass", TRUE); AssignCommand(oUser, ActionRest());
        } break;
    case 102: //Use Rest VFX
        if (GetIsDM(oUser))
            ToggleRestVariable(iCurrentMod, 0x20000000, TRUE, "GLOBAL: Rest VFX are ", oUser);
        else
        {
            SetLocalInt(oUser, "dmfi_r_alternate", ANIMATION_LOOPING_DEAD_FRONT); SetLocalInt(oUser, "dmfi_r_bypass", TRUE); AssignCommand(oUser, ActionRest());
        } break;
    case 103: //Floating Text Feedback
        if (GetIsDM(oUser))
            ToggleRestVariable(iCurrentMod, 0x40000000, TRUE, "GLOBAL: Floating Text Feedback is ", oUser);
        else
        {
            SetLocalInt(oUser, "dmfi_r_alternate", ANIMATION_LOOPING_DEAD_BACK); SetLocalInt(oUser, "dmfi_r_bypass", TRUE); AssignCommand(oUser, ActionRest());
        } break;
    case 104: //Immobilized Resting
        if (GetIsDM(oUser))
            ToggleRestVariable(iCurrentMod, 0x80000000, TRUE, "GLOBAL: Immobile resting is ", oUser);
        else
        {
            SetLocalInt(oUser, "dmfi_r_alternate", ANIMATION_LOOPING_WORSHIP); SetLocalInt(oUser, "dmfi_r_bypass", TRUE); AssignCommand(oUser, ActionRest());
        } break;
    case 108: //All PCs in Area are Rested
        break;
    case 109: //All PCs are Rested
        oLoop = GetFirstPC();
        while (GetIsObjectValid(oLoop))
        {
            ForceRest(oLoop);
            oLoop = GetNextPC();
        }
        break;
    }
}

////////////////////////////////////////////////////////////////////////
void main()
{
    string sDMFI = GetLocalString(OBJECT_SELF, "dmfi_univ_conv");
    int iDMFI = GetLocalInt(OBJECT_SELF, "dmfi_univ_int");
    location lDMFI = GetLocalLocation(OBJECT_SELF, "dmfi_univ_location");
    if (sDMFI == "emote" || sDMFI == "pc_emote")
        DoEmoteFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "fx")
        CreateEffects(iDMFI, lDMFI, OBJECT_SELF);
    else if (sDMFI == "encounter")
        CreateEncounter(iDMFI, lDMFI, OBJECT_SELF);
    else if (sDMFI == "music")
        DoMusicFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "xp")
        DoXPFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "server")
        dmwand_DoDialogChoice(iDMFI);
    else if (sDMFI == "afflict")
        DoAfflictFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "voice")
        DoVoiceFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "sound")
        DoSoundFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "onering")
        DoOneRingFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "dicebag")
        DoDMDiceBagFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "pc_dicebag")
        DoDiceBagFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "faction")
        DoControlFunction(iDMFI, OBJECT_SELF);
    else if (sDMFI == "dmw")
        DoNewDMThingy(iDMFI, OBJECT_SELF);
    else if (sDMFI == "buff")
        DoBuff(iDMFI, OBJECT_SELF);
    else if (sDMFI == "rest")
        DoRestFunction(iDMFI, OBJECT_SELF);

    DeleteLocalInt(OBJECT_SELF,"Tens");
}

