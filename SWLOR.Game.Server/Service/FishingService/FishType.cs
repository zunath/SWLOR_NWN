using System;

namespace SWLOR.Game.Server.Service.FishingService
{
    public enum FishType
    {
        [Fish("Invalid", "", -1)]
        Invalid = 0,
        [Fish("Moat Carp", "moat_carp", 0)]
        MoatCarp = 1,
        [Fish("Lamp Marimo", "lamp_marimo", 1)]
        LampMarimo = 2,
        [Fish("Viscara Urchin", "visc_urchin", 2)]
        ViscaraUrchin = 3,
        [Fish("Phanauet Newt", "phan_newt", 3)]
        PhanauetNewt = 4,
        [Fish("Cobalt Jellyfish", "cobalt_jellyfish", 4)]
        CobaltJellyfish = 5,
        [Fish("Denizanasi", "denizanasi", 5)]
        Denizanasi = 6,
        [Fish("Crayfish", "crayfish", 6)]
        Crayfish = 7,
        [Fish("Cala Lobster", "cala_lobster", 7)]
        CalaLobster = 8,
        [Fish("Bibikibo", "bibikibo", 8)]
        Bibikibo = 9,
        [Fish("Dathomir Sardine", "dath_sardine", 9)]
        DathomirSardine = 10,
        [Fish("Hamsi", "hamsi", 10)]
        Hamsi = 11,
        [Fish("Senroh Sardine", "sen_sardine", 11)]
        SenrohSardine = 12,
        [Fish("Ra'Kaznar Shellfish", "rakaz_shellfish", 12)]
        RaKaznarShellfish = 13,
        [Fish("Bastion Sweeper", "bast_sweeper", 13)]
        BastionSweeper = 14,
        [Fish("Mackerel", "mackerel", 14)]
        Mackerel = 15,
        [Fish("Greedie", "greedie", 15)]
        Greedie = 16,
        [Fish("Copper Frog", "copper_frog", 16)]
        CopperFrog = 17,
        [Fish("Yellow Globe", "yellow_globe", 17)]
        YellowGlobe = 18,
        [Fish("Muddy Siredon", "muddy_siredon", 18)]
        MuddySiredon = 19,
        [Fish("Istavrit", "istavrit", 19)]
        Istavrit = 20,
        [Fish("Translucent Salpa", "trans_salpa", 20)]
        TranslucentSalpa = 21,
        [Fish("Quus", "quus", 21)]
        Quus = 22,
        [Fish("Forest Carp", "forest_carp", 22)]
        ForestCarp = 23,
        [Fish("Tiny Goldfish", "tiny_goldfish", 23)]
        TinyGoldfish = 24,
        [Fish("Hoptoad", "hoptoad", 24)]
        Hoptoad = 25,
        [Fish("Cheval Salmon", "cheval_salmon", 25)]
        ChevalSalmon = 26,
        [Fish("Yorchete", "yorchete", 26)]
        Yorchete = 27,
        [Fish("White Lobster", "white_lobster", 27)]
        WhiteLobster = 28,
        [Fish("Fat Greedie", "fat_greedie", 28)]
        FatGreedie = 29,
        [Fish("Moorish Idol", "moorish_idol", 29)]
        MoorishIdol = 30,
        [Fish("Gurnard", "gurnard", 30)]
        Gurnard = 31,
        [Fish("Nebimonite", "nebimonite", 31)]
        Nebimonite = 32,
        [Fish("Tricolored Carp", "tricolored_carp", 32)]
        TricoloredCarp = 33,
        [Fish("Blindfish", "blindfish", 33)]
        Blindfish = 34,
        [Fish("Pipira", "pipira", 34)]
        Pipira = 35,
        [Fish("Tiger Cod", "tiger_cod", 35)]
        TigerCod = 36,
        [Fish("Bonefish", "bonefish", 36)]
        Bonefish = 37,
        [Fish("Giant Catfish", "giant_catfish", 37)]
        GiantCatfish = 38,
        [Fish("Yayinbaligi", "yayinbaligi", 38)]
        Yayinbaligi = 39,
        [Fish("Deadmoiselle", "deadmoiselle", 39)]
        Deadmoiselle = 40,
        [Fish("Lungfish", "lungfish", 40)]
        Lungfish = 41,
        [Fish("Dark Bass", "dark_bass", 41)]
        DarkBass = 42,
        [Fish("Crystal Bass", "crystal_bass", 42)]
        CrystalBass = 43,
        [Fish("Ogre Eel", "ogre_eel", 43)]
        OgreEel = 44,
        [Fish("Shining Trout", "shining_trout", 44)]
        ShiningTrout = 45,
        [Fish("Blowfish", "blowfish", 45)]
        Blowfish = 46,
        [Fish("Nosteau Herring", "nosteau_herring", 46)]
        NosteauHerring = 47,
        [Fish("Lakerda", "lakerda", 47)]
        Lakerda = 48,
        [Fish("Zafmlug Bass", "zafmlug_bass", 48)]
        ZafmlugBass = 49,
        [Fish("Ruddy Seema", "ruddy_seema", 49)]
        RuddySeema = 50,
        [Fish("Frigorifish", "frigorifish", 50)]
        Frigorifish = 51,
    }

    public class FishAttribute: Attribute
    {
        public string Name { get; set; }
        public string Resref { get; set; }
        public int Level { get; set; }

        public FishAttribute(string name, string resref, int level)
        {
            Name = name;
            Resref = resref;
            Level = level;
        }
    }
}
