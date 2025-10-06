using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Combat.Feature
{
    public class FeatConfiguration
    {
        private readonly IFeatPluginService _featPlugin;

        public FeatConfiguration(
            IFeatPluginService featPlugin,
            IEventAggregator eventAggregator)
        {
            _featPlugin = featPlugin;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleLoad>(e => ConfigureFeats());
        }

        /// <summary>
        /// When the module loads, configure all custom feats.
        /// </summary>
        public void ConfigureFeats()
        {
            _featPlugin.SetFeatModifier(FeatType.ShieldConcealment1, FeatModifierType.Concealment, 5);
            _featPlugin.SetFeatModifier(FeatType.ShieldConcealment2, FeatModifierType.Concealment, 10);
            _featPlugin.SetFeatModifier(FeatType.ShieldConcealment3, FeatModifierType.Concealment, 15);
            _featPlugin.SetFeatModifier(FeatType.ShieldConcealment4, FeatModifierType.Concealment, 20);
            _featPlugin.SetFeatModifier(FeatType.ShieldConcealment5, FeatModifierType.Concealment, 25);
        }
    }
}
