using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    /// <summary>
    /// Second-stage incubation field notes dropped by rare spawn elites. Each elite rolls its own table once per kill
    /// at 10% per note the table holds, so every note drops at 10%.
    /// </summary>
    public class IncubationFieldNoteLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Viscara();
            Dathomir();
            MonCala();
            Dantooine();
            Korriban();
            Tatooine();
            Hutlar();
            return _builder.Build();
        }

        private void Viscara()
        {
            _builder.Create("FNOTE_OLDSCAR_KATH")
                .IsRare()
                .AddItem("fnote_2006", 1, 1, true)
                .AddItem("fnote_2014", 1, 1, true)
                .AddItem("fnote_2021", 1, 1, true);

            _builder.Create("FNOTE_STORMPLUME")
                .IsRare()
                .AddItem("fnote_2086", 1, 1, true)
                .AddItem("fnote_2132", 1, 1, true);

            _builder.Create("FNOTE_SILKSHADE")
                .IsRare()
                .AddItem("fnote_2029", 1, 1, true);

            _builder.Create("FNOTE_MOSSBACK")
                .IsRare()
                .AddItem("fnote_2007", 1, 1, true)
                .AddItem("fnote_2020", 1, 1, true)
                .AddItem("fnote_2064", 1, 1, true);

            _builder.Create("FNOTE_GREYSPINE")
                .IsRare()
                .AddItem("fnote_2005", 1, 1, true)
                .AddItem("fnote_2037", 1, 1, true)
                .AddItem("fnote_2047", 1, 1, true)
                .AddItem("fnote_2090", 1, 1, true)
                .AddItem("fnote_2095", 1, 1, true);

            _builder.Create("FNOTE_REDTAIL_KOR")
                .IsRare()
                .AddItem("fnote_2082", 1, 1, true)
                .AddItem("fnote_2131", 1, 1, true);

            _builder.Create("FNOTE_SHARDEYE")
                .IsRare()
                .AddItem("fnote_2033", 1, 1, true)
                .AddItem("fnote_2040", 1, 1, true)
                .AddItem("fnote_2112", 1, 1, true);

            _builder.Create("FNOTE_ASHWING")
                .IsRare()
                .AddItem("fnote_2135", 1, 1, true);

            _builder.Create("FNOTE_ROOTCOIL")
                .IsRare()
                .AddItem("fnote_2079", 1, 1, true);

            _builder.Create("FNOTE_MIREVEIN")
                .IsRare()
                .AddItem("fnote_2063", 1, 1, true);

            _builder.Create("FNOTE_SOOT_RUSK")
                .IsRare()
                .AddItem("fnote_2068", 1, 1, true);
        }

        private void Dathomir()
        {
            _builder.Create("FNOTE_TARNAPEXMAW")
                .IsRare()
                .AddItem("fnote_2009", 1, 1, true)
                .AddItem("fnote_2046", 1, 1, true)
                .AddItem("fnote_2083", 1, 1, true);

            _builder.Create("FNOTE_QUILLSTALKER")
                .IsRare()
                .AddItem("fnote_2062", 1, 1, true)
                .AddItem("fnote_2071", 1, 1, true);

            _builder.Create("FNOTE_RHYDELALPHA")
                .IsRare()
                .AddItem("fnote_2003", 1, 1, true)
                .AddItem("fnote_2049", 1, 1, true);

            _builder.Create("FNOTE_GROTTOALPHA")
                .IsRare()
                .AddItem("fnote_2035", 1, 1, true)
                .AddItem("fnote_2050", 1, 1, true);

            _builder.Create("FNOTE_SPINEQUILL")
                .IsRare()
                .AddItem("fnote_2045", 1, 1, true)
                .AddItem("fnote_2116", 1, 1, true);

            _builder.Create("FNOTE_RITESTALKER")
                .IsRare()
                .AddItem("fnote_2118", 1, 1, true);
        }

        private void MonCala()
        {
            _builder.Create("FNOTE_REEFMAW")
                .IsRare()
                .AddItem("fnote_2113", 1, 1, true);

            _builder.Create("FNOTE_SABLE_QUARR")
                .IsRare()
                .AddItem("fnote_2028", 1, 1, true)
                .AddItem("fnote_2041", 1, 1, true);

            _builder.Create("FNOTE_KAEL_DROX")
                .IsRare()
                .AddItem("fnote_2004", 1, 1, true)
                .AddItem("fnote_2098", 1, 1, true);

            _builder.Create("FNOTE_INKVEIL")
                .IsRare()
                .AddItem("fnote_2026", 1, 1, true)
                .AddItem("fnote_2127", 1, 1, true);

            _builder.Create("FNOTE_GLASSJAW")
                .IsRare()
                .AddItem("fnote_2027", 1, 1, true)
                .AddItem("fnote_2130", 1, 1, true);
        }

        private void Dantooine()
        {
            _builder.Create("FNOTE_TRIAGEWARDEN")
                .IsRare()
                .AddItem("fnote_2048", 1, 1, true);

            _builder.Create("FNOTE_CHEMSLINGER")
                .IsRare()
                .AddItem("fnote_2053", 1, 1, true);

            _builder.Create("FNOTE_CONDUITMATRN")
                .IsRare()
                .AddItem("fnote_2059", 1, 1, true);

            _builder.Create("FNOTE_SABRAETRIAL")
                .IsRare()
                .AddItem("fnote_2133", 1, 1, true);

            _builder.Create("FNOTE_CYCLONEADPT")
                .IsRare()
                .AddItem("fnote_2016", 1, 1, true);

            _builder.Create("FNOTE_ENCLAVESENTL")
                .IsRare()
                .AddItem("fnote_2105", 1, 1, true);
        }

        private void Korriban()
        {
            _builder.Create("FNOTE_CRYPTWARDEN")
                .IsRare()
                .AddItem("fnote_2073", 1, 1, true);

            _builder.Create("FNOTE_MARKAHUNGER")
                .IsRare()
                .AddItem("fnote_2042", 1, 1, true);

            _builder.Create("FNOTE_ECLIPSESHADE")
                .IsRare()
                .AddItem("fnote_2077", 1, 1, true)
                .AddItem("fnote_2103", 1, 1, true);

            _builder.Create("FNOTE_FLAMEWEAVER")
                .IsRare()
                .AddItem("fnote_2094", 1, 1, true);
        }

        private void Tatooine()
        {
            _builder.Create("FNOTE_CANYONBULWRK")
                .IsRare()
                .AddItem("fnote_2072", 1, 1, true)
                .AddItem("fnote_2099", 1, 1, true);

            _builder.Create("FNOTE_DUNEDEADEYE")
                .IsRare()
                .AddItem("fnote_2058", 1, 1, true)
                .AddItem("fnote_2060", 1, 1, true);

            _builder.Create("FNOTE_DEADHANDZEPH")
                .IsRare()
                .AddItem("fnote_2088", 1, 1, true);

            _builder.Create("FNOTE_TUSKCHAMPION")
                .IsRare()
                .AddItem("fnote_2092", 1, 1, true);

            _builder.Create("FNOTE_TUSKLONGEYE")
                .IsRare()
                .AddItem("fnote_2134", 1, 1, true);
        }

        private void Hutlar()
        {
            _builder.Create("FNOTE_FLURRYCHAMP")
                .IsRare()
                .AddItem("fnote_2061", 1, 1, true);

            _builder.Create("FNOTE_THERMLANCER")
                .IsRare()
                .AddItem("fnote_2102", 1, 1, true);

            _builder.Create("FNOTE_BARRIEROVERSE")
                .IsRare()
                .AddItem("fnote_2125", 1, 1, true);
        }
    }
}
