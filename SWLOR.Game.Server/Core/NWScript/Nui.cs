using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {

        /// <summary>
        /// Create a NUI window from the given resref(.jui) for the given player.
        /// * The resref needs to be available on the client, not the server.
        /// * The token is a integer for ease of handling only. You are not supposed to do anything with it, except store/pass it.
        /// * The window ID needs to be alphanumeric and short. Only one window (per client) with the same ID can exist at a time.
        ///   Re-creating a window with the same id of one already open will immediately close the old one.
        /// * sEventScript is optional and overrides the NUI module event for this window only.
        /// * See nw_inc_nui.nss for full documentation.
        /// Returns the window token on success (>0), or 0 on error.
        /// </summary>
        public static int NuiCreateFromResRef(uint oPlayer, string sResRef, string sWindowId = "", string sEventScript = "")
        {
            VM.StackPush(sEventScript);
            VM.StackPush(sWindowId);
            VM.StackPush(sResRef);
            VM.StackPush(oPlayer);
            VM.Call(1010);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Create a NUI window inline for the given player.
        /// * The token is a integer for ease of handling only. You are not supposed to do anything with it, except store/pass it.
        /// * The window ID needs to be alphanumeric and short. Only one window (per client) with the same ID can exist at a time.
        ///   Re-creating a window with the same id of one already open will immediately close the old one.
        /// * sEventScript is optional and overrides the NUI module event for this window only.
        /// * See nw_inc_nui.nss for full documentation.
        /// Returns the window token on success (>0), or 0 on error.
        /// </summary>
        public static int NuiCreate(uint oPlayer, Json jNui, string sWindowId = "", string sEventScript = "")
        {
            VM.StackPush(sEventScript);
            VM.StackPush(sWindowId);
            VM.StackPush((int)EngineStructure.Json, jNui);
            VM.StackPush(oPlayer);
            VM.Call(1011);

            return VM.StackPopInt();
        }

        /// <summary>
        /// You can look up windows by ID, if you gave them one.
        /// * Windows with a ID present are singletons - attempting to open a second one with the same ID
        ///   will fail, even if the json definition is different.
        /// Returns the token if found, or 0.
        /// </summary>
        public static int NuiFindWindow(uint oPlayer, string sId)
        {
            VM.StackPush(sId);
            VM.StackPush(oPlayer);
            VM.Call(1012);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Destroys the given window, by token, immediately closing it on the client.
        /// Does nothing if nUiToken does not exist on the client.
        /// Does not send a close event - this immediately destroys all serverside state.
        /// The client will close the window asynchronously.
        /// </summary>
        public static void NuiDestroy(uint oPlayer, int nUiToken)
        {
            VM.StackPush(nUiToken);
            VM.StackPush(oPlayer);
            VM.Call(1013);
        }

        /// <summary>
        /// Returns the originating player of the current event.
        /// </summary>
        public static uint NuiGetEventPlayer()
        {
            VM.Call(1014);

            return VM.StackPopObject();
        }

        /// <summary>
        /// Gets the window token of the current event (or 0 if not in a event).
        /// </summary>
        public static int NuiGetEventWindow()
        {
            VM.Call(1015);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns the event type of the current event.
        /// * See nw_inc_nui.nss for full documentation of all events.
        /// </summary>
        public static string NuiGetEventType()
        {
            VM.Call(1016);

            return VM.StackPopString();
        }

        /// <summary>
        /// Returns the ID of the widget that triggered the event.
        /// </summary>
        public static string NuiGetEventElement()
        {
            VM.Call(1017);

            return VM.StackPopString();
        }

        /// <summary>
        /// Get the array index of the current event.
        /// This can be used to get the index into an array, for example when rendering lists of buttons.
        /// Returns -1 if the event is not originating from within an array.
        /// </summary>
        public static int NuiGetEventArrayIndex()
        {
            VM.Call(1018);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns the window ID of the window described by nUiToken.
        /// Returns "" on error, or if the window has no ID.
        /// </summary>
        public static string NuiGetWindowId(uint oPlayer, int nUiToken)
        {
            VM.StackPush(nUiToken);
            VM.StackPush(oPlayer);
            VM.Call(1019);

            return VM.StackPopString();
        }

        /// <summary>
        /// Gets the json value for the given player, token and bind.
        /// * json values can hold all kinds of values; but NUI widgets require specific bind types.
        ///   It is up to you to either handle this in NWScript, or just set compatible bind types.
        ///   No auto-conversion happens.
        /// Returns a json null value if the bind does not exist.
        /// </summary>
        public static Json NuiGetBind(uint oPlayer, int nUiToken, string sBindName)
        {
            VM.StackPush(sBindName);
            VM.StackPush(nUiToken);
            VM.StackPush(oPlayer);
            VM.Call(1020);

            return VM.StackPopStruct((int) EngineStructure.Json);
        }

        /// <summary>
        /// Sets a json value for the given player, token and bind.
        /// The value is synced down to the client and can be used in UI binding.
        /// When the UI changes the value, it is returned to the server and can be retrieved via NuiGetBind().
        /// * json values can hold all kinds of values; but NUI widgets require specific bind types.
        ///   It is up to you to either handle this in NWScript, or just set compatible bind types.
        ///   No auto-conversion happens.
        /// * If the bind is on the watch list, this will immediately invoke the event handler with the "watch"
        ///   even type; even before this function returns. Do not update watched binds from within the watch handler
        ///   unless you enjoy stack overflows.
        /// Does nothing if the given player+token is invalid.
        /// </summary>
        public static void NuiSetBind(uint oPlayer, int nUiToken, string sBindName, Json jValue)
        {
            VM.StackPush((int) EngineStructure.Json, jValue);
            VM.StackPush(sBindName);
            VM.StackPush(nUiToken);
            VM.StackPush(oPlayer);
            VM.Call(1021);
        }

        /// <summary>
        /// Swaps out the given element (by id) with the given nui layout (partial).
        /// * This currently only works with the "group" element type, and the special "_window_" root group.
        /// </summary>
        public static void NuiSetGroupLayout(uint oPlayer, int nUiToken, string sElement, Json jNui)
        {
            VM.StackPush((int)EngineStructure.Json, jNui);
            VM.StackPush(sElement);
            VM.StackPush(nUiToken);
            VM.StackPush(oPlayer);
            VM.Call(1022);
        }

        /// <summary>
        /// Mark the given bind name as watched.
        /// A watched bind will invoke the NUI script event every time it's value changes.
        /// Be careful with binding nui data inside a watch event handler: It's easy to accidentally recurse yourself into a stack overflow.
        /// </summary>
        public static int NuiSetBindWatch(uint oPlayer, int nUiToken, string sBind, bool bWatch)
        {
            VM.StackPush(bWatch ? 1 : 0);
            VM.StackPush(sBind);
            VM.StackPush(nUiToken);
            VM.StackPush(oPlayer);
            VM.Call(1023);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns the nNth window token of the player, or 0.
        /// nNth starts at 0.
        /// Iterator is not write-safe: Calling DestroyWindow() will invalidate move following offsets by one.
        /// </summary>
        public static int NuiGetNthWindow(uint oPlayer, int nNth = 0)
        {
            VM.StackPush(nNth);
            VM.StackPush(oPlayer);
            VM.Call(1024);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Return the nNth bind name of the given window, or "".
        /// If bWatched is TRUE, iterates only watched binds.
        /// If FALSE, iterates all known binds on the window (either set locally or in UI).
        /// </summary>
        public static string NuiGetNthBind(uint oPlayer, int nToken, bool bWatched, int nNth = 0)
        {
            VM.StackPush(nNth);
            VM.StackPush(bWatched ? 1 : 0);
            VM.StackPush(nToken);
            VM.StackPush(oPlayer);
            VM.Call(1025);

            return VM.StackPopString();
        }

        /// <summary>
        /// Returns the event payload, specific to the event.
        /// Returns JsonNull if event has no payload.
        /// </summary>
        public static Json NuiGetEventPayload()
        {
            VM.Call(1026);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Get the userdata of the given window token.
        /// Returns JsonNull if the window does not exist on the given player, or has no userdata set.
        /// </summary>
        public static Json NuiGetUserData(uint oPlayer, int nToken)
        {
            VM.StackPush(nToken);
            VM.StackPush(oPlayer);
            VM.Call(1027);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        /// Sets an arbitrary json value as userdata on the given window token.
        /// This userdata is not read or handled by the game engine and not sent to clients.
        /// This mechanism only exists as a convenience for the programmer to store data bound to a windows' lifecycle.
        /// Will do nothing if the window does not exist.
        /// </summary>
        public static void NuiSetUserData(uint oPlayer, int nToken, Json jUserData)
        {
            VM.StackPush((int) EngineStructure.Json, jUserData);
            VM.StackPush(nToken);
            VM.StackPush(oPlayer);
            VM.Call(1028);
        }

    }
}
