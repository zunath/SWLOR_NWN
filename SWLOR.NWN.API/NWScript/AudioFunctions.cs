using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the duration (in seconds) of the sound attached to nStrRef
        ///   * Returns 0.0f if no duration is stored or if no sound is attached
        /// </summary>
        public static float GetStrRefSoundDuration(int nStrRef)
        {
            return global::NWN.Core.NWScript.GetStrRefSoundDuration(nStrRef);
        }

        /// <summary>
        ///   Gets the length of the specified wavefile, in seconds
        ///   Only works for sounds used for dialog.
        /// </summary>
        public static float GetDialogSoundLength(int nStrRef)
        {
            return global::NWN.Core.NWScript.GetDialogSoundLength(nStrRef);
        }

        /// <summary>
        ///   This will play a sound that is associated with a stringRef, it will be a mono sound from the location of the object
        ///   running the command.
        ///   if nRunAsAction is off then the sound is forced to play intantly.
        /// </summary>
        public static void PlaySoundByStrRef(int nStrRef, bool nRunAsAction = true)
        {
            global::NWN.Core.NWScript.PlaySoundByStrRef(nStrRef, nRunAsAction ? 1 : 0);
        }

        /// <summary>
        ///   Play sSoundName
        ///   - sSoundName: TBD - SS
        ///   This will play a mono sound from the location of the object running the command.
        /// </summary>
        public static void PlaySound(string sSoundName)
        {
            global::NWN.Core.NWScript.PlaySound(sSoundName);
        }
    }
}