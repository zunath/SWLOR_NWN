using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Zone hazards (W11o): standalone minefields and pre-placed wreck sites, held by Module waypoints
    /// (<see cref="SpaceHazardTuning"/>). A minefield keeps its mines armed and re-arms each one some time after it
    /// detonates; a wreck site keeps one salvageable wreck and replaces it some time after it is stripped or expires.
    /// </summary>
    public static partial class Space
    {
        private class HazardSite
        {
            public uint Waypoint { get; init; }
            public int Tier { get; init; }
            public uint[] Objects { get; init; }
            public DateTime[] ReadyAt { get; init; }
        }

        private static List<HazardSite> _minefields;
        private static List<HazardSite> _wreckSites;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleSpaceHazards()
        {
            Scheduler.ScheduleRepeating(ProcessSpaceHazards, TimeSpan.FromSeconds(SpaceHazardTuning.TickSeconds), TimeSpan.FromSeconds(20));
        }

        private static List<HazardSite> FindHazardSites(string tag, int slots)
        {
            var sites = new List<HazardSite>();
            for (var nth = 0; ; nth++)
            {
                var waypoint = GetObjectByTag(tag, nth);
                if (!GetIsObjectValid(waypoint))
                    break;

                sites.Add(new HazardSite
                {
                    Waypoint = waypoint,
                    Tier = SpaceHazardRules.SiteTier(GetLocalInt(waypoint, SpaceHazardTuning.TierVariable)),
                    Objects = Enumerable.Repeat(OBJECT_INVALID, slots).ToArray(),
                    ReadyAt = new DateTime[slots],
                });
            }

            return sites;
        }

        private static void ProcessSpaceHazards()
        {
            try
            {
                _minefields ??= FindHazardSites(SpaceHazardTuning.MinefieldTag, SpaceHazardTuning.MinesPerField);
                _wreckSites ??= FindHazardSites(SpaceHazardTuning.WreckSiteTag, 1);
                var now = DateTime.UtcNow;

                foreach (var field in _minefields)
                {
                    for (var slot = 0; slot < field.Objects.Length; slot++)
                    {
                        if (GetIsObjectValid(field.Objects[slot]))
                            continue;

                        // A detonated mine re-arms after the delay, counted from when its slot was found empty.
                        if (field.ReadyAt[slot] == default)
                            field.ReadyAt[slot] = _hazardsSeeded ? SpaceHazardRules.MineRearmAt(now) : now;

                        if (now < field.ReadyAt[slot])
                            continue;

                        field.ReadyAt[slot] = default;
                        field.Objects[slot] = LayFieldMine(field, slot, now);
                    }
                }

                foreach (var site in _wreckSites)
                {
                    if (GetWreck(site.Objects[0]) != null)
                        continue;

                    if (site.ReadyAt[0] == default)
                        site.ReadyAt[0] = _hazardsSeeded ? SpaceHazardRules.WreckRespawnAt(now) : now;
                    if (now < site.ReadyAt[0])
                        continue;

                    site.ReadyAt[0] = default;
                    site.Objects[0] = CreateWreck(GetLocation(site.Waypoint), SpaceHazardTuning.WreckName, site.Tier,
                        SpaceHazardTuning.WreckSize, SpaceHazardTuning.WreckDifficulty, null,
                        (float)(SpaceHazardTuning.WreckLifetimeMinutes * 60));
                }

                _hazardsSeeded = true;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Space hazard processing failed: {ex}");
            }
        }

        private static bool _hazardsSeeded;

        private static uint LayFieldMine(HazardSite field, int slot, DateTime now)
        {
            var area = GetArea(field.Waypoint);
            var center = GetPosition(field.Waypoint);
            var (x, y) = SignatureSiteRules.RingOffset(slot, field.Objects.Length, SpaceHazardTuning.MineRingMeters);
            var mine = CreateObject(ObjectType.Creature, SpaceMineResref,
                Location(area, Vector3(center.X + x, center.Y + y, center.Z), Random.Next(360)));
            if (!GetIsObjectValid(mine))
                return OBJECT_INVALID;

            _spaceMines.Add(new SpaceMine { Mine = mine, Owner = OBJECT_INVALID, LaidAt = now, Field = true, Tier = field.Tier });
            return mine;
        }

        /// <summary>
        /// A standalone mine hits as a minelayer's mine of its field's tier: the tier Minelayer preset's mine DMG and
        /// launcher Attack, Accuracy and rank, fired by the mine itself.
        /// </summary>
        private static void DetonateFieldMine(SpaceMine mine, uint victim)
        {
            var location = GetLocation(mine.Mine);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), location);

            var minelayer = SpaceHazardRules.MinelayerTag(mine.Tier);
            var victimStatus = GetShipStatus(victim);
            var torpedo = RevampModuleCatalog.Steps.FirstOrDefault(x => x.Family == ShipModuleFamily.TorpedoLauncher);
            if (victimStatus == null || torpedo == null ||
                !_npcPresets.TryGetValue(minelayer, out var preset) ||
                !_spaceObjects.TryGetValue(minelayer, out var detail))
            {
                DestroyObject(mine.Mine);
                return;
            }

            var mineStatus = ShipNpcRules.BuildStatus(preset, detail.ShipItemTag, false, DateTime.UtcNow);
            var launcher = mineStatus.HighPowerModules.Values.Select(x => x.NpcShot).FirstOrDefault(x => x != null);
            var mineShot = new ShipNpcWeapon
            {
                ModuleTag = torpedo.Tag,
                Dmg = preset.MineDmg,
                Attack = launcher?.Attack ?? 0,
                Accuracy = launcher?.Accuracy ?? 0,
                Rank = launcher?.Rank ?? 0,
            };
            var shot = ShipShotBuilder.Build(torpedo, mineStatus.Size, victimStatus.Size, victimStatus.Shield, mineShot);
            shot.IsArea = true;

            var source = mine.Mine;
            AssignCommand(source, () =>
            {
                var result = RollWeaponShot(source, mineStatus, victim, victimStatus, shot);
                CompleteWeaponShot(source, victim, shot, result);
            });
            DestroyObject(source, 1.0f);
        }
    }
}
