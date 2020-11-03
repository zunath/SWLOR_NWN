using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.RimerCards
{
    public static class RimerCardsEvents
    {
        [NWNEventHandler("rimer_cpu_1")]
        public static int RimerCPU1()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Wolves;
            RimerCPU.ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Training);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_2")]
        public static int RimerCPU2()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Wolves;
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, deck, RimerAIDifficulty.Easy);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_3")]
        public static int RimerCPU3()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Random;
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, deck, RimerAIDifficulty.Easy);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_4")]
        public static int RimerCPU4()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.FastCreatures : RimerDeckType.BigCreatures;
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, deck, RimerAIDifficulty.Normal);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_5")]
        public static int RimerCPU5()
        {
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, RimerDeckType.Random, RimerAIDifficulty.Normal);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_6")]
        public static int RimerCPU6()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.Spells : RimerDeckType.Angels;
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, deck, RimerAIDifficulty.Normal);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_7")]
        public static int RimerCPU7()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.Undead : RimerDeckType.Random;
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, deck, RimerAIDifficulty.Hard);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_8")]
        public static int RimerCPU8()
        {
            var deck = Server.Service.Random.Next(4) <= 3 ? RimerDeckType.Angels : RimerDeckType.Animals;
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, deck, RimerAIDifficulty.Hard);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_9")]
        public static int RimerCPU9()
        {
            RimerCPU.ConfigureGameSettings(NWScript.OBJECT_SELF, RimerDeckType.Random, RimerAIDifficulty.Hard);
            return 0;
        }
    }
}
