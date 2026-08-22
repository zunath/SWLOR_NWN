using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class SlicingRewardCatalog
    {
        private static readonly List<SlicingRewardEntry> _entries = new();

        public static IReadOnlyList<SlicingRewardEntry> Entries => _entries;

        static SlicingRewardCatalog()
        {
            AddNamedRewards();
            AddSchematics();
            AddFieldNotes();
            AddTools();
        }

        public static IReadOnlyList<SlicingRewardEntry> Get(
            SlicingSourceType source,
            int tier,
            SlicingRewardCategory category,
            bool? exceptional = null)
        {
            return _entries.Where(x =>
                    x.Source == source &&
                    x.Tier == tier &&
                    x.Category == category &&
                    (!exceptional.HasValue || x.IsExceptional == exceptional.Value))
                .ToList();
        }

        private static void AddNamedRewards()
        {
            Named(SlicingSourceType.Lockbox, 1, "slw_quickdraw", "Quickdraw Holdout", true);
            Named(SlicingSourceType.Lockbox, 1, "slw_sabutility", "Saboteur Utility Blade");
            Named(SlicingSourceType.Lockbox, 1, "slw_cratehook", "Cratehook Spear");
            Named(SlicingSourceType.Terminal, 1, "stw_surveycoil", "Surveyor Coil Rifle");
            Named(SlicingSourceType.Terminal, 1, "stw_groundloop", "Ground-Loop Baton");

            Named(SlicingSourceType.Lockbox, 2, "slw_sidewinder", "Sidewinder Compact");
            Named(SlicingSourceType.Lockbox, 2, "slw_viperclaw", "Viper-Circuit Claws", true);
            Named(SlicingSourceType.Lockbox, 2, "slw_crosswind", "Crosswind Boarding Twinblade");
            Named(SlicingSourceType.Terminal, 2, "stw_quietcarb", "Quieting Carbine");
            Named(SlicingSourceType.Terminal, 2, "stw_phasereturn", "Phase-Return Blades");

            Named(SlicingSourceType.Lockbox, 3, "slw_debtmarker", "Debtmarker Pistol");
            Named(SlicingSourceType.Lockbox, 3, "slw_sealknife", "Counterfeit-Seal Knife");
            Named(SlicingSourceType.Lockbox, 3, "slw_blackroute", "Blackroute Greatblade");
            Named(SlicingSourceType.Terminal, 3, "stw_longwatch", "Longwatch Coil Rifle", true);
            Named(SlicingSourceType.Terminal, 3, "stw_relaybreak", "Relaybreaker Staff");

            Named(SlicingSourceType.Lockbox, 4, "slw_coldchamber", "Cold-Chamber Sidearm");
            Named(SlicingSourceType.Lockbox, 4, "slw_bastiontal", "Bastion Talons", true);
            Named(SlicingSourceType.Lockbox, 4, "slw_deaddrop", "Dead-Drop War Spear");
            Named(SlicingSourceType.Terminal, 4, "stw_nulllattice", "Null-Lattice Suppressor");
            Named(SlicingSourceType.Terminal, 4, "stw_orbitshear", "Orbit-Shear Throwers");

            Named(SlicingSourceType.Lockbox, 5, "slw_lastwitness", "Last Witness Pistol");
            Named(SlicingSourceType.Lockbox, 5, "slw_gloamknife", "Shadow Gloamsteel Knife");
            Named(SlicingSourceType.Lockbox, 5, "slw_vaultbreak", "Vaultbreaker Greatblade");
            Named(SlicingSourceType.Terminal, 5, "stw_ghostline", "Ghostline Experimental Rifle", true);
            Named(SlicingSourceType.Terminal, 5, "stw_zerostate", "Zero-State Twin Electroblade");

            Named(SlicingSourceType.Lockbox, 1, "slg_dockcipher", "Dockside Cipher Gloves");
            Named(SlicingSourceType.Lockbox, 1, "slg_ventmouse", "Vent-Mouse Treads");
            Named(SlicingSourceType.Lockbox, 1, "slg_bitterdose", "Bitterglass Doser Belt");
            Named(SlicingSourceType.Terminal, 1, "stg_contwatch", "Continuity Watch Visor");
            Named(SlicingSourceType.Terminal, 1, "stg_borrowcred", "Borrowed-Credential Bracer", true);

            Named(SlicingSourceType.Lockbox, 2, "slg_falsebottom", "False-Bottom Keycloak");
            Named(SlicingSourceType.Lockbox, 2, "slg_tripline", "Tripline Field Gloves");
            Named(SlicingSourceType.Lockbox, 2, "slg_smugcourtesy", "Smuggler's Courtesy Belt");
            Named(SlicingSourceType.Terminal, 2, "stg_hushmesh", "Hush-Mesh Tunic", true);
            Named(SlicingSourceType.Terminal, 2, "stg_raivordose", "Raivor Microdoser Bracer");

            Named(SlicingSourceType.Lockbox, 3, "slg_counterwatch", "Counterwatch Cloak", true);
            Named(SlicingSourceType.Lockbox, 3, "slg_tombfilter", "Tombspore Filter Boots");
            Named(SlicingSourceType.Lockbox, 3, "slg_gravewire", "Gravewire Utility Belt");
            Named(SlicingSourceType.Terminal, 3, "stg_echokey", "Echo-Key Sensor");
            Named(SlicingSourceType.Terminal, 3, "stg_diploghost", "Diplomatic Ghost Visor");

            Named(SlicingSourceType.Lockbox, 4, "slg_blackledger", "Black-Ledger Lock Gloves");
            Named(SlicingSourceType.Lockbox, 4, "slg_rimeinject", "Rimevenom Injector Belt");
            Named(SlicingSourceType.Lockbox, 4, "slg_ashline", "Ashline Hushcloak");
            Named(SlicingSourceType.Terminal, 4, "stg_nullfoot", "Null-Footprint Tunic");
            Named(SlicingSourceType.Terminal, 4, "stg_sabotvisor", "Sabotage Pattern Visor", true);

            Named(SlicingSourceType.Lockbox, 5, "slg_moonstep", "Moonless-Step Boots");
            Named(SlicingSourceType.Lockbox, 5, "slg_deadfall", "Deadfall Field Gloves");
            Named(SlicingSourceType.Lockbox, 5, "slg_lastcover", "Last-Cover Cloak", true);
            Named(SlicingSourceType.Terminal, 5, "stg_causalkey", "Causal-Key Bracer");
            Named(SlicingSourceType.Terminal, 5, "stg_nightroot", "Nightroot Rebreather");
        }

        private static void AddSchematics()
        {
            Schematic(SlicingSourceType.Lockbox, 1, "slbp_stitchglv", "Schematic: Stitchplate Lock Gloves");
            Schematic(SlicingSourceType.Lockbox, 1, "slbp_quietjerky", "Recipe: Quietwatch Jerky");
            Schematic(SlicingSourceType.Lockbox, 2, "slbp_falsevisor", "Schematic: False-Face Field Visor");
            Schematic(SlicingSourceType.Lockbox, 2, "slbp_dustcakes", "Recipe: Dustveil Travel Cakes");
            Schematic(SlicingSourceType.Lockbox, 3, "slbp_quietboots", "Schematic: Quietstep Reinforced Boots");
            Schematic(SlicingSourceType.Lockbox, 3, "slbp_tombbroth", "Recipe: Tombwalker Broth");
            Schematic(SlicingSourceType.Lockbox, 4, "slbp_dropcloak", "Schematic: Dead-Drop Armored Cloak");
            Schematic(SlicingSourceType.Lockbox, 4, "slbp_snowstew", "Recipe: Snowblind Hunter's Stew");
            Schematic(SlicingSourceType.Lockbox, 5, "slbp_breachhar", "Schematic: Blacksite Breach Harness");
            Schematic(SlicingSourceType.Lockbox, 5, "slbp_nightres", "Recipe: Night March Reserve");

            Schematic(SlicingSourceType.Terminal, 1, "stbp_copfuse", "Schematic: Copper Trace Fuse");
            Schematic(SlicingSourceType.Terminal, 1, "stbp_rustterm", "Schematic: Rustline Data Terminal");
            Schematic(SlicingSourceType.Terminal, 1, "stbp_whispven", "Formula: Whisperthorn Concentrate");
            Schematic(SlicingSourceType.Terminal, 2, "stbp_braidfuse", "Schematic: Braided Trace Fuse");
            Schematic(SlicingSourceType.Terminal, 2, "stbp_ciphcab", "Schematic: Cipherfile Cabinet");
            Schematic(SlicingSourceType.Terminal, 2, "stbp_glassven", "Formula: Glassfang Concentrate");
            Schematic(SlicingSourceType.Terminal, 3, "stbp_phasefuse", "Schematic: Phase Trace Fuse");
            Schematic(SlicingSourceType.Terminal, 3, "stbp_listmon", "Schematic: Listening Post Monitor");
            Schematic(SlicingSourceType.Terminal, 3, "stbp_tombven", "Formula: Tombspore Concentrate");
            Schematic(SlicingSourceType.Terminal, 4, "stbp_cryofuse", "Schematic: Cryo Trace Fuse");
            Schematic(SlicingSourceType.Terminal, 4, "stbp_ghostcon", "Schematic: Ghost-Channel Console");
            Schematic(SlicingSourceType.Terminal, 4, "stbp_rimeven", "Formula: Rimevenom Concentrate");
            Schematic(SlicingSourceType.Terminal, 5, "stbp_nullfuse", "Schematic: Null Trace Fuse");
            Schematic(SlicingSourceType.Terminal, 5, "stbp_blackstat", "Schematic: Blacksite Analysis Station");
            Schematic(SlicingSourceType.Terminal, 5, "stbp_nightven", "Formula: Nightroot Concentrate");
        }

        private static void AddFieldNotes()
        {
            Note(SlicingSourceType.Lockbox, 1, "fnote_2105", "Field Note: Sootbelly Mirekit");
            Note(SlicingSourceType.Lockbox, 2, "fnote_2007", "Field Note: Azurehorn Kargath");
            Note(SlicingSourceType.Lockbox, 3, "fnote_2112", "Field Note: Strayfang Kavor");
            Note(SlicingSourceType.Lockbox, 4, "fnote_2045", "Field Note: Duneshag Bantha");
            Note(SlicingSourceType.Lockbox, 5, "fnote_2072", "Field Note: Ironmaw Bastionback");

            Note(SlicingSourceType.Terminal, 1, "fnote_2014", "Field Note: Blinkstep Vekara");
            Note(SlicingSourceType.Terminal, 1, "fnote_2086", "Field Note: Phaseleg Silkstalker");
            Note(SlicingSourceType.Terminal, 2, "fnote_2020", "Field Note: Brassjaw Pyralisk");
            Note(SlicingSourceType.Terminal, 2, "fnote_2131", "Field Note: Venomspike Laigrek");
            Note(SlicingSourceType.Terminal, 3, "fnote_2058", "Field Note: Gilded Mirewyrm");
            Note(SlicingSourceType.Terminal, 3, "fnote_2073", "Field Note: Jadeclaw Vyrkol");
            Note(SlicingSourceType.Terminal, 4, "fnote_2053", "Field Note: Frostmaw Glacieron");
            Note(SlicingSourceType.Terminal, 4, "fnote_2006", "Field Note: Ashen Moonprowler");
            Note(SlicingSourceType.Terminal, 5, "fnote_2133", "Field Note: Vermilion Ravager");
            Note(SlicingSourceType.Terminal, 5, "fnote_2103", "Field Note: Silverveil Aerolith");
        }

        private static void AddTools()
        {
            Tool(SlicingSourceType.Lockbox, 1, "slt_ratchet", "Ratchet Bypass Pin");
            Tool(SlicingSourceType.Lockbox, 2, "slt_servo", "Reversible Servo Key");
            Tool(SlicingSourceType.Lockbox, 3, "slt_shunt", "Phase-Shunt Fork");
            Tool(SlicingSourceType.Lockbox, 4, "slt_splice", "Mnemonic Trace Splice");
            Tool(SlicingSourceType.Lockbox, 5, "slt_lattice", "Null-Signature Lattice");

            Tool(SlicingSourceType.Terminal, 1, "stt_sampler", "Continuity Sampler");
            Tool(SlicingSourceType.Terminal, 2, "stt_spectro", "Junction Spectrograph");
            Tool(SlicingSourceType.Terminal, 3, "stt_echo", "Forward-Echo Decoder");
            Tool(SlicingSourceType.Terminal, 4, "stt_overlay", "Route-Overlay Prism");
            Tool(SlicingSourceType.Terminal, 5, "stt_oracle", "Core-Pattern Oracle");
        }

        private static void Named(SlicingSourceType source, int tier, string resref, string name, bool exceptional = false)
        {
            Add(source, tier, resref, name, SlicingRewardCategory.NamedItem, exceptional);
        }

        private static void Schematic(SlicingSourceType source, int tier, string resref, string name)
        {
            Add(source, tier, resref, name, SlicingRewardCategory.Schematic);
        }

        private static void Note(SlicingSourceType source, int tier, string resref, string name)
        {
            Add(source, tier, resref, name, SlicingRewardCategory.FieldNote);
        }

        private static void Tool(SlicingSourceType source, int tier, string resref, string name)
        {
            Add(source, tier, resref, name, SlicingRewardCategory.Tool);
        }

        private static void Add(
            SlicingSourceType source,
            int tier,
            string resref,
            string name,
            SlicingRewardCategory category,
            bool exceptional = false)
        {
            _entries.Add(new SlicingRewardEntry
            {
                Source = source,
                Tier = tier,
                Resref = resref,
                Name = name,
                Category = category,
                IsExceptional = exceptional
            });
        }
    }
}
