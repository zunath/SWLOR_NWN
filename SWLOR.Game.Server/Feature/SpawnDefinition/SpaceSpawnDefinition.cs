using System.Collections.Generic;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.SpawnService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SpawnDefinition
{
    public class SpaceSpawnDefinition : ISpawnListDefinition
    {
        private readonly SpawnTableBuilder _builder = new();
        public Dictionary<string, SpawnTable> BuildSpawnTables()
        {
            ViscaraOrbit();
            MonCalaOrbit();
            HutlarOrbit();
            TatooineOrbit();
            KorribanOrbit();
            DathomirOrbit();
            EshanOrbit();
            DantooineOrbit();
            SmugglersMoonOrbit();
            MidRimNebulaOrbit();
            OuterRimApproachOrbit();
            ViscaraAsteroidBelt();
            MidRimNebula();
            KorribanDebrisField();
            WildSpace();

            return _builder.Build();
        }

        /// <summary>
        /// One ace of the area (plan section 11): a named rare spawn at a ground rare's share of the table (a weight-1
        /// rare beside about 120 weight is 0.8% of spawn rolls; RevampAceCatalog.SpawnWeight beside an orbit's 75,648).
        /// While it is alive, no other rare of the table spawns in that area.
        /// </summary>
        private void AddAce(string tag, int weight = RevampAceCatalog.SpawnWeight)
        {
            _builder.AddSpawn(ObjectType.Creature, tag)
                .WithFrequency(weight)
                .AsRare()
                .RandomlyWalks()
                .ReturnsHome();
        }

        /// <summary>The Sith and Mandalorian capitals at the weights of every open orbit but Korriban's.</summary>
        private static readonly (string Capital, int Weight)[] _rareCapitals =
        {
            ("sithcap1", 96), ("sithcap2", 48), ("sithcap3", 12), ("sithcap4", 12), ("sithcap5", 6),
            ("sithcap6", 2), ("sithcap7", 1), ("mandocap1", 12), ("mandocap2", 2), ("mandocap3", 1)
        };

        /// <summary>The Sith and Mandalorian capitals at Korriban Orbit's weights, where the Sith fleet gathers.</summary>
        private static readonly (string Capital, int Weight)[] _korribanCapitals =
        {
            ("sithcap1", 300), ("sithcap2", 700), ("sithcap3", 12), ("sithcap4", 200), ("sithcap5", 6),
            ("sithcap6", 2), ("sithcap7", 1), ("mandocap1", 12), ("mandocap2", 2), ("mandocap3", 1)
        };

        /// <summary>The Sith and Mandalorian capitals, rarest last (the open-orbit weights unless others are given).</summary>
        private void AddRareCapitals((string Capital, int Weight)[] capitals = null)
        {
            foreach (var (capital, weight) in capitals ?? _rareCapitals)
            {
                _builder.AddSpawn(ObjectType.Creature, capital)
                    .WithFrequency(weight)
                    .RandomlyWalks()
                    .ReturnsHome();
            }
        }

        /// <summary>
        /// Smuggler's Moon orbit (narshadorbit): Viscara Orbit's tier 1 ships and capitals (the table it shared before
        /// its ace), with its own ace.
        /// </summary>
        private void SmugglersMoonOrbit()
        {
            _builder.Create("SPACE_NARSHADDAA_ORBIT", "Space - Smuggler's Moon Orbit");
            AddFactionTier(1);
            AddRareCapitals();
            AddAce("ace_smuggler");
            AddHuttEnforcers();
        }

        /// <summary>
        /// The Hutt Cartel enforcers (plan section 11) on the Smuggler's Moon lanes: Smuggler's Moon Orbit and the orbits
        /// its starlanes reach (Hutlar and Tatooine). Their tags and weight are trade.py's (TradeTuning).
        /// </summary>
        private void AddHuttEnforcers()
        {
            foreach (var tag in TradeTuning.HuttEnforcerTags)
            {
                _builder.AddSpawn(ObjectType.Creature, tag)
                    .WithFrequency(TradeTuning.HuttEnforcerSpawnWeight)
                    .RandomlyWalks()
                    .ReturnsHome();
            }
        }

        /// <summary>The Mid Rim nebula (space_midrim1): Dathomir Orbit's tier 5 ships and capitals, with its own ace.</summary>
        private void MidRimNebulaOrbit()
        {
            _builder.Create("SPACE_MIDRIM_1", "Space - A Nebula within the Mid Rim");
            AddFactionTier(5);
            AddRareCapitals();
            AddAce("ace_midrim1");
        }

        /// <summary>
        /// The approach to the Outer Rim (space_midrim2): Korriban Orbit's tier 6 ships and capitals (tier 6 has no
        /// faction capital), with its own ace.
        /// </summary>
        private void OuterRimApproachOrbit()
        {
            _builder.Create("SPACE_MIDRIM_2", "Space - Approaching the Outer Rim");
            AddFactionTier(6, capitalWeight: 0);
            AddRareCapitals(_korribanCapitals);
            AddAce("ace_midrim2");
        }

        private void ViscaraOrbit()
        {
            _builder.Create("SPACE_VISCARA_ORBIT", "Space - Viscara Orbit")
                .AddSpawn(ObjectType.Creature, "t1bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t1convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t1capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_viscara");
        }

        private void MonCalaOrbit()
        {
            _builder.Create("SPACE_MONCALA_ORBIT", "Space - Mon Cala Orbit")
                .AddSpawn(ObjectType.Creature, "t2bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t2convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t2capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_moncala");
        }

        private void HutlarOrbit()
        {
            _builder.Create("SPACE_HUTLAR_ORBIT", "Space - Hutlar Orbit")
                .AddSpawn(ObjectType.Creature, "t3bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t3convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t3capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_hutlar");

            AddHuttEnforcers();
        }

        private void TatooineOrbit()
        {
            _builder.Create("SPACE_TATOOINE_ORBIT", "Space - Tatooine Orbit")
                .AddSpawn(ObjectType.Creature, "t4bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t4convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t4capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_tatooine");

            AddHuttEnforcers();
        }

        private void KorribanOrbit()
        {
            _builder.Create("SPACE_KORRIBAN_ORBIT", "Space - Korriban Orbit")
                .AddSpawn(ObjectType.Creature, "t6bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t6convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(300)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(700)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_korriban");
        }

        private void DathomirOrbit()
        {
            _builder.Create("SPACE_DATHOMIR_ORBIT", "Space - Dathomir Orbit")
                .AddSpawn(ObjectType.Creature, "t5bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t5capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_dathomir");
        }

        private void EshanOrbit()
        {
            _builder.Create("SPACE_ESHAN_ORBIT", "Space - Eshan Orbit")
                .AddSpawn(ObjectType.Creature, "t5bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t5capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();
        }

        private void DantooineOrbit()
        {
               _builder.Create("SPACE_DANTOOINE_ORBIT", "Space - Dantooine Orbit")
                .AddSpawn(ObjectType.Creature, "t5bomber")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5cargo")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5platform")
                .WithFrequency(2400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5fighter")
                .WithFrequency(38400)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5gunship")
                .WithFrequency(3840)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5interceptor")
                .WithFrequency(19200)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5ewar")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5sniper")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5minelayer")
                .WithFrequency(1920)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5logistics")
                .WithFrequency(1440)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5carrier")
                .WithFrequency(480)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "t5convoy")
                .WithFrequency(480)

                .AddSpawn(ObjectType.Creature, "t5capital")
                .WithFrequency(960)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap1")
                .WithFrequency(96)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap2")
                .WithFrequency(48)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap3")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap4")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap5")
                .WithFrequency(6)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap6")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "sithcap7")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap1")
                .WithFrequency(12)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap2")
                .WithFrequency(2)
                .RandomlyWalks()
                .ReturnsHome()

                .AddSpawn(ObjectType.Creature, "mandocap3")
                .WithFrequency(1)
                .RandomlyWalks()
                .ReturnsHome();

            AddAce("ace_dantooine");
        }

        // The three Small/Medium-only zones (plan section 12, W6c). Each area sets CREATURE_SPAWN_COUNT to 56, 1.25x the 45
        // ships of an open 32x32 orbit, and spawns its tiers' ships at the open-orbit weights below.

        /// <summary>
        /// One faction tier's ships at the open-orbit weights (Viscara Orbit's table): Fighters and Interceptors, the
        /// W5b role ships at modest weights, haulers, platforms, bombers, the carrier and the tier capital.
        /// </summary>
        private void AddFactionTier(int tier, int minelayerWeight = 1920, int capitalWeight = 960)
        {
            void Ship(string hull, int weight) => _builder
                .AddSpawn(ObjectType.Creature, $"t{tier}{hull}")
                .WithFrequency(weight)
                .RandomlyWalks()
                .ReturnsHome();

            Ship("bomber", 96);
            Ship("cargo", 2400);
            Ship("platform", 2400);
            Ship("fighter", 38400);
            Ship("gunship", 3840);
            Ship("interceptor", 19200);
            Ship("ewar", 1920);
            Ship("sniper", 1920);
            Ship("minelayer", minelayerWeight);
            Ship("logistics", 1440);
            Ship("carrier", 480);
            if (capitalWeight > 0)
                Ship("capital", capitalWeight);

            // A trade convoy flies its own route to a starlane (Trade.InitializeConvoy), so it neither wanders nor leashes.
            _builder.AddSpawn(ObjectType.Creature, $"t{tier}convoy")
                .WithFrequency(480);
        }

        /// <summary>Viscara Asteroid Belt (space_viscbelt): Septra Khanate (T1) and Krakenborne (T2) ships in equal shares.</summary>
        private void ViscaraAsteroidBelt()
        {
            _builder.Create("SPACE_VISCARA_BELT", "Space - Viscara Asteroid Belt");
            AddFactionTier(1);
            AddFactionTier(2);
            AddAce("ace_visbelt", RevampAceCatalog.SpawnWeight * 2);
        }

        /// <summary>Mid Rim Nebula (space_midrimneb): Snowdrift Syndicate (T3) and Clan Zel Arun (T4) ships in equal shares.</summary>
        private void MidRimNebula()
        {
            _builder.Create("SPACE_MIDRIM_NEBULA", "Space - Mid Rim Nebula");
            AddFactionTier(3);
            AddFactionTier(4);
            AddAce("ace_midrimneb", RevampAceCatalog.SpawnWeight * 2);
        }

        /// <summary>
        /// Korriban Debris Field (space_korrdebris): Red Hand (T5) ships with the field's derelict defenses (plan section
        /// 11): minelayers at twice the orbit weight (their spacemine mines) and derelict turrets, the unused turret rows
        /// 2153 and 2154 on the T5 Defense Platform's preset (tag t5platform). The Sith and Mandalorian capitals spawn as in
        /// the T5 orbits (Dathomir, Dantooine).
        /// </summary>
        private void KorribanDebrisField()
        {
            _builder.Create("SPACE_KORRIBAN_DEBRIS", "Space - Korriban Debris Field");
            AddFactionTier(5, minelayerWeight: 3840);

            foreach (var turret in new[] { "t5derturret1", "t5derturret2" })
            {
                _builder.AddSpawn(ObjectType.Creature, turret)
                    .WithFrequency(2400)
                    .RandomlyWalks()
                    .ReturnsHome();
            }

            AddRareCapitals();
            AddAce("ace_korrdebris");
        }

        /// <summary>
        /// Every Wild Space system (plan section 23, the six areas in WildSpaceTuning.Systems), each with its own table:
        /// Rift Raiders flying all eight W5b roles and the pirate hulls, at the open-orbit weights
        /// (tools/SpaceRevampBalance/wildspace.py SPAWN_WEIGHTS) with the bomber at the role ships' weight, and the
        /// system's own ace (W11o). No convoys or capitals.
        /// </summary>
        private void WildSpace()
        {
            foreach (var system in WildSpaceTuning.Systems)
            {
                _builder.Create(system.SpawnTable, $"Space - {system.Name}");
                foreach (var (tag, weight) in new[]
                         {
                             ("rrfighter", 38400), ("rrinterceptor", 19200), ("rrgunship", 3840), ("rrbomber", 1920), ("rrewar", 1920),
                             ("rrsniper", 1920), ("rrminelayer", 1920), ("rrlogistics", 1440), ("rrcarrier", 480)
                         })
                {
                    _builder.AddSpawn(ObjectType.Creature, tag)
                        .WithFrequency(weight)
                        .RandomlyWalks()
                        .ReturnsHome();
                }

                foreach (var ace in RevampAceCatalog.Aces)
                {
                    if (ace.WildSpace && ace.AreaResref == system.AreaResref)
                        AddAce(ace.Tag);
                }
            }
        }
    }
}
