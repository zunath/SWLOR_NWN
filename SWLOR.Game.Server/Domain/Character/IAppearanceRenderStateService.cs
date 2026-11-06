using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The runtime state of the appearance renderers: the head scales captured while a helmet renders through the
    /// head, the model catalogs read from the 2DAs when the module loads, the armor part projections the native packet
    /// writer reads, and the cache of which part preview pictures exist. The rendering logic stays in the
    /// <c>HelmetModelRenderer</c>, <c>NeckModelRenderer</c>, <c>PartSkinModelRenderer</c>, <c>ShoulderModelRenderer</c>,
    /// <c>RobeModelRenderer</c> and <c>AppearancePreview</c> Feature classes.
    /// </summary>
    public interface IAppearanceRenderStateService
    {
        /// <summary>The creature's own head scale, when a helmet renders through its head.</summary>
        bool TryGetBaseHeadScale(uint creature, out float scale);

        /// <summary>Whether a base head scale is held for the creature.</summary>
        bool HasBaseHeadScale(uint creature);

        /// <summary>The held base head scale. Throws when none is held.</summary>
        float GetBaseHeadScale(uint creature);

        /// <summary>Holds (or replaces) the base head scale of a creature.</summary>
        void SetBaseHeadScale(uint creature, float scale);

        /// <summary>Drops the held base head scale of every invalid creature once the table is large.</summary>
        void PruneBaseHeadScales();

        /// <summary>Drops and returns the held base head scale; false when none was held.</summary>
        bool RemoveBaseHeadScale(uint creature, out float scale);

        NeckModelCatalog GetNeckCatalog();

        void SetNeckCatalog(NeckModelCatalog catalog);

        bool TryGetNeckProjection(uint creature, out (uint Item, ushort Source, ushort Render) projection);

        void RemoveNeckProjection(uint creature);

        /// <summary>Holds the projection, first dropping the entries of invalid creatures once the table is large.</summary>
        void StoreNeckProjection(uint creature, (uint Item, ushort Source, ushort Render) projection);

        PartSkinModelCatalog GetPartSkinCatalog();

        void SetPartSkinCatalog(PartSkinModelCatalog catalog);

        bool TryGetPartSkinProjection(uint creature, AppearanceArmor part, out (uint Item, ushort Source, ushort Render) projection);

        bool RemovePartSkinProjection(uint creature, AppearanceArmor part);

        void StorePartSkinProjection(uint creature, AppearanceArmor part, (uint Item, ushort Source, ushort Render) projection);

        /// <summary>Drops the entries of invalid creatures once the table reaches its prune size, which then grows.</summary>
        void PrunePartSkinProjections();

        ShoulderModelCatalog GetShoulderCatalog();

        void SetShoulderCatalog(ShoulderModelCatalog catalog);

        bool TryGetShoulderProjection(uint creature, AppearanceArmor part, out (uint Item, ushort Source, ushort Render) projection);

        void RemoveShoulderProjection(uint creature, AppearanceArmor part);

        void StoreShoulderProjection(uint creature, AppearanceArmor part, (uint Item, ushort Source, ushort Render) projection);

        /// <summary>Drops the entries of invalid creatures once the table is large.</summary>
        void PruneShoulderProjections();

        RobeModelCatalog GetRobeCatalog();

        void SetRobeCatalog(RobeModelCatalog catalog);

        /// <summary>Whether the appearance editor preview picture with this resref exists in the loaded haks.</summary>
        bool PictureExists(string resref);
    }
}
