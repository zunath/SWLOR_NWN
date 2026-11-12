namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The server work the game build does at each step of its life as the host's generation
    /// (<c>Composition.GameGeneration</c>); tests replace it.
    /// </summary>
    public interface IGenerationBoot
    {
        /// <summary>Builds the service container with validation and discards it. Runs off the engine thread.</summary>
        void Validate();

        /// <summary>Boots the server on the engine thread: the host entry's bootstrap steps.</summary>
        void Activate(HostedBoot boot);

        /// <summary>
        /// On the engine thread, after <see cref="Activate"/> on a reload: what the server does when NWNX signals that the
        /// module has loaded (the script executor takes the engine's virtual machine), because the engine does not signal
        /// it again for a build that activates after the module has loaded.
        /// </summary>
        void ResumeAfterModuleLoad();

        /// <summary>Lets go of the server on the engine thread: the teardown, then NWN.Core back to the host.</summary>
        void Retire();
    }
}
