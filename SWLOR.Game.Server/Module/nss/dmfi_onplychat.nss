//::///////////////////////////////////////////////
//:: DMFI - OnPlayerChat event handler
//:: dmfi_onplychat
//:://////////////////////////////////////////////
/*
  Event handler for the module-level OnPlayerChat event. Manages scripter-added
  event scripts.
*/
//:://////////////////////////////////////////////
//:: Created By: Merle, with help from mykael22000 and tsunami282
//:: Created On: 2007.12.12
//:://////////////////////////////////////////////
//:: 2007.12.27 tsunami282 - implemented hooking tree

#include "dmfi_plychat_inc"

const string DMFI_PLAYERCHAT_SCRIPTNAME = "dmfi_plychat_exe";

////////////////////////////////////////////////////////////////////////
void main()
{
    int nVolume = GetPCChatVolume();
    object oShouter = GetPCChatSpeaker();

    int bInvoke;
    string sChatHandlerScript;
    int maskChannels;
    // int bListenAll;
    object oRunner;
    int bAutoRemove;
    int bDirtyList = FALSE;
    int iHook;
    object oMod = GetModule();
// SendMessageToPC(GetFirstPC(), "OnPlayerChat - process hooks");
    int nHooks = GetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME);
    for (iHook = nHooks; iHook > 0; iHook--) // reverse-order execution, last hook gets first dibs
    {
// SendMessageToPC(GetFirstPC(), "OnPlayerChat -- process hook #" + IntToString(iHook));
        maskChannels = GetLocalArrayInt(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, iHook);
// SendMessageToPC(GetFirstPC(), "OnPlayerChat -- channel heard=" + IntToString(nVolume) + ", soughtmask=" + IntToString(maskChannels));
        if (((1 << nVolume) & maskChannels) != 0) // right channel
        {
// SendMessageToPC(GetFirstPC(), "OnPlayerChat --- channel matched");

            bInvoke = FALSE;
            if (GetLocalArrayInt(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, iHook) != FALSE)
            {
                bInvoke = TRUE;
            }
            else
            {
                object oDesiredSpeaker = GetLocalArrayObject(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, iHook);
                if (oShouter == oDesiredSpeaker) bInvoke = TRUE;
            }
            if (bInvoke) // right speaker
            {
// SendMessageToPC(GetFirstPC(), "OnPlayerChat --- speaker matched");
                sChatHandlerScript = GetLocalArrayString(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, iHook);
                oRunner = GetLocalArrayObject(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, iHook);
// SendMessageToPC(GetFirstPC(), "OnPlayerChat --- executing script '" + sChatHandlerScript + "' on object '" + GetName(oRunner) +"'");
                ExecuteScript(sChatHandlerScript, oRunner);
                bAutoRemove = GetLocalArrayInt(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, iHook);
                if (bAutoRemove)
                {
// SendMessageToPC(GetFirstPC(), "OnPlayerChat --- scheduling autoremove");
                    bDirtyList = TRUE;
                    SetLocalArrayInt(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, iHook, 0);
                }
            }
        }
    }

    if (bDirtyList) DMFI_ChatHookRemove(0);

    // always execute the DMFI parser
    ExecuteScript(DMFI_PLAYERCHAT_SCRIPTNAME, OBJECT_SELF);

}

