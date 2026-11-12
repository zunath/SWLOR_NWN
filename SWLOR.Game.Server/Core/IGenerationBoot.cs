using System.Collections.Generic;

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

        /// <summary>On the engine thread: the server's handling of a signal from the host (<c>NativeInteropManager.ProcessSignal</c>).</summary>
        void ProcessSignal(string signal);

        /// <summary>
        /// On the engine thread, after <see cref="ResumeAfterModuleLoad"/> on a reload: the reload bootstrap, the rerun boot
        /// handlers in manifest order and then the adoption steps (<see cref="ReloadBootstrap"/>).
        /// </summary>
        IReadOnlyList<ReloadStepResult> RebuildAfterReload();

        /// <summary>
        /// On the engine thread, while a reload quiesces this build: the carry steps, which leave on the module what the next
        /// build's adoption steps cannot read back from the world.
        /// </summary>
        IReadOnlyList<ReloadStepResult> CarryForReload();

        /// <summary>The cache digests (<see cref="ReloadDigests.Cache"/>). Engine thread, hot reload on only.</summary>
        IReadOnlyList<ReloadDigestLine> DescribeCaches();

        /// <summary>The adopted-state and world digests. Engine thread, hot reload on only.</summary>
        IReadOnlyList<ReloadDigestLine> DescribeAdoptedState();

        /// <summary>
        /// Runs <paramref name="action"/> on the engine thread at the start of the next frame (the server's scheduler), so it
        /// sees the build after the boot scripts or the reload, outside the reload pause.
        /// </summary>
        void RunNextFrame(Action action);

        /// <summary>Lets go of the server on the engine thread: the teardown, then NWN.Core back to the host.</summary>
        void Retire();
    }
}
