using System;

namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>
    /// A change to a character that must reach the character file before a database write that depends on it
    /// (<see cref="CharacterCheckpoint"/>): the change is made, the file is saved, and the change is undone when the file could not be
    /// saved. A failed save never lets the caller go on to its database side, so the database cannot be credited for something the
    /// character still holds in the saved file. No engine calls: the callers pass what each step does.
    /// </summary>
    public static class CheckpointedChange
    {
        /// <summary>Makes the change and saves the character file.</summary>
        /// <param name="change">Makes the change. False when it could not be made as a whole; whatever it did is then undone.</param>
        /// <param name="save">Saves the character file. False when it could not be saved.</param>
        /// <param name="undo">Puts the character back as it was before the change, including any mark. Called when the change could not be made, the save failed, or either threw.</param>
        /// <param name="commit">Finishes the change once the file holds it (destroying what was taken). Not called when the change is undone.</param>
        /// <returns>True when the change was made and the file saved with it. False, with the change undone, otherwise.</returns>
        public static bool Run(Func<bool> change, Func<bool> save, Action undo, Action commit = null)
        {
            bool saved;
            try
            {
                saved = change() && save();
            }
            catch
            {
                undo();
                throw;
            }

            if (!saved)
            {
                undo();
                return false;
            }

            commit?.Invoke();
            return true;
        }
    }
}
