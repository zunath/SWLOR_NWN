using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The runtime state of the appearance renderers. The state lives in <see cref="IAppearanceRenderStateService"/>;
    /// every member forwards to the container's instance.
    /// </summary>
    public static class AppearanceRenderState
    {
        private static IAppearanceRenderStateService Instance => FacadeAccessor.Resolve<IAppearanceRenderStateService>();

        public static bool TryGetBaseHeadScale(uint creature, out float scale) =>
            Instance.TryGetBaseHeadScale(creature, out scale);

        public static bool HasBaseHeadScale(uint creature) =>
            Instance.HasBaseHeadScale(creature);

        public static float GetBaseHeadScale(uint creature) =>
            Instance.GetBaseHeadScale(creature);

        public static void SetBaseHeadScale(uint creature, float scale) =>
            Instance.SetBaseHeadScale(creature, scale);

        public static void PruneBaseHeadScales() =>
            Instance.PruneBaseHeadScales();

        public static bool RemoveBaseHeadScale(uint creature, out float scale) =>
            Instance.RemoveBaseHeadScale(creature, out scale);

        public static NeckModelCatalog GetNeckCatalog() =>
            Instance.GetNeckCatalog();

        public static void SetNeckCatalog(NeckModelCatalog catalog) =>
            Instance.SetNeckCatalog(catalog);

        public static bool TryGetNeckProjection(uint creature, out (uint Item, ushort Source, ushort Render) projection) =>
            Instance.TryGetNeckProjection(creature, out projection);

        public static void RemoveNeckProjection(uint creature) =>
            Instance.RemoveNeckProjection(creature);

        public static void StoreNeckProjection(uint creature, (uint Item, ushort Source, ushort Render) projection) =>
            Instance.StoreNeckProjection(creature, projection);

        public static PartSkinModelCatalog GetPartSkinCatalog() =>
            Instance.GetPartSkinCatalog();

        public static void SetPartSkinCatalog(PartSkinModelCatalog catalog) =>
            Instance.SetPartSkinCatalog(catalog);

        public static bool TryGetPartSkinProjection(
            uint creature, AppearanceArmor part, out (uint Item, ushort Source, ushort Render) projection) =>
            Instance.TryGetPartSkinProjection(creature, part, out projection);

        public static bool RemovePartSkinProjection(uint creature, AppearanceArmor part) =>
            Instance.RemovePartSkinProjection(creature, part);

        public static void StorePartSkinProjection(
            uint creature, AppearanceArmor part, (uint Item, ushort Source, ushort Render) projection) =>
            Instance.StorePartSkinProjection(creature, part, projection);

        public static void PrunePartSkinProjections() =>
            Instance.PrunePartSkinProjections();

        public static ShoulderModelCatalog GetShoulderCatalog() =>
            Instance.GetShoulderCatalog();

        public static void SetShoulderCatalog(ShoulderModelCatalog catalog) =>
            Instance.SetShoulderCatalog(catalog);

        public static bool TryGetShoulderProjection(
            uint creature, AppearanceArmor part, out (uint Item, ushort Source, ushort Render) projection) =>
            Instance.TryGetShoulderProjection(creature, part, out projection);

        public static void RemoveShoulderProjection(uint creature, AppearanceArmor part) =>
            Instance.RemoveShoulderProjection(creature, part);

        public static void StoreShoulderProjection(
            uint creature, AppearanceArmor part, (uint Item, ushort Source, ushort Render) projection) =>
            Instance.StoreShoulderProjection(creature, part, projection);

        public static void PruneShoulderProjections() =>
            Instance.PruneShoulderProjections();

        public static RobeModelCatalog GetRobeCatalog() =>
            Instance.GetRobeCatalog();

        public static void SetRobeCatalog(RobeModelCatalog catalog) =>
            Instance.SetRobeCatalog(catalog);

        public static bool PictureExists(string resref) =>
            Instance.PictureExists(resref);
    }
}
