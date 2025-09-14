using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Creates a NUI window from the given resref(.jui) for the given player.
        /// The resref needs to be available on the client, not the server.
        /// The token is an integer for ease of handling only. You are not supposed to do anything with it, except store/pass it.
        /// The window ID needs to be alphanumeric and short. Only one window (per client) with the same ID can exist at a time.
        /// Re-creating a window with the same id of one already open will immediately close the old one.
        /// See nw_inc_nui.nss for full documentation.
        /// </summary>
        /// <param name="oPlayer">The player to create the window for</param>
        /// <param name="sResRef">The resref of the .jui file</param>
        /// <param name="sWindowId">The window ID (defaults to empty string)</param>
        /// <returns>The window token on success (>0), or 0 on error</returns>
        public static int NuiCreateFromResRef(uint oPlayer, string sResRef, string sWindowId = "")
        {
            return global::NWN.Core.NWScript.NuiCreateFromResRef(oPlayer, sResRef, sWindowId);
        }

        /// <summary>
        /// Creates a NUI window inline for the given player.
        /// The token is an integer for ease of handling only. You are not supposed to do anything with it, except store/pass it.
        /// The window ID needs to be alphanumeric and short. Only one window (per client) with the same ID can exist at a time.
        /// Re-creating a window with the same id of one already open will immediately close the old one.
        /// See nw_inc_nui.nss for full documentation.
        /// </summary>
        /// <param name="oPlayer">The player to create the window for</param>
        /// <param name="jNui">The NUI JSON definition</param>
        /// <param name="sWindowId">The window ID (defaults to empty string)</param>
        /// <returns>The window token on success (>0), or 0 on error</returns>
        public static int NuiCreate(uint oPlayer, Json jNui, string sWindowId = "")
        {
            return global::NWN.Core.NWScript.NuiCreate(oPlayer, jNui, sWindowId);
        }

        /// <summary>
        /// You can look up windows by ID, if you gave them one.
        /// Windows with an ID present are singletons - attempting to open a second one with the same ID
        /// will fail, even if the json definition is different.
        /// </summary>
        /// <param name="oPlayer">The player to look up the window for</param>
        /// <param name="sId">The window ID to look up</param>
        /// <returns>The token if found, or 0</returns>
        public static int NuiFindWindow(uint oPlayer, string sId)
        {
            return global::NWN.Core.NWScript.NuiFindWindow(oPlayer, sId);
        }

        /// <summary>
        /// Destroys the given window, by token, immediately closing it on the client.
        /// Does nothing if nUiToken does not exist on the client.
        /// Does not send a close event - this immediately destroys all serverside state.
        /// The client will close the window asynchronously.
        /// </summary>
        /// <param name="oPlayer">The player to destroy the window for</param>
        /// <param name="nUiToken">The window token to destroy</param>
        public static void NuiDestroy(uint oPlayer, int nUiToken)
        {
            global::NWN.Core.NWScript.NuiDestroy(oPlayer, nUiToken);
        }

        /// <summary>
        /// Returns the originating player of the current event.
        /// </summary>
        /// <returns>The originating player of the current event</returns>
        public static uint NuiGetEventPlayer()
        {
            return global::NWN.Core.NWScript.NuiGetEventPlayer();
        }

        /// <summary>
        /// Gets the window token of the current event (or 0 if not in an event).
        /// </summary>
        /// <returns>The window token of the current event, or 0 if not in an event</returns>
        public static int NuiGetEventWindow()
        {
            return global::NWN.Core.NWScript.NuiGetEventWindow();
        }

        /// <summary>
        /// Returns the event type of the current event.
        /// See nw_inc_nui.nss for full documentation of all events.
        /// </summary>
        /// <returns>The event type of the current event</returns>
        public static string NuiGetEventType()
        {
            return global::NWN.Core.NWScript.NuiGetEventType();
        }

        /// <summary>
        /// Returns the ID of the widget that triggered the event.
        /// </summary>
        /// <returns>The ID of the widget that triggered the event</returns>
        public static string NuiGetEventElement()
        {
            return global::NWN.Core.NWScript.NuiGetEventElement();
        }

        /// <summary>
        /// Gets the array index of the current event.
        /// This can be used to get the index into an array, for example when rendering lists of buttons.
        /// </summary>
        /// <returns>The array index of the current event, or -1 if the event is not originating from within an array</returns>
        public static int NuiGetEventArrayIndex()
        {
            return global::NWN.Core.NWScript.NuiGetEventArrayIndex();
        }

        /// <summary>
        /// Returns the window ID of the window described by the UI token.
        /// </summary>
        /// <param name="oPlayer">The player to get the window ID for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <returns>The window ID, or empty string on error or if the window has no ID</returns>
        public static string NuiGetWindowId(uint oPlayer, int nUiToken)
        {
            return global::NWN.Core.NWScript.NuiGetWindowId(oPlayer, nUiToken);
        }

        /// <summary>
        /// Gets the JSON value for the given player, token and bind.
        /// JSON values can hold all kinds of values; but NUI widgets require specific bind types.
        /// It is up to you to either handle this in NWScript, or just set compatible bind types.
        /// No auto-conversion happens.
        /// </summary>
        /// <param name="oPlayer">The player to get the bind for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sBindName">The bind name</param>
        /// <returns>A JSON value, or JSON null value if the bind does not exist</returns>
        public static Json NuiGetBind(uint oPlayer, int nUiToken, string sBindName)
        {
            return global::NWN.Core.NWScript.NuiGetBind(oPlayer, nUiToken, sBindName);
        }

        /// <summary>
        /// Sets a JSON value for the given player, token and bind.
        /// The value is synced down to the client and can be used in UI binding.
        /// When the UI changes the value, it is returned to the server and can be retrieved via NuiGetBind().
        /// JSON values can hold all kinds of values; but NUI widgets require specific bind types.
        /// It is up to you to either handle this in NWScript, or just set compatible bind types.
        /// No auto-conversion happens.
        /// If the bind is on the watch list, this will immediately invoke the event handler with the "watch"
        /// event type; even before this function returns. Do not update watched binds from within the watch handler
        /// unless you enjoy stack overflows.
        /// Does nothing if the given player+token is invalid.
        /// </summary>
        /// <param name="oPlayer">The player to set the bind for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sBindName">The bind name</param>
        /// <param name="jValue">The JSON value to set</param>
        public static void NuiSetBind(uint oPlayer, int nUiToken, string sBindName, Json jValue)
        {
            global::NWN.Core.NWScript.NuiSetBind(oPlayer, nUiToken, sBindName, jValue);
        }

        /// <summary>
        /// Swaps out the given element (by id) with the given NUI layout (partial).
        /// This currently only works with the "group" element type, and the special "_window_" root group.
        /// </summary>
        /// <param name="oPlayer">The player to set the group layout for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sElement">The element ID</param>
        /// <param name="jNui">The NUI JSON layout</param>
        public static void NuiSetGroupLayout(uint oPlayer, int nUiToken, string sElement, Json jNui)
        {
            global::NWN.Core.NWScript.NuiSetGroupLayout(oPlayer, nUiToken, sElement, jNui);
        }

        /// <summary>
        /// Marks the given bind name as watched.
        /// A watched bind will invoke the NUI script event every time it's value changes.
        /// Be careful with binding NUI data inside a watch event handler: It's easy to accidentally recurse yourself into a stack overflow.
        /// </summary>
        /// <param name="oPlayer">The player to set the bind watch for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sBind">The bind name</param>
        /// <param name="bWatch">Whether to watch the bind</param>
        /// <returns>The result of the operation</returns>
        public static int NuiSetBindWatch(uint oPlayer, int nUiToken, string sBind, bool bWatch)
        {
            return global::NWN.Core.NWScript.NuiSetBindWatch(oPlayer, nUiToken, sBind, bWatch ? 1 : 0);
        }

        /// <summary>
        /// Returns the nth window token of the player, or 0.
        /// nNth starts at 0.
        /// Iterator is not write-safe: Calling DestroyWindow() will invalidate move following offsets by one.
        /// </summary>
        /// <param name="oPlayer">The player to get the window for</param>
        /// <param name="nNth">The nth window (defaults to 0)</param>
        /// <returns>The nth window token, or 0</returns>
        public static int NuiGetNthWindow(uint oPlayer, int nNth = 0)
        {
            return global::NWN.Core.NWScript.NuiGetNthWindow(oPlayer, nNth);
        }

        /// <summary>
        /// Returns the nth bind name of the given window, or empty string.
        /// If bWatched is TRUE, iterates only watched binds.
        /// If FALSE, iterates all known binds on the window (either set locally or in UI).
        /// </summary>
        /// <param name="oPlayer">The player to get the bind for</param>
        /// <param name="nToken">The UI token</param>
        /// <param name="bWatched">Whether to iterate only watched binds</param>
        /// <param name="nNth">The nth bind (defaults to 0)</param>
        /// <returns>The nth bind name, or empty string</returns>
        public static string NuiGetNthBind(uint oPlayer, int nToken, bool bWatched, int nNth = 0)
        {
            return global::NWN.Core.NWScript.NuiGetNthBind(oPlayer, nToken, bWatched ? 1 : 0, nNth);
        }

        /// <summary>
        /// Returns the event payload, specific to the event.
        /// </summary>
        /// <returns>The event payload, or JsonNull if event has no payload</returns>
        public static Json NuiGetEventPayload()
        {
            return global::NWN.Core.NWScript.NuiGetEventPayload();
        }

        /// <summary>
        /// Gets the userdata of the given window token.
        /// </summary>
        /// <param name="oPlayer">The player to get the userdata for</param>
        /// <param name="nToken">The UI token</param>
        /// <returns>The userdata, or JsonNull if the window does not exist on the given player, or has no userdata set</returns>
        public static Json NuiGetUserData(uint oPlayer, int nToken)
        {
            return global::NWN.Core.NWScript.NuiGetUserData(oPlayer, nToken);
        }

        /// <summary>
        /// Sets an arbitrary JSON value as userdata on the given window token.
        /// This userdata is not read or handled by the game engine and not sent to clients.
        /// This mechanism only exists as a convenience for the programmer to store data bound to a window's lifecycle.
        /// Will do nothing if the window does not exist.
        /// </summary>
        /// <param name="oPlayer">The player to set the userdata for</param>
        /// <param name="nToken">The UI token</param>
        /// <param name="jUserData">The JSON userdata to set</param>
        public static void NuiSetUserData(uint oPlayer, int nToken, Json jUserData)
        {
            global::NWN.Core.NWScript.NuiSetUserData(oPlayer, nToken, jUserData);
        }

    }
}
