
// DMFI_get_line: generic input line processing
//
// You can use this when you want to retrieve a spoken line of text.
//
// Specify the PC you want to listen to, the channel you want to listen on
// (often the TALK channel), and the name of the script to run when a line
// of text is heard.
//
// See the file dmfi_getln_cbtbl for a sample template script for processing
// the heard line.

#include "dmfi_plychat_inc"

const string DMFI_GETLINE_HOOK_HANDLE_VARNAME = "dmfi_getline_hookhandle";

/**
 *
 * @author  tsunami282
 * @since   1.09
 *
 * @param   oSpeaker            PC we want to listen to.
 * @param   iChannel            voice channel to listen on (use TALKVOLUME_ constants).
 * @param   sEventScriptName    sEventScriptName = name of script to call upon completion
 *                              of input (cannot be blank).
 * @param   oRequester          object requesting the result: the sEventScriptName script
 *                              will be invoked with this as the caller, and therefore it
 *                              must be valid at time of player chat event.
 * @return                      handle (positive int) of the chat event hook
*/
int DMFI_get_line(object oSpeaker, int iChannel, string sEventScriptName,
        object oRequester = OBJECT_SELF)
{
    int hdlHook = 0;

    if (GetIsObjectValid(oSpeaker) && GetIsObjectValid(oRequester) && sEventScriptName != "")
    {
// SendMessageToPC(GetFirstPC(), "getline - apply hook");
        hdlHook = DMFI_ChatHookAdd(sEventScriptName, oRequester, (1 << iChannel),
                FALSE, oSpeaker, TRUE);
// SendMessageToPC(GetFirstPC(), "getline - hook handle returned is " + IntToString(hdlHook));
        SetLocalInt(oRequester, DMFI_GETLINE_HOOK_HANDLE_VARNAME, hdlHook);
    }

    return hdlHook;
}

/**
 *
 *
 *
 * @param   hdlHookIn       handle of hook handler that we want to un-hook.
 * @param   oRequester      object requesting the result of DMFI_get_line
*/
void DMFI_cancel_get_line(int hdlHookIn = 0, object oRequester = OBJECT_SELF)
{
    int hdlHook = hdlHookIn;
    if (hdlHook == 0) hdlHook = GetLocalInt(oRequester, DMFI_GETLINE_HOOK_HANDLE_VARNAME);
    DMFI_ChatHookRemove(hdlHook);
}

