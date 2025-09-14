using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Use this in an OnPerception script to get the object that was perceived.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetLastPerceived()
        {
            return global::NWN.Core.NWScript.GetLastPerceived();
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived was heard.
        /// </summary>
        public static bool GetLastPerceptionHeard()
        {
            return global::NWN.Core.NWScript.GetLastPerceptionHeard() != 0;
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived has become inaudible.
        /// </summary>
        public static bool GetLastPerceptionInaudible()
        {
            return global::NWN.Core.NWScript.GetLastPerceptionInaudible() != 0;
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived was seen.
        /// </summary>
        public static bool GetLastPerceptionSeen()
        {
            return global::NWN.Core.NWScript.GetLastPerceptionSeen() != 0;
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived has vanished.
        /// </summary>
        public static bool GetLastPerceptionVanished()
        {
            return global::NWN.Core.NWScript.GetLastPerceptionVanished() != 0;
        }
    }
}
