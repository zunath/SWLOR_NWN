using System;

namespace SWLOR.Game.Server.Core
{
    /// <summary>One named step of the server boot, see <see cref="ServerBootstrapper.Steps"/>.</summary>
    public sealed class BootstrapStep
    {
        public BootstrapStep(string name, Action run)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Run = run ?? throw new ArgumentNullException(nameof(run));
        }

        /// <summary>The name of the method that does the work, for example <c>BuildServiceContainer</c>.</summary>
        public string Name { get; }

        /// <summary>Does the step's work.</summary>
        public Action Run { get; }
    }
}
