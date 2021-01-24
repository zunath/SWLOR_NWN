using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature
{
    public static class RimerCards
    {
        private enum RimerAIDifficulty
        {
            Training = 1,
            Easy = 2,
            Normal = 3,
            Hard = 4
        }
        private enum RimerDeckType
        {
            Angels = 0x00000001,
            Animals = 0x00000002,
            BigCreatures = 0x00000004,
            FastCreatures = 0x00000008,
            Goblins = 0x00000010,
            Kobolds = 0x00000020,
            Random = 0x00000040,
            Rats = 0x00000080,
            Spells = 0x00000100,
            Undead = 0x00000200,
            Wolves = 0x00000400
        }

        /// <summary>
        /// When the modules loads, create 20 copies of the Rimer cards areas.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CreateInstances()
        {
            var source = Cache.GetAreaByResref("cardgame003");
            if (!GetIsObjectValid(source)) return;

            // Create 20 instances of the card game area.
            const int CopyCount = 20;

            for (var x = 1; x <= CopyCount; x++)
            {
                CopyArea(source);
            }

            Console.WriteLine("Created " + CopyCount + " copies of Rimer Cards areas.");
        }


        private static void ConfigureGameSettings(uint npc, RimerDeckType deck, RimerAIDifficulty difficulty)
        {
            SetLocalInt(npc, "CARD_AI_DIFFICULTY", (int)difficulty);
            SetLocalInt(npc, "CARD_DECK_TYPE", (int)deck);
        }

        [NWNEventHandler("rimer_cpu_1")]
        public static int RimerCPU1()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Wolves;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Training);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_2")]
        public static int RimerCPU2()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Wolves;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Easy);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_3")]
        public static int RimerCPU3()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.Goblins : RimerDeckType.Random;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Easy);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_4")]
        public static int RimerCPU4()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.FastCreatures : RimerDeckType.BigCreatures;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Normal);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_5")]
        public static int RimerCPU5()
        {
            ConfigureGameSettings(OBJECT_SELF, RimerDeckType.Random, RimerAIDifficulty.Normal);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_6")]
        public static int RimerCPU6()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.Spells : RimerDeckType.Angels;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Normal);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_7")]
        public static int RimerCPU7()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.Undead : RimerDeckType.Random;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Hard);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_8")]
        public static int RimerCPU8()
        {
            var deck = Random.Next(4) <= 3 ? RimerDeckType.Angels : RimerDeckType.Animals;
            ConfigureGameSettings(OBJECT_SELF, deck, RimerAIDifficulty.Hard);
            return 0;
        }

        [NWNEventHandler("rimer_cpu_9")]
        public static int RimerCPU9()
        {
            ConfigureGameSettings(OBJECT_SELF, RimerDeckType.Random, RimerAIDifficulty.Hard);
            return 0;
        }
    }
}
