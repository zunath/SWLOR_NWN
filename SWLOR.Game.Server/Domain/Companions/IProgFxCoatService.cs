using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// The ProgFX coatings (stoneskin, barkskin, Shadow Shield and the like) drawn on tint-map materials. The client coats a
    /// legacy mesh by swapping its diffuse texture for the effect's vdu_tex_* texture, but it never swaps textures bound by an
    /// MTR. Every tint MTR therefore binds the progfx_coat atlas, and this writes the active coat's atlas cell plus one to
    /// the <c>progFxCoat</c> uniform. Static callers reach the container's instance through <c>GameServices.ProgFxCoat</c>.
    /// </summary>
    public interface IProgFxCoatService
    {
        /// <summary>
        /// Finds every visualeffects.2da row that coats once, while the module loads, because the native visual-effect hooks
        /// read the result without a script context.
        /// </summary>
        void LoadCoatingVisualEffects();

        /// <summary>True when applying or removing this visualeffects.2da row can change a coat.</summary>
        bool IsCoatingVisualEffect(int visualEffect);

        /// <summary>
        /// Writes the current coat after a complete tint refresh. A refresh that reset every shader override has already
        /// cleared the coat, so only an active coat needs writing then.
        /// </summary>
        void Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections, bool shaderOverridesReset);

        /// <summary>Coat cell plus one for a visualeffects.2da row, or zero when the row has no coating.</summary>
        int GetCoatForVisualEffect(int visualEffect);

        /// <summary>Coat cell plus one for a progfx.2da environment-map row, or zero for any other row.</summary>
        int GetCoatForProgFx(int progFx);

        /// <summary>Coat cell plus one for one progfx.2da row's Type and Param1 columns.</summary>
        int GetCoatForProgFxRow(string type, string texture);

        /// <summary>Coat cell plus one for an atlas texture name, or zero when the atlas has no such cell.</summary>
        int GetCoatForTexture(string texture);

        /// <summary>The most recently applied coating wins, as it does on the client's legacy meshes.</summary>
        int GetCoat(uint creature);
    }
}
