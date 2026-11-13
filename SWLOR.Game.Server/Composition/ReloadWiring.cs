using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// What a hot reload's player and state steps (P4-15, <see cref="ServerBootstrapper"/>) need from the domains, built from the
    /// container: the carrier of the <see cref="ReloadStateKind.Carried"/> fields with its converters for definitions the rerun
    /// caches rebuild, and the players' screens. The composition root names the domain types so the bootstrapper does not.
    /// </summary>
    public static class ReloadWiring
    {
        /// <summary>The carrier of this build's carried state, with the ability definition converter.</summary>
        public static ReloadStateCarrier CreateStateCarrier(ContainerHost container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            return new ReloadStateCarrier(typeof(ReloadWiring).Assembly, new JsonConverter[]
            {
                new AbilityDetailReloadConverter(() => container.Resolve<IAbilityService>().GetAllAbilityDetails()),
            });
        }

        /// <summary>The players' windows and conversations, through the container's GUI and conversation services.</summary>
        public static IPlayerScreenCarry CreateScreenCarry(ContainerHost container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            return new PlayerScreenCarry(container.Resolve<IGuiService>(), container.Resolve<IConversationService>());
        }
    }
}
