using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The aggro aura: the enter and exit handlers, proximity enmity and the aggro that stealth detection hands back. It is
    /// the AI's listener of the stealth notices; the handlers are on <see cref="AIAggroService"/>.
    /// </summary>
    public interface IAIAggroService : IStealthAggroListener
    {
        /// <summary>
        /// Gives stealth this service as the listener of its detection notices. A reload runs boot handlers that rerun before it
        /// adopts the carried state, so the list is full when the first detection runs.
        /// </summary>
        void RegisterStealthAggroListener();

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        void CreatureAggroEnter();

        /// <summary>
        /// When a creature exits the aggro aura of another creature,
        /// </summary>
        void CreatureAggroExit();
    }
}
