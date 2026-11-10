using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class HeadLodColorRefresh
    {
        private const float RefreshSeconds = 0.5f;
        private const int MaximumSequence = 4095;
        private static readonly Dictionary<uint, HeadLodColorRefreshSession> Sessions = new();
        private static readonly Dictionary<string, bool> LodModels = new(StringComparer.OrdinalIgnoreCase);

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Clear()
        {
            foreach (var session in Sessions.Values)
                session.Subscription?.Dispose();
            Sessions.Clear();
            LodModels.Clear();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnExit() => Stop(GetExitingObject());

        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void OnDestroyed() => Stop(OBJECT_SELF);

        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void OnHelmetUnequipped()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var creature = OBJECT_SELF;
            if (!GetIsObjectValid(item) || GetBaseItemType(item) != BaseItem.Helmet ||
                !GetIsObjectValid(creature))
                return;

            var selections = TintMapModelResolver.GetCurrentSelections(creature);
            Update(creature, selections);
            if (Sessions.ContainsKey(creature))
                HelmetModelRenderer.Apply(creature, selections);
        }

        public static void Update(uint creature, IReadOnlyList<TintMapMaterialSelection> selections)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature ||
                !RacialAppearanceRegistry.TryGet(GetAppearanceType(creature), out _) ||
                RobeModelRenderer.GetBasePhenotype(creature) != 0 || HasVisibleHelmet(creature))
            {
                Stop(creature);
                return;
            }

            var otherMaterials = selections.Where(selection => !HeadTintChannels.IsHead(selection))
                .Select(selection => selection.Material.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var heads = selections.Where(selection => HeadTintChannels.IsHead(selection) &&
                IsNormalHead(selection.ModelResref) && !otherMaterials.Contains(selection.Material.Resref)).ToArray();
            if (heads.Length == 0 || !heads.All(selection =>
                    selection.ModelResref.Equals(heads[0].ModelResref, StringComparison.OrdinalIgnoreCase)) ||
                !HasLod(heads[0].ModelResref) ||
                int.Parse(heads[0].ModelResref.Substring(9)) != GetCreatureBodyPart(CreaturePart.Head, creature))
            {
                Stop(creature);
                return;
            }

            var uuid = GetObjectUUID(creature);
            var actual = creature;

            if (Sessions.TryGetValue(actual, out var current) && Matches(current) &&
                current.Model.Equals(heads[0].ModelResref, StringComparison.OrdinalIgnoreCase))
            {
                current.Selections = heads;
                Refresh(current);
                return;
            }

            Stop(actual);
            var session = new HeadLodColorRefreshSession
            {
                Creature = actual,
                Uuid = uuid,
                Model = heads[0].ModelResref,
                Appearance = GetAppearanceType(actual),
                Gender = GetGender(actual),
                Head = GetCreatureBodyPart(CreaturePart.Head, actual),
                Selections = heads
            };
            Sessions[actual] = session;
            session.Subscription = Scheduler.ScheduleRepeating(() => Tick(session), TimeSpan.FromSeconds(RefreshSeconds));
            Refresh(session);
        }

        private static void Tick(HeadLodColorRefreshSession session)
        {
            if (!Sessions.TryGetValue(session.Creature, out var current) || !ReferenceEquals(current, session))
                return;
            if (!Matches(session))
            {
                Stop(session.Creature);
                return;
            }
            Refresh(session);
        }

        private static void Refresh(HeadLodColorRefreshSession session)
        {
            session.Sequence = session.Sequence >= MaximumSequence ? 1 : session.Sequence + 1;
            try
            {
                TintMapService.RefreshLodHeadColors(session.Creature, session.Selections, session.Sequence);
            }
            catch
            {
                Stop(session.Creature);
                throw;
            }
        }

        private static bool Matches(HeadLodColorRefreshSession session) =>
            GetIsObjectValid(session.Creature) && GetObjectType(session.Creature) == ObjectType.Creature &&
            GetObjectUUID(session.Creature) == session.Uuid &&
            GetAppearanceType(session.Creature) == session.Appearance && GetGender(session.Creature) == session.Gender &&
            GetCreatureBodyPart(CreaturePart.Head, session.Creature) == session.Head &&
            RobeModelRenderer.GetBasePhenotype(session.Creature) == 0 && !HasVisibleHelmet(session.Creature);

        private static bool HasVisibleHelmet(uint creature)
        {
            var helmet = GetItemInSlot(InventorySlot.Head, creature);
            return GetIsObjectValid(helmet) && HelmetModelRenderer.IsShownByOwner(helmet);
        }

        private static bool HasLod(string model)
        {
            if (!LodModels.TryGetValue(model, out var exists))
                LodModels[model] = exists = !string.IsNullOrEmpty(ResManGetAliasFor(model, ResType.LOD));
            return exists;
        }

        private static bool IsNormalHead(string model)
        {
            var name = model.ToLowerInvariant();
            return name.Length == 12 && name[0] == 'p' && (name[1] == 'm' || name[1] == 'f') &&
                "heodz".Contains(name[2]) && name[3] == '0' &&
                name.Skip(9).All(character => character >= '0' && character <= '9');
        }

        private static void Stop(uint creature)
        {
            if (Sessions.Count == 0)
                return;
            if (Sessions.Remove(creature, out var session))
                session.Subscription?.Dispose();
        }
    }
}
