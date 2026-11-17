using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Writes the colours a creature, its worn items and a dropped item should show to the engine's material
    /// uniforms. Static callers reach the container's instance through <c>GameServices.TintMapApply</c>.
    /// </summary>
    public interface ITintMapApplyService
    {
        /// <summary>
        /// Rebuilds the creature's complete tint state in one pass: the fit steps, the model renderers, the
        /// native robe palette, then every material colour.
        /// </summary>
        /// <param name="creature">The creature to refresh.</param>
        /// <param name="resetShaderOverrides">True to clear the creature's shader overrides first.</param>
        void ApplyCurrentColors(uint creature, bool resetShaderOverrides = true);

        /// <summary>Writes the colours of an item lying in an area.</summary>
        void ApplyCurrentItemColors(uint item);

        /// <summary>Applies the effective colours and sends the update to clients at once.</summary>
        void ApplyCurrentColorsAndPublish(uint creature);

        /// <summary>
        /// Applies the creature's colours now and once more after a short delay, because native palette changes
        /// can replace client meshes after the immediate update.
        /// </summary>
        void RefreshAfterColorChange(uint creature);

        /// <summary>Writes one material colour of one layer.</summary>
        void ApplyColor(uint creature, TintMapMaterialSelection selection, TintMapLayerType layer,
            TintMapColorSelection color);
    }
}
