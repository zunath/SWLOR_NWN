using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public class NPCShipDefinition : IShipListDefinition
    {
        private readonly ShipBuilder _builder = new();
        public Dictionary<string, ShipDetail> BuildShips()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            _builder.Create("NPC_Bomber1")
                .ItemResref("t1bomber")
                .Name("NPC - Septra Khanate Bomber");

            _builder.Create("NPC_Cargo1")
                .ItemResref("t1cargo")
                .Name("NPC - Septra Khanate Scrap Hauler");

            _builder.Create("NPC_Platform1")
                .ItemResref("t1platform")
                .Name("NPC - Septra Khanate Defense Platform")
                .CapitalShip();

            _builder.Create("NPC_Fighter1")
                .ItemResref("t1fighter")
                .Name("NPC - Septra Khanate Fighter");

            _builder.Create("NPC_Gunship1")
                .ItemResref("t1gunship")
                .Name("NPC - Septra Khanate Gunship");

            _builder.Create("NPC_Interceptor1")
                .ItemResref("t1interceptor")
                .Name("NPC - Septra Khanate Interceptor");

            _builder.Create("NPC_Capital1")
                .ItemResref("t1capital")
                .Name("NPC - Septra Khanate Raidmaster")
                .CapitalShip();
        }

        private void Tier2()
        {
            _builder.Create("NPC_Bomber2")
                .ItemResref("t2bomber")
                .Name("NPC - Krakenborne Bomber");

            _builder.Create("NPC_Cargo2")
                .ItemResref("t2cargo")
                .Name("NPC - Krakenborne Trovehauler");

            _builder.Create("NPC_Platform2")
                .ItemResref("t2platform")
                .Name("NPC - Krakenborne Defense Platform")
                .CapitalShip();

            _builder.Create("NPC_Fighter2")
                .ItemResref("t2fighter")
                .Name("NPC - Krakenbornee Fighter");

            _builder.Create("NPC_Gunship2")
                .ItemResref("t2gunship")
                .Name("NPC - Krakenborne Gunship");

            _builder.Create("NPC_Interceptor2")
                .ItemResref("t2interceptor")
                .Name("NPC - Krakenborne Interceptor");

            _builder.Create("NPC_Capital2")
                .ItemResref("t2capital")
                .Name("NPC - Krakenborne Privateer")
                .CapitalShip();
        }

        private void Tier3()
        {
            _builder.Create("NPC_Bomber3")
                .ItemResref("t3bomber")
                .Name("NPC - Snowdrift Syndicate Bomber");

            _builder.Create("NPC_Cargo3")
                .ItemResref("t3cargo")
                .Name("NPC - Snowdrift Syndicate Frostvault");

            _builder.Create("NPC_Platform3")
                .ItemResref("t3platform")
                .Name("NPC - Snowdrift Syndicate Defense Platform")
                .CapitalShip();

            _builder.Create("NPC_Fighter3")
                .ItemResref("t3fighter")
                .Name("NPC - Snowdrift Syndicate Fighter");

            _builder.Create("NPC_Gunship3")
                .ItemResref("t3gunship")
                .Name("NPC - Snowdrift Syndicate Gunship");

            _builder.Create("NPC_Interceptor3")
                .ItemResref("t3interceptor")
                .Name("NPC - Snowdrift Syndicate Interceptor");

            _builder.Create("NPC_Capital3")
                .ItemResref("t3capital")
                .Name("NPC - Snowdrift Syndicate Snowpiercer")
                .CapitalShip();
        }

        private void Tier4()
        {
            _builder.Create("NPC_Bomber4")
                .ItemResref("t4bomber")
                .Name("NPC - Clan Zel Arun Bomber");

            _builder.Create("NPC_Cargo4")
                .ItemResref("t4cargo")
                .Name("NPC - Clan Zel Arun Treasure Barge");

            _builder.Create("NPC_Platform4")
                .ItemResref("t4platform")
                .Name("NPC - Clan Zel Arun Defense Platform")
                .CapitalShip();

            _builder.Create("NPC_Fighter4")
                .ItemResref("t4fighter")
                .Name("NPC - Clan Zel Arun Fighter");

            _builder.Create("NPC_Gunship4")
                .ItemResref("t4gunship")
                .Name("NPC - Clan Zel Arun Gunship");

            _builder.Create("NPC_Interceptor4")
                .ItemResref("t4interceptor")
                .Name("NPC - Clan Zel Arun Interceptor");

            _builder.Create("NPC_Capital4")
                .ItemResref("t4capital")
                .Name("NPC - Clan Zel Arun Yacht")
                .CapitalShip();
        }

        private void Tier5()
        {
            _builder.Create("NPC_Bomber5")
                .ItemResref("t5bomber")
                .Name("NPC - Red Hand Bomber");

            _builder.Create("NPC_Cargo5")
                .ItemResref("t5cargo")
                .Name("NPC - Red Hand Munitions Smuggler");

            _builder.Create("NPC_Platform5")
                .ItemResref("t5platform")
                .Name("NPC - Red Hand Defense Platform")
                .CapitalShip();

            _builder.Create("NPC_Fighter5")
                .ItemResref("t5fighter")
                .Name("NPC - Red Hand Fighter");

            _builder.Create("NPC_Gunship5")
                .ItemResref("t5gunship")
                .Name("NPC - Red Hand Gunship");

            _builder.Create("NPC_Interceptor5")
                .ItemResref("t5interceptor")
                .Name("NPC - Red Hand Interceptor");

            _builder.Create("NPC_Capital5")
                .ItemResref("t5capital")
                .Name("NPC - Red Hand Marauder")
                .CapitalShip();

            _builder.Create("NPC_Bomber6")
                .ItemResref("t6bomber")
                .Name("NPC - Imperial Bomber");

            _builder.Create("NPC_Cargo6")
                .ItemResref("t6cargo")
                .Name("NPC - Imperial Logistics Vessel");

            _builder.Create("NPC_Platform6")
                .ItemResref("t6platform")
                .Name("NPC - Imperial Defense Platform")
                .CapitalShip();

            _builder.Create("NPC_Fighter6")
                .ItemResref("t6fighter")
                .Name("NPC - Imperial Fighter");

            _builder.Create("NPC_Gunship6")
                .ItemResref("t6gunship")
                .Name("NPC - Imperial Gunship");

            _builder.Create("NPC_Interceptor6")
                .ItemResref("t6interceptor")
                .Name("NPC - Imperial Interceptor");

            _builder.Create("NPC_SithCap1")
                .ItemResref("sithcap1")
                .Name("NPC - Thranta Corvette")
                .CapitalShip();

            _builder.Create("NPC_SithCap2")
                .ItemResref("sithcap2")
                .Name("NPC - Derriphan Destroyer")
                .CapitalShip();

            _builder.Create("NPC_SithCap3")
                .ItemResref("sithcap3")
                .Name("NPC - Inquisitor Assault Cruiser")
                .CapitalShip();

            _builder.Create("NPC_SithCap4")
                .ItemResref("sithcap4")
                .Name("NPC - Interdictor Heavy Cruiser")
                .CapitalShip();

            _builder.Create("NPC_SithCap5")
                .ItemResref("sithcap5")
                .Name("NPC - Centurion Battlecruiser")
                .CapitalShip();

            _builder.Create("NPC_SithCap6")
                .ItemResref("sithcap6")
                .Name("NPC - Supremacy Battleship")
                .CapitalShip();

            _builder.Create("NPC_SithCap7")
                .ItemResref("sithcap7")
                .Name("NPC - Harrower Dreadnought")
                .CapitalShip();

            _builder.Create("NPC_MandoCap1")
                .ItemResref("mandocap1")
                .Name("NPC - Jahaveyir Cruiser")
                .CapitalShip();

            _builder.Create("NPC_MandoCap2")
                .ItemResref("mandocap2")
                .Name("NPC - Kyramud Battleship")
                .CapitalShip();

            _builder.Create("NPC_MandoCap3")
                .ItemResref("mandocap3")
                .Name("NPC - Kandosii Dreadnought")
                .CapitalShip();
        }
    }
}
