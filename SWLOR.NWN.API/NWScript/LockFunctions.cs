namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Returns TRUE if a specific key is required to open the lock on the object.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if a key is required</returns>
        public static bool GetLockKeyRequired(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockKeyRequired(oObject) != 0;
        }

        /// <summary>
        /// Gets the tag of the key that will open the lock on the object.
        /// </summary>
        /// <param name="oObject">The object to get the key tag for</param>
        /// <returns>The key tag</returns>
        public static string GetLockKeyTag(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockKeyTag(oObject);
        }

        /// <summary>
        /// Returns TRUE if the lock on the object is lockable.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the lock is lockable</returns>
        public static bool GetLockLockable(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockLockable(oObject) != 0;
        }

        /// <summary>
        /// Gets the DC for unlocking the object.
        /// </summary>
        /// <param name="oObject">The object to get the unlock DC for</param>
        /// <returns>The unlock DC</returns>
        public static int GetLockUnlockDC(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockUnlockDC(oObject);
        }

        /// <summary>
        /// Gets the DC for locking the object.
        /// </summary>
        /// <param name="oObject">The object to get the lock DC for</param>
        /// <returns>The lock DC</returns>
        public static int GetLockLockDC(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLockLockDC(oObject);
        }
    }
}
