using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Lawful ships (plan sections 11 and 13): NPC trade convoys and customs patrols on the Neutral faction. A convoy is
    /// a hauler with escorts flying to one of the system's starlanes, where it jumps out. A pilot who fires on a lawful
    /// ship turns its whole group hostile and loses standing with its authority; destroying a hauler spills its cargo.
    /// </summary>
    public static partial class Trade
    {
        private const string LawfulGroupVariable = "TRADE_LAWFUL_GROUP";
        private const string LawfulAuthorityVariable = "TRADE_LAWFUL_AUTHORITY";
        private const string LawfulKindVariable = "TRADE_LAWFUL_KIND";
        private const string StarlaneTag = "tele_obj";
        private const string StarlaneDestinationVariable = "DESTINATION";
        private const float EscortSpacingMeters = 6f;

        private static readonly Dictionary<string, List<uint>> _lawfulGroups = new();
        private static readonly HashSet<(string Group, uint Player)> _lawfulOffenders = new();

        /// <summary>Starts a trade convoy of this tier at a location (DM tools); the hauler sets itself up as it spawns.</summary>
        public static bool StartConvoy(Location location, int tier)
        {
            var hauler = CreateObject(ObjectType.Creature, ConvoyRules.GetHaulerTag(tier), location);
            return GetIsObjectValid(hauler);
        }

        /// <summary>
        /// Starts a convoy for a faction agent's escort mission (plan section 29) and returns its hauler, or OBJECT_INVALID.
        /// The hauler sets itself up as it spawns, as every convoy does.
        /// </summary>
        public static uint StartEscortConvoy(Location location, int tier)
        {
            var hauler = CreateObject(ObjectType.Creature, ConvoyRules.GetHaulerTag(tier), location);
            return GetIsObjectValid(hauler) ? hauler : OBJECT_INVALID;
        }

        /// <summary>Whether a ship flies for a lawful authority: a trade convoy or a customs patrol.</summary>
        public static bool IsLawfulShip(uint ship)
        {
            return !string.IsNullOrEmpty(GetLocalString(ship, LawfulGroupVariable));
        }

        /// <summary>
        /// Pirate raiders of the tier strike an escorted convoy: they spawn around its hauler, go for it and the escorting
        /// pilot, and the convoy fights back. They leave once idle, as ambushers do.
        /// </summary>
        public static int SpawnConvoyRaiders(uint hauler, int tier, int size, uint pilot)
        {
            var group = GetLocalString(hauler, LawfulGroupVariable);
            var convoy = _lawfulGroups.TryGetValue(group, out var members) ? members.Where(x => GetIsObjectValid(x) && !GetIsDead(x)).ToList() : new List<uint> { hauler };
            var raiders = SpawnAround(hauler, PirateAmbushRules.GetSquad(tier, size), AmbushRangeMeters);
            foreach (var raider in raiders)
            {
                foreach (var ship in convoy)
                {
                    SetIsTemporaryEnemy(raider, ship);
                    SetIsTemporaryEnemy(ship, raider);
                    Enmity.ModifyEnmity(raider, ship, 50);
                }

                Enmity.ModifyEnmity(hauler, raider, 100);
                if (GetIsObjectValid(pilot))
                    Enmity.ModifyEnmity(pilot, raider, 50);
                DespawnWhenIdle(raider, TradeTuning.AmbushSeconds);
            }

            return raiders.Count;
        }

        /// <summary>A convoy hauler, from a spawn table or the DM tools, loads its cargo and calls its escorts.</summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void InitializeConvoyOnSpawn()
        {
            var hauler = OBJECT_SELF;
            var tier = ConvoyRules.GetHaulerTier(GetTag(hauler));
            if (tier <= 0 || !string.IsNullOrEmpty(GetLocalString(hauler, LawfulGroupVariable)))
                return;

            InitializeConvoy(hauler, tier);
        }

        private static void InitializeConvoy(uint hauler, int tier)
        {
            var area = GetArea(hauler);
            var areaResref = GetResRef(area);
            var authority = ConvoyRules.GetAuthority(TradeLookup.GetOrbit(areaResref));
            var cargo = ConvoyRules.BuildCargo(tier, areaResref, new System.Random(Random.Next()));
            WriteUnits(hauler, cargo);

            PruneLawfulGroups();
            var group = Guid.NewGuid().ToString();
            var members = new List<uint> { hauler };
            MarkLawful(hauler, group, authority, LawfulShipKind.Convoy);

            var position = GetPosition(hauler);
            var facing = GetFacing(hauler);
            for (var index = 0; index < ConvoyRules.GetEscortCount(tier); index++)
            {
                var angle = facing + 150f + 60f * index;
                var spot = Vector3(
                    position.X + EscortSpacingMeters * (float)Math.Cos(angle * Math.PI / 180),
                    position.Y + EscortSpacingMeters * (float)Math.Sin(angle * Math.PI / 180),
                    position.Z);
                var escort = CreateObject(ObjectType.Creature, ConvoyRules.GetEscortTag(tier), Location(area, spot, facing));
                if (!GetIsObjectValid(escort))
                    continue;

                MarkLawful(escort, group, authority, LawfulShipKind.Convoy);
                AssignCommand(escort, () => ActionForceFollowObject(hauler, EscortSpacingMeters));
                members.Add(escort);
            }

            _lawfulGroups[group] = members;

            var starlane = PickStarlane(area, hauler);
            if (GetIsObjectValid(starlane))
            {
                AssignCommand(hauler, () =>
                {
                    ActionMoveToObject(starlane, false, 5f);
                    ActionDoCommand(() =>
                    {
                        FactionAgents.OnConvoyArrived(hauler);
                        DismissLawfulGroup(group, 0f);
                    });
                });
            }

            foreach (var member in members)
                DespawnWhenIdle(member, TradeTuning.ConvoyLifetimeSeconds);

            Log.Write(LogGroup.Space, $"CONVOY - {authority} convoy of {members.Count} ships in {areaResref} carrying {CustomsRules.Describe(cargo)}");
        }

        /// <summary>A random starlane of the area other than the one nearest the hauler, or the nearest when it is the only one.</summary>
        private static uint PickStarlane(uint area, uint hauler)
        {
            var starlanes = new List<uint>();
            for (var obj = GetFirstObjectInArea(area, ObjectType.Placeable); GetIsObjectValid(obj); obj = GetNextObjectInArea(area, ObjectType.Placeable))
            {
                if (GetTag(obj) == StarlaneTag && !string.IsNullOrWhiteSpace(GetLocalString(obj, StarlaneDestinationVariable)))
                    starlanes.Add(obj);
            }

            if (starlanes.Count == 0)
                return OBJECT_INVALID;

            var nearest = starlanes.OrderBy(x => GetDistanceBetween(x, hauler)).First();
            var others = starlanes.Where(x => x != nearest).ToList();
            return others.Count == 0 ? nearest : others[Random.Next(others.Count)];
        }

        private static void MarkLawful(uint ship, string group, TradeAuthority authority, LawfulShipKind kind)
        {
            SetLocalString(ship, LawfulGroupVariable, group);
            SetLocalInt(ship, LawfulAuthorityVariable, (int)authority);
            SetLocalInt(ship, LawfulKindVariable, (int)kind);
        }

        /// <summary>
        /// Called when a ship damages another (Space.ApplyShipDamage and ApplyHullDamage). A player's first attack on a
        /// lawful group turns the whole group on them and costs standing with its authority.
        /// </summary>
        public static void OnShipAttacked(uint attacker, uint target)
        {
            if (!GetIsPC(attacker) || GetIsPC(target))
                return;

            var group = GetLocalString(target, LawfulGroupVariable);
            if (string.IsNullOrEmpty(group) || !_lawfulOffenders.Add((group, attacker)))
                return;

            var authority = (TradeAuthority)GetLocalInt(target, LawfulAuthorityVariable);
            var kind = (LawfulShipKind)GetLocalInt(target, LawfulKindVariable);
            TurnLawfulGroupHostile(group, attacker);

            // The Hutt Cartel's enforcers answer to the Cartel's standing, not to a customs authority.
            if (kind == LawfulShipKind.HuttEnforcer)
            {
                RecordStanding(attacker, HuttEnforcerRules.Faction, TradeStandingEvent.AttackedLawfulShip);
                SendMessageToPC(attacker, ColorToken.Red("You fire on a Hutt Cartel enforcer. Every enforcer in the system turns on you."));
                Log.Write(LogGroup.Space, $"LAWFUL ATTACK - {PlayerName.GetAuditName(attacker)} attacked a Hutt Cartel enforcer in {GetResRef(GetArea(target))}");
                return;
            }

            var convoy = kind == LawfulShipKind.Convoy;
            RecordStanding(attacker, authority, convoy ? TradeStandingEvent.RaidedConvoy : TradeStandingEvent.AttackedLawfulShip);
            SendMessageToPC(attacker, ColorToken.Red(convoy
                ? $"You attack a {CustomsRules.GetAuthorityName(authority)} trade convoy. Its escorts turn on you."
                : $"You fire on a {CustomsRules.GetAuthorityName(authority)} customs patrol. It turns on you."));
            Log.Write(LogGroup.Space, $"LAWFUL ATTACK - {PlayerName.GetAuditName(attacker)} attacked a {authority} {kind} in {GetResRef(GetArea(target))}");
        }

        private static void TurnLawfulGroupHostile(string group, uint player)
        {
            if (!_lawfulGroups.TryGetValue(group, out var members))
                return;

            foreach (var member in members.Where(x => GetIsObjectValid(x) && !GetIsDead(x)))
                TurnLawfulShipHostile(member, player);
        }

        private static void TurnLawfulShipHostile(uint ship, uint player)
        {
            SetIsTemporaryEnemy(player, ship);
            SetIsTemporaryEnemy(ship, player);
            Enmity.ModifyEnmity(player, ship, 100);
        }

        /// <summary>Removes a lawful group's ships after a delay (a convoy jumping out, a patrol leaving).</summary>
        private static void DismissLawfulGroup(string group, float delaySeconds)
        {
            if (!_lawfulGroups.TryGetValue(group, out var members))
                return;

            _lawfulGroups.Remove(group);
            _lawfulOffenders.RemoveWhere(x => x.Group == group);
            foreach (var member in members.Where(GetIsObjectValid))
                DestroyObject(member, delaySeconds);
        }

        /// <summary>Forgets the groups whose ships are all gone (destroyed, despawned or jumped out).</summary>
        private static void PruneLawfulGroups()
        {
            foreach (var group in _lawfulGroups.Where(x => !x.Value.Any(GetIsObjectValid)).Select(x => x.Key).ToList())
            {
                _lawfulGroups.Remove(group);
                _lawfulOffenders.RemoveWhere(x => x.Group == group);
            }
        }

        /// <summary>A destroyed convoy hauler spills its cargo as cargo pods.</summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void SpillConvoyCargo()
        {
            var hauler = OBJECT_SELF;
            if (ConvoyRules.GetHaulerTier(GetTag(hauler)) <= 0)
                return;

            var units = ReadUnits(hauler);
            WriteUnits(hauler, new Dictionary<CommodityType, int>());
            var spilled = SpillUnits(units, GetLocation(hauler));
            Log.Write(LogGroup.Space, $"CONVOY RAIDED - hauler in {GetResRef(GetArea(hauler))} spilled {spilled} units ({CustomsRules.Describe(units)})");
        }
    }
}
