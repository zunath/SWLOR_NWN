using System;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.FishingService
{
    public enum FishType
    {
        [Fish("Invalid", "", -1, false)]
        Invalid = 0,
        [Fish("Moat Carp", "moat_carp", 0, true)]
        MoatCarp = 1,
        [Fish("Lamp Marimo", "lamp_marimo", 1, true)]
        LampMarimo = 2,
        [Fish("Viscara Urchin", "visc_urchin", 2, true)]
        ViscaraUrchin = 3,
        [Fish("Phanauet Newt", "phan_newt", 3, true)]
        PhanauetNewt = 4,
        [Fish("Cobalt Jellyfish", "cobalt_jellyfish", 4, true)]
        CobaltJellyfish = 5,
        [Fish("Denizanasi", "denizanasi", 5, true)]
        Denizanasi = 6,
        [Fish("Crayfish", "crayfish", 6, true)]
        Crayfish = 7,
        [Fish("Cala Lobster", "cala_lobster", 7, true)]
        CalaLobster = 8,
        [Fish("Bibikibo", "bibikibo", 8, true)]
        Bibikibo = 9,
        [Fish("Dathomir Sardine", "dath_sardine", 9, true)]
        DathomirSardine = 10,
        [Fish("Hamsi", "hamsi", 10, true)]
        Hamsi = 11,
        [Fish("Senroh Sardine", "sen_sardine", 11, true)]
        SenrohSardine = 12,
        [Fish("Ra'Kaznar Shellfish", "rakaz_shellfish", 12, true)]
        RaKaznarShellfish = 13,
        [Fish("Bastion Sweeper", "bast_sweeper", 13, true)]
        BastionSweeper = 14,
        [Fish("Mackerel", "mackerel", 14, true)]
        Mackerel = 15,
        [Fish("Greedie", "greedie", 15, true)]
        Greedie = 16,
        [Fish("Copper Frog", "copper_frog", 16, true)]
        CopperFrog = 17,
        [Fish("Yellow Globe", "yellow_globe", 17, true)]
        YellowGlobe = 18,
        [Fish("Muddy Siredon", "muddy_siredon", 18, true)]
        MuddySiredon = 19,
        [Fish("Istavrit", "istavrit", 19, true)]
        Istavrit = 20,
        [Fish("Translucent Salpa", "trans_salpa", 20, true)]
        TranslucentSalpa = 21,
        [Fish("Quus", "quus", 21, true)]
        Quus = 22,
        [Fish("Forest Carp", "forest_carp", 22, true)]
        ForestCarp = 23,
        [Fish("Tiny Goldfish", "tiny_goldfish", 23, true)]
        TinyGoldfish = 24,
        [Fish("Hoptoad", "hoptoad", 24, true)]
        Hoptoad = 25,
        [Fish("Cheval Salmon", "cheval_salmon", 25, true)]
        ChevalSalmon = 26,
        [Fish("Yorchete", "yorchete", 26, true)]
        Yorchete = 27,
        [Fish("White Lobster", "white_lobster", 27, true)]
        WhiteLobster = 28,
        [Fish("Fat Greedie", "fat_greedie", 28, true)]
        FatGreedie = 29,
        [Fish("Moorish Idol", "moorish_idol", 29, true)]
        MoorishIdol = 30,
        [Fish("Gurnard", "gurnard", 30, true)]
        Gurnard = 31,
        [Fish("Nebimonite", "nebimonite", 31, true)]
        Nebimonite = 32,
        [Fish("Tricolored Carp", "tricolored_carp", 32, true)]
        TricoloredCarp = 33,
        [Fish("Blindfish", "blindfish", 33, true)]
        Blindfish = 34,
        [Fish("Pipira", "pipira", 34, true)]
        Pipira = 35,
        [Fish("Tiger Cod", "tiger_cod", 35, true)]
        TigerCod = 36,
        [Fish("Bonefish", "bonefish", 36, true)]
        Bonefish = 37,
        [Fish("Giant Catfish", "giant_catfish", 37, true)]
        GiantCatfish = 38,
        [Fish("Yayinbaligi", "yayinbaligi", 38, true)]
        Yayinbaligi = 39,
        [Fish("Deadmoiselle", "deadmoiselle", 39, true)]
        Deadmoiselle = 40,
        [Fish("Lungfish", "lungfish", 40, true)]
        Lungfish = 41,
        [Fish("Dark Bass", "dark_bass", 41, true)]
        DarkBass = 42,
        [Fish("Crystal Bass", "crystal_bass", 42, true)]
        CrystalBass = 43,
        [Fish("Ogre Eel", "ogre_eel", 43, true)]
        OgreEel = 44,
        [Fish("Shining Trout", "shining_trout", 44, true)]
        ShiningTrout = 45,
        [Fish("Blowfish", "blowfish", 45, true)]
        Blowfish = 46,
        [Fish("Nosteau Herring", "nosteau_herring", 46, true)]
        NosteauHerring = 47,
        [Fish("Lakerda", "lakerda", 47, true)]
        Lakerda = 48,
        [Fish("Zafmlug Bass", "zafmlug_bass", 48, true)]
        ZafmlugBass = 49,
        [Fish("Ruddy Seema", "ruddy_seema", 49, true)]
        RuddySeema = 50,
        [Fish("Frigorifish", "frigorifish", 50, true)]
        Frigorifish = 51,
        [Fish("Mercanbaligi", "mercanbaligi", 52, false)]
        Mercanbaligi = 52,
        [Fish("Nashmau", "nashmau", 52, false)]
        Nashmau = 53,
        [Fish("Rhinochimera", "rhinochimera", 52, false)]
        Rhinochimera = 54,
        [Fish("Mhaura", "mhaura", 52, false)]
        Mhaura = 55,
        [Fish("Zi'tah", "zitah", 52, false)]
        Zitah = 56,
        [Fish("Al'zabi", "alzabi", 52, false)]
        Alzabi = 57
    }

    public class FishAttribute: Attribute
    {
        public string Name { get; set; }
        public string Resref { get; set; }
        public int Level { get; set; }
        public ObjectType ObjectType { get; set; }
        public bool DisplayInDescription { get; set; }


        public FishAttribute(
            string name, 
            string resref, 
            int level,
            bool displayInDescription,
            ObjectType objectType = ObjectType.Item)
        {
            Name = name;
            Resref = resref;
            Level = level;
            DisplayInDescription = displayInDescription;
            ObjectType = objectType;
        }
    }
}
