using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The definition catalog of the status effects: every status effect type the server finds when the module caches, and
    /// the creation of a fresh effect by type. Part 2 of the status effect split; the tracker and the other parts read it.
    /// </summary>
    public interface IStatusEffectCatalogService
    {
        /// <summary>
        /// When the module caches, status effects will be discovered and cached.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Creates a fresh instance of a registered status effect so a caller can set per-application options before applying it.
        /// </summary>
        IStatusEffect CreateStatusEffect(Type statusEffectClass);

        /// <summary>Creates a fresh instance of a registered status effect, or reports that the type is not registered.</summary>
        bool TryCreateStatusEffect(Type statusEffectClass, out IStatusEffect statusEffect);

        bool HasStatusEffectDefinition(Type statusEffectClass);

        /// <summary>The registered type whose class name is <paramref name="name"/>, or null (a saved toggle names its type this way).</summary>
        Type FindDefinitionByName(string name);

        /// <summary>The categories a registered type declares; false when the type is not registered.</summary>
        bool TryGetCategories(Type statusEffectClass, out StatusEffectCategory categories);

        string GetStatusEffectName(Type statusEffectClass);

        StatusEffectSourceType GetStatusEffectSourceType(Type statusEffectClass);

        /// <summary>The registered effect types whose icon is <paramref name="effectIcon"/>.</summary>
        List<Type> GetStatusEffectsFromIcon(EffectIconType effectIcon);
    }
}
