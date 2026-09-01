namespace SWLOR.Game.Server.Service.KeyItemService
{
    public enum KeyItemType
    {
        [KeyItem(KeyItemCategoryType.Invalid, "Invalid", false, "")]
        Invalid = 0,
        [KeyItem(KeyItemCategoryType.QuestItems, "Avix Tatham's Work Receipt", true, "You received this work receipt from Avix Tatham, mining coordinator on CZ-220.")]
        AvixTathamsWorkReceipt = 1,
        [KeyItem(KeyItemCategoryType.QuestItems, "Halron Linth's Work Receipt", true, "You received this work receipt from Halron Linth, security officer on CZ-220.")]
        HalronLinthsWorkReceipt = 2,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crafting Terminal Droid Operator's Work Receipt", true, "You received this work receipt from the Crafting Terminal Droid Operator on CZ-220.")]
        CraftingTerminalDroidOperatorsWorkReceipt = 3,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crafting Terminal Droid Operator's Work Order", true, "This is a work order you received from the droid responsible for item construction on CZ-220. Obtain the item(s) requested and return them to him.")]
        CraftingTerminalDroidOperatorsWorkOrder = 4,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Shuttle Pass", true, "This shuttle pass enables you to travel between CZ-220 and planet Viscara.")]
        CZ220ShuttlePass = 5,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Experiment Room Key", true, "This unlocks the door leading to the experiment room, where the Colicoid should be located.")]
        CZ220ExperimentRoomKey = 6,
        [KeyItem(KeyItemCategoryType.Keys, "Mandalorian Facility Key", true, "This key unlocks the door to the Mandalorian facility in the Viscara Wildlands.")]
        MandalorianFacilityKey = 7,
        [KeyItem(KeyItemCategoryType.Keys, "Yellow Key Card", true, "This yellow key card can be used somewhere in the Mandalorian facility on Viscara.")]
        YellowKeyCard = 8,
        [KeyItem(KeyItemCategoryType.Keys, "Red Key Card", true, "This red key card can be used somewhere in the Mandalorian facility on Viscara.")]
        RedKeyCard = 9,
        [KeyItem(KeyItemCategoryType.Keys, "Blue Key Card", true, "This blue key card can be used somewhere in the Mandalorian facility on Viscara.")]
        BlueKeyCard = 10,
        [KeyItem(KeyItemCategoryType.QuestItems, "Slicing Program", true, "A data disc with a program used to slice the terminals in the Mandalorian facility.")]
        SlicingProgram = 11,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #1", true, "The first disc containing data on the Mandalorian Facility.")]
        DataDisc1 = 12,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #2", true, "The second disc containing data on the Mandalorian Facility.")]
        DataDisc2 = 13,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #3", true, "The third disc containing data on the Mandalorian Facility.")]
        DataDisc3 = 14,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #4", true, "The fourth disc containing data on the Mandalorian Facility.")]
        DataDisc4 = 15,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #5", true, "The fifth disc containing data on the Mandalorian Facility.")]
        DataDisc5 = 16,
        [KeyItem(KeyItemCategoryType.QuestItems, "Data Disc #6", true, "The sixth disc containing data on the Mandalorian Facility.")]
        DataDisc6 = 17,
        [KeyItem(KeyItemCategoryType.QuestItems, "Package for Denam Reyholm", true, "Roy Moss gave you this package to deliver to Denam Reyholm.")]
        PackageForDenamReyholm = 18,
        [KeyItem(KeyItemCategoryType.Documents, "Old Tome", true, "A man known only as \"L\" gave you this tome. It's very old and the words have faded.")]
        OldTome = 19,
        [KeyItem(KeyItemCategoryType.Keys, "Coxxion Base Key", true, "This key will unlock the doors to the Coxxion base located in the deep mountains of Viscara.")]
        CoxxionBaseKey = 20,
        [KeyItem(KeyItemCategoryType.Keys, "Taxi Hailing Device", true, "This device will enable you to call upon a taxi to quickly transport you across a region.")]
        TaxiHailingDevice = 21,

        [KeyItem(KeyItemCategoryType.Maps, "CZ-220 - Maintenance Level Map", true, "Map of the CZ-220 Maintenance Level.")]
        CZ220MaintenanceLevelMap = 22,
        [KeyItem(KeyItemCategoryType.Maps, "CZ-220 - Offices & Labs Map", true, "Map of the CZ-220 Offices & Labs.")]
        CZ220OfficesAndLabsMap = 23,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Outpost Map", true, "Map of the Hutlar Outpost.")]
        HutlarOutpostMap = 24,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Qion Tundra Map", true, "Map of Qion Tundra.")]
        QionTundraMap = 25,
        [KeyItem(KeyItemCategoryType.Maps, "Hutlar - Qion Valley Map", true, "Map of Qion Valley.")]
        QionValleyMap = 26,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Facility Map", true, "Map of the Coral Isles Facility.")]
        CoralIslesFacilityMap = 27,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Inner Map", true, "Map of the Inner Coral Isles.")]
        CoralIslesInnerMap = 28,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Coral Isles Outer Map", true, "Map of the Outer Coral Isles.")]
        CoralIslesOuterMap = 29,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - The 'Elite' Hotel Map", true, "Map of the 'Elite' Hotel.")]
        EliteHotelMap = 30,

        [KeyItem(KeyItemCategoryType.Maps, "Hutlar Orbit Map", true, "Map of the space surrounding Hutlar.")]
        HutlarOrbitMap = 31,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala Orbit Map", true, "Map of the space surrounding Mon Cala.")]
        MonCalaOrbitMap = 32,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine Orbit Map", true, "Map of the space surrounding Tatooine.")]
        TatooineOrbitMap = 33,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara Orbit Map", true, "Map of the space surrounding Viscara and CZ-220.")]
        ViscaraOrbitMap = 34,

        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Anchorhead Map", true, "Map of Anchorhead.")]
        AnchorheadMap = 35,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Desert Map", true, "Map of the Tatooine deserts.")]
        TatooineDesertMap = 36,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Mos Esper Map", true, "Map of Mos Esper.")]
        MosEsperMap = 37,
        [KeyItem(KeyItemCategoryType.Maps, "Tatooine - Tusken Raider Cave Map", true, "Map of Tusken Raider cave.")]
        TuskenRaiderCaveMap = 38,

        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Cavern Map", true, "Map of Viscara caverns.")]
        ViscaraCavernMap = 39,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Veles Colony Map", true, "Map of Veles Colony.")]
        VelesColonyMap = 40,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Coxxion Base Map", true, "Map of the Coxxion Base.")]
        CoxxionBaseMap = 41,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Deep Mountains Map", true, "Map of the Deep Mountains.")]
        DeepMountainsMap = 42,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Deepwoods Map", true, "Map of the Deepwoods.")]
        DeepwoodsMap = 43,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Druzer Map", true, "Map of Druzer.")]
        DruzerMap = 44,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Swamplands Map", true, "Map of Swamplands.")]
        ViscaraSwamplandsMap = 45,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Jedi Temple Map", true, "Map of the Viscara Jedi Temple.")]
        ViscaraJediTempleMap = 46,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Wildlands Map", true, "Map of the Wildlands.")]
        WildlandsMap = 47,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Mandalorian Facility Map", true, "Map of the Mandalorian Facility.")]
        MandalorianFacilityMap = 48,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Mountain Valley Map", true, "Map of the Mountain Valley.")]
        MountainValleyMap = 49,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Republic Base Map", true, "Map of the Viscara Republic Base.")]
        ViscaraRepublicBaseMap = 50,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Sith Lake Outpost Map", true, "Map of the Viscara Sith Lake Outpost.")]
        SithLakeOutpostMap = 51,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Sewers Map", true, "Map of the Viscara Sewers.")]
        ViscaraSewersMap = 52,
        [KeyItem(KeyItemCategoryType.Maps, "Viscara - Wildwoods Map", true, "Map of the Wildwoods.")]
        WildwoodsMap = 53,

        [KeyItem(KeyItemCategoryType.Maps, "Korriban Orbit Map", true, "Map of the space surrounding Korriban.")]
        KorribanOrbitMap = 54,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Wastelands Map", true, "Map of the wastelands on Korriban.")]
        KorribanWastelandsMap = 55,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Sith Crypt Map", true, "Map of the Sith crypt on Korriban.")]
        KorribanSithCryptMap = 56,
        [KeyItem(KeyItemCategoryType.Maps, "Korriban - Caverns Map", true, "Map of the caverns on Korriban.")]
        KorribanCavernsMap = 57,

        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sunkenhead Swamps Map", true, "Map of the Sunkenhead Swamps on Mon Cala.")]
        MonCalaSunkenhedgeSwampsMap = 58,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sharptooth Jungles Map", true, "Map of the Sharptooth Jungles on Mon Cala.")]
        MonCalaSharptoothJunglesMap = 59,
        [KeyItem(KeyItemCategoryType.Maps, "Mon Cala - Sharptooth Caverns Map", true, "Map of the Sharptooth Caverns on Mon Cala.")]
        MonCalaSharptoothCavernsMap = 60,

        [KeyItem(KeyItemCategoryType.Maps, "Dathomir Orbit Map", true, "Map of the space surrounding Dathomir.")]
        DathomirOrbitMap = 61,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Cave Ruins Map", true, "Map of the Cave Ruins on Dathomir.")]
        DathomirCaveRuinsMap = 62,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Desert Map", true, "Map of the Deserts on Dathomir.")]
        DathomirDesertMap = 63,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Grottos Map", true, "Map of the Grottos on Dathomir.")]
        DathomirGrottosMap = 64,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Grotto Caverns Map", true, "Map of the Grotto Caverns on Dathomir.")]
        DathomirGrottoCavernsMap = 65,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Jungles Map", true, "Map of the Jungles on Dathomir.")]
        DathomirJunglesMap = 66,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Mountains Map", true, "Map of the Mountain region on Dathomir.")]
        DathomirMountainsMap = 67,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Ruins Base Map", true, "Map of the Ruins Base on Dathomir.")]
        DathomirRuinsBaseMap = 68,
        [KeyItem(KeyItemCategoryType.Maps, "Dathomir - Tribes Village Map", true, "Map of the Tribes Village on Dathomir.")]
        DathomirTribeVillageMap = 69,

        [KeyItem(KeyItemCategoryType.Maps, "Dantooine Orbit Map", true, "Map of the space surrounding Dantooine.")]
        DantooineOrbitMap = 70,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Lake Map", true, "Map of the Lake on Dantooine.")]
        DantooineLakeMap = 71,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Kinrath Cave Map", true, "Map of the Kinrath Tunnels on Dantooine.")]
        DantooineKinrathMap = 72,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Tribal Map", true, "Map of the South Plains Tribes on Dantooine.")]
        DantooineTribalMap = 73,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Forsaken Jungles Map", true, "Map of the Forsaken Jungles Caverns on Dantooine.")]
        DantooineForsakenJungleMap = 74,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Mountain Jungles Map", true, "Map of the Mountain Jungles on Dantooine.")]
        DantooineMountainJunglesMap = 75,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Crystal Cave Map", true, "Map of the Crystal Cave on Dantooine.")]
        DantooineCrystalCaveMap = 76,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Abandoned Warehouse Map", true, "Map of the Abandoned Warehouse Base on Dantooine.")]
        DantooineWarehouseMap = 77,
        [KeyItem(KeyItemCategoryType.Maps, "Dantooine - Canyon River Map", true, "Map of the Canyon Rivers on Dantooine.")]
        DantooineCanyonRiverMap = 78,
        [KeyItem(KeyItemCategoryType.QuestItems, "Shovel for cave", true, "You've been given a shovel to clear the rock near the lake cave.")]
        DantooineShovel = 79,
        [KeyItem(KeyItemCategoryType.Keys, "Viscara Lake Basement Key", true, "This key allows you to enter the super secret sith basement! Shhhhhh!")]
        SithBasementKey = 80,
        [KeyItem(KeyItemCategoryType.QuestItems, "Smuggler Pass", true, "You've been given a key pass to Nar Shaddaa.")]
        SmugglerPass = 86,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon Orbit Map", true, "Map of the space surrounding Smuggler's Moon.")]
        SmugglersMoonOrbitMap = 87,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Casino Map", true, "Map of the Casino on Smuggler's Moon.")]
        SmugglersMoonCasinoMap = 88,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Catwalks Map", true, "Map of the Catwalks on Smuggler's Moon.")]
        SmugglersMoonCatwalksMap = 89,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Corporate District Map", true, "Map of the Corporate District on Smuggler's Moon.")]
        SmugglersMoonCorporateDistrictMap = 90,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Czerka Arms Map", true, "Map of Czerka Arms on Smuggler's Moon.")]
        SmugglersMoonCzerkaArmsMap = 91,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Czerka Shipyard Office Map", true, "Map of the Czerka Shipyard Office on Smuggler's Moon.")]
        SmugglersMoonCzerkaShipyardOfficeMap = 92,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Fabrication Facility Map", true, "Map of the Fabrication Facility on Smuggler's Moon.")]
        SmugglersMoonFabricationFacilityMap = 93,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Fight Club Map", true, "Map of the Fight Club on Smuggler's Moon.")]
        SmugglersMoonFightClubMap = 94,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - GSI Base Map", true, "Map of the GSI Base on Smuggler's Moon.")]
        SmugglersMoonGSIBaseMap = 95,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Hyper Dive Cantina Map", true, "Map of the Hyper Dive Cantina on Smuggler's Moon.")]
        SmugglersMoonHyperDiveCantinaMap = 96,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Landing Pads Map", true, "Map of the Landing Pads on Smuggler's Moon.")]
        SmugglersMoonLandingPadsMap = 97,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Medshed Map", true, "Map of the Medshed on Smuggler's Moon.")]
        SmugglersMoonMedshedMap = 98,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Promenade Map", true, "Map of the Promenade on Smuggler's Moon.")]
        SmugglersMoonPromenadeMap = 99,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Sewers Map", true, "Map of the Sewers on Smuggler's Moon.")]
        SmugglersMoonSewersMap = 100,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - Shipping District Map", true, "Map of the Shipping District on Smuggler's Moon.")]
        SmugglersMoonShippingDistrictMap = 101,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - The Hub Map", true, "Map of The Hub on Smuggler's Moon.")]
        SmugglersMoonTheHubMap = 102,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - The Slums Map", true, "Map of The Slums on Smuggler's Moon.")]
        SmugglersMoonTheSlumsMap = 103,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon - The Tilted Visor Map", true, "Map of The Tilted Visor on Smuggler's Moon.")]
        SmugglersMoonTiltedVisorMap = 104,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon Station - Abandoned Station Map", true, "Map of the Abandoned Station on Smuggler's Moon Station.")]
        SmugglersMoonStationAbandonedStationMap = 105,
        [KeyItem(KeyItemCategoryType.Maps, "Smuggler's Moon Station - Lower Level Map", true, "Map of the Lower Level on Smuggler's Moon Station.")]
        SmugglersMoonStationLowerLevelMap = 106,
        [KeyItem(KeyItemCategoryType.Keys, "Viscara Sewers Depths Key", true, "Sera Vonn gave you this key to the sealed Viscara Sewers Depths.")]
        ViscaraSewersDepthsKey = 81,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Vein Codex", true, "Sera Vonn's Blood Frenzy codex, recovered from the Viscara Sewers Depths scavengers.")]
        BloodFrenzyRedVeinCodex = 82,
        [KeyItem(KeyItemCategoryType.QuestItems, "Pulse Metronome", true, "A timing core recovered from the Pulse-Frame Training Droids in the Viscara Sewers Depths.")]
        BloodFrenzyPulseMetronome = 83,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adrenal Glass", true, "A shard of clotted adrenal glass taken after defeating the Blood Frenzy Butcher in the Viscara Sewers Depths.")]
        BloodFrenzyAdrenalGlass = 84,
        [KeyItem(KeyItemCategoryType.QuestItems, "Blood Frenzy Charm Fragments", true, "Fragments recovered from the Blood Frenzy Duelists in the Viscara Sewers Depths.")]
        BloodFrenzyCharmFragments = 85,
        [KeyItem(KeyItemCategoryType.Keys, "Veles Militia Annex Key", true, "This key grants access to the Veles Militia Annex capstone area on Viscara.")]
        CapstoneVelesMilitiaAnnexKey = 107,
        [KeyItem(KeyItemCategoryType.Keys, "Dantooine Jedi Enclave Trial Halls Key", true, "This key grants access to the Dantooine Jedi Enclave Trial Halls capstone area on Dantooine.")]
        CapstoneDantooineJediEnclaveTrialHallsKey = 108,
        [KeyItem(KeyItemCategoryType.Keys, "Korriban Forge Caverns Key", true, "This key grants access to the Korriban Forge Caverns capstone area on Korriban.")]
        CapstoneKorribanForgeCavernsKey = 109,
        [KeyItem(KeyItemCategoryType.Keys, "Smuggler's Moon Fight Club Backrooms Key", true, "This key grants access to the Smuggler's Moon Fight Club Backrooms capstone area on Smuggler's Moon.")]
        CapstoneSmugglersMoonFightClubBackroomsKey = 110,
        [KeyItem(KeyItemCategoryType.Keys, "CZ-220 Breaker Yard Key", true, "This key grants access to the CZ-220 Breaker Yard capstone area on CZ-220.")]
        CapstoneCZ220BreakerYardKey = 111,
        [KeyItem(KeyItemCategoryType.Keys, "Anchorhead Canyon Range Key", true, "This key grants access to the Anchorhead Canyon Range capstone area on Tatooine.")]
        CapstoneAnchorheadCanyonRangeKey = 112,
        [KeyItem(KeyItemCategoryType.Keys, "Czerka Arms Test Range Key", true, "This key grants access to the Czerka Arms Test Range capstone area on Smuggler's Moon.")]
        CapstoneCzerkaArmsTestRangeKey = 113,
        [KeyItem(KeyItemCategoryType.Keys, "Hutlar Qion Test Site Key", true, "This key grants access to the Hutlar Qion Test Site capstone area on Hutlar.")]
        CapstoneHutlarQionTestSiteKey = 114,
        [KeyItem(KeyItemCategoryType.Keys, "Korriban Sith Crypt Depths Key", true, "This key grants access to the Korriban Sith Crypt Depths capstone area on Korriban.")]
        CapstoneKorribanSithCryptDepthsKey = 115,
        [KeyItem(KeyItemCategoryType.Keys, "Viscara Republic Engineering Bunker Key", true, "This key grants access to the Viscara Republic Engineering Bunker capstone area on Viscara.")]
        CapstoneViscaraRepublicEngineeringBunkerKey = 116,
        [KeyItem(KeyItemCategoryType.Keys, "Dantooine Medical Sublevel Key", true, "This key grants access to the Dantooine Medical Sublevel capstone area on Dantooine.")]
        CapstoneDantooineMedicalSublevelKey = 117,
        [KeyItem(KeyItemCategoryType.Keys, "Dathomir Tarn Jungle Preserve Key", true, "This key grants access to the Dathomir Tarn Jungle Preserve capstone area on Dathomir.")]
        CapstoneDathomirTarnJunglePreserveKey = 118,
        [KeyItem(KeyItemCategoryType.Keys, "Dathomir Grotto Apex Den Key", true, "This key grants access to the Dathomir Grotto Apex Den capstone area on Dathomir.")]
        CapstoneDathomirGrottoApexDenKey = 119,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Veles Drill Ledger", true, "A militia drill ledger recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleVelesDrillLedger = 120,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Militia Range Relay", true, "A battered range-control relay recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleMilitiaRangeRelay = 121,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Scored Challenge Badge", true, "A scarred militia challenge badge recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleScoredChallengeBadge = 122,
        [KeyItem(KeyItemCategoryType.QuestItems, "Invincible Captain's Challenge Chit", true, "A captain's encrypted challenge chit recovered in the Veles Militia Annex during the Invincible trial.")]
        CapstoneInvincibleCaptainsChallengeChit = 123,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Veles Drill Ledger", true, "A militia drill ledger recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureVelesDrillLedger = 124,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Militia Range Relay", true, "A battered range-control relay recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureMilitiaRangeRelay = 125,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Scored Challenge Badge", true, "A scarred militia challenge badge recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureScoredChallengeBadge = 126,
        [KeyItem(KeyItemCategoryType.QuestItems, "Vital Rupture Captain's Challenge Chit", true, "A captain's encrypted challenge chit recovered in the Veles Militia Annex during the Vital Rupture trial.")]
        CapstoneVitalRuptureCaptainsChallengeChit = 127,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Veles Drill Ledger", true, "A militia drill ledger recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownVelesDrillLedger = 128,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Militia Range Relay", true, "A battered range-control relay recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownMilitiaRangeRelay = 129,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Scored Challenge Badge", true, "A scarred militia challenge badge recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownScoredChallengeBadge = 130,
        [KeyItem(KeyItemCategoryType.QuestItems, "Systemic Shutdown Captain's Challenge Chit", true, "A captain's encrypted challenge chit recovered in the Veles Militia Annex during the Systemic Shutdown trial.")]
        CapstoneSystemicShutdownCaptainsChallengeChit = 131,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Enclave Trial Slate", true, "An enclave training slate recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormEnclaveTrialSlate = 132,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Kyber Focus Shard", true, "A humming kyber focus shard recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormKyberFocusShard = 133,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Fractured Trial Sigil", true, "A fractured Jedi trial sigil recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormFracturedTrialSigil = 134,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Storm Council Trial Chit", true, "A sealed council trial chit recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Storm trial.")]
        CapstoneSaberStormCouncilTrialChit = 135,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Enclave Trial Slate", true, "An enclave training slate recovered in the Dantooine Jedi Enclave Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterEnclaveTrialSlate = 136,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Kyber Focus Shard", true, "A humming kyber focus shard recovered in the Dantooine Jedi Enclave Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterKyberFocusShard = 137,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Fractured Trial Sigil", true, "A fractured Jedi trial sigil recovered in the Dantooine Jedi Enclave Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterFracturedTrialSigil = 138,
        [KeyItem(KeyItemCategoryType.QuestItems, "Guardian Master Council Trial Chit", true, "A sealed council trial chit recovered in the Dantooine Jedi Enclave Trial Halls during the Guardian Master trial.")]
        CapstoneGuardianMasterCouncilTrialChit = 139,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Enclave Trial Slate", true, "An enclave training slate recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneEnclaveTrialSlate = 140,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Kyber Focus Shard", true, "A humming kyber focus shard recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneKyberFocusShard = 141,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Fractured Trial Sigil", true, "A fractured Jedi trial sigil recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneFracturedTrialSigil = 142,
        [KeyItem(KeyItemCategoryType.QuestItems, "Saber Cyclone Council Trial Chit", true, "A sealed council trial chit recovered in the Dantooine Jedi Enclave Trial Halls during the Saber Cyclone trial.")]
        CapstoneSaberCycloneCouncilTrialChit = 143,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Forge Heat Ledger", true, "A heat-scarred forge ledger recovered in the Korriban Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseForgeHeatLedger = 144,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Sith Tempering Matrix", true, "A volatile Sith tempering matrix recovered in the Korriban Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseSithTemperingMatrix = 145,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Cracked Anvil Sigil", true, "A cracked forge-anvil sigil recovered in the Korriban Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseCrackedAnvilSigil = 146,
        [KeyItem(KeyItemCategoryType.QuestItems, "Absolute Defense Overseer's Clearance Token", true, "An overseer's clearance token recovered in the Korriban Forge Caverns during the Absolute Defense trial.")]
        CapstoneAbsoluteDefenseOverseersClearanceToken = 147,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Forge Heat Ledger", true, "A heat-scarred forge ledger recovered in the Korriban Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionForgeHeatLedger = 148,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Sith Tempering Matrix", true, "A volatile Sith tempering matrix recovered in the Korriban Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionSithTemperingMatrix = 149,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Cracked Anvil Sigil", true, "A cracked forge-anvil sigil recovered in the Korriban Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionCrackedAnvilSigil = 150,
        [KeyItem(KeyItemCategoryType.QuestItems, "Soul Ascension Overseer's Clearance Token", true, "An overseer's clearance token recovered in the Korriban Forge Caverns during the Soul Ascension trial.")]
        CapstoneSoulAscensionOverseersClearanceToken = 151,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Forge Heat Ledger", true, "A heat-scarred forge ledger recovered in the Korriban Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneForgeHeatLedger = 152,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Sith Tempering Matrix", true, "A volatile Sith tempering matrix recovered in the Korriban Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneSithTemperingMatrix = 153,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Cracked Anvil Sigil", true, "A cracked forge-anvil sigil recovered in the Korriban Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneCrackedAnvilSigil = 154,
        [KeyItem(KeyItemCategoryType.QuestItems, "Forcebane Overseer's Clearance Token", true, "An overseer's clearance token recovered in the Korriban Forge Caverns during the Forcebane trial.")]
        CapstoneForcebaneOverseersClearanceToken = 155,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Backroom Bout Ledger", true, "A coded backroom bout ledger recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefenseBackroomBoutLedger = 156,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Ring Shock Regulator", true, "A tampered ring shock regulator recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefenseRingShockRegulator = 157,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Cracked Pit Sigil", true, "A cracked pit-fighter sigil recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefenseCrackedPitSigil = 158,
        [KeyItem(KeyItemCategoryType.QuestItems, "Crippling Defense Promoter's Payout Chit", true, "A promoter's hidden payout chit recovered in the Smuggler's Moon Fight Club Backrooms during the Crippling Defense trial.")]
        CapstoneCripplingDefensePromotersPayoutChit = 159,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Backroom Bout Ledger", true, "A coded backroom bout ledger recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomBackroomBoutLedger = 160,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Ring Shock Regulator", true, "A tampered ring shock regulator recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomRingShockRegulator = 161,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Cracked Pit Sigil", true, "A cracked pit-fighter sigil recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomCrackedPitSigil = 162,
        [KeyItem(KeyItemCategoryType.QuestItems, "Tempest Bloom Promoter's Payout Chit", true, "A promoter's hidden payout chit recovered in the Smuggler's Moon Fight Club Backrooms during the Tempest Bloom trial.")]
        CapstoneTempestBloomPromotersPayoutChit = 163,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Backroom Bout Ledger", true, "A coded backroom bout ledger recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomBackroomBoutLedger = 164,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Ring Shock Regulator", true, "A tampered ring shock regulator recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomRingShockRegulator = 165,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Cracked Pit Sigil", true, "A cracked pit-fighter sigil recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomCrackedPitSigil = 166,
        [KeyItem(KeyItemCategoryType.QuestItems, "Red Bloom Promoter's Payout Chit", true, "A promoter's hidden payout chit recovered in the Smuggler's Moon Fight Club Backrooms during the Red Bloom trial.")]
        CapstoneRedBloomPromotersPayoutChit = 167,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Breaker Yard Work Order", true, "A grease-stained breaker yard work order recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardBreakerYardWorkOrder = 168,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Junkline Control Relay", true, "A sparking junkline control relay recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardJunklineControlRelay = 169,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Sheared Bay Sigil", true, "A sheared breaker bay sigil recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardShearedBaySigil = 170,
        [KeyItem(KeyItemCategoryType.QuestItems, "Adamantine Guard Foreman's Override Chip", true, "A foreman's override chip recovered in the CZ-220 Breaker Yard during the Adamantine Guard trial.")]
        CapstoneAdamantineGuardForemansOverrideChip = 171,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Breaker Yard Work Order", true, "A grease-stained breaker yard work order recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownBreakerYardWorkOrder = 172,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Junkline Control Relay", true, "A sparking junkline control relay recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownJunklineControlRelay = 173,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Sheared Bay Sigil", true, "A sheared breaker bay sigil recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownShearedBaySigil = 174,
        [KeyItem(KeyItemCategoryType.QuestItems, "Scrapheap Lockdown Foreman's Override Chip", true, "A foreman's override chip recovered in the CZ-220 Breaker Yard during the Scrapheap Lockdown trial.")]
        CapstoneScrapheapLockdownForemansOverrideChip = 175,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Breaker Yard Work Order", true, "A grease-stained breaker yard work order recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerBreakerYardWorkOrder = 176,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Junkline Control Relay", true, "A sparking junkline control relay recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerJunklineControlRelay = 177,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Sheared Bay Sigil", true, "A sheared breaker bay sigil recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerShearedBaySigil = 178,
        [KeyItem(KeyItemCategoryType.QuestItems, "Worldbreaker Foreman's Override Chip", true, "A foreman's override chip recovered in the CZ-220 Breaker Yard during the Worldbreaker trial.")]
        CapstoneWorldbreakerForemansOverrideChip = 179,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Canyon Range Tally", true, "A sand-worn canyon range tally recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterCanyonRangeTally = 180,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Sightline Calibrator", true, "A heat-bleached sightline calibrator recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterSightlineCalibrator = 181,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Shattered Range Crest", true, "A shattered marksman range crest recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterShatteredRangeCrest = 182,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unmoving Center Marshal's Challenge Chit", true, "A marshal's range challenge chit recovered in the Anchorhead Canyon Range during the Unmoving Center trial.")]
        CapstoneUnmovingCenterMarshalsChallengeChit = 183,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Canyon Range Tally", true, "A sand-worn canyon range tally recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordCanyonRangeTally = 184,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Sightline Calibrator", true, "A heat-bleached sightline calibrator recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordSightlineCalibrator = 185,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Shattered Range Crest", true, "A shattered marksman range crest recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordShatteredRangeCrest = 186,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Word Marshal's Challenge Chit", true, "A marshal's range challenge chit recovered in the Anchorhead Canyon Range during the Last Word trial.")]
        CapstoneLastWordMarshalsChallengeChit = 187,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Canyon Range Tally", true, "A sand-worn canyon range tally recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandCanyonRangeTally = 188,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Sightline Calibrator", true, "A heat-bleached sightline calibrator recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandSightlineCalibrator = 189,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Shattered Range Crest", true, "A shattered marksman range crest recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandShatteredRangeCrest = 190,
        [KeyItem(KeyItemCategoryType.QuestItems, "Dead Man's Hand Marshal's Challenge Chit", true, "A marshal's range challenge chit recovered in the Anchorhead Canyon Range during the Dead Man's Hand trial.")]
        CapstoneDeadMansHandMarshalsChallengeChit = 191,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Czerka Test Docket", true, "A redacted Czerka test docket recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxCzerkaTestDocket = 192,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Blast-Cell Regulator", true, "A Czerka blast-cell regulator recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxBlastCellRegulator = 193,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Scored Range Crest", true, "A scored Czerka range crest recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxScoredRangeCrest = 194,
        [KeyItem(KeyItemCategoryType.QuestItems, "Kill Box Czerka Clearance Chit", true, "A Czerka clearance chit recovered in the Czerka Arms Test Range during the Kill Box trial.")]
        CapstoneKillBoxCzerkaClearanceChit = 195,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Czerka Test Docket", true, "A redacted Czerka test docket recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotCzerkaTestDocket = 196,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Blast-Cell Regulator", true, "A Czerka blast-cell regulator recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotBlastCellRegulator = 197,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Scored Range Crest", true, "A scored Czerka range crest recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotScoredRangeCrest = 198,
        [KeyItem(KeyItemCategoryType.QuestItems, "One Shot Czerka Clearance Chit", true, "A Czerka clearance chit recovered in the Czerka Arms Test Range during the One Shot trial.")]
        CapstoneOneShotCzerkaClearanceChit = 199,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Czerka Test Docket", true, "A redacted Czerka test docket recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelCzerkaTestDocket = 200,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Blast-Cell Regulator", true, "A Czerka blast-cell regulator recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelBlastCellRegulator = 201,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Scored Range Crest", true, "A scored Czerka range crest recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelScoredRangeCrest = 202,
        [KeyItem(KeyItemCategoryType.QuestItems, "Rain of Steel Czerka Clearance Chit", true, "A Czerka clearance chit recovered in the Czerka Arms Test Range during the Rain of Steel trial.")]
        CapstoneRainOfSteelCzerkaClearanceChit = 203,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Qion Test Log", true, "A frost-cracked Qion test log recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurryQionTestLog = 204,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Cryo-Range Regulator", true, "A malfunctioning cryo-range regulator recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurryCryoRangeRegulator = 205,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Frostburned Test Crest", true, "A frostburned weapons test crest recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurryFrostburnedTestCrest = 206,
        [KeyItem(KeyItemCategoryType.QuestItems, "Perfect Flurry Site Chief's Override Chip", true, "A site chief's override chip recovered in the Hutlar Qion Test Site during the Perfect Flurry trial.")]
        CapstonePerfectFlurrySiteChiefsOverrideChip = 207,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Qion Test Log", true, "A frost-cracked Qion test log recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorQionTestLog = 208,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Cryo-Range Regulator", true, "A malfunctioning cryo-range regulator recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorCryoRangeRegulator = 209,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Frostburned Test Crest", true, "A frostburned weapons test crest recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorFrostburnedTestCrest = 210,
        [KeyItem(KeyItemCategoryType.QuestItems, "Thermal Detonator Site Chief's Override Chip", true, "A site chief's override chip recovered in the Hutlar Qion Test Site during the Thermal Detonator trial.")]
        CapstoneThermalDetonatorSiteChiefsOverrideChip = 211,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Qion Test Log", true, "A frost-cracked Qion test log recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageQionTestLog = 212,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Cryo-Range Regulator", true, "A malfunctioning cryo-range regulator recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageCryoRangeRegulator = 213,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Frostburned Test Crest", true, "A frostburned weapons test crest recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageFrostburnedTestCrest = 214,
        [KeyItem(KeyItemCategoryType.QuestItems, "Overload Barrage Site Chief's Override Chip", true, "A site chief's override chip recovered in the Hutlar Qion Test Site during the Overload Barrage trial.")]
        CapstoneOverloadBarrageSiteChiefsOverrideChip = 215,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Crypt Trial Tablet", true, "An etched crypt trial tablet recovered in the Korriban Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightCryptTrialTablet = 216,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Ritual Focus Shard", true, "A pulsing ritual focus shard recovered in the Korriban Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightRitualFocusShard = 217,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Splintered Tomb Sigil", true, "A splintered Sith tomb sigil recovered in the Korriban Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightSplinteredTombSigil = 218,
        [KeyItem(KeyItemCategoryType.QuestItems, "Last Stand of the Light Keeper's Rite Token", true, "A crypt keeper's rite token recovered in the Korriban Sith Crypt Depths during the Last Stand of the Light trial.")]
        CapstoneLastStandOfTheLightKeepersRiteToken = 219,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Crypt Trial Tablet", true, "An etched crypt trial tablet recovered in the Korriban Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkCryptTrialTablet = 220,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Ritual Focus Shard", true, "A pulsing ritual focus shard recovered in the Korriban Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkRitualFocusShard = 221,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Splintered Tomb Sigil", true, "A splintered Sith tomb sigil recovered in the Korriban Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkSplinteredTombSigil = 222,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hunger of the Dark Keeper's Rite Token", true, "A crypt keeper's rite token recovered in the Korriban Sith Crypt Depths during the Hunger of the Dark trial.")]
        CapstoneHungerOfTheDarkKeepersRiteToken = 223,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Crypt Trial Tablet", true, "An etched crypt trial tablet recovered in the Korriban Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveCryptTrialTablet = 224,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Ritual Focus Shard", true, "A pulsing ritual focus shard recovered in the Korriban Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveRitualFocusShard = 225,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Splintered Tomb Sigil", true, "A splintered Sith tomb sigil recovered in the Korriban Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveSplinteredTombSigil = 226,
        [KeyItem(KeyItemCategoryType.QuestItems, "Eclipse of Resolve Keeper's Rite Token", true, "A crypt keeper's rite token recovered in the Korriban Sith Crypt Depths during the Eclipse of Resolve trial.")]
        CapstoneEclipseOfResolveKeepersRiteToken = 227,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Republic Bunker Docket", true, "A Republic bunker operations docket recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconRepublicBunkerDocket = 228,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Shield Grid Relay", true, "A Republic shield-grid relay recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconShieldGridRelay = 229,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Cracked Command Crest", true, "A cracked Republic command crest recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconCrackedCommandCrest = 230,
        [KeyItem(KeyItemCategoryType.QuestItems, "Killzone Beacon Quartermaster Override Chip", true, "A quartermaster override chip recovered in the Viscara Republic Engineering Bunker during the Killzone Beacon trial.")]
        CapstoneKillzoneBeaconQuartermasterOverrideChip = 231,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Republic Bunker Docket", true, "A Republic bunker operations docket recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerRepublicBunkerDocket = 232,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Shield Grid Relay", true, "A Republic shield-grid relay recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerShieldGridRelay = 233,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Cracked Command Crest", true, "A cracked Republic command crest recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerCrackedCommandCrest = 234,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Bunker Quartermaster Override Chip", true, "A quartermaster override chip recovered in the Viscara Republic Engineering Bunker during the Emergency Bunker trial.")]
        CapstoneEmergencyBunkerQuartermasterOverrideChip = 235,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Republic Bunker Docket", true, "A Republic bunker operations docket recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandRepublicBunkerDocket = 236,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Shield Grid Relay", true, "A Republic shield-grid relay recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandShieldGridRelay = 237,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Cracked Command Crest", true, "A cracked Republic command crest recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandCrackedCommandCrest = 238,
        [KeyItem(KeyItemCategoryType.QuestItems, "Decisive Command Quartermaster Override Chip", true, "A quartermaster override chip recovered in the Viscara Republic Engineering Bunker during the Decisive Command trial.")]
        CapstoneDecisiveCommandQuartermasterOverrideChip = 239,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Triage Ward Ledger", true, "A triage ward ledger recovered in the Dantooine Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineTriageWardLedger = 240,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Kolto Conduit Coupler", true, "A pressurized kolto conduit coupler recovered in the Dantooine Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineKoltoConduitCoupler = 241,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Fractured Ward Sigil", true, "A fractured medical ward sigil recovered in the Dantooine Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineFracturedWardSigil = 242,
        [KeyItem(KeyItemCategoryType.QuestItems, "Hold the Line Matron's Ward Token", true, "A matron's ward token recovered in the Dantooine Medical Sublevel during the Hold the Line trial.")]
        CapstoneHoldTheLineMatronsWardToken = 243,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Triage Ward Ledger", true, "A triage ward ledger recovered in the Dantooine Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailTriageWardLedger = 244,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Kolto Conduit Coupler", true, "A pressurized kolto conduit coupler recovered in the Dantooine Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailKoltoConduitCoupler = 245,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Fractured Ward Sigil", true, "A fractured medical ward sigil recovered in the Dantooine Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailFracturedWardSigil = 246,
        [KeyItem(KeyItemCategoryType.QuestItems, "Emergency Cocktail Matron's Ward Token", true, "A matron's ward token recovered in the Dantooine Medical Sublevel during the Emergency Cocktail trial.")]
        CapstoneEmergencyCocktailMatronsWardToken = 247,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Triage Ward Ledger", true, "A triage ward ledger recovered in the Dantooine Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitTriageWardLedger = 248,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Kolto Conduit Coupler", true, "A pressurized kolto conduit coupler recovered in the Dantooine Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitKoltoConduitCoupler = 249,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Fractured Ward Sigil", true, "A fractured medical ward sigil recovered in the Dantooine Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitFracturedWardSigil = 250,
        [KeyItem(KeyItemCategoryType.QuestItems, "Infinite Conduit Matron's Ward Token", true, "A matron's ward token recovered in the Dantooine Medical Sublevel during the Infinite Conduit trial.")]
        CapstoneInfiniteConduitMatronsWardToken = 251,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Tarn Hunt Tally", true, "A scratched tarn hunt tally recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBiteTarnHuntTally = 252,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Beast-Pen Scent Vial", true, "A pungent beast-pen scent vial recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBiteBeastPenScentVial = 253,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Clawed Alpha Totem", true, "A clawed alpha-beast totem recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBiteClawedAlphaTotem = 254,
        [KeyItem(KeyItemCategoryType.QuestItems, "Apex Bite Preserve Keeper's Token", true, "A preserve keeper's bone token recovered in the Dathomir Tarn Jungle Preserve during the Apex Bite trial.")]
        CapstoneApexBitePreserveKeepersToken = 255,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Tarn Hunt Tally", true, "A scratched tarn hunt tally recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastTarnHuntTally = 256,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Beast-Pen Scent Vial", true, "A pungent beast-pen scent vial recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastBeastPenScentVial = 257,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Clawed Alpha Totem", true, "A clawed alpha-beast totem recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastClawedAlphaTotem = 258,
        [KeyItem(KeyItemCategoryType.QuestItems, "Unbreakable Beast Preserve Keeper's Token", true, "A preserve keeper's bone token recovered in the Dathomir Tarn Jungle Preserve during the Unbreakable Beast trial.")]
        CapstoneUnbreakableBeastPreserveKeepersToken = 259,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Tarn Hunt Tally", true, "A scratched tarn hunt tally recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmTarnHuntTally = 260,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Beast-Pen Scent Vial", true, "A pungent beast-pen scent vial recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmBeastPenScentVial = 261,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Clawed Alpha Totem", true, "A clawed alpha-beast totem recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmClawedAlphaTotem = 262,
        [KeyItem(KeyItemCategoryType.QuestItems, "Alpha Rhythm Preserve Keeper's Token", true, "A preserve keeper's bone token recovered in the Dathomir Tarn Jungle Preserve during the Alpha Rhythm trial.")]
        CapstoneAlphaRhythmPreserveKeepersToken = 263,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Grotto Track Slate", true, "A mud-darkened grotto track slate recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunGrottoTrackSlate = 264,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Resonant Fang Charm", true, "A resonant fang charm recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunResonantFangCharm = 265,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Cracked Apex Totem", true, "A cracked apex-beast totem recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunCrackedApexTotem = 266,
        [KeyItem(KeyItemCategoryType.QuestItems, "Primal Overrun Den-Mother's Fang Token", true, "A den-mother's fang token recovered in the Dathomir Grotto Apex Den during the Primal Overrun trial.")]
        CapstonePrimalOverrunDenMothersFangToken = 267,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Grotto Track Slate", true, "A mud-darkened grotto track slate recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctGrottoTrackSlate = 268,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Resonant Fang Charm", true, "A resonant fang charm recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctResonantFangCharm = 269,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Cracked Apex Totem", true, "A cracked apex-beast totem recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctCrackedApexTotem = 270,
        [KeyItem(KeyItemCategoryType.QuestItems, "Untouchable Instinct Den-Mother's Fang Token", true, "A den-mother's fang token recovered in the Dathomir Grotto Apex Den during the Untouchable Instinct trial.")]
        CapstoneUntouchableInstinctDenMothersFangToken = 271,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Grotto Track Slate", true, "A mud-darkened grotto track slate recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastGrottoTrackSlate = 272,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Resonant Fang Charm", true, "A resonant fang charm recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastResonantFangCharm = 273,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Cracked Apex Totem", true, "A cracked apex-beast totem recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastCrackedApexTotem = 274,
        [KeyItem(KeyItemCategoryType.QuestItems, "Force-Bonded Beast Den-Mother's Fang Token", true, "A den-mother's fang token recovered in the Dathomir Grotto Apex Den during the Force-Bonded Beast trial.")]
        CapstoneForceBondedBeastDenMothersFangToken = 275,

        // Incubation field notes — one per mutation target beast. The declared Name is a
        // readable fallback; IncubationFieldNote injects the canonical Name and the full
        // requirement Description at boot from the live mutation configuration.
        [KeyItem(KeyItemCategoryType.FieldNotes, "Aardvark", true, "")]
        IncubationFieldNoteAardvark = 2000,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Abyssweb Ravager", true, "")]
        IncubationFieldNoteAbysswebRavager = 2001,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Allosaurus", true, "")]
        IncubationFieldNoteAllosaurus = 2002,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Amberhide Nimbrel", true, "")]
        IncubationFieldNoteAmberhideNimbrel = 2003,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Amethyst Selori", true, "")]
        IncubationFieldNoteAmethystSelori = 2004,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Armourback Spineguard", true, "")]
        IncubationFieldNoteArmourbackSpineguard = 2005,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ashen Moonprowler", true, "")]
        IncubationFieldNoteAshenMoonprowler = 2006,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Azurehorn Kargath", true, "")]
        IncubationFieldNoteAzurehornKargath = 2007,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Balanoro Force Mite", true, "")]
        IncubationFieldNoteBalanoroForceMite = 2008,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Basalt Gorgath", true, "")]
        IncubationFieldNoteBasaltGorgath = 2009,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bearbug", true, "")]
        IncubationFieldNoteBearbug = 2010,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bhalir", true, "")]
        IncubationFieldNoteBhalir = 2011,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Binarian Sabercat", true, "")]
        IncubationFieldNoteBinarianSabercat = 2012,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Blastail", true, "")]
        IncubationFieldNoteBlastail = 2013,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Blinkstep Vekara", true, "")]
        IncubationFieldNoteBlinkstepVekara = 2014,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Blistering Beetle", true, "")]
        IncubationFieldNoteBlisteringBeetle = 2015,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bloodtusk Ravor", true, "")]
        IncubationFieldNoteBloodtuskRavor = 2016,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Boma Beast", true, "")]
        IncubationFieldNoteBomaBeast = 2017,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Boma Beast Baby", true, "")]
        IncubationFieldNoteBomaBeastBaby = 2018,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bramble Lynx", true, "")]
        IncubationFieldNoteBrambleLynx = 2019,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Brassjaw Pyralisk", true, "")]
        IncubationFieldNoteBrassjawPyralisk = 2020,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Bronzecrest Thundros", true, "")]
        IncubationFieldNoteBronzecrestThundros = 2021,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Burrowberry Bird", true, "")]
        IncubationFieldNoteBurrowberryBird = 2022,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Burrowberry Pack", true, "")]
        IncubationFieldNoteBurrowberryPack = 2023,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cannok", true, "")]
        IncubationFieldNoteCannok = 2024,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Char Hound", true, "")]
        IncubationFieldNoteCharHound = 2025,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cloudcall Aurelith", true, "")]
        IncubationFieldNoteCloudcallAurelith = 2026,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cobalt Hornwyrm", true, "")]
        IncubationFieldNoteCobaltHornwyrm = 2027,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Coppercoil Mirelisk", true, "")]
        IncubationFieldNoteCoppercoilMirelisk = 2028,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cragmane Valshar", true, "")]
        IncubationFieldNoteCragmaneValshar = 2029,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Cragscale", true, "")]
        IncubationFieldNoteCragscale = 2030,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Crimson Skyrender", true, "")]
        IncubationFieldNoteCrimsonSkyrender = 2031,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Crocodile", true, "")]
        IncubationFieldNoteCrocodile = 2032,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Crystalflow Skimmer", true, "")]
        IncubationFieldNoteCrystalflowSkimmer = 2033,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dathomir Wyrmling", true, "")]
        IncubationFieldNoteDathomirWyrmling = 2034,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dawnfang Hound", true, "")]
        IncubationFieldNoteDawnfangHound = 2035,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Deeprock Mauler", true, "")]
        IncubationFieldNoteDeeprockMauler = 2036,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Deepstone Graxal", true, "")]
        IncubationFieldNoteDeepstoneGraxal = 2037,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Deepwoods Rager", true, "")]
        IncubationFieldNoteDeepwoodsRager = 2038,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dewback", true, "")]
        IncubationFieldNoteDewback = 2039,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Direfang Lupikar", true, "")]
        IncubationFieldNoteDirefangLupikar = 2040,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dreadmaw Barghest", true, "")]
        IncubationFieldNoteDreadmawBarghest = 2041,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dreamcap Myconite", true, "")]
        IncubationFieldNoteDreamcapMyconite = 2042,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Dreamwalker", true, "")]
        IncubationFieldNoteDreamwalker = 2043,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Drexclaw Marauder", true, "")]
        IncubationFieldNoteDrexclawMarauder = 2044,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Duneshag Bantha", true, "")]
        IncubationFieldNoteDuneshagBantha = 2045,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Duskfang Hound", true, "")]
        IncubationFieldNoteDuskfangHound = 2046,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Duskmane Ursadon", true, "")]
        IncubationFieldNoteDuskmaneUrsadon = 2047,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Elderspore Oraculum", true, "")]
        IncubationFieldNoteEldersporeOraculum = 2048,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Emberback Bristal", true, "")]
        IncubationFieldNoteEmberbackBristal = 2049,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Emeraldcrest Kalyth", true, "")]
        IncubationFieldNoteEmeraldcrestKalyth = 2050,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Frog", true, "")]
        IncubationFieldNoteFrog = 2051,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Frostback Spineguard", true, "")]
        IncubationFieldNoteFrostbackSpineguard = 2052,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Frostmaw Glacieron", true, "")]
        IncubationFieldNoteFrostmawGlacieron = 2053,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Fungal Shambler", true, "")]
        IncubationFieldNoteFungalShambler = 2054,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Garral", true, "")]
        IncubationFieldNoteGarral = 2055,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Garu Bear Ripper", true, "")]
        IncubationFieldNoteGaruBearRipper = 2056,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Giant Garu Bear", true, "")]
        IncubationFieldNoteGiantGaruBear = 2057,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Gilded Mirewyrm", true, "")]
        IncubationFieldNoteGildedMirewyrm = 2058,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Glimmerwing Mykal", true, "")]
        IncubationFieldNoteGlimmerwingMykal = 2059,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Gloomthread Skiver", true, "")]
        IncubationFieldNoteGloomthreadSkiver = 2060,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Goldpelt Sahrak", true, "")]
        IncubationFieldNoteGoldpeltSahrak = 2061,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Graniteback Ursavar", true, "")]
        IncubationFieldNoteGranitebackUrsavar = 2062,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Graymire Amalgam", true, "")]
        IncubationFieldNoteGraymireAmalgam = 2063,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Greenbulk Wallow", true, "")]
        IncubationFieldNoteGreenbulkWallow = 2064,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Grutchin", true, "")]
        IncubationFieldNoteGrutchin = 2065,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Hanadak", true, "")]
        IncubationFieldNoteHanadak = 2066,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Horned Kath Hound", true, "")]
        IncubationFieldNoteHornedKathHound = 2067,
        [KeyItem(KeyItemCategoryType.FieldNotes, "House Cat", true, "")]
        IncubationFieldNoteHouseCat = 2068,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Hssiss", true, "")]
        IncubationFieldNoteHssiss = 2069,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Hutlar Penguin", true, "")]
        IncubationFieldNoteHutlarPenguin = 2070,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Icewing Kestrelith", true, "")]
        IncubationFieldNoteIcewingKestrelith = 2071,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ironmaw Bastionback", true, "")]
        IncubationFieldNoteIronmawBastionback = 2072,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Jadeclaw Vyrkol", true, "")]
        IncubationFieldNoteJadeclawVyrkol = 2073,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Juvenile Chirodactyl", true, "")]
        IncubationFieldNoteJuvenileChirodactyl = 2074,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Juvenile Rancor", true, "")]
        IncubationFieldNoteJuvenileRancor = 2075,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Katarn", true, "")]
        IncubationFieldNoteKatarn = 2076,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Moonthorn Veloria", true, "")]
        IncubationFieldNoteMoonthornVeloria = 2077,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mush Warrior", true, "")]
        IncubationFieldNoteMushWarrior = 2078,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mustardlash Slime", true, "")]
        IncubationFieldNoteMustardlashSlime = 2079,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mutated Boar", true, "")]
        IncubationFieldNoteMutatedBoar = 2080,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Mutated Frog", true, "")]
        IncubationFieldNoteMutatedFrog = 2081,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Nightspot Aralynx", true, "")]
        IncubationFieldNoteNightspotAralynx = 2082,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ochre Maw", true, "")]
        IncubationFieldNoteOchreMaw = 2083,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Orbak Water Horse", true, "")]
        IncubationFieldNoteOrbakWaterHorse = 2084,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Orray", true, "")]
        IncubationFieldNoteOrray = 2085,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Phaseleg Silkstalker", true, "")]
        IncubationFieldNotePhaselegSilkstalker = 2086,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Porg", true, "")]
        IncubationFieldNotePorg = 2087,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Pyrestem Scarab", true, "")]
        IncubationFieldNotePyrestemScarab = 2088,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Razorhide Hound", true, "")]
        IncubationFieldNoteRazorhideHound = 2089,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Redcrest Tatterquill", true, "")]
        IncubationFieldNoteRedcrestTatterquill = 2090,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ronto", true, "")]
        IncubationFieldNoteRonto = 2091,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Rootbound Colossus", true, "")]
        IncubationFieldNoteRootboundColossus = 2092,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Royal Plumage", true, "")]
        IncubationFieldNoteRoyalPlumage = 2093,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Rubyback Drakon", true, "")]
        IncubationFieldNoteRubybackDrakon = 2094,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ruinfang Mongrel", true, "")]
        IncubationFieldNoteRuinfangMongrel = 2095,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Rustwhisker Gnawfiend", true, "")]
        IncubationFieldNoteRustwhiskerGnawfiend = 2096,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Saberleg Kharaxis", true, "")]
        IncubationFieldNoteSaberlegKharaxis = 2097,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sapphire Veylori", true, "")]
        IncubationFieldNoteSapphireVeylori = 2098,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sapphireback Vorex", true, "")]
        IncubationFieldNoteSapphirebackVorex = 2099,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Scrap Rat", true, "")]
        IncubationFieldNoteScrapRat = 2100,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Serene Grovetreader", true, "")]
        IncubationFieldNoteSereneGrovetreader = 2101,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Shatterpelt Lurax", true, "")]
        IncubationFieldNoteShatterpeltLurax = 2102,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Silverveil Aerolith", true, "")]
        IncubationFieldNoteSilverveilAerolith = 2103,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sink Crab", true, "")]
        IncubationFieldNoteSinkCrab = 2104,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sootbelly Mirekit", true, "")]
        IncubationFieldNoteSootbellyMirekit = 2105,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Speckled Seer", true, "")]
        IncubationFieldNoteSpeckledSeer = 2106,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Spined Crawler", true, "")]
        IncubationFieldNoteSpinedCrawler = 2107,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Spinosaurus", true, "")]
        IncubationFieldNoteSpinosaurus = 2108,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Stegosaurus", true, "")]
        IncubationFieldNoteStegosaurus = 2109,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Stinging Swarm", true, "")]
        IncubationFieldNoteStingingSwarm = 2110,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Stoneclad Behemoth", true, "")]
        IncubationFieldNoteStonecladBehemoth = 2111,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Strayfang Kavor", true, "")]
        IncubationFieldNoteStrayfangKavor = 2112,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Sumpback Chitinmaw", true, "")]
        IncubationFieldNoteSumpbackChitinmaw = 2113,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Swamp Rat", true, "")]
        IncubationFieldNoteSwampRat = 2114,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tach", true, "")]
        IncubationFieldNoteTach = 2115,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tempest Bulwark", true, "")]
        IncubationFieldNoteTempestBulwark = 2116,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Terentatek", true, "")]
        IncubationFieldNoteTerentatek = 2117,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tideplume Striderel", true, "")]
        IncubationFieldNoteTideplumeStriderel = 2118,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Torosaurus", true, "")]
        IncubationFieldNoteTorosaurus = 2119,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Triceratops", true, "")]
        IncubationFieldNoteTriceratops = 2120,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tukata", true, "")]
        IncubationFieldNoteTukata = 2121,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tundra Ponderer", true, "")]
        IncubationFieldNoteTundraPonderer = 2122,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Tyrannosaurus", true, "")]
        IncubationFieldNoteTyrannosaurus = 2123,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Ubese Thorn", true, "")]
        IncubationFieldNoteUbeseThorn = 2124,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Umberroot Arctara", true, "")]
        IncubationFieldNoteUmberrootArctara = 2125,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Umbral Barghest", true, "")]
        IncubationFieldNoteUmbralBarghest = 2126,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Umbratalon Corvax", true, "")]
        IncubationFieldNoteUmbratalonCorvax = 2127,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Underbrush Scamp", true, "")]
        IncubationFieldNoteUnderbrushScamp = 2128,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Undersea Carver", true, "")]
        IncubationFieldNoteUnderseaCarver = 2129,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Veilphase Arachnyx", true, "")]
        IncubationFieldNoteVeilphaseArachnyx = 2130,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Venomspike Laigrek", true, "")]
        IncubationFieldNoteVenomspikeLaigrek = 2131,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Verdant Thornwold", true, "")]
        IncubationFieldNoteVerdantThornwold = 2132,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Vermilion Ravager", true, "")]
        IncubationFieldNoteVermilionRavager = 2133,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Viridian Platewyrm", true, "")]
        IncubationFieldNoteViridianPlatewyrm = 2134,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Voidmire Echo", true, "")]
        IncubationFieldNoteVoidmireEcho = 2135,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Weasel", true, "")]
        IncubationFieldNoteWeasel = 2136,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Wraid", true, "")]
        IncubationFieldNoteWraid = 2137,
        [KeyItem(KeyItemCategoryType.FieldNotes, "Wraithweb Nythrax", true, "")]
        IncubationFieldNoteWraithwebNythrax = 2138,

	}

	public class KeyItemAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public KeyItemCategoryType Category { get; set; }
        public bool IsActive { get; set; }

        public KeyItemAttribute(KeyItemCategoryType category, string name, bool isActive, string description)
        {
            Category = category;
            Name = name;
            IsActive = isActive;
            Description = description;
        }
    }
}
