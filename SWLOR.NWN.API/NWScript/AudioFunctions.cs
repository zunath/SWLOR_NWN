namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the duration (in seconds) of the sound attached to the specified string reference.
        /// </summary>
        /// <param name="nStrRef">The string reference to get the sound duration for</param>
        /// <returns>The duration in seconds. Returns 0.0f if no duration is stored or if no sound is attached</returns>
        public static float GetStrRefSoundDuration(int nStrRef)
        {
            return global::NWN.Core.NWScript.GetStrRefSoundDuration(nStrRef);
        }

        /// <summary>
        /// Gets the length of the specified wavefile in seconds.
        /// </summary>
        /// <param name="nStrRef">The string reference to get the dialog sound length for</param>
        /// <returns>The length in seconds</returns>
        /// <remarks>Only works for sounds used for dialog.</remarks>
        public static float GetDialogSoundLength(int nStrRef)
        {
            return global::NWN.Core.NWScript.GetDialogSoundLength(nStrRef);
        }

        /// <summary>
        /// Plays a sound that is associated with a string reference as a mono sound from the location of the object running the command.
        /// </summary>
        /// <param name="nStrRef">The string reference of the sound to play</param>
        /// <param name="nRunAsAction">If false, the sound is forced to play instantly (default: true)</param>
        public static void PlaySoundByStrRef(int nStrRef, bool nRunAsAction = true)
        {
            global::NWN.Core.NWScript.PlaySoundByStrRef(nStrRef, nRunAsAction ? 1 : 0);
        }

        /// <summary>
        /// Plays the specified sound as a mono sound from the location of the object running the command.
        /// </summary>
        /// <param name="sSoundName">The name of the sound to play</param>
        public static void PlaySound(string sSoundName)
        {
            global::NWN.Core.NWScript.PlaySound(sSoundName);
        }
    }
}