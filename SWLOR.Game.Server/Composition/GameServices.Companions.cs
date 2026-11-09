using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Beast companions: the catalog, taming, spawning and incubation (the static class <c>BeastMastery</c> until P2-ZO).</summary>
        public static IBeastMasteryService BeastMastery => FacadeAccessor.Resolve<IBeastMasteryService>();

        /// <summary>The droid equipment diagnostics log (the static class <c>DroidEquipmentDiagnostics</c> until P2-ZO).</summary>
        public static IDroidEquipmentDiagnosticsService DroidEquipmentDiagnostics => FacadeAccessor.Resolve<IDroidEquipmentDiagnosticsService>();

        /// <summary>The incubation field notes that document how to produce a mutated beast (the static class <c>IncubationFieldNote</c> until P2-ZO).</summary>
        public static IIncubationFieldNoteService IncubationFieldNote => FacadeAccessor.Resolve<IIncubationFieldNoteService>();
    }
}
