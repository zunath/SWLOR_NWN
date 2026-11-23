using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Beast companions: the catalog, taming, spawning and incubation (the static class <c>BeastMastery</c> until it was removed).</summary>
        public static IBeastMasteryService BeastMastery => FacadeAccessor.Resolve<IBeastMasteryService>();

        /// <summary>Player droids: spawning, the saved equipment snapshot and the droid event handlers (the static class <c>Droid</c> until it was converted).</summary>
        public static IDroidService Droid => FacadeAccessor.Resolve<IDroidService>();

        /// <summary>The droid equipment diagnostics log (the static class <c>DroidEquipmentDiagnostics</c> until it was removed).</summary>
        public static IDroidEquipmentDiagnosticsService DroidEquipmentDiagnostics => FacadeAccessor.Resolve<IDroidEquipmentDiagnosticsService>();

        /// <summary>The incubation field notes that document how to produce a mutated beast (the static class <c>IncubationFieldNote</c> until it was removed).</summary>
        public static IIncubationFieldNoteService IncubationFieldNote => FacadeAccessor.Resolve<IIncubationFieldNoteService>();

        /// <summary>The ProgFX coatings (stoneskin, barkskin, Shadow Shield) drawn on tint-map materials.</summary>
        public static IProgFxCoatService ProgFxCoat => FacadeAccessor.Resolve<IProgFxCoatService>();

        /// <summary>The colours the tint map writes to the engine's material uniforms for a creature, its worn items and a dropped item.</summary>
        public static ITintMapApplyService TintMapApply => FacadeAccessor.Resolve<ITintMapApplyService>();

        /// <summary>The carry of an item's custom colours across a model change.</summary>
        public static ITintMapCarryService TintMapCarry => FacadeAccessor.Resolve<ITintMapCarryService>();

        /// <summary>The colours of a creature's own layers (skin, hair, tattoos) and the player record that keeps them.</summary>
        public static ITintMapCreatureColorService TintMapCreatureColor => FacadeAccessor.Resolve<ITintMapCreatureColorService>();

        /// <summary>Setting and resetting the colours of an item's materials.</summary>
        public static ITintMapItemColorService TintMapItemColor => FacadeAccessor.Resolve<ITintMapItemColorService>();

        /// <summary>The queued tint refreshes after a spawn, an area entry or an equipment change.</summary>
        public static ITintMapService TintMap => FacadeAccessor.Resolve<ITintMapService>();
    }
}
