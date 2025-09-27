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

        /// <summary>
        /// Un/pauses the given audio stream.
        /// fFadeTime is in seconds to gradually fade the audio out/in instead of pausing/resuming directly.
        /// Only one type of fading can be active at once, for example:
        /// If you call StartAudioStream() with fFadeInTime = 10.0f, any other audio stream functions with a fade time > 0.0f will have no effect
        /// until StartAudioStream() is done fading.
        /// Will do nothing if the stream is currently not in use.
        /// </summary>
        /// <param name="oPlayer">The player to set audio stream pause for</param>
        /// <param name="nStreamIdentifier">The stream identifier</param>
        /// <param name="bPaused">Whether the stream should be paused</param>
        /// <param name="fFadeTime">The fade time in seconds (default: 0.0f)</param>
        public static void SetAudioStreamPaused(uint oPlayer, int nStreamIdentifier, bool bPaused, float fFadeTime = 0.0f)
        {
            global::NWN.Core.NWScript.SetAudioStreamPaused(oPlayer, nStreamIdentifier, bPaused ? 1 : 0, fFadeTime);
        }

        /// <summary>
        /// Changes volume of audio stream.
        /// Volume is from 0.0 to 1.0.
        /// fFadeTime is in seconds to gradually change the volume.
        /// Only one type of fading can be active at once, for example:
        /// If you call StartAudioStream() with fFadeInTime = 10.0f, any other audio stream functions with a fade time > 0.0f will have no effect
        /// until StartAudioStream() is done fading.
        /// Subsequent calls to this function with fFadeTime > 0.0f while already fading the volume
        /// will start the new fade with the previous fade's progress as starting point.
        /// Will do nothing if the stream is currently not in use.
        /// </summary>
        /// <param name="oPlayer">The player to set audio stream volume for</param>
        /// <param name="nStreamIdentifier">The stream identifier</param>
        /// <param name="fVolume">The volume level (0.0 to 1.0) (default: 1.0f)</param>
        /// <param name="fFadeTime">The fade time in seconds (default: 0.0f)</param>
        public static void SetAudioStreamVolume(uint oPlayer, int nStreamIdentifier, float fVolume = 1.0f, float fFadeTime = 0.0f)
        {
            global::NWN.Core.NWScript.SetAudioStreamVolume(oPlayer, nStreamIdentifier, fVolume, fFadeTime);
        }

        /// <summary>
        /// Seeks the audio stream to the given offset.
        /// When seeking at or beyond the end of a stream, the seek offset will wrap around, even if the file is configured not to loop.
        /// Will do nothing if the stream is currently not in use.
        /// Will do nothing if the stream is in ended state (reached end of file and looping is off). In this
        /// case, you need to restart the stream.
        /// </summary>
        /// <param name="oPlayer">The player to seek audio stream for</param>
        /// <param name="nStreamIdentifier">The stream identifier</param>
        /// <param name="fSeconds">The offset in seconds to seek to</param>
        public static void SeekAudioStream(uint oPlayer, int nStreamIdentifier, float fSeconds)
        {
            global::NWN.Core.NWScript.SeekAudioStream(oPlayer, nStreamIdentifier, fSeconds);
        }
    }
}