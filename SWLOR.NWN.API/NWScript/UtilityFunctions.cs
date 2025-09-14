namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Convert nInteger to hex, returning the hex value as a string.
        ///   * Return value has the format "0x????????" where each ? will be a hex digit
        ///   (8 digits in total).
        /// </summary>
        public static string IntToHexString(int nInteger)
        {
            return global::NWN.Core.NWScript.IntToHexString(nInteger);
        }

        /// <summary>
        ///   SpawnScriptDebugger() will cause the script debugger to be executed
        ///   after this command is executed!
        ///   In order to compile the script for debugging go to Tools->Options->Script Editor
        ///   and check the box labeled "Generate Debug Information When Compiling Scripts"
        ///   After you have checked the above box, recompile the script that you want to debug.
        ///   If the script file isn't compiled for debugging, this command will do nothing.
        ///   Remove any SpawnScriptDebugger() calls once you have finished
        ///   debugging the script.
        /// </summary>
        public static void SpawnScriptDebugger()
        {
            global::NWN.Core.NWScript.SpawnScriptDebugger();
        }


        /// <summary>
        ///   Execute a script chunk.
        ///   The script chunk runs immediately, same as ExecuteScript().
        ///   The script is jitted in place and currently not cached: Each invocation will recompile the script chunk.
        ///   Note that the script chunk will run as if a separate script. This is not eval().
        ///   By default, the script chunk is wrapped into void main() {}. Pass in bWrapIntoMain = FALSE to override.
        ///   Returns "" on success, or the compilation error.
        /// </summary>
        public static string ExecuteScriptChunk(string sScriptChunk, uint oObject, bool bWrapIntoMain = true)
        {
            return global::NWN.Core.NWScript.ExecuteScriptChunk(sScriptChunk, oObject, bWrapIntoMain ? 1 : 0);
        }

        /// <summary>
        ///   Returns a UUID. This UUID will not be associated with any object.
        ///   The generated UUID is currently a v4.
        /// </summary>
        public static string GetRandomUUID()
        {
            return global::NWN.Core.NWScript.GetRandomUUID();
        }

        /// <summary>
        ///   Returns the given objects' UUID. This UUID is persisted across save boundaries,
        ///   like Save/RestoreCampaignObject and save games.
        ///   Thus, reidentification is only guaranteed in scenarios where players cannot introduce
        ///   new objects (i.e. servervault servers).
        ///   UUIDs are guaranteed to be unique in any single running game.
        ///   If a loaded object would collide with a UUID already present in the game, the
        ///   object receives no UUID and a warning is emitted to the log. Requesting a UUID
        ///   for the new object will generate a random one.
        ///   This UUID is useful to, for example:
        ///   - Safely identify servervault characters
        ///   - Track serialisable objects (like items or creatures) as they are saved to the
        ///   campaign DB - i.e. persistent storage chests or dropped items.
        ///   - Track objects across multiple game instances (in trusted scenarios).
        ///   Currently, the following objects can carry UUIDs:
        ///   Items, Creatures, Placeables, Triggers, Doors, Waypoints, Stores,
        ///   Encounters, Areas.
        ///   Will return "" (empty string) when the given object cannot carry a UUID.
        /// </summary>
        public static string GetObjectUUID(uint oObject)
        {
            return global::NWN.Core.NWScript.GetObjectUUID(oObject);
        }

        /// <summary>
        ///   Forces the given object to receive a new UUID, discarding the current value.
        /// </summary>
        public static void ForceRefreshObjectUUID(uint oObject)
        {
            global::NWN.Core.NWScript.ForceRefreshObjectUUID(oObject);
        }

        /// <summary>
        ///   Looks up a object on the server by it's UUID.
        ///   Returns OBJECT_INVALID if the UUID is not on the server.
        /// </summary>
        public static uint GetObjectByUUID(string sUUID)
        {
            return global::NWN.Core.NWScript.GetObjectByUUID(sUUID);
        }

        /// <summary>
        ///   Do not call. This does nothing on this platform except to return an error.
        /// </summary>
        public static void Reserved899()
        {
            global::NWN.Core.NWScript.Reserved899();
        }

    }
}
