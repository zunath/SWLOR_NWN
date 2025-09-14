using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if a specific key is required to open the lock on oObject.
        /// </summary>
        public static bool GetLockKeyRequired(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockKeyRequired(oObject) != 0;
        }

        /// <summary>
        ///   Get the tag of the key that will open the lock on oObject.
        /// </summary>
        public static string GetLockKeyTag(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockKeyTag(oObject);
        }

        /// <summary>
        ///   * Returns TRUE if the lock on oObject is lockable.
        /// </summary>
        public static bool GetLockLockable(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockLockable(oObject) != 0;
        }

        /// <summary>
        ///   Get the DC for unlocking oObject.
        /// </summary>
        public static int GetLockUnlockDC(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockUnlockDC(oObject);
        }

        /// <summary>
        ///   Get the DC for locking oObject.
        /// </summary>
        public static int GetLockLockDC(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockLockDC(oObject);
        }
    }
}
