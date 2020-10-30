
//  DMFI OnPlayerChat routines :: event hooking functions
//
//  history
//      2008.03.23 tsunami282 - created.
//

#include "dmfi_arrays_inc"

const string DMFI_CHATHOOK_HANDLE_ARRAYNAME = "DMFI_CHATHOOK_HANDLE";
const string DMFI_CHATHOOK_SCRIPT_ARRAYNAME = "DMFI_CHATHOOK_SCRIPT";
const string DMFI_CHATHOOK_RUNNER_ARRAYNAME = "DMFI_CHATHOOK_RUNNER";
const string DMFI_CHATHOOK_CHANNELS_ARRAYNAME = "DMFI_CHATHOOK_CHANNELS";
const string DMFI_CHATHOOK_LISTENALL_ARRAYNAME = "DMFI_CHATHOOK_LISTENALL";
const string DMFI_CHATHOOK_SPEAKER_ARRAYNAME = "DMFI_CHATHOOK_SPEAKER";
const string DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME = "DMFI_CHATHOOK_AUTOREMOVE";
const string DMFI_CHATHOOK_PREVHANDLE_VARNAME = "DMFI_CHATHOOK_PREVHANDLE";

int DMFI_CHANNELMASK_TALK = (1 << TALKVOLUME_TALK);
int DMFI_CHANNELMASK_WHISPER = (1 << TALKVOLUME_WHISPER);
int DMFI_CHANNELMASK_SHOUT = (1 << TALKVOLUME_SHOUT);
// * this channel not hookable ** int DMFI_CHANNELMASK_SILENT_TALK = (1 << TALKVOLUME_SILENT_TALK);
int DMFI_CHANNELMASK_DM = (1 << TALKVOLUME_SILENT_SHOUT);
int DMFI_CHANNELMASK_PARTY = (1 << TALKVOLUME_PARTY);
// * this channel not hookable ** int DMFI_CHANNELMASK_TELL = (1 << TALKVOLUME_TELL);

////////////////////////////////////////////////////////////////////////
void dmfi__init_chathook_data()
{
    object oMod = GetModule();

    if (!GetLocalArrayInitialized(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME))
    {
        InitializeLocalArray(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, 0);
        InitializeLocalArray(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, 0);
        InitializeLocalArray(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, 0);
        InitializeLocalArray(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, 0);
        InitializeLocalArray(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, 0);
        InitializeLocalArray(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, 0);
        InitializeLocalArray(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME);
        SetLocalArrayLowerBound(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, 1);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, 0);
    }
}

////////////////////////////////////////////////////////////////////////
//! Adds a callback function to the OnPlayerChat list.
//!
//! \param sChatHandlerScript name of script to invoke on receiving input
//! \param oScriptRunner object to execute the sChatHandlerScript on
//! \param maskChannels mask of channels to listen on (defaults to all channels)
//! \param bListenAll TRUE to listen to all PC speakers everywhere
//! \param oSpeaker if bListenAll is FALSE, creature to listen to (others will be ignored)
//! \param bAutoRemove - automatically unhook this chathook after first use
//! \return hook handle (needed to remove the hook later); 0 means failed to add the hook
int DMFI_ChatHookAdd(string sChatHandlerScript, object oScriptRunner = OBJECT_SELF,
        int maskChannels = -1, int bListenAll = TRUE, object oSpeaker = OBJECT_INVALID,
        int bAutoRemove = FALSE)
{
    dmfi__init_chathook_data();

    object oMod = GetModule();

    int iHook = GetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME);
    iHook++;
    int hdlHook = GetLocalInt(oMod, DMFI_CHATHOOK_PREVHANDLE_VARNAME);
    hdlHook++;
    if (hdlHook < 1) hdlHook = 1; // reserving 0 and negatives
    // SendMessageToPC(GetFirstPC(), "chathookadd - adding hook #" + IntToString(iHook));
    SetLocalInt(oMod, DMFI_CHATHOOK_PREVHANDLE_VARNAME, hdlHook);
    SetLocalArrayInt(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, iHook, hdlHook);
    SetLocalArrayString(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, iHook, sChatHandlerScript);
    SetLocalArrayObject(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, iHook, oScriptRunner);
    SetLocalArrayInt(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, iHook, maskChannels);
    SetLocalArrayInt(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, iHook, bListenAll);
    SetLocalArrayObject(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, iHook, oSpeaker);
    SetLocalArrayInt(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, iHook, bAutoRemove);

    return hdlHook;
}

////////////////////////////////////////////////////////////////////////
//! removes a callback function from the OnPlayerChat list.
//! \param hdlHookIn handle of hook to remove (0 for clean up orphans)
//! \return TRUE if requested hook found and removed
int DMFI_ChatHookRemove(int hdlHookIn)
{
    int bRemoved = FALSE;
    int hdlHook;
    int iHook, iHook2;
    object oMod = GetModule();
    int nHooks = GetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME);
    for (iHook = 1; iHook <= nHooks; iHook++)
    {
        while (1)
        {
            hdlHook = GetLocalArrayInt(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, iHook);
            if (hdlHook != 0 && hdlHook != hdlHookIn) break;

            // kill this one
            for (iHook2 = iHook; iHook2 < nHooks; iHook2++)
            {
                SetLocalArrayInt(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, iHook2, GetLocalArrayInt(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, iHook2+1));
                SetLocalArrayString(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, iHook2, GetLocalArrayString(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, iHook2+1));
                SetLocalArrayObject(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, iHook2, GetLocalArrayObject(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, iHook2+1));
                SetLocalArrayInt(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, iHook2, GetLocalArrayInt(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, iHook2+1));
                SetLocalArrayInt(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, iHook2, GetLocalArrayInt(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, iHook2+1));
                SetLocalArrayObject(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, iHook2, GetLocalArrayObject(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, iHook2+1));
                SetLocalArrayInt(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, iHook2, GetLocalArrayInt(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, iHook2+1));
            }
            bRemoved = TRUE;
            nHooks--;
            if (nHooks < iHook) break;
        }
    }

    if (bRemoved)
    {
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_HANDLE_ARRAYNAME, nHooks);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_SCRIPT_ARRAYNAME, nHooks);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_RUNNER_ARRAYNAME, nHooks);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_CHANNELS_ARRAYNAME, nHooks);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_LISTENALL_ARRAYNAME, nHooks);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_SPEAKER_ARRAYNAME, nHooks);
        SetLocalArrayUpperBound(oMod, DMFI_CHATHOOK_AUTOREMOVE_ARRAYNAME, nHooks);
    }

    return bRemoved;
}

