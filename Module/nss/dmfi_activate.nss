//::///////////////////////////////////////////////
//:: DMFI - widget activation processor
//:: dmfi_activate
//:://////////////////////////////////////////////
/*
  Functions to respond and process DMFI item activations.
*/
//:://////////////////////////////////////////////
//:: Created By: The DMFI Team
//:: Created On:
//:://////////////////////////////////////////////
//:: 2008.05.25 tsunami282 - changes to invisible listeners to work with
//::                         OnPlayerChat methods.
//:: 2008.07.10 tsunami282 - add Naming Wand to the exploder.
//:: 2008.08.15 tsunami282 - move init logic to new include.

#include "dmfi_init_inc"

////////////////////////////////////////////////////////////////////////
void dmw_CleanUp(object oMySpeaker)
{
   int nCount;
   int nCache;
   DeleteLocalObject(oMySpeaker, "dmfi_univ_target");
   DeleteLocalLocation(oMySpeaker, "dmfi_univ_location");
   DeleteLocalObject(oMySpeaker, "dmw_item");
   DeleteLocalString(oMySpeaker, "dmw_repamt");
   DeleteLocalString(oMySpeaker, "dmw_repargs");
   nCache = GetLocalInt(oMySpeaker, "dmw_playercache");
   for(nCount = 1; nCount <= nCache; nCount++)
   {
      DeleteLocalObject(oMySpeaker, "dmw_playercache" + IntToString(nCount));
   }
   DeleteLocalInt(oMySpeaker, "dmw_playercache");
   nCache = GetLocalInt(oMySpeaker, "dmw_itemcache");
   for(nCount = 1; nCount <= nCache; nCount++)
   {
      DeleteLocalObject(oMySpeaker, "dmw_itemcache" + IntToString(nCount));
   }
   DeleteLocalInt(oMySpeaker, "dmw_itemcache");
   for(nCount = 1; nCount <= 10; nCount++)
   {
      DeleteLocalString(oMySpeaker, "dmw_dialog" + IntToString(nCount));
      DeleteLocalString(oMySpeaker, "dmw_function" + IntToString(nCount));
      DeleteLocalString(oMySpeaker, "dmw_params" + IntToString(nCount));
   }
   DeleteLocalString(oMySpeaker, "dmw_playerfunc");
   DeleteLocalInt(oMySpeaker, "dmw_started");
}

////////////////////////////////////////////////////////////////////////
void main()
{
    object oUser = OBJECT_SELF;
    object oItem = GetLocalObject(oUser, "dmfi_item");
    object oOther = GetLocalObject(oUser, "dmfi_target");
    location lLocation = GetLocalLocation(oUser, "dmfi_location");
    string sItemTag = GetTag(oItem);

    // listening system initialization moved to new function
    dmfiInitialize(oUser);

    dmw_CleanUp(oUser);

    if (GetStringLeft(sItemTag,8) == "hlslang_")
    {
        // Remove voice stuff
        string ssLanguage = GetStringRight(sItemTag, GetStringLength(sItemTag) - 8);
        SetLocalInt(oUser, "hls_MyLanguage", StringToInt(ssLanguage));
        SetLocalString(oUser, "hls_MyLanguageName", GetName(oItem));
        DelayCommand(1.0f, FloatingTextStringOnCreature("You are speaking " + GetName(oItem) + ". Type [(what you want to say in brackets)]", oUser, FALSE));
        return;
    }

    if (GetStringLeft(sItemTag, 8) == "dmfi_pc_")
    {
        if (GetStringLeft(sItemTag, 12) == "dmfi_pc_rest")
        {
            CreateObject(OBJECT_TYPE_PLACEABLE, "dmfi_rest" + GetStringRight(sItemTag, 3), GetLocation(oUser));
            return;
        }
        if (sItemTag == "dmfi_pc_follow")
        {
            if (GetIsObjectValid(oOther))
            {
                FloatingTextStringOnCreature("Now following "+ GetName(oOther),oUser, FALSE);
                DelayCommand(2.0f, AssignCommand(oUser, ActionForceFollowObject(oOther, 2.0f)));
            }
            return;
        }
        SetLocalObject(oUser, "dmfi_univ_target", oUser);
        SetLocalLocation(oUser, "dmfi_univ_location", lLocation);
        SetLocalString(oUser, "dmfi_univ_conv", GetStringRight(sItemTag, GetStringLength(sItemTag) - 5));
        AssignCommand(oUser, ClearAllActions());
        AssignCommand(oUser, ActionStartConversation(OBJECT_SELF, "dmfi_universal", TRUE));
        return;
    }

    if (GetStringLeft(sItemTag, 5) == "dmfi_")
    {
        int iPass = FALSE;

        if (GetIsDM(oUser) || GetIsDMPossessed(oUser))
            iPass = TRUE;

        if (!GetIsPC(oUser))
            iPass = TRUE;

        if (!iPass)
        {
            FloatingTextStringOnCreature("You cannot use this item." ,oUser, FALSE);
            SendMessageToAllDMs(GetName(oUser)+ " is attempting to use a DM item.");
            return;
        }

        if (sItemTag == "dmfi_exploder")
        {
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_afflict"))) CreateItemOnObject("dmfi_afflict", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_dicebag"))) CreateItemOnObject("dmfi_dicebag", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_pc_dicebag"))) CreateItemOnObject("dmfi_pc_dicebag", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_pc_follow"))) CreateItemOnObject("dmfi_pc_follow", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_pc_emote"))) CreateItemOnObject("dmfi_pc_emote", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_server"))) CreateItemOnObject("dmfi_server", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_emote"))) CreateItemOnObject("dmfi_emote", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_encounter"))) CreateItemOnObject("dmfi_encounte", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_faction"))) CreateItemOnObject("dmfi_faction", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_fx"))) CreateItemOnObject("dmfi_fx", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_music"))) CreateItemOnObject("dmfi_music", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_sound"))) CreateItemOnObject("dmfi_sound", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_voice"))) CreateItemOnObject("dmfi_voice", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_xp"))) CreateItemOnObject("dmfi_xp", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_500xp"))) CreateItemOnObject("dmfi_500xp", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_en_ditto"))) CreateItemOnObject("dmfi_en_ditto", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_mute"))) CreateItemOnObject("dmfi_mute", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_peace"))) CreateItemOnObject("dmfi_peace", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_voicewidget"))) CreateItemOnObject("dmfi_voicewidget", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_remove"))) CreateItemOnObject("dmfi_remove", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_dmw"))) CreateItemOnObject("dmfi_dmw", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_target"))) CreateItemOnObject("dmfi_target", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_buff"))) CreateItemOnObject("dmfi_buff", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_dmbook"))) CreateItemOnObject("dmfi_dmbook", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_playerbook"))) CreateItemOnObject("dmfi_playerbook", oOther);
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_jail_widget"))) CreateItemOnObject("dmfi_jail_widget", oOther);
            // 2008.07.10 tsunami282 - add naming wand to the exploder
            if(!GetIsObjectValid(GetItemPossessedBy(oOther, "dmfi_naming"))) CreateItemOnObject("dmfi_naming", oOther);
            return;
        }
        if (sItemTag == "dmfi_peace")
        {   //This widget sets all creatures in the area to a neutral stance and clears combat.
            object oArea = GetFirstObjectInArea(GetArea(oUser));
            object oP;
            while (GetIsObjectValid(oArea))
            {
                if (GetObjectType(oArea) == OBJECT_TYPE_CREATURE && !GetIsPC(oArea))
                {
                    AssignCommand(oArea, ClearAllActions());
                    oP = GetFirstPC();
                    while (GetIsObjectValid(oP))
                    {
                        if (GetArea(oP) == GetArea(oUser))
                        {
                            ClearPersonalReputation(oArea, oP);
                            SetStandardFactionReputation(STANDARD_FACTION_HOSTILE, 25, oP);
                            SetStandardFactionReputation(STANDARD_FACTION_COMMONER, 91, oP);
                            SetStandardFactionReputation(STANDARD_FACTION_MERCHANT, 91, oP);
                            SetStandardFactionReputation(STANDARD_FACTION_DEFENDER, 91, oP);
                        }
                        oP = GetNextPC();
                    }
                    AssignCommand(oArea, ClearAllActions());
                }
                oArea = GetNextObjectInArea(GetArea(oUser));
            }
        }

        // update / remove invisible listeners as needed for onplayerchat
        if (sItemTag == "dmfi_voicewidget")
        {
            object oVoice;
            if (GetIsObjectValid(oOther)) // do we have a valid target creature?
            {
                // 2008.05.29 tsunami282 - we don't use creature listen stuff anymore
                SetLocalObject(oUser, "dmfi_VoiceTarget", oOther);

                FloatingTextStringOnCreature("You have targeted " + GetName(oOther) + " with the Voice Widget", oUser, FALSE);

                if (GetLocalInt(GetModule(), "dmfi_voice_initial")!=1)
                {
                    SetLocalInt(GetModule(), "dmfi_voice_initial", 1);
                    SendMessageToAllDMs("Listening Initialized:  .commands, .skill checks, and much more now available.");
                    DelayCommand(4.0, FloatingTextStringOnCreature("Listening Initialized:  .commands, .skill checks, and more available", oUser));
                }
                return;
            }
            else // no valid target of voice wand
            {
                //Jump any existing Voice attached to the user
                if (GetIsObjectValid(GetLocalObject(oUser, "dmfi_StaticVoice")))
                {
                    DestroyObject(GetLocalObject(oUser, "dmfi_StaticVoice"));
                }
                //Create the StationaryVoice
                object oStaticVoice = CreateObject(OBJECT_TYPE_CREATURE, "dmfi_voice", GetLocation(oUser));
                //Set Ownership of the Voice to the User
                SetLocalObject(oUser, "dmfi_StaticVoice", oVoice);
                SetLocalObject(oUser, "dmfi_VoiceTarget", oStaticVoice);
                DelayCommand(1.0f, FloatingTextStringOnCreature("A Stationary Voice has been created.", oUser, FALSE));
                return;
            }
            return;
        }
        if (sItemTag == "dmfi_mute")
        {
            SetLocalObject(oUser, "dmfi_univ_target", oUser);
            SetLocalString(oUser, "dmfi_univ_conv", "voice");
            SetLocalInt(oUser, "dmfi_univ_int", 8);
            ExecuteScript("dmfi_execute", oUser);
            return;
        }
        //encounter ditto widget
        if (sItemTag == "dmfi_en_ditto")
        {
            SetLocalObject(oUser, "dmfi_univ_target", oOther);
            SetLocalLocation(oUser, "dmfi_univ_location", lLocation);
            SetLocalString(oUser, "dmfi_univ_conv", "encounter");
            SetLocalInt(oUser, "dmfi_univ_int", GetLocalInt(oUser, "EncounterType"));
            ExecuteScript("dmfi_execute", oUser);
            return;
        }
        //Change target widget
        if (sItemTag == "dmfi_target")
        {
            SetLocalObject(oUser, "dmfi_univ_target", oOther);
            FloatingTextStringOnCreature("DMFI Target set to " + GetName(oOther),oUser);
        }
        //Destroy object widget
        if (sItemTag == "dmfi_remove")
        {
            object oKillMe;
            //Targeting Self
            if (oUser == oOther)
            {
                oKillMe = GetNearestObject(OBJECT_TYPE_PLACEABLE, oUser);
                FloatingTextStringOnCreature("Destroyed " + GetName(oKillMe) + "(" + GetTag(oKillMe) + ")", oUser, FALSE);
                DelayCommand(0.1f, DestroyObject(oKillMe));
            }
            else if (GetIsObjectValid(oOther)) //Targeting something else
            {
                FloatingTextStringOnCreature("Destroyed " + GetName(oOther) + "(" + GetTag(oOther) + ")", oUser, FALSE);
                DelayCommand(0.1f, DestroyObject(oOther));
            }
            else //Targeting the ground
            {
                int iReport = 0;
                oKillMe = GetFirstObjectInShape(SHAPE_SPHERE, 2.0f, lLocation, FALSE, OBJECT_TYPE_ALL);
                while (GetIsObjectValid(oKillMe))
                {
                    iReport++;
                    DestroyObject(oKillMe);
                    oKillMe = GetNextObjectInShape(SHAPE_SPHERE, 2.0f, lLocation, FALSE, OBJECT_TYPE_ALL);
                }
                FloatingTextStringOnCreature("Destroyed " + IntToString(iReport) + " objects.", oUser, FALSE);
            }
            return;
        }
        if (sItemTag == "dmfi_500xp")
        {
            SetLocalObject(oUser, "dmfi_univ_target", oOther);
            SetLocalLocation(oUser, "dmfi_univ_location", lLocation);
            SetLocalString(oUser, "dmfi_univ_conv", "xp");
            SetLocalInt(oUser, "dmfi_univ_int", 53);
            ExecuteScript("dmfi_execute", oUser);
            return;
        }
        if (sItemTag == "dmfi_jail_widget")
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
            return;
        }

        if (sItemTag == "dmfi_encounter")
        {

            if (GetIsObjectValid(GetWaypointByTag("DMFI_E1")))
                SetCustomToken(20771, GetName(GetWaypointByTag("DMFI_E1")));
            else
                SetCustomToken(20771, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E2")))
                SetCustomToken(20772, GetName(GetWaypointByTag("DMFI_E2")));
            else
                SetCustomToken(20772, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E3")))
                SetCustomToken(20773, GetName(GetWaypointByTag("DMFI_E3")));
            else
                SetCustomToken(20773, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E4")))
                SetCustomToken(20774, GetName(GetWaypointByTag("DMFI_E4")));
            else
                SetCustomToken(20774, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E5")))
                SetCustomToken(20775, GetName(GetWaypointByTag("DMFI_E5")));
            else
                SetCustomToken(20775, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E6")))
                SetCustomToken(20776, GetName(GetWaypointByTag("DMFI_E6")));
            else
                SetCustomToken(20776, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E7")))
                SetCustomToken(20777, GetName(GetWaypointByTag("DMFI_E7")));
            else
                SetCustomToken(20777, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E8")))
                SetCustomToken(20778, GetName(GetWaypointByTag("DMFI_E8")));
            else
                SetCustomToken(20778, "Encounter Invalid");
            if (GetIsObjectValid(GetWaypointByTag("DMFI_E9")))
                SetCustomToken(20779, GetName(GetWaypointByTag("DMFI_E9")));
            else
                SetCustomToken(20779, "Encounter Invalid");
        }
        if (sItemTag == "dmfi_afflict")
        {
            int nDNum;

            nDNum = GetLocalInt(oUser, "dmfi_damagemodifier");
            SetCustomToken(20780, IntToString(nDNum));
        }


        SetLocalObject(oUser, "dmfi_univ_target", oOther);
        SetLocalLocation(oUser, "dmfi_univ_location", lLocation);
        SetLocalString(oUser, "dmfi_univ_conv", GetStringRight(sItemTag, GetStringLength(sItemTag) - 5));
        AssignCommand(oUser, ClearAllActions());
        AssignCommand(oUser, ActionStartConversation(OBJECT_SELF, "dmfi_universal", TRUE, FALSE));
    }
}

