namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Use this in an OnPerception script to get the object that was perceived.
        /// </summary>
        /// <returns>The object that was perceived, or OBJECT_INVALID if the caller is not a valid creature</returns>
        public static uint GetLastPerceived()
        {
            return NWScriptRuntime.Current.GetLastPerceived();
        }

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived was heard.
        /// </summary>
        /// <returns>TRUE if the object was heard</returns>
        public static bool GetLastPerceptionHeard()
        {
            return NWScriptRuntime.Current.GetLastPerceptionHeard();
        }

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived has become inaudible.
        /// </summary>
        /// <returns>TRUE if the object has become inaudible</returns>
        public static bool GetLastPerceptionInaudible()
        {
            return NWScriptRuntime.Current.GetLastPerceptionInaudible();
        }

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived was seen.
        /// </summary>
        /// <returns>TRUE if the object was seen</returns>
        public static bool GetLastPerceptionSeen()
        {
            return NWScriptRuntime.Current.GetLastPerceptionSeen();
        }

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived has vanished.
        /// </summary>
        /// <returns>TRUE if the object has vanished</returns>
        public static bool GetLastPerceptionVanished()
        {
            return NWScriptRuntime.Current.GetLastPerceptionVanished();
        }
    }
}
