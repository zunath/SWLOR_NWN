using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Causes the specified object to run the specified event.
        /// </summary>
        /// <param name="oObject">The object to run the event on</param>
        /// <param name="evToRun">The event to run</param>
        /// <remarks>The script on the object that is associated with the event specified will run. Events can be created using the following event functions: EventActivateItem() - This creates an OnActivateItem module event. The script for handling this event can be set in Module Properties on the Event Tab. EventConversation() - This creates an OnConversation creature event. The script for handling this event can be set by viewing the Creature Properties on a creature and then clicking on the Scripts Tab. EventSpellCastAt() - This creates an OnSpellCastAt event. The script for handling this event can be set in the Scripts Tab of the Properties menu for the object. EventUserDefined() - This creates an OnUserDefined event. The script for handling this event can be set in the Scripts Tab of the Properties menu for the object/area/module.</remarks>
        public static void SignalEvent(uint oObject, Event evToRun)
        {
            global::NWN.Core.NWScript.SignalEvent(oObject, evToRun);
        }

        /// <summary>
        /// Creates an event of the specified user-defined event number.
        /// </summary>
        /// <param name="nUserDefinedEventNumber">The user-defined event number</param>
        /// <returns>The created user-defined event</returns>
        /// <remarks>This only creates the event. The event won't actually trigger until SignalEvent() is called using this created UserDefined event as an argument. For example: SignalEvent(oObject, EventUserDefined(9999)); Once the event has been signaled, the script associated with the OnUserDefined event will run on the object oObject. To specify the OnUserDefined script that should run, view the object's Properties and click on the Scripts Tab. Then specify a script for the OnUserDefined event. From inside the OnUserDefined script call: GetUserDefinedEventNumber() to retrieve the value of nUserDefinedEventNumber that was used when the event was signaled.</remarks>
        public static Event EventUserDefined(int nUserDefinedEventNumber)
        {
            return global::NWN.Core.NWScript.EventUserDefined(nUserDefinedEventNumber);
        }

        /// <summary>
        /// Gets the user-defined event number.
        /// </summary>
        /// <returns>The user-defined event number</returns>
        /// <remarks>This is for use in a user-defined script.</remarks>
        public static int GetUserDefinedEventNumber()
        {
            return global::NWN.Core.NWScript.GetUserDefinedEventNumber();
        }

        /// <summary>
        /// Gets the object that closed the door or placeable.
        /// </summary>
        /// <returns>The object that closed the door or placeable. Returns OBJECT_INVALID if the caller is not a valid door or placeable</returns>
        /// <remarks>Use this in an OnClosed script.</remarks>
        public static uint GetLastClosedBy()
        {
            return global::NWN.Core.NWScript.GetLastClosedBy();
        }

        /// <summary>
        /// Returns the event script for the given object and handler.
        /// </summary>
        /// <param name="oObject">The object to get the event script for</param>
        /// <param name="nHandler">The event script handler</param>
        /// <returns>The event script. Will return empty string if unset, the object is invalid, or the object cannot have the requested handler</returns>
        public static string GetEventScript(uint oObject, EventScript nHandler)
        {
            return global::NWN.Core.NWScript.GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        /// Sets the given event script for the given object and handler.
        /// </summary>
        /// <param name="oObject">The object to set the event script for</param>
        /// <param name="nHandler">The event script handler</param>
        /// <param name="sScript">The script to set</param>
        /// <returns>1 on success, 0 on failure. Will fail if oObject is invalid or does not have the requested handler</returns>
        public static int SetEventScript(uint oObject, EventScript nHandler, string sScript)
        {
            return global::NWN.Core.NWScript.SetEventScript(oObject, (int)nHandler, sScript);
        }

        /// <summary>
        /// Gets an optional vector of specific GUI events in the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns>The vector of the last GUI event. GUIEVENT_RADIAL_OPEN - World vector position of radial if on tile</returns>
        public static Vector3 GetLastGuiEventVector()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventVector();
        }
    }
}
