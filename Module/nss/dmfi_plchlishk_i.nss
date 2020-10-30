//::///////////////////////////////////////////////
//:: DMFI - internal player chat listener hooking include
//:: dmfi_plchlishk_i
//:://////////////////////////////////////////////
/*
  Include file containing routines for managing the internal DMFI chain of
  "listeners", which are now implemented as OnPlayerChat event handlers rather
  than creatures.
*/
//:://////////////////////////////////////////////
//:: Created By: tsunami282
//:: Created On: 2008.03.24
//:://////////////////////////////////////////////

const int DMFI_LISTEN_ON_CHANNEL_TALK = 1;
const int DMFI_LISTEN_ON_CHANNEL_WHISPER = 1;
const int DMFI_LISTEN_ON_CHANNEL_SHOUT = 1;
const int DMFI_LISTEN_ON_CHANNEL_DM = 1;
const int DMFI_LISTEN_ON_CHANNEL_PARTY = 1;

const string DMFI_EAVESDROP_HOOK_VARNAME = "dmfi_Eavesdrop";

const float WHISPER_DISTANCE = 1.0;
const float TALK_DISTANCE = 30.0;

string sHookTypeVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_Type"; // 1=PC (says), 2=NPC/location (hears)
string sHookCreatureVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_Creature"; // must be valid for type 1, for type 2 object_invalid means location only
string sHookRangeModeVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_RangeMode"; // listening range: for type 1, 0=pc only, 1=pc's party; for type 2, 0=earshot, 1=area, 2=module
string sHookLocationVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_Location"; // for type 2, location of "listening post"
string sHookChannelsVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_Channels"; // bitmask of TALKVOLUME channels to listen to
string sHookOwnerVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_Owner"; // unique ID of owner of this hook (he who will get the captured text)
string sHookBcastDMsVarname = DMFI_EAVESDROP_HOOK_VARNAME + "_BcastDMs"; // 0=relay message to owner only, 1=broadcast to all DMs

////////////////////////////////////////////////////////////////////////
void RemoveListenerHook(int hooknum)
{
    int hooktype;
    object hookcreature;
    location hooklocation;
    int hookchannels;
    object hookowner;
    int hookparty, hookbcast;

    int iHook = hooknum;
    string siHook = "", siHookN = "";
    object oMod = GetModule();

    while (1)
    {
        siHook = IntToString(iHook);
        siHookN = IntToString(iHook+1);

        hooktype = GetLocalInt(oMod, sHookTypeVarname+siHookN);
        if (hooktype != 0)
        {
            hookcreature = GetLocalObject(oMod, sHookCreatureVarname+siHookN);
            hooklocation = GetLocalLocation(oMod, sHookLocationVarname+siHookN);
            hookchannels = GetLocalInt(oMod, sHookChannelsVarname+siHookN);
            hookowner = GetLocalObject(oMod, sHookOwnerVarname+siHookN);
            hookparty = GetLocalInt(oMod, sHookRangeModeVarname+siHookN);
            hookbcast = GetLocalInt(oMod, sHookBcastDMsVarname+siHookN);

            SetLocalInt(oMod, sHookTypeVarname+siHook, hooktype);
            SetLocalObject(oMod, sHookCreatureVarname+siHook, hookcreature);
            SetLocalLocation(oMod, sHookLocationVarname+siHook, hooklocation);
            SetLocalInt(oMod, sHookChannelsVarname+siHook, hookchannels);
            SetLocalObject(oMod, sHookOwnerVarname+siHook, hookowner);
            SetLocalInt(oMod, sHookRangeModeVarname+siHook, hookparty);
            SetLocalInt(oMod, sHookBcastDMsVarname+siHook, hookbcast);
        }
        else
        {
            DeleteLocalInt(oMod, sHookTypeVarname+siHook);
            DeleteLocalObject(oMod, sHookCreatureVarname+siHook);
            DeleteLocalLocation(oMod, sHookLocationVarname+siHook);
            DeleteLocalInt(oMod, sHookChannelsVarname+siHook);
            DeleteLocalObject(oMod, sHookOwnerVarname+siHook);
            DeleteLocalInt(oMod, sHookRangeModeVarname+siHook);
            DeleteLocalInt(oMod, sHookBcastDMsVarname+siHook);

            break;
        }
        iHook++;
    }
}

////////////////////////////////////////////////////////////////////////
int AppendListenerHook(int hooktype, object hookcreature, location hooklocation,
        int hookchannels, int hookparty, int hookbcast, object hookowner)
{
    int iHook = 0;

    if (hooktype != 0)
    {
        int iHookType;
        string siHook = "";
        object oMod = GetModule();
        iHook = 1;
        while (1)
        {
            siHook = IntToString(iHook);
            iHookType = GetLocalInt(oMod, sHookTypeVarname+siHook);
            if (iHookType == 0) break; // end of list
            iHook++;
        }
        SetLocalInt(oMod, sHookTypeVarname+siHook, hooktype);
        SetLocalObject(oMod, sHookCreatureVarname+siHook, hookcreature);
        SetLocalLocation(oMod, sHookLocationVarname+siHook, hooklocation);
        SetLocalInt(oMod, sHookChannelsVarname+siHook, hookchannels);
        SetLocalObject(oMod, sHookOwnerVarname+siHook, hookowner);
        SetLocalInt(oMod, sHookRangeModeVarname+siHook, hookparty);
        SetLocalInt(oMod, sHookBcastDMsVarname+siHook, hookbcast);
    }

    return iHook;
}

