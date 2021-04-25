using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class ViscaraLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            KathHound();
            MandalorianLeader();
            MandalorianRanger();
            MandalorianWarrior();
            Outlaw();
            Gimpassa();
            Kinrath();
            Cairnmog();
            VellenFleshleader();
            VellenFlesheater();
            Raivor();
            Warocas();
            Nashtah();
            CrystalSpider();

            return _builder.Build();
        }

        private void KathHound()
        {
            _builder.Create("VISCARA_KATH_HOUND");
        }

        private void MandalorianLeader()
        {
            _builder.Create("VISCARA_MANDALORIAN_LEADER");
        }

        private void MandalorianWarrior()
        {
            _builder.Create("VISCARA_MANDALORIAN_WARRIOR");
        }

        private void MandalorianRanger()
        {
            _builder.Create("VISCARA_MANDALORIAN_RANGER");
        }

        private void Outlaw()
        {
            _builder.Create("VISCARA_OUTLAW");
        }

        private void Gimpassa()
        {
            _builder.Create("VISCARA_GIMPASSA");
        }

        private void Kinrath()
        {
            _builder.Create("VISCARA_KINRATH");
        }

        private void Cairnmog()
        {
            _builder.Create("VISCARA_CAIRNMOG");
        }

        private void VellenFleshleader()
        {
            _builder.Create("VISCARA_VELLEN_FLESHLEADER");
        }

        private void VellenFlesheater()
        {
            _builder.Create("VISCARA_VELLEN_FLESHEATER");
        }

        private void Raivor()
        {
            _builder.Create("VISCARA_RAIVOR");
        }

        private void Warocas()
        {
            _builder.Create("VISCARA_WAROCAS");
        }

        private void Nashtah()
        {
            _builder.Create("VISCARA_NASHTAH");
        }

        private void CrystalSpider()
        {
            _builder.Create("VISCARA_CRYSTAL_SPIDER");
        }
    }
}
