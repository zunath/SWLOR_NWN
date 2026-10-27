using System;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Thrown when game code asks the dependency-injection container for a service before the server has built it,
    /// or when the process container is built a second time. Both are boot-order mistakes, so they fail loudly
    /// instead of letting a handler run against a missing or half-replaced service graph.
    /// </summary>
    public sealed class ContainerNotBuiltException : InvalidOperationException
    {
        public ContainerNotBuiltException(string message)
            : base(message)
        {
        }
    }
}
