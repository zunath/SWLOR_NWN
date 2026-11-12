using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>The server work <see cref="GameGeneration"/> runs at each step of its life; tests replace it.</summary>
    public interface IGenerationBoot
    {
        /// <summary>Builds the service container with validation and discards it. Runs off the engine thread.</summary>
        void Validate();

        /// <summary>Boots the server on the engine thread: the host entry's bootstrap steps.</summary>
        void Activate(HostedBoot boot);

        /// <summary>Lets go of the server on the engine thread: the teardown, then NWN.Core back to the host.</summary>
        void Retire();
    }
}
