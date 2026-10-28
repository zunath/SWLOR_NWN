using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Exploration sites (plan section 24): what a signature becomes at 100%. A combat site is a squad of the area's
    /// tier, a gas site a cluster of gas clouds, a derelict a salvageable wreck with its defenses, and a cache site a
    /// hidden cache. Once a site's objectives are done its site cache appears; taking the cache completes the site.
    /// Sites despawn once completed or after <see cref="ScanningTuning.SiteLifetimeSeconds"/>.
    /// </summary>
    public static partial class Space
    {
        private const string SiteSignatureVariable = "SCAN_SIGNATURE_ID";
        private const float SquadRingMeters = 10f;
        private const float CloudRingMeters = 12f;
        private const float DefenseRingMeters = 15f;

        /// <summary>Site ships still in a fight when their site ended; each is removed once nobody fights it.</summary>
        private static readonly HashSet<uint> _lingeringSiteShips = new();

        /// <summary>Opens a signature's site where it lies: its ships, clouds, wreck or cache spawn now.</summary>
        private static void RevealSite(ScanSignature signature, uint revealer)
        {
            var area = GetArea(revealer);
            signature.RevealedAt = DateTime.UtcNow;
            var center = Vector3(signature.X, signature.Y, signature.Z);

            switch (signature.Type)
            {
                case SignatureType.CombatSite:
                    SpawnSiteRing(signature, area, center, SignatureSiteRules.CombatSquad(signature.Tier, signature.WildSpace), ObjectType.Creature, SquadRingMeters);
                    break;
                case SignatureType.GasSite:
                    var clouds = Enumerable.Repeat(SignatureSiteRules.GasCloud(signature.Tier), ScanningTuning.GasSiteClouds).ToList();
                    SpawnSiteRing(signature, area, center, clouds, ObjectType.Placeable, CloudRingMeters);
                    break;
                case SignatureType.Derelict:
                    var wreck = CreateWreck(Location(area, center, Random.Next(360)), "Derelict Wreck", signature.Tier,
                        ScanningTuning.WreckSize, ScanningTuning.WreckDifficultyLevel, GetScanPartyIds(revealer),
                        ScanningTuning.SiteLifetimeSeconds);
                    if (GetIsObjectValid(wreck))
                        signature.Objectives.Add(wreck);
                    SpawnSiteRing(signature, area, center, SignatureSiteRules.DerelictDefenses(signature.Tier), ObjectType.Creature, DefenseRingMeters);
                    break;
                case SignatureType.CacheSite:
                    SpawnSiteCache(signature, area, "Hidden Cache");
                    if (signature.Locked && GetIsObjectValid(signature.Cache))
                        LockCache(signature.Cache, signature, area);
                    break;
                case SignatureType.DataSite:
                    SpawnDataCans(signature, area, center);
                    if (signature.IsAgentLead)
                        FactionAgents.HoldHeistSite(signature, area, center);
                    break;
                case SignatureType.Rift:
                    // A rift spawns nothing: the party that located it crosses it with Jump to Signature.
                    break;
            }

            Log.Write(LogGroup.Space, $"SIGNATURE REVEALED - {PlayerName.GetAuditName(revealer)} - {signature.Type} T{signature.Tier} in {signature.AreaResref}");
        }

        /// <summary>Spawns a ring of site objects (ships, clouds) around a site's center as objectives of its signature.</summary>
        public static void SpawnSiteObjectives(ScanSignature signature, uint area, Vector3 center, IReadOnlyList<string> resrefs,
            ObjectType type, float radius)
        {
            SpawnSiteRing(signature, area, center, resrefs, type, radius);
        }

        private static void SpawnSiteRing(ScanSignature signature, uint area, Vector3 center, IReadOnlyList<string> resrefs,
            ObjectType type, float radius)
        {
            for (var index = 0; index < resrefs.Count; index++)
            {
                var (x, y) = SignatureSiteRules.RingOffset(index, resrefs.Count, radius);
                var spawned = CreateObject(type, resrefs[index], Location(area, Vector3(center.X + x, center.Y + y, center.Z), Random.Next(360)));
                if (!GetIsObjectValid(spawned))
                    continue;

                SetLocalString(spawned, SiteSignatureVariable, signature.Id);
                signature.Objectives.Add(spawned);
            }
        }

        private static void SpawnSiteCache(ScanSignature signature, uint area, string name)
        {
            signature.CacheSpawned = true;
            var cache = CreateObject(ObjectType.Placeable, ScanningTuning.CacheResref,
                Location(area, Vector3(signature.X, signature.Y, signature.Z), Random.Next(360)));
            if (!GetIsObjectValid(cache))
            {
                signature.Completed = true;
                return;
            }

            SetName(cache, name);
            SetLocalString(cache, SiteSignatureVariable, signature.Id);
            SetEventScript(cache, EventScript.Placeable_OnUsed, ScriptName.OnSiteCacheUsed);
            signature.Cache = cache;
            signature.Objectives.Add(cache);
        }

        private static bool IsObjectiveLeft(uint objective)
        {
            if (!GetIsObjectValid(objective))
                return false;

            if (GetObjectType(objective) == ObjectType.Placeable && GetResRef(objective) is WreckResref or CapitalWreckResref)
                return GetWreck(objective) != null;

            return !GetIsDead(objective);
        }

        /// <summary>Drops finished objectives and spawns the site cache once they are all done.</summary>
        private static void UpdateSite(ScanSignature signature)
        {
            signature.Objectives.RemoveAll(x => x != signature.Cache && !IsObjectiveLeft(x));
            if (signature.CacheSpawned && !IsObjectiveLeft(signature.Cache))
            {
                signature.Completed = true;
                return;
            }

            if (!SignatureSiteRules.ShouldSpawnCache(signature, signature.Objectives.Count))
                return;

            var area = Area.GetAreaByResref(signature.AreaResref);
            if (GetIsObjectValid(area))
                SpawnSiteCache(signature, area, "Site Cache");
        }

        /// <summary>Removes what is left of a site. Ships still in a fight stay until nobody fights them.</summary>
        private static void EndSite(ScanSignature signature)
        {
            foreach (var objective in signature.Objectives.Where(GetIsObjectValid).ToList())
            {
                if (GetObjectType(objective) == ObjectType.Creature)
                {
                    if (!GetIsDead(objective))
                        _lingeringSiteShips.Add(objective);
                }
                else if (GetWreck(objective) != null)
                {
                    RemoveWreck(objective);
                }
                else
                {
                    DestroyObject(objective);
                }
            }

            signature.Objectives.Clear();
            ProcessLingeringSiteShips();
        }

        private static void ProcessLingeringSiteShips()
        {
            foreach (var ship in _lingeringSiteShips.ToList())
            {
                if (!GetIsObjectValid(ship) || GetIsDead(ship))
                {
                    _lingeringSiteShips.Remove(ship);
                    continue;
                }

                if (Enmity.GetEnmityTable(ship).Count > 0)
                    continue;

                _lingeringSiteShips.Remove(ship);
                DestroyObject(ship);
            }
        }

        /// <summary>A pilot opens a site cache: its credits and items go to them, and the site is complete.</summary>
        [NWNEventHandler(ScriptName.OnSiteCacheUsed)]
        public static void OpenSiteCache()
        {
            OpenSiteCache(OBJECT_SELF, GetLastUsedBy());
        }

        /// <summary>Opens a site cache for a user, or tells them why they cannot.</summary>
        public static void OpenSiteCache(uint cache, uint user)
        {
            if (!GetIsPC(user) || !GetIsObjectValid(cache))
                return;

            if (!IsPlayerInSpaceMode(user))
            {
                SendMessageToPC(user, ColorToken.Red("Only a ship can open a site cache."));
                return;
            }

            var signature = FindSignature(GetLocalString(cache, SiteSignatureVariable));
            if (signature == null || signature.Completed)
            {
                DestroyObject(cache);
                return;
            }

            if (IsLockedCache(cache))
            {
                SendMessageToPC(user, ColorToken.Red("The cache is locked. Target it and slice it open with a Data Analyzer."));
                return;
            }

            if (ScanningRules.PartyPercent(signature, GetScanPartyIds(user)) < ScanningTuning.InfoLocatedPercent)
            {
                SendMessageToPC(user, ColorToken.Red("The cache is keyed to the ships that scanned this site. Scan it yourself to open it."));
                return;
            }

            signature.Completed = true;
            FactionAgents.OnSiteCompleted(signature, user, cache);
            var credits = SignatureSiteRules.SiteCredits(signature.Type, signature.Tier);
            if (credits > 0)
                GiveGoldToCreature(user, credits);

            var table = Loot.GetLootTableByName(SignatureSiteRules.CacheLootTable(signature.Tier, signature.WildSpace));
            var items = Enumerable.Range(0, ScanningTuning.CacheItemPulls).Select(_ => table.GetRandomItem().Resref).ToList();
            ShipHold.DeliverYield(user, items);

            SendMessageToPC(user, ColorToken.Green($"You open the {GetName(cache)}: {credits} credits."));
            Log.Write(LogGroup.Space, $"SITE CACHE - {PlayerName.GetAuditName(user)} - {signature.Type} T{signature.Tier} in {signature.AreaResref} - {credits} credits, {string.Join(", ", items)}");
            DestroyObject(cache);
        }
    }
}
