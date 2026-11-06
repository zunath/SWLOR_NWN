using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The state that used to be static in the appearance renderers and <c>AppearancePreview</c>, moved here unchanged:
    /// the same tables, thresholds and prune rules. The engine calls (is this object valid, does this resource exist)
    /// sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class AppearanceRenderStateService : IAppearanceRenderStateService
    {
        private const int BaseHeadScalePruneThreshold = 1024;
        private const int NeckProjectionPruneThreshold = 1024;
        private const int ShoulderProjectionPruneThreshold = 2048;
        private const int PartSkinProjectionMinimumPruneAt = 1024;

        // Creature head scales captured while a helmet renders through the head. Runtime only:
        // a fresh login or spawn starts unprojected, so a persisted value is never compounded.
        private readonly Dictionary<uint, float> _baseHeadScales = new();

        private NeckModelCatalog _neckCatalog = new(Array.Empty<(string, string, string)>());
        private readonly Dictionary<uint, (uint Item, ushort Source, ushort Render)> _neckProjections = new();

        private PartSkinModelCatalog _partSkinCatalog = new(
            Array.Empty<(string, string)>(), Array.Empty<(string, string, string)>());
        // Holds only parts that currently render through an alias.
        private readonly Dictionary<(uint Creature, AppearanceArmor Part), (uint Item, ushort Source, ushort Render)> _partSkinProjections = new();
        private int _partSkinPruneAt = PartSkinProjectionMinimumPruneAt;

        private ShoulderModelCatalog _shoulderCatalog = new(Array.Empty<(string, string, string)>());
        private readonly Dictionary<(uint Creature, AppearanceArmor Part), (uint Item, ushort Source, ushort Render)> _shoulderProjections = new();

        private RobeModelCatalog _robeCatalog = new(Array.Empty<(string, int, int)>());

        private readonly ConcurrentDictionary<string, bool> _pictureExists = new(StringComparer.OrdinalIgnoreCase);

        protected virtual bool IsObjectValid(uint obj) => GetIsObjectValid(obj);

        protected virtual bool ResourceExists(string resref) =>
            !string.IsNullOrEmpty(ResManGetAliasFor(resref, ResType.DDS));

        public bool TryGetBaseHeadScale(uint creature, out float scale) =>
            _baseHeadScales.TryGetValue(creature, out scale);

        public bool HasBaseHeadScale(uint creature) => _baseHeadScales.ContainsKey(creature);

        public float GetBaseHeadScale(uint creature) => _baseHeadScales[creature];

        public void SetBaseHeadScale(uint creature, float scale) => _baseHeadScales[creature] = scale;

        public void PruneBaseHeadScales()
        {
            if (_baseHeadScales.Count < BaseHeadScalePruneThreshold)
                return;

            foreach (var stale in _baseHeadScales.Keys.Where(key => !IsObjectValid(key)).ToList())
                _baseHeadScales.Remove(stale);
        }

        public bool RemoveBaseHeadScale(uint creature, out float scale) =>
            _baseHeadScales.Remove(creature, out scale);

        public NeckModelCatalog GetNeckCatalog() => _neckCatalog;

        public void SetNeckCatalog(NeckModelCatalog catalog) => _neckCatalog = catalog;

        public bool TryGetNeckProjection(uint creature, out (uint Item, ushort Source, ushort Render) projection) =>
            _neckProjections.TryGetValue(creature, out projection);

        public void RemoveNeckProjection(uint creature) => _neckProjections.Remove(creature);

        public void StoreNeckProjection(uint creature, (uint Item, ushort Source, ushort Render) projection)
        {
            if (_neckProjections.Count >= NeckProjectionPruneThreshold)
                foreach (var stale in _neckProjections.Keys.Where(id => !IsObjectValid(id)).ToArray())
                    _neckProjections.Remove(stale);
            _neckProjections[creature] = projection;
        }

        public PartSkinModelCatalog GetPartSkinCatalog() => _partSkinCatalog;

        public void SetPartSkinCatalog(PartSkinModelCatalog catalog) => _partSkinCatalog = catalog;

        public bool TryGetPartSkinProjection(
            uint creature, AppearanceArmor part, out (uint Item, ushort Source, ushort Render) projection) =>
            _partSkinProjections.TryGetValue((creature, part), out projection);

        public bool RemovePartSkinProjection(uint creature, AppearanceArmor part) =>
            _partSkinProjections.Remove((creature, part));

        public void StorePartSkinProjection(
            uint creature, AppearanceArmor part, (uint Item, ushort Source, ushort Render) projection) =>
            _partSkinProjections[(creature, part)] = projection;

        public void PrunePartSkinProjections()
        {
            if (_partSkinProjections.Count < _partSkinPruneAt)
                return;

            foreach (var key in _partSkinProjections.Keys.Where(key => !IsObjectValid(key.Creature)).ToArray())
                _partSkinProjections.Remove(key);
            // Grow past the live population so a full cache is not rescanned on every call.
            _partSkinPruneAt = Math.Max(PartSkinProjectionMinimumPruneAt, _partSkinProjections.Count * 2);
        }

        public ShoulderModelCatalog GetShoulderCatalog() => _shoulderCatalog;

        public void SetShoulderCatalog(ShoulderModelCatalog catalog) => _shoulderCatalog = catalog;

        public bool TryGetShoulderProjection(
            uint creature, AppearanceArmor part, out (uint Item, ushort Source, ushort Render) projection) =>
            _shoulderProjections.TryGetValue((creature, part), out projection);

        public void RemoveShoulderProjection(uint creature, AppearanceArmor part) =>
            _shoulderProjections.Remove((creature, part));

        public void StoreShoulderProjection(
            uint creature, AppearanceArmor part, (uint Item, ushort Source, ushort Render) projection) =>
            _shoulderProjections[(creature, part)] = projection;

        public void PruneShoulderProjections()
        {
            if (_shoulderProjections.Count < ShoulderProjectionPruneThreshold)
                return;

            foreach (var key in _shoulderProjections.Keys.Where(key => !IsObjectValid(key.Creature)).ToArray())
                _shoulderProjections.Remove(key);
        }

        public RobeModelCatalog GetRobeCatalog() => _robeCatalog;

        public void SetRobeCatalog(RobeModelCatalog catalog) => _robeCatalog = catalog;

        public bool PictureExists(string resref) =>
            _pictureExists.GetOrAdd(resref, static (name, self) => self.ResourceExists(name), this);
    }
}
